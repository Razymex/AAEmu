USE aaemu_game;

-- The first mail_sent update on already-applied DBs used DEFAULT 0. Treat those existing rows as
-- already delivered so a later tick does not replay item-bearing election mail.
UPDATE `hero_candidates`
   SET `candidate_mail_sent` = 1, `reward_mail_sent` = 1;
