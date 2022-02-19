#include <iostream>
#include <string>
#include <vector>
#include <list>
using std::list; using std::vector; using std::cout; using std::endl;

int main()
{
    list<int> ilst(5, 4);
    vector<int> ivc(5, 5);

    // list<int> to vector<double>
    vector<double> dvc(ilst.begin(), ilst.end());
    for (auto i: ilst) cout << i << " ";
    cout << endl;

    // vector<int> to vector<double>
    vector<double> dvc2(ivc.begin(), ivc.end());
    for (auto i : ivc) cout << i << " ";
    cout << endl;
    for (auto d : dvc2) cout << d << " ";

    return 0;
}