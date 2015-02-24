CREATE TABLE internet.`inforoom2_paymentsystems` (
	`Id` INT(10) NOT NULL AUTO_INCREMENT,
	`Employee` INT(10) UNSIGNED NULL DEFAULT NULL,
	PRIMARY KEY (`Id`),
	INDEX `FK_PaymentSystems` (`Employee`),
	CONSTRAINT `Employee_Key` FOREIGN KEY (`Employee`) REFERENCES `partners` (`Id`) ON UPDATE CASCADE
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB;
