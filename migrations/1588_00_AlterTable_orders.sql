USE internet;
ALTER TABLE `orders`
	ADD COLUMN `ConnectionAddress` VARCHAR(255) NULL DEFAULT NULL AFTER `IsDeactivated`;
