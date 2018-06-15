using IntelliTrader.Core;
using System;
using System.Collections.Generic;

namespace IntelliTrader.Exchange.Base
{
    public abstract class Order : IOrder
    {
        public abstract OrderSide Side { get; }
        public OrderType Type { get; set; }
        public DateTimeOffset Date { get; set; }
        public string Pair { get; set; }
        public decimal Amount { get; set; }
        public decimal Price { get; set; }
    }
}
