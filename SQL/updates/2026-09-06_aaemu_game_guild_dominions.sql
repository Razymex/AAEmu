USE aaemu_game;

-- Live guild-owned castle save for zone groups with no siege_zones row (Exeloch / Sungold, 54 / 56).
-- Same DominionData columns as `dominions`, minus faction_id (ownership is expedition_id).
CREATE TABLE IF NOT EXISTS `guild_dominions` (
  `zone_id` smallint unsigned NOT NULL COMMENT 'zone_group_id; one claim per zone group',
  `expedition_id` int unsigned NOT NULL COMMENT 'owning Expedition',
  `house` int unsigned NOT NULL COMMENT 'lodestone House.Id the claim was declared on',
  `guard_tower_setting_id` int unsigned NOT NULL DEFAULT '0',
  `guard_tower_step` tinyint unsigned NOT NULL DEFAULT '0',
  `castle_tier` tinyint unsigned NOT NULL DEFAULT '0',
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
  `siege_period` tinyint unsigned NOT NULL DEFAULT '0',
  `non_pvp_start` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `non_pvp_duration` smallint unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`zone_id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Guild castle claim state for zone groups without a siege schedule';
