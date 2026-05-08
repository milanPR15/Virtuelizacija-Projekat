using System;
using System.ServiceModel;

namespace Common
{
    [ServiceContract]
    public interface ISmartGridService
    {
        [OperationContract]
        void StartSession(string meta);

        [OperationContract]

        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        void PushSample(SmartGridSample sample);

        [OperationContract]
        void EndSession();

    }
}
