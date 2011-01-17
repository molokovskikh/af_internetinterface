ALTER TABLE `internet`.`Status` ADD COLUMN `Blocked` TINYINT(1) UNSIGNED NOT NULL AFTER `Name`;

update internet.`Status` S set s.Blocked = false where s.id = 1;
update internet.`Status` S set s.Blocked = true where s.id = 3;
update internet.`Status` S set s.Blocked = true where s.id = 5;