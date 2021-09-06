package observers

import "fmt"

type StatDisplay struct {
	maxTemp     float32
	minTemp     float32
	tempSum     float32
	numReadings int
}

func NewStatDisplay(wd WeatherData) Observer {
	sd := StatDisplay{maxTemp: 0.0, minTemp: 200, tempSum: 0.0}
	return &sd
}

func (sd *StatDisplay) update(temp float32, humidity float32, pressure float32) {
	sd.tempSum = sd.tempSum + temp
	sd.numReadings = sd.numReadings + 1

	if temp > sd.maxTemp {
		sd.maxTemp = temp
	}

	if temp < sd.minTemp {
		sd.minTemp = temp
	}

	// Another way of achieving multiple inheritance
	var d Displayer = sd
	d.display()
}

func (sd *StatDisplay) display() {
	fmt.Printf("Avg/Max/Min temperature = %0.1f/%0.1f/%0.1f\n", (sd.tempSum / float32(sd.numReadings)), sd.maxTemp, sd.minTemp)
}
