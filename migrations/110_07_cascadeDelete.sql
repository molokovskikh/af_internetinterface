ALTER TABLE `internet`.`WriteOff`
 DROP FOREIGN KEY `ClientRelation`;

ALTER TABLE `internet`.`WriteOff` ADD CONSTRAINT `ClientRelation` FOREIGN KEY `ClientRelation` (`Client`)
    REFERENCES `Clients` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE;
