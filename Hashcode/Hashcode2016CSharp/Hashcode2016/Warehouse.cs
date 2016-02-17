using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hashcode2016
{
    public class Warehouse
    {
        public Warehouse()
        {
            Products = new List<WarehouseProduct>();
        }
        public int WarehouseID { get; set; }
        public Location Warehouselocation { get; set; }
        public List<WarehouseProduct> Products { get; set; }
    }
}