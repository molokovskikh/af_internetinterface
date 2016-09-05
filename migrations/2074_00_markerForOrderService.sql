USE internet;
ALTER TABLE `orderservices`
	ADD COLUMN `NoWriteOff` TINYINT(1) UNSIGNED NOT NULL DEFAULT '0' AFTER `OrderId`;
	
UPDATE internet.orderservices AS os  SET os.NoWriteOff = 1
WHERE os.Cost IS NOT NULL AND os.Cost <> 0 AND os.IsPeriodic = 0
AND os.Id NOT IN 
(SELECT wf.Service FROM internet.WriteOff AS wf  WHERE wf.Service is not null);