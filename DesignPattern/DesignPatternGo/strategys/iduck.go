package strategys

import "fmt"

type IDuck interface {
	Display()
	PerformFly()
	PerformQuack()
	Swim()
}

type Duck struct {
	flyer   Flyer
	Quacker Quacker
}

func NewDuck() IDuck {
	duck := Duck{flyer: nil, Quacker: nil}
	duck.flyer = FlyNoWay{} // duck.SetFlyer(FlyNoWay{})
	duck.Quacker = MuteQuack{}
	return &duck
}

func (d *Duck) Display() {
	fmt.Println("I'm a duck.")
}

// PerformFly : allow fly
func (d *Duck) PerformFly() {
	d.flyer.fly()
}

// PerformQuack : allow quack
func (d *Duck) PerformQuack() {
	d.Quacker.quack()
}

// Swim : allow swim
func (d *Duck) Swim() {
	fmt.Println("All ducks float, even decoys!")
}

// NOTE: Getters / Setters are not idiomatic in Golang

// SetFlyer : may consider adding it to the IDuck interface
func (d *Duck) SetFlyer(flyer Flyer) {
	d.flyer = flyer
}
