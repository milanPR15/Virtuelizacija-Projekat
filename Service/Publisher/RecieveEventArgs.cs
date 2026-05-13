using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Publisher
{
    internal class RecieveEventArgs
    {
        public double Voltage { get; set; }
        public double Current { get; set; }
        public double Frequency { get; set; }

        public RecieveEventArgs(double voltage, double current, double frequency)
        {
            Voltage = voltage;
            Current = current;
            Frequency = frequency;
        }
    }
}
