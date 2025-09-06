-- Analytics Materialized Views Creation Script
-- This script creates the materialized views for analytics data

-- Drop existing views if they exist
DROP MATERIALIZED VIEW IF EXISTS mv_revenue_daily CASCADE;
DROP MATERIALIZED VIEW IF EXISTS mv_booking_status_daily CASCADE;
DROP MATERIALIZED VIEW IF EXISTS mv_passenger_demographics_daily CASCADE;
DROP MATERIALIZED VIEW IF EXISTS mv_route_performance_daily CASCADE;

-- Create Revenue Analytics Materialized View
CREATE MATERIALIZED VIEW mv_revenue_daily AS
SELECT 
    DATE(b."CreatedAt") as "Date",
    COALESCE(r."RouteCode", 'ALL') as "RouteCode",
    COALESCE(f."FareClass", 'ALL') as "FareClass",
    COALESCE(f."AirlineCode", 'ALL') as "AirlineCode",
    COUNT(b."Id") as "TotalBookings",
    SUM(b."TotalAmount") as "TotalRevenue",
    AVG(b."TotalAmount") as "AverageTicketPrice",
    SUM(b."TaxAmount") as "TotalTax",
    SUM(b."ServiceFeeAmount") as "TotalServiceFees",
    NOW() as "LastRefreshed"
FROM "Bookings" b
LEFT JOIN "Flights" f ON b."FlightId" = f."Id"
LEFT JOIN "Routes" r ON f."RouteId" = r."Id"
WHERE b."Status" IN (2, 3, 4) -- Confirmed, CheckedIn, Completed
GROUP BY DATE(b."CreatedAt"), r."RouteCode", f."FareClass", f."AirlineCode"

UNION ALL

-- Aggregate totals (RouteCode = 'ALL', FareClass = 'ALL', AirlineCode = 'ALL')
SELECT 
    DATE(b."CreatedAt") as "Date",
    'ALL' as "RouteCode",
    'ALL' as "FareClass", 
    'ALL' as "AirlineCode",
    COUNT(b."Id") as "TotalBookings",
    SUM(b."TotalAmount") as "TotalRevenue",
    AVG(b."TotalAmount") as "AverageTicketPrice",
    SUM(b."TaxAmount") as "TotalTax",
    SUM(b."ServiceFeeAmount") as "TotalServiceFees",
    NOW() as "LastRefreshed"
FROM "Bookings" b
LEFT JOIN "Flights" f ON b."FlightId" = f."Id"
WHERE b."Status" IN (2, 3, 4) -- Confirmed, CheckedIn, Completed
GROUP BY DATE(b."CreatedAt");

-- Create Booking Status Analytics Materialized View
CREATE MATERIALIZED VIEW mv_booking_status_daily AS
SELECT 
    DATE(b."CreatedAt") as "Date",
    COALESCE(r."RouteCode", 'ALL') as "RouteCode",
    COALESCE(f."FareClass", 'ALL') as "FareClass",
    COUNT(CASE WHEN b."Status" = 0 THEN 1 END) as "PendingBookings",
    COUNT(CASE WHEN b."Status" = 1 THEN 1 END) as "ConfirmedBookings",
    COUNT(CASE WHEN b."Status" = 2 THEN 1 END) as "CheckedInBookings",
    COUNT(CASE WHEN b."Status" = 3 THEN 1 END) as "CompletedBookings",
    COUNT(CASE WHEN b."Status" = 4 THEN 1 END) as "CancelledBookings",
    COUNT(CASE WHEN b."Status" = 5 THEN 1 END) as "ExpiredBookings",
    COUNT(CASE WHEN b."Status" = 6 THEN 1 END) as "RefundedBookings",
    COUNT(b."Id") as "TotalBookings",
    NOW() as "LastRefreshed"
FROM "Bookings" b
LEFT JOIN "Flights" f ON b."FlightId" = f."Id"
LEFT JOIN "Routes" r ON f."RouteId" = r."Id"
GROUP BY DATE(b."CreatedAt"), r."RouteCode", f."FareClass"

UNION ALL

-- Aggregate totals (RouteCode = 'ALL', FareClass = 'ALL')
SELECT 
    DATE(b."CreatedAt") as "Date",
    'ALL' as "RouteCode",
    'ALL' as "FareClass",
    COUNT(CASE WHEN b."Status" = 0 THEN 1 END) as "PendingBookings",
    COUNT(CASE WHEN b."Status" = 1 THEN 1 END) as "ConfirmedBookings",
    COUNT(CASE WHEN b."Status" = 2 THEN 1 END) as "CheckedInBookings",
    COUNT(CASE WHEN b."Status" = 3 THEN 1 END) as "CompletedBookings",
    COUNT(CASE WHEN b."Status" = 4 THEN 1 END) as "CancelledBookings",
    COUNT(CASE WHEN b."Status" = 5 THEN 1 END) as "ExpiredBookings",
    COUNT(CASE WHEN b."Status" = 6 THEN 1 END) as "RefundedBookings",
    COUNT(b."Id") as "TotalBookings",
    NOW() as "LastRefreshed"
FROM "Bookings" b
LEFT JOIN "Flights" f ON b."FlightId" = f."Id"
GROUP BY DATE(b."CreatedAt");

-- Create Passenger Demographics Analytics Materialized View
CREATE MATERIALIZED VIEW mv_passenger_demographics_daily AS
SELECT 
    DATE(b."CreatedAt") as "Date",
    COALESCE(r."RouteCode", 'ALL') as "RouteCode",
    COALESCE(f."FareClass", 'ALL') as "FareClass",
    COUNT(p."Id") as "TotalPassengers",
    COUNT(CASE WHEN p."Gender" = 'Male' THEN 1 END) as "MalePassengers",
    COUNT(CASE WHEN p."Gender" = 'Female' THEN 1 END) as "FemalePassengers",
    COUNT(CASE WHEN p."Gender" = 'Other' THEN 1 END) as "OtherGenderPassengers",
    AVG(EXTRACT(YEAR FROM AGE(p."DateOfBirth"))) as "AverageAge",
    '{}' as "PassengersByCountry",
    '{}' as "PassengersByCity",
    NOW() as "LastRefreshed"
FROM "Bookings" b
LEFT JOIN "Flights" f ON b."FlightId" = f."Id"
LEFT JOIN "Routes" r ON f."RouteId" = r."Id"
LEFT JOIN "Passengers" p ON b."Id" = p."BookingId"
WHERE b."Status" IN (2, 3, 4) -- Confirmed, CheckedIn, Completed
GROUP BY DATE(b."CreatedAt"), r."RouteCode", f."FareClass"

UNION ALL

-- Aggregate totals (RouteCode = 'ALL', FareClass = 'ALL')
SELECT 
    DATE(b."CreatedAt") as "Date",
    'ALL' as "RouteCode",
    'ALL' as "FareClass",
    COUNT(p."Id") as "TotalPassengers",
    COUNT(CASE WHEN p."Gender" = 'Male' THEN 1 END) as "MalePassengers",
    COUNT(CASE WHEN p."Gender" = 'Female' THEN 1 END) as "FemalePassengers",
    COUNT(CASE WHEN p."Gender" = 'Other' THEN 1 END) as "OtherGenderPassengers",
    AVG(EXTRACT(YEAR FROM AGE(p."DateOfBirth"))) as "AverageAge",
    '{}' as "PassengersByCountry",
    '{}' as "PassengersByCity",
    NOW() as "LastRefreshed"
FROM "Bookings" b
LEFT JOIN "Flights" f ON b."FlightId" = f."Id"
LEFT JOIN "Passengers" p ON b."Id" = p."BookingId"
WHERE b."Status" IN (2, 3, 4) -- Confirmed, CheckedIn, Completed
GROUP BY DATE(b."CreatedAt");

-- Create Route Performance Analytics Materialized View
CREATE MATERIALIZED VIEW mv_route_performance_daily AS
SELECT 
    DATE(f."DepartureDate") as "Date",
    r."RouteCode",
    r."DepartureAirport",
    r."ArrivalAirport",
    COUNT(f."Id") as "TotalFlights",
    COUNT(b."Id") as "TotalBookings",
    COUNT(p."Id") as "TotalPassengers",
    SUM(b."TotalAmount") as "TotalRevenue",
    AVG(b."TotalAmount") as "AverageTicketPrice",
    CASE 
        WHEN f."TotalSeats" > 0 THEN (COUNT(p."Id")::decimal / f."TotalSeats" * 100)
        ELSE 0 
    END as "LoadFactor",
    CASE 
        WHEN COUNT(f."Id") > 0 THEN (COUNT(CASE WHEN f."Status" = 3 THEN 1 END)::decimal / COUNT(f."Id") * 100)
        ELSE 0 
    END as "OnTimePerformance",
    COUNT(b."Id")::decimal / NULLIF(COUNT(f."Id"), 0) as "DemandScore",
    NOW() as "LastRefreshed"
FROM "Routes" r
LEFT JOIN "Flights" f ON r."Id" = f."RouteId"
LEFT JOIN "Bookings" b ON f."Id" = b."FlightId" AND b."Status" IN (2, 3, 4)
LEFT JOIN "Passengers" p ON b."Id" = p."BookingId"
GROUP BY DATE(f."DepartureDate"), r."RouteCode", r."DepartureAirport", r."ArrivalAirport", f."TotalSeats";

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_mv_revenue_daily_date ON mv_revenue_daily ("Date");
CREATE INDEX IF NOT EXISTS idx_mv_revenue_daily_route ON mv_revenue_daily ("RouteCode");
CREATE INDEX IF NOT EXISTS idx_mv_revenue_daily_fare_class ON mv_revenue_daily ("FareClass");

CREATE INDEX IF NOT EXISTS idx_mv_booking_status_daily_date ON mv_booking_status_daily ("Date");
CREATE INDEX IF NOT EXISTS idx_mv_booking_status_daily_route ON mv_booking_status_daily ("RouteCode");

CREATE INDEX IF NOT EXISTS idx_mv_passenger_demographics_daily_date ON mv_passenger_demographics_daily ("Date");
CREATE INDEX IF NOT EXISTS idx_mv_passenger_demographics_daily_route ON mv_passenger_demographics_daily ("RouteCode");

CREATE INDEX IF NOT EXISTS idx_mv_route_performance_daily_date ON mv_route_performance_daily ("Date");
CREATE INDEX IF NOT EXISTS idx_mv_route_performance_daily_route ON mv_route_performance_daily ("RouteCode");

-- Refresh all materialized views
REFRESH MATERIALIZED VIEW mv_revenue_daily;
REFRESH MATERIALIZED VIEW mv_booking_status_daily;
REFRESH MATERIALIZED VIEW mv_passenger_demographics_daily;
REFRESH MATERIALIZED VIEW mv_route_performance_daily;
