-- =====================================================
-- Script: Enable TimescaleDB Extension
-- Purpose: Enable TimescaleDB extension in the database
-- Date: October 31, 2025
-- =====================================================

-- Enable TimescaleDB extension
CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;

-- Verify extension is installed
SELECT * FROM pg_extension WHERE extname = 'timescaledb';

-- =====================================================
-- IMPORTANT NOTES:
-- - This script is idempotent (safe to run multiple times)
-- - TimescaleDB must be installed in PostgreSQL before running
-- - Extension enables time-series optimizations for PostgreSQL
-- =====================================================
