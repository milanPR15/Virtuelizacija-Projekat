using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Publisher
{
    internal class WarningEventArgs
    {
        public string Trigger { get; set; }
        public string Direction { get; set; }
        public WarningEventArgs(string trigger, string direction)
        {
            Trigger = trigger;
            Direction = direction;
        }
    }
}
