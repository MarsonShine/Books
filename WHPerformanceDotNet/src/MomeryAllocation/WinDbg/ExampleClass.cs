namespace MomeryAllocation.WinDbg
{
    public class ExampleClass
    {
        private int _myField;

        public int MyMethod() => _myField;
    }

    public class ExampleGenericClass<T>
    {
        private T _myField;

        public T MyMethod() => _myField;
    }
}
