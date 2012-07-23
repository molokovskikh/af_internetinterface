function BackPaymentValidation() {
	$('.backPayment').validate({
		errorElement: "div",
		errorLabelContainer: "#errorContainer",
		submitHandler: function (form) {
			var comment = $('#paymentReason').val();
			if (comment != "") {
				form.action += ("?comment=" + comment);
				form.submit();
			} else {
				$('#paymentReasonMessage').css("display", "inline");
			}
		}
	});
}