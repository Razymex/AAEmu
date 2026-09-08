USE aaemu_game;

-- One claimed day per account per calendar month (Event Center attendance).
CREATE TABLE IF NOT EXISTS `account_attendances` (
  `account_id` int unsigned NOT NULL,
  `year` smallint NOT NULL,
  `month` tinyint NOT NULL,
  `day` tinyint NOT NULL,
  `attended_at` bigint NOT NULL,
  `is_archelife` tinyint NOT NULL DEFAULT 0,
  PRIMARY KEY (`account_id`, `year`, `month`, `day`),
  KEY `idx_account_month` (`account_id`, `year`, `month`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
