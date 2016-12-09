use internet;
CREATE TABLE `inforoom2_connectedstreets` (
	`Id` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
	`Region` INT(10) UNSIGNED NULL DEFAULT NULL,
	`AddressStreet` INT(11) NULL DEFAULT NULL,
	`Name` VARCHAR(255) NOT NULL,
	`Disabled` TINYINT(1) NOT NULL DEFAULT '0',
	PRIMARY KEY (`Id`),
	INDEX `FK_inforoom2_connectedstreets_regions` (`Region`),
	INDEX `FK_inforoom2_connectedstreets_inforoom2_street` (`AddressStreet`),
	CONSTRAINT `FK_inforoom2_connectedstreets_inforoom2_street` FOREIGN KEY (`AddressStreet`) REFERENCES `inforoom2_street` (`Id`) ON DELETE SET NULL,
	CONSTRAINT `FK_inforoom2_connectedstreets_regions` FOREIGN KEY (`Region`) REFERENCES `regions` (`Id`) ON DELETE CASCADE
);


ALTER TABLE `inforoom2_connectedhouses`
	DROP FOREIGN KEY `FK_inforoom2_connectedhouses_inforoom2_street`;
	
ALTER TABLE `inforoom2_connectedhouses`
	DROP INDEX `FK_inforoom2_connectedhouses_inforoom2_street`;
	
ALTER TABLE `inforoom2_connectedhouses`
	CHANGE COLUMN `Street` `_Street` INT(11) NULL DEFAULT NULL AFTER `Id`,
	ADD COLUMN `Street` INT(11) UNSIGNED NULL DEFAULT NULL AFTER `_Street`;
	
	
ALTER TABLE `inforoom2_connectedhouses`
	DROP FOREIGN KEY `FK_inforoom2_connectedhouses_regions`;
	
ALTER TABLE `inforoom2_connectedhouses`
	ADD CONSTRAINT `FK_inforoom2_connectedhouses_regions` FOREIGN KEY (`Region`) REFERENCES `regions` (`Id`) ON DELETE RESTRICT,
	ADD CONSTRAINT `FK_inforoom2_connectedhouses_inforoom2_connectedstreets` FOREIGN KEY (`Street`) REFERENCES `inforoom2_connectedstreets` (`Id`) ON DELETE RESTRICT;

	
INSERT INTO internet.inforoom2_connectedstreets (Region,AddressStreet,Name)	(
	Select Region, AddressStreet, Name FROM	(
		Select * FROM internet.inforoom2_connectedhouses as sh
		INNER JOIN	(		
		SELECT Id as AddressStreet, IF(st.Alias IS NULL or st.Alias = '', TRIM(st.Name), TRIM(st.Alias)
		) as Name
		FROM	internet.inforoom2_street as st 
	) as sta ON sta.AddressStreet = sh._Street
	GROUP BY AddressStreet
) as st2 );

Update internet.inforoom2_connectedhouses AS updHouse
INNER JOIN	 internet.inforoom2_connectedstreets  as resStreet
ON updHouse._Street = resStreet.AddressStreet
SET updHouse.Street = resStreet.Id;

ALTER TABLE `inforoom2_connectedhouses`	DROP COLUMN `_Street`;