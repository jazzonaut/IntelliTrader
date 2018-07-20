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
        public decimal Amount { get; set; }
        [JsonConverter(typeof(DecimalFormatJsonConverter), 8)]
        public decimal AveragePrice { get; set; }
        [JsonConverter(typeof(DecimalFormatJsonConverter), 8)]
        public decimal FeesPairCurrency { get; set; }
        [JsonConverter(typeof(DecimalFormatJsonConverter), 8)]
        public decimal FeesMarketCurrency { get; set; }
        [JsonConverter(typeof(DecimalFormatJsonConverter), 8)]
        public decimal FeesTotal => AveragePrice * FeesPairCurrency + FeesMarketCurrency;
        [JsonConverter(typeof(DecimalFormatJsonConverter), 8)]
        public decimal RawCost => AveragePrice * Amount;
        [JsonConverter(typeof(DecimalFormatJsonConverter), 8)]
        public decimal ActualCost => GetActualCost(Amount);
        [JsonIgnore]
        public decimal? ActualCostOverride { get; set; }
        [JsonIgnore]
        public decimal CurrentCost => CurrentPrice * Amount;
        [JsonIgnore]
        public decimal CurrentPrice { get; set; }
        [JsonIgnore]
        public decimal CurrentSpread { get; set; }
        [JsonIgnore]
        public decimal CurrentMargin => Utils.CalculatePercentage(ActualCost + (Metadata.AdditionalCosts ?? 0), CurrentCost);
        [JsonIgnore]
        public double CurrentAge => OrderDates != null && OrderDates.Count > 0 ? (DateTimeOffset.Now - OrderDates.Min()).TotalDays : 0;
        [JsonIgnore]
        public double LastBuyAge => OrderDates != null && OrderDates.Count > 0 ? (DateTimeOffset.Now - OrderDates.Max()).TotalDays : 0;
        public OrderMetadata Metadata { get; set; } = new OrderMetadata();

        public decimal GetActualCost(decimal partialAmount)
        {
            if (ActualCostOverride != null)
            {
                return ActualCostOverride.Value;
            }
            else
            {
                return AveragePrice * (partialAmount + FeesPairCurrency) + FeesMarketCurrency;
            }
        }

        public void OverrideActualCost(decimal? actualCostOverride)
        {
            ActualCostOverride = actualCostOverride;
        }

        public void SetCurrentValues(decimal currentPrice, decimal currentSpread)
        {
            CurrentPrice = currentPrice;
            CurrentSpread = currentSpread;
        }

        public void SetMetadata(OrderMetadata metadata)
        {
            this.Metadata = metadata;
        }
    }
}
