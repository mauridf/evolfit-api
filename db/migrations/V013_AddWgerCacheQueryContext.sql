-- ============================================================
-- V013 — Contexto de consulta no cache wger (WGR-005)
-- ============================================================
-- Permite o fallback por parte do corpo: cada exercício em cache
-- registra a consulta que o originou (muscle_id ou category_id),
-- para que a falha da wger use o cache local correspondente.
-- ============================================================
ALTER TABLE wger_exercises_cache
    ADD COLUMN muscle_id INT,
    ADD COLUMN category_id INT;

CREATE INDEX idx_wger_cache_muscle
    ON wger_exercises_cache (muscle_id);

CREATE INDEX idx_wger_cache_category
    ON wger_exercises_cache (category_id);