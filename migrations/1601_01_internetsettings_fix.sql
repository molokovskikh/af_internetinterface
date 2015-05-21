USE internet;
ALTER TABLE `InternetSettings`
	CHANGE COLUMN `NextAuditDate` `NextDataAuditDate` DATETIME NULL DEFAULT NULL AFTER `NextSmsSendDate`;
