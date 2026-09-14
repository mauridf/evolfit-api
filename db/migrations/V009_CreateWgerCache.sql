-- ============================================================
-- V009 — Tabela wger_exercises_cache (Módulo Workouts)
-- ============================================================
-- Cache local de exercícios da wger API (WGR-002).
-- Reduz chamadas externas e permite funcionar offline após a
-- primeira consulta.
-- ============================================================
CREATE TABLE wger_exercises_cache (
    id                  SERIAL PRIMARY KEY,
    wger_exercise_id    INT NOT NULL UNIQUE,
    name                VARCHAR(255) NOT NULL,
    description         TEXT,
    category            VARCHAR(100),
    muscles             JSONB,
    equipment           JSONB,
    images              JSONB,
    cached_at           TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at          TIMESTAMPTZ NOT NULL
);

CREATE INDEX idx_wger_cache_exercise_id
    ON wger_exercises_cache (wger_exercise_id);

CREATE INDEX idx_wger_cache_expires
    ON wger_exercises_cache (expires_at);