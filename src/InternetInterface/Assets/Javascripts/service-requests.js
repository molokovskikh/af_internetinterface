$(function () {
	$('.contact_link').click(function () {
		$('#request_Contact').val($.trim(this.innerText));
	});

	var pending;
	function update(date, performer) {
		var url = "/ServiceRequest/Timetable?" + $.param({ date: date, id: performer });
		if (pending)
			pending.abort();
		pending = $.ajax({
			url: url,
			success: function (data) {
				$("#timetable").html(data);
				$("#timetable").data("url", url);
			}
		}).always(function () {
			pending = null;
		});
	}

	$("#request_PerformanceDate,#request_Performer_Id").change(function () {
		update($("#request_PerformanceDate").val(), $("#request_Performer_Id").val());
	});
	$("#request_PerformanceDate").change();

	var checkFlag = false;
	$('.validateFormService').validate({
		errorElement: "div",
		errorLabelContainer: "#errorContainer",
		submitHandler: function (form) {
			var id = $("#request_Id");
			var flagFree = document.getElementById('request_Free').checked;
			if (flagFree && checkFlag) {
				var url = "${Siteroot}/ServiceRequest/AddServiceComment";
				$.ajax({
					url: url,
					data: { requestId: id,
							commentText: $('.comment_sum_text:first').val()},
					cache: false
				});
			}
			form.submit();
		}
	});

	$.validator.addMethod(
		"zeroSumValidator",
		function (value, element, validValue) {
			if (element.checked && checkFlag) {
				if ($('.comment_sum_text').length == 0) {
					$('#comment_sum').append('<br />Причина: <input type="text" class="comment_sum_text">');
				}
				if ($('.comment_sum_text:first').val() != "") {
					return true;
				}
			} else { return true; }
			return false;
		}, "Введите комментарий");

	$("#sumField").each(function () {
		$(this).rules("add", {
			number: true,
			messages: {
				number: "Должно быть введено число"
			}
		});
	});

	$('#request_Free').each(function () {
		$(this).rules("add", {
			zeroSumValidator: true,
			messages: {
				zeroSumValidator: "Опишите причину, почему заявка стала бесплатной"
			}
		});
	});

	$('#sumField').change(function () {
		$('#request_Free').removeAttr("checked");
	});

	$('#request_Free').click(function () {
		checkFlag = true;
		if (this.checked){
			$('#sumField').val("");
			if ($('.comment_sum_text').length == 0) {
				$('#comment_sum').append('<br />Причина: <input type="text" class="comment_sum_text">');
			}
		}
	});
});
