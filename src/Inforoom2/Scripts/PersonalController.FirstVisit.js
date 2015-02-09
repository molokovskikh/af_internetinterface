$(document).ready(function () {
	$('.yearDatePicker').datepicker({ changeMonth: true,changeYear: true, firstDay: 1, dateFormat: 'dd.mm.yy' });
	$.datepicker.setDefaults({
		dayNamesMin: ['Вс', 'Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб'],
		monthNamesShort: ["Янв", "Фев", "Мар", "Апр", "Май", "Июнь", "Июль", "Авг", "Сен", "Окт", "Ноя", "Дек"]
	});
});