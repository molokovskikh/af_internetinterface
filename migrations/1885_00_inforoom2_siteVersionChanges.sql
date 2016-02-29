use internet;
CREATE TABLE `inforoom2_siteVersionChanges` (
	`Id` INT NOT NULL AUTO_INCREMENT,
	`Version` VARCHAR(50) NOT NULL DEFAULT '0',
	`Changes` TEXT NOT NULL,
	PRIMARY KEY (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB
;

