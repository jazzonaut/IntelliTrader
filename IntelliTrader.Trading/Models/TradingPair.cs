using IntelliTrader.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IntelliTrader.Trading
{
    public class TradingPair : ITradingPair
    {
        public string Pair { get; set; }
        [JsonIgnore]
        public string FormattedName => DCALevel > 0 ? $"{Pair}({DCALevel})" : Pair;
        public int DCALevel => (OrderDates.Count - 1) + (Metadata.AdditionalDCALevels ?? 0);
        public List<string> OrderIds { get; set; }
        public List<DateTimeOffset> OrderDates { get; set; }
        [JsonConverter(typeof(DecimalFormatJsonConverter), 8)]
        public decimal TotalAmount { get; set; }
        [JsonConverter(typeof(DecimalFormatJsonConverter), 8)]
        public decimal AveragePricePaid { get; set; }
        [JsonConverter(typeof(DecimalFormatJsonConverter), 8)]
        public decimal FeesPairCurrency { get; set; }
        [JsonConverter(typeof(DecimalFormatJsonConverter), 8)]
        public decimal FeesMarketCurrency { get; set; }
        [JsonConverter(typeof(DecimalFormatJsonConverter), 8)]
        public decimal AverageCostPaid => AveragePricePaid * (TotalAmount + FeesPairCurrency) + FeesMarketCurrency;
        [JsonConverter(typeof(DecimalFormatJsonConverter), 8)]
        public decimal CurrentCost => CurrentPrice * TotalAmount;
        [JsonConverter(typeof(DecimalFormatJsonConverter), 8)]
        public decimal CurrentPrice { get; set; }
        [JsonConverter(typeof(DecimalFormatJsonConverter), 2)]
        public decimal CurrentMargin => Utils.CalculateMargin(AverageCostPaid + (Metadata.AdditionalCosts ?? 0), CurrentCost);
        public double CurrentAge => OrderDates != null && OrderDates.Count > 0 ? (DateTimeOffset.Now - OrderDates.Min()).TotalDays : 0;
        public double LastBuyAge => OrderDates != null && OrderDates.Count > 0 ? (DateTimeOffset.Now - OrderDates.Max()).TotalDays : 0;
        public OrderMetadata Metadata { get; set; } = new OrderMetadata();

        public void SetCurrentPrice(decimal currentPrice)
        {
            CurrentPrice = currentPrice;
        }
    }
}
