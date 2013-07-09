$(function () {
	$("input[data-dialog-action]").click(function () {
		var id = $(this).data("dialog-action");
		var url = $(this).data("url");
		var form = $("#" + id);
		form.find("form").attr("action", url);
		$("#" + id).dialog({
			modal: true,
			zIndex: 10000,
			autoOpen: true,
			width: '350',
			resizable: false,
		});
	});
});