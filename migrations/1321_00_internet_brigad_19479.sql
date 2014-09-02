ALTER TABLE internet.connectbrigads
	ADD `WorkBegin` BIGINT(20) NULL DEFAULT 342000000000,
	ADD `WorkEnd` BIGINT(20) NULL DEFAULT 666000000000,
	ADD `WorkStep` BIGINT(20) NULL DEFAULT 18000000000;
UPDATE internet.connectbrigads SET WorkBegin = 342000000000 WHERE WorkBegin IS NULL;
UPDATE internet.connectbrigads SET WorkEnd = 666000000000 WHERE WorkEnd IS NULL;
UPDATE internet.connectbrigads SET WorkStep = 18000000000 WHERE WorkStep IS NULL;
ALTER TABLE internet.connectgraph
	CHANGE COLUMN `Day` `DateAndTime` DATETIME NULL DEFAULT NULL AFTER `Client`,
	ADD `IsReserved` TINYINT(1) NOT NULL DEFAULT '0';
UPDATE internet.connectgraph SET `IsReserved` = '1' WHERE `Client` is NULL;
UPDATE internet.connectgraph SET `DateAndTime` = ADDTIME(`DateAndTime`, "09:30") WHERE `IntervalId` = '0';
UPDATE internet.connectgraph SET `DateAndTime` = ADDTIME(`DateAndTime`, "10:00") WHERE `IntervalId` = '1';
UPDATE internet.connectgraph SET `DateAndTime` = ADDTIME(`DateAndTime`, "10:30") WHERE `IntervalId` = '2';
UPDATE internet.connectgraph SET `DateAndTime` = ADDTIME(`DateAndTime`, "11:00") WHERE `IntervalId` = '3';
UPDATE internet.connectgraph SET `DateAndTime` = ADDTIME(`DateAndTime`, "11:30") WHERE `IntervalId` = '4';
UPDATE internet.connectgraph SET `DateAndTime` = ADDTIME(`DateAndTime`, "12:00") WHERE `IntervalId` = '5';
UPDATE internet.connectgraph SET `DateAndTime` = ADDTIME(`DateAndTime`, "12:30") WHERE `IntervalId` = '6';
UPDATE internet.connectgraph SET `DateAndTime` = ADDTIME(`DateAndTime`, "13:00") WHERE `IntervalId` = '7';
UPDATE internet.connectgraph SET `DateAndTime` = ADDTIME(`DateAndTime`, "13:30") WHERE `IntervalId` = '8';
UPDATE internet.connectgraph SET `DateAndTime` = ADDTIME(`DateAndTime`, "14:00") WHERE `IntervalId` = '9';
UPDATE internet.connectgraph SET `DateAndTime` = ADDTIME(`DateAndTime`, "14:30") WHERE `IntervalId` = '10';
UPDATE internet.connectgraph SET `DateAndTime` = ADDTIME(`DateAndTime`, "15:00") WHERE `IntervalId` = '11';
UPDATE internet.connectgraph SET `DateAndTime` = ADDTIME(`DateAndTime`, "15:30") WHERE `IntervalId` = '12';
UPDATE internet.connectgraph SET `DateAndTime` = ADDTIME(`DateAndTime`, "16:00") WHERE `IntervalId` = '13';
UPDATE internet.connectgraph SET `DateAndTime` = ADDTIME(`DateAndTime`, "16:30") WHERE `IntervalId` = '14';
UPDATE internet.connectgraph SET `DateAndTime` = ADDTIME(`DateAndTime`, "17:00") WHERE `IntervalId` = '15';
UPDATE internet.connectgraph SET `DateAndTime` = ADDTIME(`DateAndTime`, "17:30") WHERE `IntervalId` = '16';
UPDATE internet.connectgraph SET `DateAndTime` = ADDTIME(`DateAndTime`, "18:00") WHERE `IntervalId` = '17';