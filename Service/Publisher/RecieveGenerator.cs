using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Publisher
{
    internal class RecieveGenerator
    {
        public delegate void RecieveEventHandler(object sender, RecieveEventArgs e);

        public event RecieveEventHandler OnSampleReceived;

        public void GenerateRecieve(double voltage, double current, double frequency)
        {
            if (OnSampleReceived != null)
            {
                RecieveEventArgs args = new RecieveEventArgs(voltage, current, frequency);
                OnSampleReceived(this, args);
            }
            else
            {
                Console.WriteLine("No subscribers!");
            }
        }
    }
}
