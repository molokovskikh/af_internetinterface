USE internet;
ALTER TABLE `InternetSettings`
	ADD COLUMN `NextDataAuditDate` DATETIME NULL DEFAULT NULL AFTER `NextSmsSendDate`;
