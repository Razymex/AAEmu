USE aaemu_game;

-- Per-account progress for HUD schedule-item timers (gave / playtime / last update).
CREATE TABLE IF NOT EXISTS `account_schedule_items` (
  `account_id` int unsigned NOT NULL,
  `schedule_id` int NOT NULL,
  `gave` tinyint unsigned NOT NULL DEFAULT 0,
  `cumulated` bigint NOT NULL DEFAULT 0,
  `updated` bigint NOT NULL,
  PRIMARY KEY (`account_id`, `schedule_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
