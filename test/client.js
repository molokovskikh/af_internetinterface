define(["register"], function(module) {
	test("knockout модель должна загружать значение из формы", function() {
		$("form").validate();
		var client = new module.Model();
		ko.applyBindings(client);
		equal(client.passportSeries(), "45871");
	});
});
