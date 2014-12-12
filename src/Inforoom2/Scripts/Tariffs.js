$(".connectfee").on("click", function() {
	cli.areYouSure("Вы уверены, что хотите сменить тариф?", function() {
		$('#changeplan').submit();
	}, "Подтвердите действие");

	return false;
})