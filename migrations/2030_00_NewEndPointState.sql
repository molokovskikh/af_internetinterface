
 use internet;
ALTER TABLE `orders`
	ADD COLUMN `NewEndPointState` TEXT NULL DEFAULT NULL AFTER `ConnectionAddress`;