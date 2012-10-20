jQuery(function () {
	jQuery.noConflict();
	jQuery(".validateDate").rules("add", { "date": true });
});