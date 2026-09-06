USE aaemu_game;

-- Candidate and reward mails go out after the matching rows commit. A later tick must be able to
-- finish a send that was interrupted, without treating "row exists" as "mail delivered".
ALTER TABLE `hero_candidates`
  ADD COLUMN `candidate_mail_sent` tinyint(1) NOT NULL DEFAULT '0' AFTER `elected`,
  ADD COLUMN `reward_mail_sent` tinyint(1) NOT NULL DEFAULT '0' AFTER `candidate_mail_sent`;
