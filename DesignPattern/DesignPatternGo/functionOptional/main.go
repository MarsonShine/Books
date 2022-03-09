package functionoptional

func NewOptions(opts ...Options) {
	o := options{}
	for _, opt := range opts {
		opt(&o)
	}
}

func NewOption(opts ...Option) {
	o := option{}
	for _, opt := range opts {
		opt.apply(&o)
	}
}
