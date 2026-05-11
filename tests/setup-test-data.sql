-- Setup test data for MerchantProcessing system
-- Run this script after creating the database schema

-- Create a test account
INSERT INTO "Accounts" ("Id", "Balance", "Version", "LastUpdated", "CreatedAt")
VALUES 
    ('00000000-0000-0000-0000-000000000001', 0.00, 0, NOW(), NOW())
ON CONFLICT ("Id") DO NOTHING;

-- Verify account was created
SELECT * FROM "Accounts" WHERE "Id" = '00000000-0000-0000-0000-000000000001';
