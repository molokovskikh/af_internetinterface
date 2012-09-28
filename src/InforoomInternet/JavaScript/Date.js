$(function () {
	$('.startDate').datepicker({
		inline: true,
		minDate: 0,
		showOn: "button",
		buttonImage: "../Content/calendar.gif",
		buttonImageOnly: true,
		buttonText: "Дата начата периода"
	});
	$('.endDate').datepicker({
		inline: true,
		minDate: +1,
		showOn: "button",
		buttonImage: "../Content/calendar.gif",
		buttonImageOnly: true,
		buttonText: "Дата конца периода"
	});
	
	$('.endDateDebt').datepicker({
		inline: true,
		minDate: +1,
		maxDate: +3,
		showOn: "button",
		buttonImage: "../Content/calendar.gif",
		buttonImageOnly: true,
		buttonText: "Дата конца периода"
	});
});