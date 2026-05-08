using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class DataFormatFault
    {
        [DataMember]
        public string Details { get; set; }

        [DataMember]
        public string ViolatingFiled { get; set; }
    }
}
