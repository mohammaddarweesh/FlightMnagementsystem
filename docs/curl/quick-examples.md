# Flight Booking Analytics API - Quick cURL Examples

This document provides quick copy-paste cURL examples for testing the Analytics API endpoints.

## Prerequisites

1. Set your environment variables:
```bash
export API_BASE_URL="https://localhost:5001"
export ACCESS_TOKEN="your_jwt_token_here"
export START_DATE="2024-01-01"
export END_DATE="2024-01-31"
```

2. Or replace the variables in the commands below with actual values.

## Authentication

### Login to get access token
```bash
curl -X POST \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@flightbooking.com","password":"Admin123!"}' \
  "$API_BASE_URL/api/auth/login"
```

## Analytics Dashboard

### Get Analytics Summary
```bash
curl -X GET \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/summary?startDate=$START_DATE&endDate=$END_DATE"
```

### Get Dashboard Data
```bash
curl -X GET \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/dashboard?startDate=$START_DATE&endDate=$END_DATE"
```

## Revenue Analytics

### Get Revenue Analytics (All Data)
```bash
curl -X GET \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/revenue?startDate=$START_DATE&endDate=$END_DATE"
```

### Get Revenue Analytics with Filters
```bash
curl -X GET \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/revenue?startDate=$START_DATE&endDate=$END_DATE&routeCodes=NYC-LAX,NYC-CHI&fareClasses=Economy,Business&airlineCodes=AA,UA"
```

### Get Revenue Trends (Daily)
```bash
curl -X GET \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/trends/revenue?startDate=$START_DATE&endDate=$END_DATE&period=Daily"
```

### Get Revenue Trends (Weekly)
```bash
curl -X GET \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/trends/revenue?startDate=$START_DATE&endDate=$END_DATE&period=Weekly"
```

### Get Total Revenue
```bash
curl -X GET \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/revenue/total?startDate=$START_DATE&endDate=$END_DATE"
```

## Booking Analytics

### Get Booking Status Analytics
```bash
curl -X GET \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/booking-status?startDate=$START_DATE&endDate=$END_DATE"
```

### Get Booking Status with Route Filter
```bash
curl -X GET \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/booking-status?startDate=$START_DATE&endDate=$END_DATE&routeCodes=NYC-LAX,NYC-CHI"
```

### Get Booking Status Summary
```bash
curl -X GET \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/booking-status/summary?startDate=$START_DATE&endDate=$END_DATE"
```

## Demographics Analytics

### Get Passenger Demographics
```bash
curl -X GET \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/demographics?startDate=$START_DATE&endDate=$END_DATE"
```

### Get Demographics with Route Filter
```bash
curl -X GET \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/demographics?startDate=$START_DATE&endDate=$END_DATE&routeCodes=NYC-LAX,NYC-CHI"
```

### Get Demographics Summary
```bash
curl -X GET \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/demographics/summary?startDate=$START_DATE&endDate=$END_DATE"
```

## Route Performance Analytics

### Get Route Performance Analytics
```bash
curl -X GET \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/route-performance?startDate=$START_DATE&endDate=$END_DATE"
```

### Get Route Performance (Filtered)
```bash
curl -X GET \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/route-performance?startDate=$START_DATE&endDate=$END_DATE&routeCodes=NYC-LAX,NYC-CHI"
```

### Get Top Performing Routes
```bash
curl -X GET \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/route-performance/top?startDate=$START_DATE&endDate=$END_DATE&limit=10"
```

### Get Performance Summary
```bash
curl -X GET \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/route-performance/summary?startDate=$START_DATE&endDate=$END_DATE"
```

## Data Export

### Export Revenue Data to CSV
```bash
curl -X POST \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
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
  }' \
  "$API_BASE_URL/api/analytics/export/csv"
```

### Export Booking Status Data to CSV
```bash
curl -X POST \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
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
  }' \
  "$API_BASE_URL/api/analytics/export/csv"
```

## Data Refresh

### Refresh Revenue Analytics Data
```bash
curl -X POST \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/refresh?viewName=revenue"
```

### Get Refresh Status
```bash
curl -X GET \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/refresh/status?viewName=revenue"
```

### Refresh All Analytics Data
```bash
curl -X POST \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/refresh"
```

## Error Testing

### Test Invalid Date Range
```bash
curl -X GET \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/revenue?startDate=2024-12-31&endDate=2024-01-01"
```

### Test Missing Parameters
```bash
curl -X GET \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  "$API_BASE_URL/api/analytics/revenue"
```

### Test Unauthorized Access (without token)
```bash
curl -X GET \
  "$API_BASE_URL/api/analytics/revenue?startDate=$START_DATE&endDate=$END_DATE"
```

## Response Examples

### Successful Response (200 OK)
```json
{
  "data": [...],
  "success": true,
  "message": "Data retrieved successfully",
  "timestamp": "2024-01-01T12:00:00Z"
}
```

### Error Response (400 Bad Request)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "The request contains invalid data",
  "instance": "/api/analytics/revenue",
  "traceId": "0HN7SPBVKQAAA:00000001",
  "errors": {
    "startDate": ["Start date cannot be in the future"],
    "endDate": ["End date must be after start date"]
  }
}
```

### Unauthorized Response (401)
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Authentication is required to access this resource"
}
```
