/*

using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/* This class produces events that are sent to the event hub.  It will run on a single thread
 

namespace DataProducer
{

    class SingleTask
    {

        public static async void Run(List<EventHubSender> sender)
        {
           
            int counter = 0;
            int y = 0;
            int senderCounter = 0;
            while (y < 1)
            {
                Thread.Sleep(10);
                if(counter % 1000 ==0 )
                { Console.WriteLine(counter); }
                //Put all data into data object for eazy serilization
                Sensor obj = new DataObject { CreditCardNumber = getCreditCardInfo(5), GeographyId = rand.Next(1, 5), FirstName = names[rand.Next(0, names.Count)] };
                double value = products[rand.Next(1, products.Count)];
                int key = products.FirstOrDefault(x => x.Value == value).Key;
                obj.ItemId = key;
                obj.ItemPrice = value;
                //send event using the sender associated to the correct partition
                SendEvent(obj,sender[senderCounter]);
                counter = counter + 1;

                //make sure we don't go over the number of senders assigned to this task
                if(senderCounter == sender.Count-1)
                {
                    senderCounter = 0;
                }
                else { senderCounter = senderCounter + 1; }
            }


        }



        //send serilized event to the event hub
    static  void SendEvent(Sensor data, EventHubSender sender)
    {     
        var serializedString = JsonConvert.SerializeObject(data);
        sender.SendAsync(new EventData(Encoding.UTF8.GetBytes(serializedString)));
    }


*/
   