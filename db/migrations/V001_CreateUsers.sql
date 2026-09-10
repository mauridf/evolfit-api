-- ============================================================
-- V001 — Tabela users (Módulo Auth)
-- ============================================================
CREATE TABLE users (
    id              SERIAL PRIMARY KEY,
    username        VARCHAR(100) NOT NULL UNIQUE,
    email           VARCHAR(255) NOT NULL UNIQUE,
    password_hash   VARCHAR(500) NOT NULL,
    display_name    VARCHAR(255) NOT NULL,
    birth_date      DATE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_auth_users_created ON users (created_at);