-- Migration: Add bonus_amount and deductions to payrolls table
-- Date: 2026-10-17

ALTER TABLE payrolls ADD COLUMN IF NOT EXISTS bonus_amount numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE payrolls ADD COLUMN IF NOT EXISTS deductions numeric(18,2) NOT NULL DEFAULT 0;
