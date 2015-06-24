window.onload = function () {
	$(".switchstatus").each(function () {
		var id = $(this).find(".endpointid").html();
		var statusElement = $(this).find(".status");
		updateEndpointStatus(id, statusElement);
	})
};

//Функция, которая пингует эндпоинт и отображает ответ
function updateEndpointStatus(id, htmlElement, timeout) {
	if (!timeout)
		timeout = 5000;
	$.ajax({
		url: "./PingEndpoint?Id=" + id,
		type: 'POST',
		dataType: "json",
		success: function (data) {
			$(htmlElement).html(data);
			setTimeout(updateEndpointStatus.bind(this, id, htmlElement, timeout), timeout);
		},
		error: function (data) {
			setTimeout(updateEndpointStatus.bind(this, id, htmlElement, timeout), timeout);
		}
	});
}



function DeleteWriteOff(path, writeOffInfo) {
	YesNoDialog("Удаление списания", "Вы действительно хотите удалить данное списание? (" + writeOffInfo + ")", function () {
		window.location = path;
	});
}