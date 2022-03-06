class Base {
public:
    static void statmem();
};
class Derived : public Base {
    void f(const Derived&);
};
void Derived::f(const Derived &d)
{
    Base::statmem();
    Derived::statmem();
    d.statmem();
    statmem();
}