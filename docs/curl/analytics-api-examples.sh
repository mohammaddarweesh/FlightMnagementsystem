#!/bin/bash

# Flight Booking Analytics API - cURL Examples
# This script provides comprehensive examples for testing all analytics endpoints
# 
# Prerequisites:
# 1. Set environment variables or update the values below
# 2. Ensure the API is running and accessible
# 3. Have valid authentication credentials

# =============================================================================
# Configuration
# =============================================================================

# API Configuration
BASE_URL="${API_BASE_URL:-https://localhost:5001}"
API_VERSION="v1"

# Authentication (update these with your credentials)
ADMIN_EMAIL="${ADMIN_EMAIL:-admin@flightbooking.com}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-Admin123!}"

# Date range for analytics queries
START_DATE="${START_DATE:-2024-01-01}"
END_DATE="${END_DATE:-2024-01-31}"

# Sample filter values
ROUTE_CODES="NYC-LAX,NYC-CHI"
FARE_CLASSES="Economy,Business"
AIRLINE_CODES="AA,UA"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# =============================================================================
# Helper Functions
# =============================================================================

print_header() {
    echo -e "\n${BLUE}=== $1 ===${NC}"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_info() {
    echo -e "${YELLOW}ℹ $1${NC}"
}

# Function to make authenticated requests
make_request() {
    local method="$1"
    local endpoint="$2"
    local data="$3"
    local description="$4"
    
    print_info "Testing: $description"
    echo "curl -X $method \"$BASE_URL$endpoint\""
    
    if [ -n "$data" ]; then
        echo "Data: $data"
        curl -X "$method" \
             -H "Authorization: Bearer $ACCESS_TOKEN" \
             -H "Content-Type: application/json" \
             -d "$data" \
             "$BASE_URL$endpoint" \
             -w "\nHTTP Status: %{http_code}\n" \
             -s
    else
        curl -X "$method" \
             -H "Authorization: Bearer $ACCESS_TOKEN" \
             "$BASE_URL$endpoint" \
             -w "\nHTTP Status: %{http_code}\n" \
             -s
    fi
    
    echo -e "\n"
}

# =============================================================================
# Authentication
# =============================================================================

authenticate() {
    print_header "Authentication"
    
    print_info "Authenticating with email: $ADMIN_EMAIL"
    
    local auth_response=$(curl -X POST \
        -H "Content-Type: application/json" \
        -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASSWORD\"}" \
        "$BASE_URL/api/auth/login" \
        -s)
    
    # Extract access token (assuming JSON response with accessToken field)
    ACCESS_TOKEN=$(echo "$auth_response" | grep -o '"accessToken":"[^"]*"' | cut -d'"' -f4)
    
    if [ -n "$ACCESS_TOKEN" ]; then
        print_success "Authentication successful"
        export ACCESS_TOKEN
    else
        print_error "Authentication failed"
        echo "Response: $auth_response"
        exit 1
    fi
}

# =============================================================================
# Analytics Dashboard Endpoints
# =============================================================================

test_dashboard_endpoints() {
    print_header "Analytics Dashboard"
    
    # Analytics Summary
    make_request "GET" \
        "/api/analytics/summary?startDate=$START_DATE&endDate=$END_DATE" \
        "" \
        "Get Analytics Summary"
    
    # Dashboard Data
    make_request "GET" \
        "/api/analytics/dashboard?startDate=$START_DATE&endDate=$END_DATE" \
        "" \
        "Get Dashboard Data"
}

# =============================================================================
# Revenue Analytics Endpoints
# =============================================================================

test_revenue_endpoints() {
    print_header "Revenue Analytics"
    
    # Revenue Analytics with Filters
    make_request "GET" \
        "/api/analytics/revenue?startDate=$START_DATE&endDate=$END_DATE&routeCodes=$ROUTE_CODES&fareClasses=$FARE_CLASSES&airlineCodes=$AIRLINE_CODES" \
        "" \
        "Get Revenue Analytics with Filters"
    
    # Revenue Analytics without Filters
    make_request "GET" \
        "/api/analytics/revenue?startDate=$START_DATE&endDate=$END_DATE" \
        "" \
        "Get Revenue Analytics (All Data)"
    
    # Revenue Trends
    make_request "GET" \
        "/api/analytics/trends/revenue?startDate=$START_DATE&endDate=$END_DATE&period=Daily" \
        "" \
        "Get Daily Revenue Trends"
    
    # Weekly Revenue Trends
    make_request "GET" \
        "/api/analytics/trends/revenue?startDate=$START_DATE&endDate=$END_DATE&period=Weekly" \
        "" \
        "Get Weekly Revenue Trends"
    
    # Total Revenue
    make_request "GET" \
        "/api/analytics/revenue/total?startDate=$START_DATE&endDate=$END_DATE" \
        "" \
        "Get Total Revenue"
}

# =============================================================================
# Booking Analytics Endpoints
# =============================================================================

test_booking_endpoints() {
    print_header "Booking Analytics"
    
    # Booking Status Analytics
    make_request "GET" \
        "/api/analytics/booking-status?startDate=$START_DATE&endDate=$END_DATE" \
        "" \
        "Get Booking Status Analytics"
    
    # Booking Status with Route Filter
    make_request "GET" \
        "/api/analytics/booking-status?startDate=$START_DATE&endDate=$END_DATE&routeCodes=$ROUTE_CODES" \
        "" \
        "Get Booking Status Analytics (Filtered by Routes)"
    
    # Booking Status Summary
    make_request "GET" \
        "/api/analytics/booking-status/summary?startDate=$START_DATE&endDate=$END_DATE" \
        "" \
        "Get Booking Status Summary"
}

# =============================================================================
# Demographics Analytics Endpoints
# =============================================================================

test_demographics_endpoints() {
    print_header "Demographics Analytics"
    
    # Passenger Demographics
    make_request "GET" \
        "/api/analytics/demographics?startDate=$START_DATE&endDate=$END_DATE" \
        "" \
        "Get Passenger Demographics"
    
    # Demographics with Route Filter
    make_request "GET" \
        "/api/analytics/demographics?startDate=$START_DATE&endDate=$END_DATE&routeCodes=$ROUTE_CODES" \
        "" \
        "Get Demographics (Filtered by Routes)"
    
    # Demographics Summary
    make_request "GET" \
        "/api/analytics/demographics/summary?startDate=$START_DATE&endDate=$END_DATE" \
        "" \
        "Get Demographics Summary"
}

# =============================================================================
# Route Performance Endpoints
# =============================================================================

test_route_performance_endpoints() {
    print_header "Route Performance Analytics"
    
    # Route Performance Analytics
    make_request "GET" \
        "/api/analytics/route-performance?startDate=$START_DATE&endDate=$END_DATE" \
        "" \
        "Get Route Performance Analytics"
    
    # Route Performance with Filter
    make_request "GET" \
        "/api/analytics/route-performance?startDate=$START_DATE&endDate=$END_DATE&routeCodes=$ROUTE_CODES" \
        "" \
        "Get Route Performance (Filtered)"
    
    # Top Performing Routes
    make_request "GET" \
        "/api/analytics/route-performance/top?startDate=$START_DATE&endDate=$END_DATE&limit=10" \
        "" \
        "Get Top 10 Performing Routes"
    
    # Performance Summary
    make_request "GET" \
        "/api/analytics/route-performance/summary?startDate=$START_DATE&endDate=$END_DATE" \
        "" \
        "Get Performance Summary"
}

# =============================================================================
# Data Export Endpoints
# =============================================================================

test_export_endpoints() {
    print_header "Data Export"
    
    # Export Revenue Data
    local revenue_export_data='{
        "dataType": "Revenue",
        "dateRange": {
            "startDate": "'$START_DATE'",
            "endDate": "'$END_DATE'"
        },
        "filters": {
            "dateRange": {
                "startDate": "'$START_DATE'",
                "endDate": "'$END_DATE'"
            },
            "routeCodes": ["NYC-LAX", "NYC-CHI"],
            "fareClasses": ["Economy", "Business"]
        },
        "configuration": {
            "format": "CSV",
            "includeHeaders": true,
            "includeMetadata": true,
            "fileName": "revenue_analytics_export.csv",
            "maxRows": 10000
        }
    }'
    
    make_request "POST" \
        "/api/analytics/export/csv" \
        "$revenue_export_data" \
        "Export Revenue Data to CSV"
    
    # Export Booking Status Data
    local booking_export_data='{
        "dataType": "BookingStatus",
        "dateRange": {
            "startDate": "'$START_DATE'",
            "endDate": "'$END_DATE'"
        },
        "configuration": {
            "format": "CSV",
            "includeHeaders": true,
            "fileName": "booking_status_export.csv"
        }
    }'
    
    make_request "POST" \
        "/api/analytics/export/csv" \
        "$booking_export_data" \
        "Export Booking Status Data to CSV"
}

# =============================================================================
# Data Refresh Endpoints
# =============================================================================

test_refresh_endpoints() {
    print_header "Data Refresh"
    
    # Refresh Revenue Analytics
    make_request "POST" \
        "/api/analytics/refresh?viewName=revenue" \
        "" \
        "Refresh Revenue Analytics Data"
    
    # Check Refresh Status
    make_request "GET" \
        "/api/analytics/refresh/status?viewName=revenue" \
        "" \
        "Get Revenue Analytics Refresh Status"
    
    # Refresh All Analytics
    make_request "POST" \
        "/api/analytics/refresh" \
        "" \
        "Refresh All Analytics Data"
}

# =============================================================================
# Error Testing
# =============================================================================

test_error_scenarios() {
    print_header "Error Scenarios"
    
    # Invalid date range
    make_request "GET" \
        "/api/analytics/revenue?startDate=2024-12-31&endDate=2024-01-01" \
        "" \
        "Invalid Date Range (End before Start)"
    
    # Missing required parameters
    make_request "GET" \
        "/api/analytics/revenue" \
        "" \
        "Missing Required Parameters"
    
    # Invalid route code
    make_request "GET" \
        "/api/analytics/revenue?startDate=$START_DATE&endDate=$END_DATE&routeCodes=INVALID-ROUTE" \
        "" \
        "Invalid Route Code"
}

# =============================================================================
# Main Execution
# =============================================================================

main() {
    print_header "Flight Booking Analytics API Testing"
    print_info "Base URL: $BASE_URL"
    print_info "Date Range: $START_DATE to $END_DATE"
    
    # Authenticate first
    authenticate
    
    # Run all test suites
    test_dashboard_endpoints
    test_revenue_endpoints
    test_booking_endpoints
    test_demographics_endpoints
    test_route_performance_endpoints
    test_export_endpoints
    test_refresh_endpoints
    test_error_scenarios
    
    print_header "Testing Complete"
    print_success "All API endpoints have been tested"
}

# Run the main function if script is executed directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi
