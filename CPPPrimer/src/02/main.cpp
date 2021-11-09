#include<iostream>

int main() {
    float f = 123456789.123456789;
    printf("%f\n",f);

    f = 123456789.654321;
    printf("%f\n",f);

    double l = 12345678998765.65656;
    printf("%lf\n",l);

    // 分多行书写的字符串字面值
    std::cout << "a really, really long string literal "
                "that spans two lines" << std::endl;
    return 0;
}