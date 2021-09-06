package strategys

import "fmt"

type OracleConnection struct {
	connectionString string
}

func (c OracleConnection) Connect() {
	fmt.Println("Oracle connection..." + c.connectionString)
}
