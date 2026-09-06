USE aaemu_game;

-- Siege raid-team roster: who's registered to fight offense/defense in a zone group's next/current siege, and
-- the running score counters (SCSiegeScorePointPacket: outlawPoint/defensePoint/offensePoint). Raid-commander
-- election (SCElectSiegeRaidOwnerPacket) is not part of this table - not built yet, see the brief.
CREATE TABLE IF NOT EXISTS `siege_raid_team_members` (
  `zone_id` smallint unsigned NOT NULL COMMENT 'zone_group_id, matches dominions/siege_zones',
  `character_id` int unsigned NOT NULL,
  `is_offense` tinyint(1) NOT NULL DEFAULT '0',
  `registered_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`zone_id`, `character_id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Siege raid-team registration per zone group';

CREATE TABLE IF NOT EXISTS `siege_scores` (
  `zone_id` smallint unsigned NOT NULL,
  `outlaw_point` int unsigned NOT NULL DEFAULT '0',
  `defense_point` int unsigned NOT NULL DEFAULT '0',
  `offense_point` int unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`zone_id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Live siege score counters per zone group, reset each siege cycle';
