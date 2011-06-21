ALTER TABLE `internet`.`physicalclients` ADD COLUMN `DateOfBirth` DATETIME AFTER `HouseObj`;

ALTER TABLE `internet`.`lawyerperson` ADD COLUMN `ContactPerson` VARCHAR(45) AFTER `RegDate`;
