var ConnectedHouses = function() {
	var _this = this;
	this.UrStreetlList = cli.getParam("baseurl") + "";

	this.UrlHouseList = cli.getParam("baseurl") + "ConnectedHouses/HouseListGet";
	this.UrlHouseGet = cli.getParam("baseurl") + "ConnectedHouses/HouseGet";
	this.UrlHouseAdd = cli.getParam("baseurl") + "ConnectedHouses/HouseSet";
	this.UrlHouseUpdate = cli.getParam("baseurl") + "ConnectedHouses/HouseSet";
	this.UrlHouseDelete = cli.getParam("baseurl") + "ConnectedHouses/HouseDelete";

	this.CssSelectorOfConnectedHouseId = "[name='model.Id']";
	this.CssSelectorOfConnectedHouseNumber = "[name='model.House']";
	this.CssSelectorOfStreet = "[name='model.Street']";
	this.CssSelectorOfDisabled = "[name='model.Disabled']";
	this.CssSelectorOfComment = "[name='model.Comment']";

	this.TService = function() {

		this.StreetListGet = function(regionId, funcSuccess, funcError) {
			if (regionId != undefined) {
				$.ajax({
					url: _this.UrlHouseAdd,
					type: 'GET',
					dataType: "json",
					data: JSON.stringify({ "id": regionId }),
					contentType: 'application/json; charset=utf-8',
					success: function(data) {
						if (funcSuccess != undefined) {
							funcSuccess(data);
						}
					},
					error: function(data) {
						if (funcError != undefined) {
							funcError(data);
						}
					}
				});
			}
		}

		this.HouseGet = function(houseId, funcSuccess, funcError) {
			if (houseId != undefined) {
				$.ajax({
					url: _this.UrlHouseGet,
					type: 'POST',
					dataType: "json",
					data: JSON.stringify({ "id": houseId }),
					contentType: 'application/json; charset=utf-8',
					success: function(data) {
						if (funcSuccess != undefined) {
							funcSuccess(data);
						}
					},
					error: function(data) {
						if (funcError != undefined) {
							funcError(data);
						}
					}
				});
			}
		}

		this.HouseListGet = function(streetId, funcSuccess, funcError) {
			if (streetId != undefined) {
				$.ajax({
					url: _this.UrlHouseAdd,
					type: 'GET',
					dataType: "json",
					data: JSON.stringify({ "id": streetId }),
					contentType: 'application/json; charset=utf-8',
					success: function(data) {
						if (funcSuccess != undefined) {
							funcSuccess(data);
						}
					},
					error: function(data) {
						if (funcError != undefined) {
							funcError(data);
						}
					}
				});
			}
		}

		this.HouseAdd = function(house, funcSuccess, funcError) {

			if (house != undefined) {
				$.ajax({
					url: _this.UrlHouseAdd,
					type: 'POST',
					dataType: "json",
					data: JSON.stringify(house),
					contentType: 'application/json; charset=utf-8',
					success: function(data) {
						if (funcSuccess != undefined) {
							funcSuccess(data);
						}
					},
					error: function(data) {
						if (funcError != undefined) {
							funcError(data);
						}
					}
				});
			}
		}

		this.HouseUpdate = function(house, funcSuccess, funcError) {
			if (house != undefined) {
				$.ajax({
					url: _this.UrlHouseUpdate,
					type: 'POST',
					dataType: "json",
					data: JSON.stringify(house),
					contentType: 'application/json; charset=utf-8',
					success: function(data) {
						if (funcSuccess != undefined) {
							funcSuccess(data);
						}
					},
					error: function(data) {
						if (funcError != undefined) {
							funcError(data);
						}
					}
				});
			}
		}

		this.HouseDelete = function(houseId, funcSuccess, funcError) {
			if (houseId != undefined) {
				$.ajax({
					url: _this.UrlHouseDelete,
					type: 'POST',
					dataType: "json",
					data: JSON.stringify({ 'id': houseId }),
					contentType: 'application/json; charset=utf-8',
					success: function(data) {
						if (funcSuccess != undefined) {
							funcSuccess(data);
						}
					},
					error: function(data) {
						if (funcError != undefined) {
							funcError(data);
						}
					}
				});
			}
		}

	}

	this.TuiBase = function() {

		this.ModelOfHouseGet = function() {
			rawStreet = $(_this.CssSelectorOfStreet).val();
			rawStreet = rawStreet == undefined || rawStreet === "" ? 0 : parseInt(rawStreet);

			rawHouse = $(_this.CssSelectorOfHouse).val();
			rawHouse = rawHouse == undefined || rawHouse === "" ? 0 : parseInt(rawHouse);

			rawDisabled = $(_this.CssSelectorOfDisabled).val();
			rawDisabled = rawDisabled == undefined || rawDisabled === "" ? false : rawDisabled === true;

			rawComment = $(_this.CssSelectorOfComment).val();

			var model = {};
			model['Street'] = rawStreet;
			model['House'] = rawHouse;
			model['Disabled'] = rawDisabled;
			model['Comment'] = rawComment;
			return model;
		}
		this.ModelOfHouseSet = function(model) {
			$(_this.CssSelectorOfStreet).val(model['Street']);
			$(_this.CssSelectorOfConnectedHouseId).val(model['Id']);
			$(_this.CssSelectorOfConnectedHouseNumber).val(model['House']);
			$(_this.CssSelectorOfDisabled).val(String(model['Disabled']));
			$(_this.CssSelectorOfComment).val(model['Comment']);

			$("#searchClientLink").attr("href", String.format($("#searchClientLink").attr("servHref"), model['Street'], model['House']))
		}
	}

	this.TuiEvents = function() {
		this.LoadModel = function(id) {
			_this.Service.HouseGet(id, function(data) {
				_this.UiBase.ModelOfHouseSet(data);
			}, function() {
				_this.ShowMessageError("Ошибка при получении данных с сервера");
			});
		}
	}
	this.ShowMessageSuccess = function(message) {
		$('.server-message').html("");
		$('.server-message').prepend('<div class="col-md-12 alert alert-success">' + message + '</div>');
	}

	this.ShowMessageError = function(message) {
		$('.server-message').html("");
		$('.server-message').prepend('<div class="col-md-12 alert alert-danger errorFixed">' + message
			+ '<button type="button" class="close" data-dismiss="alert" aria-label="Close"><span aria-hidden="true">&times;</span></button></div>');
	}

	this.CleanMessage = function() {
		$('.server-message').html("");
	}

	this.UiEvents = new this.TuiEvents();
	this.UiBase = new this.TuiBase();
	this.Service = new this.TService();
};
var ConnectedHouse = new ConnectedHouses();


function checkTneHouseNumberOnPackageForm() {
	ConnectedHouse.CleanMessage();
	if ($("[name='numberFirst']").val() == undefined && $("[name='numberLast']").val() != undefined
			|| $("[name='numberFirst']").val() === "" && $("[name='numberLast']").val() !== ""
			|| $("[name='numberLast']").val() !== "" && parseInt($("[name='numberFirst']").val()) >= parseInt($("[name='numberLast']").val())
	) {
		ConnectedHouse.ShowMessageError("Диапазон номеров задан неверно!");
	}
}

function updateStreetValueOnPackageForm(id) {
	$("[name='streetId']").val(id);
}

$(function() {
	$(".connectedHouses input").keyup(function() {
		$(".connectedHouses a").removeClass("selected");
		if ($(this).val() != "") {
			var curThis = this;
			$(".connectedHouses a[idkey='" + $(this).attr("idkey") + "']").each(function () {
				if ($(this).html() !== "") {
					if ($(this).html().toLowerCase().indexOf($(curThis).val().toLowerCase()) !== -1) {
						$(this).addClass("selected");
					}
				}
			});
		}
	});
	$("input#streetKeySearch").keyup(function() {
		$("tr.street").addClass("hid");
		if ($(this).val() != "") {
			var curThis = this;
			$(".connectedHouses tr.street strong.c-pointer").each(function () {
				if ($(this).html() !== "") {
					if ($(this).html().toLowerCase().indexOf($(curThis).val().toLowerCase()) !== -1) {
						$($(".connectedHouses tr.street[skey='" + $(this).parents("tr.street").attr("skey") + "']")).removeClass("hid");
					}
				}
			});
		} else {
			$(".connectedHouses tr.street strong.c-pointer").each(function() {
				$(".connectedHouses tr.street").removeClass("hid");
			});
		}
	});


});