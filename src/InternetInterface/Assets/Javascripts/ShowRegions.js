function registerRegion() {
	var req = {};
	req.RegionName = $('#regionHouse_Name').val();
	$.post("../UserInfo/RegisterRegion", req, function (data) {
		alert("Зарегистрировано");
		$('#RegionTable').append($('<tr><td>' + '<a href="/internetinterface/UserInfo/EditRegion?id=' + data.Id + '">Редактировать</a>' + '</td><td>'+data.regionName+'</td></tr>'));
		$('#register_region').trigger('reveal:close');
	});
};