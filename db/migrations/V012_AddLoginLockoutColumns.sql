-- ============================================================
-- V012 — Colunas de lockout de login (SECURITY §7)
-- ============================================================
-- Bloqueio temporário da conta após 5 falhas de login
-- consecutivas (janela 15 min). failed_login_count acumula as
-- falhas; lockout_until != NULL e > now indica conta bloqueada.
-- ============================================================
ALTER TABLE users
    ADD COLUMN failed_login_count INTEGER NOT NULL DEFAULT 0,
    ADD COLUMN lockout_until TIMESTAMPTZ;