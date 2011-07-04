ALTER TABLE `internet`.`appeals` ADD COLUMN `AppealType` INT(10) UNSIGNED AFTER `Client`;


update internet.appeals a set a.AppealType = 1;