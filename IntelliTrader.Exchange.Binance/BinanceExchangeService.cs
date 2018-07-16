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

        public override decimal GetPriceArbitrage(string pair, string tradingMarket, ArbitrageMarket arbitrageMarket, ArbitrageType? arbitrageType = null)
        {
            try
            {
                if (tradingMarket == Constants.Markets.BTC)
                {
                    string marketPair = ChangeMarket(pair, arbitrageMarket.ToString());
                    string arbitragePair = GetArbitrageMarketPair(arbitrageMarket);

                    if (Tickers.TryGetValue(pair, out Ticker pairTicker) &&
                        Tickers.TryGetValue(marketPair, out Ticker marketTicker) &&
                        Tickers.TryGetValue(arbitragePair, out Ticker arbitrageTicker))
                    {
                        decimal directArbitrage = 0;
                        decimal reverseArbitrage = 0;

                        if (arbitrageMarket == ArbitrageMarket.ETH)
                        {
                            directArbitrage = (1 / pairTicker.AskPrice * marketTicker.BidPrice * arbitrageTicker.BidPrice - 1) * 100;
                            reverseArbitrage = (1 / arbitrageTicker.AskPrice / marketTicker.AskPrice * pairTicker.BidPrice - 1) * 100;
                        }
                        else if (arbitrageMarket == ArbitrageMarket.BNB)
                        {
                            directArbitrage = (1 / pairTicker.AskPrice * marketTicker.BidPrice * arbitrageTicker.BidPrice - 1) * 100;
                            reverseArbitrage = (1 / arbitrageTicker.AskPrice / marketTicker.AskPrice * pairTicker.BidPrice - 1) * 100;
                        }
                        else if (arbitrageMarket == ArbitrageMarket.USDT)
                        {
                            directArbitrage = (1 / pairTicker.AskPrice * marketTicker.BidPrice / arbitrageTicker.AskPrice - 1) * 100;
                            reverseArbitrage = (arbitrageTicker.BidPrice / marketTicker.AskPrice * pairTicker.BidPrice - 1) * 100;
                        }

                        if (arbitrageType == ArbitrageType.Direct)
                        {
                            return directArbitrage;
                        }
                        else if (arbitrageType == ArbitrageType.Reverse)
                        {
                            return reverseArbitrage;
                        }
                        else
                        {
                            return Math.Max(directArbitrage, reverseArbitrage);
                        }
                    }
                }
            }
            catch { }
            return 0;
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
