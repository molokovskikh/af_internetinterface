use internet;
ALTER TABLE `inforoom2_callmeback_tickets`
	CHANGE COLUMN `Text` `Text` TEXT NULL DEFAULT NULL AFTER `Phone`;