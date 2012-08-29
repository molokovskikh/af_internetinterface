function HtmlEncode(val) {
	return $("<div/>").text(val).html();
} 

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

function changeCommentText() {
	var encoded = HtmlEncode($('#paymentReason').val());
	$('.hiddenCommentText').val(encoded);
}