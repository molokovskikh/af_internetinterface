$(function () {
	$('.contact_link').click(function () {
		$('#request_Contact').val(this.innerText);
	});

	var pendingRequest;
	function update(date, performer) {
		if (pendingRequest)
			pendingRequest.abort();
		pendingRequest = $.ajax({
			url: "/ServiceRequest/Timetable",
			data: { date: date, id: performer },
			success: function (data) {
				$("#timetable").html(data);
			}
		}).always(function () {
			pendingRequest = null;
		});
	}

	$("#request_PerformanceDate,#request_Performer_Id").change(function () {
		update($("#request_PerformanceDate").val(), $("#request_Performer_Id").val());
	});
	$("#request_PerformanceDate").change();
});
