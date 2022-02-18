package main

import (
	"encoding/json"
	"encoding/xml"
	"fmt"
)

type Visitor func(shape Shape)

type Shape interface {
	accept(Visitor)
}

type Circle struct {
	Radius int
}

type Rectangle struct {
	Width, Height int
}

func (c Circle) accept(v Visitor) {
	v(c)
}

func (r Rectangle) accept(v Visitor) {
	v(r)
}

// 独立的visitor逻辑
func JsonVisitor(shape Shape) {
	bytes, err := json.Marshal(shape)
	if err != nil {
		panic(err)
	}
	fmt.Println(string(bytes))
}

func XmlVisitor(shape Shape) {
	bytes, err := xml.Marshal(shape)
	if err != nil {
		panic(err)
	}
	fmt.Println(string(bytes))
}

func main() {
	c := Circle{10}
	r := Rectangle{100, 200}
	shapes := []Shape{c, r}
	for _, s := range shapes {
		// 同一个对象的不同处理方式
		s.accept(JsonVisitor)
		s.accept(XmlVisitor)
	}
}
