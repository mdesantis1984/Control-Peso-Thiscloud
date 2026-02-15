-- ThisCloud.ControlPeso — SQLite schema v1
-- Fuente de verdad (Database First). Ejecutar este script para crear la DB inicial.
-- Nota: ajustar tras CP0.4 si el sitio de referencia aporta entidades adicionales.

PRAGMA foreign_keys = ON;

-- Users (Google OAuth)
CREATE TABLE IF NOT EXISTS tc_users (
  id               TEXT    NOT NULL PRIMARY KEY, -- GUID
  google_subject   TEXT    NOT NULL,
  email            TEXT    NOT NULL,
  display_name     TEXT    NOT NULL,
  avatar_url       TEXT    NULL,
  locale           TEXT    NOT NULL DEFAULT 'es-ES',
  is_admin         INTEGER NOT NULL DEFAULT 0,
  created_utc      TEXT    NOT NULL,
  last_login_utc   TEXT    NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_tc_users_google_subject ON tc_users(google_subject);
CREATE UNIQUE INDEX IF NOT EXISTS ux_tc_users_email ON tc_users(email);

-- Weight entries
CREATE TABLE IF NOT EXISTS tc_weight_entries (
  id          TEXT NOT NULL PRIMARY KEY, -- GUID
  user_id     TEXT NOT NULL,
  date_local  TEXT NOT NULL, -- ISO yyyy-mm-dd
  weight_kg   REAL NOT NULL,
  notes       TEXT NULL,
  created_utc TEXT NOT NULL,
  CONSTRAINT fk_weight_user FOREIGN KEY(user_id) REFERENCES tc_users(id) ON DELETE CASCADE,
  CONSTRAINT ck_weight_positive CHECK (weight_kg > 0)
);

CREATE INDEX IF NOT EXISTS ix_tc_weight_entries_user_date ON tc_weight_entries(user_id, date_local);

-- Goals (1 active goal per user enforced at application level)
CREATE TABLE IF NOT EXISTS tc_goals (
  id               TEXT NOT NULL PRIMARY KEY, -- GUID
  user_id          TEXT NOT NULL,
  start_weight_kg  REAL NOT NULL,
  target_weight_kg REAL NOT NULL,
  start_date_local TEXT NOT NULL,
  target_date_local TEXT NULL,
  status           TEXT NOT NULL DEFAULT 'Active', -- Active|Completed|Cancelled
  created_utc      TEXT NOT NULL,
  CONSTRAINT fk_goal_user FOREIGN KEY(user_id) REFERENCES tc_users(id) ON DELETE CASCADE,
  CONSTRAINT ck_goal_target_positive CHECK (target_weight_kg > 0),
  CONSTRAINT ck_goal_start_positive CHECK (start_weight_kg > 0)
);

CREATE INDEX IF NOT EXISTS ix_tc_goals_user_status ON tc_goals(user_id, status);

-- Audit (mínimo)
CREATE TABLE IF NOT EXISTS tc_audit (
  id          TEXT NOT NULL PRIMARY KEY, -- GUID
  user_id     TEXT NOT NULL,
  action      TEXT NOT NULL,
  details_json TEXT NOT NULL,
  created_utc TEXT NOT NULL,
  CONSTRAINT fk_audit_user FOREIGN KEY(user_id) REFERENCES tc_users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_tc_audit_user_created ON tc_audit(user_id, created_utc);
