delete from Internet.WriteOff;

ALTER TABLE `internet`.`WriteOff`
 DROP FOREIGN KEY `ClientRelation`;

ALTER TABLE `internet`.`WriteOff` ADD CONSTRAINT `ClientRelation` FOREIGN KEY `ClientRelation` (`Client`)
    REFERENCES `Clients` (`Id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION;
	
	
ALTER TABLE `internet`.`WriteOff`
 DROP FOREIGN KEY `ClientRelation`;

ALTER TABLE `internet`.`WriteOff` ADD CONSTRAINT `ClientRelation` FOREIGN KEY `ClientRelation` (`Client`)
    REFERENCES `Clients` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;


insert into Internet.WriteOff (WriteOffSum, WriteOffDate,Client)
select pc.summ as WriteOffSum, Curdate() as WriteOffDate, c.Id as Client
from Internet.PaymentForConnect pc
join internet.Clients c on c.PhisicalClient = pc.ClientId
where pc.Summ > 0;