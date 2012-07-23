function BackPaymentValidation() {
	$('.backPayment').validate({
		errorLabelContainer: "#paymentReasonMessage",
		ignore: "",
		rules: {
			paymentReason: {
				required: true
			}
		},
		messages:
			{
				paymentReason: {
					required: "Введите комментарий"
				}
			}
	});
}

function validateForm(form) {
		var comment = $('#paymentReason').val();
		if (comment != "") {
			form.action += ("?comment=" + comment);
			form.submit();
			return true;
		} else {
			$('#paymentReasonMessage').css("display", "inline");
			return false;
	}
	}

function changeCommentText(field) {
	$('.hiddenCommentText').val($(field).val());
}