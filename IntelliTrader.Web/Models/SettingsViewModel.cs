using IntelliTrader.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace IntelliTrader.Web.Models
{
    public class SettingsViewModel : BaseViewModel
    {
        [Display(Name = "Buy Enabled")]
        public bool BuyEnabled { get; set; }
        [Display(Name = "Buy DCA Enabled")]
        public bool BuyDCAEnabled { get; set; }
        [Display(Name = "Sell Enabled")]
        public bool SellEnabled { get; set; }
        [Display(Name = "Trading Suspended")]
        public bool TradingSuspended { get; set; }
        [Display(Name = "Health Check Enabled")]
        public bool HealthCheckEnabled { get; set; }
        public Dictionary<string, string> Configs { get; set; }
    }
}
