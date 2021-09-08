package adaptors

import "fmt"

type windowsAdaptor struct {
	adaptor *windows
}

func newWindowsAdaptor() windowsAdaptor {
	return windowsAdaptor{
		adaptor: &windows{},
	}
}

func (wa *windowsAdaptor) insertIntoLightingPort() {
	fmt.Println("Adapter converts Lightning signal to USB.")
	wa.adaptor.insertIntoUSBPort()
}
