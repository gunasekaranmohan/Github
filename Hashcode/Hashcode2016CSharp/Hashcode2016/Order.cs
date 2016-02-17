using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hashcode2016
{
    public class Order
    {
        public Order()
        {
            Products = new List<OrderProduct>();
        }

        public int OrderID { get; set; }
        public Location DeliveryLocation { get; set; }
        public bool Dispatched { get; set; }
        public List<OrderProduct> Products { get; set; }

        public int PendingItems { get; set; }
        public int TurnsTookToDeliver { get; set; }
        public int MinimumTurns { get; set; }

    }
}