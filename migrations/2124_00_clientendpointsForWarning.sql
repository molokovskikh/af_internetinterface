use internet;
ALTER TABLE `clientendpoints`
	ADD COLUMN `WarningShow` TINYINT(1) UNSIGNED NOT NULL DEFAULT '1' AFTER `Mac`;
ALTER TABLE `clientendpoints`
	ADD COLUMN `StableTariffPackageId` INT(11) NULL DEFAULT NULL AFTER `WarningShow`;
	
UPDATE clientendpoints as ce
SET StableTariffPackageId=ce.PackageId;	

UPDATE clientendpoints as ce
SET PackageId=10 WHERE ce.`Client` in (SELECT Id FROM clients as c WHERE c.ShowBalanceWarningPage = 1);

UPDATE clients as cl
SET cl.ShowBalanceWarningPage = 0;

UPDATE `internet`.`clientendpoints` SET `Disabled`=1 , `Port`= null, `Switch`=null   WHERE  `Id` = 5276;
UPDATE `internet`.`clientendpoints` SET `Disabled`=1 , `Port`= null, `Switch`=null   WHERE  `Id` = 6421;
UPDATE `internet`.`clientendpoints` SET `Disabled`=1 , `Port`= null, `Switch`=null   WHERE  `Id` = 247;
UPDATE `internet`.`clientendpoints` SET `Disabled`=1 , `Port`= null, `Switch`=null   WHERE  `Id` = 1837;
UPDATE `internet`.`clientendpoints` SET `Disabled`=1 , `Port`= null, `Switch`=null   WHERE  `Id` = 2509;
UPDATE `internet`.`clientendpoints` SET `Disabled`=1 , `Port`= null, `Switch`=null   WHERE  `Id` = 7545;
UPDATE `internet`.`clientendpoints` SET `Disabled`=1 , `Port`= null, `Switch`=null   WHERE  `Id` = 21155;
UPDATE `internet`.`clientendpoints` SET `Disabled`=1 , `Port`= null, `Switch`=null   WHERE  `Id` = 7497;