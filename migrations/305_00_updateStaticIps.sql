ALTER TABLE `internet`.`StaticIps` 
 DROP FOREIGN KEY `FK_StaticIp_1`;

 update internet.StaticIps S
join internet.ClientEndPoints cp on cp.Client = s.Client
set s.Client = cp.id;

ALTER TABLE `internet`.`StaticIps` CHANGE COLUMN `Client` `EndPoint` INT(10) UNSIGNED DEFAULT NULL,
 DROP INDEX `FK_StaticIp_1`,
 ADD INDEX `FK_StaticIp_1` USING BTREE(`EndPoint`),
 ADD CONSTRAINT `FK_StaticIps_1` FOREIGN KEY `FK_StaticIps_1` (`EndPoint`)
    REFERENCES `ClientEndpoints` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE;
	
update internet.StaticIps S
set EndPoint = 290
where id = 9;

update internet.StaticIps S
set EndPoint = 390
where id = 23;

update internet.StaticIps S
set EndPoint = 390
where id = 25;

