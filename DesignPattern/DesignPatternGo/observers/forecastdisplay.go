package observers

import "fmt"

type ForecastDisplay struct {
	currPressure float32
	lastPressure float32
	//weatherData  WeatherData
}

func NewForecastDisplay(wd WeatherData) displayerObserver {
	fd := ForecastDisplay{}
	return &fd
}

func (fd *ForecastDisplay) update(temperature float32, humidity float32, pressure float32) {
	fd.lastPressure = fd.currPressure
	fd.currPressure = pressure

	fd.display()
}

func (fd *ForecastDisplay) display() {
	fmt.Print("Forecast : ")
	if fd.currPressure > fd.lastPressure {
		fmt.Println("Improving weather on the way!")
	} else if fd.currPressure < fd.lastPressure {
		fmt.Println("Watch out for cooler, rainy weather!")
	} else {
		fmt.Println("More of the same.")
	}
}
