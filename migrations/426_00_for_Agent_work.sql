delete from internet.agenttariffs;

insert into internet.agenttariffs (ActionName, `Sum`, Description)
values
("WorkedClient", 250, "Размер компенсации агенту за подключенного клиента"),
("AgentPayIndex", 1.5, "Коэффициент проверки соответствия суммы компенсации и платежей клиента");