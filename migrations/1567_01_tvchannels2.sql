USE internet;
CREATE TABLE `inforoom2_tvprotocols` (
	`Id` INT(11) NOT NULL AUTO_INCREMENT,
	`Name` VARCHAR(20) NOT NULL,
	PRIMARY KEY (`Id`),
	UNIQUE INDEX `Name` (`Name`)
)
ENGINE=InnoDB;
ALTER TABLE `tariffs`
	CHANGE COLUMN `Id` `Id` INT(11) UNSIGNED NOT NULL AUTO_INCREMENT FIRST;
ALTER TABLE `inforoom2_tvchannels`
	ALTER `Protocol` DROP DEFAULT;
ALTER TABLE `inforoom2_tvchannels`
	CHANGE COLUMN `Protocol` `TvProtocol` INT(11) NOT NULL AFTER `Name`;
ALTER TABLE `inforoom2_tvchannels`
	ADD UNIQUE INDEX `Name` (`Name`),
	ADD	CONSTRAINT `Protocol` FOREIGN KEY (`TvProtocol`) REFERENCES `inforoom2_tvprotocols` (`Id`);
	
ALTER TABLE `inforoom2_tvchannelgroups`
	ADD UNIQUE INDEX `Name` (`Name`);

ALTER TABLE `inforoom2_plantvchannelgroups`
	ALTER `Plan` DROP DEFAULT,
	ALTER `TvChannelGroup` DROP DEFAULT;
ALTER TABLE `inforoom2_plantvchannelgroups`
	CHANGE COLUMN `Plan` `Plan` INT(11) UNSIGNED NOT NULL AFTER `Id`,
	CHANGE COLUMN `TvChannelGroup` `TvChannelGroup` INT(11) NOT NULL AFTER `Plan`;

ALTER TABLE `inforoom2_plantvchannelgroups`
	ADD CONSTRAINT `Plan` FOREIGN KEY (`Plan`) REFERENCES `tariffs` (`Id`),
	ADD CONSTRAINT `TvChannelGroup` FOREIGN KEY (`TvChannelGroup`) REFERENCES `inforoom2_tvchannelgroups` (`Id`);

ALTER TABLE `inforoom2_tvchanneltvchannelgroups`
	ALTER `TvChannel` DROP DEFAULT,
	ALTER `TvChannelGroup` DROP DEFAULT;
ALTER TABLE `inforoom2_tvchanneltvchannelgroups`
	CHANGE COLUMN `TvChannel` `TvChannel` INT(11) NOT NULL AFTER `Id`,
	CHANGE COLUMN `TvChannelGroup` `TvChannelGroup` INT(11) NOT NULL AFTER `TvChannel`;
ALTER TABLE `inforoom2_tvchanneltvchannelgroups`
	ADD CONSTRAINT `TvChannel` FOREIGN KEY (`TvChannel`) REFERENCES `inforoom2_tvchannels` (`Id`),
	ADD CONSTRAINT `TvChannelGroup2` FOREIGN KEY (`TvChannelGroup`) REFERENCES `inforoom2_tvchannelgroups` (`Id`);
