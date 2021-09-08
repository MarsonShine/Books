package adaptors

func Start() {
	client := &client{}
	mac := &mac{}

	client.insertLightningConnectorIntoComputer(mac)

	windowsAdaptor := newWindowsAdaptor()
	client.insertLightningConnectorIntoComputer(&windowsAdaptor)
}
