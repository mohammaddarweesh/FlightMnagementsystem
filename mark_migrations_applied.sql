-- Mark existing migrations as applied
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") 
VALUES 
    ('20250830220346_AddIdentityEntities', '8.0.0'),
    ('20250831163958_AddAuditSystem', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;
