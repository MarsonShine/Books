package strategys

type IDbConnection interface {
	Connect()
}

type DbConnection struct {
	db IDbConnection
}

func (conn *DbConnection) DbConnect() {
	conn.db.Connect()
}
