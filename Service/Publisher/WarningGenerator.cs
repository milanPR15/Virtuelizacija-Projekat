using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Publisher
{
    internal class WarningGenerator
    {
        public delegate void WarningEventHandler(object sender, WarningEventArgs e);
        public delegate void OutOfBandWarningEventHandler(object sender, EventArgs e);

        public event WarningEventHandler VoltageSpike;
        public event WarningEventHandler CurrentSpike;
        public event OutOfBandWarningEventHandler OutOfBandWarning;

        public void GenerateVoltageSpike(string direction)
        {
            if (VoltageSpike != null)
            {
                WarningEventArgs args = new WarningEventArgs(direction);
                VoltageSpike(this, args);
            }
            else
            {
                Console.WriteLine("No subscribers!");
            }
        }

        public void GenerateCurrentSpike(string direction)
        {
            if (CurrentSpike != null)
            {
                WarningEventArgs args = new WarningEventArgs(direction);
                CurrentSpike(this, args);
            }
            else
            {
                Console.WriteLine("No subscribers!");
            }
        }

        public void GenerateOutOfBandWarning()
        {
            if (OutOfBandWarning != null)
            {
                EventArgs args = new EventArgs();
                OutOfBandWarning(this, args);
            }
            else
            {
                Console.WriteLine("No subscribers!");
            }
        }
    }
}
