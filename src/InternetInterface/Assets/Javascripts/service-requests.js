$(function () {

	var checkFlag = false;
	var Request = function () {
		var self = this;
		self.sum = ko.observable();
		self.isFree = ko.observable();
		self.status = ko.observable();
		self.canWriteSms = ko.computed(function () {
			return self.sum() > 0 && !self.isFree() && self.status() == 3;
		}, self);
		self.isFree.subscribe(function () {
			checkFlag = true;
			if (self.isFree()) {
				self.sum("");
			}
		});
	};

	$(".validateFormService").each(function () {
		var el = $(this);
		var model = new Request(el);
		el.data("model", model);
		ko.applyBindings(model, this);
	});

	$('.contact_link').click(function () {
		$('#request_Contact').val($.trim(this.innerText));
	});

	if ($("#timetable").length) {
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
	}

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
		function (value, element) {
			if (element.checked && checkFlag) {
				if ($('.comment_sum_text').val() != "") {
					return true;
				}
			} else {
				return true;
			}
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
});
