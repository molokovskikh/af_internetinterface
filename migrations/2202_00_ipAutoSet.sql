
use internet;
ALTER TABLE `clientendpoints`
	ADD COLUMN `IpAutoSet` TINYINT(1) UNSIGNED NULL DEFAULT NULL AFTER `StableTariffPackageId`;