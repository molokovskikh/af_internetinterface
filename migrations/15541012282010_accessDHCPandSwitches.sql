INSERT INTO `internet`.`AccessCategories` (`Name`, `ReduceName`) VALUES ('Просмотр заявок на подключение', 'VD');

INSERT INTO `internet`.`AccessCategories` (`Name`, `ReduceName`) VALUES ('Право доступа к DHCP', 'DHCP');

CREATE TABLE `internet`.`PackageSpeed` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `PackageId` INT(10) UNSIGNED NOT NULL,
  `Speed` INT(10) UNSIGNED NOT NULL,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;
