using System;
using System.ServiceModel;
using Common;

namespace Service
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Service Starting...");
            using (ServiceHost host = new ServiceHost(typeof(SmartGridService)))
            {
                host.Open();
                Console.WriteLine("Service started. Press Enter to stop.");
                Console.ReadLine();
            }
        }
    }
}
