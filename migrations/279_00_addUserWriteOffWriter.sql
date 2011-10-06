ALTER TABLE `internet`.`UserWriteOffs` ADD COLUMN `Registrator` INT(10) UNSIGNED AFTER `Comment`,
 ADD CONSTRAINT `FK_UserWriteOffs_2` FOREIGN KEY `FK_UserWriteOffs_2` (`Registrator`)
    REFERENCES `partners` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;
