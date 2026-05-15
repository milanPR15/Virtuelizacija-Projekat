using System;
using System.ServiceModel;
using Common;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Globalization;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            string clientLogPath = "client_errors.log";
            File.WriteAllText(clientLogPath, $"-----Log started: {DateTime.Now}---{Environment.NewLine}");
            
            ChannelFactory<ISmartGridService> factory = new ChannelFactory<ISmartGridService>("SmartGridEndpoint");

            try
            {
                Console.WriteLine("Connection to the server.");
                ISmartGridService proxy = factory.CreateChannel();

                proxy.StartSession("Bulk_Upload_Session");

                string filePath = ConfigurationManager.AppSettings["CsvFilePath"];

                if(File.Exists(filePath))
                {
                    Console.WriteLine("Reading dataset in batches of 100..");
                    var allLines = File.ReadLines(filePath).Skip(1).ToList();
                    int totalRows = allLines.Count;
                    int batchSize = 100;
                    int processedCount = 0; 


                    for(int i = 0; i < totalRows; i += batchSize)
                    {
                        var batch = allLines.Skip(i).Take(batchSize);
                        Console.WriteLine($"--------- Processing batch: {i} to {i + batchSize} ---------");


                        foreach(var line in batch)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            string[] parts = line.Split(',');


                            try
                            {
                                if(parts.Length<6)
                                {
                                    throw new Exception("Row does not contain enough columns");   ///doradi
                                }

                                SmartGridSample sample = new SmartGridSample
                                {
                                    Timestamp = DateTime.Parse(parts[0], CultureInfo.InvariantCulture),
                                    Voltage = double.Parse(parts[1], CultureInfo.InvariantCulture),
                                    Current = double.Parse(parts[2], CultureInfo.InvariantCulture),
                                    PowerUsage = double.Parse(parts[3], CultureInfo.InvariantCulture),
                                    Frequency = double.Parse(parts[4], CultureInfo.InvariantCulture),
                                    FaultIndicator = parts[5] == "1"

                                };

                                proxy.PushSample(sample);
                                processedCount++;
                                

                            }
                            catch(Exception ex)
                            {
                                string errorMsg = $"[Error - Row {processedCount + i}]: {ex.Message} | Content: {line}";
                                File.AppendAllText(clientLogPath, errorMsg + Environment.NewLine);
                                Console.WriteLine($" Skipped invalid row. Error logged in: {clientLogPath}");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Error: CSV file not fount at the specified path.");
                }

                proxy.EndSession();
                //proba
                //proxy.PushSample(new SmartGridSample { Voltage = -10, Current = 5, Frequency = 50 });

                if(factory.State != CommunicationState.Faulted)
                {
                    factory.Close();
                    Console.WriteLine("Connection closed successfully.");
                }
            }
            catch(FaultException<ValidationFault> ex)
            {
                Console.WriteLine($"[Validation Fault] Timestamp: {ex.Detail.Timestamp}");
                Console.WriteLine($"Message: {ex.Detail.Message}");
                factory.Abort();
            }
            catch(FaultException<DataFormatFault> ex)
            {
                Console.WriteLine($"[Data Format Fault] Violationg Field: {ex.Detail.ViolatingField}");
                Console.WriteLine($"Details: {ex.Detail.Details}");
                factory.Abort();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[General Error] {ex.Message}");
                factory.Abort();
            }
            finally
            {
                if(factory.State != CommunicationState.Closed)
                {
                    factory.Abort();
                }
                Console.WriteLine("Client resources released.");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();

        }
    }
}
