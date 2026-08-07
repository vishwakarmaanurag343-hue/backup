START TRANSACTION;

CREATE TABLE "CaseMemories" (
    "Id" uuid NOT NULL,
    "CaseId" uuid NOT NULL,
    "CaseTitle" text NOT NULL,
    "CaseType" text NOT NULL,
    "ShortSummary" text NOT NULL,
    "CurrentStatus" text NOT NULL,
    "KeyFacts" text NOT NULL,
    "ImportantDates" text NOT NULL,
    "Parties" text NOT NULL,
    "LegalIssues" text NOT NULL,
    "CurrentObjective" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "LastUpdated" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_CaseMemories" PRIMARY KEY ("Id")
);

CREATE TABLE "ConversationMemories" (
    "Id" uuid NOT NULL,
    "CaseId" uuid NOT NULL,
    "ConversationSummary" text NOT NULL,
    "ImportantDecisions" text NOT NULL,
    "PreviousAiSuggestions" text NOT NULL,
    "PendingTasks" text NOT NULL,
    "MessageCountSinceLastSummary" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "LastUpdated" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ConversationMemories" PRIMARY KEY ("Id")
);

CREATE TABLE "DraftMemories" (
    "Id" uuid NOT NULL,
    "CaseId" uuid NOT NULL,
    "DraftType" text NOT NULL,
    "DraftVersion" text NOT NULL,
    "DraftStatus" text NOT NULL,
    "LastDraftContent" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "LastUpdated" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_DraftMemories" PRIMARY KEY ("Id")
);

CREATE TABLE "UserPreferences" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "PreferredLanguage" text NOT NULL,
    "WritingStyle" text NOT NULL,
    "CitationStyle" text NOT NULL,
    "PreferredJurisdiction" text NOT NULL,
    "DraftFormat" text NOT NULL,
    "SignatureFormat" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "LastUpdated" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_UserPreferences" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260806100823_Phase1_MemoryAndContext', '8.0.11');

COMMIT;

START TRANSACTION;

CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE "DocumentChunks" (
    "Id" uuid NOT NULL,
    "DocumentId" uuid NOT NULL,
    "CaseId" uuid NOT NULL,
    "PageNumber" integer,
    "Section" text,
    "Heading" text,
    "TextContent" text NOT NULL,
    "Embedding" vector(1536),
    "Metadata" jsonb,
    "DocumentType" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_DocumentChunks" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260806101612_Phase2_DocumentChunks', '8.0.11');

COMMIT;

START TRANSACTION;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260806114059_AddAiTelemetryLog', '8.0.11');

COMMIT;

