package strategys

import "fmt"

func Start() {
	mssqlConnec := MSSQLConnection{connectionString: "mssql:user:password"}
	dbconnect := DbConnection{
		db: mssqlConnec,
	}
	dbconnect.DbConnect()

	oracleConnect := OracleConnection{connectionString: "oracle:user:password"}
	dbconnect = DbConnection{
		db: oracleConnect,
	}
	dbconnect.DbConnect()

	// section 2
	mallardDuck := NewMallardDuck()
	redheadDuck := NewRedheadDuck()
	rubberDuck := NewRubberDuck()
	decoyDuck := NewDecoyDuck()

	ducks := [4]IDuck{mallardDuck, redheadDuck, rubberDuck, decoyDuck}

	for _, duck := range ducks {
		fmt.Printf("\n%T\n", duck)
		duck.Display()
		duck.PerformFly()
		duck.PerformQuack()
		duck.Swim()
	}

	donaldDuck := Duck{}
	donaldDuck.Display()
	fmt.Printf("%#v\n", donaldDuck)

	donaldDuck.SetFlyer(FlyNoWay{})
	donaldDuck.Quacker = MuteQuack{}
	fmt.Printf("%#v\n", donaldDuck)
	donaldDuck.PerformQuack()

	fly := FlyNoWay{NoOfWings: 2}         // I don't remember seeing him fly, despite having 2 wings.
	speak := Speak{Speech: "Aw, phooey!"} // One of his catch phrases
	donaldDuck.SetFlyer(fly)
	donaldDuck.Quacker = speak
	fmt.Printf("%#v\n", donaldDuck)
	donaldDuck.PerformQuack()

}
