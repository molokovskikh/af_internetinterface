function ShowFindEditor() {
	var selectedFindType = $('#SearchPropertiesTd input[type=radio]:checked').val();
	if (selectedFindType == 5) {
		$("#AddressField").show();
		$("#AllFindT").hide();
	} else {
		$("#AddressField").hide();
		$("#AllFindT").show();
	}
}