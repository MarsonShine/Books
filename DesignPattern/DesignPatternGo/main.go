package main

import (
	"fmt"
	"godesignpattern/decorators"
	"godesignpattern/mememtos"
	"godesignpattern/propertys"
	"godesignpattern/strategys"
)

func main() {
	var b decorators.Beverager
	b = decorators.NewDarkRoast()
	b = decorators.NewMocha(b)
	b = decorators.NewMocha(b)
	b = decorators.NewWhip(b)
	fmt.Printf("%d,%s", b.Cost(), b.GetDescription())

	p := decorators.NewPerson("marsonshine", decorators.NewBigTrouser(
		decorators.NewTThirts(nil),
	))
	p.Print()
	// p := decorators.NewPerson("marsonshine")

	// trouser := decorators.NewBigTrouser(p)
	// tthirts := decorators.NewTThirts(trouser)
	// println(tthirts.Show())

	strategys.Start()
	propertys.Start()
	mememtos.Start()
}
