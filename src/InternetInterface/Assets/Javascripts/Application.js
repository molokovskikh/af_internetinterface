$(function () {
	//по умолчанию валидация скрытых полей отключена
	//однако скрытые поля используются в качестве основы для search.editor
	//на которые в свою очередь могут быть назначены правила валидации
	//по этому игнорируем все скрытые поля кроме type=hidden
	$.validator.defaults.ignore = ":hidden:not(input[type=hidden])";
	$("form.validable").each(function () {
		$(this).validate();
	});
	window.searchEditors = { client: { url: "/clients/search" } };
	registerCheckboxAll();
	registerEditable();

	Date.format = 'dd.mm.yyyy';
	$('.graph_date').datepicker();
	$('.date-pick').datepicker({ changeYear: true });
	$('.date_field').datepicker();
	$('#startDate').datepicker();
	$('#beginDate').datepicker();
	$('#endDate').datepicker();

	$(".accordion").accordion({
		collapsible: true,
		heightStyle: "content"
	});
	$('.date-pick').datepicker().change(function () {
		$('.startDate_local').val($(this).val());
	});
});
