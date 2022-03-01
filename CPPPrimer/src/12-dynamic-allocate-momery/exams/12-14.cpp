#include <iostream>
#include <string>
#include <memory>
using std::string;

struct connection
{
    string ip;
    int port;
    connection(string _ip, int _port): ip(_ip), port(_port) { }
};
struct destination
{
    string ip;
    int port;
    destination(string _ip, int _port): ip(_ip), port(_port) { }
};

connection connect(destination* d)
{
    std::shared_ptr<connection> conn(new connection(d->ip, d->port));
    std::cout << "creating connection(" << conn.use_count() << ")" << std::endl;
    return *conn;
}

void disconnect(connection conn)
{
    std::cout << "connection close(" << conn.ip << ":" << conn.port << ")" << std::endl;
}

void end_connection(connection *conn)
{
    disconnect(*conn);
}

void f(destination& d){
    connection conn = connect(&d);
    std::shared_ptr<connection> p(&conn, end_connection);
    std::cout << "connecting now(" << p.use_count() << ")" << std::endl;
}

int main()
{
    destination dest("127.0.0.1",8080);
    f(dest);
}