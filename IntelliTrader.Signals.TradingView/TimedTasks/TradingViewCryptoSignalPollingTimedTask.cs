using IntelliTrader.Core;
using IntelliTrader.Signals.Base;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace IntelliTrader.Signals.TradingView
{
    internal class TradingViewCryptoSignalPollingTimedTask : HighResolutionTimedTask
    {
        private const int HISTORICAL_SIGNALS_SNAPSHOT_MIN_INTERVAL_SECONDS = 45;
        private const int HISTORICAL_SIGNALS_ADDITIONAL_SAVE_MINUTES = 5;
        private const int HISTORICAL_SIGNALS_MAX_ADDITIONAL_ELAPSED_MINUTES = 1;

        private readonly ILoggingService loggingService;
        private readonly IHealthCheckService healthCheckService;
        private readonly ITradingService tradingService;
        private readonly TradingViewCryptoSignalReceiver signalReceiver;
        private readonly JsonSerializer signalsSerializer;
        private readonly HttpClient httpClient;

        private readonly ConcurrentDictionary<DateTimeOffset, List<Signal>> signalsHistory = new ConcurrentDictionary<DateTimeOffset, List<Signal>>();
        private DateTimeOffset lastSnapshotDate;
        private List<Signal> signals;
        private double? averageRating;

        private object syncRoot = new object();

        public TradingViewCryptoSignalPollingTimedTask(ILoggingService loggingService, IHealthCheckService healthCheckService,
            ITradingService tradingService, TradingViewCryptoSignalReceiver signalReceiver)
        {
            this.loggingService = loggingService;
            this.healthCheckService = healthCheckService;
            this.tradingService = tradingService;
            this.signalReceiver = signalReceiver;

            this.signalsSerializer = new JsonSerializer();
            this.signalsSerializer.Converters.Add(new TradingViewCryptoSignalConverter());
            this.httpClient = CreateHttpClient();
        }

        public override void Run()
        {
            var requestData = signalReceiver.Config.RequestData
                .Replace("%EXCHANGE%", tradingService.Config.Exchange.ToUpper())
                .Replace("%MARKET%", tradingService.Config.Market)
                .Replace("%PERIOD%", signalReceiver.Config.SignalPeriod <= 240 ? $"|{signalReceiver.Config.SignalPeriod}" : "")
                .Replace("%VOLATILITY%", $".{signalReceiver.Config.VolatilityPeriod?[0] ?? 'W'}");

            var requestContent = new StringContent(requestData, Encoding.UTF8, "application/json");
            try
            {
                using (var response = httpClient.PostAsync(signalReceiver.Config.RequestUrl, requestContent).Result)
                {
                    var responseContent = response.Content.ReadAsStringAsync().Result;
                    var jtokens = JObject.Parse(responseContent).SelectTokens("data[*].d");
                    lock (syncRoot)
                    {
                        List<Signal> historicalSignals = GetHistoricalSignals();
                        signals = jtokens.Select(t =>
                        {
                            try
                            {
                                var signal = t.ToObject<Signal>(signalsSerializer);
                                if (signal.Pair.EndsWith(tradingService.Config.Market))
                                {
                                    signal.Name = signalReceiver.SignalName;

                                    var historicalSignal = historicalSignals?.FirstOrDefault(s => s.Pair == signal.Pair);
                                    if (historicalSignal != null)
                                    {
                                        signal.VolumeChange = CalculatePercentageChange(historicalSignal.Volume, signal.Volume);
                                        signal.RatingChange = CalculatePercentageChange(historicalSignal.Rating, signal.Rating);
                                    }
                                    return signal;
                                }
                                else
                                {
                                    return null;
                                }
                            }
                            catch (Exception ex)
                            {
                                loggingService.Debug("Unable to parse Trading View Crypto Signal", ex);
                                return null;
                            }
                        }).Where(s => s != null && s.Pair != null).ToList();

                        if (signals.Count > 0)
                        {
                            if ((DateTimeOffset.Now - lastSnapshotDate).TotalSeconds > HISTORICAL_SIGNALS_SNAPSHOT_MIN_INTERVAL_SECONDS)
                            {
                                signalsHistory.TryAdd(DateTimeOffset.Now, signals);
                                lastSnapshotDate = DateTimeOffset.Now;
                                CleanUpSignalsHistory();
                            }
                            averageRating = signals.Any(s => s.Rating.HasValue) ? signals.Where(s => s.Rating.HasValue).Average(s => s.Rating) : null;
                            healthCheckService.UpdateHealthCheck($"{Constants.HealthChecks.TradingViewCryptoSignalsReceived} [{signalReceiver.SignalName}]", $"Total: {signals.Count()}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                loggingService.Debug("Unable to retrieve TV Signals", ex);
            }
        }

        public IEnumerable<ISignal> GetSignals()
        {
            lock (syncRoot)
            {
                return signals;
            }
        }

        public double? GetAverageRating()
        {
            lock (syncRoot)
            {
                return averageRating;
            }
        }

        private List<Signal> GetHistoricalSignals()
        {
            lock (syncRoot)
            {
                foreach (var date in signalsHistory.Keys.OrderByDescending(d => d))
                {
                    double elapsedMinutes = (DateTimeOffset.Now - date).TotalMinutes;
                    if (elapsedMinutes >= signalReceiver.Config.SignalPeriod && (elapsedMinutes - signalReceiver.Config.SignalPeriod) <= HISTORICAL_SIGNALS_MAX_ADDITIONAL_ELAPSED_MINUTES)
                    {
                        return signalsHistory[date];
                    }
                }
                return null;
            }
        }

        private void CleanUpSignalsHistory()
        {
            lock (syncRoot)
            {
                foreach (var date in signalsHistory.Keys)
                {
                    if ((DateTimeOffset.Now - date).TotalMinutes > signalReceiver.Config.SignalPeriod + HISTORICAL_SIGNALS_ADDITIONAL_SAVE_MINUTES)
                    {
                        signalsHistory.TryRemove(date, out List<Signal> signals);
                    }
                }
            }
        }

        private double? CalculatePercentageChange(double? a, double? b)
        {
            if (a != null && b != null)
            {
                if (a == 0 && b == 0 || a == b)
                {
                    return 0;
                }
                else if (a == 0)
                {
                    return 100 * Math.Sign((double)b);
                }
                else if (b == 0)
                {
                    return -100 * Math.Sign((double)a);
                }
                else
                {
                    var change = Math.Abs((double)((b - a) / a * 100));
                    return (a < b) ? change : change * -1;
                }
            }
            else
            {
                return null;
            }
        }

        private HttpClient CreateHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache, no-store, max-age=0, must-revalidate");
            return httpClient;
        }
    }
}
