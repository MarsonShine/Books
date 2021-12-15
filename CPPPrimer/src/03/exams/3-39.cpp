#include<iostream>
#include<string>
#include<vector>
using std::begin; using std::end; using std::cout; using std::endl; using std::vector; using std::string;

int main() {
    //比较两个字符串
    string s1("Mooophy"), s2("Pezy");
    if (s1 == s2)
    {
        cout << "same string." << endl;
    }
    else if (s1 > s2)
        cout << "Mooophy > Pezy" << endl;
    else
        cout << "Mooophy < Pezy" << endl;

    cout << "=========" << endl;
    //比较两个c风格字符串    
    const char* cs1 = "marsonshine";
    const char* cs2 = "summerzhu";
    auto result = strcmp(cs1, cs2);
    return 0;
}