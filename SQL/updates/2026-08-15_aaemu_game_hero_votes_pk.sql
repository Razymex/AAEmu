USE aaemu_game;

-- hero_votes' key was missing candidate_character_id - a multi-select ballot's per-candidate REPLACE INTO
-- loop silently overwrote all but the last pick. Safe to run even with existing rows: at most one row per
-- (cycle_id, faction_id, voter_character_id) exists today (the old key), so no duplicates to resolve first.
ALTER TABLE `hero_votes`
  DROP PRIMARY KEY,
  ADD PRIMARY KEY (`cycle_id`, `faction_id`, `voter_character_id`, `candidate_character_id`) USING BTREE;
