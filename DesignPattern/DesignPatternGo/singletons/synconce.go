package singletons

import (
	"fmt"
	"sync"
)

var once = &sync.Once{}

func getInstanceBySyncOnce() *single {
	if singletonIntance == nil {
		once.Do(func() {
			fmt.Println("Creating single instance now.")
			singletonIntance = &single{}
		})
	} else {
		fmt.Println("Single instance already created.")
	}
	return singletonIntance
}

func StartBySyncOnce() {
	for i := 0; i < 30; i++ {
		go getInstanceBySyncOnce()
	}
}
