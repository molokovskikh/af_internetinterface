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

	colorizeDates = setTimeout.bind(window, colorizeDates, 1);
	var options = {};
	options.firstDay = 1;
	options.dateFormat = 'dd.mm.yy';
	options.beforeShow = colorizeDates;
	options.onChangeMonthYear = colorizeDates;
	options.showOn = "both";
	options.buttonImage = "/Images/x_office_calendar.png";
	options.buttonImageOnly = true;
	options.buttonText = "Select date";

	//не во всех событиях то, что нужно нам уже появилось
	//поэтому отдаем управление потоку jquery-ui, и выполняем свой код следом
	$('.datePicker').datepicker(options);
	$.datepicker.setDefaults({ dayNamesMin: weekDays, monthNames : monthNames });
});

$("#ConnectBtn").on("click", function () {
	var currentDate = cli.getCurrentDate();
	var blockweeks = $('#weeksCount').val();
	var blockDays = blockweeks*7;
	var freeDays = parseInt(cli.getParam("FreeBlockDays"));
	if (blockDays > freeDays) {
		var daysLeft = currentDate.getDaysInMonth() - currentDate.getDate();
		var daysInThisMonth = daysLeft > freeDays ? (freeDays + 1) : daysLeft;
		var daysInNextMonth = daysInThisMonth > (freeDays + 1) ? 0 : (freeDays + 1 - daysInThisMonth);
		var newYear = currentDate.getFullYear();
		var newMonth = currentDate.getMonth();
		var newDay = currentDate.getDate();
		if (daysInNextMonth == 0) {
			newDay += daysInThisMonth;
		}
		else {
			if (newMonth == 12) {
				newYear += 1;
				newMonth = 1;
			}
			else {
				newMonth += 1;
			}
			newDay = daysInNextMonth;
		}
		var notFreeBlockDate = new Date(newYear, newMonth, newDay);
		var content = "С " + notFreeBlockDate.toLocaleDateString() + " услуга станет платная: разовая абон/плата - 50р., ежедневная абон/плата - 3р.";
		cli.areYouSure(content, function () { $(".right-block form").submit(); }, "Подтвердите действие");
		return false;
	}
	return true;
});

function getMonth(_this) {
	var weeks = $(_this).val();
	$.ajax({
		url: cli.getParam("baseurl") + "Service/BlockAccountWeek?weeks=" + weeks,
		type: 'POST',
		dataType: "json",
		success: function (data) {
			$("#dateInWeeks").html(data);
			$("#blockingEndDate").val(data);
		},
		error: function (data) {
			$("#dateInWeeks").html("кол-во указано неверно");
			$("#blockingEndDate").val("");
		},
		statusCode: {
			404: function () {
				$("#dateInWeeks").html("кол-во указано неверно");
				$("#blockingEndDate").val("");
			}
		}
	});
}
