$ ->
	test "", ->
		model = new AppViewModel()
		model.connectSum(300)
		equal model.minSum(), 300
		model.tariffs = [{id: 1, cost: 200}]
		model.tariff(1)
		equal model.minSum(), 300
		model.internetActivated(true)
		equal model.minSum(), 500
