using IntelliTrader.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IntelliTrader.Web.Models
{
    public class StatsViewModel : BaseViewModel
    {
        public double TimezoneOffset { get; set; }
        public decimal AccountInitialBalance { get; set; }
        public decimal AccountBalance { get; set; }
        public string Market { get; set; }        
        public Dictionary<DateTimeOffset, List<TradeResult>> Trades { get; set; }
        public Dictionary<DateTimeOffset, decimal> Balances { get; set; }
    }
}
