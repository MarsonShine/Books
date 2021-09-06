package strategys

import "fmt"

type Flyer interface {
	fly()
}

// FlyWithWings : Class for flying
type FlyWithWings struct {
}

func (f FlyWithWings) fly() {
	fmt.Println("I am flying")
}

// FlyNoWay : Class for not flying
type FlyNoWay struct {
	NoOfWings int
}

func (f FlyNoWay) fly() {
	fmt.Println("I can NOT fly")
}
