ALTER TABLE `internet`.`Clients` MODIFY COLUMN `ShowBalanceWarningPage` TINYINT(1) UNSIGNED NOT NULL DEFAULT 0,
 ADD COLUMN `SayDhcpIsNewClient` TINYINT(1) UNSIGNED NOT NULL AFTER `ShowBalanceWarningPage`,
 ADD COLUMN `SayBillingIsNewClient` TINYINT(1) UNSIGNED NOT NULL AFTER `SayDhcpIsNewClient`;
