using ExchangeSharp;
using IntelliTrader.Core;
using IntelliTrader.Exchange.Base;
using System;
using System.Collections.Generic;

namespace IntelliTrader.Exchange.Binance
{
    internal class BinanceExchangeService : ExchangeService
    {
        public BinanceExchangeService(ILoggingService loggingService, IHealthCheckService healthCheckService, ITasksService tasksService) :
            base(loggingService, healthCheckService, tasksService)
        {

        }

        protected override ExchangeAPI InitializeApi()
        {
            var binanceApi = new ExchangeBinanceAPI
            {
                RateLimit = new RateGate(Config.RateLimitOccurences, TimeSpan.FromSeconds(Config.RateLimitTimeframe))
            };
            return binanceApi;
        }

        public override IOrderDetails PlaceOrder(IOrder order, string originalPair = null)
        {
            var result = Api.PlaceOrderAsync(new ExchangeOrderRequest
            {
                OrderType = (ExchangeSharp.OrderType)(int)order.Type,
                IsBuy = order.Side == OrderSide.Buy,
                Amount = order.Amount,
                Price = order.Price,
                Symbol = order.Pair
            }).Result;

            return new OrderDetails
            {
                Side = result.IsBuy ? OrderSide.Buy : OrderSide.Sell,
                Result = (OrderResult)(int)result.Result,
                Date = result.OrderDate,
                OrderId = result.OrderId,
                Pair = result.Symbol,
                OriginalPair = originalPair,
                Message = result.Message,
                Amount = result.Amount,
                AmountFilled = result.AmountFilled,
                Price = result.Price,
                AveragePrice = result.AveragePrice,
                Fees = result.Fees,
                FeesCurrency = result.FeesCurrency
            };
        }

        public override IEnumerable<IOrderDetails> GetTrades(string pair)
        {
            var myTrades = new List<OrderDetails>();
            var results = ((ExchangeBinanceAPI)Api).GetMyTrades(pair);

            foreach (var result in results)
            {
                myTrades.Add(new OrderDetails
                {
                    Side = result.IsBuy ? OrderSide.Buy : OrderSide.Sell,
                    Result = (OrderResult)(int)result.Result,
                    Date = result.OrderDate,
                    OrderId = result.OrderId,
                    Pair = result.Symbol,
                    Message = result.Message,
                    Amount = result.Amount,
                    AmountFilled = result.AmountFilled,
                    Price = result.Price,
                    AveragePrice = result.AveragePrice,
                    Fees = result.Fees,
                    FeesCurrency = result.FeesCurrency
                });
            }

            return myTrades;
        }

        public override Arbitrage GetArbitrage(string pair, string tradingMarket, ArbitrageMarket? arbitrageMarket = null, ArbitrageType? arbitrageType = null)
        {
            Arbitrage arbitrage = new Arbitrage
            {
                Market = arbitrageMarket ?? ArbitrageMarket.ETH,
                Type = arbitrageType ?? ArbitrageType.Direct
            };

            try
            {
                if (tradingMarket == Constants.Markets.BTC)
                {
                    List<ArbitrageMarket> arbitrageMarkets = arbitrageMarket != null ? 
                        new List<ArbitrageMarket> { arbitrageMarket.Value } : 
                        new List<ArbitrageMarket> { ArbitrageMarket.ETH, ArbitrageMarket.BNB, ArbitrageMarket.USDT };

                    foreach (var market in arbitrageMarkets)
                    {
                        string marketPair = ChangeMarket(pair, market.ToString());
                        string arbitragePair = GetArbitrageMarketPair(market);

                        if (Tickers.TryGetValue(pair, out Ticker pairTicker) &&
                            Tickers.TryGetValue(marketPair, out Ticker marketTicker) &&
                            Tickers.TryGetValue(arbitragePair, out Ticker arbitrageTicker))
                        {
                            decimal directArbitragePercentage = 0;
                            decimal reverseArbitragePercentage = 0;

                            if (market == ArbitrageMarket.ETH)
                            {
                                directArbitragePercentage = (1 / pairTicker.AskPrice * marketTicker.BidPrice * arbitrageTicker.BidPrice - 1) * 100;
                                reverseArbitragePercentage = (1 / arbitrageTicker.AskPrice / marketTicker.AskPrice * pairTicker.BidPrice - 1) * 100;
                            }
                            else if (market == ArbitrageMarket.BNB)
                            {
                                directArbitragePercentage = (1 / pairTicker.AskPrice * marketTicker.BidPrice * arbitrageTicker.BidPrice - 1) * 100;
                                reverseArbitragePercentage = (1 / arbitrageTicker.AskPrice / marketTicker.AskPrice * pairTicker.BidPrice - 1) * 100;
                            }
                            else if (market == ArbitrageMarket.USDT)
                            {
                                directArbitragePercentage = (1 / pairTicker.AskPrice * marketTicker.BidPrice / arbitrageTicker.AskPrice - 1) * 100;
                                reverseArbitragePercentage = (arbitrageTicker.BidPrice / marketTicker.AskPrice * pairTicker.BidPrice - 1) * 100;
                            }

                            if ((directArbitragePercentage > arbitrage.Percentage || !arbitrage.IsAssigned) && (arbitrageType == null || arbitrageType == ArbitrageType.Direct))
                            {
                                arbitrage.IsAssigned = true;
                                arbitrage.Market = market;
                                arbitrage.Type = ArbitrageType.Direct;
                                arbitrage.Percentage = directArbitragePercentage;
                            }

                            if ((reverseArbitragePercentage > arbitrage.Percentage || !arbitrage.IsAssigned) && (arbitrageType == null || arbitrageType == ArbitrageType.Reverse))
                            {
                                arbitrage.IsAssigned = true;
                                arbitrage.Market = market;
                                arbitrage.Type = ArbitrageType.Reverse;
                                arbitrage.Percentage = reverseArbitragePercentage;
                            }
                        }
                    }
                }
            }
            catch { }
            return arbitrage;
        }

        public override string GetArbitrageMarketPair(ArbitrageMarket arbitrageMarket)
        {
            if (arbitrageMarket == ArbitrageMarket.ETH)
            {
                return Constants.Markets.ETH + Constants.Markets.BTC;
            }
            else if (arbitrageMarket == ArbitrageMarket.BNB)
            {
                return Constants.Markets.BNB + Constants.Markets.BTC;
            }
            else if (arbitrageMarket == ArbitrageMarket.USDT)
            {
                return Constants.Markets.BTC + Constants.Markets.USDT;
            }
            else
            {
                throw new NotSupportedException($"Unsupported arbitrage market: {arbitrageMarket}");
            }
        }
    }
}
