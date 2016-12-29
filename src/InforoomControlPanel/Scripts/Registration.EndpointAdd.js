
$(function () {

	$("#SwitchDropDown").unbind("change").change(function () {
		GetBusyPorts();
	});

	//.unbind("change") не использовать отвязку событий, т.к.  
	$("#RegionDropDown").change(function () {
		var regionId = $("#RegionDropDown").val();
		$("#SwitchDropDown").attr("disabled", "disabled");
		$("#SwitchDropDown").html("");
		if (regionId !== undefined && regionId !== 0 && regionId !== "") {
			$.ajax({
				url: cli.getParam("baseurl") + "AdminOpen/GetSwitchListForRegion?regionId=" + regionId,
				type: "POST",
				dataType: "json",
				success: function(data) {
					var html = "<option selected='selected'></option>";
					if (data != undefined && data != null && data.length > 0) {
						for (var i = 0; i < data.length; i++) {
							html += " <option value='" + data[i].Id + "' maxports='" + data[i].PortsCount + "'>" + data[i].Name + "</option>";
						}
						$("#SwitchDropDown").html(html);
						$("#SwitchDropDown").removeAttr("disabled");
						var pastVal = $("#SwitchDropDown").attr("pastValue");
						if (pastVal != undefined && pastVal !== "" && $("#SwitchDropDown option[value='" + pastVal + "']").length > 0) {
							$("#SwitchDropDown").val(pastVal);
						}
					}
				}
			});
		}
	});
	var currentRegionVal = $("#RegionDropDown").val();
	if (currentRegionVal != undefined && currentRegionVal != "" && currentRegionVal != 0) {
		$("#RegionDropDown").change();
	}
});