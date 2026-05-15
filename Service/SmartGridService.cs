using System;
using Common;
using System.ServiceModel;
using System.IO;
using System.Net.NetworkInformation;
using Service.Publisher;
using System.Configuration;

namespace Service
{
    public class SmartGridService : ISmartGridService, IDisposable
    {
        private static bool isFirstTime = true;

        private static double lastVoltage = 0;
        private static double lastCurrent = 0;

        private double ITrashold = double.Parse(ConfigurationManager.AppSettings["I_threshold"]);
        private double VTrashold = double.Parse(ConfigurationManager.AppSettings["V_threshold"]);

        private StreamWriter measuremenWriter;
        private StreamWriter rejectWriter;


        private readonly string measurementFile = "measurements_session.csv";
        private readonly string rejectFile = "rejects.csv";

        TransferGenerator transferGenerator = new TransferGenerator();
        RecieveGenerator recieverGenerator = new RecieveGenerator();
        WarningGenerator warningGenerator = new WarningGenerator();

        public SmartGridService()
        {
            bool append = !isFirstTime;
            measuremenWriter = new StreamWriter(measurementFile, append);
            rejectWriter = new StreamWriter(rejectFile, append);

            measuremenWriter.AutoFlush = true;
            rejectWriter.AutoFlush = true;

            if (isFirstTime)
            {
                isFirstTime = false;
            }

            transferGenerator.OnTransferStarted += OnTransferStarted;
            transferGenerator.OnTransferCompleted += OnTransferCompleted;

            recieverGenerator.OnSampleReceived += OnRecieve;
            warningGenerator.VoltageSpike += OnVoltageSpike;
            warningGenerator.CurrentSpike += OnCurrentSpike;
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

            if (sample.Frequency <= 0)
            {
                string rejectLine = $"{DateTime.Now} | Voltage: {sample.Voltage}, Current: {sample.Current}, Frequency: {sample.Frequency} | Reason: Negative value.";
                rejectWriter.WriteLine(rejectLine);

                throw new FaultException<ValidationFault>(new ValidationFault
                {
                    Message = $"Invalid frequency ({sample.Frequency}). Must be greater than 0.",
                    Timestamp = DateTime.Now
                }, new FaultReason("Validation error: Negative frequency detected."));
            }

            if (sample.Voltage < 0)
            {
                string rejectLine = $"{DateTime.Now} | Voltage: {sample.Voltage}, Current: {sample.Current}, Frequency: {sample.Frequency} | Reason: Negative value.";
                rejectWriter.WriteLine(rejectLine);

                throw new FaultException<DataFormatFault>(new DataFormatFault
                {
                    Details = $"Invalid voltage ({sample.Voltage}). Must be greater than 0.",
                    ViolatingField = "Voltage"
                }, new FaultReason("Data format error: Negative voltage detected."));
            }

            if (sample.Current < 0)
            {
                string rejectLine = $"{DateTime.Now} | Voltage: {sample.Voltage}, Current: {sample.Current}, Frequency: {sample.Frequency} | Reason: Negative value.";
                rejectWriter.WriteLine(rejectLine);

                throw new FaultException<DataFormatFault>(new DataFormatFault
                {
                    Details = $"Invalid current ({sample.Current}). Must be greater than 0.",
                    ViolatingField = "Current"
                }, new FaultReason("Data format error: Negarive current detected."));
            }

            recieverGenerator.GenerateRecieve(sample.Voltage, sample.Current, sample.Frequency);

            SimulateDataTransfer();
            Console.WriteLine($"\nSample received: Voltage={sample.Voltage}, Current={sample.Current}");

            CheckCurrentSpike(lastCurrent, sample);
            CheckVoltageSpike(lastVoltage, sample);
            lastCurrent = sample.Current;
            lastVoltage = sample.Voltage;
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
            if (!disposed)
            {
                if (disposing)
                {
                    if (measuremenWriter != null)
                    {
                        measuremenWriter.Close();
                        measuremenWriter.Dispose();
                    }

                    if (rejectWriter != null)
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

        private void OnVoltageSpike(object sender, WarningEventArgs e)
        {
            Console.WriteLine($"Voltage Spike: Direction: {e.Direction}");
        }

        private void OnCurrentSpike(object sender, WarningEventArgs e)
        {
            Console.WriteLine($"Current Spike: Direction: {e.Direction}");
        }

        private void CheckCurrentSpike(double lastCurrent, SmartGridSample sample)
        {
            if (lastCurrent != 0)
            {
                if (sample.Current - lastCurrent > ITrashold)
                {
                    warningGenerator.GenerateCurrentSpike("Upward");
                }
                else if (lastCurrent - sample.Current > ITrashold)
                {
                    warningGenerator.GenerateCurrentSpike("Downward");
                }

            }
        }

        private void CheckVoltageSpike(double lastVoltage, SmartGridSample sample)
        {
            if (lastVoltage != 0)
            {
                if (sample.Voltage - lastVoltage > VTrashold)
                {
                    warningGenerator.GenerateVoltageSpike("Upward");
                }
                else if (lastVoltage - sample.Voltage > VTrashold)
                {
                    warningGenerator.GenerateVoltageSpike("Downward");
                }
            }
        }
    }
}
