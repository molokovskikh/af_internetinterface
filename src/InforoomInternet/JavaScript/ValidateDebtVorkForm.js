function addDedtWorkValidation(minPayment) {
	$("#PostponedPaymentActivate").validate({
		rules: {
			debtWorkSum: {
				required: true,
				min: minPayment,
				max: 10000
			},
			endDate : {
				required: true
			}
		},
		messages: {
			debtWorkSum: {
				required: "Введите сумму",
				min: jQuery.format("Сумма должна быть не менее {0}"),
				max: jQuery.format("Сумма должна быть не более {0}")
			},
			endDate: {
				required: "Введите дату"
			}
		},
		errorClass: "errorInService"
	});
}