package commands

type device interface {
	on()
	off()
}
