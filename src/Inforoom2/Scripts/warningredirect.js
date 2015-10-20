function checkTheBlock() {
	$.ajax({
		type: "POST",
		url: cli.getParam("baseurl") + "WarningBlock/CheckRedirect",
		data: {  },
		success: function (urlRedirect) {
			if (urlRedirect != "") {
				window.location.href = urlRedirect;
			}
			console.log("Проверка блокировки", urlRedirect);
		}
	});
	setTimeout(checkTheBlock, 7000);
}

$(function () {
	checkTheBlock();
});