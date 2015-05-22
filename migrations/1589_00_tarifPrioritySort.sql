use internet;
ALTER TABLE `tariffs` ADD COLUMN `Priority` INT(11) NOT NULL DEFAULT '0' AFTER `Published`;
ALTER TABLE `requests`	ADD COLUMN `YandexStreet` VARCHAR(255) NULL AFTER `_comment`, ADD COLUMN `YandexHouse` VARCHAR(255) NULL AFTER `YandexStreet`;