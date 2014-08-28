$(function () {
	$(".imagePart a").colorbox();
	$("a.colorbox").colorbox();
	$("#residence").autocomplete({
		source: "/main/assist",
		delay: 100,
		minChars: 2,
		selectFirst: true,
		matchSubset: 1,
		cacheLength: 10,
		selectOnly: true,
		maxItemsToShow: 10
	});
	$("form.validable").each(function () {
		$(this).validate();
	});

	var selfUrl = decodeURIComponent(window.location.href);
	if (endsWith(selfUrl, "Login/LoginPage"))
		selfUrl = selfUrl.replace("Login/LoginPage", "PrivateOffice/IndexOffice");
	setupContentMenu(selfUrl);
});