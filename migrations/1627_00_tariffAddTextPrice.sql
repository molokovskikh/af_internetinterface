USE internet;
ALTER TABLE `tariffs` ADD COLUMN `TextPrice` VARCHAR(50) NULL AFTER `Price`;