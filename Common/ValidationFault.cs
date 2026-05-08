using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class ValidationFault
    {
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }

        public ValidationFault()
        {
            Timestamp = DateTime.Now;
        }
    }
}
