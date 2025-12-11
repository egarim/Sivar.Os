SELECT ""Id"", ""Title"", 
       CASE WHEN ""ContentEmbedding"" IS NULL THEN 'NULL' ELSE 'HAS_VALUE' END as embedding_status
FROM ""Sivar_Posts"" 
LIMIT 5;
