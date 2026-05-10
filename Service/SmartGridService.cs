using System;
using Common;
using System.ServiceModel;

namespace Service
{
    public class SmartGridService : ISmartGridService, IDisposable
    {
        private bool disposed = false;
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
                    ViolatingField = "Voltage"
                });
            }

            if (sample.Current < 0)
            {
                throw new FaultException<DataFormatFault>(new DataFormatFault
                {
                    Details = $"Current value ({sample.Current}) is negative.",
                    ViolatingField = "Current"
                });
            }

            Console.WriteLine($"Sample received: Voltage={sample.Voltage}, Current={sample.Current}");
        }

        public void EndSession()
        {
            Console.WriteLine("Session ended.");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            if(!disposed)
            {
                if(disposing)
                {
                    Console.WriteLine("Server resources are being released.");
                }
                disposed = true;
            }
        }

        ~SmartGridService()
        {
            Dispose(false);
        }


    }
}
