using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hashcode2016
{
    public class Product
    {
        public int ProductID { get; set; }
        public int Weight { get; set; }
    }

    public class WarehouseProduct : Product
    {
        public int WarehouseID { get; set;  }
        public int Quantity { get; set; }
        public Location Warehouselocation { get; set; }
    }

    public class OrderProduct : Product
    {
        public int OrderID { get; set; }
        public Location DeliveryLocation { get; set; }
        public bool Dispatched { get; set; }
        public int NearestWareHouseId { get; set; }
        public int DistanceToNearestWareHouseId { get; set; }
    }

    public class ProductsInDrone : Product
    {
        public int OrderID { get; set; }
        public int ProductCount { get; set; }
    }
}