USE Internet;
ALTER TABLE `clients`
	ADD COLUMN `Dealer` INT(10) UNSIGNED NULL DEFAULT NULL AFTER `StatusChangedOn`;
