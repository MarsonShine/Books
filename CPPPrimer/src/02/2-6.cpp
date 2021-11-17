#include<iostream>
#include<string>
#include<Sales_data.h>

int main() 
{
    Sales_data data1, data2;
    // 进行读取操作
    if (data1.bookNo == data2.bookNo)
    {
        unsigned totalCnt = data1.units_sold + data2.units_sold;
        double totalRevenue = data1.revenue + data2.revenue;
        if (totalCnt != 0)
            std::cout << totalRevenue/totalCnt << std::endl;
        else
            std::cout << "no sales" << std::endl;

        return 0;
    } else {
        std::cerr << "Data must refer to the same bookNo" << std::endl;
        return -1;
    }
}