-- ============================================================
-- V006 — Tabela exercise_logs (Módulo Workouts)
-- ============================================================
CREATE TABLE exercise_logs (
    id                  SERIAL PRIMARY KEY,
    user_id             INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    workout_exercise_id INT NOT NULL REFERENCES workout_exercises(id) ON DELETE CASCADE,
    date                DATE NOT NULL,
    completed           BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT uq_exercise_logs_user_exercise_date
        UNIQUE (user_id, workout_exercise_id, date)
);

CREATE INDEX idx_exercise_logs_user
    ON exercise_logs (user_id);

CREATE INDEX idx_exercise_logs_user_date
    ON exercise_logs (user_id, date);

CREATE INDEX idx_exercise_logs_exercise
    ON exercise_logs (workout_exercise_id);