ALTER TABLE internet.`inforoom2_user_role`
	DROP FOREIGN KEY `FK98962F1734E2BA77`;
ALTER TABLE internet.`inforoom2_user_role`
	CHANGE COLUMN `user` `user` INT(10) UNSIGNED NULL DEFAULT NULL FIRST;
ALTER TABLE internet.`inforoom2_user_role`
	ADD CONSTRAINT `FK98962F1734E2BA77` FOREIGN KEY (`user`) REFERENCES internet.`partners` (`Id`);
	
	
	ALTER TABLE internet.`inforoom2_newsblock`
	DROP FOREIGN KEY `FK2E9848996583FF54`;
ALTER TABLE internet.`inforoom2_newsblock`
	CHANGE COLUMN `employee` `User` INT(10) UNSIGNED NULL DEFAULT NULL FIRST;
ALTER TABLE internet.`inforoom2_newsblock`
	ADD CONSTRAINT `FK2E9848896583FF54` FOREIGN KEY (`User`) REFERENCES internet.`partners` (`Id`);
	
	ALTER TABLE internet.`inforoom2_tickets`
	DROP FOREIGN KEY `FK962FBD596583FF54`;
ALTER TABLE internet.`inforoom2_tickets`
	CHANGE COLUMN `employee` `User` INT(10) UNSIGNED NULL DEFAULT NULL FIRST;
ALTER TABLE internet.`inforoom2_tickets`
	ADD CONSTRAINT `FK962FBD506583FF54` FOREIGN KEY (`User`) REFERENCES internet.`partners` (`Id`);
