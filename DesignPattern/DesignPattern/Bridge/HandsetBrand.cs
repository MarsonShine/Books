using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignPattern.Bridge
{
    abstract class HandsetBrand
    {
        protected HandsetSoft _soft;
        public void SetHandsetSoft(HandsetSoft soft)
        {
            _soft = soft;
        }
        abstract public void Run();
    }
}
