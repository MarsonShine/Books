package strategys

import "fmt"

type MysqlConnection struct {
	connectionString string
}

func (c MysqlConnection) Connect() {
	fmt.Println("Mysql connection..." + c.connectionString)
}
