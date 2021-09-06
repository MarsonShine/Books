package strategys

import "fmt"

type MallardDuck struct {
	Duck
}

func NewMallardDuck() IDuck {
	return &MallardDuck{
		Duck{
			flyer:   FlyWithWings{},
			Quacker: Quack{},
		},
	}
}

func (m *MallardDuck) Display() {
	fmt.Println("I'm a real Mallard duck")
}
