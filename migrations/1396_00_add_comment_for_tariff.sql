UPDATE internet.tariffs t 
	SET t.Name = "<b>Быстрый старт</b>", t.Description = CONCAT(t.Description, "<b>(только для многоквартирных домов)</b>" )
	WHERE t.Name = "<b>Быстрый старт </b><br><br><i>(только для многоквартирных домов)";
UPDATE internet.tariffs t 
	SET t.Name = "<b>Старт</b>", t.Description = CONCAT(t.Description, "<b>(только для многоквартирных домов)</b>" )
	WHERE t.Name = "<b>Старт </b><br><br><i>(только для многоквартирных домов)";
UPDATE internet.tariffs t 
	SET t.Name = "<b>Акция Быстрый старт</b>", t.Description = CONCAT(t.Description, "<b>(только для многоквартирных домов)</b>" )
	WHERE t.Name = "<b>Акция Быстрый старт </b><br><br><i>(только для многоквартирных домов)";
UPDATE internet.tariffs t 
	SET t.Name = "<b>Акция СТАРТ</b>", t.Description = CONCAT(t.Description, "<b>(только для многоквартирных домов)</b>" )
	WHERE t.Name = "<b>Акция СТАРТ </b><br><br><i>(только для многоквартирных домов)";