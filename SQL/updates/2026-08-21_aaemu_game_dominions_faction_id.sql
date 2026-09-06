USE aaemu_game;

-- Real castle ownership split, confirmed by the user 2026-08-21: zone groups 33/34/43/44 (Salpimari/Nuimari/
-- Marcala/Calmlands - the ones with real siege_zones/siege_plans schedule rows) can only be owned by the
-- Nuia/Haranya alliance factions, claimed via that faction's elected Hero - not by an arbitrary guild. Zone
-- groups 54/56 (Exeloch/Sungold Fields, no siege_zones data) stay guild-owned via the existing expedition_id
-- column, unchanged. This column carries the real FactionsEnum id (148 Nuia / 149 Haranya) for the former case;
-- 0/unused for guild-owned rows, mirroring how expedition_id is 0/unused for faction-owned rows.
ALTER TABLE `dominions` ADD COLUMN `faction_id` INT UNSIGNED NOT NULL DEFAULT '0' AFTER `expedition_id`;
