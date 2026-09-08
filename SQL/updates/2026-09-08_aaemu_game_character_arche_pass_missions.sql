USE aaemu_game;

-- Weekly Arche Pass mission complete / change counters.

CREATE TABLE IF NOT EXISTS `character_arche_pass_missions` (
  `owner` int unsigned NOT NULL,
  `complete_used` int unsigned NOT NULL DEFAULT 0,
  `change_used` int unsigned NOT NULL DEFAULT 0,
  `week_start` date NOT NULL,
  PRIMARY KEY (`owner`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='Arche Pass weekly mission counters';
