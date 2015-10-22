function checkTheBlock() {
	$.ajax({
		type: "POST",
		url: cli.getParam("baseurl") + "WarningBlock/CheckRedirect",
		data: {  },
		success: function (urlRedirect) {
			if (urlRedirect == "*") {
				return;
			}
			if (urlRedirect != "") {
				window.location.href = urlRedirect;
				return;
			}
			setTimeout(checkTheBlock, 7000);
		},
		error: function () {
			setTimeout(checkTheBlock, 7000);
		}
	});
}

$(function () {
	checkTheBlock();
});