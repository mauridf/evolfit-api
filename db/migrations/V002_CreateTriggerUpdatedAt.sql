-- ============================================================
-- V002 — Função reutilizável para atualizar updated_at
-- ============================================================
-- Aplicada por tabela via CREATE TRIGGER nas migrations seguintes.
-- ============================================================
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at := CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$;

-- Trigger em users (criada já nesta migration porque users
-- depende da função)
CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();