using IntelliTrader.Core;
using IntelliTrader.Exchange.Base;
using System;
using System.Linq;

namespace IntelliTrader.Trading
{
    internal class OrderingService : IOrderingService
    {
        private readonly ILoggingService loggingService;
        private readonly INotificationService notificationService;
        private readonly ITradingService tradingService;

        public OrderingService(ILoggingService loggingService, INotificationService notificationService, ITradingService tradingService)
        {
            this.loggingService = loggingService;
            this.notificationService = notificationService;
            this.tradingService = tradingService;
        }

        public IOrderDetails PlaceBuyOrder(BuyOptions options)
        {
            IOrderDetails orderDetails = new OrderDetails();
            tradingService.StopTrailingBuy(options.Pair);
            tradingService.StopTrailingSell(options.Pair);

            if (tradingService.CanBuy(options, out string message))
            {
                try
                {
                    IPairConfig pairConfig = tradingService.GetPairConfig(options.Pair);
                    ITradingPair tradingPair = tradingService.Account.GetTradingPair(options.Pair);
                    decimal buyPrice = tradingService.GetPrice(options.Pair, TradePriceType.Ask);
                    string signalRule = options.Metadata.SignalRule ?? "N/A";
                    options.Metadata.TradingRules = pairConfig.Rules.ToList();
                    options.Metadata.LastBuyMargin = options.Metadata.LastBuyMargin ?? tradingPair?.CurrentMargin ?? 0;

                    BuyOrder buyOrder = new BuyOrder
                    {
                        Type = pairConfig.BuyType,
                        Date = DateTimeOffset.Now,
                        Pair = options.Pair,
                        Amount = options.Amount ?? (options.MaxCost.Value / buyPrice),
                        Price = buyPrice
                    };
                    buyOrder.Amount = tradingService.Exchange.ClampOrderAmount(buyOrder.Pair, buyOrder.Amount);
                    buyOrder.Price = tradingService.Exchange.ClampOrderPrice(buyOrder.Pair, buyOrder.Price);

                    lock (tradingService.Account.SyncRoot)
                    {
                        loggingService.Info($"Place buy order for {tradingPair?.FormattedName ?? options.Pair}. Price: {buyOrder.Price:0.00000000}, Amount: {buyOrder.Amount:0.########}, Signal Rule: {signalRule}");

                        if (!tradingService.Config.VirtualTrading)
                        {
                            orderDetails = tradingService.Exchange.PlaceOrder(buyOrder);
                        }
                        else
                        {
                            orderDetails = new OrderDetails
                            {
                                OrderId = DateTime.Now.ToFileTimeUtc().ToString(),
                                Side = OrderSide.Buy,
                                Result = OrderResult.Filled,
                                Date = buyOrder.Date,
                                Pair = buyOrder.Pair,
                                Amount = buyOrder.Amount,
                                AmountFilled = buyOrder.Amount,
                                Price = buyOrder.Price,
                                AveragePrice = buyOrder.Price,
                                Fees = buyOrder.Amount * buyOrder.Price * tradingService.Config.VirtualTradingFees,
                                FeesCurrency = tradingService.Exchange.GetPairMarket(options.Pair)
                            };
                        }

                        orderDetails.SetMetadata(options.Metadata);
                        NormalizePrice(orderDetails as OrderDetails, TradePriceType.Ask);
                        tradingService.Account.AddBuyOrder(orderDetails);
                        tradingService.Account.Save();
                        tradingService.LogOrder(orderDetails);

                        tradingPair = tradingService.Account.GetTradingPair(options.Pair);
                        loggingService.Info("{@Trade}", orderDetails);
                        loggingService.Info($"Buy order result for {tradingPair.FormattedName}: {orderDetails.Result} ({orderDetails.Message}). Price: {orderDetails.AveragePrice:0.00000000}, Amount: {orderDetails.Amount:0.########}, Filled: {orderDetails.AmountFilled:0.########}, Cost: {orderDetails.RawCost:0.00000000}");
                        notificationService.Notify($"Bought {tradingPair.FormattedName}. Amount: {orderDetails.AmountFilled:0.########}, Price: {orderDetails.AveragePrice:0.00000000}, Cost: {orderDetails.RawCost:0.00000000}");
                    }

                    tradingService.ReapplyTradingRules();
                }
                catch (Exception ex)
                {
                    loggingService.Error($"Unable to place buy order for {options.Pair}", ex);
                    notificationService.Notify($"Unable to buy {options.Pair}: {ex.Message}");
                }
            }
            else
            {
                loggingService.Info(message);
            }
            return orderDetails;
        }

        public IOrderDetails PlaceSellOrder(SellOptions options)
        {
            IOrderDetails orderDetails = new OrderDetails();
            tradingService.StopTrailingSell(options.Pair);
            tradingService.StopTrailingBuy(options.Pair);

            if (tradingService.CanSell(options, out string message))
            {
                try
                {
                    IPairConfig pairConfig = tradingService.GetPairConfig(options.Pair);
                    ITradingPair tradingPair = tradingService.Account.GetTradingPair(options.Pair);
                    string normalizedPair = tradingService.NormalizePair(options.Pair);
                    tradingPair.SetCurrentValues(tradingService.GetPrice(normalizedPair), tradingService.Exchange.GetPriceSpread(options.Pair));
                    decimal sellPrice = tradingService.GetPrice(normalizedPair, TradePriceType.Bid);

                    SellOrder sellOrder = new SellOrder
                    {
                        Type = pairConfig.SellType,
                        Date = DateTimeOffset.Now,
                        Pair = normalizedPair,
                        Amount = options.Amount ?? tradingPair.Amount,
                        Price = sellPrice
                    };
                    sellOrder.Amount = tradingService.Exchange.ClampOrderAmount(sellOrder.Pair, sellOrder.Amount);
                    sellOrder.Price = tradingService.Exchange.ClampOrderPrice(sellOrder.Pair, sellOrder.Price);

                    lock (tradingService.Account.SyncRoot)
                    {
                        loggingService.Info($"Place sell order for {normalizedPair ?? tradingPair.FormattedName}. Price: {sellOrder.Price:0.00000000}, Amount: {sellOrder.Amount:0.########}, Margin: {tradingPair.CurrentMargin:0.00}");

                        if (!tradingService.Config.VirtualTrading)
                        {
                            orderDetails = tradingService.Exchange.PlaceOrder(sellOrder, options.Pair);
                        }
                        else
                        {
                            orderDetails = new OrderDetails
                            {
                                OrderId = DateTime.Now.ToFileTimeUtc().ToString(),
                                Side = OrderSide.Sell,
                                Result = OrderResult.Filled,
                                Date = sellOrder.Date,
                                Pair = sellOrder.Pair,
                                OriginalPair = options.Pair,
                                Amount = sellOrder.Amount,
                                AmountFilled = sellOrder.Amount,
                                Price = sellOrder.Price,
                                AveragePrice = sellOrder.Price,
                                Fees = sellOrder.Amount * sellOrder.Price * tradingService.Config.VirtualTradingFees,
                                FeesCurrency = tradingService.Config.Market
                            };
                        }

                        tradingPair.Metadata.MergeWith(options.Metadata);
                        orderDetails.SetMetadata(tradingPair.Metadata);
                        NormalizePrice(orderDetails as OrderDetails, TradePriceType.Bid);

                        ITradeResult tradeResult = tradingService.Account.AddSellOrder(orderDetails);
                        tradeResult.SetSwap(options.Swap);
                        tradeResult.SetArbitrage(options.Arbitrage);
                        tradingService.Account.Save();
                        tradingService.LogOrder(orderDetails);

                        decimal soldMargin = (tradeResult.Profit / (tradeResult.ActualCost + (tradeResult.Metadata.AdditionalCosts ?? 0)) * 100);
                        string swapPair = options.Metadata.SwapPair != null ? $", Swap Pair: {options.Metadata.SwapPair}" : "";
                        loggingService.Info("{@Trade}", orderDetails);
                        loggingService.Info("{@Trade}", tradeResult);
                        loggingService.Info($"Sell order result for {normalizedPair ?? tradingPair.FormattedName}: {orderDetails.Result} ({orderDetails.Message}). Price: {orderDetails.AveragePrice:0.00000000}, Amount: {orderDetails.Amount:0.########}, Filled: {orderDetails.AmountFilled:0.########}, Margin: {soldMargin:0.00}, Profit: {tradeResult.Profit:0.00000000}");
                        notificationService.Notify($"Sold {normalizedPair ?? tradingPair.FormattedName}. Amount: {orderDetails.AmountFilled:0.########}, Price: {orderDetails.AveragePrice:0.00000000}, Margin: {soldMargin:0.00}, Profit: {tradeResult.Profit:0.00000000}{swapPair}");
                    }

                    tradingService.ReapplyTradingRules();
                }
                catch (Exception ex)
                {
                    loggingService.Error($"Unable to place sell order for {options.Pair}", ex);
                    notificationService.Notify($"Unable to sell {options.Pair}: {ex.Message}");
                }
            }
            else
            {
                loggingService.Info(message);
            }
            return orderDetails;
        }

        private void NormalizePrice(OrderDetails orderDetails, TradePriceType priceType)
        {
            if (!tradingService.IsNormalizedPair(orderDetails.Pair))
            {
                orderDetails.Price = tradingService.Exchange.ConvertPrice(orderDetails.Pair, orderDetails.Price, tradingService.Config.Market, priceType);
                orderDetails.AveragePrice = tradingService.Exchange.ConvertPrice(orderDetails.Pair, orderDetails.AveragePrice, tradingService.Config.Market, priceType);
                if (orderDetails.FeesCurrency == tradingService.Exchange.GetPairMarket(orderDetails.Pair))
                {
                    orderDetails.Fees = tradingService.Exchange.ConvertPrice(orderDetails.Pair, orderDetails.Fees, tradingService.Config.Market, priceType);
                    orderDetails.FeesCurrency = tradingService.Config.Market;
                }
            }
        }
    }
}
