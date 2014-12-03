console.log("AdminAddressController.js");
$('#RegionDropDown').on("change", onCityChanged);
$('#StreetDropDown').on("change", onStreetChanged);

function onCityChanged() {
	var city = $('#RegionDropDown option:selected');
	var reselected = false;
	$('#StreetDropDown option').each(function(i, el) {
		if ($(el).attr("class").match($(city).attr("class"))) {
			$(el).show();
			if (!reselected) {
				$('#StreetDropDown').get(0).selectedIndex = i;
				reselected = true;
			}
		} else {
			$(el).hide();
		};
	});
}

function onStreetChanged() {
	var street = $('#StreetDropDown option:selected');
	var reselected = false;
	$('#HouseDropDown option').each(function (i, el) {
		if($(street).hasClass($(el).attr("class"))){
			$(el).show();
			if (!reselected) {
				$('#HouseDropDown').get(0).selectedIndex = i;
				reselected = true;
			}
		} else {
			$(el).hide();
		};
	});
}
