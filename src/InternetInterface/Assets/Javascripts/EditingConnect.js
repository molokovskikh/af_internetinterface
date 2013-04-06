$(function () {
	$('#formConnect').validate();

	$('#Port').rules("add", {
		required: true, digits: true,
		messages: {
			required: "Введите номер порта",
			digits : "Должно быть введено число"
		}
	});
});