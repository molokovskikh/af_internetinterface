var name = /([^/]+)\.html/.exec(window.document.URL)[1];
configPaths = {
	"jquery": "packages/jQuery.1.6.4/Content/Scripts/jquery-1.6.4",
	"jquery-ui": "packages/jQuery.UI.Combined.1.10.3/Content/Scripts/jquery-ui-1.10.2",
	"jquery-validate": "packages/jQuery.Validation.1.11.1/Content/Scripts/jquery.validate",
	"knockout": "packages/knockoutjs.2.1.0/Content/Scripts/knockout-2.1.0.debug",
	"underscore": "packages/underscore.js.1.4.4/Content/Scripts/underscore",
	"register": "src/InternetInterface/Assets/Javascripts/Register"
};
configPaths[name + "-test"] = "test/" + name;

require.config({
	baseUrl: "..",
	shim: {
		"jquery": {
			exports: "jQuery"
		},
		"knockout": {
			exports: "ko",
		},
		"jquery-validate": {
			deps: ["jquery"],
		},
		"jquery-ui": {
			deps: ["jquery"],
		},
	},
	paths: configPaths
});

require([name + "-test"], function () {
	QUnit.start();
});
