update internet.Requests r set
r.RegDate = ActionDate
where
r.RegDate = '0001-01-01 00:00:00';


insert into internet.PaymentsForAgent (Agent, `Sum`, `Comment`, RegistrationDate, `Action`)
select r.Registrator, 50, CONCAT('Начисление за создание заявки #', r.id), r.RegDate, 1 from internet.Requests r
where r.registrator is not null and r.PaidBonus = false;

insert into internet.PaymentsForAgent (Agent, `Sum`, `Comment`, RegistrationDate, `Action`)
select r.Registrator, 50, CONCAT('Зачисление за зарегистрированного клиента #', c.id), c.RegDate, 5 from
internet.Clients c
join internet.PhysicalClients pc on c.PhysicalClient = pc.Id
join internet.Requests r on r.id = pc.Request
where r.PaidBonus = false;

insert into internet.PaymentsForAgent (Agent, `Sum`, `Comment`, RegistrationDate, `Action`)
select r.Registrator, 50, CONCAT('Зачисление за подключенного клиента #', c.id), c.BeginWork, 9 from
internet.Clients c
join internet.PhysicalClients pc on c.PhysicalClient = pc.Id
join internet.Requests r on r.id = pc.Request
where c.BeginWork is not null and r.PaidBonus = false;


insert into internet.PaymentsForAgent (Agent, `Sum`, `Comment`, RegistrationDate, `Action`)
select r.Registrator, -200, CONCAT('Списание за отказ заявки #', r.id), r.RegDate, null from internet.Requests r
where r.`Label` = 21 and r.Registrator is not null;