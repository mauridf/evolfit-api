-- ============================================================
-- V010 — Índice único parcial: 1 rotina ativa por usuário (RN-005)
-- ============================================================
-- Garante a invariante de negócio no banco: cada usuário pode ter
-- no máximo um único status = 1 (active). Datapoints em status
-- 0 (paused) ou 2 (completed) não são afetados.
-- ============================================================
CREATE UNIQUE INDEX uq_workout_routines_one_active_per_user
    ON workout_routines (user_id)
    WHERE status = 1;