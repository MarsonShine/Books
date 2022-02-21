#include <string>
#include <iostream>

using std::string;
using std::cout;
using std::endl;

void serach1()
{
    string numbers{ "123456789" };
    string alphabet { "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ" };
    string str{ "ab2c3d7R4E6" };

    cout << "numeric characters: ";
    for (int pos = 0; (pos = str.find_first_of(numbers, pos)) != string::npos; ++pos) {
        cout << str[pos] << " ";
    }

    cout << "\nalphabetic characters: ";
    for (int pos = 0; (pos = str.find_first_of(alphabet, pos)) != string::npos; ++pos)
        cout << str[pos] << " ";
    cout << endl;
}

void search2() {
    string numbers{ "123456789" };
    string alphabet { "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ" };
    string str{ "ab2c3d7R4E6" };

    cout << "numeric characters: ";
    for (int pos = 0; (pos = str.find_first_not_of(alphabet, pos)) != string::npos; ++pos) {
        cout << str[pos] << " ";
    }

    cout << "\nalphabetic characters: ";
    for (int pos = 0; (pos = str.find_first_not_of(numbers, pos)) != string::npos; ++pos)
        cout << str[pos] << " ";
    cout << endl;
}

void serach_highperformance() {
    string str{ "ab2c3d7R4E6" };
    for (size_t i = 0; i < str.size(); i++)
    {
        if (isdigit(str[i]))
        {
            cout << str[i] << " ";
        }
        else if (isalpha(str[i]))
        {
            cout << str[i] << " ";
        }
    }
}

int main() {
    search2();
    return 0;
}