#include <string>

class SmallInt
{
    friend SmallInt operator+(const SmallInt&, const SmallInt &);
private:
    /* data */
    std::size_t val;
public:
    SmallInt(int i = 0) : val(i) {};
    ~SmallInt();

    operator int() const { return val; }
};

int main() {
    SmallInt s1, s2;
    SmallInt s3 = s1 + s2;
    int i = s3 + 0; // 二义性
}