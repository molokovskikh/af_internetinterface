use internet;
ALTER TABLE `tariffs` ADD COLUMN `IgnoreDiscount` TINYINT(1) NOT NULL DEFAULT '0' AFTER `CanUseForSelfRegistration`;
