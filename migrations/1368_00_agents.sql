ALTER TABLE Internet.payments 
DROP FOREIGN KEY `Agent`;  


ALTER TABLE Internet.payments   CHANGE Agent Agent INT(10) UNSIGNED;

#Добавляем агентов без ссылки на Partner в таблицу Partner#

INSERT INTO internet.partners(Name)
SELECT ag.name FROM internet.agents ag
LEFT JOIN internet.partners pt ON pt.Id = ag.Partner
WHERE pt.Id IS NULL AND ag.name NOT IN (SELECT name FROM internet.partners);

#Проставляем агентам ссылку на Partner#

UPDATE internet.agents ag
JOIN internet.partners pt ON ag.Name = pt.Name
SET ag.Partner = pt.Id
WHERE ag.Partner = 0;

#Меняем ссылку в таблицу payments с Agent на Partner#

UPDATE internet.payments pay
JOIN internet.agents ag ON pay.Agent = ag.Id 
JOIN internet.partners pt ON ag.Partner = pt.id
SET pay.Agent = pt.Id;
 
ALTER TABLE `internet`.`payments` 
ADD CONSTRAINT `PartnerKey` 
FOREIGN KEY (`Agent`) REFERENCES `internet`.`partners` (`Id`) ON UPDATE CASCADE ON DELETE RESTRICT; 

DROP TABLE IF EXISTS Internet.agents;




