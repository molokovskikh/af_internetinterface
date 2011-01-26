CREATE TABLE `internet`.`UserCategories` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Comment` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;

insert into internet.UserCategories set Commect = "Сторонний человек, осуществляющий функции регистрации";
insert into internet.UserCategories set Commect = "Сотрудник космании Inforoom";

ALTER TABLE `internet`.`PartnerAccessSet` RENAME TO `internet`.`CategoriesAccessSet`,
 CHANGE COLUMN `Partner` `Categorie` INT(10) UNSIGNED DEFAULT NULL;
 
 ALTER TABLE `internet`.`Partners` ADD COLUMN `Categorie` INT(10) UNSIGNED AFTER `Login`;
 
 ALTER TABLE `internet`.`Partners` ADD CONSTRAINT `FK_Partners_1` FOREIGN KEY `FK_Partners_1` (`Categorie`)
    REFERENCES `UserCategories` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;
	

ALTER TABLE `internet`.`UserCategories` ADD COLUMN `ReductionName` VARCHAR(45) NOT NULL AFTER `Commect`;


UPDATE `internet`.`UserCategories` SET `ReductionName`='Diller' WHERE `Id`='1';

UPDATE `internet`.`UserCategories` SET `ReductionName`='Office' WHERE `Id`='2';

UPDATE `internet`.`Status` SET `Name`='Зарегистрирован, но нет информации о подключении', `Blocked`='1' WHERE `Id`='1';

UPDATE `internet`.`Status` SET `Name`='Зарегистрирован, подключен но не работает' WHERE `Id`='3';

UPDATE `internet`.`Status` SET `Name`='Работает', `Blocked`='0' WHERE `Id`='5';

INSERT INTO `internet`.`Status` (`Name`, `Blocked`) VALUES ('Не работает', '1');

ALTER TABLE `internet`.`PhysicalClients` CHANGE COLUMN `Login` `Email` VARCHAR(255) CHARACTER SET cp1251 COLLATE cp1251_general_ci DEFAULT NULL;

Delete FROM `internet`.`CategoriesAccessSet`;

INSERT INTO `internet`.`UserCategories` (`CategorieType`, `Commect`, `ReductionName`) VALUES ('Диллер', 'Сторонний человек,осуществляющий функции регистрации и пополнения баланса', 'Diller');

INSERT INTO `internet`.`UserCategories` (`CategorieType`, `Commect`, `ReductionName`) VALUES ('Офисный сотрудник', 'Сотрудник космании Inforoom', 'Office');


INSERT INTO `internet`.`CategoriesAccessSet` (`Categorie`, `AccessCat`) VALUES ('1', '1');

INSERT INTO `internet`.`CategoriesAccessSet` (`Categorie`, `AccessCat`) VALUES ('1', '3');

INSERT INTO `internet`.`CategoriesAccessSet` (`Categorie`, `AccessCat`) VALUES ('1', '11');

INSERT INTO `internet`.`CategoriesAccessSet` (`Categorie`, `AccessCat`) VALUES ('2', '1');

INSERT INTO `internet`.`CategoriesAccessSet` (`Categorie`, `AccessCat`) VALUES ('2', '3');

INSERT INTO `internet`.`CategoriesAccessSet` (`Categorie`, `AccessCat`) VALUES ('2', '5');

INSERT INTO `internet`.`CategoriesAccessSet` (`Categorie`, `AccessCat`) VALUES ('2', '7');

INSERT INTO `internet`.`CategoriesAccessSet` (`Categorie`, `AccessCat`) VALUES ('2', '9');

INSERT INTO `internet`.`CategoriesAccessSet` (`Categorie`, `AccessCat`) VALUES ('2', '11');

INSERT INTO `internet`.`CategoriesAccessSet` (`Categorie`, `AccessCat`) VALUES ('2', '13');

INSERT INTO `internet`.`CategoriesAccessSet` (`Categorie`, `AccessCat`) VALUES ('2', '15');

INSERT INTO `internet`.`CategoriesAccessSet` (`Categorie`, `AccessCat`) VALUES ('2', '17');

INSERT INTO `internet`.`CategoriesAccessSet` (`Categorie`, `AccessCat`) VALUES ('2', '19');

INSERT INTO `internet`.`CategoriesAccessSet` (`Categorie`, `AccessCat`) VALUES ('2', '21');



UPDATE `internet`.`Partners` SET `Categorie`='2' WHERE `Id`='1';

UPDATE `internet`.`Partners` SET `Categorie`='2' WHERE `Id`='3';

UPDATE `internet`.`Partners` SET `Categorie`='2' WHERE `Id`='13';

UPDATE `internet`.`Partners` SET `Categorie`='2' WHERE `Id`='15';

UPDATE `internet`.`Partners` SET `Categorie`='2' WHERE `Id`='17';


ALTER TABLE `internet`.`PhysicalClients` MODIFY COLUMN `OutputDate` DATETIME DEFAULT NULL,
 ADD COLUMN `ConnectedDate` DATETIME AFTER `HasConnected`;

 ALTER TABLE `internet`.`Requests` ADD COLUMN `ActionDate` DATETIME AFTER `Floor`,
 ADD COLUMN `Operator` INT(10) AFTER `ActionDate`;
 
 ALTER TABLE `internet`.`Requests` ADD CONSTRAINT `FK_Requests_1` FOREIGN KEY `FK_Requests_1` (`Operator`)
    REFERENCES `Partners` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

	
	ALTER TABLE `internet`.`ConnectBrigads` DROP COLUMN `Adress`,
 DROP COLUMN `BrigadCount`,
 DROP COLUMN `PartnerID`;

 UPDATE `internet`.`AccessCategories` SET `Name`='Управление бригадами', `ReduceName`='MB' WHERE `Id`='5';

delete FROM internet.Partners where Partners.`Id` = 5; 

delete FROM internet.Partners where Partners.`Id` = 7; 

delete FROM internet.Partners where Partners.`Id` = 11; 

