using System;
using System.ServiceModel;
using Common;

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

                proxy.StartSession("Session_01");

                Console.WriteLine("Client is ready to send data.");
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
