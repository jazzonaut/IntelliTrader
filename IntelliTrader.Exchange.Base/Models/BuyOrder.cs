using System;
using System.Collections.Generic;
using System.Text;
using IntelliTrader.Core;

namespace IntelliTrader.Exchange.Base
{
    public class BuyOrder : Order
    {
        public override OrderSide Side => OrderSide.Buy;
    }
}
