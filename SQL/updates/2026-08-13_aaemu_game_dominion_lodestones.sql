USE aaemu_game;

-- Seeds the 12 unclaimed "Archeum Lodestone" Guard Tower houses (owner=0/account_id=0) that
-- SQL/aaemu_game.sql already lists for a fresh install (template ids 139, 184-192, 271, 272 - same
-- content as the 1.2 develop branch, confirmed identical in this build's client SQLite). This live
-- database was provisioned before that seed block existed/ran, so it never got these rows - which is
-- why there was nothing in Nuimari/Salpimari etc. to target with the "purify while holding Purifying
-- Archeum" declare-dominion skill (13661). HousingManager.Load() has no owner filter, so these load
-- and spawn like any other house once present; DeclareDominion.cs already handles `target as House`.
-- Live nation lodestones (removed false in worlds/main_world/houses/{zoneKey}/house.g)
-- take XYZ/yaw from that file at HousingManager load. Seed rows below are bootstrap ids only.
-- current_step=0 (never built) makes House.AllowedToInteract() ignore Permission entirely, so any
-- player can target them regardless of the placeholder `permission` value below.
-- protected_until is pushed to 2099 (not 1.2's out-of-range 0001-01-01/2043-03-03 sentinels, which
-- don't fit MySQL's DATETIME bounds) purely so no tax/expiry sweep ever mistakes an owner=0 row for
-- an overdue one.
INSERT IGNORE INTO `housings`
  (`id`, `account_id`, `owner`, `co_owner`, `template_id`, `name`, `x`, `y`, `z`,
   `yaw`, `pitch`, `roll`, `current_step`, `current_action`, `permission`, `place_date`, `protected_until`,
   `faction_id`, `sell_to`, `sell_price`, `allow_recover`)
VALUES
  (1,  0, 0, 0, 139, 'Archeum Lodestone', 19643.0,  24385.4, 168.9, 0, 0, 0, 0, 0, 2, '2000-01-01 00:00:00', '2099-01-01 00:00:00', 1, 0, 0, 1),
  (2,  0, 0, 0, 184, 'Archeum Lodestone', 19950.092,  24270.228, 142.126, 0, 0, -0.460, 0, 0, 2, '2000-01-01 00:00:00', '2099-01-01 00:00:00', 1, 0, 0, 1),
  (3,  0, 0, 0, 185, 'Archeum Lodestone', 20379.4,  24126.2, 123.6, 0, 0, 0, 0, 0, 2, '2000-01-01 00:00:00', '2099-01-01 00:00:00', 1, 0, 0, 1),
  (4,  0, 0, 0, 186, 'Archeum Lodestone', 21235.7,  23918.5, 165.0, 0, 0, 0, 0, 0, 2, '2000-01-01 00:00:00', '2099-01-01 00:00:00', 1, 0, 0, 1),
  (5,  0, 0, 0, 187, 'Archeum Lodestone', 21449.961, 24210.300, 154.376, 0, 0, -0.205, 0, 0, 2, '2000-01-01 00:00:00', '2099-01-01 00:00:00', 1, 0, 0, 1),
  (6,  0, 0, 0, 188, 'Archeum Lodestone', 22048.2,  24241.1, 154.8, 0, 0, 0, 0, 0, 2, '2000-01-01 00:00:00', '2099-01-01 00:00:00', 1, 0, 0, 1),
  (7,  0, 0, 0, 189, 'Archeum Lodestone', 19649.921,  25089.972, 164.626, 0, 0, 0, 0, 0, 2, '2000-01-01 00:00:00', '2099-01-01 00:00:00', 1, 0, 0, 1),
  (8,  0, 0, 0, 190, 'Archeum Lodestone', 20325.6,  25174.6, 172.9, 0, 0, 0, 0, 0, 2, '2000-01-01 00:00:00', '2099-01-01 00:00:00', 1, 0, 0, 1),
  (9,  0, 0, 0, 191, 'Archeum Lodestone', 20890.8,  25238.5, 193.7, 0, 0, 0, 0, 0, 2, '2000-01-01 00:00:00', '2099-01-01 00:00:00', 1, 0, 0, 1),
  (10, 0, 0, 0, 192, 'Archeum Lodestone', 21949.933,  24890.044, 206.315, 0, 0, 0.870, 0, 0, 2, '2000-01-01 00:00:00', '2099-01-01 00:00:00', 1, 0, 0, 1),
  (11, 0, 0, 0, 271, 'Archeum Lodestone', 23060.8,  25148.3, 142.0, 0, 0, 0, 0, 0, 2, '2000-01-01 00:00:00', '2099-01-01 00:00:00', 1, 0, 0, 1),
  (12, 0, 0, 0, 272, 'Archeum Lodestone', 21800.3,  26893.9, 137.7, 0, 0, 0, 0, 0, 2, '2000-01-01 00:00:00', '2099-01-01 00:00:00', 1, 0, 0, 1);
