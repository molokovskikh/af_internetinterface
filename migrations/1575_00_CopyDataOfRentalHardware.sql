use internet;

select @hwId := rh.Id 
from inforoom2_rentalhardware rh
where rh.Name = "ТВ-приставка";

insert into inforoom2_hardwaremodels (Hardware, Model, SerialNumber)
values (@hwId, NULL, NULL), (@hwId, 'AmiNet 139STB', 'G44813D0229521');

insert into inforoom2_clientrentalhardware (Hardware, Model, `Client`, BeginDate, Active, Employee, GiveDate)
	select hm.Hardware, hm.Id, cs.`Client`, cs.BeginWorkDate, cs.Activated, cs.Activator, NOW() as GiveDate
	from ClientServices cs inner join inforoom2_hardwaremodels hm 
	on (cs.Model = hm.Model or (cs.Model is null and hm.Model is null)) 
	  and (cs.SerialNumber = cs.SerialNumber or (cs.SerialNumber is null and hm.SerialNumber is null))
	where cs.Service = 9 or cs.Service = 13;

update ClientServices cs
set cs.EndWorkDate = NOW(), cs.Activated = 0, cs.Diactivated = 1
where cs.Service = 9 or cs.Service = 13;