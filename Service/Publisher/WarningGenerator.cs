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

        public event WarningEventHandler OnWarning;

        public void GenerateWarning(string trigger, string direction)
        {
            if (OnWarning != null)
            {
                WarningEventArgs args = new WarningEventArgs(trigger, direction);
                OnWarning(this, args);
            }
            else
            {
                Console.WriteLine("No subscribers!");
            }
        }
    }
}
