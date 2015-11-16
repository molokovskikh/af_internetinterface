use internet;
ALTER TABLE `orderservices`
	CHANGE COLUMN `Cost` `Cost` DECIMAL(10,2) NOT NULL DEFAULT '0.00' AFTER `Description`;