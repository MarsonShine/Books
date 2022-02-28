#include <vector>
#include <string>
#include <memory>
#include <iostream>
#include "strblob.h"

using std::string; using std::vector; using std::cout; using std::endl;

int main()
{
    StrBlob b1;
    {
        StrBlob b2 = { "a", "b", "c"};
        b1 = b2;
        b2.push_back("d");
    }
}