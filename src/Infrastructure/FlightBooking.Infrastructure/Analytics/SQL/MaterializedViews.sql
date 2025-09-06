-- =====================================================
-- FLIGHT BOOKING ANALYTICS - MATERIALIZED VIEWS
-- =====================================================

-- Drop existing materialized views (in dependency order)
DROP MATERIALIZED VIEW IF EXISTS mv_passenger_demographics_daily CASCADE;
DROP MATERIALIZED VIEW IF EXISTS mv_booking_status_daily CASCADE;
DROP MATERIALIZED VIEW IF EXISTS mv_route_performance_daily CASCADE;
DROP MATERIALIZED VIEW IF EXISTS mv_revenue_daily CASCADE;

-- =====================================================
-- 1. DAILY REVENUE ANALYTICS MATERIALIZED VIEW
-- =====================================================
CREATE MATERIALIZED VIEW mv_revenue_daily AS
SELECT 
    -- Time Dimensions
    DATE(b.created_at) as date,
    'Daily' as period,
    
    -- Route Dimensions
    COALESCE(r.route_code, 'ALL') as route_code,
    COALESCE(fc.class_code, 'ALL') as fare_class,
    COALESCE(f.airline_code, 'ALL') as airline_code,
    
    -- Revenue Metrics
    SUM(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') 
             THEN b.total_amount ELSE 0 END) as total_revenue,
    SUM(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') 
             THEN b.base_amount ELSE 0 END) as base_revenue,
    SUM(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') 
             THEN b.tax_amount ELSE 0 END) as tax_revenue,
    SUM(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') 
             THEN b.fee_amount ELSE 0 END) as fee_revenue,
    SUM(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') 
             THEN COALESCE(b.extra_services_amount, 0) ELSE 0 END) as extra_services_revenue,
    
    -- Booking Metrics
    COUNT(*) as total_bookings,
    COUNT(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') THEN 1 END) as completed_bookings,
    COUNT(CASE WHEN b.status = 'Cancelled' THEN 1 END) as cancelled_bookings,
    COUNT(CASE WHEN b.status = 'Refunded' THEN 1 END) as refunded_bookings,
    
    -- Passenger Metrics
    SUM(b.passenger_count) as total_passengers,
    CASE WHEN SUM(b.passenger_count) > 0 
         THEN SUM(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') 
                       THEN b.total_amount ELSE 0 END) / SUM(b.passenger_count)
         ELSE 0 END as average_revenue_per_passenger,
    CASE WHEN COUNT(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') THEN 1 END) > 0
         THEN SUM(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') 
                       THEN b.total_amount ELSE 0 END) / COUNT(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') THEN 1 END)
         ELSE 0 END as average_revenue_per_booking,
    
    -- Capacity Metrics (estimated)
    COUNT(DISTINCT f.id) * 150 as total_seats, -- Assuming average 150 seats per flight
    SUM(b.passenger_count) as booked_seats,
    CASE WHEN COUNT(DISTINCT f.id) > 0 
         THEN (SUM(b.passenger_count)::decimal / (COUNT(DISTINCT f.id) * 150)) * 100
         ELSE 0 END as load_factor,
    
    -- Pricing Metrics
    CASE WHEN COUNT(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') THEN 1 END) > 0
         THEN AVG(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') 
                       THEN b.base_amount / b.passenger_count END)
         ELSE 0 END as average_fare_price,
    MIN(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') 
             THEN b.base_amount / b.passenger_count END) as min_fare_price,
    MAX(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') 
             THEN b.base_amount / b.passenger_count END) as max_fare_price,
    
    -- Promotion Metrics
    SUM(CASE WHEN b.promotion_id IS NOT NULL AND b.status IN ('Confirmed', 'CheckedIn', 'Completed')
             THEN COALESCE(b.discount_amount, 0) ELSE 0 END) as promotion_discounts,
    COUNT(CASE WHEN b.promotion_id IS NOT NULL AND b.status IN ('Confirmed', 'CheckedIn', 'Completed') 
               THEN 1 END) as bookings_with_promotions,
    CASE WHEN COUNT(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') THEN 1 END) > 0
         THEN (COUNT(CASE WHEN b.promotion_id IS NOT NULL AND b.status IN ('Confirmed', 'CheckedIn', 'Completed') 
                          THEN 1 END)::decimal / 
               COUNT(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') THEN 1 END)) * 100
         ELSE 0 END as promotion_penetration_rate,
    
    -- Metadata
    NOW() as last_refreshed,
    'MaterializedView' as data_source,
    COUNT(*) as record_count

FROM bookings b
LEFT JOIN flights f ON b.flight_id = f.id
LEFT JOIN routes r ON f.route_id = r.id
LEFT JOIN fare_classes fc ON b.fare_class_id = fc.id
WHERE b.created_at >= CURRENT_DATE - INTERVAL '2 years' -- Keep 2 years of data
GROUP BY 
    CUBE(
        DATE(b.created_at),
        r.route_code,
        fc.class_code,
        f.airline_code
    )
HAVING DATE(b.created_at) IS NOT NULL; -- Exclude NULL date groupings

-- Create indexes for performance
CREATE UNIQUE INDEX idx_mv_revenue_daily_pk ON mv_revenue_daily (date, COALESCE(route_code, ''), COALESCE(fare_class, ''), COALESCE(airline_code, ''));
CREATE INDEX idx_mv_revenue_daily_date ON mv_revenue_daily (date);
CREATE INDEX idx_mv_revenue_daily_route ON mv_revenue_daily (route_code);
CREATE INDEX idx_mv_revenue_daily_class ON mv_revenue_daily (fare_class);

-- =====================================================
-- 2. DAILY BOOKING STATUS ANALYTICS MATERIALIZED VIEW
-- =====================================================
CREATE MATERIALIZED VIEW mv_booking_status_daily AS
SELECT 
    -- Time Dimensions
    DATE(b.created_at) as date,
    'Daily' as period,
    COALESCE(r.route_code, 'ALL') as route_code,
    COALESCE(fc.class_code, 'ALL') as fare_class,
    
    -- Status Counts
    COUNT(CASE WHEN b.status = 'Pending' THEN 1 END) as pending_bookings,
    COUNT(CASE WHEN b.status = 'Confirmed' THEN 1 END) as confirmed_bookings,
    COUNT(CASE WHEN b.status = 'CheckedIn' THEN 1 END) as checked_in_bookings,
    COUNT(CASE WHEN b.status = 'Completed' THEN 1 END) as completed_bookings,
    COUNT(CASE WHEN b.status = 'Cancelled' THEN 1 END) as cancelled_bookings,
    COUNT(CASE WHEN b.status = 'Expired' THEN 1 END) as expired_bookings,
    COUNT(CASE WHEN b.status = 'Refunded' THEN 1 END) as refunded_bookings,
    
    -- Status Percentages
    CASE WHEN COUNT(*) > 0 THEN (COUNT(CASE WHEN b.status = 'Pending' THEN 1 END)::decimal / COUNT(*)) * 100 ELSE 0 END as pending_percentage,
    CASE WHEN COUNT(*) > 0 THEN (COUNT(CASE WHEN b.status = 'Confirmed' THEN 1 END)::decimal / COUNT(*)) * 100 ELSE 0 END as confirmed_percentage,
    CASE WHEN COUNT(*) > 0 THEN (COUNT(CASE WHEN b.status IN ('Completed') THEN 1 END)::decimal / COUNT(*)) * 100 ELSE 0 END as completion_rate,
    CASE WHEN COUNT(*) > 0 THEN (COUNT(CASE WHEN b.status = 'Cancelled' THEN 1 END)::decimal / COUNT(*)) * 100 ELSE 0 END as cancellation_rate,
    CASE WHEN COUNT(*) > 0 THEN (COUNT(CASE WHEN b.status = 'Refunded' THEN 1 END)::decimal / COUNT(*)) * 100 ELSE 0 END as refund_rate,
    
    -- Timing Metrics (in minutes/hours)
    AVG(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') AND b.confirmed_at IS NOT NULL
             THEN EXTRACT(EPOCH FROM (b.confirmed_at - b.created_at)) / 60 END) as avg_booking_to_confirmation_minutes,
    AVG(CASE WHEN b.status IN ('CheckedIn', 'Completed') AND b.checked_in_at IS NOT NULL AND b.confirmed_at IS NOT NULL
             THEN EXTRACT(EPOCH FROM (b.checked_in_at - b.confirmed_at)) / 3600 END) as avg_confirmation_to_checkin_hours,
    AVG(CASE WHEN b.status = 'Completed' AND b.completed_at IS NOT NULL
             THEN EXTRACT(EPOCH FROM (b.completed_at - b.created_at)) / 3600 END) as avg_booking_to_completion_hours,
    
    -- Revenue Impact
    SUM(CASE WHEN b.status = 'Pending' THEN b.total_amount ELSE 0 END) as pending_revenue,
    SUM(CASE WHEN b.status = 'Confirmed' THEN b.total_amount ELSE 0 END) as confirmed_revenue,
    SUM(CASE WHEN b.status = 'Cancelled' THEN b.total_amount ELSE 0 END) as lost_revenue_to_cancellations,
    SUM(CASE WHEN b.status = 'Refunded' THEN b.total_amount ELSE 0 END) as refunded_revenue,
    
    -- Metadata
    NOW() as last_refreshed

FROM bookings b
LEFT JOIN flights f ON b.flight_id = f.id
LEFT JOIN routes r ON f.route_id = r.id
LEFT JOIN fare_classes fc ON b.fare_class_id = fc.id
WHERE b.created_at >= CURRENT_DATE - INTERVAL '1 year'
GROUP BY 
    CUBE(
        DATE(b.created_at),
        r.route_code,
        fc.class_code
    )
HAVING DATE(b.created_at) IS NOT NULL;

-- Create indexes
CREATE UNIQUE INDEX idx_mv_booking_status_daily_pk ON mv_booking_status_daily (date, COALESCE(route_code, ''), COALESCE(fare_class, ''));
CREATE INDEX idx_mv_booking_status_daily_date ON mv_booking_status_daily (date);

-- =====================================================
-- 3. DAILY PASSENGER DEMOGRAPHICS MATERIALIZED VIEW
-- =====================================================
CREATE MATERIALIZED VIEW mv_passenger_demographics_daily AS
SELECT 
    -- Time Dimensions
    DATE(b.created_at) as date,
    'Daily' as period,
    COALESCE(r.route_code, 'ALL') as route_code,
    COALESCE(fc.class_code, 'ALL') as fare_class,
    
    -- Age Demographics (calculated from date_of_birth if available)
    COUNT(CASE WHEN p.date_of_birth IS NOT NULL AND 
                    EXTRACT(YEAR FROM AGE(p.date_of_birth)) BETWEEN 0 AND 17 THEN 1 END) as passengers_age_0_to_17,
    COUNT(CASE WHEN p.date_of_birth IS NOT NULL AND 
                    EXTRACT(YEAR FROM AGE(p.date_of_birth)) BETWEEN 18 AND 24 THEN 1 END) as passengers_age_18_to_24,
    COUNT(CASE WHEN p.date_of_birth IS NOT NULL AND 
                    EXTRACT(YEAR FROM AGE(p.date_of_birth)) BETWEEN 25 AND 34 THEN 1 END) as passengers_age_25_to_34,
    COUNT(CASE WHEN p.date_of_birth IS NOT NULL AND 
                    EXTRACT(YEAR FROM AGE(p.date_of_birth)) BETWEEN 35 AND 44 THEN 1 END) as passengers_age_35_to_44,
    COUNT(CASE WHEN p.date_of_birth IS NOT NULL AND 
                    EXTRACT(YEAR FROM AGE(p.date_of_birth)) BETWEEN 45 AND 54 THEN 1 END) as passengers_age_45_to_54,
    COUNT(CASE WHEN p.date_of_birth IS NOT NULL AND 
                    EXTRACT(YEAR FROM AGE(p.date_of_birth)) BETWEEN 55 AND 64 THEN 1 END) as passengers_age_55_to_64,
    COUNT(CASE WHEN p.date_of_birth IS NOT NULL AND 
                    EXTRACT(YEAR FROM AGE(p.date_of_birth)) >= 65 THEN 1 END) as passengers_age_65_plus,
    COUNT(CASE WHEN p.date_of_birth IS NULL THEN 1 END) as passengers_age_unknown,
    
    -- Gender Demographics
    COUNT(CASE WHEN LOWER(p.gender) = 'male' THEN 1 END) as male_passengers,
    COUNT(CASE WHEN LOWER(p.gender) = 'female' THEN 1 END) as female_passengers,
    COUNT(CASE WHEN LOWER(p.gender) NOT IN ('male', 'female') AND p.gender IS NOT NULL THEN 1 END) as other_gender_passengers,
    COUNT(CASE WHEN p.gender IS NULL THEN 1 END) as unknown_gender_passengers,
    
    -- Booking Patterns
    COUNT(CASE WHEN b.passenger_count = 1 THEN 1 END) as single_passenger_bookings,
    COUNT(CASE WHEN b.passenger_count >= 2 AND 
                    EXISTS(SELECT 1 FROM passengers p2 WHERE p2.booking_id = b.id AND 
                           p2.date_of_birth IS NOT NULL AND EXTRACT(YEAR FROM AGE(p2.date_of_birth)) < 18)
               THEN 1 END) as family_bookings,
    COUNT(CASE WHEN b.passenger_count >= 3 AND 
                    NOT EXISTS(SELECT 1 FROM passengers p2 WHERE p2.booking_id = b.id AND 
                               p2.date_of_birth IS NOT NULL AND EXTRACT(YEAR FROM AGE(p2.date_of_birth)) < 18)
               THEN 1 END) as group_bookings,
    COUNT(CASE WHEN fc.class_code IN ('Business', 'First') THEN 1 END) as business_bookings,
    
    -- Revenue by Demographics
    SUM(CASE WHEN p.date_of_birth IS NOT NULL AND 
                  EXTRACT(YEAR FROM AGE(p.date_of_birth)) BETWEEN 18 AND 34 AND
                  b.status IN ('Confirmed', 'CheckedIn', 'Completed')
             THEN b.total_amount / b.passenger_count ELSE 0 END) as revenue_from_age_18_to_34,
    SUM(CASE WHEN p.date_of_birth IS NOT NULL AND 
                  EXTRACT(YEAR FROM AGE(p.date_of_birth)) BETWEEN 35 AND 54 AND
                  b.status IN ('Confirmed', 'CheckedIn', 'Completed')
             THEN b.total_amount / b.passenger_count ELSE 0 END) as revenue_from_age_35_to_54,
    SUM(CASE WHEN p.date_of_birth IS NOT NULL AND 
                  EXTRACT(YEAR FROM AGE(p.date_of_birth)) >= 55 AND
                  b.status IN ('Confirmed', 'CheckedIn', 'Completed')
             THEN b.total_amount / b.passenger_count ELSE 0 END) as revenue_from_age_55_plus,
    SUM(CASE WHEN fc.class_code IN ('Business', 'First') AND
                  b.status IN ('Confirmed', 'CheckedIn', 'Completed')
             THEN b.total_amount ELSE 0 END) as revenue_from_business_class,
    SUM(CASE WHEN b.passenger_count >= 2 AND 
                  EXISTS(SELECT 1 FROM passengers p2 WHERE p2.booking_id = b.id AND 
                         p2.date_of_birth IS NOT NULL AND EXTRACT(YEAR FROM AGE(p2.date_of_birth)) < 18) AND
                  b.status IN ('Confirmed', 'CheckedIn', 'Completed')
             THEN b.total_amount ELSE 0 END) as revenue_from_family_bookings,
    
    -- Computed Metrics
    AVG(CASE WHEN p.date_of_birth IS NOT NULL 
             THEN EXTRACT(YEAR FROM AGE(p.date_of_birth)) END) as average_age,
    AVG(b.passenger_count::decimal) as average_group_size,
    CASE WHEN COUNT(*) > 0 
         THEN (COUNT(CASE WHEN fc.class_code IN ('Business', 'First') THEN 1 END)::decimal / COUNT(*)) * 100 
         ELSE 0 END as business_class_penetration,
    CASE WHEN COUNT(*) > 0 
         THEN (COUNT(CASE WHEN b.passenger_count >= 2 AND 
                               EXISTS(SELECT 1 FROM passengers p2 WHERE p2.booking_id = b.id AND 
                                      p2.date_of_birth IS NOT NULL AND EXTRACT(YEAR FROM AGE(p2.date_of_birth)) < 18)
                          THEN 1 END)::decimal / COUNT(*)) * 100 
         ELSE 0 END as family_booking_rate,
    
    -- Metadata
    NOW() as last_refreshed

FROM bookings b
LEFT JOIN flights f ON b.flight_id = f.id
LEFT JOIN routes r ON f.route_id = r.id
LEFT JOIN fare_classes fc ON b.fare_class_id = fc.id
LEFT JOIN passengers p ON b.id = p.booking_id
WHERE b.created_at >= CURRENT_DATE - INTERVAL '1 year'
  AND b.status IN ('Confirmed', 'CheckedIn', 'Completed') -- Only confirmed bookings for demographics
GROUP BY 
    CUBE(
        DATE(b.created_at),
        r.route_code,
        fc.class_code
    )
HAVING DATE(b.created_at) IS NOT NULL;

-- Create indexes
CREATE UNIQUE INDEX idx_mv_passenger_demographics_daily_pk ON mv_passenger_demographics_daily (date, COALESCE(route_code, ''), COALESCE(fare_class, ''));
CREATE INDEX idx_mv_passenger_demographics_daily_date ON mv_passenger_demographics_daily (date);

-- =====================================================
-- 4. DAILY ROUTE PERFORMANCE MATERIALIZED VIEW
-- =====================================================
CREATE MATERIALIZED VIEW mv_route_performance_daily AS
SELECT 
    -- Time Dimensions
    DATE(f.departure_date) as date,
    'Daily' as period,
    r.route_code,
    da.iata_code as departure_airport,
    aa.iata_code as arrival_airport,
    r.distance_km,
    
    -- Performance Metrics
    SUM(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') 
             THEN b.total_amount ELSE 0 END) as total_revenue,
    COUNT(DISTINCT f.id) as total_flights,
    COUNT(DISTINCT b.id) as total_bookings,
    SUM(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') 
             THEN b.passenger_count ELSE 0 END) as total_passengers,
    
    -- Load Factor (assuming 150 seats per flight average)
    CASE WHEN COUNT(DISTINCT f.id) > 0 
         THEN (SUM(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') 
                        THEN b.passenger_count ELSE 0 END)::decimal / (COUNT(DISTINCT f.id) * 150)) * 100
         ELSE 0 END as load_factor,
    
    -- Average Ticket Price
    CASE WHEN SUM(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') 
                       THEN b.passenger_count ELSE 0 END) > 0
         THEN SUM(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') 
                       THEN b.total_amount ELSE 0 END) / 
              SUM(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') 
                       THEN b.passenger_count ELSE 0 END)
         ELSE 0 END as average_ticket_price,
    
    -- Revenue per KM
    CASE WHEN r.distance_km > 0 
         THEN SUM(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') 
                       THEN b.total_amount ELSE 0 END) / r.distance_km
         ELSE 0 END as revenue_per_km,
    
    -- Operational Metrics (simplified - would need flight status data)
    COUNT(DISTINCT f.id) as on_time_flights, -- Placeholder
    0 as delayed_flights, -- Placeholder
    0 as cancelled_flights, -- Placeholder
    100.0 as on_time_performance, -- Placeholder
    0.0 as average_delay_minutes, -- Placeholder
    
    -- Demand Score (based on booking velocity)
    CASE WHEN COUNT(DISTINCT f.id) > 0 
         THEN COUNT(DISTINCT b.id)::decimal / COUNT(DISTINCT f.id)
         ELSE 0 END as demand_score,
    
    -- Metadata
    NOW() as last_refreshed

FROM routes r
LEFT JOIN airports da ON r.departure_airport_id = da.id
LEFT JOIN airports aa ON r.arrival_airport_id = aa.id
LEFT JOIN flights f ON r.id = f.route_id
LEFT JOIN bookings b ON f.id = b.flight_id
WHERE f.departure_date >= CURRENT_DATE - INTERVAL '1 year'
GROUP BY 
    DATE(f.departure_date),
    r.route_code,
    da.iata_code,
    aa.iata_code,
    r.distance_km
HAVING DATE(f.departure_date) IS NOT NULL
   AND COUNT(DISTINCT f.id) > 0; -- Only include routes with flights

-- Create indexes
CREATE UNIQUE INDEX idx_mv_route_performance_daily_pk ON mv_route_performance_daily (date, route_code);
CREATE INDEX idx_mv_route_performance_daily_date ON mv_route_performance_daily (date);
CREATE INDEX idx_mv_route_performance_daily_route ON mv_route_performance_daily (route_code);

-- =====================================================
-- REFRESH FUNCTIONS
-- =====================================================

-- Function to refresh all materialized views
CREATE OR REPLACE FUNCTION refresh_analytics_materialized_views()
RETURNS void AS $$
BEGIN
    REFRESH MATERIALIZED VIEW CONCURRENTLY mv_revenue_daily;
    REFRESH MATERIALIZED VIEW CONCURRENTLY mv_booking_status_daily;
    REFRESH MATERIALIZED VIEW CONCURRENTLY mv_passenger_demographics_daily;
    REFRESH MATERIALIZED VIEW CONCURRENTLY mv_route_performance_daily;
    
    -- Log the refresh
    INSERT INTO analytics_refresh_log (view_name, refreshed_at, status)
    VALUES 
        ('mv_revenue_daily', NOW(), 'SUCCESS'),
        ('mv_booking_status_daily', NOW(), 'SUCCESS'),
        ('mv_passenger_demographics_daily', NOW(), 'SUCCESS'),
        ('mv_route_performance_daily', NOW(), 'SUCCESS');
END;
$$ LANGUAGE plpgsql;

-- Function to refresh a specific materialized view
CREATE OR REPLACE FUNCTION refresh_analytics_view(view_name text)
RETURNS void AS $$
BEGIN
    CASE view_name
        WHEN 'revenue' THEN
            REFRESH MATERIALIZED VIEW CONCURRENTLY mv_revenue_daily;
        WHEN 'booking_status' THEN
            REFRESH MATERIALIZED VIEW CONCURRENTLY mv_booking_status_daily;
        WHEN 'demographics' THEN
            REFRESH MATERIALIZED VIEW CONCURRENTLY mv_passenger_demographics_daily;
        WHEN 'route_performance' THEN
            REFRESH MATERIALIZED VIEW CONCURRENTLY mv_route_performance_daily;
        ELSE
            RAISE EXCEPTION 'Unknown view name: %', view_name;
    END CASE;
    
    -- Log the refresh
    INSERT INTO analytics_refresh_log (view_name, refreshed_at, status)
    VALUES (view_name, NOW(), 'SUCCESS');
END;
$$ LANGUAGE plpgsql;

-- Create analytics refresh log table
CREATE TABLE IF NOT EXISTS analytics_refresh_log (
    id SERIAL PRIMARY KEY,
    view_name VARCHAR(100) NOT NULL,
    refreshed_at TIMESTAMP NOT NULL DEFAULT NOW(),
    status VARCHAR(20) NOT NULL DEFAULT 'SUCCESS',
    error_message TEXT,
    duration_ms INTEGER,
    rows_affected INTEGER
);

-- Create index on refresh log
CREATE INDEX idx_analytics_refresh_log_view_date ON analytics_refresh_log (view_name, refreshed_at DESC);
