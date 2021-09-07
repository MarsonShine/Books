package mediatos

type train interface {
	arrive()
	depart()
	permitArrival()
}
