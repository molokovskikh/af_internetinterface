//обновление графика сервисных работ при смене даты
var servicemenFunc = function() {
	$(".servicemen td.time").on("mouseover", function() {
		var cl = $(this).attr("class").split(" ")[0];
		$(".servicemen thead th." + cl).addClass("hover");
	});

	$(".servicemen td").on("mouseout", function() {
		$(".servicemen th, .servicemen td").removeClass("hover");
	});

	$(".servicemen td.time").on("click", function () {
		var val = $(this).html();
		var picker = $(".timepicker");
		picker.val(val);
		picker.parent().find("input[type='hidden']").val($('.datepicker').val() + " " + val);
		$(".servicemen td").removeClass("active");
		$(this).addClass("active");
		var cl = $(this).attr("class").split(" ")[0];
		var id = cl.substr("employee".length);
		console.log(id);
		$("#ServiceManDropDown").val(id);

		TimePickerValue = $(".timepicker").val();
		ServiceManDropDownValue = $("#ServiceManDropDown").val();
	});

	$(".servicemen .datepicker").get(0).onchange = refreshConnectionTable.bind($(".servicemen .datepicker").get(0));
}

//обновление графика сервисных работ при смене даты
var refreshConnectionTable = function () {
	console.log(this);
	var date = $(this).val();
	$(".datepicker").each(function () {
		$(this).val(date);
	});

	$.ajax({
		type: "POST",
		url: cli.getParam("baseurl") + "/ConnectionTeam/ConnectionTable",
		data: { date: date }
	}).done(function (msg) {
		var div = msg.toHTML();
		$(div).find("form").remove();
		$(".servicemen").find("table").remove();
		$(".servicemen .wrapper").append($(div).find(".wrapper").html());
		//$(".datepicker").datepicker({format : "dd.mm.yyyy", startView : "startView"});
		servicemenFunc();

	});
}
servicemenFunc();

$(".ConnectionTeam  .btn-green").on("click", function () {

	$(".ConnectionTeam input, .ConnectionTeam select").each(function () {
		$(this).removeAttr("disabled");
	});
	//$(".ConnectionTeam form").submit();
	return true;
})

$(".print").on("click",function() {
	window.print();
})