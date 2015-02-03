use internet;
ALTER TABLE `appeals`
	ADD COLUMN `_inforoom2` TINYINT NOT NULL DEFAULT '0' AFTER `AppealType`;
