-- Check if Hangfire database exists
SELECT datname FROM pg_database WHERE datname = 'flightbookinghangfire_mohammaddarweesh';

-- Connect to the Hangfire database
\c flightbookinghangfire_mohammaddarweesh;

-- Check all tables in the Hangfire database
SELECT table_name, table_schema 
FROM information_schema.tables 
WHERE table_schema = 'hangfire' OR table_schema = 'public'
ORDER BY table_schema, table_name;

-- Check for Hangfire-specific tables
SELECT table_name, table_schema
FROM information_schema.tables 
WHERE table_schema = 'hangfire'
ORDER BY table_name;

-- If no hangfire schema, check public schema for hangfire tables
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND (table_name LIKE '%job%' OR table_name LIKE '%queue%' OR table_name LIKE '%server%' OR table_name LIKE '%hangfire%')
ORDER BY table_name;
