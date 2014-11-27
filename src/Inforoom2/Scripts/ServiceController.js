console.log("ServiceController.js");

$(document).ready(function () {
	$('.datePicker').datepicker({ firstDay: 1, dateFormat: 'dd.mm.yy' });
	$.datepicker.setDefaults({ dayNamesMin: ['Вс', 'Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб'] });
});