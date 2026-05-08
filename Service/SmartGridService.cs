using System;
using Common;
using System.ServiceModel;

namespace Service
{
    public class SmartGridService : ISmartGridService
    {
        public void StartSession(string meta)
        {
            Console.WriteLine($"Session started with meta: {meta}");
        }

        public void PushSample(SmartGridSample sample)
        {
            if (sample == null)
            {
                throw new FaultException<ValidationFault>(new ValidationFault
                {
                    Message = "Received sample is null.",
                    Timestamp = DateTime.Now
                });
            }

            if(sample.Frequency <= 0)
            {
                throw new FaultException<ValidationFault>(new ValidationFault
                {
                    Message = $"Invalid frequency ({sample.Frequency}). Must be greater than 0.",
                    Timestamp = DateTime.Now
                });
            }

            if(sample.Voltage < 0)
            {
                throw new FaultException<DataFormatFault>(new DataFormatFault
                {
                    Details = $"Voltage value ({sample.Voltage}) is negative.",
                    ViolatingFiled = "Voltage"
                });
            }

            if (sample.Current < 0)
            {
                throw new FaultException<DataFormatFault>(new DataFormatFault
                {
                    Details = $"Current value ({sample.Current}) is negative.",
                    ViolatingFiled = "Current"
                });
            }

            Console.WriteLine($"Sample received: Voltage={sample.Voltage}, Current={sample.Current}");
        }

        public void EndSession()
        {
            Console.WriteLine("Session ended.");
        }
    }
}
