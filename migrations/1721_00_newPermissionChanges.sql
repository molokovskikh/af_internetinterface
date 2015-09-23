use internet;

CREATE TABLE `inforoom2_permissiontouser` (
	`user` INT(10) UNSIGNED NULL DEFAULT NULL,
	`permission` INT(11) NULL DEFAULT NULL,
	INDEX `user` (`user`),
	INDEX `permission` (`permission`),
	CONSTRAINT `FK_inforoom2_permissiontouser_inforoom2_permissions` FOREIGN KEY (`permission`) REFERENCES `inforoom2_permissions` (`Id`),
	CONSTRAINT `FK_inforoom2_permissiontouser_partners` FOREIGN KEY (`user`) REFERENCES `partners` (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB
;
CREATE TABLE `inforoom2_roletouser` (
	`user` INT(10) UNSIGNED NULL DEFAULT NULL,
	`role` INT(11) NULL DEFAULT NULL,
	INDEX `role` (`role`),
	INDEX `user` (`user`),
	CONSTRAINT `FK_inforoom2_roletouser_inforoom2_roles` FOREIGN KEY (`role`) REFERENCES `inforoom2_roles` (`Id`),
	CONSTRAINT `FK_inforoom2_roletouser_partners` FOREIGN KEY (`user`) REFERENCES `partners` (`Id`)
)
COLLATE='cp1251_general_ci'
ENGINE=InnoDB
;


INSERT INTO internet.inforoom2_roletouser (user,role) SELECT ur.user, ur.role FROM internet.inforoom2_user_role  as ur