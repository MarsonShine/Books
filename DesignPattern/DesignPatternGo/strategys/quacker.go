package strategys

import "fmt"

type Quacker interface {
	quack()
}

// Quack : Class for Quacking
type Quack struct {
}

func (q Quack) quack() {
	fmt.Println("quack")
}

// Squeak : Class for Squeaking
type Squeak struct {
}

func (s Squeak) quack() {
	fmt.Println("squeak")
}

// Speak : Class for Speaking
type Speak struct {
	Speech string
}

func (m Speak) quack() {
	fmt.Println(m.Speech)
}

// MuteQuack : Class for not Quacking
type MuteQuack struct {
}

func (m MuteQuack) quack() {
	fmt.Println("<< Silence >>")
}
