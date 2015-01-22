USE Internet;
ALTER TABLE `requests`
	ADD COLUMN `_RequestSource` INT(10) UNSIGNED NOT NULL DEFAULT '1' AFTER `_Serviceman`;