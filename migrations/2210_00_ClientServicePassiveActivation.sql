use internet;
ALTER TABLE `clientservices`
	ADD COLUMN `PassiveActivation` INT(1) UNSIGNED ZEROFILL NULL DEFAULT NULL AFTER `RentableHardware`;