using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignPattern.Proxy
{
    class Pursuit : IGiveGift
    {
        SchoolGirl sg;
        public Pursuit(SchoolGirl sg)
        {
            this.sg = sg;
        }
        public void GiveChocolate()
        {
            Console.WriteLine($"{sg.Name} 送你巧克力");
        }

        public void GiveDolls()
        {
            Console.WriteLine($"{sg.Name} 送你洋娃娃");
        }

        public void GiveFlowers()
        {
            Console.WriteLine($"{sg.Name} 送你鲜花");
        }
    }
}
