using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IntelliTrader.Web.Models
{
    public class RulesViewModel : BaseViewModel
    {
        public Dictionary<string, SignalRuleStats> SignalRuleStats { get; set; }
    }

    public class SignalRuleStats
    {
        public decimal TotalProfit { get; set; }
        public decimal TotalFees { get; set; }
        public decimal TotalCost { get; set; }
        public int TotalTrades { get; set; }
        public int TotalOrders { get; set; }
        public int TotalSwaps { get; set; }
        public List<double> Age { get; set; } = new List<double>();
        public List<decimal> Margin { get; set; } = new List<decimal>();
        public List<decimal> MarginDCA { get; set; } = new List<decimal>();
        public List<int> DCA { get; set; } = new List<int>();
    }
}
