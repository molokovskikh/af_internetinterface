USE internet;

ALTER TABLE `inforoom2_tvchannels`
	ALTER `Port` DROP DEFAULT;
ALTER TABLE `inforoom2_tvchannels`
	CHANGE COLUMN `Port` `Port` INT(11) NULL AFTER `Url`;
