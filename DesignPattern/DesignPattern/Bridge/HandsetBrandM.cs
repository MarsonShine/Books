using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignPattern.Bridge
{
    class HandsetBrandM : HandsetBrand
    {
        public override void Run()
        {
            _soft.Run();
        }
    }
}
