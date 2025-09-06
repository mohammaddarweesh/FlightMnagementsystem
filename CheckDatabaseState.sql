-- Check if database exists
SELECT datname FROM pg_database WHERE datname = 'FlightBookingDb_MohammadDarweesh';

-- If connected to the database, check tables
\c FlightBookingDb_MohammadDarweesh;

-- Check if Flights table exists and its structure
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'Flights' 
ORDER BY ordinal_position;

-- Check migration history
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
