package decorators

import "fmt"

type Mocha struct {
	decorator   Beverager
	cost        int32
	description string
}

func NewMocha(b Beverager) Beverager {
	m := Mocha{
		decorator:   b,
		cost:        60,
		description: "Mocha",
	}

	return &m
}

func (m Mocha) Cost() int32 {
	fmt.Println("This is Mocha...")
	return m.cost + m.decorator.Cost()
}

func (m Mocha) GetDescription() string {
	fmt.Println("This is Mocha...")
	return m.description + " and " + m.decorator.GetDescription()
}
