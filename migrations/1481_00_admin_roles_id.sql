use internet;
ALTER TABLE `inforoom2_user_role`
	ADD COLUMN `id` INT NOT NULL AUTO_INCREMENT AFTER `permission`,
	ADD PRIMARY KEY (`id`);
