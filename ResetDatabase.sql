-- Drop the database if it exists (run this as postgres superuser)
DROP DATABASE IF EXISTS "FlightBookingDb_MohammadDarweesh";

-- Create a fresh database
CREATE DATABASE "FlightBookingDb_MohammadDarweesh"
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'English_United States.1252'
    LC_CTYPE = 'English_United States.1252'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;
