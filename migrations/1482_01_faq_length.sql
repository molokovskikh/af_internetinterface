use internet;
ALTER TABLE `inforoom2_questions`
	CHANGE COLUMN `Answer` `Answer` TEXT NULL DEFAULT NULL AFTER `Text`;
