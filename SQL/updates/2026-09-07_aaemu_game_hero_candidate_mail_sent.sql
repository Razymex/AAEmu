USE aaemu_game;

-- Delivery flags for candidate / reward mail. Default 1 so an upgrade does not replay mail for
-- rows that already existed. New inserts set both columns to 0.
ALTER TABLE `hero_candidates`
  ADD COLUMN `candidate_mail_sent` tinyint(1) NOT NULL DEFAULT '1' AFTER `elected`,
  ADD COLUMN `reward_mail_sent` tinyint(1) NOT NULL DEFAULT '1' AFTER `candidate_mail_sent`;
