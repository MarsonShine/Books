package strategys

import "fmt"

type RedHeadDuck struct {
	Duck
}

// NewRedheadDuck : constructor
func NewRedheadDuck() IDuck {
	return &Duck{flyer: FlyWithWings{}, Quacker: Quack{}}
}

// Display :
func (m *RedHeadDuck) Display() {
	fmt.Println("I'm a real Red Head duck")
}
