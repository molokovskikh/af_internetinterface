CREATE TABLE `internet`.`RequestMessages` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Comment` TEXT NOT NULL,
  `Date` DATETIME NOT NULL,
  `Registrator` INT(10) UNSIGNED,
  `Request` INT(10) UNSIGNED,
  PRIMARY KEY (`Id`),
  CONSTRAINT `FK_RequestMessages_1` FOREIGN KEY `FK_RequestMessages_1` (`Registrator`)
    REFERENCES `Partners` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE,
  CONSTRAINT `FK_RequestMessages_2` FOREIGN KEY `FK_RequestMessages_2` (`Request`)
    REFERENCES `Requests` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE
)
ENGINE = InnoDB;

update internet.labels set ShortComment = "Deleted", Deleted = false
where id = 16;

update internet.Requests r
set r.Archive = true
where r.`label` in (16,21,23 );