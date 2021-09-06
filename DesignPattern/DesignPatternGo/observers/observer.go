package observers

type displayerObserver interface {
	Displayer
	Observer
}

type Displayer interface {
	display()
}

type Observer interface {
	update(tempreature float32, humidity float32, pressure float32)
}
