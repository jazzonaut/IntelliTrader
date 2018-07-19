using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    public interface IOrderDetails
    {
        bool IsNormalized { get; }
        OrderSide Side { get; }
        OrderResult Result { get; }
        DateTimeOffset Date { get; }
        string OrderId { get; }
        string Pair { get; }
        string OriginalPair { get; }
        string Message { get; }
        decimal Amount { get; }
        decimal AmountFilled { get; }
        decimal Price { get; }
        decimal AveragePrice { get; }
        decimal Fees { get; }
        string FeesCurrency { get; }
        decimal RawCost { get; }
        OrderMetadata Metadata { get; }
    }
}
