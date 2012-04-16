$(function () {

	$.validator.addMethod("validateEmailList", function (value, element) {
		if (value.toString().length > 0) {
			return /^\s*\w[\w\.\-]*[@]\w[\w\.\-]*([.]([\w]{1,})){1,3}\s*(\,\s*\w[\w\.\-]*[@]\w[\w\.\-]*([.]([\w]{1,})){1,3}\s*)*$/.test(value)
		}
		return true;
	}, "Поле содержит некорректный адрес электронной почты");

	$('.HighLightCurrentRow tr').not('.NoHighLightRow').each(function () {
		$(this).mouseout(function () { $(this).removeClass('SelectedRow'); });
		$(this).mouseover(function () { $(this).addClass('SelectedRow'); });
	});
	$('.input-date').each(function () {
		$(this).mask("99.99.9999");
	});
	/*
	$('.input-sum').each(function () {
	$(this).mask("999999999,99");
	});
	*/
	$("form.accountant-friendly input[type=text],form.accountant-friendly select,form.accountant-friendly textarea").keydown(function (e) {
		if (event.keyCode == 13) {
			var items = $(this).parents("form").find("input[type=text],select,textarea");
			var currentIndex = items.index(this);
			var nextItem = items.get(currentIndex + 1);
			if (nextItem) {
				nextItem.focus();
				nextItem.select();
				return false;
			}
		}
	});

	$(".ShowHiden").live('click', function () {
		ShowHidden(this);
	});

	$(".HideVisible").live('click', function () {
		HideVisible(this);
	});

	SetupCalendarElements();

	$(".tabs ul li a").click(function () {
		$(".tab").hide();
		$(".tabs ul li a.selected").removeClass("selected");
		$("#" + this.id + "-tab").show();
		$(this).addClass("selected");
		if ($(this).attr("href") == "#")
			return false;
		else
			return true;
	});

	$(".tabs ul li a.inline-tab").click(function () {
		var id = this.id;
		$.ajax({
			url: $(this).attr("href"),
			success: function (content) {
				$("#" + id + "-tab").html(content)
			},
			error: function () {
				alert(error);
			}
		});
		return false;
	});


	function beginDateAllowed(date) {
		if (endCalendar)
			return !(date <= endCalendar.date);
		else
			return false;
	}

	function endDateAllowed(date) {
		if (beginCalendar)
			return !(date >= beginCalendar.date);
		else
			return false;
	}

	beginCalendar = null;
	endCalendar = null;

	$(".calendar").each(function () {
		var id = this.id.substring(0, this.id.indexOf("CalendarHolder"));
		var value = $("#" + id).get(0).value;


		var calendar = Calendar.setup({
			daFormat: "%d.%m.%Y",
			ifFormat: "%d.%m.%Y",
			weekNumbers: false,
			flat: this.id,
			flatCallback: function () {
				$("#" + id).get(0).value = calendar.date.print("%d.%m.%Y")
				calendar.refresh();
				if (beginCalendar && endCalendar) {
					beginCalendar.refresh();
					endCalendar.refresh();
				}
			},
			showOthers: true
		});
		calendar.parseDate(value)
		if (id.indexOf("begin") >= 0) {
			beginCalendar = calendar;
			calendar.setDateStatusHandler(beginDateAllowed);
		}
		if (id.indexOf("end") >= 0) {
			endCalendar = calendar;
			calendar.setDateStatusHandler(endDateAllowed);
		}
	});

	if (beginCalendar)
		beginCalendar.refresh();
	if (endCalendar)
		endCalendar.refresh();
});

function ShowHidden(folder) {
	$(folder).removeClass("ShowHiden");
	$(folder).addClass("HideVisible");
	$(folder).siblings("div").removeClass("hidden");
}

function HideVisible(folder) {
	$(folder).removeClass("HideVisible");
	$(folder).addClass("ShowHiden");
	$(folder).siblings("div").addClass("hidden");
}

function SetupCalendarElements() {
	$(".CalendarInput")
	.each(function (index, value) {
		value.id = "CalendarInput" + index;
		$(value).prev().id = "CalendarInputField" + index;
		Calendar.setup({
			ifFormat: "%d.%m.%Y",
			inputField: $(value).prev().get(0),
			button: value.id,
			weekNumbers: false,
			showOthers: true
		});
	});
}

function ShowElement(show, selector) {
	var displayValue = "none";
	if (show)
		displayValue = "block";
	$(selector).css("display", displayValue);
}