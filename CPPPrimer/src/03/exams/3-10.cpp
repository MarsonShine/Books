#include <iostream>
#include <string>
using std::string;
using std::cin;
using std::cout;
using std::endl;

void each_consttype() {
    const string s = "Keep out!";
    for (auto &c : s)
    {
        
    }
    
}

int main()
{
    string in;
    while (cin >> in)
    {
        // 去除标点符号
        string out;
        for (auto &s : in)
        {
            if (!ispunct(s))
                out += s;
        }
        cout << "original characters: " << in << endl
            << "remove punct: " << out << endl;
    }
    
    return 0;
}