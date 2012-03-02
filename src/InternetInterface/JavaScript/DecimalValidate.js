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

$.validator.addMethod(
		"decimal",
		function (value, element, regexp) {
			return (this.optional(element) || DecimalValCheck(element)) && regexp;
		}, "Должно быть введено число");