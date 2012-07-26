function BackPaymentValidation() {
	$('.backPayment').validate({
		errorLabelContainer: "#paymentReasonMessage",
		ignore: "",
		rules: {
			comment: {
				required: true
			}
		},
		messages:
			{
				comment: {
					required: "Введите комментарий"
				}
			}
	});
}

function changeCommentText(field) {
	$('.hiddenCommentText').val($(field).val());
}