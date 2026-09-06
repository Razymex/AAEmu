USE aaemu_game;

ALTER TABLE `characters` ADD COLUMN `leadership_point_period` INT NOT NULL DEFAULT '0' AFTER `leadership_point`;

-- One-time backfill for existing installs: without this, every already-played character starts this new
-- per-cycle counter at 0 even though they may have a large lifetime leadership_point total, making them
-- ineligible to vote for the remainder of whatever Hero cycle happens to be active when this migration runs
-- (HeroManager.EnsureLeadershipPeriodReset only zeroes it at the next real LeadershipRanking phase start, which
-- may be far in the future or, for an already-compressed test cycle, may never fire again this cycle at all).
UPDATE `characters` SET `leadership_point_period` = `leadership_point` WHERE `leadership_point_period` = 0 AND `leadership_point` > 0;

CREATE TABLE IF NOT EXISTS `hero_period_resets` (
  `cycle_id` int unsigned NOT NULL COMMENT 'heros.id',
  `reset_at` datetime NOT NULL,
  PRIMARY KEY (`cycle_id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Idempotency marker for per-cycle leadership_point_period resets';
