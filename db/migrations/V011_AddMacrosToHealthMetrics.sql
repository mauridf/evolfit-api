-- ============================================================
-- V011 — macros_suggestion persistida em health_metrics (DAT-003)
-- ============================================================
-- A sugestão de macronutrientes (proteína/carbs/gordura) é calculada
-- pela TinyFn junto com BMR/TDEE e agora é persistida para ser devolvida
-- em GET /health/metrics/{id} e GET /health/metrics/latest (API_REFERENCE §4).
-- ============================================================
ALTER TABLE health_metrics
    ADD COLUMN protein_g INT,
    ADD COLUMN carbs_g  INT,
    ADD COLUMN fat_g    INT;