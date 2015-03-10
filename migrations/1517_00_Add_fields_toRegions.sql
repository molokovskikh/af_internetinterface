use Internet;
ALTER TABLE `regions`
	ADD COLUMN `_OfficeAddress` VARCHAR(255) NULL DEFAULT NULL AFTER `_RegionOfficePhoneNumber`,
	ADD COLUMN `_OfficeGeomark` VARCHAR(50) NULL DEFAULT NULL AFTER `_OfficeAddress`;
