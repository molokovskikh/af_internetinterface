function GetStringFromRequest(url, succes) {
	$.ajax({
		url: url,
		type: "GET",
		success: function (data) {
			succes(data);
		}
	});
}

function GetSmsStatus(but, url) {
	var smsId = $(but).parent().children('input:first').val();
	GetStringFromRequest(url + "?smsId=" + smsId, function (data) {
		$(but).parent().children().hide();
		$(but).parent().append(data);
	});
}