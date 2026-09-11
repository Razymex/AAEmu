-- Mail retention: per-side logical deletion.
-- Receiver side: deleted by the player, or expired per MailRetentionRules.
-- Sender side:   deleted by the player, or expired 30 days after send.
-- A row is physically removed only once BOTH sides are gone.

ALTER TABLE `mails`
  ADD COLUMN `sender_deleted`   TINYINT(1) NOT NULL DEFAULT 0 AFTER `received_date`,
  ADD COLUMN `receiver_deleted` TINYINT(1) NOT NULL DEFAULT 0 AFTER `sender_deleted`;
