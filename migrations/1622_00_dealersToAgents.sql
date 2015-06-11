USE internet;  
RENAME TABLE `inforoom2_dealers` TO `inforoom2_agents`;
ALTER TABLE `clients`
	ADD COLUMN `Agent` INT(10) UNSIGNED NULL DEFAULT NULL AFTER `Dealer`;