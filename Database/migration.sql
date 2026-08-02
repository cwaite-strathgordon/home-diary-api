-- ============================================================
-- HomeDiary schema migration
-- Run once against the HomeDiary database before starting the API
-- ============================================================

-- ── Fix all CHAR(1) columns → proper varchar lengths ──────────

ALTER TABLE "Area_tbl"
    ALTER COLUMN "Title"       TYPE varchar(255),
    ALTER COLUMN "Description" TYPE varchar(500);

ALTER TABLE "Contact_tbl"
    ALTER COLUMN "FirstName"   TYPE varchar(255),
    ALTER COLUMN "LastName"    TYPE varchar(255),
    ALTER COLUMN "Email"       TYPE varchar(255),
    ALTER COLUMN "Mobile"      TYPE varchar(50),
    ALTER COLUMN "CompanyName" TYPE varchar(255);

ALTER TABLE "EventStatus_tbl"
    ALTER COLUMN "Title"       TYPE varchar(255),
    ALTER COLUMN "Description" TYPE varchar(500);

ALTER TABLE "EventType_tbl"
    ALTER COLUMN "Title"       TYPE varchar(255),
    ALTER COLUMN "Description" TYPE varchar(500);

ALTER TABLE "User_tbl"
    ALTER COLUMN "FirstName"    TYPE varchar(255),
    ALTER COLUMN "LastName"     TYPE varchar(255),
    ALTER COLUMN "Email"        TYPE varchar(255),
    ALTER COLUMN "MobileNumber" TYPE varchar(50);

-- ── Fix Admin column: bit(1) → boolean ───────────────────────

ALTER TABLE "User_tbl"
    ALTER COLUMN "Admin" TYPE boolean
    USING CASE WHEN "Admin" = B'1' THEN true ELSE false END;

-- ── Add missing PRIMARY KEY to Contact_tbl ───────────────────

ALTER TABLE "Contact_tbl" ADD CONSTRAINT "Contact_tbl_pkey" PRIMARY KEY ("ContactId");

-- ── Add SERIAL sequences to all PKs ──────────────────────────

CREATE SEQUENCE IF NOT EXISTS area_areaid_seq;
SELECT setval('area_areaid_seq', COALESCE(MAX("AreaId"), 0) + 1, false) FROM "Area_tbl";
ALTER TABLE "Area_tbl" ALTER COLUMN "AreaId" SET DEFAULT nextval('area_areaid_seq');
ALTER SEQUENCE area_areaid_seq OWNED BY "Area_tbl"."AreaId";

CREATE SEQUENCE IF NOT EXISTS contact_contactid_seq;
SELECT setval('contact_contactid_seq', COALESCE(MAX("ContactId"), 0) + 1, false) FROM "Contact_tbl";
ALTER TABLE "Contact_tbl" ALTER COLUMN "ContactId" SET DEFAULT nextval('contact_contactid_seq');
ALTER SEQUENCE contact_contactid_seq OWNED BY "Contact_tbl"."ContactId";

CREATE SEQUENCE IF NOT EXISTS eventstatus_eventstatusid_seq;
SELECT setval('eventstatus_eventstatusid_seq', COALESCE(MAX("EventStatusId"), 0) + 1, false) FROM "EventStatus_tbl";
ALTER TABLE "EventStatus_tbl" ALTER COLUMN "EventStatusId" SET DEFAULT nextval('eventstatus_eventstatusid_seq');
ALTER SEQUENCE eventstatus_eventstatusid_seq OWNED BY "EventStatus_tbl"."EventStatusId";

CREATE SEQUENCE IF NOT EXISTS eventtype_eventtypeid_seq;
SELECT setval('eventtype_eventtypeid_seq', COALESCE(MAX("EventTypeId"), 0) + 1, false) FROM "EventType_tbl";
ALTER TABLE "EventType_tbl" ALTER COLUMN "EventTypeId" SET DEFAULT nextval('eventtype_eventtypeid_seq');
ALTER SEQUENCE eventtype_eventtypeid_seq OWNED BY "EventType_tbl"."EventTypeId";

CREATE SEQUENCE IF NOT EXISTS homeevents_eventid_seq;
SELECT setval('homeevents_eventid_seq', COALESCE(MAX("EventId"), 0) + 1, false) FROM "HomeEvents_tbl";
ALTER TABLE "HomeEvents_tbl" ALTER COLUMN "EventId" SET DEFAULT nextval('homeevents_eventid_seq');
ALTER SEQUENCE homeevents_eventid_seq OWNED BY "HomeEvents_tbl"."EventId";

CREATE SEQUENCE IF NOT EXISTS user_userid_seq;
SELECT setval('user_userid_seq', COALESCE(MAX("UserId"), 0) + 1, false) FROM "User_tbl";
ALTER TABLE "User_tbl" ALTER COLUMN "UserId" SET DEFAULT nextval('user_userid_seq');
ALTER SEQUENCE user_userid_seq OWNED BY "User_tbl"."UserId";

-- ── Add OAuth support columns to User_tbl ────────────────────

ALTER TABLE "User_tbl"
    ADD COLUMN IF NOT EXISTS "OAuthProvider" varchar(50),
    ADD COLUMN IF NOT EXISTS "OAuthId"       varchar(255),
    ADD COLUMN IF NOT EXISTS "OAuthEmail"    varchar(255);

CREATE INDEX IF NOT EXISTS ix_user_oauth ON "User_tbl" ("OAuthProvider", "OAuthId");

-- ── Create ErrorLog table ─────────────────────────────────────

CREATE TABLE IF NOT EXISTS "ErrorLog_tbl" (
    "ErrorLogId"   SERIAL PRIMARY KEY,
    "ErrorMessage" TEXT,
    "StackTrace"   TEXT,
    "Source"       varchar(255),
    "CreatedDate"  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ── Add foreign key constraints ───────────────────────────────

DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_homeevents_eventtype'
    ) THEN
        ALTER TABLE "HomeEvents_tbl"
            ADD CONSTRAINT fk_homeevents_eventtype
                FOREIGN KEY ("EventTypeId") REFERENCES "EventType_tbl" ("EventTypeId");
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_homeevents_area'
    ) THEN
        ALTER TABLE "HomeEvents_tbl"
            ADD CONSTRAINT fk_homeevents_area
                FOREIGN KEY ("AreaId") REFERENCES "Area_tbl" ("AreaId");
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_homeevents_eventstatus'
    ) THEN
        ALTER TABLE "HomeEvents_tbl"
            ADD CONSTRAINT fk_homeevents_eventstatus
                FOREIGN KEY ("EventStatusId") REFERENCES "EventStatus_tbl" ("EventStatusId");
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_homeevents_createdby'
    ) THEN
        ALTER TABLE "HomeEvents_tbl"
            ADD CONSTRAINT fk_homeevents_createdby
                FOREIGN KEY ("CreatedById") REFERENCES "User_tbl" ("UserId");
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_ecl_contact'
    ) THEN
        ALTER TABLE "EventContactLink_tbl"
            ADD CONSTRAINT fk_ecl_contact
                FOREIGN KEY ("ContactId") REFERENCES "Contact_tbl" ("ContactId");
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_ecl_event'
    ) THEN
        ALTER TABLE "EventContactLink_tbl"
            ADD CONSTRAINT fk_ecl_event
                FOREIGN KEY ("EventId") REFERENCES "HomeEvents_tbl" ("EventId");
    END IF;
END $$;
