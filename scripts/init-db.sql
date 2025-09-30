-- Initialize SmartUnderwrite Database
-- This script runs when the PostgreSQL container starts for the first time

-- Create the main database (already created by POSTGRES_DB env var)
-- CREATE DATABASE smartunderwrite;

-- Create additional schemas if needed
-- CREATE SCHEMA IF NOT EXISTS audit;

-- Set up basic database configuration
ALTER DATABASE smartunderwrite SET timezone TO 'UTC';

-- Create extensions that might be needed
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Grant permissions
GRANT ALL PRIVILEGES ON DATABASE smartunderwrite TO postgres;