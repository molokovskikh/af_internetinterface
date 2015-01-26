$(".connectfee").on("click", function () {
	var self = this;
	cli.areYouSure("Вы уверены, что хотите сменить тариф?", function () {
		$(self).parent("form").submit();
	}, "Подтвердите действие");

	return false;
})