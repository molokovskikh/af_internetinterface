$(function () {
	$('.contact_link').click(function () {
		$('#request_Contact').val(this.innerText);
	});

	function update(date, performer) {
		$("#timetable").html("");
		$.ajax({
			type: "GET",
			url: "/ServiceRequest/Timetable",
			data: { date: date, id: performer },
			success: function (data) {
				$("#timetable").html(data);
			}
		});
	}

	$("#request_PerformanceDate,#request_Performer_Id").change(function () {
		update($("#request_PerformanceDate").val(), $("#request_Performer_Id").val());
	});
	$("#request_PerformanceDate").change();
});
