-- ============================================================
-- V007 — Views de agregação para o Dashboard
-- ============================================================

-- ------------------------------------------------------------
-- v_daily_completion — progresso diário de exercícios
-- ------------------------------------------------------------
CREATE OR REPLACE VIEW v_daily_completion AS
SELECT
    el.user_id,
    el.date,
    COUNT(*)                                        AS total_exercises,
    COUNT(*) FILTER (WHERE el.completed)            AS completed_exercises,
    CASE
        WHEN COUNT(*) = 0 THEN 0
        ELSE ROUND(100.0 * COUNT(*) FILTER (WHERE el.completed) / COUNT(*), 2)
    END                                             AS completion_percent
FROM exercise_logs el
GROUP BY el.user_id, el.date;

-- ------------------------------------------------------------
-- v_latest_health_metric — última medição por usuário
-- ------------------------------------------------------------
CREATE OR REPLACE VIEW v_latest_health_metric AS
SELECT DISTINCT ON (hm.user_id)
    hm.user_id,
    hm.id,
    hm.bmi,
    hm.bmr,
    hm.tdee,
    hm.weight_kg,
    hm.height_cm,
    hm.measured_at
FROM health_metrics hm
ORDER BY hm.user_id, hm.measured_at DESC;