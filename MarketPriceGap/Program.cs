using Binance.Net;
using Discord;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MarketPriceGap
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();

            IConfiguration config = new ConfigurationBuilder()
                      .AddJsonFile("appsettings.json", true, true)
                      .Build();

            var discord = new Discord(Convert.ToUInt64(config["WebHookId"]), config["WebHookToken"]);

            var interval = Convert.ToInt32(config["interval"]);
            string[] pairs = { "BTCUSDT" };
            var gaplist = new Dictionary<string, int>();

            while (true)
            {
                //Thread.Sleep(interval);

                foreach (var pair in pairs)
                {
                    var market = GetMarketPrice(pair);
                    var price = GetPrice(pair);
                    var gapPercent = GetPrcentAbs(market, price);
                    Console.WriteLine($"Pair: {pair} \r\nPriceGap: {gapPercent:##.##}");

                    if (gapPercent >= 2m)
                    {
                        if (!gaplist.ContainsKey(pair))
                        {
                            gaplist.Add(pair, 0);
                        }
                        else
                        {
                            gaplist[pair]++;
                            if (gaplist[pair] > 2)
                            {
                                Console.WriteLine($"Pair: {pair} \r\nPriceGap: {gapPercent:##.##}");
                                discord.Send(pair, $"Market price: {price}\r\nPrice: {price}\r\nGap: {gapPercent:##.##}%", Color.Gold);
                            }
                        }
                    }
                    else
                    {
                        if (gaplist.ContainsKey(pair))
                        {
                            gaplist.Remove(pair);
                        }
                    }
                }

                GC.Collect();
            }
        }

        static decimal GetMarketPrice(string pair)
        {
            using BinanceClient binance = new BinanceClient();
            var result = binance.FuturesUsdt.Market.GetPrice(pair);

            return binance.FuturesUsdt.Market.GetMarkPrices(pair).Data.Last().MarkPrice;
        }

        static decimal GetPrice(string pair)
        {
            BinanceClient Client = new BinanceClient();
            return Client.FuturesUsdt.Market.GetPrice(pair).Data.Price;
        }

        static decimal GetPrcentAbs(decimal value1, decimal value2)
        {
            var percentage = (value1 / value2 * 100) - 100;
            return percentage < 0 ? percentage * -1 : percentage;
        }
    }
}
