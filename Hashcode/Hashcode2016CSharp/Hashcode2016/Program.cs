using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Hashcode2016
{
    static class Program
    {
        static StoreDB db = new StoreDB();
        static List<string> commands = new List<string>();
        static int OverAllTurn = 0;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {

            string inputFile = @"H:\Github\Hashcode\Hashcode2016CSharp\Hashcode2016\Input\busy_day.in";
            string outputfile = @"H:\Github\Hashcode\Hashcode2016CSharp\Hashcode2016\Output\busy_day.out";

            if (args.Length == 2)
            {
                inputFile = Path.Combine(Environment.CurrentDirectory, args[0]);
                outputfile = Path.Combine(Environment.CurrentDirectory, args[1]);
            }
            Console.WriteLine("InputFile:{0}, \nOutputFile:{1}\n", inputFile, outputfile);

            Console.WriteLine("Enter to continue...");
            Console.ReadLine();

            db = ReadInputFile(inputFile);

            CalculatateMinimunmTurnsForAllOrders();

            ProcessPrioriyOrder();

            PrintResults();

            var strContent = string.Join(Environment.NewLine, commands.ToArray());

            strContent = commands.Count().ToString() + Environment.NewLine + strContent;
            File.WriteAllText(outputfile, strContent);

            Console.WriteLine("Enter to exit...");
            Console.ReadLine();
            
        }

        private static void CalculatateMinimunmTurnsForAllOrders()
        {
            foreach(var o in db.Orders.Where(r=>r.Dispatched== false))
            {
                CalculatateMinimunmTurnsPerOrder(o); 
            }
        }

        /// <summary>
        /// This method is to calculate minimum Turns to deliver the Order assuming the Drone is already in Warehouse. 
        /// </summary>
        /// <param name="o"></param>
        private static void CalculatateMinimunmTurnsPerOrder(Order o)
        {
            var bestWarehouseForeachProduct = (from w in db.Warehouses
                        join wprod in db.WarehouseProducts on w.WarehouseID equals wprod.WarehouseID
                        join oprod in o.Products on wprod.ProductID equals oprod.ProductID
                        join prd in db.Products on wprod.ProductID equals prd.ProductID
                        join dr in db.Drones on 1 equals 1
                        where wprod.Quantity > 0
                       select new { w.WarehouseID,
                                    prd.ProductID,
                                    Distance = GetDistance(w.Warehouselocation, o.DeliveryLocation) + GetDistance(w.Warehouselocation, dr.CurrentLocation),
                                    prd.Weight
                       }).OrderBy(r=>r.Distance).ThenBy(r=>r.Weight)
                       ;

            int turns = 0;
            int weightInDrone = 0;
            int wareHouseId = -1;
            foreach(var p in o.Products)
            {
                var w = (from wa in bestWarehouseForeachProduct
                         where wa.ProductID == p.ProductID
                         select (new { wa.Weight, wa.Distance, wa.WarehouseID })
                         ).OrderBy(r => r.Distance)
                         .ThenBy(r=>r.WarehouseID)
                         .ThenBy(r => r.Weight)
                         .First();

                p.NearestWareHouseId = w.WarehouseID;
                p.DistanceToNearestWareHouseId = w.Distance;

                if (wareHouseId != w.WarehouseID)
                {
                    turns += w.Distance;
                    weightInDrone = 0;
                    wareHouseId = w.WarehouseID;
                }
                if( (weightInDrone + w.Weight) > db.MaxPayLoad )
                {
                    turns += w.Distance;
                    weightInDrone = 0;
                }

                turns++;  //This is for loading product into Drone
                weightInDrone += w.Weight;

            }
            o.MinimumTurns = turns;
        }

        private static void ProcessPrioriyOrder()
        {
                        
            foreach (var o in db.Orders.Where(r => r.Dispatched == false).OrderBy(r => r.MinimumTurns))
            {
                List<string> commmandToProcessOrder = new List<string>();

                //Recalculate based on the current stock
                CalculatateMinimunmTurnsPerOrder(o);
                List<int> droneIds = new List<int>();

                foreach (var p in o.Products.OrderBy(r=>r.NearestWareHouseId))
                {

                    int wareHouseId = p.NearestWareHouseId;
                    int droneID;
                    int distanceBetweenDroneAndWareHouse;
                    FindNearestDrone(wareHouseId, p.Weight, out droneID, out distanceBetweenDroneAndWareHouse);

                    if (!droneIds.Contains(droneID))
                        droneIds.Add(droneID);

                    //Add weight in Drone
                    db.Drones[droneID].CurrentLocation = db.Warehouses[wareHouseId].Warehouselocation;
                    db.Drones[droneID].CurrentWeight += db.Products[p.ProductID].Weight;
                    db.Drones[droneID].productsInDrone.Add(new ProductsInDrone() { ProductID = p.ProductID, OrderID = o.OrderID, ProductCount = 1 });

                    
                    //Reduce the warehouse quantity
                    if (db.Warehouses[wareHouseId].Products[p.ProductID].Quantity < 0)
                        throw new Exception(string.Format("Product {0} is not available in warehouse :{1}", p.ProductID, wareHouseId));

                    db.Warehouses[wareHouseId].Products[p.ProductID].Quantity -= 1;

                    if (OverAllTurn + distanceBetweenDroneAndWareHouse + 1 > db.MaxTurns)
                        return;

                    //Update pending items in Order
                    OverAllTurn = OverAllTurn + distanceBetweenDroneAndWareHouse + 1;
                    db.Orders[o.OrderID].PendingItems -= 1;
                    db.Orders[o.OrderID].TurnsTookToDeliver = OverAllTurn;

                    var command = string.Format("{0} {1} {2} {3} {4}", droneID, "L", wareHouseId, p.ProductID, 1);
                    commmandToProcessOrder.Add(command);
                    Console.WriteLine(string.Format("Load OrderId:{0} ProductID:{1} DroneId:{2} WareHouseId:{3}  CurrentTurn:{4}", o.OrderID, p.ProductID, droneID, wareHouseId, OverAllTurn));
                }
                db.Orders[o.OrderID].Dispatched = true;
                foreach (var d in droneIds)
                {
                    var commandsToDeliver = DeliverDron(d);
                    if(commandsToDeliver.Count() > 0)
                        commmandToProcessOrder.AddRange(commandsToDeliver);
                    else
                        return;
                }

                commands.AddRange(commmandToProcessOrder);
            }
        }

        private static void FindNearestDrone(int wareHouseId, int weight, out int droneId, out int distinanceBetweenDroneAndWareHouse)
        {
            var dronDet = (from drone in db.Drones
                            join ware in db.Warehouses on wareHouseId equals ware.WarehouseID 
                            where drone.FreeSpace > weight
                           select new
                            {
                                drone.DroneID,
                                FreeSpaceInDron = drone.FreeSpace,
                                drone.MaxPayLoad,
                                ware.WarehouseID,
                                DistanceBetweenDroneAndWarehouse = GetDistance(drone.CurrentLocation, ware.Warehouselocation)
                            }
                               ).OrderBy(c => c.DistanceBetweenDroneAndWarehouse)
                               .ThenBy(c => c.FreeSpaceInDron).First();

            droneId = dronDet.DroneID;
            distinanceBetweenDroneAndWareHouse = dronDet.DistanceBetweenDroneAndWarehouse;
        }
        
        private static void PrintResults()
        {
            var completedOrder = db.Orders.Where(r => r.Dispatched == true)
                            .Select(r => new
                            {
                                r.OrderID,
                                r.TurnsTookToDeliver
                            });

            int totalPoints = 0;

            foreach (var o in completedOrder)
            {
                var point =  (int) ((((decimal)OverAllTurn - (decimal)o.TurnsTookToDeliver) / (decimal)OverAllTurn) * (decimal)100.00);
                totalPoints += point;
            }

            var completedPer =  (double)completedOrder.Count() / (double) db.Orders.Count() * 100.00;

            Console.WriteLine("Total Turns:{0} \tCompleted Order:{1} of {2} Total Points:\t\t{3} Completed %: {4}", OverAllTurn, completedOrder.Count(), db.Orders.Count(), totalPoints, completedPer);
        }
        

        static List<string> DeliverDron(int droneId)
        {
            List<string> commandsToDeliver = new List<string>();

            foreach (var prod in db.Drones[droneId].productsInDrone)
            {
                var prevLocation = db.Drones[droneId].CurrentLocation;
                var newLocation = db.Orders[prod.OrderID].DeliveryLocation;
                db.Drones[droneId].CurrentLocation = newLocation;

                OverAllTurn += GetDistance(prevLocation, newLocation);
                db.Orders[prod.OrderID].TurnsTookToDeliver = OverAllTurn;

                if (OverAllTurn >= db.MaxTurns)
                    return commandsToDeliver;
                
                var command = string.Format("{0} {1} {2} {3} {4}", droneId, "D", prod.OrderID, prod.ProductID, prod.ProductCount);
                commandsToDeliver.Add(command);

                Console.WriteLine(string.Format("Deliver DroneId:{0} OrderId:{1} ProductId:{2} ProductCount:{3}", droneId, prod.OrderID, prod.ProductID, prod.ProductCount));
            }

            db.Drones[droneId].CurrentWeight = 0;
            db.Drones[droneId].productsInDrone = new List<ProductsInDrone>();
            
            Console.WriteLine(string.Format("DroneId:{0} is free now", droneId));

            PrintResults();

            return commandsToDeliver;
        }

        static int GetDistance(Location l1, Location l2)
        {
            int dist = 0;

            dist = (int) Math.Ceiling( Math.Sqrt((l1.X - l2.X) * (l1.X - l2.X) + (l1.Y - l2.Y) * (l1.Y - l2.Y)) ) ;

            return dist;
        }

        static StoreDB ReadInputFile(string filePath)
        {

            using (StreamReader sr = new StreamReader(filePath))
            {
                var values = GetIntegerValueFromALine(sr);
                db.GridSizeX = values[0];
                db.GridSizeY = values[1];
                int NoOfDrones = values[2];
                db.MaxTurns = values[3];
                db.MaxPayLoad = values[4];

                values = GetIntegerValueFromALine(sr);
                int NoOfProductTypes = values[0];

                values = GetIntegerValueFromALine(sr);
                for (int i = 0; i < values.Count(); i++)
                {
                    db.Products.Add(new Product() { ProductID = i, Weight = values[i] });
                }

                values = GetIntegerValueFromALine(sr);
                int NoOfWareHouses = values[0];

                for (int warehouseId = 0; warehouseId < NoOfWareHouses; warehouseId++)
                {
                    values = GetIntegerValueFromALine(sr);
                    var warehouse = new Warehouse();
                    warehouse.WarehouseID = warehouseId;
                    warehouse.Warehouselocation = new Location() { X = values[0], Y = values[1] };

                    values = GetIntegerValueFromALine(sr);
                    for (int productId = 0; productId < values.Count(); productId++)
                    {
                        var warehouseProduct = new WarehouseProduct();
                        warehouseProduct.WarehouseID = warehouse.WarehouseID;
                        warehouseProduct.ProductID = productId;
                        warehouseProduct.Quantity = values[productId];
                        warehouseProduct.Warehouselocation = warehouse.Warehouselocation;
                        warehouseProduct.Weight = db.GetProductWeight(productId);

                        warehouse.Products.Add(warehouseProduct);

                        //Adding the same warehouseProduct in db lever collection to make it easy in Linq
                        db.WarehouseProducts.Add(warehouseProduct);
                    }
                    db.Warehouses.Add(warehouse);
                }

                values = GetIntegerValueFromALine(sr);
                int NoOfOrders = values[0];

                for (int orderid = 0; orderid < NoOfOrders; orderid++)
                {
                    var order = new Order();
                    order.OrderID = orderid;

                    values = GetIntegerValueFromALine(sr);
                    order.DeliveryLocation = new Location() { X = values[0], Y = values[1] };

                    values = GetIntegerValueFromALine(sr);
                    int NoOfProducts = values[0];

                    order.PendingItems = NoOfProducts;
                    values = GetIntegerValueFromALine(sr);
                    for (int pid = 0; pid < values.Count(); pid++)
                    {
                        var orderProduct = new OrderProduct();
                        orderProduct.OrderID = orderid;
                        orderProduct.ProductID = values[pid];
                        orderProduct.Weight = db.GetProductWeight(values[pid]);
                        orderProduct.DeliveryLocation = order.DeliveryLocation;
                        order.Products.Add(orderProduct);

                        //Adding the same orderProduct in db lever collection to make it easy in Linq
                        db.OrderProducts.Add(orderProduct);
                    }

                    db.Orders.Add(order);                    
                }

                db.LoadDrones(NoOfDrones);
            }

             return db;

        }

        private static int[] GetIntegerValueFromALine(StreamReader sr)
        {
            string line = sr.ReadLine();
            int[] values = Array.ConvertAll(line.Split(null), int.Parse);

            return values;
        }
    }
}
