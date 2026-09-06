USE aaemu_game;

-- Live per-faction state for the current/most-recent hero election cycle. `heros`/`hero_schedules` in
-- game_decrypted.sqlite3 are the shared (not per-faction) calendar template; this is the actual save state.
CREATE TABLE IF NOT EXISTS `hero_candidates` (
  `cycle_id` int unsigned NOT NULL COMMENT 'heros.id',
  `faction_id` int unsigned NOT NULL,
  `character_id` int unsigned NOT NULL,
  `leadership_point_at_ranking` int NOT NULL DEFAULT '0' COMMENT 'snapshotted when the candidate list was computed, for the reward-ranking tie-break',
  `votes` int unsigned NOT NULL DEFAULT '0',
  `abstained` tinyint(1) NOT NULL DEFAULT '0',
  `elected` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`cycle_id`, `faction_id`, `character_id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Hero election candidates per cycle/faction';

-- One row per voter per cycle/faction so a re-vote updates rather than double-counts.
CREATE TABLE IF NOT EXISTS `hero_votes` (
  `cycle_id` int unsigned NOT NULL,
  `faction_id` int unsigned NOT NULL,
  `voter_character_id` int unsigned NOT NULL,
  `candidate_character_id` int unsigned NOT NULL,
  PRIMARY KEY (`cycle_id`, `faction_id`, `voter_character_id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Hero election votes cast per cycle/faction';
