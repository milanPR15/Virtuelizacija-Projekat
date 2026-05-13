using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Publisher
{
    internal class TransferGenerator
    {
        public delegate void TransferEventHandler(object sender, EventArgs e);

        public event TransferEventHandler OnTransferStarted;
        public event TransferEventHandler OnTransferCompleted;

        public void GenerateTransfer()
        {
            StartTransfer();
            System.Threading.Thread.Sleep(1000);
            CompleteTransfer();
        }

        private void StartTransfer()
        {
            if (OnTransferStarted != null)
            {
                OnTransferStarted(this, EventArgs.Empty);
            }
            else
            {
                Console.WriteLine("No subscribers!");
            }
        }

        private void CompleteTransfer()
        {
            if (OnTransferCompleted != null)
            {
                OnTransferCompleted(this, EventArgs.Empty);
            }
            else
            {
                Console.WriteLine("No subscribers!");
            }
        }
    }
}
