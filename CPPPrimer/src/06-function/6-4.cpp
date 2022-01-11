// 函数重载
int get();
double get();   // 无法仅区分返回类型不同的重载函数

int calc(int, int);
int calc(const int, const int);

int *reset(int *);
double *reset(double *);