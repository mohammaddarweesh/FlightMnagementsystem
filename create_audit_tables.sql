-- Create Audit Tables for Flight Booking Engine
-- Run this script if EF migrations fail

-- Create AuditOutbox table
CREATE TABLE IF NOT EXISTS "AuditOutbox" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "CorrelationId" character varying(50) NOT NULL,
    "UserId" uuid,
    "GuestId" character varying(50),
    "Route" character varying(500) NOT NULL,
    "HttpMethod" character varying(10) NOT NULL,
    "IpAddress" character varying(45) NOT NULL,
    "UserAgent" character varying(1000),
    "StatusCode" integer NOT NULL,
    "LatencyMs" bigint NOT NULL,
    "RequestBody" text,
    "ResponseBody" text,
    "ResultSummary" character varying(1000),
    "ErrorMessage" character varying(2000),
    "Timestamp" timestamp with time zone NOT NULL,
    "UserEmail" character varying(256),
    "UserRoles" character varying(500),
    "RequestSize" bigint,
    "ResponseSize" bigint,
    "Headers" text,
    "QueryParameters" character varying(2000),
    "IsProcessed" boolean NOT NULL DEFAULT false,
    "ProcessedAt" timestamp with time zone,
    "RetryCount" integer NOT NULL DEFAULT 0,
    "ProcessingError" character varying(2000),
    "NextRetryAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_AuditOutbox" PRIMARY KEY ("Id")
);

-- Create AuditEvents table
CREATE TABLE IF NOT EXISTS "AuditEvents" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "CorrelationId" character varying(50) NOT NULL,
    "UserId" uuid,
    "GuestId" character varying(50),
    "Route" character varying(500) NOT NULL,
    "HttpMethod" character varying(10) NOT NULL,
    "IpAddress" character varying(45) NOT NULL,
    "UserAgent" character varying(1000),
    "StatusCode" integer NOT NULL,
    "LatencyMs" bigint NOT NULL,
    "RequestBody" text,
    "ResponseBody" text,
    "ResultSummary" character varying(1000),
    "ErrorMessage" character varying(2000),
    "Timestamp" timestamp with time zone NOT NULL,
    "UserEmail" character varying(256),
    "UserRoles" character varying(500),
    "RequestSize" bigint,
    "ResponseSize" bigint,
    "Headers" text,
    "QueryParameters" character varying(2000),
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_AuditEvents" PRIMARY KEY ("Id")
);

-- Create indexes for AuditOutbox
CREATE INDEX IF NOT EXISTS "IX_AuditOutbox_CorrelationId" ON "AuditOutbox" ("CorrelationId");
CREATE INDEX IF NOT EXISTS "IX_AuditOutbox_IsProcessed" ON "AuditOutbox" ("IsProcessed");
CREATE INDEX IF NOT EXISTS "IX_AuditOutbox_NextRetryAt" ON "AuditOutbox" ("NextRetryAt");
CREATE INDEX IF NOT EXISTS "IX_AuditOutbox_Processing" ON "AuditOutbox" ("IsProcessed", "NextRetryAt");
CREATE INDEX IF NOT EXISTS "IX_AuditOutbox_CreatedAt" ON "AuditOutbox" ("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_AuditOutbox_Cleanup" ON "AuditOutbox" ("IsProcessed", "CreatedAt");

-- Create indexes for AuditEvents
CREATE INDEX IF NOT EXISTS "IX_AuditEvents_CorrelationId" ON "AuditEvents" ("CorrelationId");
CREATE INDEX IF NOT EXISTS "IX_AuditEvents_UserId" ON "AuditEvents" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_AuditEvents_GuestId" ON "AuditEvents" ("GuestId");
CREATE INDEX IF NOT EXISTS "IX_AuditEvents_Route" ON "AuditEvents" ("Route");
CREATE INDEX IF NOT EXISTS "IX_AuditEvents_HttpMethod" ON "AuditEvents" ("HttpMethod");
CREATE INDEX IF NOT EXISTS "IX_AuditEvents_StatusCode" ON "AuditEvents" ("StatusCode");
CREATE INDEX IF NOT EXISTS "IX_AuditEvents_Timestamp" ON "AuditEvents" ("Timestamp");
CREATE INDEX IF NOT EXISTS "IX_AuditEvents_IpAddress" ON "AuditEvents" ("IpAddress");
CREATE INDEX IF NOT EXISTS "IX_AuditEvents_UserEmail" ON "AuditEvents" ("UserEmail");
CREATE INDEX IF NOT EXISTS "IX_AuditEvents_UserId_Timestamp" ON "AuditEvents" ("UserId", "Timestamp");
CREATE INDEX IF NOT EXISTS "IX_AuditEvents_Route_Timestamp" ON "AuditEvents" ("Route", "Timestamp");
CREATE INDEX IF NOT EXISTS "IX_AuditEvents_StatusCode_Timestamp" ON "AuditEvents" ("StatusCode", "Timestamp");

-- Verify tables were created
SELECT 'AuditOutbox table created' as status WHERE EXISTS (
    SELECT 1 FROM information_schema.tables 
    WHERE table_name = 'AuditOutbox'
);

SELECT 'AuditEvents table created' as status WHERE EXISTS (
    SELECT 1 FROM information_schema.tables 
    WHERE table_name = 'AuditEvents'
);

-- Show table structures
\d "AuditOutbox"
\d "AuditEvents"
