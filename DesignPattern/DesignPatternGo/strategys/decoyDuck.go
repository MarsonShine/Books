package strategys

import "fmt"

type DecoyDuck struct {
	Duck
}

func NewDecoyDuck() IDuck {
	return &DecoyDuck{
		Duck{flyer: FlyNoWay{}, Quacker: MuteQuack{}}}
}

// Display :
func (m *DecoyDuck) Display() {
	fmt.Println("I'm a duck decoy")
}
