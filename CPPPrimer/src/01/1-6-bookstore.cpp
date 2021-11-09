#include <iostream>
#include<Sales_item.h>
int main() {
    Sales_item total;   //保存下一条交易记录的变量
    // 读入第一条交易记录，确保有数据可以处理
    if (std::cin >> total) {
        Sales_item trans;
        while (std::cin >> trans)
        {
            if (total.isbn() == trans.isbn())
            {
                total += trans; // 更新总销售值
            } else {
                // 打印前一本书的销售结果
                std::cout << total << std::endl;
                total = trans;  // total 现在表示下一本书的销售额
            }
        }
        std::cout << total << std::endl;    // 打印最后一本书
    } else {
        // 没有输入，警告读者
        std::cerr << "No data?!" << std::endl;
        return -1;
    }
    return 0;
}
