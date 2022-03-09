package functionoptional

import "time"

type Option interface {
	apply(*option)
}

type option struct {
	timeout time.Duration
	cache   bool
}

type optionFunc func(*option)

func (f optionFunc) apply(o *option) {
	f(o)
}

func WithTimeout(timeout time.Duration) optionFunc {
	return func(o *option) {
		o.timeout = timeout
	}
}

func WithCache(cache bool) optionFunc {
	return func(o *option) {
		o.cache = cache
	}
}
