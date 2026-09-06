USE aaemu_game;

-- Live Dominion (Castle) claim state. `siege_zones`/`siege_plans`/`guard_tower_settings` in the client's
-- game_decrypted.sqlite3 are read-only schedule/territory templates; this is the actual save state for who
-- currently owns which zone group's dominion, so it must survive a World restart.
CREATE TABLE IF NOT EXISTS `dominions` (
  `zone_id` smallint unsigned NOT NULL COMMENT 'zone_group_id the dominion belongs to; one claim per zone group',
  `expedition_id` int unsigned NOT NULL COMMENT 'owning Expedition (guild alliance)',
  `house` int unsigned NOT NULL COMMENT 'lodestone House.Id the claim was declared on',
  `guard_tower_setting_id` int unsigned NOT NULL DEFAULT '0' COMMENT 'guard_tower_settings.id used to resolve TerritoryData on load',
  `tax_rate` int NOT NULL DEFAULT '0',
  `x` float NOT NULL DEFAULT '0',
  `y` float NOT NULL DEFAULT '0',
  `z` float NOT NULL DEFAULT '0',
  `cur_house_tax_money` int NOT NULL DEFAULT '0',
  `cur_hunt_tax_money` int NOT NULL DEFAULT '0',
  `peace_tax_money` int NOT NULL DEFAULT '0',
  `cur_house_tax_aa_point` int NOT NULL DEFAULT '0',
  `peace_tax_aa_point` int NOT NULL DEFAULT '0',
  `last_paid_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `last_siege_end_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `reign_start_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `last_tax_rate_changed_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `siege_period` tinyint unsigned NOT NULL DEFAULT '0' COMMENT 'enum_siege_periods id; owned here until SiegeManager exists',
  `non_pvp_start` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `non_pvp_duration` smallint unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`zone_id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Live Dominion/Castle claim state, one row per claimed zone group';
