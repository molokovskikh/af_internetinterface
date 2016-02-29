var firstYear = 0;
var firstMonth = 0;

var lastYear = 0;
var lastMonth = 0;

var currentYear = 0;
var currentMonth = 0;

var markerMonth = "#monthSelector .mnt";
var markerYear = "#currentYear";

var markerDateA = "#dateA";
var markerDateB = "#dateB";
var markerDates = "#dateA,#dateB";

function Clean() {
	if (!$(markerYear).hasClass("input_disabled")) { 
		$(markerMonth).removeClass("checked");
		$(markerYear).val(lastYear);
		$(markerYear).change();
	}
	$(markerYear).addClass("input_disabled");
}

function DateSet(_this) {
	$("#selectedMonth").val("");
	$("#selectedMonth").val($(_this).attr("month"));
	document.getElementById("PaymentForm").submit();
}

function ListUpdate() {
	var currentSelectedYear = $(markerYear).val(); 
	currentSelectedYear = parseInt(currentSelectedYear);

	$(markerYear).removeClass("input_disabled");

	$(markerMonth).removeClass("checked");
	$(markerMonth).removeClass("disabled");
	$(markerMonth).each(function() {
		var cmonth = parseInt($(this).attr("month"));
		if (currentSelectedYear === currentYear && currentMonth === cmonth) {
			$(this).addClass("checked");
		}
		if ((currentSelectedYear === firstYear && cmonth < firstMonth) ||
		(currentSelectedYear === lastYear && cmonth > lastMonth)) {
			$(this).addClass("disabled");
		} else {
			$(this).unbind("click").click(function() {
				DateSet(this);
			});
		}
	});
}

function OnLoad() {
	firstYear = parseInt($("#firstYear").val());
	firstMonth = parseInt($("#firstMonth").val());

	lastYear = parseInt($("#lastYear").val());
	lastMonth = parseInt($("#lastMonth").val());

	currentYear = parseInt($("#currentYear").val());
	currentMonth = parseInt($("#currentMonth").val());
	ListUpdate();
	$(markerYear).unbind("change").change(function() {
		ListUpdate();
	});

	$(markerDates).unbind("change").change(function () { 
		Clean();
	});
}

$(function() {
	OnLoad();
});