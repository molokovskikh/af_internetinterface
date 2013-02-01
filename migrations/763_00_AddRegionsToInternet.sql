CREATE TABLE `Internet`.`Regions` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Region` VARCHAR(50),
  PRIMARY KEY(`Id`)
)
ENGINE = InnoDB;

ALTER TABLE `internet`.`houses` ADD COLUMN `RegionId` INTEGER UNSIGNED AFTER `CompetitorCount`,
 ADD CONSTRAINT `FK_houses_region` FOREIGN KEY `FK_houses_region` (`RegionId`)
    REFERENCES `regions` (`Id`)
    ON DELETE SET NULL
    ON UPDATE RESTRICT;


ALTER TABLE `internet`.`lawyerperson` MODIFY COLUMN `Id` INTEGER UNSIGNED NOT NULL,
 ADD COLUMN `RegionId` INTEGER UNSIGNED AFTER `MailingAddress`,
 ADD CONSTRAINT `FK_lawyerperson_regios` FOREIGN KEY `FK_lawyerperson_regios` (`RegionId`)
    REFERENCES `regions` (`Id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT;
