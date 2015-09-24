use internet;

ALTER TABLE `serviceiterations`
	ADD COLUMN `_inforoom2` TINYINT(4) NOT NULL DEFAULT '0' AFTER `Request`;