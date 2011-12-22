ALTER TABLE `internet`.`clients` ADD COLUMN `PaidDay` TINYINT(1) UNSIGNED NOT NULL AFTER `PercentBalance`,
 ADD COLUMN `FreeBlockDays` INT(10) UNSIGNED NOT NULL AFTER `PaidDay`,
 ADD COLUMN `YearCycleDate` DATETIME AFTER `FreeBlockDays`;
