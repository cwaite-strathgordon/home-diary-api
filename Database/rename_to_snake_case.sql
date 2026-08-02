-- ============================================================
-- Rename all tables and columns to snake_case
-- Run once after migration.sql has been applied
-- ============================================================

-- ── Rename tables ─────────────────────────────────────────────

ALTER TABLE "Area_tbl"             RENAME TO area;
ALTER TABLE "Contact_tbl"          RENAME TO contact;
ALTER TABLE "EventContactLink_tbl" RENAME TO event_contact_link;
ALTER TABLE "EventStatus_tbl"      RENAME TO event_status;
ALTER TABLE "EventType_tbl"        RENAME TO event_type;
ALTER TABLE "HomeEvents_tbl"       RENAME TO home_event;
ALTER TABLE "User_tbl"             RENAME TO app_user;
ALTER TABLE "ErrorLog_tbl"         RENAME TO error_log;

-- ── area ──────────────────────────────────────────────────────

ALTER TABLE area RENAME COLUMN "AreaId"      TO area_id;
ALTER TABLE area RENAME COLUMN "Title"       TO title;
ALTER TABLE area RENAME COLUMN "Description" TO description;

-- ── contact ───────────────────────────────────────────────────

ALTER TABLE contact RENAME COLUMN "ContactId"   TO contact_id;
ALTER TABLE contact RENAME COLUMN "FirstName"   TO first_name;
ALTER TABLE contact RENAME COLUMN "LastName"    TO last_name;
ALTER TABLE contact RENAME COLUMN "Email"       TO email;
ALTER TABLE contact RENAME COLUMN "Mobile"      TO mobile;
ALTER TABLE contact RENAME COLUMN "CompanyName" TO company_name;

-- ── event_contact_link ────────────────────────────────────────

ALTER TABLE event_contact_link RENAME COLUMN "ContactId" TO contact_id;
ALTER TABLE event_contact_link RENAME COLUMN "EventId"   TO event_id;

-- ── event_status ──────────────────────────────────────────────

ALTER TABLE event_status RENAME COLUMN "EventStatusId" TO event_status_id;
ALTER TABLE event_status RENAME COLUMN "Title"         TO title;
ALTER TABLE event_status RENAME COLUMN "Description"   TO description;

-- ── event_type ────────────────────────────────────────────────

ALTER TABLE event_type RENAME COLUMN "EventTypeId"  TO event_type_id;
ALTER TABLE event_type RENAME COLUMN "Title"        TO title;
ALTER TABLE event_type RENAME COLUMN "Description"  TO description;

-- ── home_event ────────────────────────────────────────────────

ALTER TABLE home_event RENAME COLUMN "EventId"       TO event_id;
ALTER TABLE home_event RENAME COLUMN "Title"         TO title;
ALTER TABLE home_event RENAME COLUMN "Description"   TO description;
ALTER TABLE home_event RENAME COLUMN "EventDate"     TO event_date;
ALTER TABLE home_event RENAME COLUMN "CreatedDate"   TO created_date;
ALTER TABLE home_event RENAME COLUMN "CreatedById"   TO created_by_id;
ALTER TABLE home_event RENAME COLUMN "UpdatedDate"   TO updated_date;
ALTER TABLE home_event RENAME COLUMN "EventTypeId"   TO event_type_id;
ALTER TABLE home_event RENAME COLUMN "AreaId"        TO area_id;
ALTER TABLE home_event RENAME COLUMN "EventStatusId" TO event_status_id;

-- ── app_user (avoid reserved word "user") ─────────────────────

ALTER TABLE app_user RENAME COLUMN "UserId"        TO user_id;
ALTER TABLE app_user RENAME COLUMN "FirstName"     TO first_name;
ALTER TABLE app_user RENAME COLUMN "LastName"      TO last_name;
ALTER TABLE app_user RENAME COLUMN "Email"         TO email;
ALTER TABLE app_user RENAME COLUMN "Admin"         TO admin;
ALTER TABLE app_user RENAME COLUMN "MobileNumber"  TO mobile_number;
ALTER TABLE app_user RENAME COLUMN "OAuthProvider" TO oauth_provider;
ALTER TABLE app_user RENAME COLUMN "OAuthId"       TO oauth_id;
ALTER TABLE app_user RENAME COLUMN "OAuthEmail"    TO oauth_email;

-- ── error_log ─────────────────────────────────────────────────

ALTER TABLE error_log RENAME COLUMN "ErrorLogId"   TO error_log_id;
ALTER TABLE error_log RENAME COLUMN "ErrorMessage" TO error_message;
ALTER TABLE error_log RENAME COLUMN "StackTrace"   TO stack_trace;
ALTER TABLE error_log RENAME COLUMN "Source"       TO source;
ALTER TABLE error_log RENAME COLUMN "CreatedDate"  TO created_date;
