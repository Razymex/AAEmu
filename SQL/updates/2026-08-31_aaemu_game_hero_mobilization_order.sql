USE aaemu_game;

-- Mobilization Order counters for a serving Hero. today_count is shown against
-- content_configs.mobilization_order_daily_count_max; total_count is the term total that
-- hero_bonuses.mobilization_order_count checks. Both restart when the character takes a new seat.
ALTER TABLE `characters` ADD COLUMN `mobilization_order_today_count` INT NOT NULL DEFAULT '0' AFTER `last_daily_leadership_point_time`;
ALTER TABLE `characters` ADD COLUMN `mobilization_order_total_count` INT NOT NULL DEFAULT '0' AFTER `mobilization_order_today_count`;
ALTER TABLE `characters` ADD COLUMN `last_mobilization_order_time` DATETIME NOT NULL DEFAULT '1970-01-01 00:00:00' AFTER `mobilization_order_total_count`;
