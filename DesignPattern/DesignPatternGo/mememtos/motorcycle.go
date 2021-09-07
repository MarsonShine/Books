package mememtos

import (
	"fmt"
	"strings"
	"time"
)

type MotorType int

const (
	Kawasaki MotorType = iota
	Honda
	Suzuki
	Ducati
)

type ServiceData struct {
	Id           int
	PersonalName string
	Model        MotorType
	Price        float32
	Date         time.Time
}

var counter *int
var dataHistoryList map[int]ServiceData

func UpdateDataChange(model *ServiceData) {
	if dataHistoryList == nil {
		dataHistoryList = make(map[int]ServiceData)
		counter = new(int)
	}
	*counter++
	model.Id = *counter
	dataHistoryList[*counter] = *model
}

func Undo() ServiceData {
	i := *counter - 1
	if i > -1 {
		*counter = i
		return dataHistoryList[i]
	}
	return dataHistoryList[*counter]
}

func Redo() ServiceData {
	i := *counter + 1
	if i <= len(dataHistoryList) {
		*counter = i
		return dataHistoryList[i]
	}
	return dataHistoryList[*counter]
}

func Start() {
	mainData := ServiceData{PersonalName: "marsonshine", Model: Honda, Price: 15200, Date: time.Now()}
	UpdateDataChange(&mainData)
	fmt.Println(mainData)
	mainData = ServiceData{PersonalName: "summerzhu", Model: Kawasaki, Price: 22350, Date: time.Now()}
	UpdateDataChange(&mainData)
	fmt.Println(mainData)

	mainData = ServiceData{PersonalName: "xixi", Model: Suzuki, Price: 52220, Date: time.Now()}
	UpdateDataChange(&mainData)
	fmt.Println(mainData)

	//undo
	mainData = Undo()
	fmt.Println("1 :", mainData)
	fmt.Println(strings.Repeat("-", 100))

	mainData = Undo()
	fmt.Println("1 :", mainData)
	fmt.Println(strings.Repeat("-", 100))

	mainData = Redo()
	fmt.Println("1 :", mainData)
	fmt.Println(strings.Repeat("-", 100))

	mainData = Redo()
	fmt.Println("1 :", mainData)
	fmt.Println(strings.Repeat("-", 100))
}
