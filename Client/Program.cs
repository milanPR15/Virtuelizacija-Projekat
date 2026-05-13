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
            
            ChannelFactory<ISmartGridService> factory = new ChannelFactory<ISmartGridService>("SmartGridEndpoint");

            try
            {
                Console.WriteLine("Connection to the server.");
                ISmartGridService proxy = factory.CreateChannel();

                proxy.StartSession("Bulk_Upload_Session");

                string filePath = ConfigurationManager.AppSettings["CsvFilePath"];

                if(File.Exists(filePath))
                {
                    Console.WriteLine("Reading dataset..");
                    var lines = File.ReadAllLines(filePath);

                    int count = 0;
                    foreach(var line in lines.Skip(1))
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        string[] parts = line.Split(',');

                        try
                        {
                            SmartGridSample sample = new SmartGridSample
                            {
                                Voltage = double.Parse(parts[1], CultureInfo.InvariantCulture),
                                Current = double.Parse(parts[2], CultureInfo.InvariantCulture),
                                Frequency = double.Parse(parts[4], CultureInfo.InvariantCulture)
                            };


                            proxy.PushSample(sample);
                            System.Threading.Thread.Sleep(1000);

                            count++;

                            if(count % 100 == 0)
                            {
                                Console.WriteLine($"Successfully sent {count} samples.");
                            }
                        }
                        catch (FaultException<ValidationFault> ex)
                        {
                            Console.WriteLine($"[Validation Skip] {ex.Detail.Message}");
                        }
                        catch (FaultException<DataFormatFault> ex)
                        {
                            Console.WriteLine($"[Format Skip]{ex.Detail.Details}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Line Error] Skipping line. Message: {ex.Message}");
                        }
                    }
                    Console.WriteLine($"Total samples provessed: {count}");
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
