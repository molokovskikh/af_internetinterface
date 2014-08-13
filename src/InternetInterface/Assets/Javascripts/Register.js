$(function () {
	$("#RegistrationForm").submit(function () {
		var form = this;
		var validator = $(form).data("validator");
		if (validator) {
			validator.settings.submitHandler = function () {
				var submit = $(form).find("input[type=submit],button[type=submit]");
				submit.prop("disabled", true);
				$.ajax({
					url: "/Register/CheckClient",
					type: "POST",
					cache: false,
					data: $("#RegistrationForm").serialize(),
				}).success(function (data) {
					if (data) {
						$("<p>" + data + "</p>").dialog({
							modal: true,
							buttons: {
								"Продолжить": function () {
									form.submit();
								},
								"Отменить": function () {
									submit.prop("disabled", false);
									$(this).dialog("destroy");
								}
							}
						});
					}
					else {
						form.submit();
					}
				}).error(function () {
					form.submit();
				});
			}
		}
	});

	$.validator.addMethod(
		'reg',
		function (value, element, param) {
			return this.optional(element) || new RegExp(param).test(value);
		},
		'Не соответствует требуемому выражению.'
	);

	var base = ko.bindingHandlers.value.init;
	ko.bindingHandlers.value.init = function (element, valueAccessor, allBindingsAccessor) {
		base(element, valueAccessor, allBindingsAccessor);
		if (valueAccessor().validation)
			$(element).rules("add", valueAccessor().validation);
	}
	ko.extenders.validation = function (target, validation) {
		target.validation = validation;
		return target;
	};
	var Client = function () {
		var self = this;
		self.idDocType = ko.observable();
		self.idDocName = ko.observable().extend({
			validation: {
				required: function () { return self.idDocType() == 1; }
			}
		});
		self.passportSeries = ko.observable().extend({
			validation: {
				reg: {
					param: "^(\\d{4})?$",
					depends: function () {
						return self.idDocType() == 0;
					}
				},
				messages: {
					reg: "Неправильный формат серии паспорта (4 цифры)"
				}
			}
		});
		self.passportNumber = ko.observable().extend({
			validation: {
				reg: {
					param: "^(\\d{6})?$",
					depends: function () {
						return self.idDocType() == 0;
					}
				},
				messages: {
					reg: "Неправильный формат номера паспорта (6 цифр)"
				}
			}
		});
	}
	var client = new Client();
	//window.cl = client;
	if ($("#RegistrationForm").length > 0)
		ko.applyBindings(client, $("#RegistrationForm").get(0));
	if ($("#clientEditForm").length > 0)
		ko.applyBindings(client, $("#clientEditForm").get(0));
});

function SelectHouse(item, chHouse) {
	var regionCode = $(item).val();
	if (regionCode == undefined) {
		regionCode = item;
	}
	$('#clientCityName').val(regions[regionCode]);
	$.ajax({
		url: "/Register/HouseSelect",
		type: "GET",
		cache: false,
		data: { regionCode: regionCode, chHouse: chHouse },
		success: function (data) {
			$('#SelectHouseTD').empty();
			$('#SelectHouseTD').append(data);
		}
	});
}

function registerHouse() {
	var req = {};
	req.Street = $('#house_Street').val();
	req.Number = $('#house_Number').val();
	req.Case = $('#house_Case').val();
	req.RegionId = $('#house_Region_Id').val();
	$.post("/Register/RegisterHouse", req, function (data) {
		if (data.Id > 0) {
			$('#houses_select').append($('<option value="' + data.Id + '">' + data.Name + '</option>'));
			$('#register_house').trigger('reveal:close');
		} else {
			$('#houseErrorMessageSpan').text(data.Name);
		}
	});
}