-- Migration: Add position_level to positions table
-- Date: 2026-10-17

ALTER TABLE positions ADD COLUMN IF NOT EXISTS position_level integer NOT NULL DEFAULT 1;

-- Backfill existing positions based on role/title keywords:
UPDATE positions 
SET position_level = 5 
WHERE position_name ILIKE '%giám đốc%' 
   OR position_name ILIKE '%giam doc%' 
   OR position_name ILIKE '%director%' 
   OR position_name ILIKE '%ceo%' 
   OR position_name ILIKE '%tổng giám đốc%';

UPDATE positions 
SET position_level = 4 
WHERE (position_name ILIKE '%trưởng phòng%' 
   OR position_name ILIKE '%truong phong%' 
   OR position_name ILIKE '%trưởng ban%' 
   OR position_name ILIKE '%head%' 
   OR position_name ILIKE '%lead manager%')
  AND position_level < 5;

UPDATE positions 
SET position_level = 3 
WHERE (position_name ILIKE '%phó phòng%' 
   OR position_name ILIKE '%pho phong%' 
   OR position_name ILIKE '%phó trưởng phòng%' 
   OR position_name ILIKE '%trưởng nhóm%' 
   OR position_name ILIKE '%truong nhom%' 
   OR position_name ILIKE '%team leader%' 
   OR position_name ILIKE '%leader%' 
   OR position_name ILIKE '%supervisor%')
  AND position_level < 4;

UPDATE positions 
SET position_level = 2 
WHERE (position_name ILIKE '%chuyên viên%' 
   OR position_name ILIKE '%chuyen vien%' 
   OR position_name ILIKE '%senior%' 
   OR position_name ILIKE '%specialist%' 
   OR position_name ILIKE '%kỹ sư%' 
   OR position_name ILIKE '%ky su%')
  AND position_level < 3;
