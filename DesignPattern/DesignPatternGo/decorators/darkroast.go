package decorators

import "fmt"

// 烘培咖啡
type DarkRoast struct {
	description string
	cost        int32
}

func NewDarkRoast() Beverager {
	d := DarkRoast{
		cost:        20,
		description: "Dark Roast",
	}
	return &d
}

// 实现
func (d DarkRoast) Cost() int32 {
	fmt.Println("This is DarkRoast Cost...")
	return d.cost
}

func (d DarkRoast) GetDescription() string {
	fmt.Println("this is DarkRoast Description...")
	return d.description
}
