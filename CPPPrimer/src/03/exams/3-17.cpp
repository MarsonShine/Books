#include<iostream>
#include<string>
#include<vector>
using std::cout;// 命名空间申明
using std::cin;
using std::endl;
using std::string;
using std::getline;
using std::vector;

int main()
{
    string in;
    while (getline(cin,in))
    {
        cout << "origin string = '" << in << "'" << endl;

        for (unsigned i = 0; i < in.size(); i++)
        {
            if (isspace(in[i]))
            {
                in[i] = '\n';
            }
            
            in[i] = toupper(in[i]);
        }
        cout << "new string = '" << in << "'" << endl;
        
    }
    
    
    
    return 0;
}