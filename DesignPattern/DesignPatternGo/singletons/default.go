package singletons

import (
	"fmt"
	"sync"
)

var lock = &sync.Mutex{}

type single struct{}

var singletonIntance *single

func getInstance() *single {
	if singletonIntance == nil {
		lock.Lock()
		if singletonIntance == nil {
			singletonIntance = &single{}
			fmt.Println("Creating single instance now.")
		} else {
			fmt.Println("Single instance already created.")
		}
		lock.Unlock()
	} else {
		fmt.Println("Single instance already created.")
	}
	return singletonIntance
}

func StartByLock() {
	for i := 0; i < 30; i++ {
		go getInstance()
	}
}
