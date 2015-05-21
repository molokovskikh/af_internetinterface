USE internet;
ALTER TABLE `InternetSettings`
	ADD COLUMN `NextAuditDate` DATETIME NULL DEFAULT NULL AFTER `NextSmsSendDate`;
