package singletons

// 一般的单利模式有两种
// 1 饿汉模式
// 2 懒汉模式

// 1 在包引用的时候就被初始化了
type singleton struct{}

var ins *singleton = &singleton{}

func getSingleton() *singleton {
	return ins
}

// 2 懒汉模式，但是不是线程安全的
var ins2 *singleton

func getSingleton2() *singleton {
	if ins2 == nil {
		ins2 = &singleton{}
	}
	return ins2
}
