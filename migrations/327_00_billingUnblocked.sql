ALTER TABLE `internet`.`Clients` ADD COLUMN `PercentBalance` decimal(3,3) NOT NULL AFTER `DebtWork`;
update internet.CLients set PercentBalance = 0.8;