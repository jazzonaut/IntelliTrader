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
                    decimal currentPrice = tradingService.GetPrice(options.Pair, TradePriceType.Ask);
                    options.Metadata.TradingRules = pairConfig.Rules.ToList();
                    options.Metadata.LastBuyMargin = options.Metadata.LastBuyMargin ?? tradingPair?.CurrentMargin ?? 0;
                    string signalRule = options.Metadata.SignalRule ?? "N/A";

                    BuyOrder buyOrder = new BuyOrder
                    {
                        Type = pairConfig.BuyType,
                        Date = DateTimeOffset.Now,
                        Pair = options.Pair,
                        Amount = options.Amount ?? (options.MaxCost.Value / currentPrice),
                        Price = currentPrice
                    };

                    lock (tradingService.Account.SyncRoot)
                    {
                        loggingService.Info($"Place buy order for {tradingPair?.FormattedName ?? options.Pair}. Price: {buyOrder.Price:0.00000000}, Amount: {buyOrder.Amount:0.########}, Signal Rule: {signalRule}");

                        if (!tradingService.Config.VirtualTrading)
                        {
                            orderDetails = tradingService.Exchange.PlaceOrder(buyOrder);
                            orderDetails.SetMetadata(options.Metadata);
                            ConvertToMarketPrice(orderDetails as OrderDetails, orderDetails.AveragePrice, TradePriceType.Ask);
                        }
                        else
                        {
                            decimal buyPrice = currentPrice;
                            decimal roundedAmount = Math.Round(buyOrder.Amount, 4);
                            orderDetails = new OrderDetails
                            {
                                OrderId = DateTime.Now.ToFileTimeUtc().ToString(),
                                Side = OrderSide.Buy,
                                Result = OrderResult.Filled,
                                Date = buyOrder.Date,
                                Pair = buyOrder.Pair,
                                Amount = roundedAmount,
                                AmountFilled = roundedAmount,
                                Price = buyPrice,
                                AveragePrice = buyPrice,
                                Fees = roundedAmount * buyPrice * tradingService.Config.VirtualTradingFees,
                                FeesCurrency = tradingService.Exchange.GetPairMarket(options.Pair)
                            };
                            orderDetails.SetMetadata(options.Metadata);
                            ConvertToMarketPrice(orderDetails as OrderDetails, buyPrice, TradePriceType.Bid);
                        }

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
                    tradingPair.SetCurrentValues(tradingService.GetPrice(tradingService.NormalizePair(options.Pair)), tradingService.Exchange.GetPriceSpread(options.Pair));

                    SellOrder sellOrder = new SellOrder
                    {
                        Type = pairConfig.SellType,
                        Date = DateTimeOffset.Now,
                        Pair = options.Pair,
                        Amount = options.Amount ?? tradingPair.Amount,
                        Price = tradingPair.CurrentPrice
                    };

                    lock (tradingService.Account.SyncRoot)
                    {
                        loggingService.Info($"Place sell order for {tradingPair.FormattedName}. Price: {sellOrder.Price:0.00000000}, Amount: {sellOrder.Amount:0.########}, Margin: {tradingPair.CurrentMargin:0.00}");

                        if (!tradingService.Config.VirtualTrading)
                        {
                            orderDetails = tradingService.Exchange.PlaceOrder(sellOrder, options.Metadata.ArbitrageMarket);
                            tradingPair.Metadata.MergeWith(options.Metadata);
                            orderDetails.SetMetadata(tradingPair.Metadata);
                            ConvertToMarketPrice(orderDetails as OrderDetails, orderDetails.AveragePrice, TradePriceType.Bid);
                        }
                        else
                        {
                            decimal sellPrice = tradingService.GetPrice(options.Pair, TradePriceType.Bid);
                            orderDetails = new OrderDetails
                            {
                                OrderId = DateTime.Now.ToFileTimeUtc().ToString(),
                                Side = OrderSide.Sell,
                                Result = OrderResult.Filled,
                                Date = sellOrder.Date,
                                Pair = sellOrder.Pair,
                                Amount = sellOrder.Amount,
                                AmountFilled = sellOrder.Amount,
                                Price = sellPrice,
                                AveragePrice = sellPrice,
                                Fees = sellOrder.Amount * sellPrice * tradingService.Config.VirtualTradingFees,
                                FeesCurrency = tradingService.Exchange.GetPairMarket(options.Pair)
                            };
                            tradingPair.Metadata.MergeWith(options.Metadata);
                            orderDetails.SetMetadata(tradingPair.Metadata);
                            ConvertToMarketPrice(orderDetails as OrderDetails, sellPrice, TradePriceType.Bid);
                        }

                        ITradeResult tradeResult = tradingService.Account.AddSellOrder(orderDetails);
                        tradeResult.SetSwap(options.Swap);
                        tradeResult.SetArbitrage(options.Arbitrage);
                        tradingService.Account.Save();
                        tradingService.LogOrder(orderDetails);

                        decimal soldMargin = (tradeResult.Profit / (tradeResult.ActualCost + (tradeResult.Metadata.AdditionalCosts ?? 0)) * 100);
                        string swapPair = options.Metadata.SwapPair != null ? $", Swap Pair: {options.Metadata.SwapPair}" : "";
                        loggingService.Info("{@Trade}", orderDetails);
                        loggingService.Info("{@Trade}", tradeResult);
                        loggingService.Info($"Sell order result for {tradingPair.FormattedName}: {orderDetails.Result} ({orderDetails.Message}). Price: {orderDetails.AveragePrice:0.00000000}, Amount: {orderDetails.Amount:0.########}, Filled: {orderDetails.AmountFilled:0.########}, Margin: {soldMargin:0.00}, Profit: {tradeResult.Profit:0.00000000}");
                        notificationService.Notify($"Sold {tradingPair.FormattedName}. Amount: {orderDetails.AmountFilled:0.########}, Price: {orderDetails.AveragePrice:0.00000000}, Margin: {soldMargin:0.00}, Profit: {tradeResult.Profit:0.00000000}{swapPair}");
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

        private void ConvertToMarketPrice(OrderDetails orderDetails, decimal price, TradePriceType priceType)
        {
            if (!tradingService.IsNormalizedPair(orderDetails.Pair))
            {
                decimal convertedPrice = tradingService.Exchange.ConvertPrice(orderDetails.Pair, price, tradingService.Config.Market, priceType);
                orderDetails.Price *= convertedPrice;
                orderDetails.AveragePrice *= convertedPrice;
                if (orderDetails.FeesCurrency == tradingService.Exchange.GetPairMarket(orderDetails.Pair))
                {
                    orderDetails.Fees *= convertedPrice;
                    orderDetails.FeesCurrency = tradingService.Config.Market;
                }
            }
        }
    }
}
