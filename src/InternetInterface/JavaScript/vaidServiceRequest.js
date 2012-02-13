$(function () {
	/*$('.validateFormService').validate({
		errorElement: "div",
		errorLabelContainer: "#errorContainer"
	});*/
		
	$.validator.addMethod(
	"regexContact",
	function (value, element, regexp) {
		var re = new RegExp(regexp);
		return this.optional(element) || re.test(value);
	}, "Введите телефон в формате ***-*******");



	$('.graph_date').datepicker();
});