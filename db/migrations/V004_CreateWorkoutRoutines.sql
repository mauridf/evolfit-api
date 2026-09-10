-- ============================================================
-- V004 — Tabela workout_routines (Módulo Workouts)
-- ============================================================
CREATE TABLE workout_routines (
    id              SERIAL PRIMARY KEY,
    user_id         INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name            VARCHAR(255) NOT NULL,
    goal            VARCHAR(50),
    start_date      DATE NOT NULL,
    end_date        DATE NOT NULL,
    status          INT NOT NULL DEFAULT 1,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT ck_workout_routines_status
        CHECK (status IN (0, 1, 2)),
    CONSTRAINT ck_workout_routines_dates
        CHECK (end_date >= start_date)
);

CREATE INDEX idx_workout_routines_user
    ON workout_routines (user_id);

CREATE INDEX idx_workout_routines_user_status
    ON workout_routines (user_id, status);

CREATE INDEX idx_workout_routines_user_created
    ON workout_routines (user_id, created_at DESC);

CREATE TRIGGER trg_workout_routines_updated_at
    BEFORE UPDATE ON workout_routines
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();