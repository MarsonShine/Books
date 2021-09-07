package mediatos

type mediator interface {
	canArrive(train) bool
	notifyAboutDeparture()
}

func Start() {
	stattionManager := newStationManager()

	passengerTrain := &passengerTrain{
		mediator: stattionManager,
	}
	freightTrain := &freightTrain{
		mediator: stattionManager,
	}

	passengerTrain.arrive()
	freightTrain.arrive()
	passengerTrain.depart()
}
