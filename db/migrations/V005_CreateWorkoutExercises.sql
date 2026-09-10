-- ============================================================
-- V005 — Tabela workout_exercises (Módulo Workouts)
-- ============================================================
CREATE TABLE workout_exercises (
    id                  SERIAL PRIMARY KEY,
    workout_routine_id  INT NOT NULL REFERENCES workout_routines(id) ON DELETE CASCADE,
    day_number          INT NOT NULL,
    wger_exercise_id    INT NOT NULL,
    exercise_name       VARCHAR(255) NOT NULL,
    sets                INT DEFAULT 3,
    reps                INT DEFAULT 10,
    weight              DECIMAL(5,2),
    order_in_day        INT,

    CONSTRAINT ck_workout_exercises_day
        CHECK (day_number BETWEEN 1 AND 180),
    CONSTRAINT ck_workout_exercises_sets
        CHECK (sets BETWEEN 1 AND 20),
    CONSTRAINT ck_workout_exercises_reps
        CHECK (reps BETWEEN 1 AND 100)
);

CREATE INDEX idx_workout_exercises_routine
    ON workout_exercises (workout_routine_id);

CREATE INDEX idx_workout_exercises_routine_day
    ON workout_exercises (workout_routine_id, day_number);