function DecimalValCheck(data) {
	var sFullNumber = data.value;
	var ValidChars = "0123456789,";
	var Validn = "0123456789";
	var IsDotPres = false;
	var Char;
	Char = sFullNumber.charAt(0);
	if (Validn.indexOf(Char) == -1) {
		return false;
	}
	else {
		for (i = 0; i < sFullNumber.length; i++) {
			Char = sFullNumber.charAt(i);
			if (Char == ',') {
				if (IsDotPres == false)
					IsDotPres = true;
				else {
					return false;
				}
			}
			if (ValidChars.indexOf(Char) == -1) {
				return false;
			}
		}
	}
	return true;
}
$(function () {
	$.validator.addMethod(
			"decimal",
			function (value, element, regexp) {
				return (this.optional(element) || DecimalValCheck(element)) && regexp;
			}, "Должно быть введено число");

	$.validator.addMethod("range",
		function (value, element, param) {
			//заменяем , на точку что бы приведения срабатывали
			if (value)
				value = value.replace(",", ".");
			return this.optional(element) || (value >= param[0] && value <= param[1]);
		}, "Введите телефон в формате ***-*******");
});
