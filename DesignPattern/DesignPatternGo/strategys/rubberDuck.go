package strategys

import "fmt"

type RubberDuck struct {
	Duck
}

func NewRubberDuck() IDuck {
	return &RubberDuck{
		Duck{
			flyer:   FlyNoWay{},
			Quacker: Squeak{}}}
}

// Display :
func (m *RubberDuck) Display() {
	fmt.Println("I'm a rubber duckie")
}
