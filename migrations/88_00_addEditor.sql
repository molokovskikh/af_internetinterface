CREATE TABLE `internet`.`IVRNContent` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Content` LONGTEXT NOT NULL,
  `ViewName` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;


insert into internet.Clients (disabled, Name) values
(false, "TEST");

update internet.ClientEndpoints C set
c.Client = 127
where c.Client is null;