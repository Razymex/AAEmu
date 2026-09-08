USE aaemu_game;

-- One row per owned / in-progress / dropped Arche Pass.

CREATE TABLE IF NOT EXISTS `character_arche_passes` (
  `owner` int unsigned NOT NULL,
  `pass_id` int unsigned NOT NULL,
  `status` tinyint unsigned NOT NULL DEFAULT 0,
  `point` bigint NOT NULL DEFAULT 0,
  `premium` tinyint(1) NOT NULL DEFAULT 0,
  `last_reward_tier` int unsigned NOT NULL DEFAULT 0,
  `last_premium_reward_tier` int unsigned NOT NULL DEFAULT 0,
  PRIMARY KEY (`owner`, `pass_id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='Arche Pass ownership and progress';
