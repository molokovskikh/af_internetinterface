use internet;
ALTER TABLE `payments`
	CHANGE COLUMN `BillingAccount` `BillingAccount` TINYINT(1) UNSIGNED NOT NULL DEFAULT '0' AFTER `Agent`;