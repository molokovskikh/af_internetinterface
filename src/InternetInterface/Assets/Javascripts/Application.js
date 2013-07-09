$(function () {
	//по умолчанию валидация скрытых полей отключена
	//однако скрытые поля используются в качестве основы для search.editor
	//на которые в свою очередь могут быть назначены правила валидации
	$.validator.defaults.ignore = "";
	window.searchEditors = { client: { url: "/clients/search" } };
	registerCheckboxAll();
	registerEditable();
	
	Date.format = 'dd.mm.yyyy';
	$('.graph_date').datepicker();
	$('.date-pick').datepicker({ changeYear: true });
	$('.date_field').datepicker();
});
