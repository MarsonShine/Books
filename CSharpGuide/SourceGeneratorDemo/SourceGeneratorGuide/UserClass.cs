using System.ComponentModel;

namespace MySourceGenerator
{
    public partial class UserClass
    {
        [AutoNotify]
        private bool _boolProp;
        [AutoNotify(PropertyName = "Count")]
        private int _intProp;
        public void UserMethod()
        {
            this.GeneratedMethod();
        }
    }
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class AutoNotifyAttribute : Attribute
    {
        public AutoNotifyAttribute() { }

        public string PropertyName { get; set; }
    }

    public partial class UserClass : INotifyPropertyChanged
    {
        public bool BoolProp
        {
            get => _boolProp;
            set
            {
                _boolProp = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UserBool"));
            }
        }

        public int Count
        {
            get => _intProp;
            set
            {
                _intProp = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Count"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
