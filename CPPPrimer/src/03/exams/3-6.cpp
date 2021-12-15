#include <iostream>
#include <string>
using std::string;
using std::cin;
using std::cout;
using std::endl;

int main()
{
    string in;
    while (cin >> in)
    {
        for (auto &s : in)
        {
            s = 'X';
        }
        cout << in << endl;
    }
    
    return 0;
}