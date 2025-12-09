-- Get Sivar_Posts table structure
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'Sivar_Posts' 
ORDER BY ordinal_position;
