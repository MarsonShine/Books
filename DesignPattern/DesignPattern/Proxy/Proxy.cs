using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignPattern.Proxy
{
    class Proxy : IGiveGift
    {
        Pursuit ps;
        public Proxy(SchoolGirl sg)
        {
            ps = new Pursuit(sg);
        }

        public void GiveChocolate()
        {
            ps.GiveChocolate();
        }

        public void GiveDolls()
        {
            ps.GiveDolls();
        }

        public void GiveFlowers()
        {
            ps.GiveFlowers();
        }
    }
}
