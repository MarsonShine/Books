using System;
using System.Collections.Generic;
using System.Text;

namespace Chapter_3_Generic
{
    public class LinkedList_Demo
    {
        private readonly List<TransportItem> transportItems;
        public LinkedList_Demo(int capacity)
        {
            transportItems = new List<TransportItem>(capacity);
            transportItems.Add(new TransportItem
            {
                Code = "TMS001",
                Id = 1,
                PointPlaces = new List<PointPlace>
                {
                    new PointPlace{ Id = 1,Type = 0,UnitId = 1,Name = "岳阳"},
                    new PointPlace{ Id = 2,Type = 1,UnitId = 2,Name = "长沙"}
                }
            });
            transportItems.Add(new TransportItem
            {
                Code = "TMS002",
                Id = 2,
                PointPlaces = new List<PointPlace>
                {
                    new PointPlace{ Id = 3,Type = 0,UnitId = 2,Name = "长沙"},
                    new PointPlace{ Id = 4,Type = 1,UnitId = 3,Name = "衡阳"}
                }
            });
            transportItems.Add(new TransportItem
            {
                Code = "TMS003",
                Id = 1,
                PointPlaces = new List<PointPlace>
                {
                    new PointPlace{ Id = 5,Type = 0,UnitId = 3,Name = "衡阳"},
                    new PointPlace{ Id = 6,Type = 1,UnitId = 4,Name = "株洲"}
                }
            });
            transportItems.Add(new TransportItem
            {
                Code = "TMS004",
                Id = 1,
                PointPlaces = new List<PointPlace>
                {
                    new PointPlace{ Id = 7,Type = 0,UnitId = 4,Name = "株洲"},
                    new PointPlace{ Id = 8,Type = 1,UnitId = 5,Name = "广州"}
                }
            });
            transportItems.Add(new TransportItem
            {
                Code = "TMS005",
                Id = 1,
                PointPlaces = new List<PointPlace>
                {
                    new PointPlace{ Id = 9,Type = 0,UnitId = 5,Name = "广州"},
                    new PointPlace{ Id = 10,Type = 1,UnitId = 6,Name = "深圳"}
                }
            });
        }
        public List<TransportItem> TransportItems => transportItems;
    }

    public class Transport
    {
        public int Id { get; set; }
        public List<TransportItem> Items { get; set; }
    }

    public class TransportItem
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public List<PointPlace> PointPlaces { get; set; }

    }

    public class PointPlace
    {
        public int Id { get; set; }
        public int Type { get; set; }
        public int UnitId { get; set; }
        public string Name { get; set; }
    }
}
