USE aaemu_game;

-- Quest cinema-end effects that are still owed to the character.
-- A component that defers its buff or teleport until the film ends must survive a
-- dropped connection: the client never reports the end and the quest step is already
-- saved, so the row is replayed on the next login and cleared once applied.

CREATE TABLE IF NOT EXISTS `character_quest_cinema_end_effects` (
  `owner` int unsigned NOT NULL,
  `quest_id` int unsigned NOT NULL,
  `cinema_id` int unsigned NOT NULL,
  `component_id` int unsigned NOT NULL,
  PRIMARY KEY (`owner`, `component_id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='Quest cinema-end effects still owed to the character';
