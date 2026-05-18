using System;
using Common;
using System.ServiceModel;
using System.IO;
using System.Net.NetworkInformation;
using Service.Publisher;
using System.Configuration;
using System.ServiceModel.Configuration;

namespace Service
{
    public class SmartGridService : ISmartGridService, IDisposable
    {
        private static bool isFirstTime = true;

        private static double lastVoltage = 0;
        private static double lastCurrent = 0;
        private static double totalCurrent = 0;
        private static int sampleCount = 0;

        private double ITrashold = double.Parse(ConfigurationManager.AppSettings["I_threshold"]);
        private double VTrashold = double.Parse(ConfigurationManager.AppSettings["V_threshold"]);
        private double percentageDeviation = double.Parse(ConfigurationManager.AppSettings["percentage_deviation"]);

        private static StreamWriter measuremenWriter;
        private static StreamWriter rejectWriter;

        private readonly string measurementFile = "measurements_session.csv";
        private readonly string rejectFile = "rejects.csv";

        TransferGenerator transferGenerator = new TransferGenerator();
        RecieveGenerator recieverGenerator = new RecieveGenerator();
        WarningGenerator warningGenerator = new WarningGenerator();

        public SmartGridService()
        {
            if (isFirstTime)
            {
                isFirstTime = false;
                measuremenWriter = new StreamWriter(measurementFile, append: false);
                rejectWriter = new StreamWriter(rejectFile, append: false);
                measuremenWriter.AutoFlush = true;
                rejectWriter.AutoFlush = true;
            }

            transferGenerator.OnTransferStarted += OnTransferStarted;
            transferGenerator.OnTransferCompleted += OnTransferCompleted;
            recieverGenerator.OnSampleReceived += OnRecieve;
            warningGenerator.VoltageSpike += OnVoltageSpike;
            warningGenerator.CurrentSpike += OnCurrentSpike;
            warningGenerator.OutOfBandWarning += OnOutOfBandWarning;
        }

        public static void CloseWriters()
        {
            measuremenWriter?.Close();
            rejectWriter?.Close();
        }

        private bool disposed = false;

        public void StartSession(string meta)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Session started: {meta}");
            Console.WriteLine("========================================");
        }

        public void PushSample(SmartGridSample sample)
        {
            // --- validation ---
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
                }, new FaultReason("Data format error: Negative current detected."));
            }

            recieverGenerator.GenerateRecieve(sample.Voltage, sample.Current, sample.Frequency);
            SimulateDataTransfer();

            int thisSampleNumber = sampleCount + 1;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Sample #{thisSampleNumber}: Voltage={sample.Voltage:F2} V, Current={sample.Current:F2} A, Frequency={sample.Frequency} Hz");

            totalCurrent += sample.Current;
            sampleCount++;

            CheckOutOfBandWarning(sample);
            CheckCurrentSpike(lastCurrent, sample);
            CheckVoltageSpike(lastVoltage, sample);

            lastCurrent = sample.Current;
            lastVoltage = sample.Voltage;

            Console.WriteLine("----------------------------------------");
        }

        public void EndSession()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Session ended.");
            Console.WriteLine("========================================");
            Dispose();
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
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Server resources released.");
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
            Console.Write($"[{DateTime.Now:HH:mm:ss.fff}] Transfer started... ");
        }

        private void OnTransferCompleted(object sender, EventArgs e)
        {
            Console.WriteLine("completed.");
        }

        private void OnRecieve(object sender, RecieveEventArgs e)
        {
            string validLine = $"{DateTime.Now} | Voltage: {e.Voltage}, Current: {e.Current}, Frequency: {e.Frequency}";
            measuremenWriter.WriteLine(validLine);
        }

        private void OnVoltageSpike(object sender, WarningEventArgs e)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Voltage spike event dispatched (direction: {e.Direction}).");
        }

        private void OnCurrentSpike(object sender, WarningEventArgs e)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Current spike event dispatched (direction: {e.Direction}).");
        }

        private void CheckCurrentSpike(double lastCurrent, SmartGridSample sample)
        {
            string direction = null;
            if (lastCurrent != 0)
            {
                if (sample.Current - lastCurrent > ITrashold)
                    direction = "Upward";
                else if (lastCurrent - sample.Current > ITrashold)
                    direction = "Downward";
            }

            if (direction != null)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Current Spike {direction}: Last={lastCurrent:F2} A, Current={sample.Current:F2} A (Threshold={ITrashold:F2} A)");
                warningGenerator.GenerateCurrentSpike(direction);
            }
        }

        private void CheckVoltageSpike(double lastVoltage, SmartGridSample sample)
        {
            string direction = null;
            if (lastVoltage != 0)
            {
                if (sample.Voltage - lastVoltage > VTrashold)
                    direction = "Upward";
                else if (lastVoltage - sample.Voltage > VTrashold)
                    direction = "Downward";
            }

            if (direction != null)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Voltage Spike {direction}: Last={lastVoltage:F2} V, Current={sample.Voltage:F2} V (Threshold={VTrashold:F2} V)");
                warningGenerator.GenerateVoltageSpike(direction);
            }
        }

        private void CheckOutOfBandWarning(SmartGridSample sample)
        {
            double averageCurrent = totalCurrent / sampleCount;
            double lowerBound = (1 - percentageDeviation) * averageCurrent;
            double upperBound = (1 + percentageDeviation) * averageCurrent;

            if (sample.Current < lowerBound || sample.Current > upperBound)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Out-of-band current: Value={sample.Current:F2} A, Average={averageCurrent:F2} A, Deviation allowed=±{percentageDeviation * 100}%");
                warningGenerator.GenerateOutOfBandWarning();
            }
        }

        private void OnOutOfBandWarning(object sender, EventArgs e)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Out-of-band warning event triggered.");
        }
    }
}