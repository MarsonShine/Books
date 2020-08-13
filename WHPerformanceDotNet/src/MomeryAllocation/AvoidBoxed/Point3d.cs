using System;
using System.Collections.Generic;
using System.Text;

namespace MomeryAllocation.AvoidBoxed
{
    struct Point3d
    {
        public double x;
        public double y;
        public double z;
        public string Name { get; set; }
    }

    class Vector2
    {
        private Point3d location;
        public Point3d Location { get; set; }
        public ref Point3d RefLocation => ref location;

        public int Magnitude { get; set; }
    }
}
