$(function () {
	$("#request_Street").autocomplete({
		source: "/ConnectionRequest/StreetAutoComplete",
		delay: 100,
		minChars: 2,
		selectFirst: true,
		matchSubset: 1,
		cacheLength: 10,
		selectOnly: true,
		maxItemsToShow: 10
	});
});
