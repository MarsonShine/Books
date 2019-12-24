using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignPatternCore.Decorator
{
    class BigTrouser:Finery
    {
        public override void Show()
        {
            Console.WriteLine("跨裤！");
            base.Show();
        }
    }
}
