using System;
using System.Collections.Generic;
using System.Text;

namespace CSharpGuide.LanguageVersions._8._0.demo
{
    // Refference jonskeet's blog
    // https://codeblog.jonskeet.uk/2019/05/25/lying-to-the-compiler/
    public sealed class Address
    {

    }

    public sealed class Person {
        public string Name { get; }
        public Address HomeAddress { get; }

        public Person(string name, Address homeAddress)
        {
            Name = name ??
                throw new ArgumentNullException(nameof(name));
            HomeAddress = homeAddress ??
                throw new ArgumentNullException(nameof(homeAddress));
        }
    }

    public sealed class Delivery
    {
        public Person Recipient { get; }
        public Address Address { get; }
        //public Delivery(Person recipient)
            //: this(recipient, recipient?.HomeAddress) 发生警告
        public Delivery(Person recipient)
            : this(recipient, recipient?.HomeAddress!)    //这里调用 recipient 对象可能会 null，所以要在用 ?. 但是用这个操作符会导致编译器在 null 检查下提示 null 警告。所以要加 ! 操作符，来告诉编译器这个值不会为 null，即真是情况是可以为 null 的，即对编译器 CLR 撒谎
        {
        }

        public Delivery(Person recipient, Address address)
        {
            Recipient = recipient ??
                throw new ArgumentNullException(nameof(recipient));
            Address = address ??
                throw new ArgumentNullException(nameof(address));
        }
    }
}
