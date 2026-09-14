-- ============================================================
-- V008 — Tabela refresh_tokens (Módulo Auth)
-- ============================================================
-- Armazena refresh tokens ativos. Cada rotação cria uma nova
-- linha e marca a anterior como revogada. Reuso de token
-- revogado = possível replay → revoga toda a família do usuário.
-- ============================================================
CREATE TABLE refresh_tokens (
    id              SERIAL PRIMARY KEY,
    user_id         INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash      VARCHAR(500) NOT NULL UNIQUE,
    expires_at      TIMESTAMPTZ NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    revoked_at      TIMESTAMPTZ,
    replaced_by_id  INT REFERENCES refresh_tokens(id) ON DELETE SET NULL
);

CREATE INDEX idx_refresh_tokens_user
    ON refresh_tokens (user_id);

CREATE INDEX idx_refresh_tokens_user_active
    ON refresh_tokens (user_id, revoked_at)
    WHERE revoked_at IS NULL;