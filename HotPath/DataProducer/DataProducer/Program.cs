using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Specialized;



/* Program Summary:  This Program is a data producer for testing your event hubs and Stream Analyitc queries.
 * Can produce around 2k events per second per thread through sending events directly to event hub partitions.
 * Automatically uses 90% of avaliable threads.  Can be changed in config file by specifiying a number
 * Set variables in the app.config file and look at the data object to understand format of the JSON object.  
 */


namespace DataProducer
{
    class Program
    {
        static Random random = new Random();
        static int numThreads = (int)Math.Floor(.9 * Environment.ProcessorCount);
        //    static string eventHubName = ConfigurationManager.AppSettings["eventHubName"];
        //    static string connectionString = ConfigurationManager.AppSettings["sbConnectionString"];




        static void Main(string[] args)
        {
            string eventHubName = args[0];
            string connectionString = args[1];

            double mean = 0;
            double stdDev = 5 / 3;
            int numRooms = 2;
            int plantsInRoom = 4;
            string[] plantTypes = { "Rose", "SugarCane", "Watermelon", "Wheat" };


            //Set up connections to Service Bus and create the client object
            ServiceBusConnectionStringBuilder builder = new ServiceBusConnectionStringBuilder(connectionString);
            builder.TransportType = TransportType.Amqp;
            MessagingFactory factory = MessagingFactory.CreateFromConnectionString(builder.ToString());
            NamespaceManager manager = NamespaceManager.CreateFromConnectionString(connectionString);
            EventHubDescription description = manager.CreateEventHubIfNotExists(eventHubName);
            EventHubClient client = factory.CreateEventHubClient(eventHubName);
            

            //Allows for number of threads to be specified 
            try
            {
                numThreads = Convert.ToInt32(ConfigurationManager.AppSettings["numThreads"]);
            }
            catch { }

            List<Sensor> sensors = setupGreenhouse(numRooms, plantsInRoom, plantTypes);

            //This loop continues to run and has all of the sensors updated and events sent.
            
            while(true)
            {
                advanceTime(sensors, mean, stdDev, client);
                Thread.Sleep(10000);
            }


            
        }

        //This simulates an advance in time between reporting. The values of the sensors change minimally
        //between each time advance. 
        //#####TODO#####
        //Add the functionality to recognize day/night
        private static void advanceTime(List<Sensor> sensors, double mean, double stdDev, EventHubClient client)
        {
            Parallel.ForEach(sensors, (sensor) =>
           {
               //This changes the light. It is a different distrabution than the rest given it's more rare to
               //see major changes in light
               var randomNum = random.NextDouble();
               if (randomNum < .9)
               {
                //   if (sensor.Light < 251)
                //   {
                       sensor.Light = 500;
               //    }
              //     else
             //          sensor.Light = 250;
                   
               }
               else
                   sensor.Light = 250;

               //Changes the humidity and temp 
               sensor.Humidity = sensor.Humidity + getChange(mean, stdDev);
               sensor.Temp = sensor.Temp + getChange(mean, stdDev);

               //send the event to the event hub
               sendEvent(sensor, client);
               Thread.Sleep(1000);
           });


        }
        
        private static void sendEvent(Sensor sensor, EventHubClient client)
        {
             
            var serializedString = JsonConvert.SerializeObject(sensor);
            client.SendAsync(new EventData(Encoding.UTF8.GetBytes(serializedString)));
            Console.Out.WriteLine("Sent data: "+ serializedString);
        }



        //using a box muller transform to return a value near a normal distribution based on mean and stddev
        //This value is needed to change the temp/light settings randomly each movement of time
        private static double getChange(double mean, double stdDev)
        {
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(random.NextDouble())) 
                                        *Math.Sin(2.0 * Math.PI * random.NextDouble()); //random normal(0,1)
            double randNormal =
                         mean + stdDev * randStdNormal;

            return randNormal;
        }

        

        private static List<Sensor> setupGreenhouse(int numRooms, int plantsInRoom, string[] plantTypes)
        {
            List<Sensor> sensors = new List<Sensor>();

            //This double loop creates sensors ans assigns each a location in the green house with plant type
            int x = 1;
            while (x <= numRooms)
            {
                var plantType = plantTypes[random.Next(plantTypes.Length)];
                int y = 0;
                while (y < plantsInRoom)
                {
                    sensors.Add(new Sensor { Humidity = 200, Light = 300, PlantPosition = y, PlantType = plantType, Room = x, Temp = 75 });
                    y++;
                }
                x++;

            }

            return sensors;
        }
    }

}
         

            /* 
             

             //Creates a list of avaliable event hub senders based upon the number of partitions of the event hub
             //and avaliable threads
             int x = 0;
             List<EventHubSender> senderList = new List<EventHubSender>();
             while (x != description.PartitionCount)
             {
                 EventHubSender partitionedSender = client.CreatePartitionedSender(description.PartitionIds[x]);
                 senderList.Add(partitionedSender);
                 x = x + 1;
             }
             var subLists = SplitToSublists(senderList);


             //create a list of tasks that independently send events to the event hub
             List<Task> taskList = new List<Task>();
             for (int i = 0; i < (int)numThreads; i++)
             {
                 int indexOfSublist = i;
                 taskList.Add(new Task(() => SingleTask.Run(subLists[indexOfSublist])));
             }

             if (numThreads == subLists.Count)
             {
                 Console.WriteLine("Using " + numThreads + " threads.  Press enter to continue and produce data");
                 Console.ReadKey();
             }
             else
             { 
                 Console.WriteLine("Number of threads != number of sender arrays.  Tasks will not start.");
                 Console.Read();
             }

             //Start Each Event
             taskList.ForEach(a => a.Start());

             //Wait for all to end.  This shouldn't happen but need this here or the project would close
             taskList.ForEach(a => a.Wait());


         }

         private static List<List<EventHubSender>> SplitToSublists(List<EventHubSender> source)
         {
             return source
                      .Select((x, i) => new { Index = i, Value = x })
                      .GroupBy(x => x.Index % numThreads)
                      .Select(x => x.Select(v => v.Value).ToList())
                      .ToList();
         }
   */
       
