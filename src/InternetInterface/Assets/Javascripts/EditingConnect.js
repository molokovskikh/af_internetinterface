$(function () {
	$('#formConnect').validate();

	var port = $('#Port');
	port.rules("add", {
		required: true, digits: true,
		messages: {
			required: "Введите номер порта",
			digits : "Должно быть введено число"
		}
	});

	var selectEndpoint = $("select[name='currentEndPoint']");
	selectEndpoint.bind("change", function () {
		var val = selectEndpoint.val();
		var endpointTable = $("#endpointTable");
		console.log("Преключаем инфо точки доступа: " + val);
		if (val == 0)
			endpointTable.show();
		else
			endpointTable.hide();
	});
});