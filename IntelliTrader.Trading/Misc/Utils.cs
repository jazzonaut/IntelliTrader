using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Trading
{
    public static class Utils
    {
        public static decimal CalculateMargin(decimal oldValue, decimal newValue)
        {
            return (newValue - oldValue) / oldValue * 100;
        }
    }
}
