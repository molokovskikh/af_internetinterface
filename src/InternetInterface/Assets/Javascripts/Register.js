function SelectHouse(item, chHouse) {
	var regionCode = $(item).val();
	if (regionCode == undefined) {
		regionCode = item;
	}
	$('#clientCityName').val(regions[regionCode]);
	$.ajax({
		url: "../Register/HouseSelect",
		type: "GET",
		cache: false,
		data: { regionCode: regionCode, chHouse: chHouse },
		success: function (data) {
			$('#SelectHouseTD').empty();
			$('#SelectHouseTD').append(data);
		}
	});
}