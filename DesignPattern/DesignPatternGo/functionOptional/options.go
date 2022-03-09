package functionoptional

type Options func(*options)

type options struct {
	userName string
	age      int
	address  string
	idcard   string
}

func WithUserName(userName string) Options {
	return func(o *options) {
		o.userName = userName
	}
}

func WithAge(age int) Options {
	return func(o *options) {
		o.age = age
	}
}
func WithAddress(address string) Options {
	return func(o *options) {
		o.address = address
	}
}
func WithIdcard(idcard string) Options {
	return func(o *options) {
		o.idcard = idcard
	}
}
