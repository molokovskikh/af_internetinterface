use internet;
CREATE TABLE `inforoom2_EmployeeGroup` (
	`Id` INT(11) UNSIGNED NOT NULL AUTO_INCREMENT,
	`Name` VARCHAR(255) NOT NULL,
	PRIMARY KEY (`Id`)
);

CREATE TABLE `inforoom2_employeetogroup` (
	`GroupId` INT(11) UNSIGNED NOT NULL,
	`EmployeeId` INT(11) UNSIGNED NOT NULL,
	PRIMARY KEY (`GroupId`, `EmployeeId`),
	INDEX `FK_inforoom2_employeetogroup_internet.partners` (`EmployeeId`),
	CONSTRAINT `FK_inforoom2_employeetogroup_internet.partners` FOREIGN KEY (`EmployeeId`) REFERENCES `partners` (`Id`),
	CONSTRAINT `FK_inforoom2_employeetogroup_internet.inforoom2_employeegroup` FOREIGN KEY (`GroupId`) REFERENCES `inforoom2_employeegroup` (`Id`)
);
