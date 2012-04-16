$(function () {
	Date.format = 'dd.mm.yyyy';
	$('.graph_date').datepicker();
	$('.date-pick').datepicker();
	$('.date_field').datepicker();
	
	$("input[type=checkbox].all").click(function () {
		$(this)
			.parents("table")
			.find("input[type=checkbox]")
			.attr("checked", this.checked);
	});
});
