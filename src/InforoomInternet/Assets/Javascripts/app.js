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
});