package decorators

import "fmt"

// 调味品
type Whip struct {
	decorator   Beverager
	description string
	cost        int32
}

func NewWhip(b Beverager) Beverager {
	w := Whip{
		b,
		"Whip",
		10,
	}
	return &w
}

// 实现
func (w Whip) Cost() int32 {
	fmt.Println("This is Whip Cost...")
	return w.cost + w.decorator.Cost()
}

func (w Whip) GetDescription() string {
	fmt.Println("this is Whip Description...")
	return w.description + " and " + w.decorator.GetDescription()
}
