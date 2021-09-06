package observers

type Subject interface {
	Register(ob Observer)
	Remove(ob Observer)
	notifyAll()
}

type WeatherData struct {
	observers   []Observer
	tempreature float32
	humidity    float32
	pressure    float32
}

func NewWeatherData() WeatherData {
	wd := WeatherData{}
	return wd
}

func (w *WeatherData) Register(ob Observer) {
	w.observers = append(w.observers, ob)
}
func (w *WeatherData) Remove(ob Observer) {
	i := findIndex(&w.observers, ob)
	w.observers = remove(w.observers, i)
}
func remove(obs []Observer, index int) []Observer {
	// 交换替换去掉最后一个
	obs[len(obs)-1], obs[index] = obs[index], obs[len(obs)-1]
	return obs[:len(obs)-1]
}
func (w *WeatherData) notifyAll() {
	for _, observer := range w.observers {
		observer.update(w.tempreature, w.humidity, w.pressure)
	}
}

func (w *WeatherData) SetMeasurements(tempreature float32, humidity float32, pressure float32) {
	w.tempreature = tempreature
	w.humidity = humidity
	w.pressure = pressure
	// w.notifyAll()
	w.measurementChanged()
}

func (w *WeatherData) measurementChanged() {
	w.notifyAll()
}

func findIndex(ods *[]Observer, key Observer) int {
	for id, ob := range *ods {
		if ob == key {
			return id
		}
	}
	return -1
}
