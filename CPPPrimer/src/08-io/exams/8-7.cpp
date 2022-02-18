
#include <fstream>
#include <iostream>

#include "07-class/exams/7-21.h"
using std::ifstream; using std::ofstream; using std::endl; using std::cerr;

int main(int argc, char const *argv[])
{
    ifstream input(argv[1]);
    ofstream output(argv[2]);// 输出文件，结果保存至该文件中

    Sales_data total;
    if (read(input, total))
    {
        Sales_data trans;
        while (read(input, trans))
        {
            if (total.isbn() == trans.isbn())
                total.combine(trans);
            else
            {
                print(output, total) << endl;
                total = trans;
            }
        }
        print(output, total) << endl;
    }
    else
    {
        cerr << "No data?!" << endl;
    }
        
    return 0;
}

