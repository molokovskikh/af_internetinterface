ALTER TABLE `internet`.`SubMenuField`
 DROP FOREIGN KEY `FK_SubMenuField_1`;

ALTER TABLE `internet`.`SubMenuField` ADD CONSTRAINT `FK_SubMenuField_1` FOREIGN KEY `FK_SubMenuField_1` (`MenuField`)
    REFERENCES `MenuField` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE;
