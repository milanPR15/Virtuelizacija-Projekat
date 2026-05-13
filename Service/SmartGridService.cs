using System;
using Common;
using System.ServiceModel;
using System.IO;
using System.Net.NetworkInformation;

namespace Service
{
    public class SmartGridService : ISmartGridService, IDisposable
    {
        private static bool isFirstTime = true;

        private StreamWriter measuremenWriter;
        private StreamWriter rejectWriter;


        private readonly string measurementFile = "measurements_session.csv";
        private readonly string rejectFile = "rejects.csv";

        public SmartGridService()
        {
            bool append = !isFirstTime;
            measuremenWriter = new StreamWriter(measurementFile, append);
            rejectWriter = new StreamWriter(rejectFile, append);

            measuremenWriter.AutoFlush = true;
            rejectWriter.AutoFlush = true;

            if(isFirstTime)
            {
                isFirstTime = false;
            }
        }

        private bool disposed = false;
        public void StartSession(string meta)
        {
            Console.WriteLine($"Session started with meta: {meta}");
        }

        public void PushSample(SmartGridSample sample)
        {
            Console.WriteLine("Processing incoming sample...");
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
                string rejectLine = $"{DateTime.Now} | Voltage: {sample.Voltage}, Current: {sample.Current}, Frequency: {sample.Frequency} | Reason: Negative value.";
                rejectWriter.WriteLine(rejectLine);

                throw new FaultException<ValidationFault>(new ValidationFault
                {
                    Message = $"Invalid frequency ({sample.Frequency}). Must be greater than 0.",
                    Timestamp = DateTime.Now
                });
            }

            if(sample.Voltage < 0)
            {
                string rejectLine = $"{DateTime.Now} | Voltage: {sample.Voltage}, Current: {sample.Current}, Frequency: {sample.Frequency} | Reason: Negative value.";
                rejectWriter.WriteLine(rejectLine);

                throw new FaultException<DataFormatFault>(new DataFormatFault
                {
                    Details = $"Voltage value ({sample.Voltage}) is negative.",
                    ViolatingField = "Voltage"
                });
            }

            if (sample.Current < 0)
            {
                string rejectLine = $"{DateTime.Now} | Voltage: {sample.Voltage}, Current: {sample.Current}, Frequency: {sample.Frequency} | Reason: Negative value.";
                rejectWriter.WriteLine(rejectLine);

                throw new FaultException<DataFormatFault>(new DataFormatFault
                {
                    Details = $"Current value ({sample.Current}) is negative.",
                    ViolatingField = "Current"
                });
            }
            

            string validLine = $"{DateTime.Now} | Voltage: {sample.Voltage}, Current: {sample.Current}, Frequency: {sample.Frequency}";
            measuremenWriter.WriteLine(validLine);

            System.Threading.Thread.Sleep(1000);
            Console.WriteLine($"Sample received: Voltage={sample.Voltage}, Current={sample.Current}");
            Console.WriteLine("Sample accepted and stored.");
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
                   if(measuremenWriter != null)
                   {
                        measuremenWriter.Close();
                        measuremenWriter.Dispose();
                   }

                   if(rejectWriter != null)
                   {
                        rejectWriter.Close();
                        rejectWriter.Dispose();
                   }
                    Console.WriteLine("Server resources are being relesed. ");
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
