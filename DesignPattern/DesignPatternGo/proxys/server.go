package proxys

type server interface {
	requestHandle(string, string) (int, string)
}
