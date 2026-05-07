using System;
using Common;

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
            Console.WriteLine($"Sample received: Voltage={sample.Voltage}, Current={sample.Current}");
        }

        public void EndSession()
        {
            Console.WriteLine("Session ended.");
        }
    }
}
