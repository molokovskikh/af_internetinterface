use internet;

ALTER TABLE `requests`	ADD COLUMN `Hybrid` TINYINT(1) UNSIGNED NOT NULL DEFAULT '0' AFTER `YandexHouse`;

ALTER TABLE `partners`	ADD COLUMN `SessionDurationMinutes` INT UNSIGNED NOT NULL DEFAULT '0' AFTER `ShowContractOfAgency`;
