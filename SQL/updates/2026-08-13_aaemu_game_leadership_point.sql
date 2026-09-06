USE aaemu_game;

ALTER TABLE `characters` ADD COLUMN `leadership_point` INT NOT NULL DEFAULT '0' AFTER `vocation_point`;
