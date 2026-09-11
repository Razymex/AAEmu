-- 10.x public-listing flag for house sales (sale dialog checkbox). Only public
-- sales feed the townhall property listing. Defaults to public, matching the
-- client's pre-checked box.

ALTER TABLE `housings`
	ADD COLUMN `sell_public` TINYINT UNSIGNED NOT NULL DEFAULT '1' AFTER `allow_recover`;
