$(function () {
	$('.validateFormService').validate({
		errorElement: "div",
		errorLabelContainer: "#errorContainer"
	});
	$.validator.addMethod(
	"regexContact",
	function (value, element, regexp) {
		var re = new RegExp(regexp);
		return this.optional(element) || re.test(value);
	}, "Введите телефон в формате ***-*******");

	$('#contact_text').rules("add", { required: true, regexContact: "^([0-9]{3})\-([0-9]{7})$",
		messages: {
			required: "Введите номер телефона"
		}
	});
	$('#sumField').rules("add", { number: true,
		messages: {
			number: "Должно быть введено число"
		}
	});
	$('.graph_date').datepicker();
});