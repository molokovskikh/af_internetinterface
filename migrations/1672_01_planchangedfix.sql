use internet;
ALTER TABLE `physicalclients`
	CHANGE COLUMN `_LastTimePlanChanged` `_LastTimePlanChanged` DATETIME NULL DEFAULT NULL AFTER `Tariff`;
