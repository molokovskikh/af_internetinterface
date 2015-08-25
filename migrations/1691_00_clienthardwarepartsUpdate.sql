use internet;
ALTER TABLE `inforoom2_clienthardwareparts` ADD COLUMN `NotGiven` TINYINT(4) UNSIGNED NULL DEFAULT '0' AFTER `Absent`;