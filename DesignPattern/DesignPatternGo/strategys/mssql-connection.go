package strategys

import "fmt"

type MSSQLConnection struct {
	connectionString string
}

func (c MSSQLConnection) Connect() {
	fmt.Println("SQL Server connection..." + c.connectionString)
}
