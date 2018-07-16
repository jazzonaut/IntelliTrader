using IntelliTrader.Core;
using IntelliTrader.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Primitives;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

namespace IntelliTrader.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        #region Authentication

        [AllowAnonymous]
        public async Task<IActionResult> Login()
        {
            var coreService = Application.Resolve<ICoreService>();
            if (coreService.Config.PasswordProtected)
            {
                var model = new LoginViewModel
                {
                    RememberMe = true
                };
                return View(model);
            }
            else
            {
                return await PerformLogin(true);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var coreService = Application.Resolve<ICoreService>();
                var isValid = !coreService.Config.PasswordProtected || ComputeMD5Hash(model.Password).Equals(coreService.Config.Password, StringComparison.InvariantCultureIgnoreCase);
                if (!isValid)
                {
                    ModelState.AddModelError("Password", "Invalid Password");
                    return View(model);
                }
                else
                {
                    return await PerformLogin(model.RememberMe);
                }
            }
            else
            {
                return View(model);
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        private async Task<IActionResult> PerformLogin(bool persistent)
        {
            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
            var name = "user";
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, name));
            identity.AddClaim(new Claim(ClaimTypes.Name, name));
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties { IsPersistent = persistent });

            if (Request.Query.TryGetValue("ReturnUrl", out StringValues url))
            {
                return RedirectToAction(url);
            }
            else
            {
                return RedirectToAction(nameof(Index));
            }
        }

        private string ComputeMD5Hash(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentNullException(nameof(input));
            }

            using (var md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hash = md5.ComputeHash(inputBytes);

                var sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        #endregion Authentication

        public IActionResult Index()
        {
            return Dashboard();
        }

        public IActionResult Dashboard()
        {
            var coreService = Application.Resolve<ICoreService>();
            var webService = Application.Resolve<IWebService>();
            var model = new DashboardViewModel
            {
                InstanceName = coreService.Config.InstanceName,
                Version = coreService.Version,
                ReadOnlyMode = webService.Config.ReadOnlyMode
            };
            return View(nameof(Dashboard), model);
        }

        public IActionResult Market()
        {
            var coreService = Application.Resolve<ICoreService>();
            var webService = Application.Resolve<IWebService>();
            var model = new MarketViewModel
            {
                InstanceName = coreService.Config.InstanceName,
                Version = coreService.Version,
                ReadOnlyMode = webService.Config.ReadOnlyMode
            };
            return View(model);
        }

        public IActionResult Stats()
        {
            var coreService = Application.Resolve<ICoreService>();
            var webService = Application.Resolve<IWebService>();
            var tradingService = Application.Resolve<ITradingService>();
            var accountInitialBalance = tradingService.Config.VirtualTrading ? tradingService.Config.VirtualAccountInitialBalance : tradingService.Config.AccountInitialBalance;
            var accountInitialBalanceDate = tradingService.Config.VirtualTrading ? DateTimeOffset.Now.AddDays(-30) : tradingService.Config.AccountInitialBalanceDate;

            decimal accountBalance = tradingService.Account.GetBalance();
            foreach (var tradingPair in tradingService.Account.GetTradingPairs())
            {
                accountBalance += tradingService.GetPrice(tradingPair.Pair, TradePriceType.Bid) * tradingPair.Amount;
            }

            var model = new StatsViewModel
            {
                InstanceName = coreService.Config.InstanceName,
                Version = coreService.Version,
                ReadOnlyMode = webService.Config.ReadOnlyMode,
                TimezoneOffset = coreService.Config.TimezoneOffset,
                AccountInitialBalance = accountInitialBalance,
                AccountBalance = accountBalance,
                Market = tradingService.Config.Market,
                Balances = new Dictionary<DateTimeOffset, decimal>(),
                Trades = GetTrades()
            };

            foreach (var kvp in model.Trades.OrderBy(k => k.Key))
            {
                var date = kvp.Key;
                var trades = kvp.Value;

                model.Balances[date] = accountInitialBalance;

                if (date > accountInitialBalanceDate.Date)
                {
                    for (int d = 1; d < (int)(date - accountInitialBalanceDate.Date).TotalDays; d++)
                    {
                        var prevDate = date.AddDays(-d);
                        if (model.Trades.ContainsKey(prevDate))
                        {
                            model.Balances[date] += model.Trades[prevDate].Where(t => !t.IsSwap).Sum(t => t.Profit);
                        }
                    }
                }
            }

            return View(model);
        }

        public IActionResult Rules()
        {
            var allTades = GetTrades();
            var signalRuleStats = new Dictionary<string, SignalRuleStats>();
            foreach (var trade in allTades.Values.SelectMany(t => t))
            {
                if (trade.IsSuccessful)
                {
                    var signalRule = trade?.Metadata?.SignalRule;
                    if (!String.IsNullOrWhiteSpace(signalRule))
                    {
                        if (!signalRuleStats.TryGetValue(signalRule, out SignalRuleStats ruleStats))
                        {
                            ruleStats = new SignalRuleStats();
                            signalRuleStats.Add(signalRule, ruleStats);
                        }

                        if (!trade.IsSwap)
                        {
                            ruleStats.TotalCost += trade.ActualCost;
                            ruleStats.TotalProfit += trade.Profit;
                            decimal margin = trade.Profit / (trade.ActualCost + (trade.Metadata?.AdditionalCosts ?? 0)) * 100;
                            if (trade.OrderDates.Count == 1)
                            {
                                ruleStats.Margin.Add(margin);
                            }
                            else
                            {
                                ruleStats.MarginDCA.Add(margin);
                            }
                        }
                        else
                        {
                            ruleStats.TotalSwaps++;
                        }

                        ruleStats.TotalTrades++;
                        ruleStats.TotalOrders += trade.OrderDates.Count;
                        ruleStats.TotalFees += trade.FeesTotal;
                        ruleStats.Age.Add((trade.SellDate - trade.OrderDates.Min()).TotalDays);
                        ruleStats.DCA.Add((trade.OrderDates.Count - 1) + (trade.Metadata?.AdditionalDCALevels ?? 0));
                    }
                }
            }

            var coreService = Application.Resolve<ICoreService>();
            var webService = Application.Resolve<IWebService>();
            var model = new RulesViewModel
            {
                InstanceName = coreService.Config.InstanceName,
                Version = coreService.Version,
                ReadOnlyMode = webService.Config.ReadOnlyMode,
                SignalRuleStats = signalRuleStats
            };

            return View(model);
        }

        public IActionResult Trades(DateTimeOffset id)
        {
            var coreService = Application.Resolve<ICoreService>();
            var webService = Application.Resolve<IWebService>();
            var model = new TradesViewModel()
            {
                InstanceName = coreService.Config.InstanceName,
                Version = coreService.Version,
                ReadOnlyMode = webService.Config.ReadOnlyMode,
                TimezoneOffset = coreService.Config.TimezoneOffset,
                Date = id,
                Trades = GetTrades(id).Values.FirstOrDefault() ?? new List<TradeResult>()
            };

            return View(model);
        }

        public IActionResult Settings()
        {
            var coreService = Application.Resolve<ICoreService>();
            var webService = Application.Resolve<IWebService>();
            var tradingService = Application.Resolve<ITradingService>();
            var allConfigurableServices = Application.Resolve<IEnumerable<IConfigurableService>>();

            var model = new SettingsViewModel()
            {
                InstanceName = coreService.Config.InstanceName,
                Version = coreService.Version,
                ReadOnlyMode = webService.Config.ReadOnlyMode,
                BuyEnabled = tradingService.Config.BuyEnabled,
                BuyDCAEnabled = tradingService.Config.BuyDCAEnabled,
                SellEnabled = tradingService.Config.SellEnabled,
                TradingSuspended = tradingService.IsTradingSuspended,
                HealthCheckEnabled = coreService.Config.HealthCheckEnabled,
                Configs = allConfigurableServices.Where(s => !s.GetType().Name.Contains(Constants.ServiceNames.BacktestingService)).OrderBy(s => s.ServiceName).ToDictionary(s => s.ServiceName, s => Application.ConfigProvider.GetSectionJson(s.ServiceName))
            };

            return View(model);
        }

        public IActionResult Log()
        {
            var coreService = Application.Resolve<ICoreService>();
            var webService = Application.Resolve<IWebService>();
            var loggingService = Application.Resolve<ILoggingService>();

            var model = new LogViewModel()
            {
                InstanceName = coreService.Config.InstanceName,
                Version = coreService.Version,
                ReadOnlyMode = webService.Config.ReadOnlyMode,
                LogEntries = loggingService.GetLogEntries().Reverse().Take(500)
            };

            return View(model);
        }

        public IActionResult Help()
        {
            var coreService = Application.Resolve<ICoreService>();
            var webService = Application.Resolve<IWebService>();

            var model = new HelpViewModel()
            {
                InstanceName = coreService.Config.InstanceName,
                Version = coreService.Version,
                ReadOnlyMode = webService.Config.ReadOnlyMode
            };

            return View(model);
        }



        public IActionResult Status()
        {
            var loggingService = Application.Resolve<ILoggingService>();
            var tradingService = Application.Resolve<ITradingService>();
            var signalsService = Application.Resolve<ISignalsService>();
            var healthCheckService = Application.Resolve<IHealthCheckService>();

            var status = new
            {
                Balance = tradingService.Account.GetBalance(),
                GlobalRating = signalsService.GetGlobalRating()?.ToString("0.000") ?? "N/A",
                TrailingBuys = tradingService.GetTrailingBuys(),
                TrailingSells = tradingService.GetTrailingSells(),
                TrailingSignals = signalsService.GetTrailingSignals(),
                TradingSuspended = tradingService.IsTradingSuspended,
                HealthChecks = healthCheckService.GetHealthChecks().OrderBy(c => c.Name),
                LogEntries = loggingService.GetLogEntries().Reverse().Take(5)
            };
            return Json(status);
        }

        public IActionResult SignalNames()
        {
            var signalsService = Application.Resolve<ISignalsService>();
            return Json(signalsService.GetSignalNames());
        }

        [HttpPost]
        public IActionResult TradingPairs()
        {
            var coreService = Application.Resolve<ICoreService>();
            var tradingService = Application.Resolve<ITradingService>();

            var tradingPairs = from tradingPair in tradingService.Account.GetTradingPairs()
                               let pairConfig = tradingService.GetPairConfig(tradingPair.Pair)
                               select new
                               {
                                   Name = tradingPair.Pair,
                                   DCA = tradingPair.DCALevel,
                                   TradingViewName = $"{tradingService.Config.Exchange.ToUpperInvariant()}:{tradingPair.Pair}",
                                   Margin = tradingPair.CurrentMargin.ToString("0.00"),
                                   Target = pairConfig.SellMargin.ToString("0.00"),
                                   CurrentPrice = tradingPair.CurrentPrice.ToString("0.00000000"),
                                   CurrentSpread = tradingPair.CurrentSpread.ToString("0.00"),
                                   BoughtPrice = tradingPair.AveragePrice.ToString("0.00000000"),
                                   Cost = tradingPair.ActualCost.ToString("0.00000000"),
                                   CurrentCost = tradingPair.CurrentCost.ToString("0.00000000"),
                                   Amount = tradingPair.Amount.ToString("0.########"),
                                   OrderDates = tradingPair.OrderDates.Select(d => d.ToOffset(TimeSpan.FromHours(coreService.Config.TimezoneOffset)).ToString("yyyy-MM-dd HH:mm:ss")),
                                   OrderIds = tradingPair.OrderIds,
                                   Age = tradingPair.CurrentAge.ToString("0.00"),
                                   CurrentRating = tradingPair.Metadata.CurrentRating?.ToString("0.000") ?? "N/A",
                                   BoughtRating = tradingPair.Metadata.BoughtRating?.ToString("0.000") ?? "N/A",
                                   SignalRule = tradingPair.Metadata.SignalRule ?? "N/A",
                                   SwapPair = tradingPair.Metadata.SwapPair,
                                   TradingRules = pairConfig.Rules,
                                   IsTrailingSell = tradingService.GetTrailingSells().Contains(tradingPair.Pair),
                                   IsTrailingBuy = tradingService.GetTrailingBuys().Contains(tradingPair.Pair),
                                   LastBuyMargin = tradingPair.Metadata.LastBuyMargin?.ToString("0.00") ?? "N/A",
                                   Config = pairConfig
                               };

            return Json(tradingPairs);
        }

        [HttpPost]
        public IActionResult MarketPairs(List<string> signalsFilter)
        {
            var coreService = Application.Resolve<ICoreService>();
            var tradingService = Application.Resolve<ITradingService>();
            var signalsService = Application.Resolve<ISignalsService>();

            var allSignals = signalsService.GetAllSignals();
            if (allSignals != null)
            {
                if (signalsFilter.Count > 0)
                {
                    allSignals = allSignals.Where(s => signalsFilter.Contains(s.Name));
                }

                var groupedSignals = allSignals.GroupBy(s => s.Pair).ToDictionary(g => g.Key, g => g.AsEnumerable());

                var marketPairs = from signalGroup in groupedSignals
                                  let pair = signalGroup.Key
                                  let pairConfig = tradingService.GetPairConfig(pair)
                                  select new
                                  {
                                      Name = pair,
                                      TradingViewName = $"{tradingService.Config.Exchange.ToUpperInvariant()}:{pair}",
                                      VolumeList = signalGroup.Value.Select(s => new { s.Name, s.Volume }),
                                      VolumeChangeList = signalGroup.Value.Select(s => new { s.Name, s.VolumeChange }),
                                      Price = tradingService.GetPrice(pair).ToString("0.00000000"),
                                      PriceChangeList = signalGroup.Value.Select(s => new { s.Name, s.PriceChange }),
                                      RatingList = signalGroup.Value.Select(s => new { s.Name, s.Rating }),
                                      RatingChangeList = signalGroup.Value.Select(s => new { s.Name, s.RatingChange }),
                                      VolatilityList = signalGroup.Value.Select(s => new { s.Name, s.Volatility }),
                                      Spread = tradingService.Exchange.GetPriceSpread(pair).ToString("0.00"),
                                      ArbitrageList = tradingService.Exchange.GetMarkets().Where(m => m != tradingService.Config.Market)
                                        .Select(market => new { Name = market, Arbitrage = tradingService.Exchange.GetPriceArbitrage(pair, tradingService.Config.Market, Enum.Parse<ArbitrageMarket>(market)).ToString("0.00") }),
                                      SignalRules = signalsService.GetTrailingInfo(pair)?.Select(ti => ti.Rule.Name) ?? new string[0],
                                      HasTradingPair = tradingService.Account.HasTradingPair(pair),
                                      Config = pairConfig
                                  };

                return Json(marketPairs);
            }
            else
            {
                return Json(null);
            }
        }

        [HttpPost]
        public IActionResult Settings(SettingsViewModel model)
        {
            if (!Application.Resolve<IWebService>().Config.ReadOnlyMode)
            {
                var coreService = Application.Resolve<ICoreService>();
                var tradingService = Application.Resolve<ITradingService>();

                coreService.Config.HealthCheckEnabled = model.HealthCheckEnabled;
                tradingService.Config.BuyEnabled = model.BuyEnabled;
                tradingService.Config.BuyDCAEnabled = model.BuyDCAEnabled;
                tradingService.Config.SellEnabled = model.SellEnabled;

                if (model.TradingSuspended)
                {
                    tradingService.SuspendTrading();
                }
                else
                {
                    tradingService.ResumeTrading();
                }
                return Settings();
            }
            else
            {
                return Settings();
            }
        }

        [HttpPost]
        public IActionResult SaveConfig()
        {
            string configName = Request.Form["name"].ToString();
            string configDefinition = Request.Form["definition"].ToString();

            if (!Application.Resolve<IWebService>().Config.ReadOnlyMode && !String.IsNullOrWhiteSpace(configName) && !String.IsNullOrWhiteSpace(configDefinition))
            {
                Application.ConfigProvider.SetSectionJson(configName, configDefinition);
                return new OkResult();
            }
            else
            {
                return new BadRequestResult();
            }
        }

        [HttpPost]
        public IActionResult Sell()
        {
            string pair = Request.Form["pair"].ToString();
            if (!Application.Resolve<IWebService>().Config.ReadOnlyMode && pair != null && decimal.TryParse(Request.Form["amount"], out decimal amount) && amount > 0)
            {
                var tradingService = Application.Resolve<ITradingService>();
                tradingService.Sell(new SellOptions(pair)
                {
                    Amount = amount,
                    ManualOrder = true
                });
                return new OkResult();
            }
            else
            {
                return new BadRequestResult();
            }
        }

        [HttpPost]
        public IActionResult Buy()
        {
            string pair = Request.Form["pair"].ToString();
            if (!Application.Resolve<IWebService>().Config.ReadOnlyMode && !String.IsNullOrWhiteSpace(pair) && decimal.TryParse(Request.Form["amount"], out decimal amount) && amount > 0)
            {
                var tradingService = Application.Resolve<ITradingService>();
                tradingService.Buy(new BuyOptions(pair)
                {
                    Amount = amount,
                    IgnoreExisting = true,
                    ManualOrder = true
                });
                return new OkResult();
            }
            else
            {
                return new BadRequestResult();
            }
        }

        [HttpPost]
        public IActionResult BuyDefault()
        {
            string pair = Request.Form["pair"].ToString();
            if (!Application.Resolve<IWebService>().Config.ReadOnlyMode && !String.IsNullOrWhiteSpace(pair))
            {
                var signalsService = Application.Resolve<ISignalsService>();
                var tradingService = Application.Resolve<ITradingService>();
                tradingService.Buy(new BuyOptions(pair)
                {
                    MaxCost = tradingService.GetPairConfig(pair).BuyMaxCost,
                    IgnoreExisting = true,
                    ManualOrder = true,
                    Metadata = new OrderMetadata
                    {
                        BoughtGlobalRating = signalsService.GetGlobalRating()
                    }
                });
                return new OkResult();
            }
            else
            {
                return new BadRequestResult();
            }
        }

        [HttpPost]
        public IActionResult Swap()
        {
            string pair = Request.Form["pair"].ToString();
            string swap = Request.Form["swap"].ToString();
            if (!Application.Resolve<IWebService>().Config.ReadOnlyMode && !String.IsNullOrWhiteSpace(pair) && !String.IsNullOrWhiteSpace(swap))
            {
                var tradingService = Application.Resolve<ITradingService>();
                tradingService.Swap(new SwapOptions(pair, swap, new OrderMetadata())
                {
                    ManualOrder = true
                });
                return new OkResult();
            }
            else
            {
                return new BadRequestResult();
            }
        }

        public IActionResult RefreshAccount()
        {
            if (!Application.Resolve<IWebService>().Config.ReadOnlyMode)
            {
                var tradingService = Application.Resolve<ITradingService>();
                tradingService.Account.Refresh();
                return new OkResult();
            }
            else
            {
                return new BadRequestResult();
            }
        }

        public IActionResult RestartServices()
        {
            if (!Application.Resolve<IWebService>().Config.ReadOnlyMode)
            {
                var coreService = Application.Resolve<ICoreService>();
                coreService.Restart();
                return new OkResult();
            }
            else
            {
                return new BadRequestResult();
            }
        }

        private Dictionary<DateTimeOffset, List<TradeResult>> GetTrades(DateTimeOffset? date = null)
        {
            var coreService = Application.Resolve<ICoreService>();
            var logsPath = Path.Combine(Directory.GetCurrentDirectory(), "log");
            var tradeResultPattern = new Regex($"{nameof(TradeResult)} (?<data>\\{{.*\\}})", RegexOptions.Compiled);
            var trades = new Dictionary<DateTimeOffset, List<TradeResult>>();

            if (Directory.Exists(logsPath))
            {
                foreach (var tradesLogFilePath in Directory.EnumerateFiles(logsPath, "*-trades.txt", SearchOption.TopDirectoryOnly))
                {
                    IEnumerable<string> logLines = Utils.ReadAllLinesWriteSafe(tradesLogFilePath);
                    foreach (var logLine in logLines)
                    {
                        var match = tradeResultPattern.Match(logLine);
                        if (match.Success)
                        {
                            var data = match.Groups["data"].ToString();
                            var json = Utils.FixInvalidJson(data.Replace(nameof(OrderMetadata), ""))
                                .Replace("AveragePricePaid", nameof(ITradeResult.AveragePrice)); // Old property migration

                            TradeResult tradeResult = JsonConvert.DeserializeObject<TradeResult>(json);
                            if (tradeResult.IsSuccessful)
                            {
                                DateTimeOffset tradeDate = tradeResult.SellDate.ToOffset(TimeSpan.FromHours(coreService.Config.TimezoneOffset)).Date;
                                if (date == null || date == tradeDate)
                                {
                                    if (!trades.ContainsKey(tradeDate))
                                    {
                                        trades.Add(tradeDate, new List<TradeResult>());
                                    }
                                    trades[tradeDate].Add(tradeResult);
                                }
                            }
                        }
                    }
                }
            }
            return trades;
        }
    }
}
