-- ============================================
-- Seed Default Profile Types
-- Run this script in your PostgreSQL database
-- ============================================

-- First, check if profile types already exist
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "ProfileTypes" LIMIT 1) THEN
        -- Insert default profile types
        INSERT INTO "ProfileTypes" ("Id", "Name", "DisplayName", "Description", "Icon", "IsActive", "MaxProfilesPerUser", "AllowedFeatures", "CreatedAt", "UpdatedAt")
        VALUES 
        -- Personal Profile
        ('11111111-1111-1111-1111-111111111111', 
         'Personal', 
         'Personal Profile', 
         'For individuals and personal use', 
         '👤', 
         true, 
         3, 
         '["posts", "comments", "followers", "messaging"]', 
         CURRENT_TIMESTAMP, 
         CURRENT_TIMESTAMP),
        
        -- Business Profile
        ('22222222-2222-2222-2222-222222222222', 
         'Business', 
         'Business Profile', 
         'For businesses and organizations', 
         '💼', 
         true, 
         5, 
         '["posts", "comments", "followers", "messaging", "analytics", "advertisements"]', 
         CURRENT_TIMESTAMP, 
         CURRENT_TIMESTAMP),
        
        -- Brand Profile
        ('33333333-3333-3333-3333-333333333333', 
         'Brand', 
         'Brand Profile', 
         'For brands and public figures', 
         '🏢', 
         true, 
         5, 
         '["posts", "comments", "followers", "messaging", "analytics", "verified_badge"]', 
         CURRENT_TIMESTAMP, 
         CURRENT_TIMESTAMP),
        
        -- Creator Profile
        ('44444444-4444-4444-4444-444444444444', 
         'Creator', 
         'Creator Profile', 
         'For content creators and influencers', 
         '🎬', 
         true, 
         3, 
         '["posts", "comments", "followers", "messaging", "monetization", "analytics"]', 
         CURRENT_TIMESTAMP, 
         CURRENT_TIMESTAMP);
        
        RAISE NOTICE 'Successfully seeded 4 default profile types';
    ELSE
        RAISE NOTICE 'ProfileTypes table already contains data - skipping seed';
    END IF;
END $$;

-- Verify the inserted data
SELECT "Id", "Name", "DisplayName", "Description", "IsActive", "MaxProfilesPerUser" 
FROM "ProfileTypes" 
ORDER BY "Name";
