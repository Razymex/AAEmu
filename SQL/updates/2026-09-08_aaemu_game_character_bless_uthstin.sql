USE aaemu_game;

-- Bless Uthstin pages. Header is one row per character; pages are 0-based.

CREATE TABLE IF NOT EXISTS `character_bless_uthstin` (
  `owner` int unsigned NOT NULL,
  `select_page_index` int NOT NULL DEFAULT 0,
  `extend_max_stats` int NOT NULL DEFAULT 0,
  `apply_extend_count` int NOT NULL DEFAULT 0,
  PRIMARY KEY (`owner`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='Bless Uthstin header (selected page and cap)';

CREATE TABLE IF NOT EXISTS `character_bless_uthstin_pages` (
  `owner` int unsigned NOT NULL,
  `page_index` tinyint unsigned NOT NULL,
  `str` int NOT NULL DEFAULT 0,
  `dex` int NOT NULL DEFAULT 0,
  `sta` int NOT NULL DEFAULT 0,
  `int` int NOT NULL DEFAULT 0,
  `spi` int NOT NULL DEFAULT 0,
  `apply_normal` int NOT NULL DEFAULT 0,
  `apply_special` int NOT NULL DEFAULT 0,
  PRIMARY KEY (`owner`, `page_index`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='Bless Uthstin applied stats per page';
