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

	$(markerDates).unbind("change").change(function() {
		Clean();
	});
}

function onPaymentCreateForm() {
	var noMistakes = true;
	var html = "<ul>";
	var valSum = parseFloat($("#BankPaymentSum").val().replace(',',"."));

	if (String(valSum) === "NaN" || typeof valSum != "number") {
		html = "<li>Укажите сумму</li>";
		noMistakes = false;
	}
	if (valSum <= 0) {
		html = "<li>Сумма должна быть больше 0</li>";
		noMistakes = false;
	}
	if (valSum > 100000) {
		html = "<li>Сумма должна быть больше 100 000</li>";
		noMistakes = false;
	}
	html += "<ul>";
	$("#paymentMoveMessage").html(html);
	return noMistakes;
}

function getPaymentReciverUpdateList() {
	var currentVal = $("#clientReciverId").val();
	if (currentVal != undefined && currentVal !== "") {
		var objWithRecipient = $("#clientReciverMessage [onclick*='getPaymentClientIdUpdate']");
		objWithRecipient.each(function() {
			if (currentVal === $(this).html()) {
				var recipient = $(this).attr('recipient');
				if (recipient != undefined && recipient !== "") {
					$("#RecipientDropDown option").removeClass("hid");
					$("#RecipientDropDown option").addClass("hid");
					var shownObj = $("#RecipientDropDown option[value='" + recipient + "']");
					if (shownObj.length > 0) {
						$(shownObj).removeClass("hid");
					}  
					if ($("#RecipientDropDown option:selected").val() !== String(recipient)) {
						$("#RecipientDropDown").val("");
					}

				}
			}
		});
	}
}

$(function() {
	OnLoad();

	$("#clientReciverId").change(
		function() {
			getPaymentReciver('#clientReciverId', '#clientReciverMessage', false, 2, function() {
				getPaymentReciverUpdateList();
				$($("#clientReciverMessage [onclick*='getPaymentClientIdUpdate']")).click(function() {
					getPaymentReciverUpdateList();
				});
			})
		}
	);
});