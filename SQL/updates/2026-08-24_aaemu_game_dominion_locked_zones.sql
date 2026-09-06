USE aaemu_game;

-- GM-controlled "no new claims" lock per zone group, for both castle systems. See
-- DominionZoneLockManager/IDominionZoneLockManager's doc comment - deliberately narrow scope: blocks new
-- claims only, does not touch any existing claim's state or progression.
CREATE TABLE IF NOT EXISTS `dominion_locked_zones` (
  `zone_id` smallint unsigned NOT NULL COMMENT 'zone_group_id the castle system is locked for',
  `locked_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`zone_id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Zone groups where new Dominion/castle claims are currently blocked by a GM';
