using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hashcode2016
{
    public class Drone
    {
        public Drone()
        {
            productsInDrone = new List<ProductsInDrone>();
        }
        public int DroneID { get; set; }
        public Location CurrentLocation { get; set; }
        public int CurrentWeight { get; set; }
        public int FreeSpace
        {
            get
            {
                return MaxPayLoad - CurrentWeight;
            }
        }
        public int MaxPayLoad { get; set; }

        public List<ProductsInDrone> productsInDrone  { get; set; }

}
}