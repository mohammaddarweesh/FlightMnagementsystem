-- =====================================================
-- ANALYTICS TABLES AND MATERIALIZED VIEWS MIGRATION
-- =====================================================

-- Create analytics refresh log table first (required by materialized view functions)
CREATE TABLE IF NOT EXISTS analytics_refresh_log (
    id SERIAL PRIMARY KEY,
    view_name VARCHAR(100) NOT NULL,
    refreshed_at TIMESTAMP NOT NULL DEFAULT NOW(),
    status VARCHAR(20) NOT NULL DEFAULT 'SUCCESS',
    error_message TEXT,
    duration_ms INTEGER,
    rows_affected INTEGER,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Create index on refresh log
CREATE INDEX IF NOT EXISTS idx_analytics_refresh_log_view_date 
ON analytics_refresh_log (view_name, refreshed_at DESC);

-- =====================================================
-- MATERIALIZED VIEWS
-- =====================================================

-- 1. DAILY REVENUE ANALYTICS MATERIALIZED VIEW
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_revenue_daily AS
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
    0 as revenue_per1000_asm, -- Placeholder
    
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
    
    -- Growth Rates (placeholders - would need historical comparison)
    0 as revenue_growth_rate,
    0 as booking_growth_rate,
    
    -- Metadata
    NOW() as last_refreshed,
    'MaterializedView' as data_source,
    COUNT(*) as record_count,
    
    -- Additional required fields for Entity Framework
    gen_random_uuid() as id,
    NOW() as created_at,
    NOW() as updated_at,
    false as is_archived

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

-- 2. DAILY BOOKING STATUS ANALYTICS MATERIALIZED VIEW
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_booking_status_daily AS
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
    
    -- Timing Metrics (simplified)
    30.0 as average_booking_to_confirmation_minutes, -- Placeholder
    2.0 as average_confirmation_to_check_in_hours, -- Placeholder
    24.0 as average_booking_to_completion_hours, -- Placeholder
    
    -- Revenue Impact
    SUM(CASE WHEN b.status = 'Pending' THEN b.total_amount ELSE 0 END) as pending_revenue,
    SUM(CASE WHEN b.status = 'Confirmed' THEN b.total_amount ELSE 0 END) as confirmed_revenue,
    SUM(CASE WHEN b.status = 'Cancelled' THEN b.total_amount ELSE 0 END) as lost_revenue_to_cancellations,
    SUM(CASE WHEN b.status = 'Refunded' THEN b.total_amount ELSE 0 END) as refunded_revenue,
    
    -- Metadata
    NOW() as last_refreshed,
    
    -- Additional required fields for Entity Framework
    gen_random_uuid() as id,
    NOW() as created_at,
    NOW() as updated_at,
    false as is_archived

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

-- 3. DAILY PASSENGER DEMOGRAPHICS MATERIALIZED VIEW
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_passenger_demographics_daily AS
SELECT 
    -- Time Dimensions
    DATE(b.created_at) as date,
    'Daily' as period,
    COALESCE(r.route_code, 'ALL') as route_code,
    COALESCE(fc.class_code, 'ALL') as fare_class,
    
    -- Age Demographics (simplified - would need passenger table with date_of_birth)
    COUNT(*) / 8 as passengers_age_0_to_17, -- Placeholder distribution
    COUNT(*) / 6 as passengers_age_18_to_24,
    COUNT(*) / 4 as passengers_age_25_to_34,
    COUNT(*) / 4 as passengers_age_35_to_44,
    COUNT(*) / 5 as passengers_age_45_to_54,
    COUNT(*) / 6 as passengers_age_55_to_64,
    COUNT(*) / 8 as passengers_age_65_plus,
    COUNT(*) / 10 as passengers_age_unknown,
    
    -- Gender Demographics (simplified)
    COUNT(*) / 2 as male_passengers, -- Placeholder 50/50 split
    COUNT(*) / 2 as female_passengers,
    0 as other_gender_passengers,
    0 as unknown_gender_passengers,
    
    -- Booking Patterns
    COUNT(CASE WHEN b.passenger_count = 1 THEN 1 END) as single_passenger_bookings,
    COUNT(CASE WHEN b.passenger_count >= 2 THEN 1 END) / 3 as family_bookings, -- Estimate
    COUNT(CASE WHEN b.passenger_count >= 3 THEN 1 END) / 2 as group_bookings, -- Estimate
    COUNT(CASE WHEN fc.class_code IN ('Business', 'First') THEN 1 END) as business_bookings,
    
    -- Revenue by Demographics (simplified)
    SUM(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') THEN b.total_amount ELSE 0 END) / 3 as revenue_from_age_18_to_34,
    SUM(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') THEN b.total_amount ELSE 0 END) / 2 as revenue_from_age_35_to_54,
    SUM(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') THEN b.total_amount ELSE 0 END) / 4 as revenue_from_age_55_plus,
    SUM(CASE WHEN fc.class_code IN ('Business', 'First') AND b.status IN ('Confirmed', 'CheckedIn', 'Completed') THEN b.total_amount ELSE 0 END) as revenue_from_business_class,
    SUM(CASE WHEN b.passenger_count >= 2 AND b.status IN ('Confirmed', 'CheckedIn', 'Completed') THEN b.total_amount ELSE 0 END) / 2 as revenue_from_family_bookings,
    
    -- Computed Metrics
    35.5 as average_age, -- Placeholder
    AVG(b.passenger_count::decimal) as average_group_size,
    CASE WHEN COUNT(*) > 0 THEN (COUNT(CASE WHEN fc.class_code IN ('Business', 'First') THEN 1 END)::decimal / COUNT(*)) * 100 ELSE 0 END as business_class_penetration,
    CASE WHEN COUNT(*) > 0 THEN (COUNT(CASE WHEN b.passenger_count >= 2 THEN 1 END)::decimal / COUNT(*)) * 100 / 3 ELSE 0 END as family_booking_rate,
    
    -- Geographic Data (as JSONB)
    '{}'::jsonb as passengers_by_country,
    '{}'::jsonb as passengers_by_city,
    
    -- Metadata
    NOW() as last_refreshed,
    
    -- Additional required fields for Entity Framework
    gen_random_uuid() as id,
    NOW() as created_at,
    NOW() as updated_at,
    false as is_archived

FROM bookings b
LEFT JOIN flights f ON b.flight_id = f.id
LEFT JOIN routes r ON f.route_id = r.id
LEFT JOIN fare_classes fc ON b.fare_class_id = fc.id
WHERE b.created_at >= CURRENT_DATE - INTERVAL '1 year'
  AND b.status IN ('Confirmed', 'CheckedIn', 'Completed') -- Only confirmed bookings for demographics
GROUP BY 
    CUBE(
        DATE(b.created_at),
        r.route_code,
        fc.class_code
    )
HAVING DATE(b.created_at) IS NOT NULL;

-- 4. DAILY ROUTE PERFORMANCE MATERIALIZED VIEW
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_route_performance_daily AS
SELECT 
    -- Time Dimensions
    DATE(f.departure_date) as date,
    'Daily' as period,
    r.route_code,
    da.iata_code as departure_airport,
    aa.iata_code as arrival_airport,
    COALESCE(r.distance_km, 1000) as distance_km, -- Default distance if not set
    
    -- Performance Metrics
    SUM(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') THEN b.total_amount ELSE 0 END) as total_revenue,
    COUNT(DISTINCT f.id) as total_flights,
    COUNT(DISTINCT b.id) as total_bookings,
    SUM(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') THEN b.passenger_count ELSE 0 END) as total_passengers,
    
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
    CASE WHEN COALESCE(r.distance_km, 1000) > 0 
         THEN SUM(CASE WHEN b.status IN ('Confirmed', 'CheckedIn', 'Completed') 
                       THEN b.total_amount ELSE 0 END) / COALESCE(r.distance_km, 1000)
         ELSE 0 END as revenue_per_km,
    
    -- Operational Metrics (placeholders)
    COUNT(DISTINCT f.id) as on_time_flights,
    0 as delayed_flights,
    0 as cancelled_flights,
    95.0 as on_time_performance,
    12.5 as average_delay_minutes,
    
    -- Demand Score
    CASE WHEN COUNT(DISTINCT f.id) > 0 
         THEN COUNT(DISTINCT b.id)::decimal / COUNT(DISTINCT f.id)
         ELSE 0 END as demand_score,
    
    -- Seasonality and Competition (placeholders)
    1.0 as seasonality_index,
    0.5 as competitive_index,
    
    -- Profitability (placeholders)
    NULL::decimal as estimated_costs,
    NULL::decimal as estimated_profit,
    NULL::decimal as profit_margin,
    
    -- Metadata
    NOW() as last_refreshed,
    
    -- Additional required fields for Entity Framework
    gen_random_uuid() as id,
    NOW() as created_at,
    NOW() as updated_at,
    false as is_archived

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

-- =====================================================
-- CREATE INDEXES FOR PERFORMANCE
-- =====================================================

-- Revenue Analytics Indexes
CREATE UNIQUE INDEX IF NOT EXISTS idx_mv_revenue_daily_pk 
ON mv_revenue_daily (date, COALESCE(route_code, ''), COALESCE(fare_class, ''), COALESCE(airline_code, ''));

CREATE INDEX IF NOT EXISTS idx_mv_revenue_daily_date ON mv_revenue_daily (date);
CREATE INDEX IF NOT EXISTS idx_mv_revenue_daily_route ON mv_revenue_daily (route_code);
CREATE INDEX IF NOT EXISTS idx_mv_revenue_daily_class ON mv_revenue_daily (fare_class);

-- Booking Status Analytics Indexes
CREATE UNIQUE INDEX IF NOT EXISTS idx_mv_booking_status_daily_pk 
ON mv_booking_status_daily (date, COALESCE(route_code, ''), COALESCE(fare_class, ''));

CREATE INDEX IF NOT EXISTS idx_mv_booking_status_daily_date ON mv_booking_status_daily (date);

-- Passenger Demographics Indexes
CREATE UNIQUE INDEX IF NOT EXISTS idx_mv_passenger_demographics_daily_pk 
ON mv_passenger_demographics_daily (date, COALESCE(route_code, ''), COALESCE(fare_class, ''));

CREATE INDEX IF NOT EXISTS idx_mv_passenger_demographics_daily_date ON mv_passenger_demographics_daily (date);

-- Route Performance Indexes
CREATE UNIQUE INDEX IF NOT EXISTS idx_mv_route_performance_daily_pk 
ON mv_route_performance_daily (date, route_code);

CREATE INDEX IF NOT EXISTS idx_mv_route_performance_daily_date ON mv_route_performance_daily (date);
CREATE INDEX IF NOT EXISTS idx_mv_route_performance_daily_route ON mv_route_performance_daily (route_code);

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
