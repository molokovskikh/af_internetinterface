var regionDD = $("select[name='street.Region.Id']").get(0);
var streetDD = $("select[name='house.Street.Id']").get(0);
var houseDD = $("select[name='SwitchAddress.House.Id']").get(0);

//Обновление списков дропдаунов, чтобы улицы соответствовали регионам
$(regionDD).on("change", refreshDropDowns);
$(streetDD).on("change", refreshDropDowns);
refreshDropDowns();

//Есть подъеезд или обслуживается весь дом?
$("input[name='noEntrances']").on("change", function () {

	var checked = $(this).is(":checked");
	console.log("change",checked);
	if (checked)
		$("#SwitchAddress_Entrance").attr("disabled", "disabled");
	else
		$("#SwitchAddress_Entrance").removeAttr("disabled");
});

function refreshDropDowns() {
	console.log("Обновляем дропдауны: " + $(regionDD).val() +" "+ $(streetDD).val());
	$(houseDD).removeAttr("disabled");
	$(streetDD).removeAttr("disabled");
	var selected = $(streetDD).find("option:selected").hasClass($(regionDD).val());
	var val = $(regionDD).val();
	var els = [];
	//Обновляем улицы
	$(streetDD).find("option").each(function (i, el) {
		$(el).show();
		if ($(el).hasClass(val)) {
			els.push(el);
			if (!selected) {
				selected = true;
				$(streetDD).val($(el).val());
			}
		} else {
			$(el).hide();
		}
	});

	//хак бага с исчезанием опций
	for (var i = els.length - 1; i >= 0; i--)
		$(streetDD).prepend($(els[i]));

	//Блокируем если нет вариантов
	if (!selected) {
		$(streetDD).attr("disabled", "disabled");
		$(houseDD).attr("disabled", "disabled");
	}

	//Обновляем дома
	selected = $(houseDD).find("option:selected").hasClass($(streetDD).val());
	val = $(streetDD).val();
	els = [];
	$(houseDD).find("option").each(function (i, el) {
		$(el).show();
		if ($(el).hasClass(val)) {
			els.push(el);
			if (!selected) {
				selected = true;
				$(houseDD).val($(el).val());
			}
		} else {
			$(el).hide();
			$(houseDD).append(el);
		}
	});

	//хак бага с исчезанием
	for (var i = els.length - 1; i >= 0; i--)
		$(houseDD).prepend($(els[i]));
	//Блокируем если нет вариантов
	if (!selected)
		$(houseDD).attr("disabled", "disabled");
	
}

