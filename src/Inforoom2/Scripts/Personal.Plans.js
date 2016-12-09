$(".connectfee").on("click", function () {
	var self = this;
	var messageText = "Вы уверены, что хотите сменить тариф?";
	if ($(this).attr("title") != null) {
		messageText += "<br/>" + $(this).attr("title");
	}
	if ($(this).attr("price") != null) {
		messageText += "<br/>Стоимость смены тарифа " + $(this).attr("price") + " руб.";
	}
	cli.areYouSure(messageText, function () {
		$(self).parent("form").submit();
	}, "Подтвердите действие");

	return false;
})