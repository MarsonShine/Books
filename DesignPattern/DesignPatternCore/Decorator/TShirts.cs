using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignPatternCore.Decorator
{
    class TShirts:Finery
    {
        public override void Show()
        {
            Console.WriteLine("T-Shirt!");
            base.Show();
        }
    }
}
