using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DonationTracker
{
    public struct VolatileDouble
    {
        private long _value;

        public double Value
        {
            get => BitConverter.Int64BitsToDouble(Interlocked.Read(ref _value));
            set => Interlocked.Exchange(ref _value, BitConverter.DoubleToInt64Bits(value));
        }

        public VolatileDouble(double value)
        {
            _value = BitConverter.DoubleToInt64Bits(value);
        }
    }
}
