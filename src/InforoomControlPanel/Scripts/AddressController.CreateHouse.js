ymaps.ready(function() {
	var helper = new YandexMapHelper();
	helper.init("yandexMap");
	helper.setErrorElement($("#yandexError").get(0));
	helper.setCityInput($("#cityName").get(0));
	helper.setStreetInput($("select[name='House.Street.Id']").get(0));
	helper.setHouseInput($("#House_Number"));
	helper.setHouseOutput($("#yandexHouse"));
	helper.setMarkOutput($("#House_Geomark").get(0));
	helper.setPositionOutput($("#yandexPosition").get(0));
	helper.enableMarks();
	if ($("#House_Geomark").val() && $("#House_Geomark").val() != $("#yandexPosition").val()) {
		var split = $("#House_Geomark").val().split(",");
		helper.setCurrentMark(split[0], split[1]);
	}

	//чекбокс отключает карту
	helper.addCondition(function () {
		return $("#House_Confirmed").is(":checked");
	});
	$("#House_Confirmed").on("change", function () {
		if ($(this).is(":checked"))
			helper.run();
	});
	$("select[name='House.Street.Id']").on("change", function () {
		var coords = $(this).find("option:selected").attr("geomark").split(",");
		if (coords.length == 2) {
			console.log("Найдены координаты улицы");
			helper.setCurrentMark(coords[0], coords[1]);
		}
		else
			helper.run();
	});
	$('#House_Number').on("keyup", helper.run);
	helper.run(1);

	//Меняем регион
	$("select[name='House.Street.Id']").on("change", function () {
		var city = $(this).find("option:selected").attr("class");
		$("#cityName").val(city);
	});
});
