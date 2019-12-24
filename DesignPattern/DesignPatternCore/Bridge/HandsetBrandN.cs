using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignPatternCore.Bridge
{
    class HandsetBrandN : HandsetBrand
    {
        public override void Run()
        {
            _soft.Run();
        }
    }
}
