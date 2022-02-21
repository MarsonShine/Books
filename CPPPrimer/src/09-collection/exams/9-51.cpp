// 设计一个类，有三个字段，标识年月日的日期。
// 构造函数能支持多种时间的表述方式如
// January 1,1900、1/1/1900、Jan 1 1900
#include <iostream>
#include <string>
#include <vector>
using std::string; using std::vector; using std::cout; using std::endl;

struct Date
{
private:
    unsigned year,month,day;
public:
    Date(const string& s) {
        unsigned tag;
        unsigned format;
        format = tag = 0;

        // 1/1/1900
        if (s.find_first_of("/") != string::npos)
        {
            format = 0x01;
        }
        // January 1, 1900 or Jan 1, 1900
        if ((s.find_first_of(',') >= 4 && s.find_first_of(',') != string::npos))
        {
            format = 0x10;
        }
        else {
            // Jan 1 1900
            if(s.find_first_of(' ') >= 3
                && s.find_first_of(' ')!= string::npos){
                format = 0x10;
                tag = 1;
            }
        }
        
        switch (format)
        {
        case 0x01:
            day = std::stoi(s.substr(s.find_first_of("/")));
            month = std::stoi(s.substr(s.find_first_of("/") + 1, s.find_last_of("/") - s.find_first_of("/")));
            year = std::stoi(s.substr(s.find_last_of("/")+1, 4));
            break;
        case 0x10:

        default:
            break;
        }
    }

    void print()
    {
        cout << "day:" << day << " " << "month: " << month << " " << "year: " << year;
    }
};


int main()
{
    return 0;
}