USE aaemu_game;

-- One row per Dominion Point a serving Hero distributes to a territory their nation owns.
-- Daily count (content_configs.hero_dominion_daily_limit), cooldown (hero_dominion_cooldown) and the
-- weekly cap (hero_rewards.dominion_point_weekly_count) are all derived from this log.
CREATE TABLE IF NOT EXISTS `hero_dominion_point_gives` (
  `id` INT unsigned NOT NULL AUTO_INCREMENT,
  `character_id` INT unsigned NOT NULL,
  `zone_group_id` INT unsigned NOT NULL,
  `points` INT NOT NULL,
  `given_at` DATETIME NOT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_hero_dominion_point_gives_character` (`character_id`, `given_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- A Hero's activity bonus (hero_bonuses) is paid once per term.
CREATE TABLE IF NOT EXISTS `hero_bonus_claims` (
  `character_id` INT unsigned NOT NULL,
  `cycle_id` INT unsigned NOT NULL,
  `claimed_at` DATETIME NOT NULL,
  PRIMARY KEY (`character_id`, `cycle_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
