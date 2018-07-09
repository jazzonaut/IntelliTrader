using ExchangeSharp;
using IntelliTrader.Core;
using IntelliTrader.Exchange.Base;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

        public override IOrderDetails PlaceOrder(IOrder order, string priceCurrency = null)
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

        public override decimal GetPriceArbitrage(string pair, string crossMarket, string market)
        {
            try
            {
                if (market == Constants.Markets.BTC)
                {
                    string crossMarketPair = pair.Substring(0, pair.Length - market.Length) + crossMarket;
                    string marketPair = GetArbitrageMarket(crossMarket);

                    if (Tickers.TryGetValue(pair, out Ticker pairTicker) &&
                        Tickers.TryGetValue(crossMarketPair, out Ticker crossTicker) &&
                        Tickers.TryGetValue(marketPair, out Ticker marketTicker))
                    {
                        if (crossMarket == Constants.Markets.ETH)
                        {
                            // Buy XVGBTC, Sell XVGETH, Sell ETHBTC
                            decimal arb1 = Utils.CalculatePercentage(1, 1M / pairTicker.AskPrice * crossTicker.BidPrice * marketTicker.BidPrice);
                            // Buy ETHBTC, Buy XVGETH, Sell XVGBTC
                            decimal arb2 = Utils.CalculatePercentage(1M / crossTicker.AskPrice * pairTicker.BidPrice * marketTicker.AskPrice, 0);
                            return Math.Max(arb1, arb2);
                        }
                        else if (crossMarket == Constants.Markets.BNB)
                        {
                            // Buy XVGBTC, Sell XVGBNB, Sell BNBBTC
                            decimal arb1 = Utils.CalculatePercentage(1, 1M / pairTicker.AskPrice * crossTicker.BidPrice * marketTicker.BidPrice);
                            // Buy BNBBTC, Buy XVGBNB, Sell XVGBTC
                            decimal arb2 = Utils.CalculatePercentage(1M / crossTicker.AskPrice * pairTicker.BidPrice * marketTicker.AskPrice, 0);
                            return Math.Max(arb1, arb2);
                        }
                        else if (crossMarket == Constants.Markets.USDT)
                        {
                            // Buy XVGBTC, Sell XVGUSDT, Buy BTCUSDT
                            decimal arb1 = Utils.CalculatePercentage(1, 1M / pairTicker.AskPrice * crossTicker.BidPrice / marketTicker.AskPrice);
                            // Buy BTCUSDT, Buy XVGUSDT, Sell XVGBTC
                            decimal arb2 = Utils.CalculatePercentage(1M / pairTicker.BidPrice * crossTicker.AskPrice / marketTicker.BidPrice, 0);
                            return Math.Max(arb1, arb2);
                        }
                    }
                }
            }
            catch { }
            return 0;
        }

        public override string GetArbitrageMarket(string crossMarket)
        {
            if (crossMarket == Constants.Markets.ETH || crossMarket == Constants.Markets.BTC)
            {
                return Constants.Markets.ETH + Constants.Markets.BTC;
            }
            else if (crossMarket == Constants.Markets.BNB)
            {
                return Constants.Markets.BNB + Constants.Markets.BTC;
            }
            else if (crossMarket == Constants.Markets.USDT)
            {
                return Constants.Markets.BTC + Constants.Markets.USDT;
            }
            else
            {
                throw new NotSupportedException($"Unsupported arbitrage market: {crossMarket}");
            }
        }
    }
}
