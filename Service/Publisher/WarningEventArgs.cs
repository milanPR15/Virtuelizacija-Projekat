using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Publisher
{
    internal class WarningEventArgs : EventArgs
    {
        public string Direction { get; set; }
        public WarningEventArgs(string direction)
        {
            Direction = direction;
        }
    }
}
