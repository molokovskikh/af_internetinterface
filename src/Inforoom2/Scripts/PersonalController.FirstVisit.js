$(document).ready(function () {
	$('.yearDatePicker').datepicker({ changeMonth: true,changeYear: true, firstDay: 1, dateFormat: 'dd.mm.yy' });
	$.datepicker.setDefaults({
		dayNamesMin: ['Вс', 'Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб'],
		monthNamesShort: ["Янв", "Фев", "Мар", "Апр", "Май", "Июнь", "Июль", "Авг", "Сен", "Окт", "Ноя", "Дек"]
	});
	$("form input").each(function () {
		if ($(this).val() == "") {
			$(this).focus();
		}
	});
});

//Сокрытие полей паспорта
var certificate = $("#physicalClient_CertificateType").get(0);
var oldValue = $("#physicalClient_CertificateName").val();
var checkFields = function () {
	if ($(certificate).val() == "Passport") {
		console.log("Отображаем данные паспорта");
		$('#PassportData').show();
		$('#OtherDocData').hide();
		//Устанавливаем значение по умолчанию, чтобы валидатор модели сработал
		oldValue = $("#physicalClient_CertificateName").val();
		$("#physicalClient_CertificateName").val("Паспорт");
	} else {
		console.log("Отображаем данные иного документа");
		$('#PassportData').hide();
		$('#OtherDocData').show();
		$("#physicalClient_CertificateName").val(oldValue);
	}
};
$(certificate).on("change", checkFields);
checkFields();

