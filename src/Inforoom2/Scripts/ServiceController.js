console.log("ServiceController.js");

$(document).ready(function () {
	var monthNames = ["Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь"];
	var weekDays = ['Вс', 'Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб'];
	var colorizeDates = function () {
		//собираем необходимую инфу
		var currentDate = cli.getCurrentDate();
		var freeDays = parseInt(cli.getParam("FreeBlockDays"));

		//фунция подкрашивания
		var render = function (start, end) {
			console.log("Закрашиваем даты ",start, end);
			$(".ui-datepicker-calendar td a.ui-state-default").each(function(i,el) {
				var number = $(el).html();
				if (number > start && number <= end) {
					$(el).attr("style", "background: #c8e8cb");
				}
			});
		}

		//Месяц который отображается в календаре (обрати внимание, что это не месяц даты, выбранной в календаре)
		var datePickerMonth = null;
		var monthName = $(".ui-datepicker-month").html();
		for(var i=0; i < monthNames.length; i++)
			if (monthName == monthNames[i])
				datePickerMonth = i;

		//определяем кол-во бесплатных дней в этом и следующем месяцах
		var daysLeft = currentDate.getDaysInMonth() - currentDate.getDate();
		var daysInThisMonth = daysLeft > freeDays ? freeDays : daysLeft;
		var daysInNextMonth = daysInThisMonth > freeDays ? 0 : freeDays - daysInThisMonth;
		console.log('Бесплатных дней в этом и следующем месяцах:', daysInThisMonth, daysInNextMonth);
		console.log("Отображаемый месяц", datePickerMonth);

		//подкрашиваем даты
		if (datePickerMonth == currentDate.getMonth() && daysInThisMonth > 0)
			render(currentDate.getDate(),currentDate.getDate() + daysInThisMonth);
		else if (datePickerMonth == currentDate.getMonth() + 1 && daysInNextMonth > 0)
			render(0, daysInNextMonth);
	}

	//не во всех событиях то, что нужно нам уже появилось
	//поэтому отдаем управление потоку jquery-ui, и выполняем свой код следом
	colorizeDates = setTimeout.bind(window, colorizeDates, 1);
	$('.datePicker').datepicker({ firstDay: 1, dateFormat: 'dd.mm.yy', beforeShow: colorizeDates, onChangeMonthYear : colorizeDates});
	$.datepicker.setDefaults({ dayNamesMin: weekDays, monthNames : monthNames });
});