package commands

func Start() {
	tv := tv{}
	tvOn := onCommand{device: &tv}
	tvOff := offCommand{device: &tv}
	tvOn.execute()
	tvOff.execute()
}
