CREATE TABLE `internet`.`WriteOff` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `WriteOffSum` DECIMAL NOT NULL,
  `WriteOffDate` DATETIME NOT NULL,
  `Client` INT(10) UNSIGNED,
  PRIMARY KEY (`Id`),
  CONSTRAINT `ClientRelation` FOREIGN KEY `ClientRelation` (`Client`)
    REFERENCES `Clients` (`Id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE
)
ENGINE = InnoDB;


update internet.PhysicalClients P, internet.PaymentForConnect pfc set
p.Balance = p.Balance - pfc.Summ
where pfc.ClientId = p.id and p.id > 29;

insert into internet.WriteOff (WriteOffSum, writeoffDate, Client)
SELECT  pfc.Summ as WriteOffSum, curdate() as WriteOffDate, c.id as Client FROM internet.PhysicalClients P
left join internet.PaymentForConnect pfc on pfc.ClientId = p.id
join internet.Clients c on c.PhisicalClient = p.id
where p.id > 29 and pfc.Summ > 0;