USE aaemu_game;

-- Hero-board today-quest completions per (character, today_quest_steps.id) for the current term.
-- hero_bonus_today_assignments gives the count each step needs for the Hero's bonus tier. Cleared when
-- the character takes a new seat.
CREATE TABLE IF NOT EXISTS `character_hero_bonus_progress` (
  `character_id` INT unsigned NOT NULL,
  `today_quest_step_id` INT unsigned NOT NULL,
  `count` INT unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`character_id`, `today_quest_step_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
