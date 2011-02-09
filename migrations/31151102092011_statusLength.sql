ALTER TABLE `internet`.`Status` MODIFY COLUMN `Name` VARCHAR(100) CHARACTER SET cp1251 COLLATE cp1251_general_ci NOT NULL;

update internet.`Status` S set
s.Name = "Зарегистрирован, но нет информации о подключении"
where id = 1;