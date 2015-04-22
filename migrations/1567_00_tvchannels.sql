USE internet;

CREATE TABLE `inforoom2_tvchannels` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Name` VARCHAR(250) NOT NULL,
	`Protocol` INT(11) NOT NULL,
	`Url` VARCHAR(250) NOT NULL,
	`Port` INT(11) NOT NULL,
	PRIMARY KEY (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB
AUTO_INCREMENT=1;

CREATE TABLE `inforoom2_tvchannelgroups` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Name` VARCHAR(250) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB
AUTO_INCREMENT=1;

CREATE TABLE `inforoom2_tvchanneltvchannelgroups` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`TvChannel` INT(11) NULL DEFAULT NULL,
	`TvChannelGroup` INT(11) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB
AUTO_INCREMENT=1;

CREATE TABLE `inforoom2_plantvchannelgroups` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Plan` INT(11) NULL DEFAULT NULL,
	`TvChannelGroup` INT(11) NULL DEFAULT NULL,
	PRIMARY KEY (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB
AUTO_INCREMENT=1;