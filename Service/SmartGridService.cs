using System;
using Common;
using System.ServiceModel;
using System.IO;
using System.Net.NetworkInformation;
using Service.Publisher;

namespace Service
{
    public class SmartGridService : ISmartGridService, IDisposable
    {
        private static bool isFirstTime = true;

        private StreamWriter measuremenWriter;
        private StreamWriter rejectWriter;


        private readonly string measurementFile = "measurements_session.csv";
        private readonly string rejectFile = "rejects.csv";

        TransferGenerator transferGenerator = new TransferGenerator();
        RecieveGenerator recieverGenerator = new RecieveGenerator();

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

            transferGenerator.OnTransferStarted += OnTransferStarted;
            transferGenerator.OnTransferCompleted += OnTransferCompleted;

            recieverGenerator.OnSampleReceived += OnRecieve;
        }

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
            


            SimulateDataTransfer();
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

        private void SimulateDataTransfer()
        {
            transferGenerator.GenerateTransfer();
        }

        private void OnTransferStarted(object sender, EventArgs e)
        {
            Console.WriteLine("Processing incoming sample...");
        }

        private void OnTransferCompleted(object sender, EventArgs e)
        {
            Console.WriteLine("Sample accepted and stored.");
        }

        private void OnRecieve(object sender, RecieveEventArgs e)
        {
            string validLine = $"{DateTime.Now} | Voltage: {e.Voltage}, Current: {e.Current}, Frequency: {e.Frequency}";
            measuremenWriter.WriteLine(validLine);
        }
    }
}
