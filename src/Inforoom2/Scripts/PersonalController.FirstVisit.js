$(document).ready(function () {
	$('.yearDatePicker').datepicker({ changeMonth: true,changeYear: true, firstDay: 1, dateFormat: 'dd.mm.yy' });
	$.datepicker.setDefaults({
		dayNamesMin: ['Вс', 'Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб'],
		monthNamesShort: ["Янв", "Фев", "Мар", "Апр", "Май", "Июнь", "Июль", "Авг", "Сен", "Окт", "Ноя", "Дек"]
	});
});

function showDocData() {
	if (document.getElementById('physicalClient_CertificateType').value == "Passport") {
		document.getElementById('PassportData').hidden = false;
		document.getElementById('OtherDocData').hidden = true;
	} else {
		document.getElementById('PassportData').hidden = true;
		document.getElementById('OtherDocData').hidden = false;
	}
};
