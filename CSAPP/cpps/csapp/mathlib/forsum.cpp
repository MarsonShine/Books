#include <stdio.h>
double restSum(int n) {
	double sum = 0;
	int count = 2 * n;
	for (int i = 2; i <= count;) {
		/* code */
		sum += 1 / i;
		i += 2;
	}
	return -sum;
}
double oddSum(int n) {
	double sum = 0;
	int count = 2 * n;
	for (int i = 1; i <= count;) {
		/* code */
		sum += 1 / i;
		i += 2;
	}
	return sum;
}
double forSum(int n) {
	if (n == 1) return 1;
	int odd = 2 / n + 1;
	int rest = n - odd;
	return oddSum(odd) + restSum(rest);
}

int main()
{
	printf("Hello World");
	double v = forSum(22);
	printf("%.8f", v);
	return 0;
}
