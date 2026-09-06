USE aaemu_game;

-- Leadership is four figures:
--   leadership_point              current period; ranks the leaderboard and Hero candidacy. Reset by the
--                                 election roll at each LeadershipRanking start, after being copied into
--                                 leadership_period_point. The old leadership_point_period column takes
--                                 this role.
--   leadership_period_point       the previous period's final figure; the Hero vote eligibility gate and
--                                 the sheet's "last season" row. New, starts at 0.
--   accumulated_leadership_point  lifetime total, never reset. Takes over the old leadership_point data.
--   daily_leadership_point / last_daily_leadership_point_time  daily accrual tracker.
ALTER TABLE `characters`
  CHANGE COLUMN `leadership_point` `accumulated_leadership_point` int NOT NULL DEFAULT '0'
    COMMENT 'Lifetime leadership, never reset',
  CHANGE COLUMN `leadership_point_period` `leadership_point` int NOT NULL DEFAULT '0'
    COMMENT 'Current period leadership; reset by the election roll after leadership_period_point is snapshotted',
  ADD COLUMN `leadership_period_point` int NOT NULL DEFAULT '0'
    COMMENT 'Previous period final leadership; Hero vote eligibility gate'
    AFTER `leadership_point`,
  ADD COLUMN `daily_leadership_point` int unsigned NOT NULL DEFAULT '0'
    COMMENT 'Leadership earned since last_daily_leadership_point_time'
    AFTER `accumulated_leadership_point`,
  ADD COLUMN `last_daily_leadership_point_time` datetime NOT NULL DEFAULT '1970-01-01 00:00:00'
    COMMENT 'When the daily leadership counter last rolled over'
    AFTER `daily_leadership_point`;
