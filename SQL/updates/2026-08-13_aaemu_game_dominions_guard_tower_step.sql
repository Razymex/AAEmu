USE aaemu_game;

ALTER TABLE `dominions` ADD COLUMN `guard_tower_step` TINYINT UNSIGNED NOT NULL DEFAULT '0' AFTER `guard_tower_setting_id`;
