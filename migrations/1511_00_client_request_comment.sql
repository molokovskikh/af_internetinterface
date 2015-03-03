use internet;
ALTER TABLE `requests`
	ADD COLUMN `_comment` TEXT NULL AFTER `_RequestSource`;