ALTER TABLE `internet`.`physicalclients` ADD COLUMN `DateOfBirth` DATETIME AFTER `HouseObj`;

ALTER TABLE `internet`.`lawyerperson` ADD COLUMN `ContactPerson` VARCHAR(45) AFTER `RegDate`;

ALTER TABLE `internet`.`Houses` MODIFY COLUMN `CaseHouse` VARCHAR(5) DEFAULT NULL;