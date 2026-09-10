-- ============================================================
-- V003 — Tabela health_metrics (Módulo Health)
-- ============================================================
CREATE TABLE health_metrics (
    id              SERIAL PRIMARY KEY,
    user_id         INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    height_cm       DECIMAL(5,2) NOT NULL,
    weight_kg       DECIMAL(5,2) NOT NULL,
    bmi             DECIMAL(4,2) NOT NULL,
    bmr             INT,
    tdee            INT,
    activity_level  VARCHAR(20),
    measured_at     TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT ck_health_metrics_activity
        CHECK (activity_level IN ('sedentary','light','moderate','active','extreme')),
    CONSTRAINT ck_health_metrics_height
        CHECK (height_cm BETWEEN 100 AND 250),
    CONSTRAINT ck_health_metrics_weight
        CHECK (weight_kg BETWEEN 40 AND 300)
);

CREATE INDEX idx_health_metrics_user
    ON health_metrics (user_id);

CREATE INDEX idx_health_metrics_user_measured
    ON health_metrics (user_id, measured_at DESC);