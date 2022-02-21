#include <vector>
#include <string>
#include <iostream>
using std::vector; using std::string; using std::cout; using std::endl; 

int main()
{
    vector<char> vcs(10,'c');
    string s(vcs.begin(),vcs.end());
    cout << s << endl;
    vcs[2] = 'm';
    cout << s << endl;

    string s1;
    s1.reserve(120);
    cout << s1.capacity() << endl << s1.size() << endl;
    return 0;
}