$(function () {
	$("#filter_Zone_Id").change(function () {
		var switches = $("#filter_Switch_Id");
		switches.prop("disabled", true);
		var data = {
			format: "json"
		}
		var el = $("#filter_Zone_Id");
		if (el.val() > 0) {
			data.zoneId = el.val();
		}
		$.get("/Switches/ShowSwitches", data , function (result) {
			switches.empty();
			switches.append($("<option></option>").html("Все"));
			$.each(result, function (i, item) {
				if (!item.name)
					return;
				switches.append($("<option></option>").val(item.id).html(item.name));
			});
		})
		.always(function () {
			switches.prop("disabled", false);
		});
	});
});