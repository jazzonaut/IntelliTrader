using IntelliTrader.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Exchange.Base
{
    public class OrderDetails : IOrderDetails
    {
        public bool IsNormalized { get; set; }
        public OrderSide Side { get; set; }
        public OrderResult Result { get; set; }
        public DateTimeOffset Date { get; set; }
        public string OrderId { get; set; }
        public string Pair { get; set; }
        public string OriginalPair { get; set; }
        public string Message { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountFilled { get; set; }
        public decimal Price { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal Fees { get; set; }
        public string FeesCurrency { get; set; }
        public decimal Cost => AveragePrice * AmountFilled;
        public OrderMetadata Metadata { get; set; } = new OrderMetadata();
    }
}
