USE aaemu_game;

ALTER TABLE `dominions` ADD COLUMN `castle_tier` TINYINT UNSIGNED NOT NULL DEFAULT '0' AFTER `guard_tower_step`;
