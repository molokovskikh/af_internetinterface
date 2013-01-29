function ShowFindEditor() {
	var selectedFindType = $('.filter_searchProperties:checked').val();
	if (selectedFindType == 5) {
		$("#AddressField").show();
		$("#AllFindT").hide();
	} else {
		$("#AddressField").hide();
		$("#AllFindT").show();
	}
}