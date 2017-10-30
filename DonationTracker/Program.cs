using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DonationTracker
{
    public class Program
    {
        private const int TeamZe = 35254;
        
        public static void Main(string[] args)
        {
            using (var tracker = new Tracker(TeamZe, "Donations.txt", "Totals.txt"))
            {
                tracker.Start().Wait();
                Console.WriteLine("Press any key to stop.");
                Console.ReadKey(true);
            }
        }
    }
}
