using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignPattern.Bridge
{
    class HandsetBrandN : HandsetBrand
    {
        public override void Run()
        {
            _soft.Run();
        }
    }
}
