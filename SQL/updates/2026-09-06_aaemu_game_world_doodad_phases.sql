USE aaemu_game;

-- Phase and progress of permanent world fixtures (doodad_almighties.system_doodad, e.g. the faction
-- statues). They are spawned from the level's doodad_spawns and do not go through the `doodads`
-- table, so their construction state is kept here, keyed by template and rounded spawn position.
CREATE TABLE IF NOT EXISTS `world_doodad_phases` (
  `template_id` INT unsigned NOT NULL,
  `x` INT NOT NULL,
  `y` INT NOT NULL,
  `func_group_id` INT unsigned NOT NULL,
  `data` INT NOT NULL DEFAULT '0',
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`template_id`, `x`, `y`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
