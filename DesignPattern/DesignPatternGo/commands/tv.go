package commands

import "fmt"

type tv struct {
}

func (t *tv) on() {
	fmt.Println("Turning tv on")
}

func (t *tv) off() {
	fmt.Println("Turning tv off")
}
