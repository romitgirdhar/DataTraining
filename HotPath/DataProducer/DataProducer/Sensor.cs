using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataProducer
{
    class Sensor
    {
        public string PlantType { get; set; }
        public double Temp { get; set; }
        public double Light { get; set; }
        public double Humidity { get; set; }
        public int Room { get; set; }
        public int PlantPosition { get; set; }
        public string SensorDateTime {
            get { return DateTime.Now.ToString("yyyy-MM-dd") + "T" + DateTime.Now.ToString("HH:mm:ss"); } }

    }
}
