#include <iostream>
#include <string>
#include <vector>
#include <algorithm>
#include <map>
using std::string; using std::vector; using std::cin; using std::cout; using std::map;

int main()
{
    map<string, vector<string>> family;
    string lastname;
    string firstname;
    cin >> lastname >> firstname;
    family[lastname].push_back(firstname);
}