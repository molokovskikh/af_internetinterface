ymaps.ready(function () {
	var helper = new YandexMapHelper();
	helper.init("yandexMap");
	helper.setErrorElement($("#yandexError").get(0));
	helper.setStreetInput($("#Street_Name").get(0));
	helper.setCityInput($("select[name='Street.Region.Id']").get(0));
	helper.setStreetOutput($("#yandexStreet"));
	helper.setMarkOutput($("#Street_Geomark").get(0));
	helper.setPositionOutput($("#yandexPosition").get(0));
	helper.enableMarks();
	if ($("#Street_Geomark").val() && $("#Street_Geomark").val() != $("#yandexPosition").val()) {
		var split = $("#Street_Geomark").val().split(",");
		helper.setCurrentMark(split[0],split[1]);
	}

	//чекбокс отключает карту
	helper.addCondition(function() {
		return $("#Street_Confirmed").is(":checked");
	});
	$("#Street_Confirmed").on("change", function() {
		if ($(this).is(":checked"))
			helper.run();
	});

	$("select[name='Street.Region.Id']").on("change", helper.run);
	$('#Street_Name').on("keyup", helper.run);
	helper.run(1);
});
