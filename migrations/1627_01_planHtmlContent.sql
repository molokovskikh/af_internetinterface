USE internet;
CREATE TABLE `inforoom2_planhtmlcontent` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Region` INT(11) UNSIGNED NULL DEFAULT NULL,
	`Content` TEXT NOT NULL,
	PRIMARY KEY (`Id`),
	INDEX `FK_inforoom2_planhtml_region` (`Region`),
	CONSTRAINT `FK_inforoom2_planhtml_region` FOREIGN KEY (`Region`) REFERENCES `regions` (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB
;