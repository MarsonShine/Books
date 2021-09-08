package adaptors

import "fmt"

type mac struct{}

func (m *mac) insertIntoLightingPort() {
	fmt.Println("USB connector is plugged into mac machine.")
}
