-- Connect to the database
\c FlightBookingDb_MohammadDarweesh;

-- Check all tables in the database
SELECT table_name, table_schema 
FROM information_schema.tables 
WHERE table_schema = 'public' 
ORDER BY table_name;

-- Check specifically for Hangfire tables (they usually start with 'hangfire')
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_name LIKE '%hangfire%'
ORDER BY table_name;

-- Check for any job-related tables
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND (table_name LIKE '%job%' OR table_name LIKE '%queue%' OR table_name LIKE '%server%')
ORDER BY table_name;
