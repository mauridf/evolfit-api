-- ============================================================
-- V014 — Coluna name_pt no cache wger (busca PT)
-- ============================================================
-- Adiciona coluna para armazenar o nome do exercício em
-- português, permitindo busca local PT e exibição preferencial.
-- ============================================================
ALTER TABLE wger_exercises_cache
    ADD COLUMN name_pt VARCHAR(255);

CREATE INDEX idx_wger_cache_name_pt
    ON wger_exercises_cache (name_pt);
