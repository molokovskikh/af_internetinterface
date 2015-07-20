use internet;
UPDATE internet.physicalclients as p
INNER JOIN  internet.tariffs AS t ON p.Tariff = t.Id  
INNER JOIN  internet.clients AS c ON p.Id = c.PhysicalClient
 SET p._LastTimePlanChanged = c.RegDate
 WHERE
 p._LastTimePlanChanged = '0001-01-01' 
 AND p.tariff = 85