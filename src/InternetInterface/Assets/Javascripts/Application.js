$(function () {
	
	registerCheckboxAll();
	registerEditable();
	
	Date.format = 'dd.mm.yyyy';
	$('.graph_date').datepicker();
	$('.date-pick').datepicker();
	$('.date_field').datepicker();
});
