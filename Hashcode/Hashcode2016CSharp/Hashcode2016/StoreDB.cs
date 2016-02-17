using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hashcode2016
{
    public class StoreDB
    {
        public StoreDB()
        {
            Products = new List<Product>();
            Warehouses = new List<Warehouse>();
            WarehouseProducts = new List<WarehouseProduct>();
            Drones = new List<Drone>();
            Orders = new List<Order>();
            OrderProducts = new List<OrderProduct>();
        }

        public int GridSizeX { get; set; }
        public int GridSizeY { get; set; }
        public List<Drone> Drones { get; set; }
        public List<Product> Products { get; set; }
        public List<Warehouse> Warehouses { get; set; }
        public List<WarehouseProduct> WarehouseProducts { get; set; }

        public List<Order> Orders { get; set; }
        public List<OrderProduct> OrderProducts { get; set; }
        
        public int MaxTurns { get; set; }

        public int MaxPayLoad { get; set; }

        public void LoadDrones(int noOfDrones)
        {
            for (int i = 0; i < noOfDrones; i++)
            {
                var drone = new Drone();
                drone.DroneID = i;
                drone.CurrentLocation = this.Warehouses[0].Warehouselocation;
                drone.MaxPayLoad = this.MaxPayLoad;
                this.Drones.Add(drone);
            }
        }


        public int GetProductWeight(int pid)
        {
            foreach (var product in this.Products)
            {
                if (product.ProductID == pid) return product.Weight;
            }
            return 0;
        }

    }
}