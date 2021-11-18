#include <iostream>
#include <string>
using std::string;

void better()
{
    string value;
    for (string buffer; std::cin >> buffer; value += buffer);
    std::cout << "string concat: " << value << std::endl;

    for (string buffer; std::cin >> buffer; value += " " + buffer);
    std::cout << "separator is empty: " << value << std::endl;
}

int main()
{
    // string s1, s2, s3, s4;
    // std::cin >> s1 >> s2 >> s3 >> s4;
    // std::cout << "string concat: " << s1 + s2 + s3 + s4 << std::endl;

    // string separator = " ";
    // std::cout << "separator is empty: " << s1 + separator + s2 + separator + s3 + separator + s4 << std::endl;

    better();
    return 0;
}