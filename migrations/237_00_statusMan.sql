ALTER TABLE internet.`status`  ADD COLUMN `ManualSet` TINYINT(1) UNSIGNED NOT NULL AFTER `ShortName`;

UPDATE internet.`status` SET `ManualSet`=1 WHERE `Id`=5 LIMIT 1;
UPDATE internet.`status` SET `ManualSet`=1 WHERE `Id`=7 LIMIT 1;