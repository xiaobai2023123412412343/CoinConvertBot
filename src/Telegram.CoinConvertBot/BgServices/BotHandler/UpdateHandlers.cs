using FreeSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.CoinConvertBot.Domains.Tables;
using Telegram.CoinConvertBot.Helper;
using Telegram.CoinConvertBot.Models;
using TronNet;
using TronNet.Contracts;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Data;
using System.Text;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Numerics;
using System.Globalization;
using System.Collections.Concurrent;

namespace Telegram.CoinConvertBot.BgServices.BotHandler;

//yifanfu或@yifanfu或t.me/yifanfu为管理员ID
//yifanfubot或t.me/yifanfubot或@yifanfubot为机器人ID
//TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv为监控的收款地址
//TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv为监控的转账地址
// 将这个值替换为目标群组的ID
//const long TARGET_CHAT_ID = -1002006327353;//指定群聊转发用户对机器人发送的信息
// 将这个值替换为你的机器人用户名
//const string BOT_USERNAME = "yifanfubot";//机器人用户名
// 指定管理员ID
//const int ADMIN_ID = 1427768220;//指定管理员ID不转发
// 将这个值替换为目标群组的ID
//const long TARGET_CHAT_ID = -1002006327353;//指定群聊转发用户对机器人发送的信息
//    await botClient.SendTextMessageAsync(
//        chatId: -1002006327353, // 群聊ID   用户点击按钮 自动在指定群聊 艾特作者 已取消！！！！！
//        text: $"@yifanfu 有人需要帮助，用户名： @{update.CallbackQuery.From.Username} 用户ID：{update.CallbackQuery.From.Id}"
//    );
//    static GroupManager()  广告发到指定群聊
//    {
//        // 添加初始群组 ID
//        groupIds.Add(-1001862069013);  // 用你的初始群组 ID 替换 
//        //groupIds.Add(-994581226);  // 添加第二个初始群组 ID
//    }
//    if (message.From.Id == 1427768220 && message.Chat.Type == ChatType.Group)  指定管理员可以发送：开启广告 关闭广告
//拉黑+id  拉白+id
// 获取任务的结果
//decimal todayIncome = Math.Round(todayIncomeTask.Result, 2);
//decimal monthlyIncome = Math.Round(monthlyIncomeTask.Result, 2);
//decimal totalIncome = Math.Round(totalIncomeTask.Result - 19045, 2); 累计承兑-21639USDT  如果是新号可以不用减
// 先发送GIF
// string gifUrl = "https://i.postimg.cc/0QKYJ0Cb/333.gif"; // 替换为您的GIF URL  网站自己上传
// 发送GIF和带按钮的文本
// string gifUrl = "https://i.postimg.cc/Jzrm1m9c/277574078-352558983556639-7702866525169266409-n.png"; 自己注册
// 获取24小时爆仓信息 后面为网站秘钥 coinglass注册免费获取
// decimal h24TotalVolUsd = await GetH24TotalVolUsdAsync("https://open-api.coinglass.com/public/v2/liquidation_info?time_type=h24&symbol=all", "9e8ff0ca25f14355a015972f21f162de");
//(decimal btcLongRate, decimal btcShortRate) = await GetH24LongShortAsync("https://open-api.coinglass.com/public/v2/long_short?time_type=h24&symbol=BTC", "9e8ff0ca25f14355a015972f21f162de");
//(decimal ethLongRate, decimal ethShortRate) = await GetH1EthLongShortAsync("https://open-api.coinglass.com/public/v2/long_short?time_type=h1&symbol=ETH", "9e8ff0ca25f14355a015972f21f162de");
//谷歌 关键词 搜索注释掉了 
//if (message.From.Id == 1427768220 && message.Text.StartsWith("群发 "))  指定用户可以群发
//发送用户名：**或ID：**  会触发储存资料
//运行机器人发送 /yccl   启动全局异常处理    /qdgg  启动广告
//代绑 id 地址  可以帮用户绑定地址 代解 id 用户名 （可选）地址 帮用户解绑地址  原理是模仿用户发送 绑定指令/解绑指令
//添加群聊：群名字： 群ID： 群链接：
//Console.WriteLine($"API URL: {apiUrl}, Response status code: {response.StatusCode}");//增加调试输出日志输出服务器日志 都可以用这个方法
//                "5090e006-163f-4d61-8fa1-1f41fa70d7f8",
//                "f49353bd-db65-4719-a56c-064b2eb231bf",
//                "92854974-68da-4fd8-9e50-3948c1e6fa7e"     ok链api     https://www.oklink.com/cn/account/my-api  注册
// 指数秘钥  private static readonly List<string> licences = new List<string> { "504ddb535666d9312d", "64345c8caebdd5133d", "94181401476c458453" };  string url = $"http://api.mairui.club/zs/sssj/{indexCode}/{licence}";


public static class UpdateHandlers
{
    public static string? BotUserName = null!;
    public static IConfiguration configuration = null!;
    public static IFreeSql freeSql = null!;
    public static IServiceScopeFactory serviceScopeFactory = null!;
    public static long AdminUserId => configuration.GetValue<long>("BotConfig:AdminUserId");
    public static string AdminUserUrl => configuration.GetValue<string>("BotConfig:AdminUserUrl");
    public static decimal MinUSDT => configuration.GetValue("MinToken:USDT", 5m);
    public static decimal FeeRate => configuration.GetValue("FeeRate", 0.1m);
    public static decimal USDTFeeRate => configuration.GetValue("USDTFeeRate", 0.01m);
    /// <summary>
    /// 错误处理
    /// </summary>
    /// <param name="botClient"></param>
    /// <param name="exception"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
//统计非小号币种数据
private static Dictionary<long, (int count, DateTime lastQueryDate)> userShizhiLimits = new Dictionary<long, (int count, DateTime lastQueryDate)>(); //限制用户每日查询次数字典	
public static class CryptoMarketAnalyzer
{
    private static readonly HttpClient httpClient = new HttpClient();

    //获取各平台永续合约资金费
    public static async Task<string> GetFundingRateAsync(string symbol)
    {
        try
        {
            // 尝试从抹茶(MEXC)获取资金费率
            string mexcUrl = $"https://contract.mexc.com/api/v1/contract/funding_rate/{symbol}_USDT";
            var mexcResponse = await httpClient.GetStringAsync(mexcUrl);
            var mexcData = JsonSerializer.Deserialize<JsonElement>(mexcResponse);
            if (mexcData.GetProperty("success").GetBoolean() && mexcData.GetProperty("code").GetInt32() == 0)
            {
                decimal fundingRate = mexcData.GetProperty("data").GetProperty("fundingRate").GetDecimal();
                return $"{fundingRate * 100:0.####}%";
            }

            // 尝试从币安(Binance)获取资金费率
            string binanceUrl = $"https://fapi.binance.com/fapi/v1/premiumIndex?symbol={symbol}USDT";
            var binanceResponse = await httpClient.GetStringAsync(binanceUrl);
            var binanceData = JsonSerializer.Deserialize<JsonElement>(binanceResponse);
            if (binanceData.TryGetProperty("lastFundingRate", out var lastFundingRate))
            {
                decimal fundingRate = lastFundingRate.GetDecimal();
                return $"{fundingRate * 100:0.####}%";
            }

            // 尝试从欧意(OKX)获取资金费率
            string okxUrl = $"https://www.okx.com/api/v5/public/funding-rate?instId={symbol}-USD-SWAP";
            var okxResponse = await httpClient.GetStringAsync(okxUrl);
            var okxData = JsonSerializer.Deserialize<JsonElement>(okxResponse);
            if (okxData.GetProperty("code").GetString() == "0" && okxData.GetProperty("data").GetArrayLength() > 0)
            {
                decimal fundingRate = okxData.GetProperty("data")[0].GetProperty("fundingRate").GetDecimal();
                return $"{fundingRate * 100:0.####}%";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取资金费率失败: {ex.Message}");
        }

        // 如果所有API都没有返回有效的资金费率
        return "无合约资金费";
    }	
    //private static readonly string ApiUrl = "https://fxhapi.feixiaohao.com/public/v1/ticker?limit=450"; //已取消从api获取 直接从本地获取

    public static async Task AnalyzeAndReportAsync(ITelegramBotClient botClient, long chatId)
    {
        try
        {
            // 获取比特币和以太坊的数据
            var btcData = await CoinDataCache.GetCoinInfoAsync("BTC");
            var ethData = await CoinDataCache.GetCoinInfoAsync("ETH");

            decimal btcPercentChange1h = btcData != null && btcData.ContainsKey("percent_change_1h") ? btcData["percent_change_1h"].GetDecimal() : 0m;
            decimal btcPercentChange24h = btcData != null && btcData.ContainsKey("percent_change_24h") ? btcData["percent_change_24h"].GetDecimal() : 0m;
            decimal btcPercentChange7d = btcData != null && btcData.ContainsKey("percent_change_7d") ? btcData["percent_change_7d"].GetDecimal() : 0m;

            decimal ethPercentChange1h = ethData != null && ethData.ContainsKey("percent_change_1h") ? ethData["percent_change_1h"].GetDecimal() : 0m;
            decimal ethPercentChange24h = ethData != null && ethData.ContainsKey("percent_change_24h") ? ethData["percent_change_24h"].GetDecimal() : 0m;
            decimal ethPercentChange7d = ethData != null && ethData.ContainsKey("percent_change_7d") ? ethData["percent_change_7d"].GetDecimal() : 0m;

            // 获取所有币种的数据
            var allCoinsData = CoinDataCache.GetAllCoinsData();
            var coins = allCoinsData.Values.ToList();

            var filteredAndSortedCoins = coins
                .Where(coin =>
                    coin["volume_24h_usd"].GetDecimal() >= coin["market_cap_usd"].GetDecimal() * 0.4m && //24小时成交量占比市值>40%
                    coin["percent_change_24h"].GetDecimal() > 5m && //24小时涨幅大于5%
                    coin["percent_change_24h"].GetDecimal() <= 20m && //24小时涨幅小于20%
                    coin["percent_change_1h"].GetDecimal() > 0m &&  //近1小时涨幅大于0%  不想要比特币数据直接： 0m) //近1小时涨幅大于0%
                   ((btcPercentChange24h > 0 && coin["percent_change_24h"].GetDecimal() > btcPercentChange24h) || // 近24小时如果比特币上涨，币种涨幅需大于比特币
                   (btcPercentChange24h < 0 && (coin["percent_change_24h"].GetDecimal() > btcPercentChange24h || coin["percent_change_24h"].GetDecimal() >= 0)))) // 近24小时如果比特币下跌，币种跌幅需小于比特币或者币种为上涨
                .Select(coin => new
                {
                    Id = coin["id"].GetString(), // 获取币种ID
                    Symbol = coin["symbol"].GetString(),
                    PriceUsd = coin["price_usd"].GetDecimal(),
                    Rank = coin["rank"].GetInt32(),
                    MarketCapUsd = coin["market_cap_usd"].GetDecimal() / 1_000_000m,
                    Volume24hUsd = coin["volume_24h_usd"].GetDecimal() / 1_000_000m,
                    VolumePercentage = coin["volume_24h_usd"].GetDecimal() / coin["market_cap_usd"].GetDecimal() * 100m,
                    PercentChange1h = coin["percent_change_1h"].GetDecimal(),
                    PercentChange24h = coin["percent_change_24h"].GetDecimal(),
                    PercentChange7d = coin["percent_change_7d"].GetDecimal()
                })
                .Where(coin => chatId == 1427768220 || coin.Symbol != "TRX") // 如果使用者ID非1427768220，则不包含TRX
                .OrderByDescending(coin => coin.VolumePercentage)
                .Take(10);

            if (!filteredAndSortedCoins.Any())
            {
                var customQueryKeyboard = new InlineKeyboardMarkup(new[]
                {
		    InlineKeyboardButton.WithCallbackData("查BTC", "查BTC"),
                    InlineKeyboardButton.WithCallbackData("查ETH", "查ETH"),
                    InlineKeyboardButton.WithCallbackData("自定义查询", "/genjuzhiding")
                });		    
		    
                string noDataMessage = "暂未发现财富密码，持续监控中...\n\n" +
                           "判断标准：\n" +
                           "近1小时涨幅大于0%\n" +
                           "24小时成交量占比市值>40%\n" +
                           "24小时涨幅大于5%，小于20%\n" +
                           "24小时比特币上涨的话，涨幅大于比特币\n" +
                           "24小时比特币下跌的话，跌幅小于比特币\n\n" +
                           "如果有更合理的条件判断，欢迎联系作者！";

                await botClient.SendTextMessageAsync(chatId, noDataMessage, ParseMode.Html, replyMarkup: customQueryKeyboard);
                return;
            }

            foreach (var coin in filteredAndSortedCoins)
            {
                // 尝试获取资金费率
                string fundingRateMessage = await GetFundingRateAsync(coin.Symbol);
                string fundingRateDisplay = fundingRateMessage != "无合约资金费" ? $" 资金费：{fundingRateMessage}" : "";	
		    
                string marketCapDisplay = coin.MarketCapUsd >= 100 ? $"{Math.Round(coin.MarketCapUsd / 100, 2)}亿" : $"{Math.Round(coin.MarketCapUsd, 2)}m";
                string volume24hDisplay = coin.Volume24hUsd >= 100 ? $"{Math.Round(coin.Volume24hUsd / 100, 2)}亿" : $"{Math.Round(coin.Volume24hUsd, 2)}m";

                string change1hSymbol = coin.PercentChange1h >= 0 ? "\U0001F4C8" : "\U0001F4C9";
                string change24hSymbol = coin.PercentChange24h >= 0 ? "\U0001F4C8" : "\U0001F4C9";
                string change7dSymbol = coin.PercentChange7d >= 0 ? "\U0001F4C8" : "\U0001F4C9";

                string message = $"<code>{coin.Symbol}</code>   价格:$ {coin.PriceUsd} 排名：No.{coin.Rank} {fundingRateDisplay}\n" +
                                 $"市值：{marketCapDisplay}，24小时成交：{volume24hDisplay}，占比：{Math.Round(coin.VolumePercentage, 2)}%\n" +
                                 $"1h{change1hSymbol}：{coin.PercentChange1h}% | 24h{change24hSymbol}：{coin.PercentChange24h}% | 7d{change7dSymbol}：{coin.PercentChange7d}%";

                // 创建内联键盘
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] // 第一排按钮
                    {
                        InlineKeyboardButton.WithUrl("合约数据", "https://www.coinglass.com/zh/BitcoinOpenInterest"),
                        InlineKeyboardButton.WithUrl($"{coin.Symbol}详细数据", $"https://www.feixiaohao.com/currencies/{coin.Id}/")
                    },
                    new[] // 第二排按钮，增加监控行情波动按钮
                    {
                        InlineKeyboardButton.WithCallbackData($"监控 {coin.Symbol} 行情波动", $"监控 {coin.Symbol}")
                    }
                });
                await botClient.SendTextMessageAsync(chatId, message, ParseMode.Html, replyMarkup: inlineKeyboard);
            }
            // 在发送完币种数据后，构建并发送比特币和以太坊的涨跌数据
            string btcChange1hSymbol = btcPercentChange1h >= 0 ? "\U0001F4C8" : "\U0001F4C9";
            string btcChange24hSymbol = btcPercentChange24h >= 0 ? "\U0001F4C8" : "\U0001F4C9";
            string btcChange7dSymbol = btcPercentChange7d >= 0 ? "\U0001F4C8" : "\U0001F4C9";

            string ethChange1hSymbol = ethPercentChange1h >= 0 ? "\U0001F4C8" : "\U0001F4C9";
            string ethChange24hSymbol = ethPercentChange24h >= 0 ? "\U0001F4C8" : "\U0001F4C9";
            string ethChange7dSymbol = ethPercentChange7d >= 0 ? "\U0001F4C8" : "\U0001F4C9";
		
        // 过滤掉TRX并获取近1小时上涨最多的前三个币种
        var top3CoinsBy1hChange = allCoinsData.Values
            .Where(coin => coin["symbol"].GetString() != "TRX")
            .OrderByDescending(coin => coin["percent_change_1h"].GetDecimal())
            .Take(3)
            .Select(coin => new { Symbol = coin["symbol"].GetString(), Change = coin["percent_change_1h"].GetDecimal() })
            .ToList(); // 使用ToList确保是一个具体的集合类型

        // 过滤掉TRX并获取近24小时上涨最多的前三个币种
        var top3CoinsBy24hChange = allCoinsData.Values
            .Where(coin => coin["symbol"].GetString() != "TRX")
            .OrderByDescending(coin => coin["percent_change_24h"].GetDecimal())
            .Take(3)
            .Select(coin => new { Symbol = coin["symbol"].GetString(), Change = coin["percent_change_24h"].GetDecimal() })
            .ToList(); // 使用ToList确保是一个具体的集合类型

        // 构建汇总消息
        string summaryMessage = $"<b>BTC</b> 1h{btcChange1hSymbol}：{btcPercentChange1h:F2}% | 24h{btcChange24hSymbol}：{btcPercentChange24h:F2}% | 7d{btcChange7dSymbol}：{btcPercentChange7d:F2}%\n" +
                                $"<b>ETH</b> 1h{ethChange1hSymbol}：{ethPercentChange1h:F2}% | 24h{ethChange24hSymbol}：{ethPercentChange24h:F2}% | 7d{ethChange7dSymbol}：{ethPercentChange7d:F2}%\n\n" +
                                $"1小时涨幅榜：\n{string.Join(" | ", top3CoinsBy1hChange.Select((coin, index) => $"{index + 1}️⃣ {coin.Symbol} ：{coin.Change:F2}%"))}\n\n" +
                                $"24小时涨幅榜：\n{string.Join(" | ", top3CoinsBy24hChange.Select((coin, index) => $"{index + 4}️⃣ {coin.Symbol} ：{coin.Change:F2}%"))}";

        // 创建内联键盘按钮，横排排列
        var inlineKeyboardButtons = new List<InlineKeyboardButton>();
        int index = 1;
        foreach (var coin in top3CoinsBy1hChange.Concat(top3CoinsBy24hChange))
        {
            inlineKeyboardButtons.Add(InlineKeyboardButton.WithCallbackData($"{index}️⃣", $"查{coin.Symbol.ToLower()}"));
            index++;
        }

        var summaryInlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            inlineKeyboardButtons.ToArray(), // 第一排按钮，包含1-6的按钮
            new[] // 新增第二排按钮
            {
                InlineKeyboardButton.WithCallbackData("查BTC", "查BTC"),
                InlineKeyboardButton.WithCallbackData("查ETH", "查ETH"),		    
                InlineKeyboardButton.WithCallbackData("自定义查询", "/genjuzhiding")
            }
        });

        await botClient.SendTextMessageAsync(chatId, summaryMessage, ParseMode.Html, replyMarkup: summaryInlineKeyboard);
		
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API请求失败: {ex.Message}");
            await botClient.SendTextMessageAsync(chatId, "数据超时，请稍后重试！", ParseMode.Html);
        }
    }
}
	
// 用户查询1小时数据次数字典
private static Dictionary<long, (int count, DateTime lastQueryDate)> user1hShujuLimits = new Dictionary<long, (int count, DateTime lastQueryDate)>();	
// 用户查询24小时数据次数字典
private static Dictionary<long, (int count, DateTime lastQueryDate)> user24hQueryLimits = new Dictionary<long, (int count, DateTime lastQueryDate)>();	
// 用户查询7天数据次数字典
private static Dictionary<long, (int count, DateTime lastQueryDate)> user7dQueryLimits = new Dictionary<long, (int count, DateTime lastQueryDate)>();	
	
//非小号查币    
public static class CoinDataCache
{
    private static bool _initialized = false; // 用于标记是否已经初始化和开始缓存更新
    private static Dictionary<string, Dictionary<string, JsonElement>> _coinData = new();
    private static readonly string ApiUrl = "https://fxhapi.feixiaohao.com/public/v1/ticker?limit=450";
    private static Timer _timer;
    private static readonly HttpClient _httpClient = new();

    static CoinDataCache()
    {
        // 移除初始的数据更新调用，改为按需更新
    }
    public static Dictionary<string, Dictionary<string, JsonElement>> GetAllCoinsData()   //获取本地缓存所有币种的信息
    {
        return _coinData;
    }
    // 在 CoinDataCache 类中添加一个新的方法来获取排序后的币种信息
    public static async Task<(string, InlineKeyboardMarkup)> GetTopMoversAsync(string timeFrame)
    {
        await EnsureCacheInitializedAsync(); // 确保缓存已初始化

        string percentChangeKey = timeFrame switch
        {
            "1h" => "percent_change_1h",
            "24h" => "percent_change_24h",
            "7d" => "percent_change_7d",
            _ => throw new ArgumentException("Invalid time frame", nameof(timeFrame))
        };

        // 先筛选出非TRX的币种，然后根据变化幅度排序
        var topMovers = _coinData
            .Where(kv => kv.Key != "TRX") // 过滤掉TRX
            .Select(kv => new
            {
                Symbol = kv.Key,
                PercentChange = kv.Value.TryGetValue(percentChangeKey, out JsonElement percentChangeElement) && percentChangeElement.TryGetDouble(out double percentChange) ? percentChange : 0.0,
                PriceUsd = kv.Value.TryGetValue("price_usd", out JsonElement priceElement) && priceElement.TryGetDouble(out double price) ? price : 0.0
            })
            .ToList();

        // 获取上涨和下跌的前5名
        var topRisers = topMovers.Where(x => x.PercentChange > 0).OrderByDescending(x => x.PercentChange).Take(5);
        var topFallers = topMovers.Where(x => x.PercentChange < 0).OrderBy(x => x.PercentChange).Take(5);

        string message = $"全网{timeFrame}上涨TOP5：\n";
        List<InlineKeyboardButton[]> rows = new List<InlineKeyboardButton[]>();
        InlineKeyboardButton[] row1 = new InlineKeyboardButton[5];
        InlineKeyboardButton[] row2 = new InlineKeyboardButton[5];
        int index = 0;

        foreach (var mover in topRisers)
        {
            message += $"{index}️⃣ {mover.Symbol} \U0001F4C8 {mover.PercentChange:F2}%   $：{mover.PriceUsd:F2}\n";
            row1[index] = InlineKeyboardButton.WithCallbackData($"{index}️⃣", $"查{mover.Symbol.ToLower()}");
            index++;
        }

        message += $"\n全网{timeFrame}下跌TOP5：\n";
        foreach (var mover in topFallers)
        {
            message += $"{index}️⃣ {mover.Symbol} \U0001F4C9 {mover.PercentChange:F2}%   $：{mover.PriceUsd:F2}\n";
            row2[index - 5] = InlineKeyboardButton.WithCallbackData($"{index}️⃣", $"查{mover.Symbol.ToLower()}");
            index++;
        }

        // 添加按钮行
        rows.Add(row1);
        rows.Add(row2);
        // 添加关闭按钮
        rows.Add(new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("关闭", "back") });

        var inlineKeyboard = new InlineKeyboardMarkup(rows);
        return (message, inlineKeyboard);
    }
    public static async Task EnsureCacheInitializedAsync()
    {
        if (!_initialized)
        {
            await UpdateDataAsync(retryCount: 3);
            StartTimer();
            _initialized = true; // 标记为已初始化
        }
    }   
    // 确保StartTimer方法是public或者被EnsureCacheInitializedAsync调用
    private static void StartTimer()
    {
        if (_timer == null)
        {
            _timer = new Timer(async _ =>
            {
                //Console.WriteLine("Timer triggered for data update.");
                await UpdateDataAsync(retryCount: 3);
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(new Random().Next(45, 61)));
        }
    }

    public static async Task<Dictionary<string, JsonElement>> GetCoinInfoAsync(string symbol)
    {
        if (!_coinData.ContainsKey(symbol.ToUpper()))
        {
            //Console.WriteLine($"Cache miss for {symbol}. Fetching data...");
            await UpdateDataAsync(retryCount: 3);
            StartTimer(); // 确保计时器在首次需要时启动
        }
        else
        {
            //Console.WriteLine($"Cache hit for {symbol}.");
        }

        _coinData.TryGetValue(symbol.ToUpper(), out var coinInfo);
        return coinInfo;
    }
    
    //储存币种到本地数据库
    private static async Task UpdateDataAsync(int retryCount)
    {
        for (int attempt = 0; attempt < retryCount; attempt++)
        {
            try
            {
                //Console.WriteLine("Attempting to fetch data from API...");
                var response = await _httpClient.GetStringAsync(ApiUrl);
                var coins = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(response);
                if (coins != null)
                {
                    var newCoinData = new Dictionary<string, Dictionary<string, JsonElement>>();
                    foreach (var coin in coins)
                    {
                        if (coin.TryGetValue("symbol", out JsonElement symbolElement))
                        {
                            var symbol = symbolElement.GetString();
                            if (!string.IsNullOrEmpty(symbol) && !symbol.Equals("TRX", StringComparison.OrdinalIgnoreCase)) //如果是TRX  直接不缓存数据
                            {
                                newCoinData[symbol.ToUpper()] = coin;
                            }
                        }
                    }
                    _coinData = newCoinData; // 更新缓存
                    //Console.WriteLine("Data updated successfully.");
                    break; // 成功更新数据后退出循环
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Failed to fetch data from API: {ex.Message}");
                if (attempt == retryCount - 1) // 最后一次尝试仍然失败
                {
                    Console.WriteLine("Final attempt to fetch data failed. Waiting for next cycle.");
                }
                await Task.Delay(5000); // 等待一段时间后重试
            }
        }
    }
}
public static async Task QueryCoinInfoAsync(ITelegramBotClient botClient, long chatId, string coinSymbol)
{
    try
    {
        // 如果是TRX，直接返回特定信息和按钮
        if (coinSymbol.Equals("TRX", StringComparison.OrdinalIgnoreCase))
        {
            var trxKeyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithUrl("点击进群", "https://t.me/+b4NunT6Vwf0wZWI1")
            });

            await botClient.SendTextMessageAsync(chatId, "TRX数据请进群查看！", ParseMode.Html, replyMarkup: trxKeyboard);
            return;
        }	    
        var coinInfo = await CoinDataCache.GetCoinInfoAsync(coinSymbol);
        if (coinInfo == null)
        {
            //Console.WriteLine("No data found for the requested symbol.");
            await botClient.SendTextMessageAsync(chatId, "未查到该币种的信息！", ParseMode.Html);
            return;
        }

            string symbol = coinInfo["symbol"].GetString();
	    string id = coinInfo["id"].GetString(); 
            decimal priceUsd = coinInfo["price_usd"].GetDecimal();
            decimal marketCapUsd = coinInfo["market_cap_usd"].GetDecimal();
            int rank = coinInfo["rank"].GetInt32();
            decimal volume24hUsd = coinInfo["volume_24h_usd"].GetDecimal();
            decimal percentChange1h = coinInfo["percent_change_1h"].GetDecimal();
            decimal percentChange24h = coinInfo["percent_change_24h"].GetDecimal();
            decimal percentChange7d = coinInfo["percent_change_7d"].GetDecimal();

            string marketCapDisplay = marketCapUsd >= 100_000_000 ? $"{Math.Round(marketCapUsd / 100_000_000, 2)}亿" : $"{Math.Round(marketCapUsd / 1_000_000, 2)}m";
            string volume24hDisplay = volume24hUsd >= 100_000_000 ? $"{Math.Round(volume24hUsd / 100_000_000, 2)}亿" : $"{Math.Round(volume24hUsd / 1_000_000, 2)}m";

            string change1hSymbol = percentChange1h >= 0 ? "\U0001F4C8" : "\U0001F4C9";
            string change24hSymbol = percentChange24h >= 0 ? "\U0001F4C8" : "\U0001F4C9";
            string change7dSymbol = percentChange7d >= 0 ? "\U0001F4C8" : "\U0001F4C9";

            string message = $"<code>{symbol}</code> 价格：$ {priceUsd}\n" +
                             $"市值：{marketCapDisplay}  No.<b>{rank}</b>\n" +
                             $"24小时成交：${volume24hDisplay}\n" +
                             $"1h{change1hSymbol}：{percentChange1h}%\n" +
                             $"24h{change24hSymbol}：{percentChange24h}%\n" +
                             $"7d{change7dSymbol}：{percentChange7d}%";

var keyboard = new InlineKeyboardMarkup(new[]
{
    // 第一排按钮
    new[]
    {
        InlineKeyboardButton.WithUrl("合约", "https://www.coinglass.com/zh/BitcoinOpenInterest"),
        InlineKeyboardButton.WithUrl($"详情", $"https://www.feixiaohao.com/currencies/{id}/")
    },
    // 第二排按钮
    new[]
    {
	InlineKeyboardButton.WithCallbackData("监控", $"监控 {symbol}"), // 添加监控按钮   
        InlineKeyboardButton.WithCallbackData("关闭", "back")
    }
});

        await botClient.SendTextMessageAsync(chatId, message, ParseMode.Html, replyMarkup: keyboard);
        
    }
    catch (Exception ex)
    {
        Console.WriteLine($"API请求失败: {ex.Message}");
        await botClient.SendTextMessageAsync(chatId, "数据超时，请稍后重试！", ParseMode.Html);
    }
}
//非小号大数据	
public static class CryptoDataFetcher
{
    public static string FetchAndFormatCryptoDataAsync(int startRank, int endRank)
    {
        try
        {
            var cryptos = CoinDataCache.GetAllCoinsData().Values.ToList();
            // 筛选出排名在指定范围内的加密货币
            var filteredCryptos = cryptos.Where(crypto =>
                crypto.ContainsKey("rank") &&
                crypto["rank"].GetInt32() >= startRank &&
                crypto["rank"].GetInt32() <= endRank).ToList();
            return FormatCryptoData(filteredCryptos, startRank, endRank);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取数据失败: {ex.Message}");
            return "获取加密货币数据失败，请稍后再试。";
        }
    }

    private static string FormatCryptoData(List<Dictionary<string, JsonElement>> cryptos, int startRank, int endRank)
    {
        if (cryptos == null || cryptos.Count == 0) return "<b>当前没有可用的加密货币数据。</b>";

        int up1h = 0, down1h = 0, up24h = 0, down24h = 0, up7d = 0, down7d = 0;

        var formattedData = new List<string> { $"<b>币圈市值TOP{startRank}-{endRank} 近1h/24h/7d数据</b>" };
        foreach (var crypto in cryptos)
        {
            // 跳过TRX币种
            if (crypto["symbol"].GetString().Equals("TRX", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var percentChange1h = crypto["percent_change_1h"].GetDecimal();
            var percentChange24h = crypto["percent_change_24h"].GetDecimal();
            var percentChange7d = crypto["percent_change_7d"].GetDecimal();

            // 更新上涨下跌计数
            if (percentChange1h > 0) up1h++; else down1h++;
            if (percentChange24h > 0) up24h++; else down24h++;
            if (percentChange7d > 0) up7d++; else down7d++;

            var upEmoji = "\U0001F4C8";//上涨符号
            var downEmoji = "\U0001F4C9";//下跌符号

            formattedData.Add($"<b>{crypto["rank"].GetInt32()}: {crypto["symbol"].GetString()}</b>  $:{crypto["price_usd"].GetDecimal()} " +
                              $"流通市值$: {crypto["market_cap_usd"].GetDecimal() / 100000000:F2}亿\n " +
                              $"{(percentChange1h > 0 ? upEmoji : downEmoji)}{percentChange1h}% ；" +
                              $"{(percentChange24h > 0 ? upEmoji : downEmoji)}{percentChange24h}%；" +
                              $"{(percentChange7d > 0 ? upEmoji : downEmoji)}{percentChange7d}%");
        }
        // 添加上涨下跌总数
        formattedData.Add($"<b>1小时变动</b>：\U0001F4C8：{up1h}   \U0001F4C9：{down1h}\n<b>24小时变动</b>：\U0001F4C8：{up24h}   \U0001F4C9：{down24h}\n<b>近7天变动</b>：\U0001F4C8：{up7d}   \U0001F4C9：{down7d}");

        return string.Join("\n\n", formattedData);
    }
}	
private static async Task HandleUserJoinOrLeave(ITelegramBotClient botClient, Message message)
{
    try
    {
        if (message.Type == MessageType.ChatMembersAdded)
        {
            foreach (var newUser in message.NewChatMembers)
            {
                if (!newUser.IsBot) // 确保不是机器人加入
                {
                    string displayName = newUser.FirstName + (newUser.LastName != null ? " " + newUser.LastName : "");
                    // 如果没有用户名，添加 "ID:" 前缀
                    string usernameOrId = newUser.Username != null ? "@" + newUser.Username : "ID:" + newUser.Id.ToString();
                    string msg = $"{displayName} {usernameOrId} 欢迎加入群组！";
                    await botClient.SendTextMessageAsync(message.Chat.Id, msg);
                }
            }
        }
        else if (message.Type == MessageType.ChatMemberLeft)
        {
            var leftUser = message.LeftChatMember;
            if (leftUser != null && !leftUser.IsBot) // 确保不是机器人离开
            {
                string displayName = leftUser.FirstName + (leftUser.LastName != null ? " " + leftUser.LastName : "");
                // 如果没有用户名，添加 "ID:" 前缀
                string usernameOrId = leftUser.Username != null ? "@" + leftUser.Username : "ID:" + leftUser.Id.ToString();
                string msg = $"{displayName} {usernameOrId} 离开群组！";
                await botClient.SendTextMessageAsync(message.Chat.Id, msg);
            }
        }
    }
    catch (Telegram.Bot.Exceptions.ApiRequestException ex)
    {
        // 处理没有发送消息权限的情况或其他API请求异常
        Console.WriteLine($"无法发送消息: {ex.Message}");
    }
    catch (Exception ex)
    {
        // 处理其他异常
        Console.WriteLine($"发生异常: {ex.Message}");
    }
}	
//短期30分钟涨跌数据
// 为 /jisuzhangdie 命令创建一个新的字典来跟踪用户查询限制
private static Dictionary<long, (int count, DateTime lastQueryDate)> userJisuZhangdieLimits = new Dictionary<long, (int count, DateTime lastQueryDate)>();	
public class CryptoPriceMonitor
{
    private static readonly int MaxMinutes = 30; // 储存30分钟数据
    private static Queue<Dictionary<string, decimal>> priceHistory = new Queue<Dictionary<string, decimal>>(MaxMinutes);
    private static Timer priceUpdateTimer;
    private static bool isMonitoringStarted = false;

    public static async Task StartMonitoringAsync(ITelegramBotClient botClient, long chatId)
    {
        if (!isMonitoringStarted)
        {
            isMonitoringStarted = true;
            priceUpdateTimer = new Timer(async _ => await UpdatePricesAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            await botClient.SendTextMessageAsync(chatId, "数据初始化中，请30分钟后查询...");
        }
        else
        {
            await CompareAndSendPriceChangeAsync(botClient, chatId);
        }
    }

    private static async Task UpdatePricesAsync()
    {
        var prices = await FetchCurrentPricesAsync();
        if (priceHistory.Count == MaxMinutes)
        {
            priceHistory.Dequeue(); // 移除最旧的一分钟数据
        }
        priceHistory.Enqueue(prices); // 添加最新的一分钟数据
    }
//新版数据直接从本地缓存获取
private static async Task<Dictionary<string, decimal>> FetchCurrentPricesAsync()
{
    // 确保缓存已经初始化并且是最新的
    await CoinDataCache.EnsureCacheInitializedAsync();

    // 获取所有币种的数据
    var allCoinsData = CoinDataCache.GetAllCoinsData();

    // 过滤掉 "TRX" 币种，并将数据转换为所需的格式
    var filteredPrices = allCoinsData
        .Where(kv => !kv.Key.Equals("TRX", StringComparison.OrdinalIgnoreCase)) // 过滤掉 "TRX"
        .Select(kv =>
        {
            decimal price = 0m;
            if (kv.Value.TryGetValue("price_usd", out JsonElement priceElement) && decimal.TryParse(priceElement.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out price))
            {
                return new { Key = kv.Key + "USDT", Price = price }; // 假设您需要将币种符号转换为以 "USDT" 结尾的格式
            }
            return null;
        })
        .Where(kv => kv != null)
        .ToDictionary(kv => kv.Key, kv => kv.Price);

    return filteredPrices;
}
//旧版数据，从api获取  已取消 已注释
/*	
private static async Task<Dictionary<string, decimal>> FetchCurrentPricesAsync()
{
    string spotApiUrl = "https://api.binance.com/api/v3/ticker/price";//币安现货价格
    string futuresApiUrl = "https://fapi.binance.com/fapi/v1/ticker/price"; // 币安合约价格
    int maxRetries = 3;
    int retryDelay = 5000; // 重试间隔，单位毫秒

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            // 尝试从现货API获取数据
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetStringAsync(spotApiUrl);
                var prices = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(response);
                return prices.Where(p => p["symbol"].EndsWith("USDT")).ToDictionary(p => p["symbol"], p => decimal.Parse(p["price"]));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"现货API获取失败，尝试次数 {attempt}。错误: {ex.Message}");
            if (attempt == maxRetries)
            {
                // 现货API尝试次数用尽，切换到合约API
                Console.WriteLine("切换到合约API...");
                for (int futuresAttempt = 1; futuresAttempt <= maxRetries; futuresAttempt++)
                {
                    try
                    {
                        using (var httpClient = new HttpClient())
                        {
                            var response = await httpClient.GetStringAsync(futuresApiUrl);
                            var prices = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(response);
                            return prices.Where(p => p["symbol"].EndsWith("USDT")).ToDictionary(p => p["symbol"], p => decimal.Parse(p["price"]));
                        }
                    }
                    catch (Exception futuresEx)
                    {
                        Console.WriteLine($"合约API获取失败，尝试次数 {futuresAttempt}。错误: {futuresEx.Message}");
                        if (futuresAttempt == maxRetries)
                        {
                            // 合约API尝试次数用尽，暂停任务并清空数据
                            Console.WriteLine("合约API获取失败，任务暂停，清空数据...");
                            priceHistory.Clear();
                            isMonitoringStarted = false; // 停止监控
                            return null; // 或者抛出异常，根据您的需求处理
                        }
                    }
                    await Task.Delay(retryDelay);
                }
            }
        }
        await Task.Delay(retryDelay);
    }

    return null; // 如果所有尝试都失败，返回null或适当处理
}
*/    //旧版数据，从api获取  已取消 已注释
private static async Task<Dictionary<string, decimal>> FetchPricesFromBackupApi(string url, int maxRetries, int retryDelay)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetStringAsync(url);
                var prices = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(response);
                return prices.Where(p => p["symbol"].EndsWith("USDT")).ToDictionary(p => p["symbol"], p => decimal.Parse(p["price"]));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"备用API尝试 {attempt} 失败: {ex.Message}");
            if (attempt == maxRetries)
            {
                Console.WriteLine("备用API获取失败，停止任务并清空数据...");
                StopMonitoringAndClearData();
            }
            await Task.Delay(retryDelay);
        }
    }

    // 如果所有尝试都失败，返回空字典
    return new Dictionary<string, decimal>();
}

private static void StopMonitoringAndClearData()
{
    priceUpdateTimer?.Change(Timeout.Infinite, 0); // 停止定时器
    priceHistory.Clear(); // 清空价格历史
    isMonitoringStarted = false; // 重置监控状态
    Console.WriteLine("监控任务已停止，数据已清空。");
}
private static string FormatPrice(string priceStr)
{
    // 将字符串转换为decimal，确保不丢失精度
    decimal price = decimal.Parse(priceStr, CultureInfo.InvariantCulture);

    // 使用 "G29" 保证转换回来的字符串不会使用科学记数法
    // 并且使用 "0.#############################" 格式化字符串以去除末尾无用的零
    string formattedPrice = price.ToString("0.#############################", CultureInfo.InvariantCulture);

    return formattedPrice;
}
private static async Task CompareAndSendPriceChangeAsync(ITelegramBotClient botClient, long chatId)
{
    var currentPrices = await FetchCurrentPricesAsync();
    if (priceHistory.Count < MaxMinutes)
    {
        int minutesToWait = MaxMinutes - priceHistory.Count;
        await botClient.SendTextMessageAsync(chatId, $"价格数据尚未积累到30分钟，请{minutesToWait}分钟后再试。", parseMode: ParseMode.Html);
        return;
    }

    // 获取30分钟前的价格数据，即队列中的第一条数据  倒叙 新的在最后一条 旧的在第一条
    var fifteenMinutesAgoPrices = priceHistory.Peek();

    // 特别处理比特币和以太坊的价格变化
    var btcChange = CalculatePriceChange("BTCUSDT", currentPrices, fifteenMinutesAgoPrices);
    var ethChange = CalculatePriceChange("ETHUSDT", currentPrices, fifteenMinutesAgoPrices);
	
    // 过滤掉TRXUSDT交易对
    var filteredCurrentPrices = currentPrices.Where(p => p.Key != "TRXUSDT").ToDictionary(p => p.Key, p => p.Value);
    var filteredFifteenMinutesAgoPrices = fifteenMinutesAgoPrices.Where(p => p.Key != "TRXUSDT").ToDictionary(p => p.Key, p => p.Value);

    var priceChanges = filteredCurrentPrices.Select(cp =>
    {
        var symbol = cp.Key.Replace("USDT", "");
        var currentPrice = cp.Value;
        var fifteenMinutesAgoPrice = filteredFifteenMinutesAgoPrices.ContainsKey(cp.Key) ? filteredFifteenMinutesAgoPrices[cp.Key] : 0m;
        var changePercent = fifteenMinutesAgoPrice != 0 ? (currentPrice - fifteenMinutesAgoPrice) / fifteenMinutesAgoPrice * 100 : 0;
        return new { Symbol = symbol, ChangePercent = changePercent, CurrentPrice = currentPrice };
    }).ToList();

// 统计上涨和下跌的总数
int totalGainers = priceChanges.Count(p => p.ChangePercent > 0);
int totalLosers = priceChanges.Count(p => p.ChangePercent < 0);	

    // 根据变化百分比排序，并考虑过滤TRXUSDT后的条目数量
    var topGainers = priceChanges.OrderByDescending(p => p.ChangePercent).Take(5 + (currentPrices.ContainsKey("TRXUSDT") ? 1 : 0));
    var topLosers = priceChanges.OrderBy(p => p.ChangePercent).Take(5 + (currentPrices.ContainsKey("TRXUSDT") ? 1 : 0));

    // 如果TRXUSDT存在，确保只取前5条数据
    var finalTopGainers = topGainers.Take(5);
    var finalTopLosers = topLosers.Take(5);

// 组装消息文本
string message = $"<b>30分钟走势：</b>\n\n比特币{(btcChange.ChangePercent >= 0 ? "\U0001F4C8" : "\U0001F4C9")}: {btcChange.ChangePercent:F2}%, ${FormatPrice(btcChange.CurrentPrice.ToString())}\n以太坊{(ethChange.ChangePercent >= 0 ? "\U0001F4C8" : "\U0001F4C9")}: {ethChange.ChangePercent:F2}%, ${FormatPrice(ethChange.CurrentPrice.ToString())}\n\n<b>急速上涨：</b>\n" 
+ string.Join("\n", finalTopGainers.Select((g, index) => $"{index}️⃣  <code>{g.Symbol}</code> \U0001F4C8：{g.ChangePercent:F2}%，${FormatPrice(g.CurrentPrice.ToString())}").Take(5))
+ "\n\n<b>急速下跌：</b>\n" 
+ string.Join("\n", finalTopLosers.Select((l, index) => $"{index + 5}️⃣  <code>{l.Symbol}</code> \U0001F4C9{l.ChangePercent:F2}%，${FormatPrice(l.CurrentPrice.ToString())}").Take(5))
+ $"\n\n\U0001F4C8上涨总数： <b>{totalGainers}</b>\n\U0001F4C9下跌总数： <b>{totalLosers}</b>";

// 构建按钮
List<InlineKeyboardButton[]> rows = new List<InlineKeyboardButton[]>();

// 添加上涨按钮
InlineKeyboardButton[] riseButtons = new InlineKeyboardButton[5];
for (int i = 0; i < 5; i++)
{
    riseButtons[i] = InlineKeyboardButton.WithCallbackData($"{i}️⃣", $"查{finalTopGainers.ElementAt(i).Symbol.ToLower().Replace("usdt", "")}");
}
rows.Add(riseButtons);

// 添加下跌按钮
InlineKeyboardButton[] fallButtons = new InlineKeyboardButton[5];
for (int i = 0; i < 5; i++)
{
    fallButtons[i] = InlineKeyboardButton.WithCallbackData($"{i + 5}️⃣", $"查{finalTopLosers.ElementAt(i).Symbol.ToLower().Replace("usdt", "")}");
}
rows.Add(fallButtons);

// 添加原有的按钮
rows.Add(new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("市值TOP50 大数据", "feixiaohao") });

// 创建键盘
var inlineKeyboard = new InlineKeyboardMarkup(rows);

// 发送消息
await botClient.SendTextMessageAsync(chatId, message, parseMode: ParseMode.Html, replyMarkup: inlineKeyboard);
}

private static (decimal ChangePercent, decimal CurrentPrice) CalculatePriceChange(string symbol, Dictionary<string, decimal> currentPrices, Dictionary<string, decimal> fifteenMinutesAgoPrices)
{
    decimal currentPrice = currentPrices.ContainsKey(symbol) ? currentPrices[symbol] : 0m;
    decimal fifteenMinutesAgoPrice = fifteenMinutesAgoPrices.ContainsKey(symbol) ? fifteenMinutesAgoPrices[symbol] : 0m;
    decimal changePercent = fifteenMinutesAgoPrice != 0 ? (currentPrice - fifteenMinutesAgoPrice) / fifteenMinutesAgoPrice * 100 : 0;
    return (ChangePercent: changePercent, CurrentPrice: currentPrice);
}
}
//查询指定时间的币种价格到现在的价格涨跌	
public static async Task QueryCryptoPriceTrendAsync(ITelegramBotClient botClient, long chatId, string messageText)
{
    try
    {
        var parts = messageText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var symbol = parts[0].ToUpper();
        var dateTimeStr = parts[1] + " " + parts[2];
        var dateTime = DateTime.ParseExact(dateTimeStr, "yyyy/MM/dd HH.mm", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);

        var utcDateTime = dateTime.ToUniversalTime();
        var unixTimestamp = ((DateTimeOffset)utcDateTime).ToUnixTimeMilliseconds();

        // 计算15分钟和1小时前的时间戳
        var unixTimestamp15MinAgo = unixTimestamp - 900000; // 15分钟前
        var unixTimestamp1HourAgo = unixTimestamp - 3600000; // 1小时前

        string priceType = "现货";
        string priceUrl = $"https://api.binance.com/api/v3/ticker/price?symbol={symbol}USDT";
        string klineUrl = $"https://api.binance.com/api/v3/klines?symbol={symbol}USDT&interval=1m&startTime={unixTimestamp}&endTime={unixTimestamp + 60000}";

        using (var httpClient = new HttpClient())
        {
            var currentPriceResponse = await httpClient.GetStringAsync(priceUrl);
            var currentPriceData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(currentPriceResponse);
            var currentPrice = currentPriceData?["price"].GetString();

            var klineResponse = await httpClient.GetStringAsync(klineUrl);
            var klineData = JsonSerializer.Deserialize<List<List<JsonElement>>>(klineResponse);

            if (klineData == null || klineData.Count == 0)
            {
                priceType = "合约";
                priceUrl = $"https://fapi.binance.com/fapi/v1/ticker/price?symbol={symbol}USDT";
                klineUrl = $"https://fapi.binance.com/fapi/v1/klines?symbol={symbol}USDT&interval=1m&startTime={unixTimestamp}&endTime={unixTimestamp + 60000}";

                currentPriceResponse = await httpClient.GetStringAsync(priceUrl);
                currentPriceData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(currentPriceResponse);
                currentPrice = currentPriceData?["price"].GetString();

                klineResponse = await httpClient.GetStringAsync(klineUrl);
                klineData = JsonSerializer.Deserialize<List<List<JsonElement>>>(klineResponse);

                if (klineData == null || klineData.Count == 0)
                {
                    await botClient.SendTextMessageAsync(chatId, "未找到指定时间的价格数据。");
                    return;
                }
            }

            var openPrice = klineData[0][1].GetString();
		
// 获取当前UTC时间，并转换为北京时间进行调试输出
var beijingTimeNow = DateTime.UtcNow.AddHours(8);
//Console.WriteLine($"当前时间是 {beijingTimeNow.ToString("yyyy-MM-dd HH:mm:ss")}"); // 增加调试输出

// 计算北京时间当日0点对应的UTC时间
// 注意：这里创建的DateTime已经是UTC时间，因为我们从UTC时间减去8小时得到的
var startOfBeijingDayUtc = new DateTime(beijingTimeNow.Year, beijingTimeNow.Month, beijingTimeNow.Day, 0, 0, 0, DateTimeKind.Utc).AddHours(-8);
var unixTimestampStartOfBeijingDay = ((DateTimeOffset)startOfBeijingDayUtc).ToUnixTimeMilliseconds();

// 调试输出，确保我们有正确的北京时间0点对应的UTC时间
//Console.WriteLine($"北京时间0点对应的UTC时间是 {startOfBeijingDayUtc.ToString("yyyy-MM-dd HH:mm:ss")}"); 

// 获取当前时间的Unix时间戳
var unixTimestampNow = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();

// 获取近24小时涨跌幅
var unixTimestamp24HoursAgo = unixTimestampNow - 86400000; // 24小时前
decimal priceChangePercent24Hours = await GetPriceChangePercentAsync(httpClient, symbol, unixTimestamp24HoursAgo, unixTimestampNow, true);

// 获取当日涨跌幅（基于北京时间0点）
decimal priceChangePercentDay = await GetPriceChangePercentAsync(httpClient, symbol, unixTimestampStartOfBeijingDay, unixTimestampNow, false);

	
// 获取15分钟前的价格数据
var startTime15MinAgo = unixTimestamp - 900000; // 15分钟前的开始时间
var endTimeFor15Min = unixTimestamp; // 指定时间的结束时间，确保包含指定时间的整个分钟数据
var klineResponse15Min = await httpClient.GetStringAsync($"https://api.binance.com/api/v3/klines?symbol={symbol}USDT&interval=1m&startTime={startTime15MinAgo}&endTime={endTimeFor15Min}");
var klineData15Min = JsonSerializer.Deserialize<List<List<JsonElement>>>(klineResponse15Min);
var openPrice15Min = klineData15Min?[0][1].GetString();
var closePrice15Min = klineData15Min?[klineData15Min.Count - 1][4].GetString();
var priceChangePercent15Min = (decimal.Parse(closePrice15Min) - decimal.Parse(openPrice15Min)) / decimal.Parse(openPrice15Min) * 100;

// 获取1小时前的价格数据
var startTime1HourAgo = unixTimestamp - 3600000; // 1小时前的开始时间
var endTimeFor1Hour = unixTimestamp; // 指定时间的结束时间，确保包含指定时间的整个分钟数据
var klineResponse1Hour = await httpClient.GetStringAsync($"https://api.binance.com/api/v3/klines?symbol={symbol}USDT&interval=1m&startTime={startTime1HourAgo}&endTime={endTimeFor1Hour}");
var klineData1Hour = JsonSerializer.Deserialize<List<List<JsonElement>>>(klineResponse1Hour);
var openPrice1Hour = klineData1Hour?[0][1].GetString();
var closePrice1Hour = klineData1Hour?[klineData1Hour.Count - 1][4].GetString();
var priceChangePercent1Hour = (decimal.Parse(closePrice1Hour) - decimal.Parse(openPrice1Hour)) / decimal.Parse(openPrice1Hour) * 100;

            var priceChangePercent = (decimal.Parse(currentPrice) - decimal.Parse(openPrice)) / decimal.Parse(openPrice) * 100;
            // 根据涨跌幅正负决定符号📈📉
            var trendSymbol = priceChangePercent >= 0 ? "\U0001F4C8" : "\U0001F4C9";	
            var trendSymbol15Min = priceChangePercent15Min >= 0 ? "\U0001F4C8" : "\U0001F4C9";
            var trendSymbol1Hour = priceChangePercent1Hour >= 0 ? "\U0001F4C8" : "\U0001F4C9";
	    string trendSymbol24Hours = priceChangePercent24Hours >= 0 ? "\U0001F4C8" : "\U0001F4C9";
            string trendSymbolDay = priceChangePercentDay >= 0 ? "\U0001F4C8" : "\U0001F4C9";	

            var reply = $"查询币种： <code>{symbol}</code>  {priceType}\n\n" +
                        $"初始时间：<code>{dateTimeStr}</code>\n" +
                        $"前15分钟：{trendSymbol15Min} {priceChangePercent15Min:F2}%\n" +
                        $"前60分钟：{trendSymbol1Hour} {priceChangePercent1Hour:F2}%\n" +
                        $"初始价格：{openPrice}\n" +
                        $"当前价格：{currentPrice}\n" +
                        $"初始到现在涨跌幅：{trendSymbol} {priceChangePercent:F2}%\n\n" +
                        $"近24小时涨跌幅：{trendSymbol24Hours} {priceChangePercent24Hours:F2}%\n" +
                        $"北京时间当日涨跌幅：{trendSymbolDay} {priceChangePercentDay:F2}%";

// 创建内联键盘按钮
var inlineKeyboard = new InlineKeyboardMarkup(new[]
{
    new[] // 第一行按钮
    {
        InlineKeyboardButton.WithCallbackData("1天前", $"{symbol} {dateTime.AddDays(-1).ToString("yyyy/MM/dd HH.mm")}"),
        InlineKeyboardButton.WithCallbackData("3天前", $"{symbol} {dateTime.AddDays(-3).ToString("yyyy/MM/dd HH.mm")}")
    },
    new[] // 第二行按钮
    {
        InlineKeyboardButton.WithCallbackData("再查一次", $"{symbol} {dateTime.ToString("yyyy/MM/dd HH.mm")}"),
        InlineKeyboardButton.WithCallbackData("详细信息", $"{symbol}")
    },
    new[] // 第三行按钮
    {
        InlineKeyboardButton.WithCallbackData($"订阅 {symbol} 价格变动提醒", $"监控 {symbol}")
    }
});

// 使用内联键盘发送消息
await botClient.SendTextMessageAsync(chatId, reply, ParseMode.Html, replyMarkup: inlineKeyboard);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"查询时发生错误：{ex.Message}");
        await botClient.SendTextMessageAsync(chatId, $"查询时发生错误：{ex.Message}");
    }
}
private static async Task<decimal> GetPriceChangePercentAsync(HttpClient httpClient, string symbol, long startTime, long endTime, bool is24Hours = false)
{
    // 获取当前价格
    string currentPriceUrl = $"https://api.binance.com/api/v3/ticker/price?symbol={symbol}USDT";
    var currentPriceResponse = await httpClient.GetStringAsync(currentPriceUrl);
    var currentPriceData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(currentPriceResponse);
    var currentPriceStr = currentPriceData?["price"].GetString();
    decimal currentPrice = decimal.Parse(currentPriceStr);

    // 尝试使用现货API获取开盘价
    string spotApiUrlFormat = "https://api.binance.com/api/v3/klines?symbol={0}USDT&interval=1m&startTime={1}&endTime={2}";
    string spotApiUrl = string.Format(spotApiUrlFormat, symbol, startTime, endTime);

    // 尝试使用合约API获取开盘价
    string futuresApiUrlFormat = "https://fapi.binance.com/fapi/v1/klines?symbol={0}USDT&interval=1m&startTime={1}&endTime={2}";
    string futuresApiUrl = string.Format(futuresApiUrlFormat, symbol, startTime, endTime);

    // 尝试获取K线数据
    var response = await TryGetKlineDataAsync(httpClient, spotApiUrl);
    if (response == null)
    {
        // 如果现货数据为空，尝试获取合约K线数据
        response = await TryGetKlineDataAsync(httpClient, futuresApiUrl);
    }

    if (response != null)
    {
        var klineData = JsonSerializer.Deserialize<List<List<JsonElement>>>(response);
        if (klineData != null && klineData.Count > 0)
        {
            var openPrice = decimal.Parse(klineData[0][1].GetString());

            // 计算涨跌幅
            var priceChangePercent = (currentPrice - openPrice) / openPrice * 100;

            // 增加调试输出
            if (is24Hours)
            {
                //Console.WriteLine($"24小时前的开盘价是：{openPrice}, 当前价格是：{currentPrice}");
            }
            else
            {
                //Console.WriteLine($"北京时间0点的开盘价是：{openPrice}, 当前价格是：{currentPrice}");
            }

            return priceChangePercent;
        }
    }

    // 如果API调用失败或没有数据，返回0
    return 0;
}

// 封装一个尝试获取K线数据的方法
private static async Task<string> TryGetKlineDataAsync(HttpClient httpClient, string apiUrl)
{
    try
    {
        var response = await httpClient.GetStringAsync(apiUrl);
        if (!string.IsNullOrEmpty(response))
        {
            return response;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"获取数据失败: {ex.Message}");
    }
    return null;
}
//现货合约价格差以及字典
private static Dictionary<long, (int count, DateTime lastQueryDate)> userQueryLimits = new Dictionary<long, (int count, DateTime lastQueryDate)>();
public static class CryptoPriceChecker
{
    private static readonly HttpClient client = new HttpClient();
    private static readonly string spotPriceUrl = "https://api.binance.com/api/v3/ticker/price";
    private static readonly string futuresPriceUrl = "https://fapi.binance.com/fapi/v1/ticker/price";
    private static readonly string fundingRateUrl = "https://fapi.binance.com/fapi/v1/premiumIndex?symbol=";

public static async Task<string> CheckPriceDifferencesAsync()
{
    try
    {
        var spotPrices = await GetPricesAsync(spotPriceUrl);
        var futuresPrices = await GetPricesAsync(futuresPriceUrl);

        var message = "<b>信号广场：</b>\n\n";
        bool foundDifference = false;
        int count = 0; // 用于计数已添加的币种数量

        foreach (var spotPrice in spotPrices)
        {
            if (count >= 20) // 如果已添加20个币种，则停止添加新的币种信息
            {
                break;
            }

            if (spotPrice.Symbol.EndsWith("USDT"))
            {
                var futuresPrice = futuresPrices.Find(p => p.Symbol == spotPrice.Symbol);
                if (futuresPrice != null)
                {
                    var spotPriceDecimal = decimal.Parse(spotPrice.Price, CultureInfo.InvariantCulture);
                    var futuresPriceDecimal = decimal.Parse(futuresPrice.Price, CultureInfo.InvariantCulture);

                    if (spotPriceDecimal == 0)
                    {
                        continue;
                    }

                    var difference = Math.Abs(spotPriceDecimal - futuresPriceDecimal) / spotPriceDecimal * 100;
                    var differenceFormatted = Math.Round(difference, 2);

                    if (difference > 0.5m) // 使用0.5%作为差异阈值
                    {
                        foundDifference = true;
                        var fundingRate = await GetFundingRateAsync(spotPrice.Symbol);
                        var baseCurrency = spotPrice.Symbol.Substring(0, spotPrice.Symbol.Length - 4);
			var symbolFormatted = $"<code>{baseCurrency}</code> / USDT"; // 只对币种名称使用<code>标签    
                        message += $"{symbolFormatted}\n现货价格：{TrimTrailingZeros(spotPriceDecimal.ToString(CultureInfo.InvariantCulture))}\n合约价格：{TrimTrailingZeros(futuresPriceDecimal.ToString(CultureInfo.InvariantCulture))}\n价格差异：{differenceFormatted}%\n合约资金费率：{fundingRate}\n\n";
                        count++; // 增加已添加的币种数量
                    }
                }
            }
        }

        if (!foundDifference)
        {
            message += "没有发现显著的价格差异。";
        }

        return message;
    }
    catch (Exception ex)
    {
        return $"在检查价格差异时发生错误：{ex.Message}";
    }
}

    private static async Task<List<PriceInfo>> GetPricesAsync(string url)
    {
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<PriceInfo>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

private static async Task<string> GetFundingRateAsync(string symbol)
{
    try
    {
        string requestUrl = fundingRateUrl + symbol;
        var response = await client.GetAsync(requestUrl);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"无法获取{symbol}的合约资金费率。HTTP状态码：{response.StatusCode}");
            return "N/A";
        }

        var content = await response.Content.ReadAsStringAsync();
        var fundingInfo = JsonSerializer.Deserialize<FundingInfo>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (fundingInfo == null || string.IsNullOrEmpty(fundingInfo.LastFundingRate))
        {
            //Console.WriteLine($"未找到{symbol}的合约资金费率信息。");
            return "错误";
        }

        // 尝试将LastFundingRate从string转换为decimal
        if (!decimal.TryParse(fundingInfo.LastFundingRate, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal fundingRateDecimal))
        {
            //Console.WriteLine($"无法将{symbol}的合约资金费率转换为数字。");
            return "错误";
        }

        var fundingRatePercent = Math.Round(fundingRateDecimal * 100, 2);
        return $"{fundingRatePercent:F2}%";
    }
    catch (Exception ex)
    {
        //Console.WriteLine($"获取{symbol}的合约资金费率时发生错误：{ex.Message}");
        return "错误";
    }
}

    private static string TrimTrailingZeros(string number)
    {
        return number.TrimEnd('0').TrimEnd('.');
    }

    class PriceInfo
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; }

        [JsonPropertyName("price")]
        public string Price { get; set; }
    }

    class FundingInfo
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; }

        [JsonPropertyName("lastFundingRate")]
        public string LastFundingRate { get; set; } // 修改为string类型
    }
}

public static class IndexDataFetcher//指数行情
{
    private static readonly HttpClient client = new HttpClient();
    private static readonly List<string> licences = new List<string> { "a8d568553657cff90", "", "" };//可以一直添加秘钥 http://mairui.club/ 申请
    private static readonly string[] indexCodes = { "sh000001", "sz399001", "sh000300" };
    private static readonly string[] indexNames = { "上证指数", "深证指数", "沪深  300" };

public static async Task<string> FetchIndexDataAsync()
{
    string resultText = "";
    foreach (var (indexCode, indexName) in indexCodes.Zip(indexNames))
    {
        var licence = licences.OrderBy(x => Guid.NewGuid()).First();
        string url = $"http://api.mairui.club/zs/sssj/{indexCode}/{licence}";
        HttpResponseMessage response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            foreach (var altLicence in licences.Where(x => x != licence))
            {
                url = $"http://api.mairui.club/zs/sssj/{indexCode}/{altLicence}";
                response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode) break;
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            resultText += $"{indexName}: 数据获取失败\n\n";
            continue;
        }

        var jsonString = await response.Content.ReadAsStringAsync();
        try
        {
            using var jsonDoc = JsonDocument.Parse(jsonString);
            var root = jsonDoc.RootElement;
            var currentPriceStr = root.GetProperty("p").GetString();
            var yesterdayCloseStr = root.GetProperty("yc").GetString();
            var turnoverStr = root.GetProperty("cje").GetString();

            if (decimal.TryParse(currentPriceStr, out decimal currentPrice) && decimal.TryParse(yesterdayCloseStr, out decimal yesterdayClose) && decimal.TryParse(turnoverStr, out decimal turnover))
            {
                var changePercent = ((currentPrice - yesterdayClose) / yesterdayClose) * 100;
                string changeSymbol = changePercent >= 0 ? "↑" : "↓";
                var turnoverInBillion = Math.Round(turnover / 100_000_000, 2);
                resultText += $"{indexName}：<b>{currentPrice:F2}</b>   {changeSymbol} <b>{Math.Abs(changePercent):F2}%</b>  \n成交额：<b>{turnover:N2}</b> （约<b>{turnoverInBillion}</b>亿）\n\n";
            }
            else
            {
                resultText += $"{indexName}: 数值解析失败\n\n";
            }
        }
        catch (Exception ex)
        {
            resultText += $"{indexName}: 数据获取异常 - {ex.Message}\n\n";
        }
    }

    return resultText.TrimEnd('-').Trim();
}

public static async Task<string> FetchMarketOverviewAsync()
{
    string resultText = "沪深两市上涨下跌数概览\n\n";
    var licence = licences.OrderBy(x => Guid.NewGuid()).First();
    string[] urls = {
        $"http://api.mairui.club/zs/lsgl/{licence}",
        $"http://api1.mairui.club/zs/lsgl/{licence}"
    };

    HttpResponseMessage response = null;
    foreach (var url in urls)
    {
        response = await client.GetAsync(url);
        if (response.IsSuccessStatusCode) break;
    }

    if (response == null || !response.IsSuccessStatusCode)
    {
        return resultText + "数据获取失败";
    }

    var jsonString = await response.Content.ReadAsStringAsync();
    try
    {
        using var jsonDoc = JsonDocument.Parse(jsonString);
        var root = jsonDoc.RootElement;
        resultText += $"涨停总数：<b>{root.GetProperty("zt")}</b>     跌停总数：<b>{root.GetProperty("dt")}</b>\n";
        resultText += $"上涨总数：<b>{root.GetProperty("totalUp")}</b>   下跌总数：<b>{root.GetProperty("totalDown")}</b>\n";
    }
    catch (Exception ex)
    {
        resultText += $"数据解析异常 - {ex.Message}";
    }

    return resultText;
}
}
//香港六合彩特码统计
private static Dictionary<long, (int count, DateTime lastQueryDate)> userQueries = new Dictionary<long, (int count, DateTime lastQueryDate)>();	
public static class LotteryStatisticsHelper
{
    public static async Task<string> FetchSpecialNumberStatisticsAsync(HttpClient client)
    {
        try
        {
            var response = await client.GetAsync("https://kclm.site/api/trial/drawResult?code=hk6&format=json&rows=50");
            if (!response.IsSuccessStatusCode)
            {
                return "获取历史开奖信息失败，请稍后再试。";
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonObject = JObject.Parse(jsonString);
            var historyData = jsonObject["data"];

            int bigCount = 0, smallCount = 0, oddCount = 0, evenCount = 0;
            var numberFrequency = new Dictionary<int, int>();
            var zodiacFrequency = new Dictionary<string, int>();
            var colorFrequency = new Dictionary<string, int>();

            foreach (var item in historyData)
            {
                var drawResults = item["drawResult"].ToString().Split(',');
                var specialNumber = int.Parse(drawResults.Last());
                var drawTime = DateTime.Parse(item["drawTime"].ToString());

                // 大小统计
                if (specialNumber >= 26) bigCount++;
                else smallCount++;

                // 单双统计
                if (specialNumber % 2 == 0) evenCount++;
                else oddCount++;

                // 号码频率统计
                if (!numberFrequency.ContainsKey(specialNumber)) numberFrequency[specialNumber] = 0;
                numberFrequency[specialNumber]++;

                // 生肖频率统计
                var zodiacDictionary = LotteryFetcherr.GetZodiacDictionary(drawTime);
                var specialNumberZodiac = zodiacDictionary[specialNumber];
                if (!zodiacFrequency.ContainsKey(specialNumberZodiac)) zodiacFrequency[specialNumberZodiac] = 0;
                zodiacFrequency[specialNumberZodiac]++;

                // 波色频率统计
                var specialNumberColor = LotteryFetcherr.numberToColor[specialNumber];
                if (!colorFrequency.ContainsKey(specialNumberColor)) colorFrequency[specialNumberColor] = 0;
                colorFrequency[specialNumberColor]++;
            }

// 数据分析 - 修改为前三名
var topThreeFrequentNumbers = numberFrequency.OrderByDescending(kvp => kvp.Value).Take(3).ToList();
var topThreeLeastFrequentNumbers = numberFrequency.OrderBy(kvp => kvp.Value).Take(3).ToList();
var topThreeFrequentZodiacs = zodiacFrequency.OrderByDescending(kvp => kvp.Value).Take(3).ToList();
var topThreeLeastFrequentZodiacs = zodiacFrequency.OrderBy(kvp => kvp.Value).Take(3).ToList();
var topThreeFrequentColors = colorFrequency.OrderByDescending(kvp => kvp.Value).Take(3).ToList();
var topThreeLeastFrequentColors = colorFrequency.OrderBy(kvp => kvp.Value).Take(3).ToList();

// 构建结果字符串，包括前三名和次数，格式调整为使用“—”分隔号码/生肖和次数
var result = $"香港六合彩近50期特码：\n\n" +
             $"大：{bigCount} 期\n" +
             $"小：{smallCount} 期\n" +
             $"单：{oddCount} 期\n" +
             $"双：{evenCount} 期\n\n" +
             $"最常出现的号码是：\n" + string.Join(" | ", topThreeFrequentNumbers.Select(kvp => $"{kvp.Key}—{kvp.Value} 期")) + "\n\n" +
             $"最少出现的号码是：\n" + string.Join(" | ", topThreeLeastFrequentNumbers.Select(kvp => $"{kvp.Key}—{kvp.Value} 期")) + "\n\n" +
             $"最常出现的生肖是：\n" + string.Join(" | ", topThreeFrequentZodiacs.Select(kvp => $"{kvp.Key}—{kvp.Value} 期")) + "\n\n" +
             $"最少出现的生肖是：\n" + string.Join(" | ", topThreeLeastFrequentZodiacs.Select(kvp => $"{kvp.Key}—{kvp.Value} 期")) + "\n\n" +
             $"波色统计：\n" + string.Join(" | ", topThreeFrequentColors.Select(kvp => $"{kvp.Key}  {kvp.Value} 期"));

return result;
        }
        catch (Exception ex)
        {
            return $"获取历史开奖信息时发生错误：{ex.Message}";
        }
    }
}	
public static async Task<string> FetchLotteryHistoryByZodiacAsync(HttpClient client)// 香港六合彩
{
    try
    {
        var response = await client.GetAsync("https://kclm.site/api/trial/drawResult?code=hk6&format=json&rows=50");
        if (!response.IsSuccessStatusCode)
        {
            return "获取历史开奖信息失败，请稍后再试。";
        }

        var jsonString = await response.Content.ReadAsStringAsync();
        var jsonObject = JObject.Parse(jsonString);
        var historyData = jsonObject["data"];

        var formattedHistory = new StringBuilder();
        foreach (var item in historyData)
        {
            var issue = item["issue"].ToString();
            var drawResults = item["drawResult"].ToString().Split(',');
            var drawTime = DateTime.Parse(item["drawTime"].ToString());
            var zodiacDictionary = LotteryFetcherr.GetZodiacDictionary(drawTime);
            var zodiacResults = drawResults.Select(number => zodiacDictionary[int.Parse(number)]).ToArray();
            var formattedZodiacResults = string.Join(", ", zodiacResults);
            formattedHistory.AppendLine($"{issue}   {formattedZodiacResults}");
        }

        return formattedHistory.ToString();
    }
    catch (Exception ex)
    {
        return $"获取历史开奖信息时发生错误：{ex.Message}";
    }
}	
public static async Task<string> FetchLotteryHistoryByColorAsync(HttpClient client)
{
    try
    {
        var response = await client.GetAsync("https://kclm.site/api/trial/drawResult?code=hk6&format=json&rows=50");
        if (!response.IsSuccessStatusCode)
        {
            return "获取历史开奖信息失败，请稍后再试。";
        }

        var jsonString = await response.Content.ReadAsStringAsync();
        var jsonObject = JObject.Parse(jsonString);
        var historyData = jsonObject["data"];

        var formattedHistory = new StringBuilder();
        foreach (var item in historyData)
        {
            var issue = item["issue"].ToString();
            var drawResults = item["drawResult"].ToString().Split(',');
            var colorResults = drawResults.Select(number => LotteryFetcherr.numberToColor[int.Parse(number)]).ToArray();
            var formattedColorResults = string.Join("  ", colorResults);
            formattedHistory.AppendLine($"{issue}   {formattedColorResults}");
        }

        return formattedHistory.ToString();
    }
    catch (Exception ex)
    {
        return $"获取历史开奖信息时发生错误：{ex.Message}";
    }
}	
public static class HttpClientHelper
{
    public static readonly HttpClient Client = new HttpClient();
}	
public static async Task<string> FetchLotteryHistoryAsyncc(HttpClient client)
{
    try
    {
        var response = await client.GetAsync("https://kclm.site/api/trial/drawResult?code=hk6&format=json&rows=50");
        if (!response.IsSuccessStatusCode)
        {
            return "获取历史开奖信息失败，请稍后再试。";
        }

        var jsonString = await response.Content.ReadAsStringAsync();
        var jsonObject = JObject.Parse(jsonString);
        var historyData = jsonObject["data"];

        var formattedHistory = new StringBuilder();
        foreach (var item in historyData)
        {
            var issue = item["issue"].ToString();
            var drawResult = item["drawResult"].ToString();
            formattedHistory.AppendLine($"{issue}   {drawResult}");
        }

        return formattedHistory.ToString();
    }
    catch (Exception ex)
    {
        return $"获取历史开奖信息时发生错误：{ex.Message}";
    }
}	
public static class LotteryFetcherr 
{
    public static readonly HttpClient client = new HttpClient();
    public static readonly Dictionary<int, string> numberToColor = new Dictionary<int, string>
    {
        {1, "\uD83D\uDD34"}, {2, "\uD83D\uDD34"}, {7, "\uD83D\uDD34"}, {8, "\uD83D\uDD34"}, {12, "\uD83D\uDD34"}, {13, "\uD83D\uDD34"}, {18, "\uD83D\uDD34"}, {19, "\uD83D\uDD34"}, {23, "\uD83D\uDD34"}, {24, "\uD83D\uDD34"}, {29, "\uD83D\uDD34"}, {30, "\uD83D\uDD34"}, {34, "\uD83D\uDD34"}, {35, "\uD83D\uDD34"}, {40, "\uD83D\uDD34"}, {45, "\uD83D\uDD34"}, {46, "\uD83D\uDD34"},
        {3, "\uD83D\uDD35"}, {4, "\uD83D\uDD35"}, {9, "\uD83D\uDD35"}, {10, "\uD83D\uDD35"}, {14, "\uD83D\uDD35"}, {15, "\uD83D\uDD35"}, {20, "\uD83D\uDD35"}, {25, "\uD83D\uDD35"}, {26, "\uD83D\uDD35"}, {31, "\uD83D\uDD35"}, {36, "\uD83D\uDD35"}, {37, "\uD83D\uDD35"}, {41, "\uD83D\uDD35"}, {42, "\uD83D\uDD35"}, {47, "\uD83D\uDD35"}, {48, "\uD83D\uDD35"},
        {5, "\uD83D\uDFE2"}, {6, "\uD83D\uDFE2"}, {11, "\uD83D\uDFE2"}, {16, "\uD83D\uDFE2"}, {17, "\uD83D\uDFE2"}, {21, "\uD83D\uDFE2"}, {22, "\uD83D\uDFE2"}, {27, "\uD83D\uDFE2"}, {28, "\uD83D\uDFE2"}, {32, "\uD83D\uDFE2"}, {33, "\uD83D\uDFE2"}, {38, "\uD83D\uDFE2"}, {39, "\uD83D\uDFE2"}, {43, "\uD83D\uDFE2"}, {44, "\uD83D\uDFE2"}, {49, "\uD83D\uDFE2"}
    };
    private static readonly ChineseLunisolarCalendar chineseCalendar = new ChineseLunisolarCalendar();

public static Dictionary<int, string> GetZodiacDictionary(DateTime drawDate)
{
    int chineseYear = chineseCalendar.GetYear(drawDate);
    Dictionary<int, DateTime> springFestivalDates = new Dictionary<int, DateTime>
    {
        {2023, new DateTime(2023, 1, 22)}, // 2023年春节的日期
        {2024, new DateTime(2024, 2, 10)}, // 2024年春节的日期
        {2025, new DateTime(2025, 1, 28)}, // 2025年春节的日期	  
        {2026, new DateTime(2026, 2, 16)}, // 2026年春节的日期
        {2027, new DateTime(2027, 2, 05)}, // 2027年春节的日期
        {2028, new DateTime(2028, 1, 25)}, // 2028年春节的日期	 
        {2029, new DateTime(2029, 2, 12)}, // 2029年春节的日期
        {2030, new DateTime(2030, 2, 02)}, // 2030年春节的日期
        // 根据需要添加更多年份
    };

    if (drawDate < springFestivalDates[chineseYear])
    {
        chineseYear--;
    }

    //Console.WriteLine($"开奖日期：{drawDate:yyyy-MM-dd HH:mm:ss}，农历年份：{chineseYear}");

    var baseYear = 2023;
    // 由于我们需要向后移动，所以我们改变shift的计算方式
    var shift = (12 - (chineseYear - baseYear) % 12) % 12;

    var zodiacsBase = new Dictionary<int, string>
    {
        // 2023年的生肖对照表
        {1, "兔"}, {13, "兔"}, {25, "兔"}, {37, "兔"}, {49, "兔"},
        {2, "虎"}, {14, "虎"}, {26, "虎"}, {38, "虎"},
        {3, "牛"}, {15, "牛"}, {27, "牛"}, {39, "牛"},
        {4, "鼠"}, {16, "鼠"}, {28, "鼠"}, {40, "鼠"},
        {5, "猪"}, {17, "猪"}, {29, "猪"}, {41, "猪"},
        {6, "狗"}, {18, "狗"}, {30, "狗"}, {42, "狗"},
        {7, "鸡"}, {19, "鸡"}, {31, "鸡"}, {43, "鸡"},
        {8, "猴"}, {20, "猴"}, {32, "猴"}, {44, "猴"},
        {9, "羊"}, {21, "羊"}, {33, "羊"}, {45, "羊"},
        {10, "马"}, {22, "马"}, {34, "马"}, {46, "马"},
        {11, "蛇"}, {23, "蛇"}, {35, "蛇"}, {47, "蛇"},
        {12, "龙"}, {24, "龙"}, {36, "龙"}, {48, "龙"},
    };

    var zodiacOrder = new List<string> { "兔", "虎", "牛", "鼠", "猪", "狗", "鸡", "猴", "羊", "马", "蛇", "龙" };

    var zodiacDictionary = new Dictionary<int, string>();
    foreach (var pair in zodiacsBase)
    {
        var baseZodiacIndex = zodiacOrder.IndexOf(zodiacsBase[pair.Key]);
        var newZodiacIndex = (baseZodiacIndex + shift) % 12;
        var newZodiac = zodiacOrder[newZodiacIndex];
        zodiacDictionary[pair.Key] = newZodiac;
    }

    // 输出日志以验证
    foreach (var item in zodiacDictionary)
    {
        //Console.WriteLine($"号码：{item.Key}, 生肖：{item.Value}");
    }

    return zodiacDictionary;
}
public static async Task<string> FetchHongKongLotteryResultAsync()
{
    try
    {
        var response = await client.GetAsync("https://kclm.site/api/trial/drawResult?code=hk6&format=json&rows=50");
        if (!response.IsSuccessStatusCode)
        {
            return "获取香港六合彩开奖信息失败，请稍后再试。";
        }

            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonObject = JObject.Parse(jsonString);
            var latestResult = jsonObject["data"][0];

            var issue = latestResult["issue"].ToString();
            var drawResult = latestResult["drawResult"].ToString().Split(',');
            var drawTime = DateTime.Parse(latestResult["drawTime"].ToString());

            var formattedDrawResult = string.Join("  ", drawResult.Take(drawResult.Length - 1)) + "， " + drawResult.Last();

            var zodiacDictionary = GetZodiacDictionary(drawTime);
            var zodiacs = drawResult.Select(number => zodiacDictionary[int.Parse(number)]).ToArray();
            var formattedZodiacs = string.Join("  ", zodiacs);

        // 添加波色的格式化逻辑
        var colors = drawResult.Select(number => numberToColor[int.Parse(number)]).ToArray();
        var formattedColors = string.Join("  ", colors);

        var result = $"香港六合彩\n\n" +
                     $"期数：{issue}\n" +
                     $"开奖日期：{drawTime:yyyy-MM-dd HH:mm:ss}\n" +
                     $"开奖号码：{formattedDrawResult}\n" +
                     $"生肖：{formattedZodiacs}\n" +
                     $"波色：{formattedColors}";

        return result;
    }
    catch (Exception ex)
    {
        return $"获取香港六合彩开奖信息时发生错误：{ex.Message}";
    }
}
}
// 新澳门六合彩特码统计
private static Dictionary<long, (int count, DateTime lastQueryDate)> newMacauUserQueries = new Dictionary<long, (int count, DateTime lastQueryDate)>();	
public static class NewMacauLotteryStatisticsHelper
{
    public static async Task<string> FetchNewMacauSpecialNumberStatisticsAsync(HttpClient client)
    {
        try
        {
            var historyResults = await NewLotteryFetcher.FetchLotteryHistoryAsync(DateTime.Now.Year, 50);
            var zodiacResults = await NewLotteryFetcher.FetchLotteryZodiacHistoryAsync(DateTime.Now.Year, 50);
            var waveResults = await NewLotteryFetcher.FetchLotteryWaveHistoryAsync(DateTime.Now.Year, 50);

            if (historyResults.Count == 0 || historyResults[0].StartsWith("获取历史开奖信息时发生错误"))
            {
                return "获取历史开奖信息失败，请稍后再试。";
            }

            int bigCount = 0, smallCount = 0, oddCount = 0, evenCount = 0;
            var numberFrequency = new Dictionary<int, int>();
            var zodiacFrequency = new Dictionary<string, int>();
            var colorFrequency = new Dictionary<string, int>();

            // 号码、生肖、波色统计
            for (int i = 0; i < historyResults.Count; i++)
            {
                var parts = historyResults[i].Split(new[] { "   " }, StringSplitOptions.None);
                var openCode = parts[1].Split(", ").Select(int.Parse).ToArray();
                var specialNumber = openCode.Last();

                // 大小、单双统计
                if (specialNumber >= 26) bigCount++;
                else smallCount++;
                if (specialNumber % 2 == 0) evenCount++;
                else oddCount++;

                // 号码频率统计
                if (!numberFrequency.ContainsKey(specialNumber)) numberFrequency[specialNumber] = 0;
                numberFrequency[specialNumber]++;

                // 生肖频率统计 - 只统计特码对应的最后一个生肖
                var zodiacs = zodiacResults[i].Split(new[] { "   " }, StringSplitOptions.None)[1].Split(',');
                var lastZodiac = zodiacs.Last();
                if (!zodiacFrequency.ContainsKey(lastZodiac)) zodiacFrequency[lastZodiac] = 0;
                zodiacFrequency[lastZodiac]++;

                // 波色频率统计 - 只统计特码对应的最后一个波色
                var waves = waveResults[i].Split(new[] { "   " }, StringSplitOptions.None)[1].Split(' ');
                var lastWave = waves.Last();
                if (!colorFrequency.ContainsKey(lastWave)) colorFrequency[lastWave] = 0;
                colorFrequency[lastWave]++;
            }

            // 数据分析 - 修改为前三名
            var topThreeFrequentNumbers = numberFrequency.OrderByDescending(kvp => kvp.Value).Take(3).ToList();
            var topThreeLeastFrequentNumbers = numberFrequency.OrderBy(kvp => kvp.Value).Take(3).ToList();
            var topThreeFrequentZodiacs = zodiacFrequency.OrderByDescending(kvp => kvp.Value).Take(3).ToList();
            var topThreeLeastFrequentZodiacs = zodiacFrequency.OrderBy(kvp => kvp.Value).Take(3).ToList();
            var topThreeFrequentColors = colorFrequency.OrderByDescending(kvp => kvp.Value).Take(3).ToList();
            var topThreeLeastFrequentColors = colorFrequency.OrderBy(kvp => kvp.Value).Take(3).ToList();

            // 构建结果字符串
            var result = $"新澳门六合彩近50期特码：\n\n" +
                         $"大：{bigCount} 期\n" +
                         $"小：{smallCount} 期\n" +
                         $"单：{oddCount} 期\n" +
                         $"双：{evenCount} 期\n\n" +
                    $"最常出现的号码是：\n" + string.Join(" | ", topThreeFrequentNumbers.Select(kvp => $"{kvp.Key}—{kvp.Value} 期")) + "\n\n" +
                    $"最少出现的号码是：\n" + string.Join(" | ", topThreeLeastFrequentNumbers.Select(kvp => $"{kvp.Key}—{kvp.Value} 期")) + "\n\n" +
                    $"最常出现的生肖是：\n" + string.Join(" | ", topThreeFrequentZodiacs.Select(kvp => $"{kvp.Key}—{kvp.Value} 期")) + "\n\n" +
                    $"最少出现的生肖是：\n" + string.Join(" | ", topThreeLeastFrequentZodiacs.Select(kvp => $"{kvp.Key}—{kvp.Value} 期")) + "\n\n" +
                    $"波色统计：\n" + string.Join(" | ", topThreeFrequentColors.Select(kvp => $"{kvp.Key} {kvp.Value} 期"));

            return result;
        }
        catch (Exception ex)
        {
            return $"获取历史开奖信息时发生错误：{ex.Message}";
        }
    }
}
//新澳门六合彩
private static Dictionary<long, (int count, DateTime lastQueryDate)> oldMacauUserQueries = new Dictionary<long, (int count, DateTime lastQueryDate)>();
public static class NewLotteryFetcher
{
    public static readonly HttpClient client = new HttpClient();

    public static async Task<string> FetchLotteryResultAsync()
    {
        try
        {
            var response = await client.GetAsync("https://api.macaumarksix.com/api/macaujc2.com");
            if (!response.IsSuccessStatusCode)
            {
                return "获取开奖信息失败，请稍后再试。";
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonArray = JArray.Parse(jsonString);
            var latestResult = jsonArray[0];

            var expect = latestResult["expect"].ToString();
            var openCode = latestResult["openCode"].ToString().Split(',');
            var zodiac = latestResult["zodiac"].ToString().Split(',');
            var wave = latestResult["wave"].ToString().Split(',').Select(w => w.Replace("red", "\uD83D\uDD34").Replace("green", "\uD83D\uDFE2").Replace("blue", "\uD83D\uDD35")).ToArray();
            var openTime = DateTime.Parse(latestResult["openTime"].ToString());
            var now = DateTime.Now;
            var nextOpenTime = openTime.AddDays(1);
            var timeSpan = nextOpenTime - now;

            var formattedOpenCode = string.Join("  ", openCode.Take(openCode.Length - 1)) + "， " + openCode.Last();
            var formattedZodiac = string.Join("  ", zodiac.Take(zodiac.Length - 1)) + "，  " + zodiac.Last();
            var formattedWave = string.Join("  ", wave);

            var result = $"新澳门六合彩\n\n" +
                         $"距离下期：{timeSpan.Hours} 时 {timeSpan.Minutes} 分 {timeSpan.Seconds} 秒\n" +
                         $"期数：{expect}\n" +
                         $"开奖日期：{openTime:yyyy-MM-dd HH:mm:ss}\n" +
                         $"号码：{formattedOpenCode}\n" +
                         $"生肖：{formattedZodiac}\n" +
                         $"波色：{formattedWave}";

            return result;
        }
        catch (Exception ex)
        {
            return $"获取开奖信息时发生错误：{ex.Message}";
        }
    }

    public static async Task<List<string>> FetchLotteryHistoryAsync(int year, int count = 50)
    {
        var historyResults = new List<string>();
        try
        {
            while (historyResults.Count < count)
            {
                string url = $"https://api.macaumarksix.com/history/macaujc2/y/{year}";
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var jsonObject = JObject.Parse(jsonString);
                    var results = jsonObject["data"].ToObject<List<JObject>>();

                    foreach (var result in results)
                    {
                        var expect = result["expect"].ToString();
                        var openCode = result["openCode"].ToString();
                        historyResults.Add($"{expect}   {openCode.Replace(",", ", ")}");
                        if (historyResults.Count == count) break;
                    }
                }
                year--; // 如果当前年份数据不足，尝试获取前一年的数据
            }
        }
        catch (Exception ex)
        {
            return new List<string> { $"获取历史开奖信息时发生错误：{ex.Message}" };
        }
        return historyResults.Take(count).ToList();
    }
public static async Task<List<string>> FetchLotteryWaveHistoryAsync(int year, int count = 50)
{
    var waveResults = new List<string>();
    try
    {
        while (waveResults.Count < count)
        {
            string url = $"https://api.macaumarksix.com/history/macaujc2/y/{year}";
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var jsonObject = JObject.Parse(jsonString);
                    var results = jsonObject["data"].ToObject<List<JObject>>();

                    foreach (var result in results)
                    {
                        var expect = result["expect"].ToString();
                        var wave = result["wave"].ToString().Split(',').Select(w => w.Replace("red", "\uD83D\uDD34").Replace("green", "\uD83D\uDFE2").Replace("blue", "\uD83D\uDD35")).ToArray();
                        var formattedWave = string.Join("  ", wave);
                        waveResults.Add($"{expect}   {formattedWave}");
                        if (waveResults.Count == count) break;
                    }
                }
            }
            year--; // 如果当前年份数据不足，尝试获取前一年的数据
        }
    }
    catch (Exception ex)
    {
        // 返回一个包含错误信息的列表，以便调用者可以处理这个错误
        return new List<string> { $"获取历史开奖波色信息时发生错误：{ex.Message}" };
    }
    return waveResults.Take(count).ToList(); // 确保不超过50条数据
}

public static async Task<List<string>> FetchLotteryZodiacHistoryAsync(int year, int count = 50)
{
    var zodiacResults = new List<string>();
    try
    {
        while (zodiacResults.Count < count)
        {
            string url = $"https://api.macaumarksix.com/history/macaujc2/y/{year}";
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var jsonObject = JObject.Parse(jsonString);
                    var results = jsonObject["data"].ToObject<List<JObject>>();

                    foreach (var result in results)
                    {
                        var expect = result["expect"].ToString();
                        var zodiac = result["zodiac"].ToString();
                        zodiacResults.Add($"{expect}   {zodiac}");
                        if (zodiacResults.Count == count) break;
                    }
                }
            }
            year--; // 如果当前年份数据不足，尝试获取前一年的数据
        }
    }
    catch (Exception ex)
    {
        // 返回一个包含错误信息的列表，以便调用者可以处理这个错误
        return new List<string> { $"获取历史开奖生肖信息时发生错误：{ex.Message}" };
    }
    return zodiacResults.Take(count).ToList(); // 确保不超过50条数据
}	
}
// 老澳门六合彩特码统计
public static class OldMacauLotteryStatisticsHelper
{
    public static async Task<string> FetchOldMacauSpecialNumberStatisticsAsync()
    {
        try
        {
            var historyResults = await LotteryFetcher.FetchLotteryHistoryAsync(DateTime.Now.Year, 50);
            var zodiacResults = await LotteryFetcher.FetchLotteryZodiacHistoryAsync(DateTime.Now.Year, 50);
            var waveResults = await LotteryFetcher.FetchLotteryWaveHistoryAsync(DateTime.Now.Year, 50);

            if (historyResults.Count == 0 || historyResults[0].StartsWith("获取历史开奖信息时发生错误"))
            {
                return "获取历史开奖信息失败，请稍后再试。";
            }

            int bigCount = 0, smallCount = 0, oddCount = 0, evenCount = 0;
            var numberFrequency = new Dictionary<int, int>();
            var zodiacFrequency = new Dictionary<string, int>();
            var colorFrequency = new Dictionary<string, int>();

foreach (var historyResult in historyResults)
{
    var parts = historyResult.Split(new[] { "   " }, StringSplitOptions.None);
    var openCode = parts[1].Split(", ").Select(int.Parse).ToArray();
    var specialNumber = openCode.Last();

    // 大小、单双统计
    if (specialNumber >= 26) bigCount++;
    else smallCount++;
    if (specialNumber % 2 == 0) evenCount++;
    else oddCount++;

    // 号码频率统计
    if (!numberFrequency.ContainsKey(specialNumber)) numberFrequency[specialNumber] = 0;
    numberFrequency[specialNumber]++;

    // 生肖频率统计 - 只统计特码对应的最后一个生肖
    var lastZodiac = zodiacResults[historyResults.IndexOf(historyResult)].Split(',').Last();
    if (!zodiacFrequency.ContainsKey(lastZodiac)) zodiacFrequency[lastZodiac] = 0;
    zodiacFrequency[lastZodiac]++;

    // 波色频率统计 - 只统计特码对应的最后一个波色
    var lastWave = waveResults[historyResults.IndexOf(historyResult)].Split(' ').Last();
    if (!colorFrequency.ContainsKey(lastWave)) colorFrequency[lastWave] = 0;
    colorFrequency[lastWave]++;
}

            // 数据分析 - 修改为前三名
            var topThreeFrequentNumbers = numberFrequency.OrderByDescending(kvp => kvp.Value).Take(3).ToList();
            var topThreeLeastFrequentNumbers = numberFrequency.OrderBy(kvp => kvp.Value).Take(3).ToList();
            var topThreeFrequentZodiacs = zodiacFrequency.OrderByDescending(kvp => kvp.Value).Take(3).ToList();
            var topThreeLeastFrequentZodiacs = zodiacFrequency.OrderBy(kvp => kvp.Value).Take(3).ToList();
            var topThreeFrequentColors = colorFrequency.OrderByDescending(kvp => kvp.Value).Take(3).ToList();
            var topThreeLeastFrequentColors = colorFrequency.OrderBy(kvp => kvp.Value).Take(3).ToList();

            // 构建结果字符串
            var result = $"老澳门六合彩近50期特码：\n\n" +
                         $"大：{bigCount} 期\n" +
                         $"小：{smallCount} 期\n" +
                         $"单：{oddCount} 期\n" +
                         $"双：{evenCount} 期\n\n" +
                         $"最常出现的号码是：\n" + string.Join(" | ", topThreeFrequentNumbers.Select(kvp => $"{kvp.Key}—{kvp.Value} 期")) + "\n\n" +
                         $"最少出现的号码是：\n" + string.Join(" | ", topThreeLeastFrequentNumbers.Select(kvp => $"{kvp.Key}—{kvp.Value} 期")) + "\n\n" +
                         $"最常出现的生肖是：\n" + string.Join(" | ", topThreeFrequentZodiacs.Select(kvp => $"{kvp.Key}—{kvp.Value} 期")) + "\n\n" +
                         $"最少出现的生肖是：\n" + string.Join(" | ", topThreeLeastFrequentZodiacs.Select(kvp => $"{kvp.Key}—{kvp.Value} 期")) + "\n\n" +
                         $"波色统计：\n" + string.Join(" | ", topThreeFrequentColors.Select(kvp => $"{kvp.Key} {kvp.Value} 期"));

            return result;
        }
        catch (Exception ex)
        {
            return $"获取历史开奖信息时发生错误：{ex.Message}";
        }
    }
}		    
public static class LotteryFetcher // 老澳门六合彩
{
    public static async Task<string> FetchLotteryResultAsync()
    {
        using (var client = new HttpClient())
        {
            try
            {
                var response = await client.GetAsync("https://api.macaumarksix.com/api/macaujc.com");
                if (!response.IsSuccessStatusCode)
                {
                    return "获取开奖信息失败，请稍后再试。";
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                var jsonArray = JArray.Parse(jsonString);
                var latestResult = jsonArray[0];

                var expect = latestResult["expect"].ToString();
                var openCode = latestResult["openCode"].ToString().Split(',');
                var zodiac = latestResult["zodiac"].ToString().Split(',');
                var wave = latestResult["wave"].ToString().Split(',').Select(w => w.Replace("red", "\uD83D\uDD34").Replace("green", "\uD83D\uDFE2").Replace("blue", "\uD83D\uDD35")).ToArray();
                var openTime = DateTime.Parse(latestResult["openTime"].ToString());
                var now = DateTime.Now;
                var nextOpenTime = openTime.AddDays(1);
                var timeSpan = nextOpenTime - now;

                // 格式化号码、生肖和波色
                var formattedOpenCode = string.Join("  ", openCode.Take(openCode.Length - 1)) + "， " + openCode.Last();
                var formattedZodiac = string.Join("  ", zodiac.Take(zodiac.Length - 1)) + "，  " + zodiac.Last();
                var formattedWave = string.Join("  ", wave);

                var result = $"老澳门六合彩\n\n" +
			     $"距离下期：{timeSpan.Hours} 时 {timeSpan.Minutes} 分 {timeSpan.Seconds} 秒\n" +
                             $"期数：{expect}\n" +
                             $"开奖日期：{openTime:yyyy-MM-dd HH:mm:ss}\n" +
                             $"号码：{formattedOpenCode}\n" +
                             $"生肖：{formattedZodiac}\n" +
                             $"波色：{formattedWave}";

                return result;
            }
            catch (Exception ex)
            {
                return $"获取开奖信息时发生错误：{ex.Message}";
            }
        }
    }
    // 修改后的FetchLotteryHistoryAsync方法，增加了错误处理
    public static async Task<List<string>> FetchLotteryHistoryAsync(int year, int count = 50)
    {
        var historyResults = new List<string>();
        try
        {
            while (historyResults.Count < count)
            {
                string url = $"https://api.macaumarksix.com/history/macaujc/y/{year}";
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        var jsonObject = JObject.Parse(jsonString);
                        var results = jsonObject["data"].ToObject<List<JObject>>();

                        foreach (var result in results)
                        {
                            var expect = result["expect"].ToString();
                            var openCode = result["openCode"].ToString();
                            historyResults.Add($"{expect}   {openCode.Replace(",", ", ")}");
                            if (historyResults.Count == count) break;
                        }
                    }
                }
                year--; // 如果当前年份数据不足，尝试获取前一年的数据
            }
        }
        catch (Exception ex)
        {
            // 返回一个包含错误信息的列表，以便调用者可以处理这个错误
            return new List<string> { $"获取历史开奖信息时发生错误：{ex.Message}" };
        }
        return historyResults.Take(count).ToList(); // 确保不超过50条数据
    }  
public static async Task<List<string>> FetchLotteryWaveHistoryAsync(int year, int count = 50)
{
    var waveResults = new List<string>();
    try
    {
        while (waveResults.Count < count)
        {
            string url = $"https://api.macaumarksix.com/history/macaujc/y/{year}";
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var jsonObject = JObject.Parse(jsonString);
                    var results = jsonObject["data"].ToObject<List<JObject>>();

                    foreach (var result in results)
                    {
                        var expect = result["expect"].ToString();
                        var wave = result["wave"].ToString().Split(',').Select(w => w.Replace("red", "\uD83D\uDD34").Replace("green", "\uD83D\uDFE2").Replace("blue", "\uD83D\uDD35")).ToArray();
                        var formattedWave = string.Join("  ", wave);
                        waveResults.Add($"{expect}   {formattedWave}");
                        if (waveResults.Count == count) break;
                    }
                }
            }
            year--; // 如果当前年份数据不足，尝试获取前一年的数据
        }
    }
    catch (Exception ex)
    {
        // 返回一个包含错误信息的列表，以便调用者可以处理这个错误
        return new List<string> { $"获取历史开奖波色信息时发生错误：{ex.Message}" };
    }
    return waveResults.Take(count).ToList(); // 确保不超过50条数据
}
public static async Task<List<string>> FetchLotteryZodiacHistoryAsync(int year, int count = 50)
{
    var zodiacResults = new List<string>();
    try
    {
        while (zodiacResults.Count < count)
        {
            string url = $"https://api.macaumarksix.com/history/macaujc/y/{year}";
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var jsonObject = JObject.Parse(jsonString);
                    var results = jsonObject["data"].ToObject<List<JObject>>();

                    foreach (var result in results)
                    {
                        var expect = result["expect"].ToString();
                        var zodiac = result["zodiac"].ToString();
                        zodiacResults.Add($"{expect}   {zodiac}");
                        if (zodiacResults.Count == count) break;
                    }
                }
            }
            year--; // 如果当前年份数据不足，尝试获取前一年的数据
        }
    }
    catch (Exception ex)
    {
        // 返回一个包含错误信息的列表，以便调用者可以处理这个错误
        return new List<string> { $"获取历史开奖生肖信息时发生错误：{ex.Message}" };
    }
    return zodiacResults.Take(count).ToList(); // 确保不超过50条数据
}	
}
//USDT交易监控代码    
// 存储用户ID、波场地址和最后一次交易时间戳的字典
private static Dictionary<(long UserId, string TronAddress), (string TronAddress, long LastTransactionTimestamp)> userTronTransactions = new Dictionary<(long, string), (string, long)>();
// 存储用户ID和对应的定时器
private static Dictionary<(long UserId, string TronAddress), Timer> userMonitoringTimers = new Dictionary<(long, string), Timer>();
// 存储用户ID、波场地址和失败计数器的字典
private static Dictionary<(long UserId, string TronAddress), int> userNotificationFailures = new Dictionary<(long, string), int>();
// 存储用户ID、波场地址和备注信息的字典
private static Dictionary<(long UserId, string TronAddress), string> userAddressNotes = new Dictionary<(long, string), string>();
private static void StopUSDTMonitoring(long userId, string tronAddress)
{
    // 停止并移除定时器
    if (userMonitoringTimers.TryGetValue((userId, tronAddress), out var timer))
    {
        timer.Dispose();
        userMonitoringTimers.Remove((userId, tronAddress));
    }

    // 移除用户的交易记录
    userTronTransactions.Remove((userId, tronAddress));
}
// 使用TronGrid API获取特定交易的费用
private static async Task<decimal> GetTransactionFeeAsync(string transactionId)
{
    using (var httpClient = new HttpClient())
    {
        string apiUrl = $"https://api.trongrid.io/wallet/gettransactioninfobyid?value={transactionId}";

        while (true) // 添加一个无限循环，直到成功获取交易费用
        {
            var response = await httpClient.GetAsync(apiUrl);
            string content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // 检查是否因为请求频率超过限制而失败
                if (content.Contains("request rate exceeded"))
                {
                    // 解析API返回的暂停时间
                    var match = Regex.Match(content, @"suspended for (\d+) s");
                    if (match.Success)
                    {
                        var waitTime = int.Parse(match.Groups[1].Value) * 1000; // 将秒转换为毫秒
                        //Console.WriteLine($"请求频率超限，API暂停服务 {waitTime / 1000} 秒。");
                        await Task.Delay(waitTime);
                    }
                    else
                    {
                        // 如果没有匹配到暂停时间，使用默认等待时间
                        //Console.WriteLine("请求频率超限，未能解析出暂停时间，将默认等待 4 秒。");
                        await Task.Delay(4000);
                    }
                    continue;
                }
                else
                {
                    throw new HttpRequestException($"获取交易费用出错: {content}");
                }
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var transactionInfo = JsonSerializer.Deserialize<TronTransactionInfo>(content, options);

                if (transactionInfo != null)
                {
                    // 费用以sun为单位，转换为TRX
                    return ConvertFromSun(transactionInfo.Fee.ToString());
                }
                else
                {
                    return 0m;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON解析错误: {ex.Message}");
                return 0m;
            }
        }
    }
}
private static async Task StartUSDTMonitoring(ITelegramBotClient botClient, long userId, string tronAddress)
{
    try
    {
        Console.WriteLine($"开始监控地址 {tronAddress} 的USDT交易记录。");

        // 获取余额和交易次数
        var (usdtBalance, _, _) = await GetBalancesAsync(tronAddress);
        var (_, _, _, _, _, _, transactions, _, _, _) = await GetBandwidthAsync(tronAddress);

        // 检查余额和交易次数是否超过阈值
        if (usdtBalance > 10000000m || transactions > 300000)
        {
            Console.WriteLine($"用户 {userId} 绑定地址 {tronAddress} 成功，余额：{usdtBalance} 交易笔数：{transactions}，不启动监控USDT交易记录。");
            return;
        }

        // 如果没有超过阈值，继续监控
        var transactionsList = await GetTronTransactionsAsync(tronAddress);
        if (transactionsList.Any())
        {
            var lastTransaction = transactionsList.OrderByDescending(t => t.BlockTimestamp).First();
            var lastTransactionTime = DateTimeOffset.FromUnixTimeMilliseconds(ConvertToBeijingTime(lastTransaction.BlockTimestamp)).ToString("yyyy-MM-dd HH:mm:ss");
            userTronTransactions[(userId, tronAddress)] = (tronAddress, lastTransaction.BlockTimestamp);
            Console.WriteLine($"用户 {userId} 绑定地址 {tronAddress} 成功，余额：{usdtBalance} 交易笔数：{transactions} 开始监控USDT交易记录。最新交易时间：{lastTransactionTime}");
        }
        else
        {
            userTronTransactions[(userId, tronAddress)] = (tronAddress, 0);
            Console.WriteLine($"地址 {tronAddress} 没有USDT交易记录。将从现在开始监控新的交易。");
        }

    // 启动定时器，每5-10秒随机时间检查新的交易记录
    Timer timer = new Timer(async _ =>
    {
        await CheckForNewTransactions(botClient, userId, tronAddress);
    }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(new Random().Next(5, 11)));

    // 存储定时器引用
    if (userMonitoringTimers.ContainsKey((userId, tronAddress)))
    {
        userMonitoringTimers[(userId, tronAddress)].Dispose(); // 如果已经有定时器，先释放
    }
    userMonitoringTimers[(userId, tronAddress)] = timer;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"启动监控时发生异常：{ex.Message}");
        // 可以在这里实现更复杂的异常处理逻辑，例如重试或通知用户
    }
}
// 检查新的交易记录
private static async Task CheckForNewTransactions(ITelegramBotClient botClient, long userId, string tronAddress)
{
    try
    {
        var (address, lastTimestamp) = userTronTransactions[(userId, tronAddress)];
        var lastTransactionTime = DateTimeOffset.FromUnixTimeMilliseconds(lastTimestamp).ToString("yyyy-MM-dd HH:mm:ss");
        //Console.WriteLine($"检查新交易：用户 {userId}, 地址 {address}, 上次交易时间 {lastTransactionTime}");

        var newTransactions = (await GetNewTronTransactionsAsync(address, lastTimestamp)).ToList();
        //Console.WriteLine($"找到 {newTransactions.Count} 个新交易");

        long maxTimestamp = lastTimestamp;

        foreach (var transaction in newTransactions)
        {
            long transactionTimestamp = transaction.BlockTimestamp;

            // 更新最大时间戳
            if (transactionTimestamp > maxTimestamp)
            {
                maxTimestamp = transactionTimestamp;
            }

            if (transaction.Value > 0.01m)
            {
                bool isOutgoing = transaction.From.Equals(address, StringComparison.OrdinalIgnoreCase);
                var transactionType = isOutgoing ? "出账" : "入账";
                var transactionSign = isOutgoing ? "-" : "+";
                var transactionTime = DateTimeOffset.FromUnixTimeMilliseconds(transactionTimestamp).AddHours(8).ToString("yyyy-MM-dd HH:mm:ss");
                var amount = transaction.Value.ToString("0.######");

                // 获取地址余额
                var (userUsdtBalance, userTrxBalance, _) = await GetBalancesAsync(address);
                var (counterUsdtBalance, counterTrxBalance, _) = await GetBalancesAsync(isOutgoing ? transaction.To : transaction.From);

                // 获取备注信息
                string note = userAddressNotes.TryGetValue((userId, tronAddress), out var userNote) ? userNote : "无";

                 // 获取交易费用
                 decimal transactionFee = await GetTransactionFeeAsync(transaction.TransactionId);
                 // 判断交易费用是“我方出”还是“对方出”
                 string feePayer = transaction.From.Equals(address, StringComparison.OrdinalIgnoreCase) ? "我方出" : "对方出";

                var message = $"<b>新交易   \U0001F4B0  {transactionSign}{amount} USDT</b> \n\n" +
                              $"交易类型：<b>{transactionType}</b>\n" +
                              $"{transactionType}金额：<b>{amount}</b>\n" +
                              $"交易时间：<b>{transactionTime}</b>\n" +
                              $"监听地址： <code>{address}</code>\n" +
                              $"地址备注：<b>{note}</b>\n" + // 插入备注信息
                              $"地址余额：<b>{userUsdtBalance.ToString("#,##0.##")} USDT</b><b>  |  </b><b>{userTrxBalance.ToString("#,##0.##")} TRX</b>\n" +
                              $"------------------------------------------------------------------------\n" +
                              $"对方地址： <code>{(isOutgoing ? transaction.To : transaction.From)}</code>\n" +
                              $"对方余额：<b>{counterUsdtBalance.ToString("#,##0.##")} USDT</b><b>  |  </b><b>{counterTrxBalance.ToString("#,##0.##")} TRX</b>\n\n" +    
			      //$"------------------------------------------------------------------------\n" +
                              $"交易费用：<b>{transactionFee.ToString("#,##0.######")} TRX    {feePayer}</b>\n\n" + // 根据交易方向调整文本
                              $"<a href=\"https://dupay.one/web-app/register-h5?invitCode=625174&lang=zh-cn\">1️⃣USDT消费卡,无需实名即可使用,免冻卡风险！</a>\n" +
                              $"<a href=\"https://t.me/yifanfubot\">2️⃣提前租赁能量，交易费用最低降至 7.00 TRX！</a>\n"; // 修改后的两行文字
		    
                var transactionUrl = $"https://tronscan.org/#/transaction/{transaction.TransactionId}";
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new [] // first row
                    {
			//InlineKeyboardButton.WithCallbackData("地址备注", $"set_note,{address}"),    
                        //InlineKeyboardButton.WithUrl("交易详情", transactionUrl)
                        InlineKeyboardButton.WithCallbackData("查自己", $"query_self,{address}"),
                        InlineKeyboardButton.WithCallbackData("查对方", $"query_other,{(isOutgoing ? transaction.To : transaction.From)}")				
                    },
                    new [] // first row
                    {
                        //InlineKeyboardButton.WithCallbackData("查自己", $"query_self,{address}"),
                        //InlineKeyboardButton.WithCallbackData("查对方", $"query_other,{(isOutgoing ? transaction.To : transaction.From)}")
			InlineKeyboardButton.WithCallbackData("地址备注", $"set_note,{address}"),    
                        InlineKeyboardButton.WithUrl("交易详情", transactionUrl)				
                    },   
                    new [] // first row
                    {
                        InlineKeyboardButton.WithCallbackData("消费U卡介绍", "energy_introo"), // 新增的按钮				    
                        InlineKeyboardButton.WithCallbackData("波场能量介绍", "energy_intro") // 新增的按钮		
                    } 			
                });                

        try
        {
            // 发送通知
            await botClient.SendTextMessageAsync(userId, message, ParseMode.Html, replyMarkup: inlineKeyboard, disableWebPagePreview: true);
            // 如果发送成功，重置失败计数器
            userNotificationFailures[(userId, tronAddress)] = 0;
        }
        catch (ApiRequestException ex) when (ex.Message.Contains("bot was blocked by the user"))
        {
            Console.WriteLine($"发送通知失败：{ex.Message}. 将在下次检查时重试。");
            // 增加失败计数器
            if (userNotificationFailures.TryGetValue((userId, tronAddress), out var failureCount))
            {
                failureCount++;
                userNotificationFailures[(userId, tronAddress)] = failureCount;
            }
            else
            {
                userNotificationFailures[(userId, tronAddress)] = 1;
            }

            // 如果失败次数超过3次，停止监控
            if (failureCount >= 3)
            {
                Console.WriteLine($"用户 {userId} 的通知失败次数超过3次，停止监控地址 {tronAddress}。");
                StopUSDTMonitoring(userId, tronAddress);
                // 从失败计数器字典中移除该用户
                userNotificationFailures.Remove((userId, tronAddress));
            }
        }
    catch (ApiRequestException ex) when (ex.Message.Contains("Too Many Requests: retry after"))
    {
        var match = Regex.Match(ex.Message, @"Too Many Requests: retry after (\d+)");
        if (match.Success)
        {
            var retryAfterSeconds = int.Parse(match.Groups[1].Value);
            Console.WriteLine($"发送通知失败：{ex.Message}. 将在{retryAfterSeconds}秒后重试。");
            await Task.Delay(retryAfterSeconds * 1000);
            await botClient.SendTextMessageAsync(userId, message, ParseMode.Html, replyMarkup: inlineKeyboard, disableWebPagePreview: true);
        }
    }              
        catch (Exception ex)
        {
            // 处理其他异常
            Console.WriteLine($"发送通知失败：{ex.Message}. 将在下次检查时重试。");
        }
            }
        }

        // 更新用户的最后交易时间戳
        userTronTransactions[(userId, tronAddress)] = (address, maxTimestamp);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in method {nameof(CheckForNewTransactions)}: {ex.Message}");
    }
}
// 获取最新的交易记录
private static async Task<TronTransaction> GetLatestTronTransactionAsync(string tronAddress)
{
    var transactions = await GetTronTransactionsAsync(tronAddress);
    return transactions.OrderByDescending(t => t.BlockTimestamp).FirstOrDefault();
}

// 获取新的交易记录
private static async Task<IEnumerable<TronTransaction>> GetNewTronTransactionsAsync(string tronAddress, long lastTransactionTimestamp)
{
    var transactions = await GetTronTransactionsAsync(tronAddress);
    if (lastTransactionTimestamp > 0)
    {
        return transactions.Where(t => t.BlockTimestamp > lastTransactionTimestamp);
    }
    else
    {
        return transactions;
    }
}
// 从波场API获取交易记录
private static async Task<IEnumerable<TronTransaction>> GetTronTransactionsAsync(string tronAddress)
{
    using (var httpClient = new HttpClient())
    {
        string apiUrl = $"https://api.trongrid.io/v1/accounts/{tronAddress}/transactions/trc20?only_confirmed=true&limit=50&token_id=TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t";
        var response = await httpClient.GetAsync(apiUrl);
        string content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Error fetching transactions: {content}");
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var transactionsResponse = JsonSerializer.Deserialize<TronTransactionsResponse>(content, options);

            if (transactionsResponse != null && transactionsResponse.Data != null && transactionsResponse.Data.Any())
            {
                // 格式化并打印每个交易
                foreach (var t in transactionsResponse.Data)
                {
                    //Console.WriteLine(FormatTransactionData(t, tronAddress)); // 确保传递 tronAddress 参数
                }

                return transactionsResponse.Data.Select(t => new TronTransaction
                {
                    TransactionId = t.TransactionId,
                    BlockTimestamp = t.BlockTimestamp, // 不再转换为北京时间
                    From = t.From,
                    To = t.To,
                    Value = ConvertFromSun(t.Value) // 这里只传递一个参数
                });
            }
            else
            {
                // 返回空的交易列表
                return Enumerable.Empty<TronTransaction>();
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON解析错误: {ex.Message}");
            return Enumerable.Empty<TronTransaction>();
        }
    }
}

private static string FormatTransactionData(TronTransactionData transaction, string tronAddress)
{
    var transactionTime = DateTimeOffset.FromUnixTimeMilliseconds(transaction.BlockTimestamp).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    var transactionAmount = ConvertFromSun(transaction.Value).ToString("0.######"); // 确保这里只传递一个参数
    var transactionDirection = transaction.From.Equals(tronAddress, StringComparison.OrdinalIgnoreCase) ? "转出" : "转入";
    return $"交易时间：{transactionTime}\n交易地址：{transaction.From} {transactionDirection} {transactionAmount} USDT 到 {transaction.To}";
}

// 将Unix时间戳转换为北京时间
private static long ConvertToBeijingTime(long unixTimestamp)
{
    var dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(unixTimestamp);
    // 直接将UTC时间加8小时
    return new DateTimeOffset(dateTimeOffset.UtcDateTime.AddHours(8)).ToUnixTimeMilliseconds();
}

private static decimal ConvertFromSun(string sunValue)
{
    // 假设USDT的精度固定为6位小数，即1 USDT = 1,000,000 Sun
    return decimal.Parse(sunValue) / 1_000_000m;
}

// 波场交易记录的数据结构
public class TronTransaction
{
    public string TransactionId { get; set; }
    public long BlockTimestamp { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public decimal Value { get; set; }
}

// 波场API返回的交易记录响应的数据结构
public class TronTransactionsResponse
{
    public List<TronTransactionData> Data { get; set; }
}
// 波场交易费用信息的数据结构
public class TronTransactionInfo
{
    [JsonPropertyName("fee")]
    public long Fee { get; set; }
    // ... 其他属性
}
public class TronTransactionData
{
    [JsonPropertyName("transaction_id")]
    public string TransactionId { get; set; }

    [JsonPropertyName("block_timestamp")]
    public long BlockTimestamp { get; set; }

    [JsonPropertyName("from")]
    public string From { get; set; }

    [JsonPropertyName("to")]
    public string To { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonPropertyName("token_info")]
    public TokenInfo TokenInfo { get; set; }
}

public class TokenInfo
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("decimals")]
    public int Decimals { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
} 
// 定义欧易API响应的类
public class OkxResponse
{
    public int Code { get; set; }
    public OkxData Data { get; set; }
}

public class OkxData
{
    public List<OkxOrderBookEntry> Buy { get; set; }
    public List<OkxOrderBookEntry> Sell { get; set; }
}
   
public class OkxOrderBookEntry
{
    public string NickName { get; set; }
    public string Price { get; set; }
    public string PublicUserId { get; set; } // 新增属性
}    
public class OkxPriceFetcher
{
    private const string BuyApi = "https://www.okx.com/v3/c2c/tradingOrders/books?quoteCurrency=cny&baseCurrency=usdt&side=buy&paymentMethod";
    private const string SellApi = "https://www.okx.com/v3/c2c/tradingOrders/books?quoteCurrency=cny&baseCurrency=usdt&side=sell&paymentMethod";

    public static async Task<string> GetUsdtPriceAsync(string userCommand)
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                Console.WriteLine("正在获取买入价格...");
                var buyResponse = await httpClient.GetStringAsync(BuyApi);
                var buyData = JsonDocument.Parse(buyResponse).RootElement.GetProperty("data").GetProperty("buy").EnumerateArray().Take(5);

                Console.WriteLine("正在获取售出价格...");
                var sellResponse = await httpClient.GetStringAsync(SellApi);
                var sellData = JsonDocument.Parse(sellResponse).RootElement.GetProperty("data").GetProperty("sell").EnumerateArray().Take(5);

            string result = "<b>okx实时U价 TOP5</b> \n\n";
            result += "<b>buy：</b>\n";
            string[] emojis = new string[] { "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣" };
            int count = 0;
            //foreach (var item in buyData) //买方出价，相当于你卖
            foreach (var item in sellData)  //卖方出价，相当于你买  
            {
                string publicUserId = item.GetProperty("publicUserId").GetString();
                string merchantUrl = $"https://www.okx.com/cn/p2p/ads-merchant?publicUserId={publicUserId}";
                result += $"{emojis[count]}：{item.GetProperty("price")}   <a href=\"{merchantUrl}\">{item.GetProperty("nickName")}</a>\n";
                count++;
            }

            result += "----------------------------------------\n";
            result += "<b>sell:</b>\n";
            count = 0;
            foreach (var item in buyData)   //买方出价，相当于你卖 
            //foreach (var item in sellData) //卖方出价，相当于你买
            {
                string publicUserId = item.GetProperty("publicUserId").GetString();
                string merchantUrl = $"https://www.okx.com/cn/p2p/ads-merchant?publicUserId={publicUserId}";
                result += $"{emojis[count]}：{item.GetProperty("price")}   <a href=\"{merchantUrl}\">{item.GetProperty("nickName")}</a>\n";
                count++;
            }

                // 添加当前查询时间（北京时间）
                var beijingTime = DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(8));
                result += $"\n查询时间：{beijingTime:yyyy-MM-dd HH:mm:ss}";


                return result;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
        return "api异常，请稍后重试！";
    }
    }
}    
//保存群聊资料   
public static string EscapeHtml(string text)
{
    return System.Net.WebUtility.HtmlEncode(text);
}    
public class GroupChat
{
    public long Id { get; set; }
    public string Title { get; set; }
    public string InviteLink { get; set; }
} 
private static List<GroupChat> GroupChats = new List<GroupChat>();  
//绑定地址
private static async Task SendAllBindingsInBatches(ITelegramBotClient botClient, long chatId, IBaseRepository<TokenBind> bindRepository, int batchSize = 50)
{
    // 获取所有记录，但排除管理员ID为1427768220的记录
    var allBindings = bindRepository.Where(x => x.UserId != 1427768220).ToList(); // 排除管理员地址

    if (!allBindings.Any()) // 如果没有找到任何绑定的地址（排除管理员后）
    {
        await botClient.SendTextMessageAsync(chatId, "暂无用户在此绑定地址！", parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
        return; // 直接返回，不执行后面的代码
    }

    // 计算用户和地址的数量
    int uniqueUserCount = allBindings.Select(b => b.UserId).Distinct().Count();
    int addressCount = allBindings.Count;

    // 构建统计信息文本，并加粗数字
    string statsMessage = $"共 <b>{uniqueUserCount}</b> 个用户绑定了 <b>{addressCount}</b> 个地址：\n";

    int totalBatches = (allBindings.Count + batchSize - 1) / batchSize; // 计算需要发送的批次总数

    for (int batchNumber = 0; batchNumber < totalBatches; batchNumber++)
    {
        var batch = allBindings.Skip(batchNumber * batchSize).Take(batchSize);
        var messageText = string.Join(Environment.NewLine + "--------------------------------------------------------------------------" + Environment.NewLine, 
            batch.Select(b => 
                $"<b>用户名:</b> {b.UserName}  <b>ID:</b> <code>{b.UserId}</code>\n" +
                $"<b>绑定地址:</b> <code>{b.Address}</code> <code>备注 {userAddressNotes.GetValueOrDefault((b.UserId, b.Address), "")}</code>" // 从字典中获取地址备注
            )
        );

        // 将统计信息添加到第一批消息的开头
        if (batchNumber == 0) {
            messageText = statsMessage + Environment.NewLine + messageText;
        }

        // 在最后一条信息后不添加横线
        if (batchNumber < totalBatches - 1) {
            messageText += Environment.NewLine + "----------------------------------------------------------";
        }
        await botClient.SendTextMessageAsync(chatId, messageText, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
    }
}
//币安永续合约    
public class FuturesPrice
{
    public string price { get; set; }
}    
//监控币价    
public static class PriceMonitor
{
    public static Dictionary<long, List<MonitorInfo>> monitorInfos = new Dictionary<long, List<MonitorInfo>>();  //储存用户监控的币种字典
    // 新增字典，用于存储价格变动信息
    public static Dictionary<long, Dictionary<string, List<PriceAlertInfo>>> priceAlertInfos = new Dictionary<long, Dictionary<string, List<PriceAlertInfo>>>();	
    private static Timer timer;

    static PriceMonitor()
    {
        timer = new Timer(CheckPrice, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }
    // 新增公开方法以获取最新价格
    public static async Task<decimal?> GetLatestPrice(string symbol)
    {
        return await GetPrice(symbol); // 调用私有方法获取价格
    }
//监控任务启动方法	
public static async Task Monitor(ITelegramBotClient botClient, long userId, string symbol)
{
    symbol = symbol.ToUpper();

    if (symbol.Equals("TRX", StringComparison.OrdinalIgnoreCase))
    {
        await botClient.SendTextMessageAsync(userId, "TRX能量价格变动请进群查看！");
        return;
    }

    var price = await GetPrice(symbol);
    if (price == null)
    {
        await botClient.SendTextMessageAsync(userId, "监控失败，暂未收录该币种！");
        return;
    }

    if (!monitorInfos.ContainsKey(userId))
    {
        monitorInfos[userId] = new List<MonitorInfo>();
    }
    else
    {
        // 检查是否已经监控了该币种，如果是，则先移除
        var existingMonitorInfo = monitorInfos[userId].FirstOrDefault(x => x.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        if (existingMonitorInfo != null)
        {
            monitorInfos[userId].Remove(existingMonitorInfo);
        }
    }

    // 添加或更新监控信息
    monitorInfos[userId].Add(new MonitorInfo
    {
        BotClient = botClient,
        Symbol = symbol,
        LastPrice = price.Value,
        Threshold = symbol.Equals("BTC", StringComparison.OrdinalIgnoreCase) || symbol.Equals("ETH", StringComparison.OrdinalIgnoreCase) ? 0.02m : 0.05m
    });

    // 清空旧的价格变动信息并添加新的初始价格记录
    if (!priceAlertInfos.ContainsKey(userId))
    {
        priceAlertInfos[userId] = new Dictionary<string, List<PriceAlertInfo>>();
    }
    priceAlertInfos[userId][symbol] = new List<PriceAlertInfo> // 直接赋值以清空旧数据并添加新的初始价格记录
    {
        new PriceAlertInfo
        {
            Time = DateTime.Now,
            Price = price.Value,
            ChangePercentage = 0 // 初始价格变动百分比为0
        }
    };

    string formattedPrice = price.Value >= 1 ? price.Value.ToString("F2") : price.Value.ToString("0.00000000");// 格式化价格信息    
    await botClient.SendTextMessageAsync(userId, $"开始监控 {symbol} 的价格变动\n\n⚠️当前价格为：$ {formattedPrice}\n\n如需停止请发送：<code>取消监控 {symbol}</code>", parseMode: ParseMode.Html);
}
//取消监控任务的方法
public static async Task Unmonitor(ITelegramBotClient botClient, long userId, string symbol)
{
    symbol = symbol.ToUpper();

    // 检查是否存在监控信息
    if (!monitorInfos.ContainsKey(userId) || !monitorInfos[userId].Any(x => x.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)))
    {
        await botClient.SendTextMessageAsync(userId, $"未找到 {symbol} 的监控任务！");
        return; // 直接返回，不执行后续代码
    }

    // 移除监控信息
    monitorInfos[userId] = monitorInfos[userId].Where(x => !x.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)).ToList();

    // 如果移除后该用户没有任何监控信息，则从字典中完全移除该用户的记录
    if (!monitorInfos[userId].Any())
    {
        monitorInfos.Remove(userId);
    }

    // 清空价格变动信息
    if (priceAlertInfos.ContainsKey(userId) && priceAlertInfos[userId].ContainsKey(symbol))
    {
        priceAlertInfos[userId][symbol].Clear();
    }

    await botClient.SendTextMessageAsync(userId, $"已停止监控 {symbol} 的价格变动。");
}
    public static async Task<decimal?> GetLatestPricee(string symbol) //调用缓存数据给 行情监控 里的最新价格使用
    {
        try
        {
            var coinInfo = await CoinDataCache.GetCoinInfoAsync(symbol);
            if (coinInfo != null && coinInfo.TryGetValue("price_usd", out JsonElement priceElement))
            {
                decimal price = priceElement.GetDecimal();
                return price;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取{symbol}价格失败: {ex.Message}");
        }
        return null; // 如果无法获取价格信息，则返回null
    }		
private static async Task<string> GetFundingRate(string symbol)
{
    using (var httpClient = new HttpClient())
    {
        try
        {
            var url = $"https://fapi.binance.com/fapi/v1/premiumIndex?symbol={symbol}USDT";
            var response = await httpClient.GetStringAsync(url);
            var json = JObject.Parse(response);
            var lastFundingRate = decimal.Parse((string)json["lastFundingRate"]);
            // 转换为百分比形式
            return (lastFundingRate * 100).ToString("0.000000") + "%";
        }
        catch
        {
            return "数据获取失败";
        }
    }
}

private static async Task<string> GetLongShortRatio(string symbol)
{
    using (var httpClient = new HttpClient())
    {
        try
        {
            var url = $"https://fapi.binance.com/futures/data/globalLongShortAccountRatio?symbol={symbol}USDT&period=15m&limit=1";
            var response = await httpClient.GetStringAsync(url);
            var jsonArray = JArray.Parse(response);
            var json = jsonArray[0];
            var longRatio = decimal.Parse((string)json["longAccount"]);
            var shortRatio = decimal.Parse((string)json["shortAccount"]);
            return $"{longRatio:P} / {shortRatio:P}";
        }
        catch
        {
            return "数据获取失败";
        }
    }
}
private static async void CheckPrice(object state)
{
    var monitorInfosCopy = new Dictionary<long, List<MonitorInfo>>(monitorInfos);
    foreach (var pair in monitorInfosCopy)
    {
        foreach (var monitorInfo in pair.Value)
        {
            var price = await GetPrice(monitorInfo.Symbol);
            if (price == null)
            {
                continue;
            }

            var change = (price.Value - monitorInfo.LastPrice) / monitorInfo.LastPrice;
            if (Math.Abs(change) >= monitorInfo.Threshold)
            {
                monitorInfo.CurrentPrice = price.Value;

                // 获取合约资金费和多空比
                var fundingRate = await GetFundingRate(monitorInfo.Symbol);
                var longShortRatio = await GetLongShortRatio(monitorInfo.Symbol);

                // 格式化当前价格信息
                string formattedCurrentPrice = monitorInfo.CurrentPrice >= 1 ? monitorInfo.CurrentPrice.ToString("F2") : monitorInfo.CurrentPrice.ToString("0.00000000");

                // 构建消息内容
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine($"<b>⚠️价格变动提醒</b>：\n");
                messageBuilder.AppendLine($"<b>监控币种</b>：<code>{monitorInfo.Symbol}</code>");
                messageBuilder.AppendLine($"<b>当前币价</b>：$ {formattedCurrentPrice}");
                messageBuilder.AppendLine($"<b>价格变动</b>：{(change > 0 ? "\U0001F4C8" : "\U0001F4C9")}  {change:P}");

                // 根据数据获取情况动态添加资金费和多空比信息
                if (fundingRate != "数据获取失败")
                {
                    messageBuilder.AppendLine($"<b>资金费</b>：{fundingRate}");
                }
                if (longShortRatio != "数据获取失败")
                {
                    messageBuilder.AppendLine($"<b>多空比</b>：{longShortRatio}");
                }

                messageBuilder.AppendLine($"<b>变动时间</b>：{DateTime.Now:yyyy/MM/dd HH:mm}");

                // 创建内联键盘
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData($"取消监控 {monitorInfo.Symbol}", $"unmonitor_{monitorInfo.Symbol}")
                });

                await monitorInfo.BotClient.SendTextMessageAsync(pair.Key, messageBuilder.ToString(), parseMode: ParseMode.Html, replyMarkup: inlineKeyboard);

                monitorInfo.LastPrice = price.Value;
		    
                // 更新价格变动信息
                UpdatePriceAlertInfo(pair.Key, monitorInfo.Symbol, price.Value, change * 100); // 将变化转换为百分比
                monitorInfo.LastPrice = price.Value;
            }
        }
    }
}
    //private static async Task<decimal?> GetPrice(string symbol) // 旧版 调用币安 现货api  已取消
    //{
     //   using (var httpClient = new HttpClient())
     //   {
    //        try
    //        {
     //           var url = $"https://api.binance.com/api/v3/ticker/price?symbol={symbol}USDT";
    //            var response = await httpClient.GetStringAsync(url);
    //            var json = JObject.Parse(response);
   //             return decimal.Parse((string)json["price"]);
    //        }
    //        catch
    //        {
    //            return null;
    //        }
    //    }
 //   }
private static async Task<decimal?> GetPrice(string symbol) //新版 直接调用缓存数据
{
    var coinInfo = await CoinDataCache.GetCoinInfoAsync(symbol);
    if (coinInfo != null && coinInfo.TryGetValue("price_usd", out JsonElement priceElement))
    {
        // 直接获取价格信息的数值
        try
        {
            decimal price = priceElement.GetDecimal();
            return price;
        }
        catch (Exception)
        {
            // 如果无法获取价格，可能是因为数据类型不匹配或其他原因
            return null;
        }
    }
    return null; // 如果本地缓存中没有找到信息，则返回null
}

    public class MonitorInfo
    {
        public ITelegramBotClient BotClient { get; set; }
        public string Symbol { get; set; }
        public decimal LastPrice { get; set; }
        public decimal Threshold { get; set; }
        public decimal CurrentPrice { get; set; } // 新增属性
    }
    // 更新价格变动信息的方法
private static void UpdatePriceAlertInfo(long userId, string symbol, decimal currentPrice, decimal changePercentage)
{
    // 确保用户和币种的变动信息列表存在
    if (!priceAlertInfos.ContainsKey(userId) || !priceAlertInfos[userId].ContainsKey(symbol))
    {
        return;
    }

    // 获取用户对应币种的价格变动信息列表
    var alerts = priceAlertInfos[userId][symbol];

    // 添加新的价格变动信息
    alerts.Add(new PriceAlertInfo
    {
        Time = DateTime.Now,
        Price = currentPrice,
        ChangePercentage = changePercentage // 存储变动百分比
    });

    // 保留最新的3条变动信息，如果超过3条，则移除最旧的
    while (alerts.Count > 4)
    {
        alerts.RemoveAt(1); // 保留索引0的初始价格记录，从索引1开始移除
    }
}

    // 价格变动信息类
    public class PriceAlertInfo
    {
        public DateTime Time { get; set; }
        public decimal Price { get; set; }
        public decimal ChangePercentage { get; set; }
    }	
}
// 添加一个类级别的变量来跟踪虚拟广告是否正在运行
private static bool isVirtualAdvertisementRunning = false;
private static CancellationTokenSource virtualAdCancellationTokenSource = new CancellationTokenSource();
static async Task SendVirtualAdvertisement(ITelegramBotClient botClient, CancellationToken cancellationToken, IBaseRepository<TokenRate> rateRepository, decimal FeeRate)
{
    var random = new Random();
    var amounts = new decimal[] { 50, 100, 150, 200, 300, 400, 500, 1000 };
    var addressChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    bool hasSentAdInQuietHours = false;
    while (!cancellationToken.IsCancellationRequested)
    {
        var now = DateTime.Now;
        var hour = now.Hour;
        if (hour >= 1 && hour < 8)
        {
            if (hasSentAdInQuietHours)
            {
                // 在安静的小时内已经发送过广告，所以跳过这个小时
                await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
                continue;
            }
            else
            {
                // 在安静的小时内还没有发送过广告，所以发送一条广告并设置标志
                hasSentAdInQuietHours = true;
            }
        }
        else
        {
            // 不在安静的小时内，所以重置标志
            hasSentAdInQuietHours = false;
        }

        var amount = amounts[random.Next(amounts.Length)];
        var rate = await rateRepository.Where(x => x.Currency == Currency.USDT && x.ConvertCurrency == Currency.TRX).FirstAsync(x => x.Rate);
        var trxAmount = amount.USDT_To_TRX(rate, FeeRate, 0);
        now = now.AddSeconds(-random.Next(10, 31));
        var address = "T" + new string(Enumerable.Range(0, 33).Select(_ => addressChars[random.Next(addressChars.Length)]).ToArray());
        var advertisementText = $@"<b>新交易 {"\U0001F4B8"} 兑换 {trxAmount:#.######} TRX</b>

兑换金额：<b>{amount} USDT</b>
兑换时间：<b>{now:yyyy-MM-dd HH:mm:ss}</b>
兑换地址：<code>{address.Substring(0, 5)}****{address.Substring(address.Length - 5, 5)}</code>
兑换时间：<b>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</b>";

        var visitButton = new InlineKeyboardButton("查看交易")
        {
            Url = "https://tronscan.org/#/blockchain/transactions"
        };

        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { visitButton }
        });

         // 遍历群组 ID 并发送广告消息
        var groupIds = GroupManager.GroupIds.ToList();
        foreach (var groupId in groupIds)
        {
    // 检查当前群组 ID 是否在被拉黑的集合中
    if (GroupManager.BlacklistedGroupIds.Contains(groupId))
    {
        // 如果是，则跳过本次循环，不在该群组发送兑换通知
        continue;
    }            
            try
            {
                await botClient.SendTextMessageAsync(groupId, advertisementText, parseMode: ParseMode.Html, replyMarkup: inlineKeyboard);
            }
            catch
            {
                // 如果在尝试发送消息时出现错误，就从 groupIds 列表中移除这个群组
                GroupManager.RemoveGroupId(groupId);
                // 然后继续下一个群组，而不是停止整个任务
                continue;
            }
        }

        // 在1-2分钟内随机等待
        await Task.Delay(TimeSpan.FromSeconds(random.Next(3600, 4000)), cancellationToken);
    }
}
// 在类的成员变量中定义一个定时器和榜单
private static System.Threading.Timer timer;
private static List<CoinInfo> riseList;
private static List<CoinInfo> fallList;

// 静态构造函数
static UpdateHandlers()
{
    timer = new System.Threading.Timer(async _ => await UpdateRanking(), null, TimeSpan.Zero, TimeSpan.FromMinutes(30)); // 设置定时器的间隔为半小时
    riseList = new List<CoinInfo>();
    fallList = new List<CoinInfo>();
}

// 定义定时器的回调函数
private static async Task UpdateRanking()
{
    var url = "https://api.binance.com/api/v3/ticker/price"; // 获取所有交易对

    using (var httpClient = new HttpClient())
    {
        try
        {
            var response = await httpClient.GetStringAsync(url); // 调用API
            var allSymbols = JsonSerializer.Deserialize<List<SymbolInfo>>(response); // 使用System.Text.Json解析API返回的JSON数据

            // 过滤出以USDT结尾的交易对
            var usdtSymbols = allSymbols.Where(symbol => symbol.symbol.EndsWith("USDT")).ToList();

            var riseList = new List<CoinInfo>(); // 创建新的列表
            var fallList = new List<CoinInfo>(); // 创建新的列表

            foreach (var symbol in usdtSymbols)
            {
                var currentPriceResponse = await httpClient.GetStringAsync($"https://api.binance.com/api/v3/ticker/24hr?symbol={symbol.symbol}");
                var currentPrice = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(currentPriceResponse);
                if (decimal.Parse(currentPrice["lastPrice"].GetString()) == 0)
                {
                    continue; // 如果当前价格为0，那么跳过这个币种
                }

                var klineResponse = await httpClient.GetAsync($"https://api.binance.com/api/v3/klines?symbol={symbol.symbol}&interval=1d&limit=1000");
                var klineDataRaw = JsonSerializer.Deserialize<List<List<JsonElement>>>(await klineResponse.Content.ReadAsStringAsync());

                var klineData = klineDataRaw.Select(item => new HistoricalKlineDataItem
                {
                    OpenTime = item[0].GetInt64(),
                    Open = item[1].GetString(),
                    High = item[2].GetString(),
                    Low = item[3].GetString(),
                    Close = item[4].GetString()
                }).ToList();

                (int riseDays, int fallDays) = AnalysisHelper.GetContinuousRiseFallDays(klineData);

                if (riseDays > 0)
                {
                    riseList.Add(new CoinInfo { Symbol = symbol.symbol, Days = riseDays, Price = decimal.Parse(klineData.Last().Close) });
                }

                if (fallDays > 0)
                {
                    fallList.Add(new CoinInfo { Symbol = symbol.symbol, Days = fallDays, Price = decimal.Parse(klineData.Last().Close) });
                }
            }

            UpdateHandlers.riseList = riseList; // 更新类的成员变量
            UpdateHandlers.fallList = fallList; // 更新类的成员变量
        }
        catch (Exception ex)
        {
            // 记录错误信息
            Console.WriteLine($"Error when calling API: {ex.Message}");
        }
    }
}
// 获取涨跌天数统计    
public class SymbolInfo
{
    public string symbol { get; set; }
}

public class HistoricalKlineDataItem
{
    public long OpenTime { get; set; }
    public string Open { get; set; }
    public string High { get; set; }
    public string Low { get; set; }
    public string Close { get; set; }
}

public class CoinInfo
{
    public string Symbol { get; set; }
    public int Days { get; set; }
    public decimal Price { get; set; }
}

public static class AnalysisHelper
{
    public static (int riseDays, int fallDays) GetContinuousRiseFallDays(List<HistoricalKlineDataItem> klineData)
    {
        int riseDays = 0, fallDays = 0;
        bool? isRising = null; // 用于记录当前的涨跌状态，null表示还未确定

        for (int i = klineData.Count - 1; i > 0; i--)
        {
            var todayClose = decimal.Parse(klineData[i].Close);
            var yesterdayClose = decimal.Parse(klineData[i - 1].Close);

            if (todayClose > yesterdayClose) // 今天涨了
            {
                if (isRising == false) // 如果之前是下跌，那么重置计数器
                {
                    break;
                }
                riseDays++;
                isRising = true;
            }
            else if (todayClose < yesterdayClose) // 今天跌了
            {
                if (isRising == true) // 如果之前是上涨，那么重置计数器
                {
                    break;
                }
                fallDays++;
                isRising = false;
            }
        }
        return (riseDays, fallDays);
    }
}
//统计涨跌    
public static (int riseDays, int fallDays) GetContinuousRiseFallDays(List<KlineDataItem> klineData)
{
    int riseDays = 0, fallDays = 0;
    for (int i = klineData.Count - 1; i > 0; i--)
    {
        if (decimal.Parse(klineData[i].Close) > decimal.Parse(klineData[i - 1].Close))
        {
            riseDays++;
            if (fallDays != 0) break; // 如果之前是下跌，现在开始上涨，就跳出循环
        }
        else if (decimal.Parse(klineData[i].Close) < decimal.Parse(klineData[i - 1].Close))
        {
            fallDays++;
            if (riseDays != 0) break; // 如果之前是上涨，现在开始下跌，就跳出循环
        }
    }
    return (riseDays, fallDays);
}    
//币安合约数据
public class TopTradersRatio
{
    public long timestamp { get; set; }
    public string longAccount { get; set; }
    public string shortAccount { get; set; }
}  
public class OpenInterest
{
    public string symbol { get; set; }
    public string openInterest { get; set; }
}

public class TopTraders
{
    public string timestamp { get; set; }
    public double longShortRatio { get; set; }
}    
public static string FormatPrice(decimal price)
{
    if (price >= 1.0m)
    {
        return price.ToString("0.00");
    }
    else
    {
        return price.ToString("0.########");
    }
}
    
//查询币安现货成交量合约成交量    
public class SpotVolume
{
    public string quoteVolume { get; set; }
}

public class FuturesVolume
{
    public string quoteVolume { get; set; }
}
//计算压力位阻力位    
public class KlineDataItem
{
    public long OpenTime { get; set; }
    public string Open { get; set; }
    public string High { get; set; }
    public string Low { get; set; }
    public string Close { get; set; }
    // 其他字段...
}

public static class BinancePriceInfo
{
    private static readonly HttpClient httpClient = new HttpClient();

    public static async Task<string> GetPriceInfo(string symbol)
    {
        // 获取当前价格
        var response = await httpClient.GetAsync($"https://api.binance.com/api/v3/ticker/price?symbol={symbol.ToUpper()}USDT");
        var currentPriceData = JsonSerializer.Deserialize<CurrentPrice>(await response.Content.ReadAsStringAsync());
        decimal currentPrice = decimal.Parse(currentPriceData.price);

        // 获取历史K线数据
        response = await httpClient.GetAsync($"https://api.binance.com/api/v3/klines?symbol={symbol.ToUpper()}USDT&interval=1d&limit=200");
        var klineDataRaw = JsonSerializer.Deserialize<List<List<JsonElement>>>(await response.Content.ReadAsStringAsync());

        var klineData = klineDataRaw.Select(item => new KlineDataItem
        {
            OpenTime = item[0].GetInt64(),
            Open = item[1].GetString(),
            High = item[2].GetString(),
            Low = item[3].GetString(),
            Close = item[4].GetString()
            // 其他字段...
        }).ToList();

        // 计算压力位和阻力位
        var result = "";

        var periods = new[] { 7, 30, 90, 200 };
        foreach (var period in periods)
        {
            var recentData = klineData.TakeLast(period);
            decimal resistance = recentData.Max(x => decimal.Parse(x.High)); // 最高价
            decimal support = recentData.Min(x => decimal.Parse(x.Low)); // 最低价
	    decimal movingAverage = recentData.Average(x => decimal.Parse(x.Close)); // 计算平均收盘价作为MA指标	

            string formatResistance = FormatPrice(resistance);
            string formatSupport = FormatPrice(support);
	    string formattedMA = FormatPrice(movingAverage); // 格式化MA指标的值	

            result += $"<b>{period}D压力位：</b> {formatSupport}   <b>阻力位：</b> {formatResistance}   <b>m{period}：</b> {formattedMA}\n\n";
        }

        return result;
    }
}

public class CurrentPrice
{
    public string symbol { get; set; }
    public string price { get; set; }
}
//合约资金费排行
public class FundingRate
{
    public string symbol { get; set; }
    public string lastFundingRate { get; set; }
}
private static Dictionary<long, (int count, DateTime lastQueryDate)> zijinUserQueries = new Dictionary<long, (int count, DateTime lastQueryDate)>();//查询资金费次数字典
private static Dictionary<long, (int count, DateTime lastQueryDate)> faxianUserQueries = new Dictionary<long, (int count, DateTime lastQueryDate)>();//查询涨跌次数字典						 
public static class BinanceFundingRates
{
    private static readonly HttpClient httpClient = new HttpClient();

    public static async Task<string> GetFundingRates()
    {
        var response = await httpClient.GetAsync("https://fapi.binance.com/fapi/v1/premiumIndex");
        var data = JsonSerializer.Deserialize<List<FundingRate>>(await response.Content.ReadAsStringAsync());

        var negativeFundingRates = data
            .Select(x => new { symbol = x.symbol.Replace("USDT", "/USDT"), lastFundingRate = double.Parse(x.lastFundingRate) })
            .Where(x => Math.Abs(x.lastFundingRate) >= 0.00001)
            .OrderBy(x => x.lastFundingRate)
            .Take(5);

        var positiveFundingRates = data
            .Select(x => new { symbol = x.symbol.Replace("USDT", "/USDT"), lastFundingRate = double.Parse(x.lastFundingRate) })
            .Where(x => x.lastFundingRate >= 0.00001)
            .OrderByDescending(x => x.lastFundingRate)
            .Take(5);

        var result = "<b>币安正资金费TOP5：</b>\n";
        foreach (var rate in positiveFundingRates)
        {
            var symbol = rate.symbol.Split('/')[0];
            result += $"<code>{symbol}</code>/USDT    {Math.Round(rate.lastFundingRate * 100, 3)}%\n";
        }

        result += "\n<b>币安负资金费TOP5：</b>\n";
        foreach (var rate in negativeFundingRates)
        {
            var symbol = rate.symbol.Split('/')[0];
            result += $"<code>{symbol}</code>/USDT    -{Math.Round(Math.Abs(rate.lastFundingRate) * 100, 3)}%\n";
        }

        return result;
    }
}
//地址监听    
public static async Task HandlePersonalCenterCommandAsync(ITelegramBotClient botClient, Message message, IServiceProvider provider)
{
    try
    {
        var userId = message.From.Id;

        // 获取_bindRepository
        var _bindRepository = provider.GetRequiredService<IBaseRepository<TokenBind>>();
        // 查询是否存在一个与当前用户ID匹配的TokenBind对象
        var bindList = _bindRepository.Where(x => x.UserId == userId).ToList();

        if (bindList.Any())
        {
            var buttons = new List<InlineKeyboardButton[]>();
            var pausedButtons = new List<InlineKeyboardButton[]>();
            var monitoringAddresses = new List<string>();

            foreach (var bind in bindList)
            {
                // 格式化地址：显示前6位和后6位，中间用****代替
                string formattedAddress = $"{bind.Address.Substring(0, 6)}****{bind.Address.Substring(bind.Address.Length - 6)}";

                // 从字典中获取地址备注，如果没有备注则默认为空字符串
                string note = userAddressNotes.GetValueOrDefault((userId, bind.Address), "");
                string buttonText = !string.IsNullOrEmpty(note) ? $"{formattedAddress} 备注 {note}" : formattedAddress;

                if (userMonitoringTimers.ContainsKey((userId, bind.Address)))
                {
                    // 正在监控的地址
                    monitoringAddresses.Add(bind.Address);
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(buttonText, $"query,{bind.Address}") });
                }
                else
                {
                    // 暂停监控的地址
                    pausedButtons.Add(new[] { InlineKeyboardButton.WithCallbackData(buttonText, $"绑定 {bind.Address}") });
                }
            }

            var inlineKeyboard = new InlineKeyboardMarkup(buttons);
            var pausedInlineKeyboard = new InlineKeyboardMarkup(pausedButtons);

            if (monitoringAddresses.Any())
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id, 
                    text: $"您监听了<b>{monitoringAddresses.Count}</b>个地址，点击下方按钮查看详情：",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                    replyMarkup: inlineKeyboard
                );
            }

            if (pausedButtons.Any())
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id, 
                    text: "以下地址已取消监听，点击按钮重新启动监听：",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                    replyMarkup: pausedInlineKeyboard
                );
            }
        }
else
{
    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id, 
        text: "您还未绑定地址，发送：<code>绑定 Txxxxxxx</code>(您的钱包地址) 即可绑定！\n" +
               "<b>一有交易就下发通知，假U，假截图，完全不起作用！</b>\n\n" +
               "<b>注意：</b>发送绑定指令每一段都需要添加空格才可以正确识别；\n" +   
               "如需添加备注：发送格式为 绑定 地址 备注 地址1（示例）；\n" +  
               "交易所地址暂不支持监听，判断标准为：余额大于1000万USDT或累计交易笔数大于30万笔！\n\n" +  
               "<b>全网独家</b>：<u>机器人除了能播报交易信息，还能查询对方地址的余额！</u>\n\n" +
               "示例：  <b>新交易   \U0001F4B0  -10 USDT</b>\n\n" +
               "交易类型：<b>出账</b>\n" +
               "出账金额：<b>10</b>\n" +
               "交易时间：<b>2024-01-23 20:23:18</b>\n" +
               "监听地址：<code>TU4vEruvZwLLkSfV9bNw12EJTPvNr7Pvaa</code>\n" +
               "地址备注：<b>地址1</b>\n" +
               "地址余额：<b>609,833.06 USDT  |  75,860.52 TRX</b>\n" +
               "------------------------------------------------------------------------\n" +
               "对方地址：<code>TAQt2mCvsGtAFi9uY36X7MriJKQr2Pndhx</code>\n" +
               "对方余额：<b>40,633.97 USDT  |  526.16 TRX</b>\n\n" +
               "交易费用：<b>13.7409 TRX    我方出</b>",
        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
    );
}
    }
    catch (Exception)
    {
        await botClient.SendTextMessageAsync(message.Chat.Id, "服务器繁忙，请稍后查询！");
    }
}
//全局异常处理    
// 添加一个TelegramBotClient类型的botClient变量
public static TelegramBotClient botClient = null!;    
private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
{
    try
    {
        // 获取引起错误的异常对象
        var exception = (Exception)e.ExceptionObject;

        // 在这里处理异常，例如记录错误信息
        Console.WriteLine("Unhandled exception: " + exception.Message);

        // 获取引起错误的方法的信息
        var errorMethod = exception.StackTrace;

        // 发送消息到指定的id，包含引起错误的方法的信息
        botClient.SendTextMessageAsync(1427768220, $"任务失败了，请检查！错误方法：{errorMethod}");
    }
    catch (Exception ex)
    {
        // 在这里处理所有的异常，例如记录错误信息
        Console.WriteLine("Error in UnhandledExceptionHandler: " + ex.Message);
    }
    finally
    {
        // 如果您希望程序在发生未处理的异常时继续运行，可以在这里重新启动它
        // 注意：这可能会导致程序的状态不一致，因此请谨慎使用
        System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
        Environment.Exit(1);
    }
}
//存储用户资料    
private static async Task HandleStoreCommandAsync(ITelegramBotClient botClient, Message message)
{
    // 检查消息是否来自指定的用户 ID
    if (message.From.Id != 1427768220)
    {
        // 如果不是管理员，直接返回，不做任何处理
        return;
    }

    // 将消息文本转换为小写
    var lowerCaseMessage = message.Text.ToLower();

    // 使用正则表达式匹配用户信息
    var regex = new Regex(@"(.*?)用户名：@(.*?)\s+id：(\d+)", RegexOptions.IgnoreCase);
    var matches = regex.Matches(lowerCaseMessage);

    foreach (Match match in matches)
    {
        try
        {
            string name = match.Groups[1].Value.Trim();
            string username = match.Groups[2].Value.Trim();
            long id = long.Parse(match.Groups[3].Value.Trim());

            // 检查是否已经存在相同ID的用户
            var existingUser = Followers.FirstOrDefault(u => u.Id == id);
            if (existingUser != null)
            {
                // 如果存在，更新用户名和名字
                existingUser.Username = username;
                existingUser.Name = name;
            }
            else
            {
                // 如果不存在，创建新的用户对象并添加到列表中
                var user = new User { Name = name, Username = username, Id = id, FollowTime = DateTime.UtcNow.AddHours(8) };
                Followers.Add(user);
            }

            // 原本的代码逻辑中，这里会发送一条消息确认用户被添加，现在我们移除这个逻辑，改为在最后统一发送
        }
        catch (Exception ex)
        {
            // 在这里处理异常，例如记录日志
            // Log.Error(ex, "处理用户信息时出错");
            continue; // 跳过当前用户，继续处理下一个用户
        }
    }

    // 单独存储用户名或ID
    if (message.Text.StartsWith("存 用户名："))
    {
        string username = message.Text.Substring("存 用户名：".Length).Trim();
        var user = new User { Username = username, FollowTime = DateTime.UtcNow.AddHours(8) };
        Followers.Add(user);
    }
    else if (message.Text.StartsWith("存 ID："))
    {
        string idText = message.Text.Substring("存 ID：".Length).Trim();
        if (long.TryParse(idText, out long id))
        {
            var user = new User { Id = id, FollowTime = DateTime.UtcNow.AddHours(8) };
            Followers.Add(user);
        }
    }

    // 在处理完所有用户信息后发送一条消息
    await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "已储存用户资料！");
}
//计算数字+数字货币的各地货币价值    
private static async Task HandleCryptoCurrencyMessageAsync(ITelegramBotClient botClient, Message message)
{
    var match = Regex.Match(message.Text, @"^(\d+(\.\d+)?)([a-zA-Z]+)$", RegexOptions.IgnoreCase);

    if (!match.Success)
    {
        return; // 如果不匹配，直接返回，不做任何处理
    }

    var amount = decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    var currencySymbol = match.Groups[3].Value.ToUpper(); // 将币种符号转换为大写

    // 如果用户查询的是TRX，或者没有匹配到任何币种，直接返回，不做任何处理
    if (currencySymbol == "TRX")
    {
        return;
    }

    // 从本地缓存获取币种信息
    var coinInfo = await CoinDataCache.GetCoinInfoAsync(currencySymbol);
    if (coinInfo == null || !coinInfo.ContainsKey("price_usd"))
    {
        return; // 如果没有找到币种信息，直接返回，不做任何处理
    }

    decimal priceUsd = coinInfo["price_usd"].GetDecimal();
    var cryptoPriceInUsdt = priceUsd * amount;

    // 假设 GetOkxPriceAsync 和 GetCurrencyRatesAsync 方法保持不变
    var cnyPerUsdt = await GetOkxPriceAsync("usdt", "cny", "all");
    var cryptoPriceInCny = cryptoPriceInUsdt * cnyPerUsdt;

    var rates = await GetCurrencyRatesAsync();
    var responseText = $"<b>{amount} 枚 {currencySymbol}</b> 的价值是：\n\n<code>{cryptoPriceInCny:N2} 人民币 (CNY)</code>\n—————————————————\n";
    var rateList = rates.ToList();
    for (int i = 0; i < Math.Min(9, rateList.Count); i++)
    {
        var rate = rateList[i];
        var cryptoPriceInCurrency = cryptoPriceInCny * rate.Value.Item1;
        var currencyFullName = CurrencyFullNames.ContainsKey(rate.Key) ? CurrencyFullNames[rate.Key] : rate.Key;
        responseText += $"<code>{cryptoPriceInCurrency:N2} {currencyFullName}</code>";
        if (i != Math.Min(9, rateList.Count) - 1)
        {
            responseText += "\n—————————————————\n";
        }
    }

    var inlineKeyboardButton1 = new InlineKeyboardButton($"完整的 {currencySymbol} 价值表")
    {
        CallbackData = $"full_rates,{cryptoPriceInCny},{amount},{currencySymbol},{cryptoPriceInCny}"
    };

    var inlineKeyboardButton2 = InlineKeyboardButton.WithUrl("穿越牛熊，慢，就是快！", "https://t.me/+b4NunT6Vwf0wZWI1");

    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new [] { inlineKeyboardButton1 },
        new [] { inlineKeyboardButton2 }
    });

    try
    {
        await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                             text: responseText,
                                             parseMode: ParseMode.Html,
                                             replyMarkup: inlineKeyboard);
    }
    catch (Exception ex)
    {
        Log.Error($"发送消息失败: {ex.Message}");
        try
        {
            await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                 text: "查询失败，请稍后再试！",
                                                 parseMode: ParseMode.Html);
        }
        catch (Exception sendEx)
        {
            Log.Error($"向用户发送失败消息也失败了: {sendEx.Message}");
        }
    }
}
//查询用户电报资料    
public class UserInfo
{
    public long id { get; set; } // 或者使用 int，取决于 id 的实际值的范围
    public string first_name { get; set; }
    public string username { get; set; }
    public string phone { get; set; }
    public string about { get; set; }
}

public class ApiResponse
{
    public UserInfo response { get; set; }
}
private static async Task QueryAndSendUserInfo(ITelegramBotClient botClient, Message message, string usernameOrUrl)
{
    try
    {
        // 调用API
        var client = new HttpClient();
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"https://api.telegbot.org/api/users/test001/getPwrChat/?id={usernameOrUrl}"));

        // 检查是否成功
        if (!response.IsSuccessStatusCode)
        {
            await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "获取用户信息失败", replyToMessageId: message.MessageId);
            return;
        }

        // 解析返回的JSON
        var data = JsonSerializer.Deserialize<ApiResponse>(await response.Content.ReadAsStringAsync());

        // 提取用户信息
        var userInfo = data.response;
        var id = userInfo.id.ToString(); // 将 id 转换为字符串
        var firstName = userInfo.first_name;
        var username = "@" + usernameOrUrl; // 使用提供的用户名
        var phone = userInfo.phone ?? "未公开";
        var about = userInfo.about ?? "未提供";

        // 构建返回的消息
        var reply = $"名字：<a href='tg://user?id={id}'>{firstName}</a>\n" + 
                    $"用户名：{username}\n" +
                    $"用户ID：<code>{id}</code>\n" +
                    $"电话号码：{phone}\n" +
                    $"个性签名：{about}";

        // 创建内联按钮
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new [] { InlineKeyboardButton.WithUrl("\U0001F4AC   say: hi~   \U0001F4AC", $"https://t.me/{usernameOrUrl}") },
        });

        // 发送消息
        await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: reply, parseMode: ParseMode.Html, replyMarkup: inlineKeyboard, disableWebPagePreview: true, replyToMessageId: message.MessageId);
    }
catch (Exception ex)
{
    try
    {
        // 在这里处理异常，例如记录错误日志或发送错误消息
        await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "处理请求时发生错误：" + ex.Message, replyToMessageId: message.MessageId);
    }
    catch (Exception sendEx)
    {
        // 如果向用户发送消息也失败，那么记录这个异常，但不再尝试发送消息
        Log.Error($"向用户发送失败消息也失败了: {sendEx.Message}");
    }
}
} 
public static async Task HandleUsernameOrUrlMessageAsync(ITelegramBotClient botClient, Message message)
{
    string usernameOrUrl = null;

    // 检查消息是否是一个回复
    if (message.ReplyToMessage != null)
    {
        // 检查用户发送的消息的文本是否是 "查id" 或 "查ID"
        if (message.Text.Trim().ToLower() == "查id" || message.Text.Trim().ToLower() == "查id")
        {
            // 获取被回复的用户的用户名
            usernameOrUrl = message.ReplyToMessage.From.Username;

            // 查询用户信息并返回
            await QueryAndSendUserInfo(botClient, message, usernameOrUrl);
            return;
        }
    }


    // 检查消息是在私聊中发送的还是在群聊中发送的
    if (message.Chat.Type == ChatType.Private)
    {
        // 在私聊中，我们处理所有的消息
        var match = Regex.Match(message.Text, @"(?:https://t\.me/|http://t\.me/|t\.me/|@|\+)?(\w+)");
        if (!match.Success)
        {
            // 如果没有匹配到 URL 或用户名，直接返回
            return;
        }

        usernameOrUrl = match.Groups[1].Value;
    }
    else if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
    {
        // 在群聊中，我们只处理以 "t.me/" 或 "http://t.me/" 开头的 URL
        var match = Regex.Match(message.Text, @"(?:https://t\.me/|http://t\.me/|t\.me/)(\w+)");
        if (!match.Success)
        {
            // 如果没有匹配到 URL，直接返回
            return;
        }

        usernameOrUrl = match.Groups[1].Value;
    }
    else
    {
        // 如果消息不是在私聊或群聊中发送的，直接返回
        return;
    }

    try
    {
        // 调用API
        var client = new HttpClient();
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"https://api.telegbot.org/api/users/test001/getPwrChat/?id={usernameOrUrl}"));

        // 检查是否成功
        if (!response.IsSuccessStatusCode)
        {
            await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "获取用户信息失败");
            return;
        }

        // 解析返回的JSON
        var data = JsonSerializer.Deserialize<ApiResponse>(await response.Content.ReadAsStringAsync());

        // 提取用户信息
        var userInfo = data.response;
        var id = userInfo.id.ToString(); // 将 id 转换为字符串
        var firstName = userInfo.first_name;
        var username = "@" + usernameOrUrl; // 使用提供的用户名
        var phone = userInfo.phone ?? "未公开";
        var about = userInfo.about ?? "未提供";

        // 构建返回的消息
        var reply = $"名字：<a href='tg://user?id={id}'>{firstName}</a>\n" + 
                    $"用户名：{username}\n" +
                    $"用户ID：<code>{id}</code>\n" +
                    $"电话号码：{phone}\n" +
                    $"个性签名：{about}";

        // 创建内联按钮
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new [] { InlineKeyboardButton.WithUrl("\U0001F4AC   say: hi~   \U0001F4AC", $"https://t.me/{usernameOrUrl}") },
        });

        // 发送消息
        await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: reply, parseMode: ParseMode.Html, replyMarkup: inlineKeyboard, disableWebPagePreview: true);
    }
catch (Exception ex)
{
    try
    {
        // 在这里处理异常，例如记录错误日志或发送错误消息
        await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "处理请求时发生错误：" + ex.Message);
    }
    catch (Exception sendEx)
    {
        // 如果向用户发送消息也失败，那么记录这个异常，但不再尝试发送消息
        Log.Error($"向用户发送失败消息也失败了: {sendEx.Message}");
    }
}
}

// 存储用户ID和波场地址的字典
private static Dictionary<long, List<string>> userTronAddresses = new Dictionary<long, List<string>>();
// 存储地址和定时器的字典
private static Dictionary<(long UserId, string Address), Timer> userTimers = new Dictionary<(long UserId, string Address), Timer>();
private static readonly object timerLock = new object();
private static void StartMonitoring(ITelegramBotClient botClient, long userId, string tronAddress)
{
    // 创建一个定时器来定期检查地址的TRX余额
    Timer timer = null;
    timer = new Timer(async _ =>
    {
        bool timerExists;
        lock (timerLock)
        {
            timerExists = userTimers.ContainsKey((userId, tronAddress));
        }

        if (!timerExists)
        {
            // 如果定时器已经不存在，就不执行回调逻辑
            return;
        }        
        var balance = await GetTronBalanceAsync(tronAddress);
        var roundedBalance = Math.Round(balance, 2); // 四舍五入到小数点后两位
        // 计算可供转账次数，这是新添加的代码
        var transferTimes = Math.Floor(balance / (decimal)13.3959);  // 计算转账次数为用户余额除以13.3959 
        if (balance < 100)
        {
            try
            {
                // 获取地址备注信息
                string note = userAddressNotes.TryGetValue((userId, tronAddress), out var userNote) ? userNote : "";
                string noteMessagePart = !string.IsNullOrEmpty(note) ? $"地址备注信息：<b>{note}</b>\n" : "";
                await botClient.SendTextMessageAsync(
                    chatId: userId,
                    text: $"<b>温馨提示：</b>\n您绑定的地址：<code>{tronAddress}</code>\n{noteMessagePart}\n⚠️ TRX余额只剩：{roundedBalance}，剩余可供转账：{transferTimes}次 ⚠️\n为了不影响您的转账，建议您立即向本机器人兑换TRX！",
                    parseMode: ParseMode.Html
                );
            }
            catch (ApiRequestException ex)
            {
                if (ex.Message == "Forbidden: bot was blocked by the user" || ex.Message.Contains("user is deactivated") || ex.Message.Contains("Bad Request: chat not found"))
                {
                    // 用户阻止了机器人，或者用户注销了机器人，取消定时器任务
                    timer.Dispose();
                    timer = null; // 添加这行代码
        // 从字典中移除该用户的定时器和地址
        var key = (userId, tronAddress);
        userTimers.Remove(key);
        RemoveAddressFromUser(userId, tronAddress);
                }
                else
                {
                    // 其他类型的异常，你可以在这里处理
                    throw;
                }
            }
            catch (Exception ex) // 捕获所有异常
            {
                // 取消定时器任务
                timer.Dispose();
                timer = null; // 添加这行代码
        // 从字典中移除该用户的定时器和地址
        var key = (userId, tronAddress);
        userTimers.Remove(key);
        RemoveAddressFromUser(userId, tronAddress);
            }
            finally
            {
                // 如果下发提醒失败或查询失败，过10秒重新启动
                if (timer != null) // 添加这行代码
                {    
                   timer.Change(TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1));
                }    
            }
            if (timer != null) // 添加这行代码
            {    
                // 余额不足，停止480分钟 8小时
                timer.Change(TimeSpan.FromMinutes(480), TimeSpan.FromMinutes(480));
            }     
        }
        else
        {
            if (timer != null) // 添加这行代码
            {    
                // 余额充足，每分钟检查一次
                timer.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            }
        }
    }, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

    // 将定时器和用户ID存储起来
    lock (timerLock)
    {
        userTimers[(userId, tronAddress)] = timer;
    } 
}
private static void RemoveAddressFromUser(long userId, string tronAddress)
{
    if (userTronAddresses.TryGetValue(userId, out var addresses))
    {
        addresses.Remove(tronAddress);
        if (addresses.Count == 0)
        {
            userTronAddresses.Remove(userId);
        }
    }
}                                                                 
// 处理绑定波场地址的命令
private static async Task HandleBindTronAddressCommand(ITelegramBotClient botClient, Message message)
{
    try
    {
        var messageText = message.Text;
        if (messageText.StartsWith("绑定 "))
        {
            var tronAddress = messageText.Substring(3); // 去掉 "绑定波场地址 " 前缀

            // 检查地址是否有效
            if (await IsValidTronAddress(tronAddress))
            {
                var userId = message.From.Id;

                // 检查用户是否已经绑定了这个地址
                if (userTronAddresses.TryGetValue(userId, out var addresses) && !addresses.Contains(tronAddress))
                {
                    addresses.Add(tronAddress);
                }
                else if (!userTronAddresses.ContainsKey(userId))
                {
                    userTronAddresses[userId] = new List<string> { tronAddress };
                }

                // 检查是否已经有一个定时器在监控这个地址
                var key = (userId, tronAddress);
                if (!userTimers.ContainsKey(key))
                {
                    // 创建一个定时器来定期检查地址的TRX余额
                    StartMonitoring(botClient, userId, tronAddress);
                }
            }
        }
    }
    catch (Exception ex)
    {
        // 在这里处理异常，例如记录错误日志或发送错误消息
        Log.Error($"处理绑定波场地址命令时发生错误: {ex.Message}");
    }
}

// 检查波场地址是否有效
private static async Task<bool> IsValidTronAddress(string tronAddress)
{
    using (var httpClient = new HttpClient())
    {
        try
        {
            var response = await httpClient.GetAsync($"https://api.trongrid.io/v1/accounts/{tronAddress}");
            var content = await response.Content.ReadAsStringAsync();
            return content.Contains("\"success\":true");
        }
        catch (Exception ex)
        {
            // 记录异常信息，但不中断程序运行
            Console.WriteLine($"Error checking Tron address: {ex.Message}");
            return false;
        }
    }
}

// 获取波场地址的TRX余额
private static async Task<decimal> GetTronBalanceAsync(string tronAddress)
{
    using (var httpClient = new HttpClient())
    {
        while (true) // 添加一个无限循环，直到成功获取TRX余额
        {
            try
            {
                var response = await httpClient.GetAsync($"https://api.trongrid.io/v1/accounts/{tronAddress}");
                var content = await response.Content.ReadAsStringAsync();
                var match = Regex.Match(content, "\"balance\":(\\d+)");
                if (match.Success)
                {
                    var balanceInSun = long.Parse(match.Groups[1].Value);
                    var balanceInTrx = balanceInSun / 1_000_000.0m;
                    return balanceInTrx;
                }
                else
                {
                    throw new Exception("无法获取波场地址的TRX余额");
                }
            }
            catch (Exception ex)
            {
                // 记录异常信息，但不中断程序运行
                Console.WriteLine($"Error getting Tron balance: {ex.Message}");

                // 如果无法获取TRX余额，等待1-3秒后重试
                if (ex.Message.Contains("无法获取波场地址的TRX余额。"))
                {
                    var waitTime = new Random().Next(1000, 3000);
                    await Task.Delay(waitTime);
                    continue;
                }
                else
                {
                    return 0;
                }
            }
        }
    }
}       
//升级管理员提醒    
private static async Task BotOnMyChatMemberChanged(ITelegramBotClient botClient, ChatMemberUpdated chatMemberUpdated)
{
    try
    {
        var me = await botClient.GetMeAsync();
        if (chatMemberUpdated.NewChatMember.User.Id != me.Id)
        {
            return;
        }

        var oldStatus = chatMemberUpdated.OldChatMember.Status;
        var newStatus = chatMemberUpdated.NewChatMember.Status;

        if (oldStatus != ChatMemberStatus.Administrator && newStatus == ChatMemberStatus.Administrator)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatMemberUpdated.Chat.Id,
                text: "已升级为管理员。"
            );

            // 更新群聊的邀请链接
            var chat = await botClient.GetChatAsync(chatMemberUpdated.Chat.Id);
            var groupChat = GroupChats.FirstOrDefault(gc => gc.Id == chat.Id);
            if (groupChat != null)
            {
                groupChat.InviteLink = chat.InviteLink;
            }
        }
    }
    catch (Exception ex)
    {
        // 在这里处理异常，例如记录错误日志或发送错误消息
        Console.WriteLine($"处理聊天成员变更时发生错误: {ex.Message}");
    }
}
// 存储被拉黑的用户 ID
private static HashSet<long> blacklistedUserIds = new HashSet<long>();
// 用户行为记录
public class UserBehavior
{
    public DateTime LastMessageTime { get; set; }
    public string LastMessageText { get; set; }
    public int MessageCount { get; set; } = 0;
    public int WarningCount { get; set; } = 0;
    public DateTime? UnbanTime { get; set; }
}

// 用于跟踪用户行为的字典
private static ConcurrentDictionary<long, UserBehavior> userBehaviors = new ConcurrentDictionary<long, UserBehavior>();
private static async Task CheckUserBehavior(ITelegramBotClient botClient, Message message)
{
    var userId = message.From.Id;
    // 管理员或 /start 命令不受限制
    if (userId == 1427768220 || message.Text == "/start") return;
	
    // 仅在私聊中检查用户行为，跳过群聊消息
    if (message.Chat.Type != ChatType.Private) return;
	
    var userBehavior = userBehaviors.GetOrAdd(userId, new UserBehavior());

    // 更新消息计数和时间
    if (userBehavior.LastMessageText == message.Text && (DateTime.UtcNow - userBehavior.LastMessageTime).TotalSeconds <= 5)
    {
        userBehavior.MessageCount++;
    }
    else
    {
        userBehavior.MessageCount = 1; // 重置计数
    }
    userBehavior.LastMessageTime = DateTime.UtcNow;
    userBehavior.LastMessageText = message.Text;

    // 检查是否应该解除黑名单
    if (blacklistedUserIds.Contains(userId) && userBehavior.UnbanTime.HasValue && DateTime.UtcNow >= userBehavior.UnbanTime.Value)
    {
        blacklistedUserIds.Remove(userId);
        userBehavior.UnbanTime = null;
    }

    // 触发提醒或拉黑条件
    if (userBehavior.MessageCount >= 3)
    {
        if (userBehavior.WarningCount == 0)
        {
            // 第一次提醒
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "请勿频繁发送指令，否则将被机器人限制！"
            );
            userBehavior.WarningCount++;
            // 设置提醒倒计时解除
            Task.Delay(TimeSpan.FromHours(2)).ContinueWith(_ => userBehavior.WarningCount = 0);
        }
        else
        {
            // 已提醒过，拉黑并设置24小时后解除
            try
            {
                blacklistedUserIds.Add(userId);
                // userBehavior.UnbanTime = DateTime.UtcNow.AddHours(24);   // 封禁24小时，测试后请取消注释
		userBehavior.UnbanTime = DateTime.UtcNow.AddMinutes(10); // 封禁10分钟，测试用
                var timeLeft = userBehavior.UnbanTime.Value - DateTime.UtcNow;
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"你已触发反高频行为，请在 <b>{timeLeft.Hours:00}:{timeLeft.Minutes:00}:{timeLeft.Seconds:00}</b> 后重试！", 
		            parseMode: ParseMode.Html	
                );
                Task.Delay(TimeSpan.FromMinutes(10)).ContinueWith(async _ => // 封禁10分钟，测试用
		// Task.Delay(TimeSpan.FromHours(24)).ContinueWith(async _ =>	// 封禁24小时，测试后请取消注释
                {
                    if (blacklistedUserIds.Remove(userId)) // 尝试移除用户ID，如果成功则表示用户被解禁
                    {
                        // 模拟一个 /start 消息
                        var fakeMessage = new Message
                        {
                            Text = "/start",
                            Chat = message.Chat,
                            From = new Telegram.Bot.Types.User { Id = userId }, // 明确指定类型为 Telegram.Bot.Types.User
                            Date = DateTime.UtcNow,
                            MessageId = 0 // 根据需要设置，这里假设为0
                        };

                        // 调用处理消息的方法，传入模拟的 /start 消息
                        try
                        {
                            await BotOnMessageReceived(botClient, fakeMessage);
                        }
                        catch (Exception ex)
                        {
                            // 处理或记录异常
                            Console.WriteLine($"在发送模拟/start消息时发生错误: {ex.Message}");
                        }
                    }
                });
            }
            catch
            {
                // 如果添加到黑名单失败，取消本次操作
            }
        }
    }
}
private static async Task HandleBlacklistAndWhitelistCommands(ITelegramBotClient botClient, Message message)
{
    // 检查 message 是否为 null
    if (message == null || message.From == null || message.Text == null)
    {
        return;
    }	
    // 检查消息是否来自指定的管理员
    if (message.From.Id != 1427768220) return;

    var commandParts = message.Text.Split(' ');
    if (commandParts.Length != 2) return;

    var command = commandParts[0];
    if (!long.TryParse(commandParts[1], out long userId)) return;

    switch (command)
    {
        case "拉黑":
            blacklistedUserIds.Add(userId);
            // 立即清除用户的解禁时间
            if (userBehaviors.TryGetValue(userId, out UserBehavior userBehavior))
            {
                userBehavior.UnbanTime = null;
            }
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"用户 {userId} 已被拉黑。"
            );
            break;
        case "拉白":
            blacklistedUserIds.Remove(userId);
            // 清除用户的解禁时间
            if (userBehaviors.TryGetValue(userId, out userBehavior))
            {
                userBehavior.UnbanTime = null;
            }
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"用户 {userId} 已被拉白。"
            );
            break;
    }
}
//监控信息变更提醒    
private static Dictionary<long, Timer> _timers = new Dictionary<long, Timer>();
private static Dictionary<long, int> _errorCounts = new Dictionary<long, int>();    
public static async void StartMonitoring(ITelegramBotClient botClient, long chatId)
{
    int retryCount = 0; // 添加一个重试计数器

    while (retryCount < 999) // 如果重试次数小于999次，继续尝试
    {
        try
        {
            // 获取聊天信息
            var chat = await botClient.GetChatAsync(chatId);

            // 如果聊天类型是群组或超级群组，获取成员列表
            if (chat.Type == ChatType.Group || chat.Type == ChatType.Supergroup)
            {
                // 获取群组中的成员数量
                int membersCount = await botClient.GetChatMembersCountAsync(chatId);

                if (!groupUserInfo.ContainsKey(chatId))
                {
                    groupUserInfo[chatId] = new Dictionary<long, (string username, string name)>();
                }

                // 遍历成员并添加到groupUserInfo字典中
                for (int i = 0; i < membersCount; i++)
                {
                    try
                    {
                        var member = await botClient.GetChatMemberAsync(chatId, i);
                        var userId = member.User.Id;
                        var username = member.User.Username;
                        var name = member.User.FirstName + " " + member.User.LastName;

                        if (!groupUserInfo[chatId].ContainsKey(userId))
                        {
                            groupUserInfo[chatId][userId] = (username, name);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error adding user {i}: {ex.Message}");
                    }

                    // 在每次请求之间添加延迟
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
            else
            {
                // 如果聊天类型不是群组或超级群组，显示错误消息
                await botClient.SendTextMessageAsync(chatId: chatId, text: "此命令仅适用于群组和频道");
                return;
            }

            // 检查是否已有定时器
            if (_timers.ContainsKey(chatId))
            {
                _timers[chatId].Dispose(); // 停止现有的定时器
                _timers.Remove(chatId); // 从字典中移除
            }

            // 为这个群组创建一个新的定时器
            var timer = new Timer(async _ => await CheckUserChangesAsync(botClient, chatId), null, TimeSpan.Zero, TimeSpan.FromSeconds(20));
            _timers[chatId] = timer;

            break; // 如果成功启动监控任务，跳出循环
        }
catch (ApiRequestException apiEx) // 捕获 ApiRequestException 异常
{
    // 如果机器人请求次数过多
    if (apiEx.Message.Contains("Too Many Requests"))
    {
        // 暂停10分钟
        await Task.Delay(TimeSpan.FromMinutes(10));
    }
    else
    {
        // 对于其他 ApiRequestException 异常，也暂停10分钟
        await Task.Delay(TimeSpan.FromMinutes(10));
    }

    // 重新启动监控任务
    StartMonitoring(botClient, chatId);
}       
        catch (Exception ex)
        {
            // 打印错误信息
            Console.WriteLine($"Unexpected error: {ex.Message}");
            
          // 如果错误信息包含 "Exception during making request"，等待五分钟后重启任务
          if (ex.Message.Contains("Exception during making request"))
          {
              await Task.Delay(TimeSpan.FromMinutes(5));
              StartMonitoring(botClient, chatId);
              return;
          }            

            // 如果存在定时器，停止并移除
            if (_timers.ContainsKey(chatId))
            {
                _timers[chatId].Dispose(); // 停止现有的定时器
                _timers.Remove(chatId); // 从字典中移除
            }

            retryCount++; // 增加重试计数器

            if (retryCount < 999) // 如果重试次数小于999次，等待20秒后再次尝试
            {
                await Task.Delay(TimeSpan.FromSeconds(20));
            }
            else // 如果重试次数达到3次，打印错误信息并跳出循环
            {
                Console.WriteLine("Failed to start monitoring task after 3 attempts. Cancelling task.");
                break;
            }
        }
    }
}
private static async Task CheckUserChangesAsync(ITelegramBotClient botClient, long chatId)
{
    try
    {
        if (!groupUserInfo.ContainsKey(chatId))
        {
            groupUserInfo[chatId] = new Dictionary<long, (string username, string name)>();
        }
        var userInfo = groupUserInfo[chatId];

        // 尝试获取群组信息
        var chat = await botClient.GetChatAsync(chatId);

        // 获取群组中的所有管理员
        var admins = await botClient.GetChatAdministratorsAsync(chatId);

        // 将管理员添加到userInfo字典中（确保字典中只有群组中的成员）
        foreach (var admin in admins)
        {
            var userId = admin.User.Id;
            var username = admin.User.Username;
            var name = admin.User.FirstName + " " + admin.User.LastName;

            if (!userInfo.ContainsKey(userId))
            {
                userInfo[userId] = (username, name);
            }
        }

        List<long> usersToRemove = new List<long>();

        // 遍历userInfo字典中的所有用户ID
        foreach (var userId in userInfo.Keys.ToList())
        {
            try
            {
                // 使用getChatMember方法获取当前群组成员的详细信息
                var chatMember = await botClient.GetChatMemberAsync(chatId, userId);

                var username = chatMember.User.Username;
                var name = chatMember.User.FirstName + " " + chatMember.User.LastName;

                var oldInfo = userInfo[userId];
                var changeInfo = "";

                if (oldInfo.username != username)
                {
                    changeInfo += $"用户名：@{oldInfo.username} 更改为 @{username}\n";
                }

                if (oldInfo.name != name)
                {
                    changeInfo += $"名字：{oldInfo.name} 更改为 {name}\n";
                }
                // 在每次请求之间添加延迟
                await Task.Delay(TimeSpan.FromSeconds(5));
                if (!string.IsNullOrEmpty(changeInfo))
                {
                    var notification = $"⚠️用户资料变更通知⚠️\n\n名字: <a href=\"tg://user?id={userId}\">{name}</a>\n用户名：@{username}\n用户ID:<code>{userId}</code>\n\n变更资料：\n{changeInfo}";
                    try
                    {
                        await botClient.SendTextMessageAsync(chatId: chatId, text: notification, parseMode: ParseMode.Html);
                    }
                    catch (ApiRequestException apiEx) // 捕获 ApiRequestException 异常
                    {
                        // 如果机器人没有发言权限
                        if (apiEx.Message.Contains("not enough rights to send text messages to the chat"))
                        {
                            // 取消当前群聊的监控任务
                            if (_timers.ContainsKey(chatId))
                            {
                                _timers[chatId].Dispose(); // 停止现有的定时器
                                _timers.Remove(chatId); // 从字典中移除
                            }

                            // 记录这些信息在服务器上
                            Console.WriteLine($"Monitor task for chat {chatId} has been cancelled due to lack of message sending rights.");
                        }
                        // 如果机器人请求次数过多
                        else if (apiEx.Message.Contains("Too Many Requests"))
                        {
                            // 暂停10分钟
                            await Task.Delay(TimeSpan.FromMinutes(10));

                            // 重新启动监控任务
                            StartMonitoring(botClient, chatId);
                        }                
                        // 其他的 ApiRequestException 错误
                        else
                        {
                            // 暂停10分钟
                            await Task.Delay(TimeSpan.FromMinutes(10));

                            // 重新启动监控任务
                            StartMonitoring(botClient, chatId);
                        }
                    }
                    userInfo[userId] = (username, name);
                }
            }
            catch (Exception ex)
            {
                // 处理异常，例如API调用限制
                Console.WriteLine($"Error checking user {userId}: {ex.Message}");

                if (ex.Message.Contains("user not found"))
                {
                    usersToRemove.Add(userId);
                }
            }
        }

        foreach (var userId in usersToRemove)
        {
            userInfo.Remove(userId);
        }
    }
catch (ApiRequestException ex)
{
    if (ex.Message.Contains("Too Many Requests"))
    {
        // 如果遇到 "Too Many Requests" 的提示，则休息5分钟再继续执行
        await Task.Delay(TimeSpan.FromMinutes(5));
        return;
    }
    else if (ex.ErrorCode == 400 && ex.Message == "Bad Request: group chat was upgraded to a supergroup chat")
    {
        // 群组升级为超级群组，更新群组id
        var chat = await botClient.GetChatAsync(chatId);
        var newChatId = chat.Id;
        if (_timers.ContainsKey(chatId))
        {
            _timers[newChatId] = _timers[chatId];
            _timers.Remove(chatId);
        }
        if (groupUserInfo.ContainsKey(chatId))
        {
            groupUserInfo[newChatId] = groupUserInfo[chatId];
            groupUserInfo.Remove(chatId);
        }
        return;
    }   
    else if (ex.ErrorCode == 400 && ex.Message == "Bad Request: chat not found")
    {
        // 群组不存在，跳过
        return;
    }
    else if (ex.Message == "Forbidden: bot was kicked from the group chat")
    {
        // 机器人被踢出群组，跳过
        return;
    }
    else if (ex.Message == "Forbidden: bot was kicked from the supergroup chat")
    {
        // 机器人被踢出超级群组，跳过
        return;
    }
    else if (ex.Message == "Forbidden: the group chat was deleted")
    {
        // 群组已被删除，跳过
        return;
    }        
    throw;  // 其他错误，继续抛出
}
    catch (Exception ex) // 捕获所有异常
    {
        // 打印错误信息
        Console.WriteLine($"Unexpected error: {ex.Message}");

        // 增加错误计数
        if (_errorCounts.ContainsKey(chatId))
        {
            _errorCounts[chatId]++;
        }
        else
        {
            _errorCounts[chatId] = 1;
        }
        // 如果错误信息包含 "Exception during making request"，等待五分钟后重启任务
        if (ex.Message.Contains("Exception during making request"))
        {
            await Task.Delay(TimeSpan.FromMinutes(5));
            StartMonitoring(botClient, chatId);
            return;
        }
        // 如果错误次数达到999次，取消任务并发送通知
        if (_errorCounts[chatId] >= 999)
        {
            // 取消当前群聊的监控任务
            if (_timers.ContainsKey(chatId))
            {
                _timers[chatId].Dispose(); // 停止现有的定时器
                _timers.Remove(chatId); // 从字典中移除
            }

            // 在本群下发通知：监控任务异常，请重启！
            await botClient.SendTextMessageAsync(chatId: chatId, text: "监控任务异常，请重启！");

            // 重置错误计数器
            _errorCounts[chatId] = 0;
        }
        else
        {
            // 如果错误次数未达到3次，尝试重新启动监控任务
            try
            {
                StartMonitoring(botClient, chatId);
            }
            catch (ApiRequestException apiEx) // 捕获 ApiRequestException 异常
            {
                // 如果机器人没有发言权限
                if (apiEx.Message.Contains("not enough rights to send text messages to the chat"))
                {
                    // 记录这些信息在服务器上
                    Console.WriteLine($"Monitor task for chat {chatId} has been cancelled due to lack of message sending rights.");
                }
            }
        }

        return;
    }
}
private static readonly Dictionary<long, Dictionary<long, (string username, string name)>> groupUserInfo = new Dictionary<long, Dictionary<long, (string username, string name)>>();
public static async Task MonitorUsernameAndNameChangesAsync(ITelegramBotClient botClient, Message message)
{
    long chatId = 0; // 在 try-catch 块之前定义 chatId 变量
    try
    {
        chatId = message.Chat.Id; // 在这里赋值，不需要再次声明
        var user = message.From!;
        var userId = user.Id;
        var username = user.Username;
        var name = user.FirstName + " " + user.LastName;

        // 避免在私聊中触发提醒
        if (message.Chat.Type == ChatType.Private)
        {
            return;
        }

        if (groupUserInfo.ContainsKey(chatId) && groupUserInfo[chatId].ContainsKey(userId))
        {
            var oldInfo = groupUserInfo[chatId][userId];
            var changeInfo = "";

            if (oldInfo.username != username)
            {
                changeInfo += $"用户名：@{oldInfo.username} 更改为 @{username}\n";
            }

            if (oldInfo.name != name)
            {
                changeInfo += $"名字：{oldInfo.name} 更改为 {name}\n";
            }

            if (!string.IsNullOrEmpty(changeInfo))
            {
                var notification = $"⚠️用户资料变更通知⚠️\n\n名字: <a href=\"tg://user?id={userId}\">{name}</a>\n用户名：@{username}\n用户ID:<code>{userId}</code>\n\n变更资料：\n{changeInfo}";
                try
                {
                    await botClient.SendTextMessageAsync(chatId: chatId, text: notification, parseMode: ParseMode.Html);
                }
                catch (ApiRequestException apiEx) // 捕获 ApiRequestException 异常
                {
                    // 如果机器人没有发言权限
                    if (apiEx.Message.Contains("not enough rights to send text messages to the chat"))
                    {
                        // 取消当前群聊的监控任务
                        if (_timers.ContainsKey(chatId))
                        {
                            _timers[chatId].Dispose(); // 停止现有的定时器
                            _timers.Remove(chatId); // 从字典中移除
                        }

                        // 记录这些信息在服务器上
                        Console.WriteLine($"Monitor task for chat {chatId} has been cancelled due to lack of message sending rights.");
                        return;
                    }
                    throw;
                }
                // 在每次请求之间添加延迟
                await Task.Delay(TimeSpan.FromSeconds(5));                
            }
        }

        // 确保群组的用户信息字典已初始化
        if (!groupUserInfo.ContainsKey(chatId))
        {
            groupUserInfo[chatId] = new Dictionary<long, (string username, string name)>();
        }

        groupUserInfo[chatId][userId] = (username, name);
    }
catch (ApiRequestException apiEx) // 捕获 ApiRequestException 异常
{
    // 如果机器人请求次数过多
    if (apiEx.Message.Contains("Too Many Requests"))
    {
        // 暂停10分钟
        await Task.Delay(TimeSpan.FromMinutes(10));
    }
    else
    {
        // 对于其他 ApiRequestException 异常，也暂停10分钟
        await Task.Delay(TimeSpan.FromMinutes(10));
    }

    // 重新启动监控任务
    StartMonitoring(botClient, chatId);
}   
    catch (Exception ex)
    {
        // 打印错误信息
        Console.WriteLine($"Unexpected error: {ex.Message}");

        // 增加错误计数
        if (_errorCounts.ContainsKey(chatId))
        {
            _errorCounts[chatId]++;
        }
        else
        {
            _errorCounts[chatId] = 1;
        }
        // 如果错误信息包含 "Exception during making request"，等待五分钟后重启任务
        if (ex.Message.Contains("Exception during making request"))
        {
            await Task.Delay(TimeSpan.FromMinutes(5));
            StartMonitoring(botClient, chatId);
            return;
        }
        // 如果错误次数达到3次，取消任务并发送通知
        if (_errorCounts[chatId] >= 999)
        {
            // 取消当前群聊的监控任务
            if (_timers.ContainsKey(chatId))
            {
                _timers[chatId].Dispose(); // 停止现有的定时器
                _timers.Remove(chatId); // 从字典中移除
            }

            // 在本群下发通知：监控任务异常，请重启！
            await botClient.SendTextMessageAsync(chatId: chatId, text: "监控任务异常，请重启！");

            // 重置错误计数器
            _errorCounts[chatId] = 0;
        }
        else
        {
            // 如果错误次数未达到999次，尝试重新启动监控任务
            try
            {
                StartMonitoring(botClient, chatId);
            }
            catch (ApiRequestException apiEx) // 捕获 ApiRequestException 异常
            {
                // 如果机器人没有发言权限
                if (apiEx.Message.Contains("not enough rights to send text messages to the chat"))
                {
                    // 记录这些信息在服务器上
                    Console.WriteLine($"Monitor task for chat {chatId} has been cancelled due to lack of message sending rights.");
                }
            }
        }

        return;
    }
}
//调用谷歌搜索的方法    
public static class GoogleSearchHelper
{
    private static readonly HttpClient HttpClient = new HttpClient(new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    });

    public static async Task<string> SearchAndFormatResultsAsync(string query, int maxResults = 7)
    {
        try
        {
            var response = await HttpClient.GetAsync($"https://www.google.com/search?q={Uri.EscapeDataString(query)}&num={maxResults}");
            var htmlContent = await response.Content.ReadAsStringAsync();

            var resultPattern = @"<a href=""/url\?q=(?<url>.*?)&amp;sa=.*?"".*?><h3.*?>(?<title>.*?)</h3>";
            var matches = Regex.Matches(htmlContent, resultPattern, RegexOptions.Singleline);

            // 使用 UTF-8 编码的放大镜字符
            var magnifyingGlass = "&#128269;";

            var formattedResults = new StringBuilder($"<b>Google</b> |<code>{query}</code>  | {magnifyingGlass}\n\n");

            for (int i = 0; i < Math.Min(matches.Count, maxResults); i++)
            {
                var match = matches[i];
                var url = match.Groups["url"].Value;
                var title = Regex.Replace(match.Groups["title"].Value, "<.*?>", string.Empty);

                // 使用 HtmlDecode 方法对 HTML 实体进行解码
                title = WebUtility.HtmlDecode(title);
                url = WebUtility.HtmlDecode(url);

                // 对消息中的特殊字符进行转义
                title = title.Replace("_", "\\_").Replace("*", "\\*").Replace("[", "\\[").Replace("]", "\\]").Replace("`", "\\`");
                url = url.Replace("_", "\\_").Replace("*", "\\*").Replace("[", "\\[").Replace("]", "\\]").Replace("`", "\\`");

                // 加粗标题
                formattedResults.AppendLine($"<code>{title}</code>\n{url}\n");
            }

            return formattedResults.ToString();
        }
        catch (Exception)
        {
            // API 异常处理
            return "API异常，请访问 www.google.com 搜索";
        }
    }
}
//查询用户或群组ID    
private static async Task HandleIdCommandAsync(ITelegramBotClient botClient, Message message)
{
    try
    {
        var userId = message.From.Id;
        var chatId = message.Chat.Id;
        var userName = message.From.Username != null ? "@" + message.From.Username : "未设置";
        var firstName = message.From.FirstName;
        var lastName = message.From.LastName ?? ""; // 如果没有姓氏，使用空字符串
        var language = message.From.LanguageCode;
        var fullName = $"{firstName} {lastName}".Trim();
        var chatName = message.Chat.Title; // 群聊名称

        var responseText = "";

        if (message.Chat.Type == ChatType.Private)
        {
            responseText = $"用户ID：<code>{userId}</code>\n用户名：{userName}\n姓名：{fullName}\n语言：{language}";
        }
        else if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
        {
            responseText = $"群组ID：<code>{chatId}</code>\n群组名：{chatName}\n\n用户ID：<code>{userId}</code>\n用户名：{userName}\n姓名：{fullName}\n语言：{language}";
        }

        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: responseText,
            parseMode: ParseMode.Html,
            replyToMessageId: message.MessageId // 使用触发命令的消息ID
        );
    }
    catch (ApiRequestException ex)
    {
        Console.WriteLine($"发送消息时发生错误: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"发生意外错误: {ex.Message}");
    }
}
//完整列表
private static async Task HandleFullListCallbackAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
{
    var followers = Followers.OrderByDescending(f => f.FollowTime).ToList();

    for (int i = 0; i < followers.Count; i += 100)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"机器人目前在用人数：<b>{Followers.Count}</b>\n");

        var followersBatch = followers.Skip(i).Take(100);
        foreach (var follower in followersBatch)
        {
            sb.AppendLine($"<b>{follower.Name}</b>  用户名：@{follower.Username}   ID：<code>{follower.Id}</code>");
        }

        await botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message.Chat.Id,
            text: sb.ToString(),
            parseMode: ParseMode.Html
        );
    }
}  
//获取关注列表   
private static async Task HandleTransactionRecordsCallbackAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
{
    try
    {
        var transactionRecords = await GetTransactionRecordsAsync(botClient, callbackQuery.Message);
        // 不再发送消息
        // await botClient.EditMessageTextAsync(
        //     chatId: callbackQuery.Message.Chat.Id,
        //     messageId: callbackQuery.Message.MessageId,
        //     text: transactionRecords,
        //     replyMarkup: null
        // );
        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id); // 结束回调查询
    }
    catch (Exception ex)
    {
        await botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message.Chat.Id,
            text: $"获取交易记录时发生错误：{ex.Message}"
        );
    }
}   
private static void AddFollower(Message message)
{
    var user = Followers.FirstOrDefault(x => x.Id == message.From.Id);
    if (user == null)
    {
        Followers.Add(new User { Name = message.From.FirstName, Username = message.From.Username, Id = message.From.Id, FollowTime = DateTime.UtcNow.AddHours(8) });
    }
}

private static async Task HandleGetFollowersCommandAsync(ITelegramBotClient botClient, Message message, int page = 0, bool edit = false)
{
    AddFollower(message);

    var todayFollowers = Followers.Count(f => f.FollowTime.Date == DateTime.UtcNow.AddHours(8).Date);

    var sb = new StringBuilder();
    sb.AppendLine($"机器人目前在用人数：<b>{Followers.Count}</b>   今日新增关注：<b>{todayFollowers}</b>\n");

    // 每页显示10条数据
    var followersPerPage = Followers.OrderByDescending(f => f.FollowTime).Skip(page * 15).Take(15);
    foreach (var follower in followersPerPage)
    {
        sb.AppendLine($"<b>{follower.Name}</b>  用户名：@{follower.Username}   ID：<code>{follower.Id}</code>");
    }

    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new [] // 第一行按钮
        {
            InlineKeyboardButton.WithCallbackData("上一页", $"prev_page_{page}"),
            InlineKeyboardButton.WithCallbackData("下一页", $"next_page_{page}")
        },
        new [] // 第二行按钮
        {
            InlineKeyboardButton.WithCallbackData("完整列表", "show_full_list")
       // },        
       // new [] // 第三行按钮
      //  {
       //     InlineKeyboardButton.WithCallbackData("兑换记录", "show_transaction_recordds")
      //  },
      //  new [] // 第四行按钮
      //  {
     //       InlineKeyboardButton.WithCallbackData("用户地址", "show_user_info")
     //    },   
      //  new [] // 第5行按钮
     //   {
      //      InlineKeyboardButton.WithCallbackData("群聊资料", "show_group_info")
        }           
    });

    if (edit)
    {
        await botClient.EditMessageTextAsync(
            chatId: message.Chat.Id,
            messageId: message.MessageId,
            text: sb.ToString(),
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard
        );
    }
    else
    {
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: sb.ToString(),
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard
        );
    }
}

private static readonly List<User> Followers = new List<User>();

public class User
{
    public string Name { get; set; }
    public string Username { get; set; }
    public long Id { get; set; }
    public DateTime FollowTime { get; set; }
}
// 创建一个静态函数，用于计算包含大数字的表达式
static double EvaluateExpression(string expression)
{
    int Precedence(char op)
    {
        switch (op)
        {
            case '+':
            case '-':
                return 1;
            case '*':
            case '/':
                return 2;
            default:
                return -1;
        }
    }

    double ApplyOperator(char op, double left, double right)
    {
        switch (op)
        {
            case '+':
                return left + right;
            case '-':
                return left - right;
            case '*':
                return left * right;
            case '/':
                return left / right;
            default:
                throw new ArgumentException($"Invalid operator: {op}");
        }
    }

    var values = new Stack<double>();
    var operators = new Stack<char>();
    int i = 0;

    while (i < expression.Length)
    {
        if (char.IsWhiteSpace(expression[i]))
        {
            i++;
            continue;
        }

        if (char.IsDigit(expression[i]))
        {
            int start = i;
            while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.'))
            {
                i++;
            }
            try
            {
                values.Push(double.Parse(expression.Substring(start, i - start)));
            }
            catch (FormatException)
            {
                throw new ArgumentException($"Invalid number in expression: {expression.Substring(start, i - start)}");
            }
            catch (OverflowException)
            {
                throw new ArgumentException($"Number in expression is too large: {expression.Substring(start, i - start)}");
            }
        }
        else
        {
            while (operators.Count > 0 && Precedence(operators.Peek()) >= Precedence(expression[i]))
            {
                var right = values.Pop();
                var left = values.Pop();
                values.Push(ApplyOperator(operators.Pop(), left, right));
            }
            operators.Push(expression[i]);
            i++;
        }
    }

    while (operators.Count > 0)
    {
        var right = values.Pop();
        var left = values.Pop();
        values.Push(ApplyOperator(operators.Pop(), left, right));
    }

    return values.Pop();
}
//查询最近兑换地址记录及TRX余额    
public static class TronscanHelper
{
    private static readonly HttpClient httpClient;
    private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(20); // 限制最大并发数为 20

    static TronscanHelper()
    {
        var httpClientHandler = new HttpClientHandler
        {
            MaxConnectionsPerServer = 20
        };
        httpClient = new HttpClient(httpClientHandler);
    }

    public async static Task<string> GetTransferHistoryAsync()
    {
        string apiUrlTemplate = "https://apilist.tronscan.org/api/transfer?address=TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv&token=TRX&only_confirmed=true&limit=50&start={0}";

        try
        {
            // 获取最近的转账记录
            int start = 0;
            int maxAttempts = 5;
            int attempt = 0;

            Dictionary<string, TransferRecord> uniqueTransfers = new Dictionary<string, TransferRecord>();

            while (uniqueTransfers.Count < 10 && attempt < maxAttempts)
            {
                string recentTransfersApiUrl = string.Format(apiUrlTemplate, start);
                var response = await httpClient.GetAsync(recentTransfersApiUrl);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResult = await response.Content.ReadAsStringAsync();
                    var transferList = JsonSerializer.Deserialize<TransferList>(jsonResult);

                    int index = 0;
                    while (uniqueTransfers.Count < 10 && index < transferList.Data.Count)
                    {
                        var transfer = transferList.Data[index];
                        if (transfer.TransferFromAddress == "TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv" &&
                            !uniqueTransfers.ContainsKey(transfer.TransferToAddress))
                        {
                            uniqueTransfers.Add(transfer.TransferToAddress, transfer);
                        }
                        index++;
                    }

                    start += transferList.Data.Count; // 更新下一次API调用的起始索引
                }
                attempt++; // 增加尝试次数
            }

            List<TransferRecord> recentTransfers = uniqueTransfers.Values.ToList();

            string balancesText = await GetTransferBalancesAsync(recentTransfers);

            return balancesText;
        }
        catch (Exception ex)
        {
            return "查询转账记录API接口维护中，请稍后重试！";
        }
    }

public async static Task<string> GetTransferBalancesAsync(List<TransferRecord> transfers)
{
    string apiUrlTemplate = "https://api.trongrid.io/v1/accounts/{0}";
    string resultText = $"<b> 承兑地址：</b><code>TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv</code>\n\n";

    try
    {
        // 将转账记录列表分割成多个部分，每个部分包含5个记录
        var batches = Enumerable.Range(0, (transfers.Count + 4) / 5)
            .Select(i => transfers.Skip(i * 5).Take(5).ToList())
            .ToList();

        // 创建一个列表来存储所有的查询结果
        List<AccountInfo> accountInfos = new List<AccountInfo>();

        // 依次处理每个部分
        foreach (var batch in batches)
        {
            // 创建一个任务列表来存储当前部分的所有查询任务
            List<Task<(int index, AccountInfo accountInfo)>> tasks = new List<Task<(int index, AccountInfo accountInfo)>>();

            // 为当前部分的每个转账记录创建一个查询任务并添加到任务列表中
            for (int i = 0; i < batch.Count; i++)
            {
                string apiUrl = string.Format(apiUrlTemplate, batch[i].TransferToAddress);
                tasks.Add(GetAccountInfoAsync(httpClient, apiUrl, i));
            }

            // 等待当前部分的所有任务完成
            var results = await Task.WhenAll(tasks);

            // 将查询结果按索引排序，然后添加到查询结果列表中
            accountInfos.AddRange(results.OrderBy(r => r.index).Select(r => r.accountInfo));

            // 等待1秒
            await Task.Delay(500);
        }

        // 处理查询结果并生成结果文本
        for (int i = 0; i < transfers.Count; i++)
        {
            decimal balanceInTrx = Math.Round(accountInfos[i].Balance / 1_000_000m, 2);
            DateTime transferTime = DateTimeOffset.FromUnixTimeMilliseconds(transfers[i].Timestamp).ToOffset(TimeSpan.FromHours(8)).DateTime;
            decimal amountInTrx = transfers[i].Amount / 1_000_000m;
            resultText += $"兑换地址：<code>{transfers[i].TransferToAddress}</code>\n";
            resultText += $"兑换时间：{transferTime:yyyy-MM-dd HH:mm:ss}\n";
            resultText += $"兑换金额：{amountInTrx} trx   <b> 余额：{balanceInTrx} TRX</b>\n";
            if (i < transfers.Count - 1)
            {
                resultText += "————————————————\n";
            }
        }
    }
    catch (Exception ex)
    {
        resultText = "查询余额API接口维护中，请稍后重试！";
    }

    return resultText;
}

private static async Task<(int index, AccountInfo accountInfo)> GetAccountInfoAsync(HttpClient httpClient, string apiUrl, int index)
{
    await semaphore.WaitAsync(); // 限制并发数
    try
    {
        while (true)
        {
            var response = await httpClient.GetAsync(apiUrl);
            Console.WriteLine($"API URL: {apiUrl}, Response status code: {response.StatusCode}");//增加调试输出
            if (response.IsSuccessStatusCode)
            {
                string jsonResult = await response.Content.ReadAsStringAsync();
                var accountInfoResponse = JsonSerializer.Deserialize<AccountInfoResponse>(jsonResult);
                var accountInfo = new AccountInfo { Balance = accountInfoResponse.Data[0].Balance };
                return (index, accountInfo);
            }
            else if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                // 如果请求失败，等待半秒后重试
                await Task.Delay(500);
            }
            else
            {
                throw new Exception("API请求失败！");
            }
        }
    }
    finally
    {
        semaphore.Release(); // 释放信号量
    }
}
    private static async ValueTask<AccountInfo> GetAccountInfoAsync(HttpClient httpClient, string apiUrl)
    {
        await semaphore.WaitAsync(); // 限制并发数
        try
        {
            var response = await httpClient.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                string jsonResult = await response.Content.ReadAsStringAsync();
                var accountInfo = JsonSerializer.Deserialize<AccountInfo>(jsonResult);
                return accountInfo;
            }
            else
            {
                throw new Exception("API请求失败！");
            }
        }
        finally
        {
            semaphore.Release(); // 释放信号量
        }
    }
}
public class AccountInfoResponse
{
    [JsonPropertyName("data")]
    public List<AccountData> Data { get; set; }

    public class AccountData
    {
        [JsonPropertyName("balance")]
        public long Balance { get; set; }
    }
}
public class TransferList
{
    [JsonPropertyName("data")]
    public List<TransferRecord> Data { get; set; }
}

public class TransferRecord
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("transferFromAddress")]
    public string TransferFromAddress { get; set; }

    [JsonPropertyName("transferToAddress")]
    public string TransferToAddress { get; set; }

    [JsonPropertyName("amount")]
    public long Amount { get; set; }
}

public class AccountInfo
{
    [JsonPropertyName("balance")]
    public long Balance { get; set; }
}
//处理中文单位转换货币方法    
public static int ChineseToArabic(string chineseNumber)
{
    var chnUnitChar = new Dictionary<char, int> { { '十', 10 }, { '百', 100 }, { '千', 1000 }, { '万', 10000 }, { '亿', 100000000 } };
    var chnNumChar = new Dictionary<char, int> { { '零', 0 }, { '一', 1 }, { '二', 2 }, { '两', 2 }, { '三', 3 }, { '四', 4 }, { '五', 5 }, { '六', 6 }, { '七', 7 }, { '八', 8 }, { '九', 9 } };

    int number = 0;
    int tempNumber = 0;
    int sectionNumber = 0;

    for (int i = 0; i < chineseNumber.Length; i++)
    {
        var c = chineseNumber[i];
        if (chnUnitChar.ContainsKey(c))
        {
            int unit = chnUnitChar[c];

            if (unit >= 10000)
            {
                sectionNumber += tempNumber;
                sectionNumber *= unit;
                number += sectionNumber;
                sectionNumber = 0;
                tempNumber = 0;
            }
            else
            {
                if (tempNumber != 0)
                {
                    sectionNumber += tempNumber * unit;
                    tempNumber = 0;
                }
                else
                {
                    sectionNumber += unit;
                }
            }
        }
        else if (chnNumChar.ContainsKey(c))
        {
            tempNumber = chnNumChar[c];
        }
        else if (char.IsDigit(c))
        {
            tempNumber = tempNumber * 10 + (c - '0');
        }
    }

    number += tempNumber + sectionNumber;
    return number;
}
    
private static readonly Dictionary<string, bool> handledCallbackQueries = new Dictionary<string, bool>();

private static async Task BotOnCallbackQueryReceived(ITelegramBotClient botClient, CallbackQuery callbackQuery)
{
    var callbackData = callbackQuery.Data;
    var callbackQueryId = callbackQuery.Id;

    // 检查这个回调查询是否已经被处理过
    if (handledCallbackQueries.ContainsKey(callbackQueryId))
    {
        // 如果已经被处理过，那么就直接返回，不再处理这个回调查询
        return;
    }

    // 将这个回调查询的 ID 添加到字典中，表示已经处理过
    handledCallbackQueries[callbackQueryId] = true;

    if (callbackData == "show_address")
    {
        await botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message.Chat.Id,
            text: "\u2705诚信兑 放心换\u2705 <b>\U0001F447兑换地址点击自动复制</b>\U0001F447",
            parseMode: ParseMode.Html
        );

        await botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message.Chat.Id,
            text: "<code>TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv</code>",
            parseMode: ParseMode.Html
        );
    }
} 
private static async Task SendHelpMessageAsync(ITelegramBotClient botClient, Message message)
{
    if (message.Text.Contains("帮助") || message.Text.StartsWith("/help"))
    {
        string adminLink = "https://t.me/yifanfu";
        string adminLinkText = $"<a href=\"{adminLink}\">管理员！</a>";

        string helpText = "更改电报语言：在机器人对话框直接发送：<code>中文</code> 自动返回包括原zh_cn等众多简体中文语言包，点击任意链接即可更改界面语言！\n\n" +
                          "关于兑换：<code>点击U兑TRX，给收款地址转10u以上的任意金额，机器人自动返回TRX到原付款地址，过程全自动，无人工干预！(点击机器人任意菜单只要有反应即可正常兑换，无需联系管理二次确认)</code>\n\n" +
                          "实时汇率：<code>TRX能量跟包括比特币在内的所有数字货币一样，价格起起落落有涨有跌，受市场行情影响，机器人的兑换汇率自动跟随市场行情进行波动！</code>\n\n" +
                          "资金费率：<code>发送 /zijin 即可查询币安永续合约资金费正负前五币种以及资金费率！</code>\n\n" +      
                          "涨跌榜单：<code>发送 /faxian 即可查询币安加密货币连续上涨或下跌榜单TOP5</code>\n\n" +                  
                          "地址监听：<code>绑定您的钱包地址，即可开启交易通知！一有交易就提示，假U，假截图，完全不起作用。发送：绑定 Txxxxxxx(您的钱包地址，，中间有个空格)即可完成绑定！ 注：为了不浪费服务器资源，暂不支持监听交易所地址，判定标准为：钱包余额大于1000万USDT或累计交易笔数大于30万笔！同时0.01USDT以下的交易将会被过滤掉！ </code>\n\n" +
                          //"能量监控：<code>使用前发送：绑定 Txxxxxxx(您的钱包地址，中间有个空格)绑定钱包地址，当TRX余额不足100时机器人会自动下发提醒！</code>\n\n" +            
                          "防骗助手：<code>把机器人拉进群聊并设置为管理员，当群内成员更改名字或用户名后，机器人会发送资料变更提醒，以防被骗！</code>\n\n" +
                          "授权查询：<code>在任意群组发送波场地址即可查询该地址授权情况，支持查询USDT和USDC授权！</code>\n\n" +    
                          "实时u价：<code>发送 z0 或者 /usdt 返回okx实时usdt买入卖出价格表</code>\n\n" +                
                          "兑换通知：<code>如果不想在群组内接受机器人兑换通知，可以发送：关闭兑换通知/开启兑换通知</code>\n\n" +            
                          //"谷歌搜索：<code>发送：谷歌+空格+搜索词自动启动谷歌搜索并返回，例如发送：</code><code>谷歌 上海天气</code>\n\n" +
                          "汇率计算：<code>发送数字+币种(支持货币代码及数字货币)自动计算并返回对应的人民币价值，例如发送1000美元或1000usd 自动按实时汇率计算并返回1000美元 ≈ ****元人民币</code>\n\n" +
                          "数字货币：<code>发送任意数字货币代码自动查询返回该币种详情，例如发送：btc 自动返回交易数据实时价格等信息-支持查询任意数字货币</code>\n\n" +
                          "查询地址：<code>发送任意TRC20波场地址自动查询地址详情并返回近期USDT交易记录！</code>\n\n" +
                          "关于翻译：<code>发送任意外文自动翻译成简体中文并返回(本功能调用谷歌翻译) 群里不想使用可以发送：关闭翻译 或：开启翻译 </code>\n\n" +
                          "中文转外文：<code>发送例如：\"转英语 你好\" 自动将你好翻译成英语：hello （附带的文件为mp3格式的外语发音）</code>\n\n" +
                          "实时查看：<code>如果想自动获取TRX-比特币-美元-USDT等在内的所有汇率，把机器人拉到群里即可，24小时自动推送！（注：如果发现推送停止，把机器人移出群重新拉群即可恢复推送！）</code>\n\n" +
                          "关于ID：<code>直接发送id自动返回用户ID，群内发送会返回用户ID以及本群群ID！ </code>\n\n" +
                          "群里使用：<code>所有功能都可在机器人私聊使用，如果在群里，需要设置机器人为管理或者回复机器人消息才可使用！</code>\n\n" +
                          "管理员：<code>机器人管理员额外支持用户列表管理，地址管理，群聊列表，双向回复，承兑账单等诸多实用功能！</code>\n\n" +      
		          "功能越加越多，受限于篇幅，没法一一写出来，请在实际使用中慢慢探索~~~感谢大家支持，我将不断完善/添加新的功能！\n\n" +      
                          "机器人兑换过程公平公正公开，交易记录全开放，发送：<code>兑换记录</code> 自动返回近期USDT收入以及TRX转出记录，欢迎监督！\n\n" +
                          "\U0001F449        本机器人源码出售，如有需要可联系" + adminLinkText + "      \U0001F448";

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: helpText,
            parseMode: ParseMode.Html,
            disableWebPagePreview: true
        );
    }
}
public static async Task<string> GetTransactionRecordsAsync(ITelegramBotClient botClient, Message message)
{
    var responseMessage = await botClient.SendTextMessageAsync(message.Chat.Id, "正在统计，请稍后...");
    var responseMessageId = responseMessage.MessageId;
    
    try
    {
        string outcomeAddress = "TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv";
        string outcomeUrl = $"https://apilist.tronscanapi.com/api/transfer/trx?address={outcomeAddress}&start=0&limit=20&direction=0&reverse=true&fee=true&db_version=1&start_timestamp=&end_timestamp=";
        string usdtUrl = $"https://api.trongrid.io/v1/accounts/TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv/transactions/trc20?limit=30&contract_address=TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t";

        using (var httpClient = new HttpClient())
        {
            //Console.WriteLine("正在获取交易记录...");
            // 添加User-Agent来模拟浏览器请求
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");

        var outcomeTransactions = new List<(DateTime timestamp, string token, decimal amount)>();
        int start = 0;
        int maxAttempts = 5; // 设置最大尝试次数以避免无限循环
        while (outcomeTransactions.Count < 8 && maxAttempts > 0)
        {
            outcomeUrl = $"https://apilist.tronscanapi.com/api/transfer/trx?address={outcomeAddress}&start={start}&limit=20&direction=0&reverse=true&fee=true&db_version=1&start_timestamp=&end_timestamp=";
            //Console.WriteLine($"正在访问URL: {outcomeUrl}");
            var outcomeResponse = await httpClient.GetStringAsync(outcomeUrl);
            var transactions = ParseTransactions(outcomeResponse, "TRX")
                .OrderByDescending(t => t.timestamp)
                .ToList();

            // 只保留金额大于10的交易记录
            transactions = transactions.Where(t => t.amount > 10).ToList();

            outcomeTransactions.AddRange(transactions);
            if (transactions.Count == 0 || outcomeTransactions.Count >= 8) // 如果没有新的符合条件的记录或已经有足够的记录，则停止循环
            {
                break;
            }
            start += 20;
            maxAttempts--;
        }
// 获取USDT交易记录
//Console.WriteLine($"正在访问URL: {usdtUrl}");
var usdtResponse = await httpClient.GetStringAsync(usdtUrl);
var usdtTransactionsTemp = ParseTransactions(usdtResponse, "USDT")
    .OrderByDescending(t => t.timestamp)
    .ToList();

// 只保留金额大于1的交易记录，并限制到前8条
var usdtTransactions = usdtTransactionsTemp
    .Where(t => t.amount > 1)
    .Take(8)
    .ToList();
		
            //Console.WriteLine("已获取所有交易记录，正在格式化...");

            var transactionRecords = FormatTransactionRecords(outcomeTransactions.Concat(usdtTransactions).ToList());

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData("\u2705 收入支出全公开，请放心兑换！\u2705", "show_address")
                }
            });

            try
            {
                //Console.WriteLine("正在发送消息...");
                await botClient.EditMessageTextAsync(message.Chat.Id, responseMessageId, transactionRecords, replyMarkup: inlineKeyboard);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"替换消息失败：{ex.Message}");
            }            

            return transactionRecords;
        }
    }
catch (HttpRequestException ex) when (ex.Message.Contains("403"))
{
    Console.WriteLine("服务器拒绝访问：403 Forbidden");
    await botClient.SendTextMessageAsync(message.Chat.Id, "查询超时，请进交易群查看！", replyMarkup: new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithUrl("点击加入交易群", "https://t.me/+b4NunT6Vwf0wZWI1")
    }));
    return "服务器超时，请进交易群查看！";
}    
catch (Exception ex)
{
    Console.WriteLine($"获取交易记录时发生错误：{ex.Message}");
    return $"获取交易记录时发生错误：{ex.Message}";
}
}
private static List<(DateTime timestamp, string token, decimal amount)> ParseTransactions(string jsonResponse, string token)
{
    var transactions = new List<(DateTime timestamp, string token, decimal amount)>();

    var json = JObject.Parse(jsonResponse);
    var dataArray = json["data"] as JArray;

    if (dataArray != null)
    {
        foreach (var data in dataArray)
        {
if (token == "TRX")
{
    // 添加条件，只添加金额大于10的交易记录
    if (data["contract_type"] != null && data["contract_type"].ToString() == "TransferContract" &&
        data["from"] != null && data["from"].ToString() == "TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv" &&
        data["block_timestamp"] != null && data["amount"] != null)
    {
        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)data["block_timestamp"]).LocalDateTime;
        var amount = decimal.Parse(data["amount"].ToString()) / 1000000;

        if (amount > 10)
        {
            transactions.Add((timestamp, token, amount));
            //Console.WriteLine($"添加了一条大于10TRX的支出记录：{timestamp:yyyy-MM-dd HH:mm:ss} 支出{token} {amount}");
        }
    }
}
            else if (token == "USDT")
            {
                // 添加条件，只添加金额大于1的交易记录
                if (data["to"] != null && data["to"].ToString() == "TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv" &&
                    data["block_timestamp"] != null && data["value"] != null)
                {
                    var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)data["block_timestamp"]).LocalDateTime;
                    var amount = decimal.Parse(data["value"].ToString()) / 1000000;

                    if (amount > 1)
                    {
                        transactions.Add((timestamp, token, amount));
                        //Console.WriteLine($"添加了一条大于1USDT的收入记录：{timestamp:yyyy-MM-dd HH:mm:ss} 收入{token} {amount}");
                    }
                }
            }
        }
    }

    return transactions;
}

private static string FormatTransactionRecords(List<(DateTime timestamp, string token, decimal amount)> transactions)
{
    var sb = new StringBuilder();
    var incomeTransactions = transactions.Where(t => t.token == "USDT").OrderByDescending(t => t.timestamp).ToList();
    var outcomeTransactions = transactions.Where(t => t.token == "TRX").OrderByDescending(t => t.timestamp).ToList();

    int totalRecords = Math.Min(incomeTransactions.Count, outcomeTransactions.Count);
    for (int i = 0; i < totalRecords; i++)
    {
        if (i < incomeTransactions.Count)
        {
            sb.AppendLine($"{incomeTransactions[i].timestamp:yyyy-MM-dd HH:mm:ss}  收入 {incomeTransactions[i].amount} {incomeTransactions[i].token}");
        }

        if (i < outcomeTransactions.Count)
        {
            sb.AppendLine($"{outcomeTransactions[i].timestamp:yyyy-MM-dd HH:mm:ss}  支出 {outcomeTransactions[i].amount} {outcomeTransactions[i].token}");
        }

        // 只有在不是最后一条记录时才添加横线
        if (i < totalRecords - 1)
        {
            sb.AppendLine("—————————————————————");
        }
    }

    return sb.ToString();
}
//以上3个方法是监控收款地址以及出款地址的交易记录并返回！   
//谷歌翻译
static Dictionary<long, bool> groupTranslationSettings = new Dictionary<long, bool>();    
public class GoogleTranslateFree
{
    private const string GoogleTranslateUrl = "https://translate.google.com/translate_a/single?client=gtx&sl=auto&tl={0}&dt=t&q={1}";

    public static async Task<(string TranslatedText, string Pronunciation, bool IsError)> TranslateAsync(string text, string targetLanguage)
    {
        using var httpClient = new HttpClient();

        HttpResponseMessage response;
        try
        {
            var url = string.Format(GoogleTranslateUrl, Uri.EscapeDataString(targetLanguage), Uri.EscapeDataString(text));
            response = await httpClient.GetAsync(url);
        }
        catch (Exception)
        {
            return (string.Empty, string.Empty, true);
        }

        var json = await response.Content.ReadAsStringAsync();

        var jsonArray = JsonSerializer.Deserialize<JsonElement>(json);

        var translatedTextBuilder = new StringBuilder();
        foreach (var segment in jsonArray[0].EnumerateArray())
        {
            translatedTextBuilder.Append(segment[0].ToString());
        }

        var translatedText = translatedTextBuilder.ToString();
        var pronunciation = jsonArray[0][0][1].ToString();

        return (translatedText, pronunciation, false);
    }

    public static string GetPronunciationAudioUrl(string text, string languageCode)
    {
        var encodedText = Uri.EscapeDataString(text);
        var audioUrl = $"https://translate.google.com/translate_tts?ie=UTF-8&total=1&idx=0&textlen={text.Length}&client=tw-ob&q={encodedText}&tl={languageCode}";
        return audioUrl;
    }
}

private static async Task HandleTranslateCommandAsync(ITelegramBotClient botClient, Message message)
{
    // 修改正则表达式以匹配多行文本
    var match = Regex.Match(message.Text, @"转([\u4e00-\u9fa5]+)(\s+)(?<textToTranslate>(?:.|\n)+)", RegexOptions.Multiline);

    if (match.Success)
    {
        var targetLanguageName = match.Groups[1].Value;
        var textToTranslate = match.Groups["textToTranslate"].Value; // 使用命名捕获组获取待翻译文本

        if (LanguageCodes.TryGetValue(targetLanguageName, out string targetLanguageCode))
        {
            // 使用 GoogleTranslateFree 或其他翻译服务进行翻译
            var (translatedText, _, isError) = await GoogleTranslateFree.TranslateAsync(textToTranslate, targetLanguageCode);

            if (isError)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "翻译服务异常，请稍后重试。");
            }
            else
            {
                var responseText = $"翻译结果：\n\n<code>{translatedText}</code>";

                await botClient.SendTextMessageAsync(message.Chat.Id, responseText, parseMode: ParseMode.Html);

                // 发送发音音频
                var audioUrl = GoogleTranslateFree.GetPronunciationAudioUrl(translatedText, targetLanguageCode);

                // 检查音频 URL 是否有效
                if (IsValidUrl(audioUrl))
                {
                    try
                    {
                        await botClient.SendAudioAsync(message.Chat.Id, new InputOnlineFile(audioUrl));
                    }
                    catch (ApiRequestException)
                    {
                        // 如果发送音频失败，忽略错误并继续
                    }
                }
            }
        }
        else
        {
            // 如果目标语言不在字典中，返回不支持的消息
            var supportedLanguages = string.Join("、", LanguageCodes.Keys);
            await botClient.SendTextMessageAsync(message.Chat.Id, $"暂不支持该语种转换，目前转换语言支持：{supportedLanguages}");
        }
    }
    else
    {
        // 如果消息格式不正确，返回错误消息
        await botClient.SendTextMessageAsync(message.Chat.Id, "无法识别的翻译命令，请确保您的输入格式正确，例如：<code>转英语 你好</code>", parseMode: ParseMode.Html);
    }
}
private static bool IsValidUrl(string urlString)
{
    return Uri.TryCreate(urlString, UriKind.Absolute, out Uri uriResult)
        && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
}
public static class EmojiHelper
{
    private static readonly Regex EmojiRegex = new Regex(@"\p{Cs}", RegexOptions.Compiled);

    public static bool ContainsEmoji(string input)
    {
        return EmojiRegex.IsMatch(input);
    }

    public static bool IsOnlyEmoji(string input)
    {
        return EmojiRegex.Replace(input, "").Length == 0;
    }
}
private static readonly Dictionary<string, string> LanguageCodes = new Dictionary<string, string>
{
    { "英语", "en" },
    { "日语", "ja" },
    { "韩语", "ko" },
    { "越南语", "vi" },
    { "高棉语", "km" },
    { "泰语", "th" },
    { "菲律宾语", "tl" },
    { "阿拉伯语", "ar" },
    { "老挝语", "lo" },
    { "马来西亚语", "ms" },
    { "西班牙语", "es" },
    { "印地语", "hi" },
    { "孟加拉文", "bn" },
    { "葡萄牙语", "pt" },
    { "俄语", "ru" },
    { "德语", "de" },
    { "法语", "fr" },
    { "意大利语", "it" },
    { "荷兰语", "nl" },
    { "土耳其语", "tr" },
    { "希腊语", "el" },
    { "匈牙利语", "hu" },
    { "波兰语", "pl" },
    { "瑞典语", "sv" },
    { "挪威语", "no" },
    { "丹麦语", "da" },
    { "芬兰语", "fi" },
    { "捷克语", "cs" },
    { "罗马尼亚语", "ro" },
    { "斯洛文尼亚语", "sl" },
    { "克罗地亚语", "hr" },
    { "保加利亚语", "bg" },
    { "塞尔维亚语", "sr" },
    { "斯洛伐克语", "sk" },
    { "立陶宛语", "lt" },
    { "拉脱维亚语", "lv" },
    { "爱沙尼亚语", "et" },
    { "乌克兰语", "uk" },
    { "格鲁吉亚语", "ka" },
    { "亚美尼亚语", "hy" },
    { "阿塞拜疆语", "az" },
    { "波斯语", "fa" },
    { "乌尔都语", "ur" },
    { "帕什图语", "ps" },
    { "哈萨克语", "kk" },
    { "乌兹别克语", "uz" },
    { "塔吉克语", "tg" },
    { "藏语", "bo" },
    { "蒙古语", "mn" },
    { "白俄罗斯语", "be" },
    { "阿尔巴尼亚语", "sq" },
    { "马其顿语", "mk" },
    { "卢森堡语", "lb" },
    { "爱尔兰语", "ga" },
    { "威尔士语", "cy" },
    { "巴斯克语", "eu" },
    { "冰岛语", "is" },
    { "马耳他语", "mt" },
    { "加利西亚语", "gl" },
    { "塞尔维亚克罗地亚语", "sh" },
    { "斯瓦希里语", "sw" },
    { "印尼语", "id" }
};
public static async Task<(decimal TotalIncome, decimal TotalOutcome, decimal MonthlyIncome, decimal MonthlyOutcome, decimal DailyIncome, decimal DailyOutcome, bool IsError)> GetTotalIncomeAsync(string address, bool isTrx)
{
    try
    {
        var apiUrl = $"https://api.trongrid.io/v1/accounts/{address}/transactions/trc20?only_confirmed=true&limit=200&contract_address=TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t";
        using var httpClient = new HttpClient();

        decimal totalIncome = 0m;
        decimal totalOutcome = 0m;
        decimal monthlyIncome = 0m;
        decimal monthlyOutcome = 0m;
        decimal dailyIncome = 0m;
        decimal dailyOutcome = 0m;
        string fingerprint = null;

        // 获取当月1号和今天的日期
        DateTime nowInUtc = DateTime.UtcNow;
        DateTime nowInBeijing = nowInUtc.AddHours(8);
        DateTime firstDayOfMonth = new DateTime(nowInBeijing.Year, nowInBeijing.Month, 1);
        DateTime today = nowInBeijing.Date;

        while (true)
        {
            var currentUrl = apiUrl + (fingerprint != null ? $"&fingerprint={fingerprint}" : "");
            var response = await httpClient.GetAsync(currentUrl);
            var json = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(json);

            if (!jsonDocument.RootElement.TryGetProperty("data", out JsonElement dataElement))
            {
                break;
            }

            foreach (var transactionElement in dataElement.EnumerateArray())
            {
                if (!transactionElement.TryGetProperty("type", out var typeElement) || typeElement.GetString() != "Transfer")
                {
                    continue;
                }

                if (!transactionElement.TryGetProperty("value", out var valueElement))
                {
                    continue;
                }
                var value = valueElement.GetString();

                decimal amount = decimal.Parse(value) / 1_000_000; // 假设USDT有6位小数

                // 获取交易时间
                DateTime transactionTime = DateTime.MinValue;
                if (transactionElement.TryGetProperty("block_timestamp", out var timestampElement))
                {
                    var timestamp = timestampElement.GetInt64();
                    DateTime transactionTimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
                    transactionTime = transactionTimeUtc.AddHours(8);
                }

                // 判断是收入还是支出
                bool isIncome = transactionElement.GetProperty("to").GetString() == address;
                if (isIncome)
                {
                    totalIncome += amount;
                    if (transactionTime >= firstDayOfMonth)
                    {
                        monthlyIncome += amount;
                    }
                    if (transactionTime.Date == today)
                    {
                        dailyIncome += amount;
                    }
                }
                else
                {
                    totalOutcome += amount;
                    if (transactionTime >= firstDayOfMonth)
                    {
                        monthlyOutcome += amount;
                    }
                    if (transactionTime.Date == today)
                    {
                        dailyOutcome += amount;
                    }
                }

                if (transactionElement.TryGetProperty("transaction_hash", out var transactionIdElement))
                {
                    fingerprint = transactionIdElement.GetString();
                }
            }

            if (!jsonDocument.RootElement.TryGetProperty("has_next", out JsonElement hasNextElement) || !hasNextElement.GetBoolean())
            {
                break;
            }
        }

        // 如果没有发生错误，返回结果和IsError=false
        return (totalIncome, totalOutcome, monthlyIncome, monthlyOutcome, dailyIncome, dailyOutcome, false);
    }
    catch (Exception ex)
    {
        // 发生错误时，返回默认值和IsError=true
        Console.WriteLine($"Error in method {nameof(GetTotalIncomeAsync)}: {ex.Message}");
        return (0m, 0m, 0m, 0m, 0m, 0m, true);
    }
}
    
public static DateTime ConvertToBeijingTime(DateTime utcDateTime)
{
    var timeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
    return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
}
    
public static async Task<(DateTime LastTransactionTime, bool IsError)> GetLastTransactionTimeAsync(string address)
{
    try
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"https://api.trongrid.io/v1/accounts/{address}/transactions");
        var json = await response.Content.ReadAsStringAsync();

        var jsonDocument = JsonDocument.Parse(json);
        var lastTimestamp = 0L;

        if (jsonDocument.RootElement.TryGetProperty("data", out JsonElement dataElement) && dataElement.GetArrayLength() > 0)
        {
            var lastElement = dataElement[0];

            if (lastElement.TryGetProperty("block_timestamp", out JsonElement lastTimeElement))
            {
                lastTimestamp = lastTimeElement.GetInt64();
            }
        }

        var utcDateTime = DateTimeOffset.FromUnixTimeMilliseconds(lastTimestamp).DateTime;
        return (ConvertToBeijingTime(utcDateTime), false); // 如果没有发生错误，返回结果和IsError=false
    }
    catch (Exception ex)
    {
        // 发生错误时，返回默认值和IsError=true
        Console.WriteLine($"Error in method {nameof(GetLastTransactionTimeAsync)}: {ex.Message}");
        return (DateTime.MinValue, true);
    }
}
    
public static async Task<(DateTime CreationTime, bool IsError)> GetAccountCreationTimeAsync(string address)
{
    try
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"https://api.trongrid.io/v1/accounts/{address}");
        var json = await response.Content.ReadAsStringAsync();

        var jsonDocument = JsonDocument.Parse(json);
        var creationTimestamp = 0L;

        if (jsonDocument.RootElement.TryGetProperty("data", out JsonElement dataElement) && dataElement.GetArrayLength() > 0)
        {
            var firstElement = dataElement[0];

            if (firstElement.TryGetProperty("create_time", out JsonElement createTimeElement))
            {
                creationTimestamp = createTimeElement.GetInt64();
            }
        }

        var utcDateTime = DateTimeOffset.FromUnixTimeMilliseconds(creationTimestamp).DateTime;
        return (ConvertToBeijingTime(utcDateTime), false); // 如果没有发生错误，返回结果和IsError=false
    }
    catch (Exception ex)
    {
        // 发生错误时，返回默认值和IsError=true
        Console.WriteLine($"Error in method {nameof(GetAccountCreationTimeAsync)}: {ex.Message}");
        return (DateTime.MinValue, true);
    }
} 
   
public static async Task<(decimal UsdtBalance, decimal TrxBalance, bool IsError)> GetBalancesAsync(string address)
{
    try
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"https://api.trongrid.io/v1/accounts/{address}");
        var json = await response.Content.ReadAsStringAsync();

        var jsonDocument = JsonDocument.Parse(json);

        var usdtBalance = 0m;
        var trxBalance = 0m;

        if (jsonDocument.RootElement.TryGetProperty("data", out JsonElement dataElement) && dataElement.GetArrayLength() > 0)
        {
            var firstElement = dataElement[0];

            if (firstElement.TryGetProperty("balance", out JsonElement trxBalanceElement))
            {
                trxBalance = trxBalanceElement.GetDecimal() / 1_000_000;
            }

            if (firstElement.TryGetProperty("trc20", out JsonElement trc20Element))
            {
                foreach (var token in trc20Element.EnumerateArray())
                {
                    foreach (var property in token.EnumerateObject())
                    {
                        if (property.Name == "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t") //这是USDT合约地址 可以换成任意合约地址
                        {
                            usdtBalance = decimal.Parse(property.Value.GetString()) / 1_000_000;
                            break;
                        }
                    }
                }
            }
        }

        return (usdtBalance, trxBalance, false); // 如果没有发生错误，返回结果和IsError=false
    }
    catch (Exception ex)
    {
        // 发生错误时，返回零余额和IsError=true
        Console.WriteLine($"Error in method {nameof(GetBalancesAsync)}: {ex.Message}");
        return (0m, 0m, true);
    }
}
public static async Task<(double remainingBandwidth, double totalBandwidth, double netRemaining, double netLimit, double energyRemaining, double energyLimit, int transactions, int transactionsIn, int transactionsOut, bool isError)> GetBandwidthAsync(string address)
{
    try
    {
        // 构建请求URL
        string url = $"https://apilist.tronscanapi.com/api/accountv2?address={address}";
        using var httpClient = new HttpClient();
        // 发送请求并获取结果
        var result = await httpClient.GetStringAsync(url);

        // 解析返回的JSON数据
        var jsonResult = JObject.Parse(result);

        // 检查JSON对象是否为空
        if (!jsonResult.HasValues)
        {
            // 如果为空，则返回默认值
            return (0, 0, 0, 0, 0, 0, 0, 0, 0, false);
        }

        double freeNetRemaining = jsonResult["bandwidth"]["freeNetRemaining"].ToObject<double>();
        double freeNetLimit = jsonResult["bandwidth"]["freeNetLimit"].ToObject<double>();
        double netRemaining = jsonResult["bandwidth"]["netRemaining"].ToObject<double>();
        double netLimit = jsonResult["bandwidth"]["netLimit"].ToObject<double>();
        double energyRemaining = jsonResult["bandwidth"]["energyRemaining"].ToObject<double>();
        double energyLimit = jsonResult["bandwidth"]["energyLimit"].ToObject<double>();
        int transactions = jsonResult["transactions"].ToObject<int>();
        int transactionsIn = jsonResult["transactions_in"].ToObject<int>();
        int transactionsOut = jsonResult["transactions_out"].ToObject<int>();

        return (freeNetRemaining, freeNetLimit, netRemaining, netLimit, energyRemaining, energyLimit, transactions, transactionsIn, transactionsOut, false);
    }
    catch (Exception ex)
    {
        // 如果发生异常，返回一个特殊的元组值
        Console.WriteLine($"Error in method {nameof(GetBandwidthAsync)}: {ex.Message}");
        return (0, 0, 0, 0, 0, 0, 0, 0, 0, true);
    }
}
public static async Task<(string, bool)> GetLastFiveTransactionsAsync(string tronAddress)
{
    int limit = 20; // 可以增加 limit 以获取更多的交易记录
    string tokenId = "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t"; // USDT合约地址
    string url = $"https://api.trongrid.io/v1/accounts/{tronAddress}/transactions/trc20?only_confirmed=true&limit={limit}&token_id={tokenId}";

    using (var httpClient = new HttpClient())
    {
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return (string.Empty, false); // 如果没有返回消息或服务器在维护，返回空字符串且IsError=false
            }

            string jsonString = await response.Content.ReadAsStringAsync();
            JObject jsonResponse = JObject.Parse(jsonString);

            JArray transactions = (JArray)jsonResponse["data"];

            if (transactions == null || !transactions.HasValues)
            {
                return (string.Empty, false); // 如果没有交易数据，返回空字符串且IsError=false
            }

            // 筛选与Tether相关的交易，并过滤金额小于1USDT的交易
            transactions = new JArray(transactions.Where(t => (string)t["token_info"]["name"] == "Tether USD" && (string)t["token_info"]["address"] == "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t" && decimal.Parse((string)t["value"]) >= 1_000_000));

            // 取筛选后的前5笔交易
            transactions = new JArray(transactions.Take(5));

            StringBuilder transactionTextBuilder = new StringBuilder();

            foreach (var transaction in transactions)
            {
                // 获取交易哈希值
                string txHash = (string)transaction["transaction_id"];

                // 获取交易时间，并转换为北京时间
                long blockTimestamp = (long)transaction["block_timestamp"];
                DateTime transactionTimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(blockTimestamp).UtcDateTime;
                DateTime transactionTimeBeijing = TimeZoneInfo.ConvertTime(transactionTimeUtc, TimeZoneInfo.FindSystemTimeZoneById("China Standard Time"));

                // 判断交易是收入还是支出
                string type;
                string fromAddress = (string)transaction["from"];
                string toAddress = (string)transaction["to"];

                if (tronAddress.Equals(fromAddress, StringComparison.OrdinalIgnoreCase))
                {
                    type = "出 ";
                }
                else
                {
                    type = "入 ";
                }

                // 获取交易金额，并转换为USDT
                string value = (string)transaction["value"];
                decimal usdtAmount = decimal.Parse(value) / 1_000_000;
                // 输出API返回的数据，在解析JSON响应之后添加
                //Console.WriteLine(jsonString);

                // 构建交易文本并添加链接
                transactionTextBuilder.AppendLine($"{transactionTimeBeijing:yyyy-MM-dd HH:mm:ss}  {type}<a href=\"https://tronscan.org/#/transaction/{txHash}\">{usdtAmount:N2} U</a>");
            }

            return (transactionTextBuilder.ToString(), false); // 如果没有发生错误，返回结果和IsError=false
        }
        catch (Exception ex)
        {
            // 发生错误时，返回空字符串和IsError=true
            Console.WriteLine($"Error in method {nameof(GetLastFiveTransactionsAsync)}: {ex.Message}");
            return (string.Empty, true);
        }
    }
}
public static async Task<(string, bool)> GetOwnerPermissionAsync(string tronAddress)
{
    try
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"https://api.trongrid.io/v1/accounts/{tronAddress}");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            if (json.ContainsKey("data") && json["data"] is JArray dataArray && dataArray.Count > 0)
            {
                var accountData = dataArray[0] as JObject;
                if (accountData != null && accountData.ContainsKey("owner_permission") &&
                    accountData["owner_permission"]["keys"] is JArray keysArray && keysArray.Count > 0)
                {
                    // 获取第一个签名地址
                    string firstSignAddress = keysArray[0]["address"].ToString();
                    return (firstSignAddress, false);
                }
                else
                {
                    return ("当前地址未激活", false);
                }
            }
            else
            {
                return ("当前地址未激活", false);
            }
        }
        else
        {
            return (string.Empty, true);
        }
    }
    catch (HttpRequestException ex)
    {
        // 当发生 HttpRequestException 时，返回一个指示错误的元组值
        Console.WriteLine($"Error in method {nameof(GetOwnerPermissionAsync)}: {ex.Message}");
        return (string.Empty, true);
    }
    catch (JsonException ex)
    {
        // 当发生 JsonException 时，返回一个指示错误的元组值
        Console.WriteLine($"Error in method {nameof(GetOwnerPermissionAsync)}: {ex.Message}");
        return (string.Empty, true);
    }
    catch (Exception ex)
    {
        // 当发生其他异常时，返回一个指示错误的元组值
        Console.WriteLine($"Error in method {nameof(GetOwnerPermissionAsync)}: {ex.Message}");
        return (string.Empty, true);
    }
}
// 计算尾数中连续相同字符（忽略大小写）的数量
private static int CountConsecutiveIdenticalChars(string input)
{
    int count = 1;
    char currentChar = char.ToLowerInvariant(input[input.Length - 1]);

    // 从倒数第二个字符开始遍历
    for (int i = input.Length - 2; i >= 0; i--)
    {
        char currentInputChar = char.ToLowerInvariant(input[i]);

        // 如果当前字符与上一个字符相同（忽略大小写），计数器加1
        if (currentInputChar == currentChar)
        {
            count++;
        }
        else
        {
            break;
        }
    }

    return count;
}
//oklink查询授权                                                                 
public class AuthorizedRecord
{
    public string approvedContractAddress { get; set; }
    public string approvedAmount { get; set; }
    public string tokenId { get; set; }
    public string approvedTime { get; set; }
    public string approvedTxId { get; set; }
    public string approvedProjectName { get; set; }
}

public class Data
{
    public string chainShortName { get; set; }
    public string protocolType { get; set; }
    public string tokenContractAddress { get; set; }
    public string authorizationAddress { get; set; }
    public string precision { get; set; }
    public string tokenFullName { get; set; }
    public string token { get; set; }
    public string holdingAmount { get; set; }
    public List<AuthorizedRecord> authorizedList { get; set; }
}

public class Root
{
    public string code { get; set; }
    public string msg { get; set; }
    public List<Data> data { get; set; }
}  
//只查询一条                                                                 
public static async Task<string> GetUsdtAuthorizedListAsync(string tronAddress)
{
    try
    {
        using (var httpClient = new HttpClient())
        {
            // 添加所有的秘钥到列表
            List<string> keys = new List<string>
            {
                "5090e006-163f-4d61-8fa1-1f41fa70d7f8",
                "f49353bd-db65-4719-a56c-064b2eb231bf",
                "92854974-68da-4fd8-9e50-3948c1e6fa7e"
            };

            // 随机选择一个秘钥
            Random random = new Random();
            int index = random.Next(keys.Count);
            string key = keys[index];

            // 添加请求头
            httpClient.DefaultRequestHeaders.Add("OK-ACCESS-KEY", key);

            // 构建请求URI
            string requestUri = $"https://www.oklink.com/api/v5/tracker/contractscanner/token-authorized-list?chainShortName=tron&address={tronAddress}";
            var response = await httpClient.GetAsync(requestUri);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"请求失败，状态码：{response.StatusCode}");

                // 移除失败的秘钥
                keys.Remove(key);

                // 如果还有其他秘钥，随机选择一个并重试请求
                if (keys.Count > 0)
                {
                    index = random.Next(keys.Count);
                    key = keys[index];
                    httpClient.DefaultRequestHeaders.Remove("OK-ACCESS-KEY");
                    httpClient.DefaultRequestHeaders.Add("OK-ACCESS-KEY", key);
                    response = await httpClient.GetAsync(requestUri);
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"重试请求失败，状态码：{response.StatusCode}");
                        return "无法获取授权记录，请稍后再试。";
                    }
                }
                else
                {
                    return "无法获取授权记录，请稍后再试。";
                }
            }

            string content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"响应内容：{content}");

            // 反序列化响应内容
            var result = System.Text.Json.JsonSerializer.Deserialize<Root>(content);
            Console.WriteLine($"解析后的结果：{result}");

            // 检查返回的code是否为"0"
            if (result.code != "0")
            {
                return "查询授权记录出错：" + result.msg;
            }

            StringBuilder sb = new StringBuilder();
            //sb.AppendLine("———————授权列表———————"); // 移动到循环外面

            // 检查data数组是否为空
            if (result.data == null || result.data.Count == 0 || result.data[0].authorizedList == null)
            {
                sb.AppendLine("无授权记录。");
            }
            else
            {
// 遍历授权记录
foreach (var dataItem in result.data)
{
    // 只处理 Tether USD (USDT) 或 USD Coin (USDC) 的记录
    if (dataItem.tokenFullName == "Tether USD" || dataItem.token == "USDT" ||
        dataItem.tokenFullName == "USD Coin" || dataItem.token == "USDC")
    {
        // 只处理第一条记录
        var record = dataItem.authorizedList.FirstOrDefault();
        if (record != null)
        {
            string projectName = string.IsNullOrEmpty(record.approvedProjectName) ? "点击查看授权详情" : record.approvedProjectName;
            string amount = record.approvedAmount == "unlimited" ? "无限" : $"{decimal.Parse(record.approvedAmount):N0}"; // 使用带有逗号的数字格式
            string address = record.approvedContractAddress;
            // 确保从JsonElement获取字符串表示
            string approvedTimeString = record.approvedTime.ToString();
            if (!long.TryParse(approvedTimeString, out long approvedTime))
            {
                Console.WriteLine($"无法将'{approvedTimeString}'转换为长整型。");
                continue; // 跳过这个记录，继续处理下一个记录
            }
            // 将Unix时间戳转换为北京时间（UTC+8）
            DateTime time = DateTimeOffset.FromUnixTimeMilliseconds(approvedTime).DateTime.AddHours(8);
            string tokenFullName = dataItem.tokenFullName == "Tether USD" ? "Tether USD (USDT)" : "USD Coin (USDC)";

            // 创建授权项目的链接
            string projectLink = $"https://tronscan.org/#/transaction/{record.approvedTxId}";
            string linkedProjectName = $"<a href=\"{projectLink}\">{projectName}</a>";

            sb.AppendLine($"授权币种： {tokenFullName}");
            sb.AppendLine($"授权金额： {amount}");
            sb.AppendLine($"授权项目： {linkedProjectName}");
            //sb.AppendLine($"授权地址： {address}");
            // 添加时分秒到授权时间
            sb.AppendLine($"授权时间： {time:yyyy年MM月dd日HH时mm分ss秒}");
            //sb.AppendLine($"授权项目： {linkedProjectName}");
            sb.AppendLine($"授权地址： {address}");
            sb.AppendLine("------------------");
        }
        break; // 只处理第一条记录，然后跳出循环
    }
}
            }

// 移除最后的分隔线
if (sb.Length > 0)
{
    sb.Length -= Environment.NewLine.Length + 18; // "------------------".Length + Environment.NewLine.Length
}

return sb.ToString();
        }
    }
    catch (Exception ex)
    {
        // 捕获并处理异常
        Console.WriteLine($"在获取授权记录时发生异常：{ex.Message}");
        return "无授权记录\n";
    }
}
//// 辅助方法：将长文本分割成指定数量的记录                                                                 
private static IEnumerable<string> SplitIntoChunks(string text, int recordsPerChunk)
{
    var lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
    var chunk = new List<string>();
    foreach (var line in lines)
    {
        chunk.Add(line);
        if (chunk.Count >= recordsPerChunk * 6) // 每条记录6行
        {
            yield return string.Join(Environment.NewLine, chunk);
            chunk.Clear();
        }
    }
    if (chunk.Count > 0)
    {
        yield return string.Join(Environment.NewLine, chunk);
    }
}                                                                 
//完整授权列表                                                                 
public static async Task<string> GetUsdtAuthorizedListAsyncquanbu(string tronAddress)
{
    try
    {
        using (var httpClient = new HttpClient())
        {
            // 添加所有的秘钥到列表
            List<string> keys = new List<string>
            {
                "5090e006-163f-4d61-8fa1-1f41fa70d7f8",
                "f49353bd-db65-4719-a56c-064b2eb231bf",
                "92854974-68da-4fd8-9e50-3948c1e6fa7e"
            };

            // 随机选择一个秘钥
            Random random = new Random();
            int index = random.Next(keys.Count);
            string key = keys[index];

            // 添加请求头
            httpClient.DefaultRequestHeaders.Add("OK-ACCESS-KEY", key);

            // 构建请求URI
            string requestUri = $"https://www.oklink.com/api/v5/tracker/contractscanner/token-authorized-list?chainShortName=tron&address={tronAddress}";
            var response = await httpClient.GetAsync(requestUri);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"请求失败，状态码：{response.StatusCode}");

                // 移除失败的秘钥
                keys.Remove(key);

                // 如果还有其他秘钥，随机选择一个并重试请求
                if (keys.Count > 0)
                {
                    index = random.Next(keys.Count);
                    key = keys[index];
                    httpClient.DefaultRequestHeaders.Remove("OK-ACCESS-KEY");
                    httpClient.DefaultRequestHeaders.Add("OK-ACCESS-KEY", key);
                    response = await httpClient.GetAsync(requestUri);
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"重试请求失败，状态码：{response.StatusCode}");
                        return "无法获取授权记录，请稍后再试。";
                    }
                }
                else
                {
                    return "无法获取授权记录，请稍后再试。";
                }
            }

            string content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"响应内容：{content}");

            // 反序列化响应内容
            var result = System.Text.Json.JsonSerializer.Deserialize<Root>(content);
            Console.WriteLine($"解析后的结果：{result}");

            // 检查返回的code是否为"0"
            if (result.code != "0")
            {
                return "查询授权记录出错：" + result.msg;
            }

            StringBuilder sb = new StringBuilder();
            //sb.AppendLine("———————授权列表———————"); // 移动到循环外面

            // 检查data数组是否为空
            if (result.data == null || result.data.Count == 0 || result.data[0].authorizedList == null)
            {
                sb.AppendLine("无授权记录。");
            }
            else
            {
        foreach (var dataItem in result.data)
        {
            // 只处理 Tether USD (USDT) 或 USD Coin (USDC) 的记录
            if (dataItem.tokenFullName == "Tether USD" || dataItem.token == "USDT" ||
                dataItem.tokenFullName == "USD Coin" || dataItem.token == "USDC")
            {
                foreach (var record in dataItem.authorizedList)
                {
                    string projectName = string.IsNullOrEmpty(record.approvedProjectName) ? "点击查看授权详情" : record.approvedProjectName;
                    string amount = record.approvedAmount == "unlimited" ? "无限" : $"{decimal.Parse(record.approvedAmount):N0}";
                    string address = record.approvedContractAddress;
                    long approvedTime = long.Parse(record.approvedTime);
                    DateTime time = DateTimeOffset.FromUnixTimeMilliseconds(approvedTime).DateTime.AddHours(8);
                    string tokenFullName = dataItem.tokenFullName == "Tether USD" ? "Tether USD (USDT)" : "USD Coin (USDC)";

                    string projectLink = $"https://tronscan.org/#/transaction/{record.approvedTxId}";
                    string linkedProjectName = $"<a href=\"{projectLink}\">{projectName}</a>";

                    sb.AppendLine($"授权币种： {tokenFullName}");
                    sb.AppendLine($"授权金额： {amount}");
                    sb.AppendLine($"授权项目： {linkedProjectName}");
                    sb.AppendLine($"授权时间： {time:yyyy年MM月dd日HH时mm分ss秒}");
                    sb.AppendLine($"授权地址： {address}");
                    sb.AppendLine("--------------------------------------------------------");
                }
            }
        }
            }

    // 移除最后的分隔线
    if (sb.Length > 0)
    {
        sb.Length -= Environment.NewLine.Length + 18; // "---------------------------------------------------------------------".Length + Environment.NewLine.Length
    }

    return sb.ToString();
        }
    }
    catch (Exception ex)
    {
        // 捕获并处理异常
        Console.WriteLine($"在获取授权记录时发生异常：{ex.Message}");
        return "无授权记录\n";
    }
}                                                                 
public static async Task HandleQueryCommandAsync(ITelegramBotClient botClient, Message message)
{
    var text = message.Text;
    var match = Regex.Match(text, @"(T[A-Za-z0-9]{33})"); // 验证Tron地址格式
    if (!match.Success)
    {
        await botClient.SendTextMessageAsync(message.Chat.Id, "查询地址错误，请重新输入");
        return;
    }
    var tronAddress = match.Groups[1].Value;

    // 如果查询的地址是TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv，直接返回错误信息
    if (tronAddress == "TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv")
    {
        await botClient.SendTextMessageAsync(message.Chat.Id, "此为机器人收款地址，转账USDT自动返回TRX！");
        return;
    }
    // 在此处添加获取USDT OTC价格的代码
    var getOkxPriceTask = GetOkxPriceAsync("usdt", "cny", "alipay");
    await getOkxPriceTask;
    decimal okxPrice = getOkxPriceTask.Result;
    
    // 回复用户正在查询
    await botClient.SendTextMessageAsync(message.Chat.Id, "正在查询，请稍后...");

// 同时启动所有任务
var getUsdtTransferTotalTask = GetUsdtTransferTotalAsync(tronAddress, "TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv");
var getBalancesTask = GetBalancesAsync(tronAddress);
var getAccountCreationTimeTask = GetAccountCreationTimeAsync(tronAddress);
var getLastTransactionTimeTask = GetLastTransactionTimeAsync(tronAddress);
var getTotalIncomeTask = GetTotalIncomeAsync(tronAddress, false);
var getBandwidthTask = GetBandwidthAsync(tronAddress);
var getLastFiveTransactionsTask = GetLastFiveTransactionsAsync(tronAddress);
var getOwnerPermissionTask = GetOwnerPermissionAsync(tronAddress);
var usdtAuthorizedListTask = GetUsdtAuthorizedListAsync(tronAddress);    

// 等待所有任务完成
//await Task.WhenAll(getUsdtTransferTotalTask, getBalancesTask, getAccountCreationTimeTask, getLastTransactionTimeTask, getTotalIncomeTask, getBandwidthTask, getLastFiveTransactionsTask, getOwnerPermissionTask);
await Task.WhenAll(getUsdtTransferTotalTask, getBalancesTask, getAccountCreationTimeTask, getLastTransactionTimeTask, getTotalIncomeTask, getBandwidthTask, getLastFiveTransactionsTask, getOwnerPermissionTask, usdtAuthorizedListTask);
    

// 处理结果
var usdtTransferTotalResult = getUsdtTransferTotalTask.Result;
var (usdtTotal, transferCount, isErrorUsdtTransferTotal) = usdtTransferTotalResult;

var getBandwidthResult = getBandwidthTask.Result;
var (remainingBandwidth, totalBandwidth, netRemaining, netLimit, energyRemaining, energyLimit, transactions, transactionsIn, transactionsOut, isErrorGetBandwidth) = getBandwidthResult;

var getLastFiveTransactionsResult = getLastFiveTransactionsTask.Result;
var (lastFiveTransactions, isErrorGetLastFiveTransactions) = getLastFiveTransactionsResult;

var getBalancesResult = getBalancesTask.Result;
var (usdtBalance, trxBalance, isErrorGetBalances) = getBalancesResult;

var getAccountCreationTimeResult = getAccountCreationTimeTask.Result;
var (creationTime, isErrorGetAccountCreationTime) = getAccountCreationTimeResult;

var getLastTransactionTimeResult = getLastTransactionTimeTask.Result;
var (lastTransactionTime, isErrorGetLastTransactionTime) = getLastTransactionTimeResult;

var getTotalIncomeResult = getTotalIncomeTask.Result;
var (usdtTotalIncome, usdtTotalOutcome, monthlyIncome, monthlyOutcome, dailyIncome, dailyOutcome, isErrorGetTotalIncome) = getTotalIncomeResult;

var getOwnerPermissionResult = getOwnerPermissionTask.Result;
var (ownerPermissionAddress, isErrorGetOwnerPermission) = getOwnerPermissionResult;

var usdtAuthorizedListResult = usdtAuthorizedListTask.Result;
    
 // 计算人民币余额
 decimal cnyBalance = usdtBalance * okxPrice;
// 计算可供转账的次数
int availableTransferCount = (int)(trxBalance / 13.3959m);    
    
// 检查是否发生了请求错误 欧意otc价格未加入，异常会导致返回的价格为0 全部关闭  所有的接口都处理了异常了
//if (isErrorUsdtTransferTotal || isErrorGetBandwidth || isErrorGetLastFiveTransactions || isErrorGetBalances || isErrorGetAccountCreationTime || isErrorGetLastTransactionTime || isErrorGetTotalIncome || isErrorGetOwnerPermission)
//{
//    await botClient.SendTextMessageAsync(message.Chat.Id, "查询地址有误或接口维护中，请稍后重试！");
//    return;
//}
    
    // 判断是否所有返回的数据都是0
//if (usdtTotal == 0 && transferCount == 0 && usdtBalance == 0 && trxBalance == 0 && 
    //usdtTotalIncome == 0 && remainingBandwidth == 0 && totalBandwidth == 0 && 
    //transactions == 0 && transactionsIn == 0 && transactionsOut == 0)
//{
    // 如果都是0，那么添加提醒用户的语句
    //string warningText = "查询地址有误或地址未激活，请激活后重试！";
    //await botClient.SendTextMessageAsync(message.Chat.Id, warningText);
    //return;
//}
// 计算地址中连续相同字符的数量（忽略大小写）
int maxConsecutiveIdenticalCharsCount = 0;
int currentConsecutiveIdenticalCharsCount = 0;
char previousChar = '\0';

foreach (char c in tronAddress)
{
    if (char.ToUpperInvariant(c) == char.ToUpperInvariant(previousChar))
    {
        currentConsecutiveIdenticalCharsCount++;
        maxConsecutiveIdenticalCharsCount = Math.Max(maxConsecutiveIdenticalCharsCount, currentConsecutiveIdenticalCharsCount);
    }
    else
    {
        currentConsecutiveIdenticalCharsCount = 1;
        previousChar = c;
    }
}

// 当连续相同字符数量大于等于4时，添加“靓号”信息
string fireEmoji = "\uD83D\uDD25";
string buyLink = "https://t.me/lianghaonet";
string userLabelSuffix = $" <a href=\"{buyLink}\">购买靓号</a>";

if (maxConsecutiveIdenticalCharsCount >= 4)
{
    userLabelSuffix = $" {fireEmoji}{maxConsecutiveIdenticalCharsCount}连靓号{fireEmoji} <a href=\"{buyLink}\">我也要靓号</a>";
}
    
// 添加地址权限的信息
string addressPermissionText;
if (string.IsNullOrEmpty(ownerPermissionAddress))
{
    addressPermissionText = $"<b>当前地址未激活</b>";
}
else if (ownerPermissionAddress.Equals(tronAddress, StringComparison.OrdinalIgnoreCase))
{
    addressPermissionText = "当前地址未多签";
}
else
{
    addressPermissionText = $"<code>{ownerPermissionAddress}</code>";
}
    // 根据USDT余额判断用户标签
    string userLabel;
    if (usdtBalance < 100_000)
    {
        userLabel = "普通用户";
    }
    else if (usdtBalance >= 100_000 && usdtBalance < 1_000_000)
    {
        userLabel = "土豪大佬";
    }
    else
    {
        userLabel = "远古巨鲸";
    }

    string resultText;
    
string exchangeUrl = "https://t.me/yifanfubot";
string exchangeLink = $"<a href=\"{exchangeUrl}\">立即兑换</a>";
decimal monthlyProfit = monthlyIncome - monthlyOutcome;//月盈亏
decimal dailyProfit = dailyIncome - dailyOutcome; //日盈亏 

//不想要可以把 3301-3315删除    3318-3320删除 $"<b>来自 </b>{userLink}<b>的查询</b>\n\n" +删除即可
// 获取发送消息的用户信息
var fromUser = message.From;
string userLink = "未知用户";

if (fromUser != null)
{
    string fromUsername = fromUser.Username;
    string fromFirstName = fromUser.FirstName;
    string fromLastName = fromUser.LastName;

    // 创建用户链接
    if (!string.IsNullOrEmpty(fromUsername))
    {
        userLink = $"<a href=\"tg://user?id={fromUser.Id}\">{fromFirstName} {fromLastName}</a>";
    }
    else
    {
        userLink = $"{fromFirstName} {fromLastName}";
    }
}
//计算累计盈亏
decimal usdtProfit = usdtTotalIncome - usdtTotalOutcome; 
//私聊广告    
string botUsername = "yifanfubot"; // 你的机器人的用户名
string startParameter = ""; // 如果你希望机器人在被添加到群组时收到一个特定的消息，可以设置这个参数
string shareLink = $"https://t.me/{botUsername}?startgroup={startParameter}";    
string groupExclusiveText = $"<a href=\"{shareLink}\">群内发送地址可查：\n所有收入/支出统计，波场地址授权记录！</a>\n";

 // 添加授权列表的信息
string usdtAuthorizedListText = "";
if (!string.IsNullOrEmpty(usdtAuthorizedListResult))
{
    usdtAuthorizedListText = "———————<b>授权列表</b>———————\n" + usdtAuthorizedListResult;
}
    
// 判断 TRX 余额是否小于100
if (message.Chat.Type != ChatType.Private)
{
if (trxBalance < 100)
{
    resultText = $"<b>来自 </b>{userLink}<b>的查询</b>\n\n" +
    $"查询地址：<code>{tronAddress}</code>\n" +
    $"多签地址：<b>{addressPermissionText}</b>\n" +    
    $"注册时间：<b>{creationTime:yyyy-MM-dd HH:mm:ss}</b>\n" +
    $"最后活跃：<b>{lastTransactionTime:yyyy-MM-dd HH:mm:ss}</b>\n" +
    $"————————<b>资源</b>————————\n"+
    $"用户标签：<b>{userLabel} {userLabelSuffix}</b>\n" +
    $"交易次数：<b>{transactions} （ ↑{transactionsOut} _ ↓{transactionsIn} ）</b>\n" +        
    $"USDT余额：<b>{usdtBalance.ToString("N2")} ≈ {cnyBalance.ToString("N2")}元人民币</b>\n" +
    $"TRX余额：<b>{trxBalance.ToString("N2")}  |  TRX能量不足，请{exchangeLink}</b>\n" +
    $"免费带宽：<b>{remainingBandwidth.ToString("N0")}/{totalBandwidth.ToString("N0")}</b>\n" +
    $"质押带宽：<b>{netRemaining.ToString("N0")} / {netLimit.ToString("N0")}</b>\n" +
    $"质押能量：<b>{energyRemaining.ToString("N0")} / {energyLimit.ToString("N0")}</b>\n" +   
    $"累计兑换：<b>{usdtTotal.ToString("N2")} USDT</b>\n" +
    $"兑换次数：<b>{transferCount.ToString("N0")} 次</b>\n" +  
    usdtAuthorizedListText + // 添加授权列表的信息
    $"———————<b>USDT账单</b>———————\n" +
    $"{lastFiveTransactions}\n"+
    //$"USDT转入：<b>{usdtTotalIncome.ToString("N2")}</b> | 本月：<b>{monthlyIncome.ToString("N2")}</b> | 今日：<b>{dailyIncome.ToString("N2")}</b>\n" +
    //$"USDT转出：<b>{usdtTotalOutcome.ToString("N2")}</b> | 本月：<b>{monthlyOutcome.ToString("N2")}</b> | 今日：<b>{dailyOutcome.ToString("N2")}</b>\n";
    $"累计收入：<b>{usdtTotalIncome.ToString("N2")}</b> | 支出：<b>{usdtTotalOutcome.ToString("N2")}</b> | 盈亏：<b>{usdtProfit.ToString("N2")}</b>\n" +
    $"本月收入：<b>{monthlyIncome.ToString("N2")}</b> | 支出：<b>-{monthlyOutcome.ToString("N2")}</b> | 盈亏：<b>{monthlyProfit.ToString("N2")}</b>\n" +
    $"今日收入：<b>{dailyIncome.ToString("N2")}</b> | 支出：<b>-{dailyOutcome.ToString("N2")}</b> | 盈亏：<b>{dailyProfit.ToString("N2")}</b>";
    //$"USDT今日收入：<b>{dailyIncome.ToString("N2")}</b>\n" ;    
}
else
{
    resultText = $"<b>来自 </b>{userLink}<b>的查询</b>\n\n" +
    $"查询地址：<code>{tronAddress}</code>\n" +
    $"多签地址：<b>{addressPermissionText}</b>\n" +    
    $"注册时间：<b>{creationTime:yyyy-MM-dd HH:mm:ss}</b>\n" +
    $"最后活跃：<b>{lastTransactionTime:yyyy-MM-dd HH:mm:ss}</b>\n" +
    $"————————<b>资源</b>————————\n"+
    $"用户标签：<b>{userLabel} {userLabelSuffix}</b>\n" +
    $"交易次数：<b>{transactions} （ ↑{transactionsOut} _ ↓{transactionsIn} ）</b>\n" +    
    $"USDT余额：<b>{usdtBalance.ToString("N2")} ≈ {cnyBalance.ToString("N2")}元人民币</b>\n" +
    $"TRX余额：<b>{trxBalance.ToString("N2")}  |  可供转账{availableTransferCount}次</b> \n" +
    $"免费带宽：<b>{remainingBandwidth.ToString("N0")}/{totalBandwidth.ToString("N0")}</b>\n" +
    $"质押带宽：<b>{netRemaining.ToString("N0")} / {netLimit.ToString("N0")}</b>\n" +
    $"质押能量：<b>{energyRemaining.ToString("N0")} / {energyLimit.ToString("N0")}</b>\n" +       
    $"累计兑换：<b>{usdtTotal.ToString("N2")} USDT</b>\n" +
    $"兑换次数：<b>{transferCount.ToString("N0")} 次</b>\n" +   
    usdtAuthorizedListText + // 添加授权列表的信息        
    $"———————<b>USDT账单</b>———————\n" +
    $"{lastFiveTransactions}\n"+
    //$"USDT转入：<b>{usdtTotalIncome.ToString("N2")}</b> | 本月：<b>{monthlyIncome.ToString("N2")}</b> | 今日：<b>{dailyIncome.ToString("N2")}</b>\n" +
    //$"USDT转出：<b>{usdtTotalOutcome.ToString("N2")}</b> | 本月：<b>{monthlyOutcome.ToString("N2")}</b> | 今日：<b>{dailyOutcome.ToString("N2")}</b>\n";
    $"累计收入：<b>{usdtTotalIncome.ToString("N2")}</b> | 支出：<b>{usdtTotalOutcome.ToString("N2")}</b> | 盈亏：<b>{usdtProfit.ToString("N2")}</b>\n" +    
    $"本月收入：<b>{monthlyIncome.ToString("N2")}</b> | 支出：<b>-{monthlyOutcome.ToString("N2")}</b> | 盈亏：<b>{monthlyProfit.ToString("N2")}</b>\n" +
    $"今日收入：<b>{dailyIncome.ToString("N2")}</b> | 支出：-<b>{dailyOutcome.ToString("N2")}</b> | 盈亏：<b>{dailyProfit.ToString("N2")}</b>"; 
    //$"USDT今日收入：<b>{dailyIncome.ToString("N2")}</b>\n" ;    
}
}  
else
{  
if (trxBalance < 100)
{
    resultText =  $"查询地址：<code>{tronAddress}</code>\n" +
    $"多签地址：<b>{addressPermissionText}</b>\n" +    
    $"注册时间：<b>{creationTime:yyyy-MM-dd HH:mm:ss}</b>\n" +
    $"最后活跃：<b>{lastTransactionTime:yyyy-MM-dd HH:mm:ss}</b>\n" +
    $"————————<b>资源</b>————————\n"+
    $"用户标签：<b>{userLabel} {userLabelSuffix}</b>\n" +
    $"交易次数：<b>{transactions} （ ↑{transactionsOut} _ ↓{transactionsIn} ）</b>\n" +        
    $"USDT余额：<b>{usdtBalance.ToString("N2")} ≈ {cnyBalance.ToString("N2")}元人民币</b>\n" +
    $"TRX余额：<b>{trxBalance.ToString("N2")}  |  TRX能量不足，请{exchangeLink}</b>\n" +
    $"免费带宽：<b>{remainingBandwidth.ToString("N0")}/{totalBandwidth.ToString("N0")}</b>\n" +
    $"质押带宽：<b>{netRemaining.ToString("N0")} / {netLimit.ToString("N0")}</b>\n" +
    $"质押能量：<b>{energyRemaining.ToString("N0")} / {energyLimit.ToString("N0")}</b>\n" +   
    $"累计兑换：<b>{usdtTotal.ToString("N2")} USDT</b>\n" +
    $"兑换次数：<b>{transferCount.ToString("N0")} 次</b>\n" +
    $"———————<b>USDT账单</b>———————\n" +
    $"{lastFiveTransactions}\n"+
    //$"USDT转入：<b>{usdtTotalIncome.ToString("N2")}</b> | 本月：<b>{monthlyIncome.ToString("N2")}</b> | 今日：<b>{dailyIncome.ToString("N2")}</b>\n" +
    //$"USDT转出：<b>{usdtTotalOutcome.ToString("N2")}</b> | 本月：<b>{monthlyOutcome.ToString("N2")}</b> | 今日：<b>{dailyOutcome.ToString("N2")}</b>\n";
    $"本月收入：<b>{monthlyIncome.ToString("N2")}</b> | 支出：<b>-{monthlyOutcome.ToString("N2")}</b> | 盈亏：<b>{monthlyProfit.ToString("N2")}</b>\n" +
    $"今日收入：<b>{dailyIncome.ToString("N2")}</b> | 支出：<b>-{dailyOutcome.ToString("N2")}</b> | 盈亏：<b>{dailyProfit.ToString("N2")}</b>\n\n" +    
    $"{groupExclusiveText}"; // 在这里使用 groupExclusiveText
    //$"USDT今日收入：<b>{dailyIncome.ToString("N2")}</b>\n" ;    
}
else
{
    resultText = $"查询地址：<code>{tronAddress}</code>\n" +
    $"多签地址：<b>{addressPermissionText}</b>\n" +    
    $"注册时间：<b>{creationTime:yyyy-MM-dd HH:mm:ss}</b>\n" +
    $"最后活跃：<b>{lastTransactionTime:yyyy-MM-dd HH:mm:ss}</b>\n" +
    $"————————<b>资源</b>————————\n"+
    $"用户标签：<b>{userLabel} {userLabelSuffix}</b>\n" +
    $"交易次数：<b>{transactions} （ ↑{transactionsOut} _ ↓{transactionsIn} ）</b>\n" +    
    $"USDT余额：<b>{usdtBalance.ToString("N2")} ≈ {cnyBalance.ToString("N2")}元人民币</b>\n" +
    $"TRX余额：<b>{trxBalance.ToString("N2")}  |  可供转账{availableTransferCount}次</b> \n" +
    $"免费带宽：<b>{remainingBandwidth.ToString("N0")}/{totalBandwidth.ToString("N0")}</b>\n" +
    $"质押带宽：<b>{netRemaining.ToString("N0")} / {netLimit.ToString("N0")}</b>\n" +
    $"质押能量：<b>{energyRemaining.ToString("N0")} / {energyLimit.ToString("N0")}</b>\n" +       
    $"累计兑换：<b>{usdtTotal.ToString("N2")} USDT</b>\n" +
    $"兑换次数：<b>{transferCount.ToString("N0")} 次</b>\n" +
    $"———————<b>USDT账单</b>———————\n" +
    $"{lastFiveTransactions}\n"+
    //$"USDT转入：<b>{usdtTotalIncome.ToString("N2")}</b> | 本月：<b>{monthlyIncome.ToString("N2")}</b> | 今日：<b>{dailyIncome.ToString("N2")}</b>\n" +
    //$"USDT转出：<b>{usdtTotalOutcome.ToString("N2")}</b> | 本月：<b>{monthlyOutcome.ToString("N2")}</b> | 今日：<b>{dailyOutcome.ToString("N2")}</b>\n";
    $"本月收入：<b>{monthlyIncome.ToString("N2")}</b> | 支出：<b>-{monthlyOutcome.ToString("N2")}</b> | 盈亏：<b>{monthlyProfit.ToString("N2")}</b>\n" +
    $"今日收入：<b>{dailyIncome.ToString("N2")}</b> | 支出：-<b>{dailyOutcome.ToString("N2")}</b> | 盈亏：<b>{dailyProfit.ToString("N2")}</b>\n\n" + 
    $"{groupExclusiveText}"; // 在这里使用 groupExclusiveText 
    //$"USDT今日收入：<b>{dailyIncome.ToString("N2")}</b>\n" ;    
}
}    


        // 创建内联键盘
   // string botUsername = "yifanfubot"; // 替换为你的机器人的用户名
   // string startParameter = ""; // 如果你希望机器人在被添加到群组时收到一个特定的消息，可以设置这个参数
   // string shareLink = $"https://t.me/{botUsername}?startgroup={startParameter}";

// 创建内联键盘
InlineKeyboardMarkup inlineKeyboard;
if (message.Chat.Type == ChatType.Private)
{
    inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new [] // 第一行按钮
        {
            //InlineKeyboardButton.WithCallbackData("监听此地址", $"绑定 {tronAddress}"), // 修改为CallbackData类型
            //InlineKeyboardButton.WithCallbackData("查授权记录", "query_eye"), // 修改为CallbackData类型
            InlineKeyboardButton.WithCallbackData("再查一次", $"query_again,{tronAddress}"), // 添加新的按钮
            InlineKeyboardButton.WithUrl("进群使用", shareLink) // 添加机器人到群组的链接		
        },
        new [] // 第二行按钮
        {
            //InlineKeyboardButton.WithCallbackData("再查一次", $"query_again,{tronAddress}"), // 添加新的按钮
            //InlineKeyboardButton.WithUrl("进群使用", shareLink) // 添加机器人到群组的链接
            InlineKeyboardButton.WithCallbackData("监听此地址", $"绑定 {tronAddress}"), // 修改为CallbackData类型
            InlineKeyboardButton.WithCallbackData("查授权记录", "query_eye"), // 修改为CallbackData类型		    
        },
        new [] // 第三行按钮
        {
            InlineKeyboardButton.WithCallbackData("TRX消耗统计", $"trx_usage,{tronAddress}"), // 添加新的按钮
	    InlineKeyboardButton.WithCallbackData("联系bot作者", "contactAdmin") // 修改为打开链接的按钮      	
        }	    
    });
}
else
{
    inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new [] // 第一行按钮
        {
            InlineKeyboardButton.WithUrl("详细信息", $"https://tronscan.org/#/address/{tronAddress}"), // 链接到Tron地址的详细信息
            InlineKeyboardButton.WithUrl("链上天眼", $"https://www.oklink.com/cn/trx/address/{tronAddress}"), // 链接到欧意地址的详细信息
           // InlineKeyboardButton.WithCallbackData("再查一次", $"query_again,{tronAddress}"), // 添加新的按钮
           // InlineKeyboardButton.WithUrl("进群使用", shareLink), // 添加机器人到群组的链接
            //InlineKeyboardButton.WithCallbackData("授权列表", $"authorized_list,{tronAddress}") // 添加新的按钮
        },
        new [] // 第二行按钮
        {
            InlineKeyboardButton.WithCallbackData("再查一次", $"query_again,{tronAddress}"), // 添加新的按钮
            //InlineKeyboardButton.WithCallbackData("监听此地址", $"绑定 {tronAddress}"), // 修改为CallbackData类型
	    InlineKeyboardButton.WithUrl("进群使用", shareLink) // 添加机器人到群组的链接	
        },
        new [] // 第三行按钮
        {
            InlineKeyboardButton.WithCallbackData("监听此地址", $"绑定 {tronAddress}"), // 修改为CallbackData类型
            InlineKeyboardButton.WithCallbackData("TRX消耗统计", $"trx_usage,{tronAddress}"), // 添加新的按钮
        },
        new [] // 第四行按钮
        {
            InlineKeyboardButton.WithCallbackData("完整授权列表", $"authorized_list,{tronAddress}"), // 添加新的按钮
            InlineKeyboardButton.WithCallbackData("联系bot作者", "contactAdmin") // 修改为打开链接的按钮      
        }	    
        
    });
}

    // 发送GIF和带按钮的文本
    string gifUrl = "https://i.postimg.cc/Jzrm1m9c/277574078-352558983556639-7702866525169266409-n.png";
    await botClient.SendPhotoAsync(
        chatId: message.Chat.Id,
        photo: new InputOnlineFile(gifUrl),
        caption: resultText, // 将文本作为图片说明发送
        parseMode: ParseMode.Html,
        replyMarkup: inlineKeyboard // 添加内联键盘
    );
}
// 查询带宽消耗
public static async Task<(decimal, decimal, decimal, decimal, decimal, decimal, decimal, decimal, decimal)> GetBandwidthUsageAsync(string tronAddress)
{
    string url = $"https://apilist.tronscanapi.com/api/account/analysis?address={tronAddress}&type=3"; // 注意这里的type=3
    try
    {
        using (HttpClient client = new HttpClient())
        {
            var response = await client.GetStringAsync(url);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<JsonElement>(response, options);
            var bandwidthData = JsonSerializer.Deserialize<List<BandwidthData>>(data.GetProperty("data").GetRawText(), options)?.OrderBy(d => d.day).ToList();

            if (bandwidthData == null || !bandwidthData.Any()) return (0, 0, 0, 0, 0, 0, 0, 0, 0);

            var yesterdayData = bandwidthData.TakeLast(1);
            var lastWeekData = bandwidthData.TakeLast(7);
            var lastMonthData = bandwidthData.TakeLast(30);

            decimal yesterdayNetUsage = yesterdayData.Sum(d => d.net_usage);
            decimal yesterdayNetBurn = yesterdayData.Sum(d => d.net_burn);
            decimal yesterdayNetUsageTotal = yesterdayData.Sum(d => d.net_usage_total);

            decimal lastWeekNetUsage = lastWeekData.Sum(d => d.net_usage);
            decimal lastWeekNetBurn = lastWeekData.Sum(d => d.net_burn);
            decimal lastWeekNetUsageTotal = lastWeekData.Sum(d => d.net_usage_total);

            decimal lastMonthNetUsage = lastMonthData.Sum(d => d.net_usage);
            decimal lastMonthNetBurn = lastMonthData.Sum(d => d.net_burn);
            decimal lastMonthNetUsageTotal = lastMonthData.Sum(d => d.net_usage_total);

            return (yesterdayNetUsage, yesterdayNetBurn, yesterdayNetUsageTotal, lastWeekNetUsage, lastWeekNetBurn, lastWeekNetUsageTotal, lastMonthNetUsage, lastMonthNetBurn, lastMonthNetUsageTotal);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching bandwidth usage: {ex.Message}");
        return (0, 0, 0, 0, 0, 0, 0, 0, 0);
    }
}
//查能量使用情况
public static async Task<(decimal, decimal, decimal, decimal, decimal, decimal, decimal, decimal, decimal)> GetEnergyUsageAsync(string tronAddress)
{
    string url = $"https://apilist.tronscanapi.com/api/account/analysis?address={tronAddress}&type=2"; // 注意这里的type=2
    try
    {
        using (HttpClient client = new HttpClient())
        {
            var response = await client.GetStringAsync(url);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<JsonElement>(response, options);
            var energyData = JsonSerializer.Deserialize<List<EnergyData>>(data.GetProperty("data").GetRawText(), options)?.OrderBy(d => d.day).ToList();

            if (energyData == null || !energyData.Any()) return (0, 0, 0, 0, 0, 0, 0, 0, 0);

            var yesterdayData = energyData.TakeLast(1);
            var lastWeekData = energyData.TakeLast(7);
            var lastMonthData = energyData.TakeLast(30);

            decimal yesterdayEnergyUsage = yesterdayData.Sum(d => d.energy_usage);
            decimal yesterdayEnergyBurn = yesterdayData.Sum(d => d.energy_burn);
            decimal yesterdayEnergyUsageTotal = yesterdayData.Sum(d => d.energy_usage_total);

            decimal lastWeekEnergyUsage = lastWeekData.Sum(d => d.energy_usage);
            decimal lastWeekEnergyBurn = lastWeekData.Sum(d => d.energy_burn);
            decimal lastWeekEnergyUsageTotal = lastWeekData.Sum(d => d.energy_usage_total);

            decimal lastMonthEnergyUsage = lastMonthData.Sum(d => d.energy_usage);
            decimal lastMonthEnergyBurn = lastMonthData.Sum(d => d.energy_burn);
            decimal lastMonthEnergyUsageTotal = lastMonthData.Sum(d => d.energy_usage_total);

            return (yesterdayEnergyUsage, yesterdayEnergyBurn, yesterdayEnergyUsageTotal, lastWeekEnergyUsage, lastWeekEnergyBurn, lastWeekEnergyUsageTotal, lastMonthEnergyUsage, lastMonthEnergyBurn, lastMonthEnergyUsageTotal);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching energy usage: {ex.Message}");
        return (0, 0, 0, 0, 0, 0, 0, 0, 0);
    }
}
public class EnergyData
{
    public string day { get; set; }
    public decimal energy_usage { get; set; }
    public decimal energy_burn { get; set; }
    public decimal energy_usage_total { get; set; }
}
public class BandwidthData
{
    public string day { get; set; }
    public decimal net_usage { get; set; }
    public decimal net_burn { get; set; }
    public decimal net_usage_total { get; set; }
}
//查询能量 带宽售价
public static async Task<(decimal burnEnergyCost, decimal burnNetCost)> GetAcquisitionCostAsync()
{
    string url = "https://apilist.tronscanapi.com/api/acquisition_cost_statistic?limit=1";
    try
    {
        using (HttpClient client = new HttpClient())
        {
            var response = await client.GetStringAsync(url);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<JsonElement>(response, options);
            var costData = data.GetProperty("data").EnumerateArray().FirstOrDefault();

            decimal burnEnergyCost = costData.GetProperty("burn_energy_cost").GetDecimal();
            decimal burnNetCost = costData.GetProperty("burn_net_cost").GetDecimal();

            return (burnEnergyCost, burnNetCost);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching acquisition cost: {ex.Message}");
        // 如果API调用失败，返回默认的能量和带宽售价
        return (0.00042M, 0.001M);
    }
}								 
public static async Task<(decimal UsdtTotal, int TransferCount, bool IsError)> GetUsdtTransferTotalAsync(string fromAddress, string toAddress)
{
    try
    {
        var apiUrl = $"https://api.trongrid.io/v1/accounts/{toAddress}/transactions/trc20?only_confirmed=true&limit=200&token_id=TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t";
        using var httpClient = new HttpClient();

        var usdtTotal = 0m;
        var transferCount = 0;

        while (true)
        {
            var response = await httpClient.GetAsync(apiUrl);
            var json = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(json);

            if (!jsonDocument.RootElement.TryGetProperty("data", out JsonElement dataElement))
            {
                break;
            }

            foreach (var transactionElement in dataElement.EnumerateArray())
            {
                if (transactionElement.TryGetProperty("from", out JsonElement fromElement) && fromElement.GetString() == fromAddress)
                {
                    var value = transactionElement.GetProperty("value").GetString();
                    usdtTotal += decimal.Parse(value) / 1_000_000; // 假设USDT有6位小数
                    transferCount++;  // 当找到符合条件的转账时，计数器加一
                }
            }

            if (!jsonDocument.RootElement.TryGetProperty("has_next", out JsonElement hasNextElement) || !hasNextElement.GetBoolean())
            {
                break;
            }

            apiUrl = $"https://api.trongrid.io/v1/accounts/{toAddress}/transactions/trc20?only_confirmed=true&limit=200&token_id=TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t&fingerprint={jsonDocument.RootElement.GetProperty("fingerprint").GetString()}";
        }

        return (usdtTotal, transferCount, false);
    }
    catch (Exception ex)
    {
        // 当发生异常时，返回一个特殊的结果，表示发生了错误
        Console.WriteLine($"Error in method {nameof(GetUsdtTransferTotalAsync)}: {ex.Message}");
        return (0, 0, true);
    }
}
private static readonly List<string> CurrencyOrder = new List<string>
{
    "CNY","USD", "HKD", "TWD", "JPY", "GBP", "EUR", "AUD", "KRW", "THB", "VND",
    "LAK", "MMK", "INR", "CHF", "NZD", "SGD", "KHR", "PHP", "MXN", "AED",
    "RUB", "CAD", "MYR", "KWD"
};
public class ExchangeRateData
{
    public string Base { get; set; }
    public Dictionary<string, decimal> Rates { get; set; }
}
private static async Task<string> GetExchangeRatesAsync(decimal amount, string baseCurrency, bool fullList = false)
{
    decimal usdtToCnyRate = await GetOkxPriceAsync("usdt", "cny", "sell");

    try
    {
        using (var httpClient = new HttpClient())
        {
            string apiUrl = $"https://api.exchangerate-api.com/v4/latest/{baseCurrency}";
            var response = await httpClient.GetAsync(apiUrl);
            string content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"获取汇率失败，状态码：{response.StatusCode}";
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var exchangeData = JsonSerializer.Deserialize<ExchangeRateData>(content, options);

            if (exchangeData == null || exchangeData.Rates == null)
            {
                return "无法获取汇率数据。";
            }

            StringBuilder result = new StringBuilder($"<b>{amount} {CurrencyMappings[baseCurrency].Name}兑换汇率 ≈</b>\n\n");

            // 计算并添加USDT汇率
            if (exchangeData.Rates.TryGetValue("CNY", out var cnyRate))
            {
                decimal amountInCny = amount * cnyRate;
                decimal amountInUsdt = amountInCny / usdtToCnyRate;
                result.AppendLine($"<code>{amountInUsdt.ToString("N2")} 泰达币(USDT)</code>\n————————————");
            }

            int count = 0;
            int totalRates = exchangeData.Rates.Count(r => CurrencyOrder.Contains(r.Key) && r.Key != baseCurrency);
            int ratesToShow = fullList ? totalRates : Math.Min(10, totalRates);

            foreach (var currencyCode in CurrencyOrder)
            {
                if (currencyCode == baseCurrency || !exchangeData.Rates.TryGetValue(currencyCode, out var rate)) // 跳过查询的货币本身和未找到汇率的货币
                {
                    continue;
                }

                decimal convertedAmount = amount * rate;
                if (CurrencyMappings.TryGetValue(currencyCode, out var currencyInfo))
                {
                    count++;
                    result.Append($"<code>{convertedAmount.ToString("N2")} {currencyInfo.Name} ({currencyCode})</code>");
                    if (count < ratesToShow) // 如果当前条目不是最后一个，则添加横线
                    {
                        result.AppendLine("\n————————————");
                    }
                    else
                    {
                        result.AppendLine(); // 最后一个条目后不添加横线，只换行
                    }
                    if (!fullList && count >= 10) break; // 如果不是请求完整列表且已添加10条数据，则停止添加
                }
            }

            return result.ToString();
        }
    }
    catch (Exception ex)
    {
        return $"在获取汇率时发生错误：{ex.Message}";
    }
}
private static readonly Dictionary<string, string> CurrencyAliases = new Dictionary<string, string>
{
    {"元", "CNY"},
    {"块", "CNY"},
    {"美金", "USD"},	
    {"法郎", "CHF"},	
    {"新币", "SGD"},
    {"瑞尔", "KHR"},	
    {"柬币", "KHR"},    
    {"迪拉姆", "AED"},	
    {"卢布", "RUB"},	
    {"披索", "PHP"},
    {"比索", "MXN"},    
    {"马币", "MYR"},	
    {"第纳尔", "KWD"},	
    {"卢比", "INR"}	
};
private static readonly Dictionary<string, (string Name, string Symbol)> CurrencyMappings = new Dictionary<string, (string, string)>
{
    {"CNY", ("人民币", "¥")},
    {"USD", ("美元", "$")},
    {"HKD", ("港币", "HK$")},
    {"TWD", ("台币", "NT$")},
    {"JPY", ("日元", "¥")},
    {"GBP", ("英镑", "£")},
    {"EUR", ("欧元", "€")},
    {"AUD", ("澳元", "A$")},
    {"KRW", ("韩元", "₩")},
    {"THB", ("泰铢", "฿")},
    {"VND", ("越南盾", "₫")},
    {"LAK", ("老挝币", "₭")},
    {"MMK", ("缅甸币", "K")},
    {"INR", ("印度卢比", "₹")},
    {"CHF", ("瑞士法郎", "Fr")},
    {"NZD", ("新西兰元", "NZ$")},
    {"SGD", ("新加坡新元", "S$")},
    {"KHR", ("柬埔寨瑞尔", "៛")},
    {"PHP", ("菲律宾披索", "₱")},
    {"MXN", ("墨西哥比索", "$")},
    {"AED", ("迪拜迪拉姆", "د.إ")},
    {"RUB", ("俄罗斯卢布", "₽")},
    {"CAD", ("加拿大加元", "C$")},
    {"MYR", ("马来西亚币", "RM")},
    {"KWD", ("科威特第纳尔", "KD")}
};    
private static readonly Dictionary<string, string> CurrencyFullNames = new Dictionary<string, string>
{
    { "USD", "美元" },
    { "HKD", "港币" },
    { "TWD", "台币" },
    { "JPY", "日元" },
    { "GBP", "英镑" },
    { "EUR", "欧元" },
    { "AUD", "澳元" },
    { "KRW", "韩元" },
    { "THB", "泰铢" },
    { "VND", "越南盾" },
    { "INR", "卢比" },
    { "SGD", "新币" },
    { "KHR", "瑞尔" },
    { "PHP", "披索" },
    { "AED", "迪拉姆" },
    { "LAK", "老挝币" },
    { "MMK", "缅甸币" },
    { "MYR", "马来西亚币" },
    { "KWD", "科威特第纳尔" },
    { "RUB", "俄罗斯卢布" },
    { "CHF", "瑞士法郎" },
    { "CAD", "加拿大加元" },
    { "MXN", "墨西哥比索" },
    { "NZD", "新西兰元" },
};
static bool TryGetRateByCurrencyCode(Dictionary<string, (decimal, string)> rates, string currencyCode, out KeyValuePair<string, (decimal, string)> rate)
{
    foreach (var entry in rates)
    {
        if (entry.Key.Contains(currencyCode))
        {
            rate = entry;
            return true;
        }
    }

    rate = default;
    return false;
}
// 将 maxPage 提升为类的成员变量
private static int CalculateMaxPage(Dictionary<string, (decimal, string)> rates, int itemsPerPage)
{
    return (int)Math.Ceiling((double)rates.Count / itemsPerPage);
}
public static async Task HandleCurrencyRatesCommandAsync(ITelegramBotClient botClient, Message message, int page, bool updateMessage = false)
{
    var rates = await GetCurrencyRatesAsync();
    int itemsPerPage = 10; // 设置每页显示的条目数为10
    int maxPage = CalculateMaxPage(rates, itemsPerPage); // 计算最大页数
    var text = "<b>100元人民币兑换其他国家货币</b>:\n\n";

    int count = 0;

    foreach (var rate in rates.Skip((page - 1) * itemsPerPage).Take(itemsPerPage)) // 修改循环以只显示当前页的条目
    {
        decimal convertedAmount = rate.Value.Item1 * 100;
        decimal exchangeRate = 1 / rate.Value.Item1;
        text += $"<code>{rate.Key}: {convertedAmount:0.#####} {rate.Value.Item2}  汇率≈{exchangeRate:0.######}</code>\n";

        // 如果还有更多的汇率条目，添加分隔符
        if (count < itemsPerPage - 1)
        {
            text += "——————————————————————\n";
        }

        count++;
    }

    string botUsername = "yifanfubot"; // 替换为你的机器人的用户名
    string startParameter = ""; // 如果你希望机器人在被添加到群组时收到一个特定的消息，可以设置这个参数
    string shareLink = $"https://t.me/{botUsername}?startgroup={startParameter}";

    // 创建一个虚拟键盘
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new [] // 第一行按钮
        {
            InlineKeyboardButton.WithCallbackData("上一页", $"shangye_rate_{page}"),
            InlineKeyboardButton.WithCallbackData("下一页", $"xiaye_rate_{page}")
        },
        new [] // 第二行按钮
        {
            InlineKeyboardButton.WithUrl("分享到群组", shareLink)
        }
    });

    if (updateMessage)
    {
        await botClient.EditMessageTextAsync(
            chatId: message.Chat.Id,
            messageId: message.MessageId,
            text: text,
            parseMode: ParseMode.Html,
            disableWebPagePreview: true,
            replyMarkup: inlineKeyboard
        );
    }
    else
    {
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: text,
            parseMode: ParseMode.Html,
            disableWebPagePreview: true,
            replyMarkup: inlineKeyboard
        );
    }
}     
static async Task<Dictionary<string, (decimal, string)>> GetCurrencyRatesAsync()
{
    var apiUrl = "https://api.exchangerate-api.com/v4/latest/CNY"; // CNY为人民币代号

    using var httpClient = new HttpClient();
    HttpResponseMessage response;
    try
    {
        response = await httpClient.GetAsync(apiUrl);
    }
    catch (Exception)
    {
        Console.WriteLine("API异常，暂无法访问。");
        return new Dictionary<string, (decimal, string)>();
    }

    var json = await response.Content.ReadAsStringAsync();

    using var jsonDocument = JsonDocument.Parse(json);
    if (!jsonDocument.RootElement.TryGetProperty("rates", out JsonElement ratesElement))
    {
        throw new Exception("Rates property not found");
    }

    var rates = new Dictionary<string, (decimal, string)>();

    try
    {
        rates = new Dictionary<string, (decimal, string)>
        {
            { "美元 (USD)", (ratesElement.GetProperty("USD").GetDecimal(), "$") },
            { "港币 (HKD)", (ratesElement.GetProperty("HKD").GetDecimal(), "HK$") },
            { "台币 (TWD)", (ratesElement.GetProperty("TWD").GetDecimal(), "NT$") },
            { "日元 (JPY)", (ratesElement.GetProperty("JPY").GetDecimal(), "¥") },
            { "英镑 (GBP)", (ratesElement.GetProperty("GBP").GetDecimal(), "£") },
            { "欧元 (EUR)", (ratesElement.GetProperty("EUR").GetDecimal(), "€") },
            { "澳元 (AUD)", (ratesElement.GetProperty("AUD").GetDecimal(), "A$") },
            { "韩元 (KRW)", (ratesElement.GetProperty("KRW").GetDecimal(), "₩") },
            { "泰铢 (THB)", (ratesElement.GetProperty("THB").GetDecimal(), "฿") },
            { "越南盾 (VND)", (ratesElement.GetProperty("VND").GetDecimal(), "₫") },
            { "老挝币 (LAK)", (ratesElement.GetProperty("LAK").GetDecimal(), "₭") },
            { "缅甸币 (MMK)", (ratesElement.GetProperty("MMK").GetDecimal(), "K") },       
            { "印度卢比 (INR)", (ratesElement.GetProperty("INR").GetDecimal(), "₹") },
            { "瑞士法郎 (CHF)", (ratesElement.GetProperty("CHF").GetDecimal(), "Fr") },   
            { "新西兰元 (NZD)", (ratesElement.GetProperty("NZD").GetDecimal(), "NZ$") },            
            { "新加坡新元 (SGD)", (ratesElement.GetProperty("SGD").GetDecimal(), "S$") },
            { "柬埔寨瑞尔 (KHR)", (ratesElement.GetProperty("KHR").GetDecimal(), "៛") },
            { "菲律宾披索 (PHP)", (ratesElement.GetProperty("PHP").GetDecimal(), "₱") },
            { "墨西哥比索 (MXN)", (ratesElement.GetProperty("MXN").GetDecimal(), "$") },            
            { "迪拜迪拉姆 (AED)", (ratesElement.GetProperty("AED").GetDecimal(), "د.إ") },     
            { "俄罗斯卢布 (RUB)", (ratesElement.GetProperty("RUB").GetDecimal(), "₽") },
            { "加拿大加元 (CAD)", (ratesElement.GetProperty("CAD").GetDecimal(), "C$") },
            { "马来西亚币 (MYR)", (ratesElement.GetProperty("MYR").GetDecimal(), "RM") },
            { "科威特第纳尔 (KWD)", (ratesElement.GetProperty("KWD").GetDecimal(), "KD") }
        };
    }
    catch (Exception)
    {
        Console.WriteLine("汇率数据异常，暂无法获取。");
        return new Dictionary<string, (decimal, string)>();
    }

    return rates;
}
//统计恐惧已贪婪指数
public static async Task<(int Today, int Yesterday, double Weekly, double Monthly)> GetFearAndGreedIndexAsync()
{
    var apiUrl = "https://api.alternative.me/fng/?limit=62&format=csv&date_format=cn";

    using (var httpClient = new HttpClient())
    {
        try
        {
            var response = await httpClient.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();

            var rawData = await response.Content.ReadAsStringAsync();

            var match = Regex.Match(rawData, @"\d{4}-\d{2}-\d{2}");
            if (!match.Success)
            {
                Console.WriteLine("Error: Cannot find the start of the data.");
                return (0, 0, 0, 0); // 返回默认值
            }

            var csvDataStartIndex = match.Index;
            var csvData = rawData.Substring(csvDataStartIndex);

            var rows = csvData.Split('\n');
            var dataList = new List<FngData>();

for (int i = 0; i < rows.Length; i++)
{
    var columns = rows[i].Split(',');

    if (columns.Length >= 3)
    {
        if (!DateTime.TryParse(columns[0], out var date))
        {
            Console.WriteLine($"Error: Cannot parse date '{columns[0]}'. Skipping this row.");
            continue;
        }

        if (!int.TryParse(columns[1], out var fngValue))
        {
            Console.WriteLine($"Error: Cannot parse FNG value '{columns[1]}'. Skipping this row.");
            continue;
        }

        dataList.Add(new FngData
        {
            Date = date,
            FngValue = fngValue,
            FngClassification = columns[2]
        });
    }
}

            var today = dataList[0].FngValue;
            var yesterday = dataList[1].FngValue;

// 计算上周和上月的日期范围
var endOfWeek = dataList[0].Date.AddDays(-((int)dataList[0].Date.DayOfWeek + 6) % 7);
var startOfWeek = endOfWeek.AddDays(-6);
var startOfMonth = dataList[0].Date.AddMonths(-1);
startOfMonth = new DateTime(startOfMonth.Year, startOfMonth.Month, 1);
var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            // 使用LINQ筛选满足日期范围的数据，并计算平均值
            var weeklyAverage = dataList.Where(d => d.Date >= startOfWeek && d.Date <= endOfWeek).Average(d => d.FngValue);
            var monthlyAverage = dataList.Where(d => d.Date >= startOfMonth && d.Date <= endOfMonth).Average(d => d.FngValue);

            return (today, yesterday, weeklyAverage, monthlyAverage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetFearAndGreedIndexAsync: {ex.Message}");
            return (0, 0, 0, 0); // 返回默认值
        }
    }
}

public class FngData
{
    public DateTime Date { get; set; }
    public int FngValue { get; set; }
    public string FngClassification { get; set; }
}
// 将 cryptoSymbols 提升为类的成员变量
private static string[] cryptoSymbols = new[] { 
    "tether", 
    "bitcoin", 
    "bitcoin-cash", 
    "ethereum", 
    "ethereum-classic", 
    "binancecoin", 
    "bitget-token", 
    "okb", 
    "huobi-token", 
    "the-open-network",    
    "ripple", 
    "cardano", 
    "uniswap", 
    "dogecoin", 
    "shiba-inu", 
    "solana", 
    "avalanche-2", 
    "litecoin", 
    "monero", 
    "chainlink"
};
static async Task<Message> SendCryptoPricesAsync(ITelegramBotClient botClient, Message message, int page = 1, bool editMessage = false)
{
    try
    {
        //var cryptoSymbols = new[] { "tether","bitcoin", "ethereum", "binancecoin","bitget-token", "okb","huobi-token","ripple", "cardano", "dogecoin","shiba-inu", "solana", "litecoin", "chainlink", "the-open-network" };

        var pageSize = 10; // 每页显示的数量
        var totalPages = (int)Math.Ceiling((double)cryptoSymbols.Length / pageSize); // 总页数
        page = Math.Max(1, Math.Min(page, totalPages)); // 确保页数在有效范围内

        // 同时开始三个任务
        var fearAndGreedIndexTask = GetFearAndGreedIndexAsync();
        var cryptoPricesTask = GetCryptoPricesAsync(cryptoSymbols);
        var usdtOtcPriceTask = GetOkxPriceAsync("usdt", "cny", "bank"); // 添加了这一行

        // 等待所有任务完成
        await Task.WhenAll(fearAndGreedIndexTask, cryptoPricesTask, usdtOtcPriceTask); // 修改了这一行

        // 获取任务的结果
        var (today, yesterday, weekly, monthly) = fearAndGreedIndexTask.Result;
        var (prices, changes) = cryptoPricesTask.Result;
        var usdtOtcPrice = usdtOtcPriceTask.Result; // 添加了这一行

        var cryptoNames = new Dictionary<string, string>
        {
            { "tether", "USDT" },
            { "bitcoin", "比特币" },
            { "bitcoin-cash", "比特现金" }, 
            { "ethereum", "以太坊" },
            { "ethereum-classic", "以太经典" },
            { "binancecoin", "币安币" },
            { "bitget-token", "币记-BGB" },  
            { "okb", "欧易-okb" }, 
            { "huobi-token", "火币积分-HT" }, 
            { "the-open-network", "电报币" },          
            { "ripple", "瑞波币" },
            { "cardano", "艾达币" },
            { "uniswap", "uni" },
            { "dogecoin", "狗狗币" },
            { "shiba-inu", "shib" },
            { "solana", "Sol" },
            { "avalanche-2", "AVAX" },
            { "litecoin", "莱特币" },
            { "monero", "门罗币" },
            { "chainlink", "link" }
        };

var text = "<b>币圈热门币种实时价格及恐惧与贪婪指数:</b>\n\n";

//var (today, yesterday, weekly, monthly) = await GetFearAndGreedIndexAsync();

Func<int, string> GetClassification = value =>
{
    if (value >= 0 && value <= 24)
        return "极度恐惧";
    if (value >= 25 && value <= 49)
        return "恐惧";
    if (value >= 50 && value <= 74)
        return "贪婪";
    return "极度贪婪";
};

text += $"今日：{today} {GetClassification(today)}     昨日：{yesterday} {GetClassification(yesterday)}\n";
text += $"上周：{weekly:0} {GetClassification((int)weekly)}     上月：{monthly:0} {GetClassification((int)monthly)}\n\n";

var startIndex = (page - 1) * pageSize;
var endIndex = Math.Min(startIndex + pageSize, cryptoSymbols.Length);        
for (int i = startIndex; i < endIndex; i++)
{
    var cryptoName = cryptoNames[cryptoSymbols[i]];
    var changeText = changes[i] < 0 ? $"<b>-</b>{Math.Abs(changes[i]):0.##}%" : $"<b>+</b>{changes[i]:0.##}%";
    var priceText = $"{prices[i]:0.######}"; // 定义 priceText 变量
    if (cryptoSymbols[i] == "tether") // 如果是 USDT
    {
        var usdtPrice = Math.Round(prices[i], 2); // 将价格四舍五入到两位小数
        priceText = $"{usdtPrice}≈{usdtOtcPrice} CNY"; // 更新 priceText 变量
    }
    else if (cryptoSymbols[i] == "shiba-inu") // 如果是 SHIB
    {
        var shibPrice = Math.Round(prices[i], 9); // 将价格四舍五入到九位小数
        priceText = $"{shibPrice}"; // 更新 priceText 变量
    }
    text += $"<code>{cryptoName}: ${priceText}  {changeText}</code>\n"; // 将 priceText 添加到消息文本中
    // 添加分隔符
    if (i < endIndex - 1) // 只在当前页的非最后一行添加分隔符
    {
        text += "———————————————\n";
    }
}

    // 创建包含三行，每行4个按钮的虚拟键盘
    var keyboard = new ReplyKeyboardMarkup(new[]
    {
        new [] // 第一行
        {
            new KeyboardButton("U兑TRX"),
            new KeyboardButton("实时汇率"),
            new KeyboardButton("查询余额"),
            new KeyboardButton("能量租赁"),
        },   
        new [] // 第二行
        {
            new KeyboardButton("外汇助手"),
            new KeyboardButton("加密货币"),
            new KeyboardButton("行情监控"),
            new KeyboardButton("地址监听"),
        },   
        new [] // 第三行
        {
            new KeyboardButton("财富密码"),
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("更多功能"),
        }
    });		

        keyboard.ResizeKeyboard = true; // 将键盘高度设置为最低
        keyboard.OneTimeKeyboard = false; // 添加这一行，确保虚拟键盘在用户与其交互后保持可见

// 创建内联按钮
var inlineKeyboard = new InlineKeyboardMarkup(new[]
{
    new [] // 第一行按钮
    {
        InlineKeyboardButton.WithCallbackData("上一页", $"shangyiye_{page - 1}"),
        InlineKeyboardButton.WithCallbackData("下一页", $"xiayiye_{page + 1}"),
    },
    new [] // 第2行按钮
    {
        InlineKeyboardButton.WithCallbackData("资金费率", $"zijinn"),	    
        InlineKeyboardButton.WithCallbackData("信号广场", $"bijiacha"),
    },
});

    if (editMessage)
    {
        // 使用 EditMessageTextAsync 方法编辑现有的消息
        return await botClient.EditMessageTextAsync(
            chatId: message.Chat.Id,
            messageId: message.MessageId,
            text: text, // 你可以将 'text' 替换为需要发送的文本
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard
        );
    }
    else
    {
        // 使用 SendTextMessageAsync 方法发送新的消息
        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: text, // 你可以将 'text' 替换为需要发送的文本
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard
        );
    }
    }
    catch (Exception ex)
    {
        Log.Logger.Error($"Error in SendCryptoPricesAsync: {ex.Message}");
        return null; // 当发生异常时，返回 null
    }
}

static async Task<(decimal[], decimal[])> GetCryptoPricesAsync(string[] symbols, int retryCount = 0)
{
    try
    {
        var apiUrl = "https://api.coingecko.com/api/v3/simple/price?ids=" + string.Join(",", symbols) + "&vs_currencies=usd&include_market_cap=false&include_24hr_vol=false&include_24hr_change=true&include_last_updated_at=false";

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(apiUrl);
        var json = await response.Content.ReadAsStringAsync();
        var prices = new decimal[symbols.Length];
        var changes = new decimal[symbols.Length];

        using var jsonDocument = JsonDocument.Parse(json);
        for (int i = 0; i < symbols.Length; i++)
        {
            if (jsonDocument.RootElement.TryGetProperty(symbols[i], out JsonElement symbolElement) &&
                symbolElement.TryGetProperty("usd", out JsonElement priceElement) &&
                symbolElement.TryGetProperty("usd_24h_change", out JsonElement changeElement))
            {
                prices[i] = priceElement.GetDecimal();
                changes[i] = changeElement.GetDecimal();
            }
            else
            {
                throw new Exception($"Error parsing JSON for symbol: '{symbols[i]}'");
            }
        }

        return (prices, changes);
    }
    catch (Exception ex)
    {
        Log.Logger.Error($"Error in GetCryptoPricesAsync: {ex.Message}");

        // 如果重试次数小于3，再次调用此方法
        if (retryCount < 3)
        {
            Log.Logger.Information("Retrying GetCryptoPricesAsync...");
            return await GetCryptoPricesAsync(symbols, retryCount + 1);
        }

        return (new decimal[0], new decimal[0]); // 当发生异常时，返回空数组
    }
}
public static async Task<decimal> GetOkxPriceAsync(string baseCurrency, string quoteCurrency, string method)
{
    var client = new HttpClient();

    var url = $"https://www.okx.com/v3/c2c/tradingOrders/books?quoteCurrency={quoteCurrency}&baseCurrency={baseCurrency}&side=sell&paymentMethod={method}&userType=blockTrade&showTrade=false&receivingAds=false&showFollow=false&showAlreadyTraded=false&isAbleFilter=false&urlId=2";

    HttpResponseMessage response;
    try
    {
        response = await client.GetAsync(url);
    }
    catch (Exception ex) // 修改了这里
    {
        Console.WriteLine($"API异常，暂无法访问。错误信息：{ex.Message}"); // 修改了这里
        return default; // 返回默认值（0）
    }

    if (response.IsSuccessStatusCode)
    {
        try
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(jsonString);

            if (doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("sell", out var sell))
            {
                var sellArray = sell.EnumerateArray();

                if (sellArray.MoveNext())
                {
                    var firstElement = sellArray.Current;

                    if (firstElement.TryGetProperty("price", out var price))
                    {
                        return decimal.Parse(price.GetString());
                    }
                }
            }
        }
        catch (Exception ex) // 修改了这里
        {
            Console.WriteLine($"获取价格数据异常。错误信息：{ex.Message}"); // 修改了这里
            return default; // 返回默认值（0）
        }
    }

    Console.WriteLine("无法从OKX API获取价格。");
    return default; // 返回默认值（0）
}

static async Task SendAdvertisementOnce(ITelegramBotClient botClient, CancellationToken cancellationToken, IBaseRepository<TokenRate> rateRepository, decimal FeeRate, long chatId)
{    
    // 获取大户持仓量多空比信息
static async Task<string> GetTopTradersRatio(string symbol)
{
    using (var httpClient = new HttpClient())
    {
        try
        {
            var topTradersResponse = await httpClient.GetAsync($"https://fapi.binance.com/futures/data/topLongShortPositionRatio?symbol={symbol}USDT&period=1h");
            var topTradersData = JsonSerializer.Deserialize<List<TopTradersRatio>>(await topTradersResponse.Content.ReadAsStringAsync());
            if (topTradersData != null && topTradersData.Any())
            {
                var latestData = topTradersData.Last();
                var longRatio = Math.Round(double.Parse(latestData.longAccount) * 100, 2);
                var shortRatio = Math.Round(double.Parse(latestData.shortAccount) * 100, 2);
                return $" {longRatio}% / {shortRatio}%";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error when calling API: {ex.Message}");
        }
    }
    return " 0% / 0%"; // 返回0%的多空比
}

    // 启动所有异步任务
    var btcTopTradersRatioTask = GetTopTradersRatio("BTC");
    var ethTopTradersRatioTask = GetTopTradersRatio("ETH");
    var rateTask = rateRepository.Where(x => x.Currency == Currency.USDT && x.ConvertCurrency == Currency.TRX).FirstAsync(x => x.Rate);
    var fearAndGreedIndexTask = GetFearAndGreedIndexAsync();
    var cryptoPricesTask = GetCryptoPricesAsync(new[] { "bitcoin", "ethereum" });
    var currencyRatesTask = GetCurrencyRatesAsync();
    var okxPriceTask = GetOkxPriceAsync("USDT", "CNY", "all");

    // 等待所有任务完成
    await Task.WhenAll(btcTopTradersRatioTask, ethTopTradersRatioTask, rateTask, fearAndGreedIndexTask, cryptoPricesTask, currencyRatesTask, okxPriceTask);

    // 获取所有任务的结果
    var btcTopTradersRatio = await btcTopTradersRatioTask;
    var ethTopTradersRatio = await ethTopTradersRatioTask;
    var rate = await rateTask;
    var (today, yesterday, weekly, monthly) = await fearAndGreedIndexTask;
    var (prices, changes) = await cryptoPricesTask;
    var currencyRates = await currencyRatesTask;
    var okxPrice = await okxPriceTask;

    string GetFearGreedDescription(int value)
    {
        if (value >= 0 && value <= 24)
            return "极度恐惧";
        if (value >= 25 && value <= 49)
            return "恐惧";
        if (value >= 50 && value <= 74)
            return "贪婪";
        return "极度贪婪";
    }

    string fearGreedDescription = GetFearGreedDescription(today);        
    decimal usdtToTrx = 100m.USDT_To_TRX(rate, FeeRate, 0);
    var bitcoinPrice = prices[0];
    var ethereumPrice = prices[1];
    var bitcoinChange = changes[0];
    var ethereumChange = changes[1];

    if (!currencyRates.TryGetValue("美元 (USD)", out var usdRateTuple)) 
    {
        Console.WriteLine("Could not find USD rate in response.");
        return; // 或者你可以选择继续，只是不显示美元汇率
    }
    var usdRate = 1 / usdRateTuple.Item1;
        
        string channelLink = "tg://resolve?domain=yifanfu"; // 使用 'tg://' 协议替换为你的频道链接
string advertisementText = $"—————————<b>合约大数据</b>—————————\n" +
    $"<b>\U0001F4B0 美元汇率参考 ≈ {usdRate:#.####}</b>\n" +
    $"<b>\U0001F4B0 USDT实时OTC价格 ≈ {okxPrice} CNY</b>\n" +
    $"<b>\U0001F4B0 专属兑换汇率：100 USDT = {usdtToTrx:#.####} TRX</b>\n\n" +
    $"<code>\U0001F4B8 币圈今日恐惧与贪婪指数：{today} {fearGreedDescription}</code>\n" +                 
    $"<code>\U0001F4B8 比特币价格 ≈ {bitcoinPrice} USDT    {(bitcoinChange >= 0 ? "+" : "")}{bitcoinChange:0.##}% </code>\n" +
    //$"<code>\U0001F4B8 比特币合约多空比：{btcTopTradersRatio}</code>\n" +
    $"<code>\U0001F4B8 以太坊价格 ≈ {ethereumPrice} USDT  {(ethereumChange >= 0 ? "+" : "")}{ethereumChange:0.##}% </code>\n" +
    $"<code>\U0001F4B8 比特币合约多空比：{btcTopTradersRatio}</code>\n" +    
    $"<code>\U0001F4B8 以太坊合约多空比：{ethTopTradersRatio}</code>\n";
    //$"<code>\U0001F4B8 全网24小时合约爆仓 ≈ {h24TotalVolUsd:#,0} USDT</code>\n" +     
   // $"<code>\U0001F4B8 以太坊1小时合约： {ethLongRate:#.##}% 做多  {ethShortRate:#.##}% 做空</code>\n" +
   // $"<code>\U0001F4B8 比特币24小时合约：{btcLongRate:#.##}% 做多  {btcShortRate:#.##}% 做空</code>\n" ;
            
            
string botUsername = "yifanfubot"; // 替换为你的机器人的用户名
string startParameter = ""; // 如果你希望机器人在被添加到群组时收到一个特定的消息，可以设置这个参数
string shareLink = $"https://t.me/{botUsername}?startgroup={startParameter}";

// 你想要发送的照片的URL或file_id
//string photoUrl = "https://i.postimg.cc/jjK3vbsS/What-is-Bitcoin-Cash.jpg"; // 替换为你的图片URL或file_id

// 创建 InlineKeyboardButton 并设置文本和回调数据
var visitButton1 = new InlineKeyboardButton("\U0000267B 进交流群")
{
    Url = "https://t.me/+b4NunT6Vwf0wZWI1" // 将此链接替换为你想要跳转的左侧链接
};

var shareToGroupButton = InlineKeyboardButton.WithUrl("\U0001F449 分享到群组 \U0001F448", shareLink);

// 创建 InlineKeyboardMarkup 并添加按钮
var inlineKeyboard = new InlineKeyboardMarkup(new[]
{
    new[] { visitButton1, shareToGroupButton }, // 一行按钮
});

// 发送带有说明的照片到指定的聊天
//await botClient.SendPhotoAsync(
//    chatId: chatId,
//    photo: photoUrl,
//    caption: advertisementText,
//    parseMode: ParseMode.Html,
//    replyMarkup: inlineKeyboard, // 使用新的inlineKeyboard对象
//    cancellationToken: cancellationToken);
    
    // 发送广告到指定的聊天
    await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: advertisementText,
        parseMode: ParseMode.Html,
        replyMarkup: inlineKeyboard, // 使用新的inlineKeyboard对象
        cancellationToken: cancellationToken);    
}

//获取24小时全网合约爆仓
private static async Task<decimal> GetH24TotalVolUsdAsync(string apiUrl, string apiKey)
{
    try
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("accept", "application/json");
        httpClient.DefaultRequestHeaders.Add("coinglassSecret", apiKey);

        var response = await httpClient.GetAsync(apiUrl);
        response.EnsureSuccessStatusCode();

        var jsonString = await response.Content.ReadAsStringAsync();
        var jsonObject = JObject.Parse(jsonString);

        return jsonObject["data"]["h24TotalVolUsd"].ToObject<decimal>();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"获取24小时交易量时发生异常：{ex.Message}");
        return 0;
    }
}
//获取24小时比特币合约
private static async Task<(decimal longRate, decimal shortRate)> GetH24LongShortAsync(string apiUrl, string apiKey)
{
    try
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("accept", "application/json");
        httpClient.DefaultRequestHeaders.Add("coinglassSecret", apiKey);

        var response = await httpClient.GetAsync(apiUrl);
        response.EnsureSuccessStatusCode();

        var jsonString = await response.Content.ReadAsStringAsync();
        var jsonObject = JObject.Parse(jsonString);

        var data = jsonObject["data"].FirstOrDefault(d => d["symbol"].ToString() == "BTC");
        if (data == null)
        {
            throw new Exception("BTC 数据在响应中未找到。");
        }

        decimal longRate = data["longRate"].ToObject<decimal>();
        decimal shortRate = data["shortRate"].ToObject<decimal>();

        return (longRate, shortRate);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"获取比特币24小时长短期利率时发生异常：{ex.Message}");
        return (0, 0);
    }
}
//获取以太坊1小时合约
private static async Task<(decimal longRate, decimal shortRate)> GetH1EthLongShortAsync(string apiUrl, string apiKey)
{
    try
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("accept", "application/json");
        httpClient.DefaultRequestHeaders.Add("coinglassSecret", apiKey);

        var response = await httpClient.GetAsync(apiUrl);
        response.EnsureSuccessStatusCode();

        var jsonString = await response.Content.ReadAsStringAsync();
        var jsonObject = JObject.Parse(jsonString);

        var data = jsonObject["data"].FirstOrDefault(d => d["symbol"].ToString() == "ETH");
        if (data == null)
        {
            throw new Exception("ETH 数据在响应中未找到。");
        }

        decimal longRate = data["longRate"].ToObject<decimal>();
        decimal shortRate = data["shortRate"].ToObject<decimal>();

        return (longRate, shortRate);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"获取以太坊1小时长短期利率时发生异常：{ex.Message}");
        return (0, 0);
    }
}
public static class GroupManager
{
    private static HashSet<long> groupIds = new HashSet<long>();
    public static HashSet<long> BlacklistedGroupIds = new HashSet<long>();
    static GroupManager()
    {
        // 添加初始群组 ID
        groupIds.Add(-1001862069013);  // 大号群ID
        //groupIds.Add(-917223865);  // 添加第二个初始群组 ID
    }

    public static IReadOnlyCollection<long> GroupIds => groupIds.ToList().AsReadOnly();

    public static void AddGroupId(long id)
    {
        // 只有当 ID 是负数时才将其添加到 groupIds 集合中
        if (id < 0)
        {
            groupIds.Add(id);
        }
    }

    public static void RemoveGroupId(long id)  // 这是新添加的方法
    {
        groupIds.Remove(id);
    }

    public static void ToggleAdvertisement(long groupId, bool enable)
    {
        if (enable)
        {
            AddGroupId(groupId);
        }
        else
        {
            RemoveGroupId(groupId);
        }
    }
}
// 添加一个类级别的变量来跟踪广告是否正在运行
private static bool isAdvertisementRunning = false;        
static async Task SendAdvertisement(ITelegramBotClient botClient, CancellationToken cancellationToken, IBaseRepository<TokenRate> rateRepository, decimal FeeRate)
{
  

    while (!cancellationToken.IsCancellationRequested)
    {
        var rate = await rateRepository.Where(x => x.Currency == Currency.USDT && x.ConvertCurrency == Currency.TRX).FirstAsync(x => x.Rate);
        decimal usdtToTrx = 100m.USDT_To_TRX(rate, FeeRate, 0);
        var (today, yesterday, weekly, monthly) = await GetFearAndGreedIndexAsync();
string GetFearGreedDescription(int value)
{
    if (value >= 0 && value <= 24)
        return "极度恐惧";
    if (value >= 25 && value <= 49)
        return "恐惧";
    if (value >= 50 && value <= 74)
        return "贪婪";
    return "极度贪婪";
}

string fearGreedDescription = GetFearGreedDescription(today);        
        // 获取比特币以太坊价格和涨跌幅
        var cryptoSymbols = new[] { "bitcoin", "ethereum" };
        var (prices, changes) = await GetCryptoPricesAsync(cryptoSymbols);
        var bitcoinPrice = prices[0];
        var ethereumPrice = prices[1];
        var bitcoinChange = changes[0];
        var ethereumChange = changes[1];
        // 获取美元汇率
        var currencyRates = await GetCurrencyRatesAsync();
        if (!currencyRates.TryGetValue("美元 (USD)", out var usdRateTuple)) 
        {
            Console.WriteLine("Could not find USD rate in response.");
            return; // 或者你可以选择继续，只是不显示美元汇率
        }
        var usdRate = 1 / usdRateTuple.Item1;
        decimal okxPrice = await GetOkxPriceAsync("USDT", "CNY", "all");
        
        string channelLink = "tg://resolve?domain=yifanfu"; // 使用 'tg://' 协议替换为你的频道链接
        string advertisementText = $"\U0001F4B9实时汇率：<b>100 USDT = {usdtToTrx:#.####} TRX</b>\n\n" +
            "机器人收款地址:\n (<b>点击自动复制</b>):<code>TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv</code>\n\n\n" + //手动输入地址
            "\U0000267B转U自动原地址返TRX,10U起兑!\n" +
            "\U0000267B请勿使用交易所或中心化钱包转账!\n" +
            $"\U0000267B有任何问题,请私聊联系<a href=\"{channelLink}\">机器人管理员</a>\n\n" +
            "<b>另代开TG会员</b>:\n\n" +
            "\u2708三月高级会员   24.99 u\n" +
            "\u2708六月高级会员   39.99 u\n" +
            "\u2708一年高级会员   70.99 u\n" +
            "(<b>需要开通会员请联系管理,切记不要转TRX兑换地址!!!</b>)\n" +  
            $"————————<b>其它汇率</b>————————\n" +
            $"<b>\U0001F4B0 美元汇率参考 ≈ {usdRate:#.####} </b>\n" +
            $"<b>\U0001F4B0 USDT实时OTC价格 ≈ {okxPrice} CNY</b>\n" +            
            $"<b>\U0001F4B0 比特币价格 ≈ {bitcoinPrice} USDT     {(bitcoinChange >= 0 ? "+" : "")}{bitcoinChange:0.##}% </b>\n" +
            $"<b>\U0001F4B0 以太坊价格 ≈ {ethereumPrice} USDT  {(ethereumChange >= 0 ? "+" : "")}{ethereumChange:0.##}% </b>\n" +
            $"<b>\U0001F4B0 币圈今日恐惧与贪婪指数：{today}  {fearGreedDescription}</b>\n" ;
            
            
string botUsername = "yifanfubot"; // 替换为你的机器人的用户名
string startParameter = ""; // 如果你希望机器人在被添加到群组时收到一个特定的消息，可以设置这个参数
string shareLink = $"https://t.me/{botUsername}?startgroup={startParameter}";

// 创建 InlineKeyboardButton 并设置文本和回调数据
var visitButton1 = new InlineKeyboardButton("\U0000267B 更多汇率")
{
    Url = "https://t.me/yifanfubot" // 将此链接替换为你想要跳转的左侧链接
};

var visitButton2 = new InlineKeyboardButton("\u2B50 会员代开")
{
    Url = "https://t.me/Yifanfu" // 将此链接替换为你想要跳转的右侧链接
};

var shareToGroupButton = InlineKeyboardButton.WithUrl("\U0001F449 分享到群组 \U0001F448", shareLink);

// 创建 InlineKeyboardMarkup 并添加按钮
var inlineKeyboard = new InlineKeyboardMarkup(new[]
{
    new[] { visitButton1, visitButton2 }, // 第一行按钮
    new[] { shareToGroupButton } // 第二行按钮
});

        try
        {
            // 用于存储已发送消息的字典
            var sentMessages = new Dictionary<long, Message>();
       
            // 遍历群组 ID 并发送广告消息
            var groupIds = GroupManager.GroupIds.ToList();
            foreach (var groupId in groupIds)
            {
                try
                {
                    Message sentMessage = await botClient.SendTextMessageAsync(groupId, advertisementText, parseMode: ParseMode.Html, replyMarkup: inlineKeyboard);
                    sentMessages[groupId] = sentMessage;
                }
                catch
                {
                    // 如果在尝试发送消息时出现错误，就从 groupIds 列表中移除这个群组
                    GroupManager.RemoveGroupId(groupId);
                    // 然后继续下一个群组，而不是停止整个任务
                    continue;
                }
            }

            // 等待10分钟
            await Task.Delay(TimeSpan.FromSeconds(600), cancellationToken);

            // 遍历已发送的消息并撤回
            foreach (var sentMessage in sentMessages)
            {
                await botClient.DeleteMessageAsync(sentMessage.Key, sentMessage.Value.MessageId);
            }

            // 等待5秒，再次发送广告
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
        catch (Exception ex)
        {
            // 发送广告过程中出现异常
            Console.WriteLine("Error in advertisement loop: " + ex.Message);

            // 等10秒重启广告服务
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }
}


    public static Task PollingErrorHandler(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Log.Error(exception, ErrorMessage);
        return Task.CompletedTask;
    }
    /// <summary>
    /// 处理更新
    /// </summary>
    /// <param name="botClient"></param>
    /// <param name="update"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
    var handler = update.Type switch
    {
        UpdateType.Message => BotOnMessageReceived(botClient, update.Message!),
        UpdateType.CallbackQuery => BotOnCallbackQueryReceived(botClient, update.CallbackQuery!),    
        UpdateType.MyChatMember => BotOnMyChatMemberChanged(botClient, update.MyChatMember!),    
        _ => UnknownUpdateHandlerAsync(botClient, update)
    };

    if (update.Type == UpdateType.Message)
    {
        var message = update.Message;

        // 当有新成员加入时
        if (message.NewChatMembers != null && message.NewChatMembers.Any())
        {
            foreach (var newMember in message.NewChatMembers)
            {
                // 直接调用 MonitorUsernameAndNameChangesAsync，将新成员资料存储起来
                await MonitorUsernameAndNameChangesAsync(botClient, new Message
                {
                    Chat = message.Chat,
                    From = newMember
                });
            }
        }
        else
        {
            AddFollower(message);
        }

        // ... 其他现有代码 ...
    }
if (update.Type == UpdateType.CallbackQuery)
{
    var callbackQuery = update.CallbackQuery;
    var callbackData = callbackQuery.Data;
    // 假设callbackData是"trx_usage,TRON_ADDRESS"的形式
    if (callbackData.StartsWith("trx_usage"))
    {
        var parts = callbackData.Split(',');
        if (parts.Length > 1)
        {
            var tronAddress = parts[1];
            // 首先回复正在统计的消息
            await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "正在统计，请稍后...", cancellationToken: cancellationToken);
		
        // 获取能量和带宽的售价
        var costTask = GetAcquisitionCostAsync();
    
        // 调用之前定义的方法获取带宽和能量数据
        var bandwidthUsageTask = GetBandwidthUsageAsync(tronAddress);
        var energyUsageTask = GetEnergyUsageAsync(tronAddress);

        // 等待所有任务完成
        await Task.WhenAll(costTask, bandwidthUsageTask, energyUsageTask);

        // 获取任务结果
        var (burnEnergyCost, burnNetCost) = costTask.Result;
        var (yesterdayNetUsage, yesterdayNetBurn, yesterdayNetUsageTotal, lastWeekNetUsage, lastWeekNetBurn, lastWeekNetUsageTotal, lastMonthNetUsage, lastMonthNetBurn, lastMonthNetUsageTotal) = bandwidthUsageTask.Result;
        var (yesterdayEnergyUsage, yesterdayEnergyBurn, yesterdayEnergyUsageTotal, lastWeekEnergyUsage, lastWeekEnergyBurn, lastWeekEnergyUsageTotal, lastMonthEnergyUsage, lastMonthEnergyBurn, lastMonthEnergyUsageTotal) = energyUsageTask.Result;

// 计算燃烧TRX的总和
var totalBurnedTrxYesterday = burnEnergyCost * yesterdayEnergyBurn + burnNetCost * yesterdayNetBurn;
var totalBurnedTrxLastWeek = burnEnergyCost * lastWeekEnergyBurn + burnNetCost * lastWeekNetBurn;
var totalBurnedTrxLastMonth = burnEnergyCost * lastMonthEnergyBurn + burnNetCost * lastMonthNetBurn;
// 定义固定能量单价
decimal fixedEnergyPrice = 0.00021875m;

// 计算现在的价格，为燃烧TRX获得的能量乘以优惠汇率，加上获得带宽燃烧了多少TRX
var currentPriceYesterday = Math.Round(fixedEnergyPrice * yesterdayEnergyBurn + burnNetCost * yesterdayNetBurn, 2);
var currentPriceLastWeek = Math.Round(fixedEnergyPrice * lastWeekEnergyBurn + burnNetCost * lastWeekNetBurn, 2);
var currentPriceLastMonth = Math.Round(fixedEnergyPrice * lastMonthEnergyBurn + burnNetCost * lastMonthNetBurn, 2);
		
            
            // 构建响应消息
            string resultText = $"地址：<code>{tronAddress}</code>\n\n" +
            $"<b>能量：</b>\n" +
            $"昨日能量消耗：总<b> {yesterdayEnergyUsageTotal}</b>\n" +
            $"燃烧 <b>{burnEnergyCost * yesterdayEnergyBurn}TRX </b>获得能量：<b>{yesterdayEnergyBurn}</b>  |  质押能量：<b>{yesterdayEnergyUsage}</b>\n\n" +
            $"近7天能量消耗：总<b> {lastWeekEnergyUsageTotal}</b>\n" +
            $"燃烧 <b>{burnEnergyCost * lastWeekEnergyBurn}TRX </b>获得能量：<b>{lastWeekEnergyBurn}</b>  |  质押能量：<b>{lastWeekEnergyUsage}</b>\n\n" +
            $"近30天能量消耗：总<b> {lastMonthEnergyUsageTotal}</b>\n" +
            $"燃烧 <b>{burnEnergyCost * lastMonthEnergyBurn}TRX </b>获得能量：<b>{lastMonthEnergyBurn}</b>  |  质押能量：<b>{lastMonthEnergyUsage}</b>\n" +
            "------------------------------------------------------------------\n" +
            $"<b>带宽：</b>\n" +
            $"昨日带宽消耗：总<b> {yesterdayNetUsageTotal}</b>\n" +
            $"燃烧 <b>{burnNetCost * yesterdayNetBurn}TRX </b>获得带宽：<b>{yesterdayNetBurn}</b>  |  免费带宽：<b>{yesterdayNetUsage}</b>\n\n" +
            $"近7天带宽消耗：总 <b>{lastWeekNetUsageTotal}</b>\n" +
            $"燃烧 <b>{burnNetCost * lastWeekNetBurn}TRX </b>获得带宽：<b>{lastWeekNetBurn}</b>  |  免费带宽：<b>{lastWeekNetUsage}</b>\n\n" +
            $"近30天带宽消耗：总<b> {lastMonthNetUsageTotal}</b>\n" +
            $"燃烧 <b>{burnNetCost * lastMonthNetBurn}TRX </b>获得带宽：<b>{lastMonthNetBurn}</b>  |  免费带宽：<b>{lastMonthNetUsage}</b>\n" +
	    "------------------------------------------------------------------\n" +	 
    $"<b>总计：</b>\n" +
    $"昨日转账消耗：<b>{Math.Round(totalBurnedTrxYesterday, 2)} TRX</b>\n" +
    $"近7天转账消耗：<b>{Math.Round(totalBurnedTrxLastWeek, 2)} TRX</b>\n" +
    $"近30天转账消耗：<b>{Math.Round(totalBurnedTrxLastMonth, 2)} TRX</b>\n\n" +	
    $"<b>通过提前租赁能量，可以节省大量TRX：</b>\n\n" +
    $"昨日转账消耗：<del>{Math.Round(totalBurnedTrxYesterday, 2)} TRX</del>  <b>现只需： {currentPriceYesterday} TRX</b>\n" +
    $"近7天转账消耗：<del>{Math.Round(totalBurnedTrxLastWeek, 2)} TRX</del>  <b>现只需： {currentPriceLastWeek} TRX</b>\n" +
    $"近30天转账消耗：<del>{Math.Round(totalBurnedTrxLastMonth, 2)} TRX</del>  <b>现只需： {currentPriceLastMonth} TRX</b>\n\n" +		    
            $"查询时间：<b>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</b>";
		
        // 创建内联键盘按钮
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new [] // first row
            {
                InlineKeyboardButton.WithCallbackData("能量介绍", "energy_intro"),
                InlineKeyboardButton.WithCallbackData("能量租赁", "contactAdmin"),
            }
        });
            // 发送统计完的消息
            await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, resultText, ParseMode.Html, replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
        }
    }
}	    
if (update.Type == UpdateType.CallbackQuery)
{
    var callbackQuery = update.CallbackQuery;
    var callbackData = callbackQuery.Data;
    Message fakeMessage = null; // 将 fakeMessage 的定义移到 switch 语句之前

    switch (callbackData)
    {
        case "show_user_info": // 处理新按钮的回调
            fakeMessage = new Message
            {
                Text = "/bangdingdizhi",
                Chat = callbackQuery.Message.Chat,
                From = callbackQuery.From
            };
            await BotOnMessageReceived(botClient, fakeMessage);
            break;
            
        case "show_group_info": // 处理群聊资料按钮的回调
            fakeMessage = new Message
            {
                Text = "/qunliaoziliao",
                Chat = callbackQuery.Message.Chat,
                From = callbackQuery.From
            };
            await BotOnMessageReceived(botClient, fakeMessage);
            break;       
        case "send_help": // 处理群聊资料按钮的回调
            fakeMessage = new Message
            {
                Text = "帮助",
                Chat = callbackQuery.Message.Chat,
                From = callbackQuery.From
            };
            await BotOnMessageReceived(botClient, fakeMessage);
            break;    
        case "zaicha": // 处理群聊资料按钮的回调
            fakeMessage = new Message
            {
                Text = "z0",
                Chat = callbackQuery.Message.Chat,
                From = callbackQuery.From
            };
            await BotOnMessageReceived(botClient, fakeMessage);
            break;   
        case "energy_intro": // 处理群聊资料按钮的回调
            fakeMessage = new Message
            {
                Text = "能量",
                Chat = callbackQuery.Message.Chat,
                From = callbackQuery.From
            };
            await BotOnMessageReceived(botClient, fakeMessage);
            break;              
        case "energy_introo": // 处理群聊资料按钮的回调
            fakeMessage = new Message
            {
                Text = "/ucard",
                Chat = callbackQuery.Message.Chat,
                From = callbackQuery.From
            };
            await BotOnMessageReceived(botClient, fakeMessage);
            break;    
	        case "绑定": // 处理群聊资料按钮的回调
            fakeMessage = new Message
            {
                Text = "绑定 tronAddress",
                Chat = callbackQuery.Message.Chat,
                From = callbackQuery.From
            };
            await BotOnMessageReceived(botClient, fakeMessage);
            break;  
	        case "zijinn": // 处理群聊资料按钮的回调
            fakeMessage = new Message
            {
                Text = "/zijin",
                Chat = callbackQuery.Message.Chat,
                From = callbackQuery.From
            };
            await BotOnMessageReceived(botClient, fakeMessage);
            break; 	
	        case "zhangdiee": // 处理群聊资料按钮的回调
            fakeMessage = new Message
            {
                Text = "/faxian",
                Chat = callbackQuery.Message.Chat,
                From = callbackQuery.From
            };
            await BotOnMessageReceived(botClient, fakeMessage);
            break; 	
	        case "jkbtcc": // 处理群聊资料按钮的回调
            fakeMessage = new Message
            {
                Text = "/jkbtc",
                Chat = callbackQuery.Message.Chat,
                From = callbackQuery.From
            };
            await BotOnMessageReceived(botClient, fakeMessage);
            break; 		    
	        case "bijiacha": // 处理群聊资料按钮的回调
            fakeMessage = new Message
            {
                Text = "/bijiacha",
                Chat = callbackQuery.Message.Chat,
                From = callbackQuery.From
            };
            await BotOnMessageReceived(botClient, fakeMessage);
            break; 		    
case "understandMultiSig": // 处理了解多签按钮的回调
    fakeMessage = new Message
    {
        Text = "多签",
        Chat = callbackQuery.Message.Chat,
        From = callbackQuery.From
    };
    await BotOnMessageReceived(botClient, fakeMessage);
    break;	
case "send_chinese": // 当用户点击“简体中文”按钮
    fakeMessage = new Message
    {
        Text = "中文",
        Chat = callbackQuery.Message.Chat,
        From = callbackQuery.From
    };
    await BotOnMessageReceived(botClient, fakeMessage);
    break;	
case "ExecuteZjdhMethod": // 当用户点击“简体中文”按钮
    fakeMessage = new Message
    {
        Text = "/zjdh",
        Chat = callbackQuery.Message.Chat,
        From = callbackQuery.From
    };
    await BotOnMessageReceived(botClient, fakeMessage);
    break;
case "chengdui": // 当用户点击“简体中文”按钮
    fakeMessage = new Message
    {
        Text = "/vip",
        Chat = callbackQuery.Message.Chat,
        From = callbackQuery.From
    };
    await BotOnMessageReceived(botClient, fakeMessage);
    break;	
case "shiyong": // 当用户点击“简体中文”按钮
    fakeMessage = new Message
    {
        Text = "/erc",
        Chat = callbackQuery.Message.Chat,
        From = callbackQuery.From
    };
    await BotOnMessageReceived(botClient, fakeMessage);
    break;	
case "send_huansuan": // 当用户点击“简体中文”按钮
    fakeMessage = new Message
    {
        Text = "/fu",
        Chat = callbackQuery.Message.Chat,
        From = callbackQuery.From
    };
    await BotOnMessageReceived(botClient, fakeMessage);
    break;			    
case "indexMarket": // 当用户点击“简体中文”按钮
    fakeMessage = new Message
    {
        Text = "/zhishu",
        Chat = callbackQuery.Message.Chat,
        From = callbackQuery.From
    };
    await BotOnMessageReceived(botClient, fakeMessage);
    break;		    
case "laoaomen": // 当用户点击“简体中文”按钮
    fakeMessage = new Message
    {
        Text = "/laoaomen",
        Chat = callbackQuery.Message.Chat,
        From = callbackQuery.From
    };
    await BotOnMessageReceived(botClient, fakeMessage);
    break;		    
case "xinaomen": // 当用户点击“简体中文”按钮
    fakeMessage = new Message
    {
        Text = "/xinaomen",
        Chat = callbackQuery.Message.Chat,
        From = callbackQuery.From
    };
    await BotOnMessageReceived(botClient, fakeMessage);
    break;		    
case "xianggang": // 当用户点击“简体中文”按钮
    fakeMessage = new Message
    {
        Text = "/xianggang",
        Chat = callbackQuery.Message.Chat,
        From = callbackQuery.From
    };
    await BotOnMessageReceived(botClient, fakeMessage);
    break;
case "lamzhishu": // 当用户点击“简体中文”按钮
    fakeMessage = new Message
    {
        Text = "/lamzhishu",
        Chat = callbackQuery.Message.Chat,
        From = callbackQuery.From
    };
    await BotOnMessageReceived(botClient, fakeMessage);
    break;			    
case "xamzhishu": // 当用户点击“简体中文”按钮
    fakeMessage = new Message
    {
        Text = "/xamzhishu",
        Chat = callbackQuery.Message.Chat,
        From = callbackQuery.From
    };
    await BotOnMessageReceived(botClient, fakeMessage);
    break;	
case "xgzhishu": // 当用户点击“简体中文”按钮
    fakeMessage = new Message
    {
        Text = "/xgzhishu",
        Chat = callbackQuery.Message.Chat,
        From = callbackQuery.From
    };
    await BotOnMessageReceived(botClient, fakeMessage);
    break;	
case "feixiaohao": // 处理市值TOP50大数据按钮的回调
    fakeMessage = new Message
    {
        Text = "/feixiaohao",
        Chat = callbackQuery.Message.Chat,
        From = callbackQuery.From
    };
    await BotOnMessageReceived(botClient, fakeMessage);
    break;
case "xiaohao": // 处理市值TOP50大数据按钮的回调
    fakeMessage = new Message
    {
        Text = "/xiaohao",
        Chat = callbackQuery.Message.Chat,
        From = callbackQuery.From
    };
    await BotOnMessageReceived(botClient, fakeMessage);
    break;		    
    case var callbackCommand when callbackCommand.StartsWith("unmonitor_"):
        var symbolToUnmonitor = callbackCommand.Substring("unmonitor_".Length);
        fakeMessage = new Message
        {
            Text = $"取消监控 {symbolToUnmonitor}",
            Chat = callbackQuery.Message.Chat,
            From = callbackQuery.From
        };
        await BotOnMessageReceived(botClient, fakeMessage);
        break;		    
        // 处理其他回调...
    }
}
if (update.Type == UpdateType.CallbackQuery)
{
    var callbackQuery = update.CallbackQuery;
    var callbackData = callbackQuery.Data;
    Message fakeMessage = null;

    if (callbackData.StartsWith("start_monitoring_"))
    {
        // 从回调数据中提取symbol
        var symbol = callbackData.Substring("start_monitoring_".Length);
        fakeMessage = new Message
        {
            Text = $"监控 {symbol}",
            Chat = callbackQuery.Message.Chat,
            From = callbackQuery.From
        };
        await BotOnMessageReceived(botClient, fakeMessage);
    }
    // ... 其他 case 处理逻辑 ...
}
if (update.Type == UpdateType.CallbackQuery)
{
    var callbackQuery = update.CallbackQuery;
    var callbackData = callbackQuery.Data.Split(',');
    if (callbackData[0] == "query")
    {
        // 从 CallbackData 中获取Tron地址
        var tronAddress = callbackData[1];

        // 调用 HandleQueryCommandAsync 方法来查询并返回结果
        await HandleQueryCommandAsync(botClient, new Message
        {
            Chat = callbackQuery.Message.Chat,
            Text = tronAddress
        });
    }
    // ... 其他现有代码 ...
}
//再查一次回调代码
//这是旧的
//if (update.Type == UpdateType.CallbackQuery)
//{
//    var callbackQuery = update.CallbackQuery;
//    var callbackData = callbackQuery.Data.Split(',');
//    if (callbackData[0] == "query_again")
//    {
//        // 从 CallbackData 中获取Tron地址
//        var tronAddress = callbackData[1];

//        // 调用 HandleQueryCommandAsync 方法来查询并返回结果
//        await HandleQueryCommandAsync(botClient, new Message
//        {
//            Chat = callbackQuery.Message.Chat,
//            Text = tronAddress
//        });
//    }
    // ... 其他现有代码 ...
//} 
if (update.Type == UpdateType.CallbackQuery)
{
    var callbackQuery = update.CallbackQuery;
    var callbackData = callbackQuery.Data.Split(',');
    switch (callbackData[0])
    {
        case "query_self":
        case "query_other":
            // 从 CallbackData 中获取Tron地址
            var tronAddress = callbackData[1];

            // 创建一个新的 Message 对象，并确保包含 From 属性
            var message = new Message
            {
                Chat = callbackQuery.Message.Chat,
                From = callbackQuery.From,
                Text = tronAddress
            };

            // 调用 HandleQueryCommandAsync 方法来查询并返回结果
            await HandleQueryCommandAsync(botClient, message);
            break;
        // ... 保留其他 case 分支不变 ...
    }
}        
//这是新的回调
if (update.Type == UpdateType.CallbackQuery)
{
    var callbackQuery = update.CallbackQuery;
    var callbackData = callbackQuery.Data.Split(',');
    if (callbackData[0] == "query_again")
    {
        // 从 CallbackData 中获取Tron地址
        var tronAddress = callbackData[1];

        // 创建一个新的 Message 对象，并确保包含 From 属性
        var message = new Message
        {
            Chat = callbackQuery.Message.Chat,
            From = callbackQuery.From, // 新增的：设置 From 属性
            Text = tronAddress
        };

        // 调用 HandleQueryCommandAsync 方法来查询并返回结果
        await HandleQueryCommandAsync(botClient, message);
    }
    else if (callbackData[0] == "authorized_list")
    {
        // 从 CallbackData 中获取Tron地址
        var tronAddress = callbackData[1];

        // 查询授权列表
        var authorizedListText = await GetUsdtAuthorizedListAsyncquanbu(tronAddress);

        // 分割授权列表文本，每5条记录为一组
        var authorizedListChunks = SplitIntoChunks(authorizedListText, 5);

        // 发送每一组授权记录
        foreach (var chunk in authorizedListChunks)
        {
            await botClient.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: chunk,
                disableWebPagePreview: true, // 这将关闭链接预览
                parseMode: ParseMode.Html
            );
        }
    }
   // else if (callbackData[0] == "query_detail")
   // {
   //     await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "查询详细信息为群聊查询特供，请在任意群组使用此功能！");
   // }
    else if (callbackData[0] == "query_eye")
    {
        string botUsername = "yifanfubot"; // 替换为你的机器人的用户名
        string startParameter = ""; // 如果你希望机器人在被添加到群组时收到一个特定的消息，可以设置这个参数
        string shareLink = $"https://t.me/{botUsername}?startgroup={startParameter}";

        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new [] // 第一行按钮
            {
                InlineKeyboardButton.WithUrl("点击拉我进群使用！", shareLink) // 添加机器人到群组的链接
            },
        });

        await botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message.Chat.Id,
            text: "查询波场地址授权记录为群聊查询特供，请在任意群组使用此功能！",
            replyMarkup: inlineKeyboard
        );
    } 
}
if (update.Type == UpdateType.CallbackQuery)
{
    var callbackQuery = update.CallbackQuery;
    var callbackData = callbackQuery.Data.Split(',');
    if (callbackData[0] == "set_note")
    {
        // 从 CallbackData 中获取Tron地址
        var tronAddress = callbackData[1];

        // 创建备注地址指令
        var message = "为您的每一个钱包设置单独的名字，方便您进行多钱包监听并识别：\n\n" +
              $"\U0001F4B3  |  <code>{tronAddress}</code>\n\n" +
              "<b>请先复制您的钱包地址 回复 如下消息 即可修改您的钱包地址备注：</b>\n\n" +
              $"如：<code>绑定 {tronAddress} 备注 地址1</code>";

        // 发送备注地址指令
        await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, message, ParseMode.Html);
    }
    // handle other buttons...
}	    
if (update.Type == UpdateType.CallbackQuery)
{
    var callbackQuery = update.CallbackQuery;
    var callbackData = callbackQuery.Data.Split(',');
    if (callbackData[0] == "full_rates")
    {
        // 从 CallbackData 中获取信息
        var cryptoPriceInCny = decimal.Parse(callbackData[1]);
        var amount = decimal.Parse(callbackData[2]);
        var currencyName = callbackData[3];
        var cryptoPriceInCnyOriginal = decimal.Parse(callbackData[4]);

        // 获取所有汇率
        var rates = await GetCurrencyRatesAsync();
        // 使用 amount 变量来动态生成消息
        var responseText = $"<b>{amount} 枚 {currencyName}</b> 的价值是：\n\n<code>{cryptoPriceInCnyOriginal:N2} 人民币 (CNY)</code>\n—————————————————\n";
        var rateList = rates.ToList();
        for (int i = 0; i < rateList.Count; i++)
        {
            var rate = rateList[i];
            var cryptoPriceInCurrency = cryptoPriceInCny * rate.Value.Item1;
            var currencyFullName = CurrencyFullNames.ContainsKey(rate.Key) ? CurrencyFullNames[rate.Key] : rate.Key;
            responseText += $"<code>{cryptoPriceInCurrency:N2} {currencyFullName}</code>";
            if (i != rateList.Count - 1)
            {
                responseText += "\n—————————————————\n";
            }
        }

        // 创建一个新的内联按钮
        var inlineKeyboardButton = InlineKeyboardButton.WithUrl("穿越牛熊，慢，就是快！", "https://t.me/+b4NunT6Vwf0wZWI1");
        var inlineKeyboard = new InlineKeyboardMarkup(new[] { inlineKeyboardButton });

        // 替换旧的消息，并添加新的内联按钮
        await botClient.EditMessageTextAsync(chatId: callbackQuery.Message.Chat.Id,
                                             messageId: callbackQuery.Message.MessageId,
                                             text: responseText,
                                             parseMode: ParseMode.Html,
                                             replyMarkup: inlineKeyboard);
    }
    else
    {
        await BotOnCallbackQueryReceived(botClient, callbackQuery);
    }
}
if (update.Type == UpdateType.CallbackQuery)
{
    var callbackQuery = update.CallbackQuery;
    var callbackData = callbackQuery.Data.Split(',');
    if (callbackData[0] == "full_ratess")
    {
        var amount = decimal.Parse(callbackData[1]);
        var currencyCode = callbackData[2];

        var exchangeRates = await GetExchangeRatesAsync(amount, currencyCode, true); // 请求完整的汇率表

        // 创建内联键盘按钮
        var keyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("联系作者", "contactAdmin"));

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: exchangeRates,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard // 添加这一行来设置键盘
        );
    }
    else
    {
        // 处理其他回调
    }
}	    
if (update.Type == UpdateType.CallbackQuery)
{
    var callbackQuery = update.CallbackQuery;
    var callbackData = callbackQuery.Data; // 这里的callbackData就是你之前设置的symbol

    // 调用你的查询函数来查询并返回结果
    await BotOnMessageReceived(botClient, new Message
    {
        Chat = callbackQuery.Message.Chat,
        From = callbackQuery.From,
        Text = callbackData
    });
}     
if (update.Type == UpdateType.CallbackQuery)
{
    var callbackQuery = update.CallbackQuery;
    var callbackData = callbackQuery.Data;

    try
    {
        if (callbackData.StartsWith("xiaye_rate_") || callbackData.StartsWith("shangye_rate_"))
        {
            var page = int.Parse(callbackData.Split('_')[2]);
            var rates = await GetCurrencyRatesAsync();
            int itemsPerPage = 10; // 设置每页显示的条目数为10
            int maxPage = CalculateMaxPage(rates, itemsPerPage); // 计算最大页数
            if (callbackData.StartsWith("xiaye_rate_"))
            {
                page++;
                if (page > maxPage) // 如果已经是最后一页
                {
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "已经是最后一页啦！");
                    return;
                }
            }
            else // 如果是 "shangye_rate_"
            {
                page--;
                if (page < 1) // 如果已经是第一页
                {
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "已经是第一页啦！");
                    return;
                }
            }

            // 更新消息内容而不是发送新的消息
            await HandleCurrencyRatesCommandAsync(botClient, callbackQuery.Message, page, true);
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id); // 关闭加载提示
        }
        else
        {
            switch (callbackData)
            {
                case "show_transaction_records":
                    await HandleTransactionRecordsCallbackAsync(botClient, callbackQuery);
                    break;
                // 其他回调处理...
            }
        }
    }
    catch (Exception ex)
    {
        if (ex.Message.Contains("message can't be edited"))
        {
            await botClient.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: "操作超时，请重新获取！"
            );
        }
        else
        {
            // 处理其他类型的异常
        }
    }
}     
if (update.Type == UpdateType.CallbackQuery)
{
    var callbackQuery = update.CallbackQuery;
    var callbackData = callbackQuery.Data;

    try
    {
        if (callbackData.StartsWith("shangyiye_") || callbackData.StartsWith("xiayiye_"))
        {
            var page = int.Parse(callbackData.Split('_')[1]);
            var totalPages = (int)Math.Ceiling((double)cryptoSymbols.Length / 10); // 总页数

            if (callbackData.StartsWith("shangyiye_"))
            {
                if (page > 0) // 修改了这里，确保页数大于0才能减少
                {
                    page--;
                }
                else
                {
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "已经是第一页啦！");
                    return;
                }
            }
            else // xiayiye
            {
                if (page <= totalPages) // 确保页数小于或等于总页数才能增加
                {
                    page++;
                }
                else
                {
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "已经是最后一页啦！");
                    return;
                }
            }

            // 更新消息内容而不是发送新的消息
            await SendCryptoPricesAsync(botClient, callbackQuery.Message, page, true);
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id); // 关闭加载提示
        }
        else
        {
            switch (callbackData)
            {
                case "show_transaction_records":
                    await HandleTransactionRecordsCallbackAsync(botClient, callbackQuery);
                    break;
                // 其他回调处理...
            }
        }
    }
    catch (Exception ex)
    {
        if (ex.Message.Contains("message can't be edited"))
        {
            await botClient.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: "操作超时，请重新获取！"
            );
        }
        else
        {
            // 处理其他类型的异常
        }
    }
} 
if (update.Type == UpdateType.CallbackQuery)
{
    var callbackQuery = update.CallbackQuery;
    var callbackData = callbackQuery.Data;

    try
    {
        if (callbackData.StartsWith("prev_page_") || callbackData.StartsWith("next_page_"))
        {
            var page = int.Parse(callbackData.Split('_')[2]);
            if (callbackData.StartsWith("prev_page_"))
            {
                if (page > 0)
                {
                    page--;
                }
                else
                {
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "已经是第一页啦！");
                    return;
                }
            }
            else // next_page
            {
                if (page < Followers.Count / 15) // 假设 Followers 是你的数据源
                {
                    page++;
                }
                else
                {
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "已经是最后一页啦！");
                    return;
                }
            }

            // 更新消息内容而不是发送新的消息
            await HandleGetFollowersCommandAsync(botClient, callbackQuery.Message, page, true);
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id); // 关闭加载提示
        }
        else
        {
            switch (callbackData)
            {
                case "show_full_list":
                    await HandleFullListCallbackAsync(botClient, callbackQuery);
                    break;
                case "show_transaction_recordds":
                    await HandleTransactionRecordsCallbackAsync(botClient, callbackQuery);
                    break;
                // 其他回调处理...
            }
        }
    }
    catch (Exception ex)
    {
        if (ex.Message.Contains("message can't be edited"))
        {
            await botClient.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: "操作超时，请重新获取！"
            );
        }
        else
        {
            // 处理其他类型的异常
        }
    }
}
        if (update.Type == UpdateType.Message)
    {
var message = update.Message;
if (message?.Text != null)
{
if (message.Text.StartsWith("/gzgzgz") && message.From.Id == AdminUserId)
{
    await HandleGetFollowersCommandAsync(botClient, message);
}
    
// 检查输入文本是否为 Tron 地址
var isTronAddress = Regex.IsMatch(message.Text, @"^(T[A-Za-z0-9]{33})$");
var addressLength = message.Text.Length;

// 检查地址长度是否大于10且小于33，或者大于33
var isInvalidLength = message.Text.StartsWith("T") && (addressLength > 20 && addressLength < 34 || addressLength > 34);

if (isTronAddress)
{
    await HandleQueryCommandAsync(botClient, message); // 当满足条件时，调用查询方法
}
else if (isInvalidLength)
{
    await botClient.SendTextMessageAsync(message.Chat.Id, "这好像是个波场TRC-20地址，长度不正确，请仔细检查！");
}
else
{
    // 在这里处理其他文本消息
}
}
        // 检查消息文本是否以 "转" 开头
        if (message?.Text != null && message.Text.StartsWith("转"))
        {
            await HandleTranslateCommandAsync(botClient, message); // 在这里处理翻译命令
        } 
else if (message?.Text != null && (message.Text.StartsWith("z0") || message.Text.StartsWith("zo")|| message.Text.StartsWith("shijian")|| message.Text.StartsWith("sj")))
{
    // 如果消息文本以 "z0" 开头，则不执行翻译
    return;
} 
else if (Regex.IsMatch(message?.Text ?? "", @"^[a-zA-Z0-9]{2,}\s+\d{4}/\d{2}/\d{2}\s+\d{2}\.\d{2}$"))
{
    // 如果消息文本符合数字货币+时间的格式，并且中间允许有多个空格，则不执行翻译
    return;
}		
else
{
// 检查用户是否在黑名单中
if (blacklistedUserIds.Contains(message.From.Id))
{
    return;
}    
    if (message != null && !string.IsNullOrWhiteSpace(message.Text))
    {
    // 检查群聊的翻译设置
    if ((message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup) && groupTranslationSettings.TryGetValue(message.Chat.Id, out var isTranslationEnabled) && !isTranslationEnabled)
    {
        return;
    }        
        var inputText = message.Text.Trim();
        // 添加新正则表达式以检查输入文本是否以 "绑定" 或 "解绑" 开头
        var isBindOrUnbindCommand = Regex.IsMatch(inputText, @"^(绑定|解绑|代绑|代解|添加群聊|回复|群发)");

        // 如果输入文本以 "绑定" 或 "解绑" 开头，则不执行翻译
        if (isBindOrUnbindCommand)
        {
            return;
        }  
// 添加新的正则表达式以检查输入文本是否包含 "用户名："
var containsUsername = Regex.IsMatch(inputText, @"用户名：");

// 如果输入文本包含 "用户名："，则不执行翻译
if (containsUsername)
{
    return;
}  
// 检查输入文本是否为数字（包括小数）加~或～的组合，例如 "55~23"、"55～23" 或 "0.12~0.15"
var isNumberRange = Regex.IsMatch(inputText, @"^\d+(\.\d+)?[~～]\d+(\.\d+)?$");
// 检查输入文本是否为以#开头的加密货币标识，例如 "#btc"
var isCryptoSymbol = Regex.IsMatch(inputText, @"^(#|查\s*)[a-zA-Z0-9]+$");
// 如果输入文本符合数字（包括小数）加~或～的组合，或者是以#开头的加密货币标识，则不执行翻译
if (isNumberRange || isCryptoSymbol)
{
    return;
}
// 如果输入文本符合数字（包括小数）加~或～的组合，则不执行翻译
if (isNumberRange)
{
    return;
}    
        
        // 添加新正则表达式以检查输入文本是否仅为 'id' 或 'ID'
        var isIdOrID = Regex.IsMatch(inputText, @"^\b(id|ID)\b$", RegexOptions.IgnoreCase);
        // 添加新正则表达式以检查输入文本是否包含 "查id"、"查ID" 或 "t.me/"
        var containsIdOrTme = Regex.IsMatch(inputText, @"查id|查ID|yhk|t\.me/", RegexOptions.IgnoreCase);

        // 如果输入文本包含 "查id"、"查ID" 或 "t.me/"，则不执行翻译
        if (containsIdOrTme)
        {
            return;
        }
        
        if (!string.IsNullOrWhiteSpace(inputText))
        {
            // 修改正则表达式以匹配带小数点的数字计算
            var containsKeywordsOrCommandsOrNumbersOrAtSign = Regex.IsMatch(inputText, @"^\/(start|yi|fan|qdgg|yccl|fu|btc|xamzhishu|xgzhishu|lamzhishu|music|usd|more|usdt|tron|z0|cny|trc|home|jiankong|caifu|help|qunliaoziliao|baocunqunliao|bangdingdizhi|zijin|faxian|chaxun|xuni|ucard|jisuzhangdie|bijiacha|jkbtc)|更多功能|人民币|能量租赁|实时汇率|U兑TRX|合约助手|查询余额|地址监听|加密货币|外汇助手|监控|汇率|^[\d\+\-\*/\.\s]+$|^@");

            // 检查输入文本是否为数字+货币的组合
            var isNumberCurrency = Regex.IsMatch(inputText, @"(^\d+\s*[A-Za-z\u4e00-\u9fa5]+$)|(^\d+(\.\d+)?(btc|比特币|eth|以太坊|usdt|泰达币|币安币|bnb|bgb|币记-BGB|okb|欧易-okb|ht|火币积分-HT|瑞波币|xrp|艾达币|ada|狗狗币|doge|shib|sol|莱特币|ltc|link|电报币|ton|比特现金|bch|以太经典|etc|uni|avax|门罗币|xmr)$)", RegexOptions.IgnoreCase);

            // 检查输入文本是否为纯中文文本带空格
            var isChineseTextWithSpaces = Regex.IsMatch(inputText, @"^[\u4e00-\u9fa5\s]+$");

            // 检查输入文本是否为 Tron 地址
            //var isTronAddress = Regex.IsMatch(inputText, @"^(T[A-Za-z0-9]{33})$");
	    var isTronAddress = Regex.IsMatch(inputText, @"^T[A-Za-z0-9]{20,}$");

            // 检查输入文本是否为币种
            var currencyNamesRegex = new Regex(@"(美元|港币|台币|日元|英镑|欧元|澳元|韩元|柬币|泰铢|越南盾|老挝币|缅甸币|印度卢比|瑞士法郎|新西兰元|新加坡新元|柬埔寨瑞尔|菲律宾披索|墨西哥比索|迪拜迪拉姆|俄罗斯卢布|加拿大加元|马来西亚币|科威特第纳尔|元|块|美金|法郎|新币|瑞尔|迪拉姆|卢布|披索|比索|马币|第纳尔|卢比|CNY|USD|HKD|TWD|JPY|GBP|EUR|AUD|KRW|THB|VND|LAK|MMK|INR|CHF|NZD|SGD|KHR|PHP|MXN|AED|RUB|CAD|MYR|KWD)", RegexOptions.IgnoreCase);		
            // 检查输入文本是否仅包含表情符号
            var isOnlyEmoji = EmojiHelper.IsOnlyEmoji(inputText);
            
            // 如果输入文本仅为 'id' 或 'ID'，则不执行翻译
            if (isIdOrID)
            {
                return;
            }

            if (!containsKeywordsOrCommandsOrNumbersOrAtSign && !isTronAddress && !isOnlyEmoji && !isNumberCurrency && !isChineseTextWithSpaces)
            {
// 检查输入文本是否包含货币的中文名称
var containsCurrencyName = currencyNamesRegex.IsMatch(inputText);

// 如果输入文本包含货币的中文名称，则不执行翻译
if (containsCurrencyName)
{
    return;
}		    
                // 检查输入文本是否包含任何非中文字符
                var containsNonChinese = Regex.IsMatch(inputText, @"[^\u4e00-\u9fa5]");
                // 添加新的正则表达式以检查输入文本是否只包含符号
                var isOnlySymbols = Regex.IsMatch(inputText, @"^[^\w\s]+$");
// 检查输入文本是否为 "拉黑 用户ID" 类型的文本
var isBlacklistCommand = Regex.IsMatch(inputText, @"^拉黑|拉白\s+\d+$");

// 如果输入文本为 "拉黑 用户ID" 类型的文本，则不执行翻译
if (isBlacklistCommand)
{
    return;
}                

                 // 如果输入文本仅包含符号，则不执行翻译
                if (isOnlySymbols)
                {
                   return;
                }

                if (containsNonChinese)
                {
                    var targetLanguage = "zh-CN"; // 将目标语言设置为简体中文
                    var (translatedText, _, isError) = await GoogleTranslateFree.TranslateAsync(inputText, targetLanguage); // 修改这里
                    if (isError) // 添加这个 if-else 语句
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "翻译服务异常，请稍后重试。");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"翻译结果：\n\n<code>{translatedText}</code>", parseMode: ParseMode.Html);
                    }
                }
            }
        }
    }
}
    }
if(update.CallbackQuery != null)
{
    try
    {
if(update.CallbackQuery.Data == "membershipOptions")
{
    var membershipKeyboard = new InlineKeyboardMarkup(new[]
    {
        new [] // 第一行按钮
        {
            InlineKeyboardButton.WithCallbackData("3个月会员    24.99 u", "3months"),
        },
        new [] // 第二行按钮
        {
            InlineKeyboardButton.WithCallbackData("6个月会员    39.99 u", "6months"),
        },
        new [] // 第三行按钮
        {
            InlineKeyboardButton.WithCallbackData("一年会员    70.99 u", "1year"),
        },
        new [] // 第四行按钮
        {
            InlineKeyboardButton.WithCallbackData("返回", "back"),
        }
    });

    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: "请选择会员期限：",
        replyMarkup: membershipKeyboard
    );
}
        else if (update.CallbackQuery.Data == "3months" || update.CallbackQuery.Data == "6months" || update.CallbackQuery.Data == "1year")
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new [] // 第一行按钮
                {
                    InlineKeyboardButton.WithUrl("支付成功", "https://t.me/yifanfu"),
                    InlineKeyboardButton.WithCallbackData("重新选择", "cancelPayment"),
                }
            });

            await botClient.EditMessageTextAsync(
                chatId: update.CallbackQuery.Message.Chat.Id,
                messageId: update.CallbackQuery.Message.MessageId,
                text: "<b>收款地址</b>：<code>TDqwLwzr12FZhQf2cyk14sGuRVkXGcpJrf</code>",
                parseMode: ParseMode.Html,
                replyMarkup: inlineKeyboard
            );
        }
        else if (update.CallbackQuery.Data == "cancelPayment")
        {
            var membershipKeyboard = new InlineKeyboardMarkup(new[]
            {
                new [] // 第一行按钮
                {
                    InlineKeyboardButton.WithCallbackData("3个月会员    24.99 u", "3months"),
                },
                new [] // 第二行按钮
                {
                    InlineKeyboardButton.WithCallbackData("6个月会员    39.99 u", "6months"),
                },
                new [] // 第三行按钮
                {
                    InlineKeyboardButton.WithCallbackData("一年会员    70.99 u", "1year"),
                },
                new [] // 第四行按钮
                {
                    InlineKeyboardButton.WithCallbackData("返回", "back"),
                }
            });

            await botClient.EditMessageTextAsync(
                chatId: update.CallbackQuery.Message.Chat.Id,
                messageId: update.CallbackQuery.Message.MessageId,
                text: "请选择会员期限：",
                replyMarkup: membershipKeyboard
            );
        }
else if(update.CallbackQuery.Data == "back")
{
    // 删除包含会员选项按钮的消息
    await botClient.DeleteMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        messageId: update.CallbackQuery.Message.MessageId
    );

    // 如果需要，可以在这里发送一个确认消息，告知用户已返回
    // 例如：
    // await botClient.SendTextMessageAsync(
    //     chatId: update.CallbackQuery.Message.Chat.Id,
    //     text: "已返回主菜单。"
    // );
}
else if(update.CallbackQuery.Data == "commandList")
{
    var commandListMessage = @"指令备忘录，帮助菜单里面有介绍的不再重复！

发送：<code>汇率+数字</code>（查手续费专用）
自动查询该汇率对应的手续费是多少，u价为实时价格。
例如发送：<code>汇率14</code>  自动计算返回：当汇率14时，手续费为 48.79%

在群里发送：<code>开启兑换通知</code>/<code>关闭兑换通知</code>
自动在本群开启或关闭机器人兑换账单播报！

在群里发送：<code>关闭键盘</code>
自动把机器人键盘收回去。

在群里发：<code>关闭翻译</code>/<code>开启翻译</code>（默认开启）
自动在本群停止翻译，发送外语不再自动翻译成中文！

发送 /bijiacha 自动查询币安所有现货/合约价格差
当价格出现偏差，意味着价格波动大，套利机会来临！

发送加密货币代码+时间 即可查询从查询时间到现在的涨跌幅：
如发送：<code>btc 2024/04/04 00.00</code>（发 <code>#btc</code> 查当前时间）
机器人自动计算从2024/04/04 00.00到现在比特币的涨跌幅情况！
发送：查+币种返回近1h/24h/7d数据，如发送：<code>查btc</code>

发送单个数字自带计算正负10%的涨跌幅；
发送两个数字（中间加~）直接返回二者的涨跌幅百分比：
如发送： <code> 1~2  </code>机器人计算并回复：从1到2，上涨 100%！
";

    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: commandListMessage,
        parseMode: ParseMode.Html
    );
}	    
else if(update.CallbackQuery.Data == "smsVerification")
{
    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: @"支持国外一切软件注册短信接码，可选国家！
支持国内部分软件注册短信接码，可选国家！
同时支持租赁/购买电报虚拟号码 +888号段！
如有需要，请点击下方按钮联系管理！",
        replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("联系管理", "contactAdmin"))
    );
}
else if (update.CallbackQuery.Data == "onlineAudio")
{
    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: @"欧乐网：https://www.olevod.com/
天天视频：https://www.ttsp.tv/index.html
泥视频：https://www.nivod4.tv/
fofo影院：https://www.fofoyy.com/
努努影院：https://nnyy.in/
爱看：https://www.ikan4k.com/
cn影院：https://cnys.tv/
茶杯狐电视电影推荐：https://cupfox.love/

在线音乐推荐使用洛雪播放器：https://lxmusic.toside.cn/download",
        disableWebPagePreview: true // 关闭链接预览
    );
}
else if (update.CallbackQuery.Data == "onlineReading")
{
    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: @"新闻类：
bbc：https://www.bbc.com/zhongwen/simp
纽约时报：https://cn.nytimes.com/
法广：https://www.rfi.fr/cn/
推特：https://twitter.com/home  

小说类：
笔趣阁：http://www.biquxs.com/
笔趣阁：http://www.biqu520.net/
笔趣阁：https://www.beqege.cc/

论坛类：
狮城bbs：https://www.shichengbbs.com/
柬埔寨通：http://www.jpztong.com/
柬单网：https://www.58cam.com/
老挝通：http://www.laowotong.com/
缅华网：http://mhwmm.com/
菲华网：https://www.phhua.com/
迪拜通：https://www.dubaichina.com/
泰国通：https://hua.in.th/portal.php
华人网：https://usa.huarenca.com/
	    ",
        disableWebPagePreview: true // 关闭链接预览
    );
}	    
else if (update.CallbackQuery.Data == "queryByColor")
{
    var colorResult = await FetchLotteryHistoryByColorAsync(HttpClientHelper.Client);

    // 创建内联键盘，添加“返回”按钮
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithCallbackData("返回", "back"), 
	InlineKeyboardButton.WithCallbackData("按生肖查询", "queryByZodiacc")	
    });

await botClient.SendTextMessageAsync(
    chatId: update.CallbackQuery.Message.Chat.Id,
        text: colorResult,
        parseMode: ParseMode.Html,
        replyMarkup: inlineKeyboard // 将内联键盘作为参数传递
    );
}
else if (update.CallbackQuery.Data == "queryByZodiacc")
{
    var zodiacResult = await FetchLotteryHistoryByZodiacAsync(HttpClientHelper.Client);

    // 创建内联键盘，添加“返回”和“按波色查询”按钮
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithCallbackData("返回", "back"),
        InlineKeyboardButton.WithCallbackData("按波色查询", "queryByColor")
    });

    // 发送生肖查询结果，并附带内联键盘
    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: zodiacResult,
        parseMode: ParseMode.Html,
        replyMarkup: inlineKeyboard // 将内联键盘作为参数传递
    );
}	    
else if (update.CallbackQuery.Data == "historyy")
{
    var historyResult = await FetchLotteryHistoryAsyncc(HttpClientHelper.Client);

    // 创建内联键盘，添加“按波色查询”按钮
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
	InlineKeyboardButton.WithCallbackData("返回", "back"),    
	InlineKeyboardButton.WithCallbackData("按生肖查询", "queryByZodiacc")  ,  
        InlineKeyboardButton.WithCallbackData("按波色查询", "queryByColor")
    });

await botClient.SendTextMessageAsync(
    chatId: update.CallbackQuery.Message.Chat.Id,
    text: historyResult,
    parseMode: ParseMode.Html,
    replyMarkup: inlineKeyboard // 将内联键盘作为参数传递
);
}	    
else if (update.CallbackQuery.Data == "randomSelection")
{
    // 生成随机号码
    var numbers = Enumerable.Range(1, 49).OrderBy(x => Guid.NewGuid()).Take(7).ToList();
    var regularNumbers = numbers.Take(6).OrderBy(n => n); // 平码，排序
    var specialNumber = numbers.Last(); // 特码

    var messageText = $"机选号码：{string.Join("  ", regularNumbers)}， {specialNumber}";

    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: messageText
    );
}
else if (update.CallbackQuery.Data == "queryByZodiac")
{
    int currentYear = DateTime.Now.Year;
    var zodiacResults = await LotteryFetcher.FetchLotteryZodiacHistoryAsync(currentYear);

    var messageText = string.Join("\n", zodiacResults);

    // 定义内联按钮
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithCallbackData("返回", "back"),
	InlineKeyboardButton.WithCallbackData("按波色查询", "queryByWave")	
    });

    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: messageText,
        parseMode: ParseMode.Html,
        replyMarkup: inlineKeyboard
    );
}	    
else if (update.CallbackQuery.Data == "history")
{
    int currentYear = DateTime.Now.Year;
    var historyResults = await LotteryFetcher.FetchLotteryHistoryAsync(currentYear);

    var messageText = string.Join("\n", historyResults);

    // 定义内联按钮，包括新的“按生肖查询”按钮
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithCallbackData("返回", "back"),
        InlineKeyboardButton.WithCallbackData("按波色查询", "queryByWave"),
        InlineKeyboardButton.WithCallbackData("按生肖查询", "queryByZodiac") // 新增按钮
    });

    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: messageText,
        parseMode: ParseMode.Html,
        replyMarkup: inlineKeyboard
    );
}
else if (update.CallbackQuery.Data == "queryByWave")
{
    int currentYear = DateTime.Now.Year;
    var waveResults = await LotteryFetcher.FetchLotteryWaveHistoryAsync(currentYear);

    var messageText = string.Join("\n", waveResults);

    // 定义内联按钮
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithCallbackData("返回", "back"),
	InlineKeyboardButton.WithCallbackData("按生肖查询", "queryByZodiac") // 新增按钮	
    });

    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: messageText,
        parseMode: ParseMode.Html,
        replyMarkup: inlineKeyboard
    );
}
else if (update.CallbackQuery.Data == "newQueryByZodiac")
{
    // 处理按生肖查询的回调，使用新澳门六合彩的数据
    int currentYear = DateTime.Now.Year;
    var zodiacResults = await NewLotteryFetcher.FetchLotteryZodiacHistoryAsync(currentYear);

    var messageText = string.Join("\n", zodiacResults);

    // 定义内联按钮
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithCallbackData("返回", "back"),
	InlineKeyboardButton.WithCallbackData("按波色查询", "newQueryByWave")	
    });

    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: messageText,
        parseMode: ParseMode.Html,
        replyMarkup: inlineKeyboard
    );
}
else if (update.CallbackQuery.Data == "newHistory")
{
    // 处理新澳门六合彩的历史开奖查询
    int currentYear = DateTime.Now.Year;
    var historyResults = await NewLotteryFetcher.FetchLotteryHistoryAsync(currentYear);

    var messageText = string.Join("\n", historyResults);

    // 定义内联按钮，包括新的“按生肖查询”和“按波色查询”按钮
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithCallbackData("返回", "back"),
        InlineKeyboardButton.WithCallbackData("按波色查询", "newQueryByWave"),
        InlineKeyboardButton.WithCallbackData("按生肖查询", "newQueryByZodiac")
    });

    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: messageText,
        parseMode: ParseMode.Html,
        replyMarkup: inlineKeyboard
    );
}
else if (update.CallbackQuery.Data == "newQueryByWave")
{
    // 处理按波色查询的回调，使用新澳门六合彩的数据
    int currentYear = DateTime.Now.Year;
    var waveResults = await NewLotteryFetcher.FetchLotteryWaveHistoryAsync(currentYear);

    var messageText = string.Join("\n", waveResults);

    // 定义内联按钮
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithCallbackData("返回", "back"),
	InlineKeyboardButton.WithCallbackData("按生肖查询", "newQueryByZodiac")	
    });

    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: messageText,
        parseMode: ParseMode.Html,
        replyMarkup: inlineKeyboard
    );
}	    
else if(update.CallbackQuery.Data == "fancyNumbers")
{
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new [] // 新增的按钮行
        {
            InlineKeyboardButton.WithCallbackData("了解多签", "understandMultiSig"),
            InlineKeyboardButton.WithCallbackData("联系管理", "contactAdmin")
        }
    });

    await botClient.SendPhotoAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        photo: "https://i.postimg.cc/rpg41NWV/photo-2023-05-03-14-15-51.jpg",
        caption: @"出售TRX靓号生成器： 本地生成 不保存秘钥 支持断网生成
同时支持直接购买 ：   尾号4连-5连-6连-7连-8连-9连-10连

【6连靓号】
所有号码50U一个

【7连靓号】
所有号码100U一个

【8连靓号】
200U    8位豹子【英文小写】
300U   8位豹子【英文大写】
500U   8位豹子【数字1.2.3.4.5】
666U   8位豹子【数字6.7.8.9】  
888U   8位顺子【步步高升号】
【顺子1-8 2-9】

【9连靓号】
3000U    9位豹子【英文小写】
4000U   9位豹子【英文大写】
6000U   9位豹子【数字1.2.3.4.5】  
8000U   9位豹子【数字6.7.8.9】
12000U   9位顺子【步步高升号】
【顺子1-9】

【10连靓号】
12000U   10位豹子【英文小写】
16000U  10位豹子【英文大写】
33000U  10位豹子【数字1.2.3.4.5】  
56000U  10位豹子【数字6.7.8.9】
88000U  10位顺子【步步高升号】
【顺子o-9】（波场没有数字0，o代替0）

购买之后，可联系管理协助变更地址权限，对地址进行多签！",
        replyMarkup: inlineKeyboard
    );
}
else if(update.CallbackQuery.Data == "memberEmojis")
{
    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: @"热门会员emoji表情包，点击链接即可添加：
	
1：热门：https://t.me/addemoji/yifanfu
2：热门：https://t.me/addemoji/YifanfuTGvip
3：合集：https://t.me/addemoji/Yifanfufacai
4：熊猫：https://t.me/addemoji/Yifanfupanda
5：米老鼠：https://t.me/addemoji/Yifanfumilaoshu
6：龙年特辑：https://t.me/addemoji/Yifanfu2024
7：币圈专用：https://t.me/addemoji/Yifanfubtc
8：qq经典表情：https://t.me/addemoji/Yifanfuqq
",
        disableWebPagePreview: true // 关闭链接预览
    );
}
else if(update.CallbackQuery.Data == "energyComparison")
{
    string comparisonText = @"<b>TRX/能量 消耗对比</b>
<code>
日转账10笔：
燃烧TRX：10*13.39=133.9 TRX消耗；
租赁能量：10*7=70 TRX消耗，立省63.9TRX！

日转账20笔：
燃烧TRX：20*13.39=267.8 TRX消耗；
租赁能量：20*7=140 TRX消耗，立省127.8TRX！

日转账50笔：
燃烧TRX：50*13.39=669.5 TRX消耗；
租赁能量：50*7=350 TRX消耗，立省319.5TRX！

日转账100笔：
燃烧TRX：100*13.39=1339 TRX消耗；
租赁能量：100*7=700 TRX消耗，立省639TRX！
</code>
<b>通过对比可以看出，每日转账次数越多，提前租赁能量就更划算！</b>
	    ";

    var comparisonKeyboard = new InlineKeyboardMarkup(new[]
    {
        new [] // 新增的按钮行
        {
            InlineKeyboardButton.WithCallbackData("立即租赁能量", "contactAdmin"),
        }
    });

    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: comparisonText,
        parseMode: ParseMode.Html, // 确保解析模式设置为HTML
        replyMarkup: comparisonKeyboard
    );
}
else if(update.CallbackQuery.Data == "contactAdmin")
{
    var contactKeyboard = new InlineKeyboardMarkup(new[]
    {
        new [] // 新增的按钮行
        {
            InlineKeyboardButton.WithUrl("直接联系作者", "https://t.me/yifanfu"),
            InlineKeyboardButton.WithCallbackData("由作者联系您", "authorContactRequest")
        }
    });

    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: "双向用户可以直接私聊机器人，作者会第一时间回复您！",
        replyMarkup: contactKeyboard
    );
}
else if(update.CallbackQuery.Data == "mingling" && update.CallbackQuery.From.Id == AdminUserId)
{
    string commandsText = @"拉黑 ID 或者 拉白 ID 可以将用户拉入黑名单或移出；
群发 +文本（（内容，链接）分行（内容，加粗）或（按钮，名字，链接或指令）） 机器人可以一键群发内容；
<code>开启广告</code> <code>关闭广告</code> 指定管理员才可以执行；
<code>开启兑换通知</code> <code>关闭兑换通知</code> 群内兑换通知开启关闭；
<code>开启翻译</code> <code>关闭翻译</code> 群内开启或关闭翻译功能；
添加群聊：群名字： 群ID： 群链接： 指令：开启/关闭 
储存群聊资料到仓库，指令为开启或关闭兑换通知；
代绑 ID 用户名（不用 @） 地址 备注  帮助用户绑定地址；
（发送仓库储存的用户地址可以批量代绑）
代解 ID 地址 帮助用户解除地址；
绑定地址后面加 TRX 不监控TRX余额；
发送：回复 群ID 内容 可以向指定群聊发文本
英文括号（内容，链接）中文括号（内容，加粗）中文括号（按钮，名称，链接或回调）末尾带置顶，可以尝试置顶；
机器人可以将用户的操作转发到指定群聊，在群里回复该信息，机器人可直接转发信息给用户。

启动机器人先：先开启保存群聊资料：<code>/baocunqunliao</code>
储存之前的用户资料 代绑地址
<code>/qdgg</code> 启动广告
<code>关闭翻译</code> <code>/xuni</code>
<code>监控 btc </code>可选
<code>监控 eth </code>可选

<code>绑定 TJ4c6esQYEM7jn5s8DD5zk2DBYJTLHnFR3 TRX 备注 安卓比特派</code>
<code>绑定 TWs6YaFusBbL6UYPjfK9XxpffNGCDu1ApF TRX 备注 安卓抹茶</code>
<code>绑定 TLowmih1pMgmeUGTAg3Z7Fdk1CZ5KP5ZgB TRX 备注 iOS抹茶</code>
<code>绑定 TDqwLwzr12FZhQf2cyk14sGuRVkXGcpJrf TRX 备注 飞机钱包</code>

";

    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: commandsText,
        parseMode: ParseMode.Html
    );
}
else if(update.CallbackQuery.Data == "shoucang")
{
    string favoriteLinks = @"<b>币圈：</b>
paxful：https://paxful.com/zh  （无需实名 otc交易）
remitano：https://remitano.com/r/cn
合约数据：https://www.coinglass.com/zh 
合约帝国际版：https://bitfrog.io/zh-CN/
波场手续费预估：https://tool.cha636.com/Home/Index3
混币器：https://fixedfloat.com/cn/
啄木鸟合约：https://thekingfisher.io/04hj
虚拟信用卡：https://card.onekey.so/
虚拟信用卡：https://dupay.one/zh-cn/index.html
货币交易/虚拟信用卡：https://www.chippay.com/
查币：https://www.dextools.io/app/cn/ether/pairs
tron：https://www.trongrid.io/
链上工具：https://cointool.app/approve/trx
查币工具：https://www.geckoterminal.com/eth/uniswap_v2/pools
波场地址转换：https://tron-converter.com/
btc-20：https://test.coinank.com/zh/ordinals/brc20
租能量：https://feee.io/
租能量官网：https://app.justlend.org/homeNew?lang=zh-TC
蚁穴工具：https://antcave.club/#term-149

<b>新闻：</b>
bbc：https://www.bbc.com/zhongwen/simp
纽约时报：https://cn.nytimes.com/
法广：https://www.rfi.fr/cn/
看中国：https://www.secretchina.com/
禁文网：https://www.bannedbook.org/
新品葱：https://pincong.rocks/
狮城：https://www.shichengbbs.com/
中国数字时代：https://chinadigitaltimes.net/chinese/404-articles-archive

<b>电影/音乐：</b>
洛雪：https://lxmusic.toside.cn/download
清风：https://www.vvvdj.com/
欧乐电影：https://www.olevod.com/
天天视频：https://www.ttsp.tv/
茶杯：https://cupfox.love/
泥视频：https://www.nivod4.tv/index.html
电视直播软件：https://www.ahhhhfs.com/36457/

<b>短信：</b>
接码：https://sms-activate.org/cn
接码：https://5sim.net/zh
接码：https://tiger-sms.com/cabinet/sms
接码网：https://www.w3h5.com/post/619.html
电子邮件生成：https://email-helper.vercel.app/
临时邮箱：https://linshiyouxiang.net/
临时邮箱：https://www.linshi-email.com/
地址生成：https://www.fakepersongenerator.com/
地址生成：https://www.meiguodizhi.com/

<b>工具：</b>
波场api文档：https://docs.tronscan.org/getting-started/api-keys
TRX api文档：https://developers.tron.network/reference/select-network
电报 api文档：https://core.telegram.org/bots/api#inline-mode
安卓苹果：https://www.krpano.tech/
视频转换：https://www.adobe.com/cn/
视频下载：https://www.freemake.com/cn/free_video_downloader/
diy表情：https://www.diydoutu.com/diy/doutu/340
翻转GIF：https://flipgif.imageonline.co/index-cn.php
视频加文字：https://www.67tool.com/video/edit/addText
剪映：https://www.capcut.cn/editor
文件处理：https://www.iloveimg.com/zh-cn
文件转换：https://ezgif.com/optimize?err=expired
GIF压缩：https://www.mnggiflab.com/product/gif-compress-v2
文件转换：https://convertio.co/zh/
vpn：https://vilavpn.com/?language=chinese
在线文字识别：https://ocr.wdku.net/
工具分享：https://www.ahhhhfs.com/
去水印：https://watermark.liumingye.cn/
推特视频下载：https://twitter.iiilab.com/
在线转换webm：https://www.video2edit.com/zh/convert-to-webm
tgs转GIF：https://www.emojibest.com/tgs-to-gif
文件转换：https://www.aconvert.com/video/gif-to-webm/
视频合并：https://cdkm.com/cn/merge-video
webm裁剪：https://online-video-cutter.com/cn/crop-video/webm
工具箱：https://tools.liumingye.cn/
poe gpt：https://poe.com/login
服务器：https://my.nextcli.com/index.php?rp=/store/hk-vps-linux
服务器：https://www.bwgyhw.cn/
服务器：https://manage.edisglobal.com/cart.php?gid=192&language=chinese
服务器：https://oneprovider.com/fr/configure/dediconf/2592
图片托管：https://postimg.cc/NLPvXFQ0/f01732c3  （机器人图片储存在此）
代码转换：http://www.esjson.com/utf8Encode.html
图片修改：https://www.gaitubao.com/
api大全：https://www.apispace.com/#/api/detail/?productID=89
韩小韩接口：https://api.vvhan.com/
大象工具：https://www.sunzhongwei.com/go/tools
机器人代码地址：https://github.com/xiaobai2023123412412343/CoinConvertBot/blob/master/wiki/manual_RUN.md";

    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: favoriteLinks,
        parseMode: ParseMode.Html,
        disableWebPagePreview: true // 这里关闭链接预览
    );
}
else if(update.CallbackQuery.Data == "authorContactRequest")
{
    string responseText;

    // 无论用户是否设置了用户名，都向管理员发送提示信息
    string adminMessage = $"有人需要帮助，用户名： @{update.CallbackQuery.From.Username ?? "未设置"} 用户ID： {update.CallbackQuery.From.Id}";
    await botClient.SendTextMessageAsync(
        chatId: AdminUserId, // 私聊ID
        text: adminMessage
    );

    // 检查用户是否有用户名
    if (string.IsNullOrEmpty(update.CallbackQuery.From.Username))
    {
        responseText = "操作失败，你还未设置用户名，请设置用户名后使用此功能！";
    }
    else
    {
        responseText = "收到请求，作者将很快联系您！";
    }

    // 回复用户
    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: responseText
    );
}
    }
    catch (Exception ex)
    {
        if (ex.Message.Contains("message can't be edited"))
        {
            await botClient.SendTextMessageAsync(
                chatId: update.CallbackQuery.Message.Chat.Id,
                text: "操作超时！请重新获取！"
            );
        }
        else
        {
            // 处理其他类型的异常
        }
    }
}       
    else if (update.Type == UpdateType.MyChatMember)
    {
        var chatMemberUpdated = update.MyChatMember;

        if (chatMemberUpdated.NewChatMember.Status == ChatMemberStatus.Member)
        {
            // 保存这个群组的ID
            GroupManager.AddGroupId(chatMemberUpdated.Chat.Id);
        }
        else if (chatMemberUpdated.NewChatMember.Status == ChatMemberStatus.Kicked || chatMemberUpdated.NewChatMember.Status == ChatMemberStatus.Left)  // 这是新添加的判断语句
        {
            // 如果机器人被踢出群组或者离开群组，我们移除这个群组的 ID
            GroupManager.RemoveGroupId(chatMemberUpdated.Chat.Id);
        }
    }

        try
        {
            await handler;
        }
catch (ApiRequestException apiEx) // 捕获 ApiRequestException 异常
{
    // 如果机器人没有发言权限
    if (apiEx.Message.Contains("not enough rights to send text messages to the chat"))
    {
        // 记录这些信息在服务器上
        Console.WriteLine($"在群里被禁言拉，指令不作处理！！！");
    }
}        
        catch (Exception exception)
        {
            Log.Error(exception, "呜呜呜，机器人输错啦~");
            await PollingErrorHandler(botClient, exception, cancellationToken);
        }
    }
    /// <summary>
    /// 消息接收
    /// </summary>
    /// <param name="botClient"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    private static async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
    {
        await HandleBlacklistAndWhitelistCommands(botClient, message);
        Log.Information($"Receive message type: {message.Type}");

    // 处理用户加入或离开群组的事件
    if (message.Type == MessageType.ChatMembersAdded || message.Type == MessageType.ChatMemberLeft)
    {
        await HandleUserJoinOrLeave(botClient, message);
    }
	    
    // 检查消息是否为图片类型
    if (message.Type == MessageType.Photo)
    {
        var caption = message.Caption;

        if (!string.IsNullOrEmpty(caption))
        {
            // 如果存在caption，输出到操作台
            Log.Information($"Photo caption: {caption}");

            // 创建一个模拟的文本消息，其内容为图片的caption
            var fakeMessage = new Message
            {
                Text = caption,
                Chat = message.Chat,
                From = message.From,
                Date = message.Date,
                MessageId = message.MessageId // 根据需要设置更多属性
            };

            // 使用模拟的文本消息调用BotOnMessageReceived方法
            await BotOnMessageReceived(botClient, fakeMessage);
        }
        else
        {
            // 如果不存在caption，输出提示信息
            Log.Information("图片没有附带文字");
        }
    }	    
     // 检查机器人是否被添加到新的群组
// 检查机器人是否被添加到新的群组
if (message.Type == MessageType.ChatMembersAdded)
{
    var me = await botClient.GetMeAsync();
    foreach (var newUser in message.NewChatMembers)
    {
        if (newUser.Id == me.Id)
        {
            // 发送欢迎消息
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "进群成功！请给予管理员权限以体验机器人完整功能！"
            );

            var chat = await botClient.GetChatAsync(message.Chat.Id);
            // 只有当群ID为负数时才保存
            if (chat.Id < 0)
            {
                // 检查是否已存在该群聊信息
                var existingGroupChat = GroupChats.FirstOrDefault(gc => gc.Id == chat.Id);
                if (existingGroupChat != null)
                {
                    // 如果已存在，则更新群聊信息
                    existingGroupChat.Title = chat.Title;
                    existingGroupChat.InviteLink = chat.InviteLink;
                }
                else
                {
                    // 如果不存在，则添加新的群聊信息
                    GroupChats.Add(new GroupChat { Id = chat.Id, Title = chat.Title, InviteLink = chat.InviteLink });
                }
            }
                // 自动将群组ID添加到兑换通知黑名单
                GroupManager.BlacklistedGroupIds.Add(chat.Id);
                await botClient.SendTextMessageAsync(chat.Id, "兑换通知已关闭。如需开启发送指令： 开启兑换通知");
		
            // 发送带有链接的文本消息
            string adminLink = "t.me/yifanfu"; // 管理员的Telegram链接
            string messageWithLink = "汇率表每10分钟更新发送一次！如需关闭请" + $"<a href=\"https://{adminLink}\">联系作者</a>！";
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: messageWithLink,
		disableWebPagePreview: true,    
                parseMode: ParseMode.Html // 确保解析模式设置为HTML以解析链接
            );
            // 向管理员发送群聊信息
            string adminMessage = $"机器人被拉到新群聊！\n\n群名：{chat.Title}\n群ID：{chat.Id}";
            await botClient.SendTextMessageAsync(
                chatId: AdminUserId, // 确保你已经设置了AdminUserId变量
                text: adminMessage
            );
            return;
        }
    }
}
        if (message.Text is not { } messageText)
            return;
        var scope = serviceScopeFactory.CreateScope();
        var provider = scope.ServiceProvider;
        var _myTronConfig = provider.GetRequiredService<IOptionsSnapshot<MyTronConfig>>();
        try
        {
            await InsertOrUpdateUserAsync(botClient, message);
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "更新Telegram用户信息失败！");
        }
    // 检查用户是否在黑名单中
    if (blacklistedUserIds.Contains(message.From.Id))
    {
        var userBehavior = userBehaviors.GetOrAdd(message.From.Id, new UserBehavior());
        if (userBehavior.UnbanTime.HasValue)
        {
            var timeLeft = userBehavior.UnbanTime.Value - DateTime.UtcNow;
            if (timeLeft > TimeSpan.Zero)
            {
                // 用户被自动拉黑，回复剩余时间
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"你已触发反高频行为，请在 <b>{timeLeft.Hours:00}:{timeLeft.Minutes:00}:{timeLeft.Seconds:00}</b> 后重试！", 
		    parseMode: ParseMode.Html	
                );
            }
            else
            {
                // 解禁时间已过，但由于某种原因未能自动移除黑名单，尝试移除
                blacklistedUserIds.Remove(message.From.Id);
                userBehavior.UnbanTime = null;
            }
        }
        else
        {
            // 用户是被管理员手动拉黑，回复通用消息
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "受限用户！"
            );
        }
        return;
    }
// 新增：检查用户行为是否触发提醒或拉黑	    
await CheckUserBehavior(botClient, message);	  
	    
// 将这个值替换为目标群组的ID
const long TARGET_CHAT_ID = -1002006327353;//指定群聊转发用户对机器人发送的信息
// 将这个值替换为你的机器人用户名
const string BOT_USERNAME = "yifanfubot";//机器人用户名
// 指定管理员ID
const int ADMIN_ID = 1427768220;//指定管理员ID不转发

// 存储机器人的所有命令
string[] botCommands = { "/start", "/yi", "/fan", "/qdgg", "/yccl", "/fu", "/btc", "/usd", "/more","/music", "/cny","/lamzhishu","/xgzhishu","/xamzhishu", "/trc","/caifu", "/usdt","/tron", "/home", "/jiankong", "/help", "/qunliaoziliao", "/baocunqunliao", "/bangdingdizhi", "/zijin", "/faxian", "/chaxun", "/xuni","/ucard","/bijiacha", "/jkbtc", "更多功能", "能量租赁", "实时汇率", "U兑TRX", "合约助手", "查询余额", "地址监听", "加密货币", "外汇助手", "监控" };    

if (message.Type == MessageType.Text)
{	
if (messageText.Contains("中文") || messageText.Contains("简体") || messageText.Contains("语言") || messageText.Contains("language"))
{
    string languagePackMessage = @"Telegram 简体中文语言包

管理员自用，原zh_cn简体中文包: https://t.me/setlanguage/classic-zh-cn

支持 Telegram for iOS/Android/macOS/Desktop, Telegram X for iOS/Android 官方客户端
支持 Nicegram/Plus Messager/Unigram 第三方客户端
Telegram 官网网页版不能使用语言包.
如果遇到不能更改语言包, 先把Telegram客户端升级新版

各个语言包:

中文(简体)-聪聪: https://t.me/setlanguage/zhcncc
中文(简体)-@zh_CN: https://t.me/setlanguage/classic-zh-cn
中文(简体)-简体: https://t.me/setlanguage/classic-zh
中文(简体)-langCN: https://t.me/setlanguage/zhlangcn
中文(简体)-zh-hans: https://t.me/setlanguage/zh-hans-beta
中文(简体)-瓜体: https://t.me/setlanguage/duang-zh-cn
中文(简体)-瓜皮中文: https://t.me/setlanguage/duangr-zhcn
中文(简体)-小哇花里胡哨: https://t.me/setlanguage/qingwa
中文(简体)-爱吃辣条的小学生: https://t.me/setlanguage/xiaowa
中文(简体)-江湖中文版: https://t.me/setlanguage/jianghu
中文(简体)-江湖侠客版: https://t.me/setlanguage/baoku
中文(简体)-@cnmoe: https://t.me/setlanguage/moecn
中文(简体)-@teslacn: https://t.me/setlanguage/vexzh
中文(简体)-: https://t.me/setlanguage/cnsimplified
中文(简体)-@MiaoCN: https://t.me/setlanguage/meowcn
中文(简体)-@Fengzh: https://t.me/setlanguage/fengcs
中文(简体)-简体字: https://t.me/setlanguage/jiantizi
中文(香港)-简体中文: https://t.me/setlanguage/zh-hans-raw
中文(香港)-繁体1: https://t.me/setlanguage/hongkong
中文(香港)-繁体2: https://t.me/setlanguage/zhhant-hk
中文(香港)-繁体3: https://t.me/setlanguage/zh-hant-raw
中文(香港)-人口语: https://t.me/setlanguage/hongkonger
中文(香港)-广东话1: https://t.me/setlanguage/zhhkpb1
中文(香港)-广东话2: https://t.me/setlanguage/hkcantonese
中文(香港)-廣東話: https://t.me/setlanguage/cantonese
中文(香港)-郭桓桓: https://t.me/setlanguage/zhong-taiwan-traditional
中文(台灣)-正体: https://t.me/setlanguage/taiwan
中文(台灣)-繁体: https://t.me/setlanguage/zh-hant-beta
中文(台灣)-文言: https://t.me/setlanguage/chinese-ancient
中文(台灣)-文言: https://t.me/setlanguage/chinese-literary
中文(台灣)-魔法師: https://t.me/setlanguage/encha
日文: https://t.me/setlanguage/ja-beta

说明:
Telegram 官方只开放了语言包翻译接口, 并没有提供中文语言包；
目前所有的中文语言包都是非官方人员翻译, 由作者统一整理编录的；
如果中文语言包对您有帮助，欢迎使用并在有需要时推荐给他人，谢谢！";

    // 创建内联键盘并添加按钮
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithUrl("点击应用原 @zh_cn 简体中文语言包", "https://t.me/setlanguage/classic-zh-cn")
    });

    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: languagePackMessage,
        disableWebPagePreview: true, // 关闭链接预览
        replyMarkup: inlineKeyboard // 添加内联键盘
    );
}    
// 获取北京时区
TimeZoneInfo chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");

// 获取当前时间或消息时间，并转换为北京时间
var timestamp = message.Date != default(DateTime) 
    ? TimeZoneInfo.ConvertTimeFromUtc(message.Date.ToUniversalTime(), chinaTimeZone).ToString("yyyy-MM-dd HH:mm:ss") 
    : TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, chinaTimeZone).ToString("yyyy-MM-dd HH:mm:ss");
    var userFullName = $"{message.From.FirstName} {message.From.LastName}".Trim();
    var username = message.From.Username;
    var userId = message.From.Id;
    var text = message.Text;
    var chatType = message.Chat.Type;
    var isMentioned = message.Entities?.Any(e => e.Type == MessageEntityType.Mention) ?? false;
    var containsCommand = botCommands.Any(cmd => text.StartsWith($"{cmd}@{BOT_USERNAME}") || text.StartsWith(cmd));

    string chatOrigin;
    if (chatType == ChatType.Private)
    {
        chatOrigin = "来自私聊";
    }
    else if (chatType == ChatType.Group || chatType == ChatType.Supergroup)
    {
        chatOrigin = "来自群聊";
    }
    else if (chatType == ChatType.Channel)
    {
        chatOrigin = "来自频道";
    }
    else
    {
        chatOrigin = "未知来源";
    }

    string forwardedMessage = $"{timestamp}  {userFullName}  @{username} (ID:<code> {userId}</code>)\n\n{chatOrigin}：<code>{text}</code>";
    var isTronAddress = Regex.IsMatch(text, @"^(T[A-Za-z0-9]{33})$");// 新增：检查消息是否是波场地址  新增：检查消息是否是数字+货币的组合
    var isNumberCurrency = Regex.IsMatch(text, @"(^\d+\s*[A-Za-z\u4e00-\u9fa5]+$)|(^\d+(\.\d+)?(btc|比特币|eth|以太坊|usdt|泰达币|币安币|bnb|bgb|币记-BGB|okb|欧易-okb|ht|火币积分-HT|瑞波币|xrp|艾达币|ada|狗狗币|doge|shib|sol|莱特币|ltc|link|电报币|ton|比特现金|bch|以太经典|etc|uni|avax|门罗币|xmr)$)", RegexOptions.IgnoreCase);


if (chatType == ChatType.Private || (chatType != ChatType.Private && containsCommand) || isTronAddress || isNumberCurrency)
{
    if (userId != ADMIN_ID)
    {
        try
        {
            await botClient.SendTextMessageAsync(
                chatId: TARGET_CHAT_ID,
                text: forwardedMessage,
                parseMode: ParseMode.Html
            );
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex)
        {
            // 这里处理Telegram API请求异常，例如机器人被禁言或没有权限等
            Console.WriteLine($"消息转发失败，原因：{ex.Message}");
            // 可以选择将错误消息发送回管理员
            await botClient.SendTextMessageAsync(
                chatId: ADMIN_ID,
                text: $"消息转发失败，原因：{ex.Message}"
            );
        }
        catch (Exception ex)
        {
            // 这里处理其他类型的异常
            Console.WriteLine($"发生异常，原因：{ex.Message}");
            // 可以选择将错误消息发送回管理员
            await botClient.SendTextMessageAsync(
                chatId: ADMIN_ID,
                text: $"发生异常，原因：{ex.Message}"
            );
        }
    }
}
} 
// 获取群资料
try
{
    if (message.Type == MessageType.Text && message.Text.Equals("/baocunqunliao", StringComparison.OrdinalIgnoreCase))
    {
        var chat = await botClient.GetChatAsync(message.Chat.Id);
        Console.WriteLine($"收到保存群聊指令，群ID：{chat.Id}");
        // 无论如何都回复
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "已开启群聊资料保存！"
        );
        Console.WriteLine("已回复用户：已开启群聊资料保存！");
        // 只有当群ID为负数时才保存
        if (chat.Id < 0)
        {
            // 检查是否已存在该群聊信息
            var existingGroupChat = GroupChats.FirstOrDefault(gc => gc.Id == chat.Id);
            if (existingGroupChat != null)
            {
                // 如果已存在，则更新群聊信息
                existingGroupChat.Title = chat.Title;
                existingGroupChat.InviteLink = chat.InviteLink;
                Console.WriteLine($"更新群聊信息，群ID：{chat.Id}");
            }
            else
            {
                // 如果不存在，则添加新的群聊信息
                GroupChats.Add(new GroupChat { Id = chat.Id, Title = chat.Title, InviteLink = chat.InviteLink });
                Console.WriteLine($"保存新的群聊信息，群ID：{chat.Id}");
            }
        }
        else
        {
            Console.WriteLine("群ID为正数，不保存群聊信息");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"处理/baocunqunliao命令时发生异常：{ex.Message}");
}

// 查询群聊资料
try
{
    if (message.Type == MessageType.Text && message.Text.Equals("/qunliaoziliao", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"收到查询群聊资料指令，用户ID：{message.From.Id}");
        // 检查是否为指定管理员
        if (message.From.Id == 1427768220)
        {
            if (GroupChats.Count == 0)
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "机器人所在 <b>0</b> 个群：",
                    parseMode: ParseMode.Html
                );
                Console.WriteLine("回复用户：机器人所在 0 个群");
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine($"机器人所在 <b>{GroupChats.Count}</b> 个群：\n");
                for (int i = 0; i < GroupChats.Count; i++)
                {
                    var groupChat = GroupChats[i];
                    sb.AppendLine($"{i + 1}：群名字：{EscapeHtml(groupChat.Title)}   群ID：{groupChat.Id}");
                    if (!string.IsNullOrEmpty(groupChat.InviteLink))
                    {
                        sb.AppendLine($"进群链接：{groupChat.InviteLink}");
                    }
                    if (i < GroupChats.Count - 1)
                    {
                        sb.AppendLine("-----------------------------------------------------------------");
                    }

                    // 每20条群聊信息发送一次消息
                    if ((i + 1) % 20 == 0 || i == GroupChats.Count - 1)
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: sb.ToString(),
                            parseMode: ParseMode.Html,
                            disableWebPagePreview: true // 关闭链接预览
                        );
                        Console.WriteLine($"发送群聊资料，群数量：{i + 1}");
                        sb.Clear();
                    }
                }
            }
        }
        else
        {
            Console.WriteLine($"非指定管理员尝试查询群聊资料，用户ID：{message.From.Id}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"处理/qunliaoziliao命令时发生异常：{ex.Message}");
}
if (message.ReplyToMessage != null && message.ReplyToMessage.From.Id == botClient.BotId)
{
    // 解析出被回复消息中的用户ID
    var match = Regex.Match(message.ReplyToMessage.Text, @"ID: (\d+)");
    if (match.Success)
    {
        var userId = long.Parse(match.Groups[1].Value);

        try
        {
            // 尝试向该用户发送新的消息
            await botClient.SendTextMessageAsync(
                chatId: userId,
                text: message.Text
            );

            // 如果消息发送成功，向当前用户发送成功消息
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "发送成功！"
            );
        }
        catch (Exception ex)
        {
            // 如果发送消息失败，捕获异常并在当前位置发送错误消息
            var errorMsg = ex.Message.Contains("Forbidden: bot was blocked by the user") 
                ? "信息发送失败：机器人被用户阻止" 
                : $"信息发送失败：<code>{ex.Message}</code>";
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: errorMsg,
                parseMode: ParseMode.Html
            );
        }
    }
}
// 检查消息是否来自指定管理员ID，并且文本以"回复"开头
if (message.From.Id == 1427768220 && message.Text.StartsWith("回复"))
{
    // 解析出群组ID和要发送的消息
    var parts = message.Text.Split(new[] { ' ' }, 3); // 分割文本以获取群组ID和消息
    if (parts.Length < 3)
    {
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "消息格式错误。",
            parseMode: ParseMode.Html
        );
        return;
    }

    long groupId;
    if (!long.TryParse(parts[1], out groupId))
    {
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "无效的群组ID。",
            parseMode: ParseMode.Html
        );
        return;
    }

    var replyMessage = parts[2]; // 要发送的消息
    bool shouldPin = replyMessage.EndsWith("置顶");
    if (shouldPin)
    {
        // 移除文本中的“置顶”
        replyMessage = replyMessage.Substring(0, replyMessage.Length - 2).Trim();
    }

    // 处理加粗和链接
    replyMessage = Regex.Replace(replyMessage, @"[\(\（](.*?)[，,]加粗[\)\）]", m =>
    {
        var textToBold = m.Groups[1].Value.Trim();
        return $"<b>{textToBold}</b>";
    });

    replyMessage = Regex.Replace(replyMessage, @"[\(\）](.*?)[，,](.*?)[\)\）]", m =>
    {
        var text = m.Groups[1].Value.Trim();
        var url = m.Groups[2].Value.Trim();
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = "http://" + url;
        }
        return $"<a href='{url}'>{text}</a>";
    });

    // 解析并处理内联按钮
    var buttonPattern = @"[\(\（]按钮，(.*?)[，,](.*?)[\)\）]";
    var buttonMatches = Regex.Matches(replyMessage, buttonPattern);
    List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();

    foreach (Match match in buttonMatches)
    {
        var buttonText = match.Groups[1].Value.Trim();
        var buttonAction = match.Groups[2].Value.Trim();
        InlineKeyboardButton button;

        if (buttonAction.Contains(".") || Uri.IsWellFormedUriString(buttonAction, UriKind.Absolute))
        {
            if (!buttonAction.StartsWith("http://") && !buttonAction.StartsWith("https://"))
            {
                buttonAction = "http://" + buttonAction;
            }
            button = InlineKeyboardButton.WithUrl(buttonText, buttonAction);
        }
        else
        {
            button = InlineKeyboardButton.WithCallbackData(buttonText, buttonAction);
        }

        buttons.Add(button);
    }

    // 从原始消息中移除所有按钮标记
    replyMessage = Regex.Replace(replyMessage, buttonPattern, "");

    InlineKeyboardMarkup inlineKeyboard = null;
    if (buttons.Count > 0)
    {
        inlineKeyboard = new InlineKeyboardMarkup(buttons.Select(b => new[] { b }).ToArray());
    }

    try
    {
// 尝试向指定群组发送消息
var sentReplyMessage = await botClient.SendTextMessageAsync(
    chatId: groupId,
    text: replyMessage,
    parseMode: ParseMode.Html,
    disableWebPagePreview: true, // 关闭链接预览
    replyMarkup: inlineKeyboard // 添加内联键盘
);

// 如果消息发送成功，回复管理员
await botClient.SendTextMessageAsync(
    chatId: message.Chat.Id,
    text: "发送成功",
    parseMode: ParseMode.Html
);

if (shouldPin)
{
    try
    {
        // 尝试置顶消息，使用静默置顶
        await botClient.PinChatMessageAsync(
            chatId: groupId,
            messageId: sentReplyMessage.MessageId, // 使用发送消息后返回的Message对象的MessageId
            disableNotification: true
        );
    }
    catch (Exception ex)
    {
        // 如果置顶失败（例如，由于权限问题），可以在这里处理
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"消息已发送，但置顶失败，原因：{ex.Message}",
            parseMode: ParseMode.Html
        );
    }
}
    }
    catch (Exception ex)
    {
        // 如果发送失败，回复管理员失败原因
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"信息发送失败，原因：{ex.Message}",
            parseMode: ParseMode.Html
        );
    }
}
// 检查是否接收到了 /laoaomen 消息，收到就查询老澳门六合彩开奖结果
if (messageText.StartsWith("/laoaomen"))
{
    var lotteryResult = await LotteryFetcher.FetchLotteryResultAsync();

    // 定义内联键盘
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new [] // 第一行按钮
        {
            InlineKeyboardButton.WithCallbackData("开奖规律", "lamzhishu"),
            InlineKeyboardButton.WithCallbackData("历史开奖", "history")
        }
    });

    // 发送文本和内联键盘作为一个消息
    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: lotteryResult, // 将开奖结果作为文本发送
        parseMode: ParseMode.Html, // 使用HTML解析模式以支持文本加粗
        replyMarkup: inlineKeyboard // 包含内联键盘
    );
}
// 检查是否接收到了 /lamzhishu 消息，收到就查询老澳门六合彩特码统计
if (messageText.StartsWith("/lamzhishu"))
{
    var userId = message.From.Id;
    var today = DateTime.UtcNow.AddHours(8).Date; // 转换为北京时间并获取日期部分
    bool allowQuery = true; // 默认允许查询

    // 检查用户是否已经查询过
    if (oldMacauUserQueries.ContainsKey(userId))
    {
        var (count, lastQueryDate) = oldMacauUserQueries[userId]; // 取出元组
        if (lastQueryDate == today && count >= 1)
        {
            try
            {
                var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                if (member.Status == ChatMemberStatus.Left || member.Status == ChatMemberStatus.Kicked)
                {
                    // 用户不在群组中
                    var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                    {
                        InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                    });

                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可不限制查询！",
                        replyMarkup: keyboard
                    );
                    return;
                }
                // 如果用户在群组中，不需要更新查询次数，直接进行查询
            }
            catch (Exception)
            {
                // 发生异常，可能是因为机器人不在群组中或群组ID错误，允许查询
                allowQuery = true;
            }
        }
        else if (lastQueryDate != today)
        {
            // 如果今天是用户第一次查询，重置查询次数和日期
            oldMacauUserQueries[userId] = (1, today);
        }
        else
        {
            // 如果用户今天的查询次数还没有用完，增加查询次数
            oldMacauUserQueries[userId] = (count + 1, today);
        }
    }
    else
    {
        // 如果用户之前没有查询过，添加新的记录
        oldMacauUserQueries[userId] = (1, today);
    }

    if (allowQuery)
    {
        // 执行查询逻辑
        var messageToEdit = await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "正在统计，请稍后...",
            parseMode: ParseMode.Html
        );

        var statisticsResult = await OldMacauLotteryStatisticsHelper.FetchOldMacauSpecialNumberStatisticsAsync();

        await botClient.EditMessageTextAsync(
            chatId: message.Chat.Id,
            messageId: messageToEdit.MessageId,
            text: statisticsResult,
            parseMode: ParseMode.Html
        );
    }
}
// 检查是否接收到了 /xinaomen 消息，收到就查询新澳门六合彩开奖结果
if (messageText.StartsWith("/xinaomen"))
{
    var lotteryResult = await NewLotteryFetcher.FetchLotteryResultAsync();

    // 定义内联键盘
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new [] // 第一行按钮
        {
            InlineKeyboardButton.WithCallbackData("开奖规律", "xamzhishu"),
            InlineKeyboardButton.WithCallbackData("历史开奖", "newHistory")
        }
    });

    // 发送文本和内联键盘作为一个消息
    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: lotteryResult, // 将开奖结果作为文本发送
        parseMode: ParseMode.Html, // 使用HTML解析模式以支持文本加粗
        replyMarkup: inlineKeyboard // 包含内联键盘
    );
}
// 检查是否接收到了 /xamzhishu 消息，收到就查询新澳门六合彩特码统计
if (messageText.StartsWith("/xamzhishu"))
{
    var userId = message.From.Id;
    var today = DateTime.UtcNow.AddHours(8).Date; // 转换为北京时间并获取日期部分

    bool allowQuery = true; // 默认允许查询

    // 检查用户是否已经查询过
    if (newMacauUserQueries.ContainsKey(userId))
    {
        var (count, lastQueryDate) = newMacauUserQueries[userId]; // 取出元组
        if (lastQueryDate == today && count >= 1)
        {
            try
            {
                var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                if (member.Status == ChatMemberStatus.Left || member.Status == ChatMemberStatus.Kicked)
                {
                    // 用户不在群组中
                    var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                    {
                        InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                    });

                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可不限制查询！",
                        replyMarkup: keyboard
                    );
                    return;
                }
                // 如果用户在群组中，不需要更新查询次数，直接进行查询
                allowQuery = true;
            }
            catch (Exception ex)
            {
                // 如果检查群组成员时出现异常（例如机器人不在群组中），允许查询
                allowQuery = true;
            }
        }
        else if (lastQueryDate != today)
        {
            // 如果今天是用户第一次查询，重置查询次数和日期
            newMacauUserQueries[userId] = (1, today);
        }
        else
        {
            // 如果用户今天的查询次数还没有用完，增加查询次数
            newMacauUserQueries[userId] = (count + 1, today);
        }
    }
    else
    {
        // 如果用户之前没有查询过，添加新的记录
        newMacauUserQueries[userId] = (1, today);
    }

    if (allowQuery)
    {
        // 执行查询逻辑
        var messageToEdit = await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "正在统计，请稍后...",
            parseMode: ParseMode.Html
        );

        var statisticsResult = await NewMacauLotteryStatisticsHelper.FetchNewMacauSpecialNumberStatisticsAsync(NewLotteryFetcher.client);

        await botClient.EditMessageTextAsync(
            chatId: message.Chat.Id,
            messageId: messageToEdit.MessageId,
            text: statisticsResult,
            parseMode: ParseMode.Html
        );
    }
}
// 检查是否接收到了 /xianggang 消息，收到就查询香港六合彩开奖结果
if (messageText.StartsWith("/xianggang"))
{
    var lotteryResult = await LotteryFetcherr.FetchHongKongLotteryResultAsync();

    // 定义内联键盘，添加历史记录按钮
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithCallbackData("开奖规律", "xgzhishu"),	    
        InlineKeyboardButton.WithCallbackData("历史开奖", "historyy")
    });

    // 发送文本和内联键盘作为一个消息
    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: lotteryResult, // 将开奖结果作为文本发送
        parseMode: ParseMode.Html, // 使用HTML解析模式以支持文本加粗
        replyMarkup: inlineKeyboard // 包含内联键盘
    );
}  
// 检查是否接收到了 /xgzhishu 消息，收到就查询香港六合彩特码统计
if (messageText.StartsWith("/xgzhishu"))
{
    var userId = message.From.Id;
    var today = DateTime.UtcNow.AddHours(8).Date; // 转换为北京时间并获取日期部分

    bool allowQuery = true; // 默认允许查询

    // 检查用户是否已经查询过
    if (userQueries.ContainsKey(userId))
    {
        var (count, lastQueryDate) = userQueries[userId]; // 取出元组
        if (lastQueryDate == today && count >= 1)
        {
            try
            {
                var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                if (member.Status == ChatMemberStatus.Left || member.Status == ChatMemberStatus.Kicked)
                {
                    // 用户不在群组中
                    var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                    {
                        InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                    });

                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可不限制查询！",
                        replyMarkup: keyboard
                    );
                    return;
                }
                // 如果用户在群组中，不需要更新查询次数，直接进行查询
                allowQuery = true;
            }
            catch
            {
                // 发生异常，可能是因为机器人不在群组中或群组ID错误，允许查询
                allowQuery = true;
            }
        }
        else if (lastQueryDate != today)
        {
            // 如果今天是用户第一次查询，重置查询次数和日期
            userQueries[userId] = (1, today);
        }
        else
        {
            // 如果用户今天的查询次数还没有用完，增加查询次数
            userQueries[userId] = (count + 1, today);
        }
    }
    else
    {
        // 如果用户之前没有查询过，添加新的记录
        userQueries[userId] = (1, today);
    }

    if (allowQuery)
    {
        // 执行查询逻辑
        var messageToEdit = await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "正在统计，请稍后...",
            parseMode: ParseMode.Html
        );

        var statisticsResult = await LotteryStatisticsHelper.FetchSpecialNumberStatisticsAsync(LotteryFetcherr.client);

        await botClient.EditMessageTextAsync(
            chatId: message.Chat.Id,
            messageId: messageToEdit.MessageId,
            text: statisticsResult,
            parseMode: ParseMode.Html
        );
    }
}

// 检查是否接收到了 /zhishu 消息，收到就查询指数数据和沪深两市上涨下跌数概览
if (messageText.StartsWith("/zhishu"))
{
    bool allRequestsFailed = true; // 新增标志位

    // 查询指数数据
    var indexData = await IndexDataFetcher.FetchIndexDataAsync();
    // 查询沪深两市上涨下跌数概览
    var marketOverview = await IndexDataFetcher.FetchMarketOverviewAsync();

    // 检查是否所有API请求都失败了
    if (!indexData.Contains("数据获取失败") && !marketOverview.Contains("数据获取失败"))
    {
        allRequestsFailed = false; // 如果有任何一个请求成功，就更新标志位
    }

    var messageContent = "";

    if (allRequestsFailed)
    {
        // 如果所有API请求都失败了，只添加额外的链接文本
        messageContent = @"
<a href='https://www.google.com/finance/quote/.IXIC:INDEXNASDAQ'>谷歌财经</a>  <a href='https://m.cn.investing.com/markets/'>英为财情</a>  <a href='https://www.jin10.com/'>金十数据 </a> <a href='https://rili.jin10.com/'>金十日历 </a>";
    }
    else
    {
        // 如果API请求成功，将指数数据和市场概览整合到一条消息中
        messageContent = $"{indexData}\n————————————————————\n{marketOverview}";

        // 添加额外的链接文本
        var additionalText = @"
<a href='https://www.google.com/finance/quote/.IXIC:INDEXNASDAQ'>谷歌财经</a>  <a href='https://m.cn.investing.com/markets/'>英为财情</a>  <a href='https://www.jin10.com/'>金十数据 </a> <a href='https://rili.jin10.com/'>金十日历 </a>";

        // 将additionalText添加到messageContent
        messageContent += $"{additionalText}";
    }

    // 向用户发送整合后的数据，确保使用ParseMode.Html以正确解析HTML标签，并关闭链接预览
    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: messageContent,
        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
        disableWebPagePreview: true // 关闭链接预览
    );
}
// 检查消息是否以“汇率”开头，并跟随一个数字
var userMessageText = message.Text;
var ratePrefix = "汇率";
if (userMessageText.StartsWith(ratePrefix))
{
    var userRateText = userMessageText.Substring(ratePrefix.Length).Trim();
    if (decimal.TryParse(userRateText, out decimal userRate) && userRate > 0)
    {
        // 启动查询USDT价格的方法
        _ = GetOkxPriceAsync("usdt", "cny", "anyMethod") // 假设您查询的是USDT对CNY的汇率，method参数根据需要调整
            .ContinueWith(async task =>
            {
                string responseText;
                if (task.IsFaulted || task.Result == default)
                {
                    // 如果发生异常或返回默认值，向用户发送错误消息
                    responseText = "API异常，请稍后重试！";
                }
                else
                {
                    // 使用查询到的USDT价格信息计算手续费
                    var realRate = task.Result;
                    var feePercentage = (1 - realRate / userRate) * 100;
                    responseText = $"当汇率{userRate}时，手续费为 {feePercentage:N2}%";
                }

                // 向用户发送查询到的手续费信息或错误消息
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: responseText,
                    parseMode: ParseMode.Html,
                    disableWebPagePreview: true,
                    replyToMessageId: message.MessageId
                );
            });
    }
    else
    {
        // 如果用户发送的文本不符合“汇率+数字”的格式，则不执行任何操作
        // 这里可以留空或添加其他逻辑
    }
}
// 如果用户发送的文本包含"红色"、"绿色"、"蓝色"、"红波"、"绿波"或"蓝波"
if (messageText.Contains("红色") || messageText.Contains("绿色") || messageText.Contains("蓝色") || messageText.Contains("红波") || messageText.Contains("绿波") || messageText.Contains("蓝波"))
{
    // 回复用户波色信息
string waveText = "红波\uD83D\uDD34：01、02、07、08、12、13、18、19、23、24、29、30、34、35、40、45、46\n" +
                  "蓝波\uD83D\uDD35：03、04、09、10、14、15、20、25、26、31、36、37、41、42、47、48\n" +
                  "绿波\uD83D\uDFE2：05、06、11、16、17、21、22、27、28、32、33、38、39、43、44、49";

    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: waveText
    );
}
if (messageText.Equals("ID", StringComparison.OrdinalIgnoreCase) || messageText.Equals("id", StringComparison.OrdinalIgnoreCase))
{
    await HandleIdCommandAsync(botClient, message);
    return;
}        
await SendHelpMessageAsync(botClient, message);        
// 获取交易记录
if (messageText.StartsWith("/gk") || messageText.Contains("兑换记录"))
{
    try
    {
        // 调用GetTransactionRecordsAsync时传递botClient和message参数
        var transactionRecords = await UpdateHandlers.GetTransactionRecordsAsync(botClient, message);
    }
    catch (Exception ex)
    {
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"获取交易记录时发生错误：{ex.Message}"
        );
    }
}  
// 使用正则表达式来匹配 /home 命令，允许命令后面跟随 "@机器人用户名"
var homeCommandRegex = new Regex(@"^/home(@\w+)?$", RegexOptions.IgnoreCase);
if (homeCommandRegex.IsMatch(message.Text) || message.Text.Equals("地址监听", StringComparison.OrdinalIgnoreCase))
{
    //if (message.From.Id == AdminUserId)
   // {
        // 如果用户是管理员，执行 "/faxian" 的方法
   //     var topRise = riseList.OrderByDescending(x => x.Days).Take(5);
   //     var topFall = fallList.OrderByDescending(x => x.Days).Take(5);

   //     var reply = "<b>币安连续上涨TOP5：</b>\n";
   //     foreach (var coin in topRise)
  //      {
  //          var symbol = coin.Symbol.Replace("USDT", "");
  //          reply += $"<code>{symbol}</code>/USDT 连涨{coin.Days}天   ${coin.Price.ToString("0.####")}\n";
//        }

 //       reply += "\n<b>币安连续下跌TOP5：</b>\n";
//        foreach (var coin in topFall)
 //       {
  //          var symbol = coin.Symbol.Replace("USDT", "");
  //          reply += $"<code>{symbol}</code>/USDT 连跌{coin.Days}天   ${coin.Price.ToString("0.####")}\n";
  //      }

  //      await botClient.SendTextMessageAsync(
  //          chatId: message.Chat.Id,
   //         text: reply,
   //         parseMode: ParseMode.Html
   //     );
  //  }
 //   else
 //   {
        // 如果用户不是管理员，执行你现在的方法
        await HandlePersonalCenterCommandAsync(botClient, message, provider);
  //  }
    return;
}
// 检查是否是/jiankong命令
if (message.Type == MessageType.Text && message.Text.StartsWith("/jiankong"))
{
    // 如果消息来源于私聊
    if (message.Chat.Type == ChatType.Private)
    {
        await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "此命令仅适用于群组和频道");
        return;
    }
    
    // 启动监控
    StartMonitoring(botClient, message.Chat.Id);

    // 发送 "监控已启动" 消息
    var sentMessageForMonitoring = await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: "监控已启动"
    );

    // 等待1秒
    await Task.Delay(500);

    // 删除消息
    await botClient.DeleteMessageAsync(
        chatId: message.Chat.Id,
        messageId: sentMessageForMonitoring.MessageId
    );

    // 尝试撤回 /jiankong 指令
    try
    {
        await botClient.DeleteMessageAsync(
            chatId: message.Chat.Id,
            messageId: message.MessageId
        );
    }
    catch (Telegram.Bot.Exceptions.ApiRequestException)
    {
        // 如果机器人没有权限，忽略异常
    }
}
if (messageText.Contains("费用") || messageText.Contains("能量") || messageText.Contains("/tron") || messageText.Contains("手续费") || messageText.Contains("能量租赁"))
{
    // 向用户发送能量介绍
    string multisigText = @"波场手续费说明（⚠️务必仔细阅读⚠️）

波场具有独特的资源模型，分为【带宽】和【能量】，每个账户初始具有 600 带宽 和 0 能量。
转账USDT主要消耗能量，当账户可用能量不足时，燃烧TRX获取能量，燃烧的TRX就是我们常说的转账手续费。

<b>转账消耗的能量与转账金额无关，与对方地址是否有USDT有关！</b>

转账给有U的地址，消耗约 3.2万 能量；转账给没U的地址，消耗约 6.5万 能量。

如果通过燃烧TRX获取3.2万能量，约需燃烧 13.39 TRX；如果通过燃烧TRX获取6.5万能量，约需燃烧 27.25 TRX。

通过提前租赁能量，可以避免燃烧TRX来获取能量，为您的转账节省大量TRX：

租赁3.2万能量/日，仅需  7.00 TRX，节省   6.39 TRX (节省约48%)
租赁6.5万能量/日，仅需13.00 TRX，节省 14.25 TRX (节省约53%)";

    // 创建内联键盘按钮
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new [] // first row
        {
	    InlineKeyboardButton.WithCallbackData("能量消耗对比", "energyComparison"),
            InlineKeyboardButton.WithCallbackData("立即租赁能量", "contactAdmin"),
        }
    });

    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: multisigText,
        parseMode: ParseMode.Html,
        replyMarkup: inlineKeyboard
    );
    // 如果发送者是管理员且消息文本为“能量租赁”，则额外发送管理员菜单
    if (message.From.Id == AdminUserId && messageText.Contains("能量租赁"))
    {
        string adminMenuText = "兑换TRX机器人 管理员菜单:";
        var adminInlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new [] // first row
            {
                InlineKeyboardButton.WithCallbackData("网址收藏", "shoucang"), 	
		InlineKeyboardButton.WithCallbackData("兑换记录", "show_transaction_recordds"),   		
            },
            new [] // second row
            {
                InlineKeyboardButton.WithCallbackData("操作指令", "mingling"),	
		InlineKeyboardButton.WithCallbackData("群聊资料", "show_group_info"), 		    
            },
            new [] // second row
            {
		    
                InlineKeyboardButton.WithCallbackData("用户地址", "show_user_info"),
                InlineKeyboardButton.WithCallbackData("关注列表", "shiyong"),		    
            },
            new [] // second row
            {
                InlineKeyboardButton.WithCallbackData("客户地址余额", "ExecuteZjdhMethod"),
		InlineKeyboardButton.WithCallbackData("承兑账单详情", "chengdui"),  		    	    
            }
		
        });

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: adminMenuText,
            replyMarkup: adminInlineKeyboard
        );
    }	
}
if (messageText.Contains("作者") || messageText.Contains("管理") || messageText.Contains("你好") || messageText.Contains("在吗")|| messageText.Contains("？")|| messageText.Contains("如何")|| messageText.Contains("怎么")|| messageText.Contains("?"))
{
    // 向用户发送作者联系信息
    string contactText = @"双向用户可以直接私聊机器人，作者会第一时间回复您！";
    
    // 创建内联键盘按钮
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new [] // first row
        {
            InlineKeyboardButton.WithUrl("直接联系作者", "https://t.me/yifanfu"),
            InlineKeyboardButton.WithCallbackData("由作者联系您", "authorContactRequest")
        }
    });

    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: contactText,
        parseMode: ParseMode.Html,
        replyMarkup: inlineKeyboard // 添加这行代码
    );
}	    
// 修改正则表达式来同时匹配 "/zijin" 命令和 "资金费率" 文本
var zijinCommandRegex = new Regex(@"^(/zijin(@\w+)?|资金费率)$", RegexOptions.IgnoreCase);
if (zijinCommandRegex.IsMatch(message.Text))
{
    var userId = message.From.Id;
    var today = DateTime.UtcNow.AddHours(8).Date; // 转换为北京时间并获取日期部分
    bool allowQuery = true; // 默认允许查询

    // 检查用户是否已经查询过
    if (zijinUserQueries.ContainsKey(userId))
    {
        var (count, lastQueryDate) = zijinUserQueries[userId]; // 取出元组
        if (lastQueryDate == today && count >= 1)
        {
            try
            {
                var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                if (member.Status == ChatMemberStatus.Left || member.Status == ChatMemberStatus.Kicked)
                {
                    // 用户不在群组中
                    var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                    {
                        InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                    });

                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可不限制查询！",
                        replyMarkup: keyboard
                    );
                    return;
                }
                // 如果用户在群组中，不需要更新查询次数，直接进行查询
                allowQuery = true;
            }
            catch
            {
                // 发生异常，可能是因为机器人不在群组中或群组ID错误，允许查询
                allowQuery = true;
            }
        }
        else if (lastQueryDate != today)
        {
            // 如果今天是用户第一次查询，重置查询次数和日期
            zijinUserQueries[userId] = (1, today);
        }
        else
        {
            // 如果用户今天的查询次数还没有用完，增加查询次数
            zijinUserQueries[userId] = (count + 1, today);
        }
    }
    else
    {
        // 如果用户之前没有查询过，添加新的记录
        zijinUserQueries[userId] = (1, today);
    }

    if (allowQuery)
    {
        try
        {
            var fundingRates = await BinanceFundingRates.GetFundingRates();
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: fundingRates,
                parseMode: ParseMode.Html
            );
        }
        catch (Exception ex)
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"获取资金费率时发生错误：{ex.Message}"
            );
        }
    }
}  
// 检查是否接收到了 z0 或 /usdt 消息，收到就查询USDT价格
if (messageText.StartsWith("z0") || messageText.StartsWith("/usdt")| messageText.StartsWith("zo"))
{
    // 启动查询USDT价格的方法
    _ = OkxPriceFetcher.GetUsdtPriceAsync(messageText)
        .ContinueWith(async task =>
        {
            string responseText;
            if (task.IsFaulted)
            {
                // 如果发生异常，向用户发送错误消息
                responseText = "api异常，请稍后重试！";
            }
            else
            {
                // 否则，使用查询到的USDT价格信息
                responseText = task.Result;
            }

            // 创建内联键盘
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new [] // 第一行按钮
                {
                    InlineKeyboardButton.WithCallbackData("再查一次", "zaicha"), // 修改这里
                    InlineKeyboardButton.WithCallbackData("白资兑换", "contactAdmin")
                }
            });

            // 向用户发送查询到的USDT价格信息或错误消息
            // 使用ParseMode.Html以便Telegram解析HTML链接，并关闭链接预览
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: responseText,
                parseMode: ParseMode.Html,
                disableWebPagePreview: true,
                replyMarkup: inlineKeyboard,
                replyToMessageId: message.MessageId // 添加这一行
            );
        });
}
// 检查是否是"查询余额"命令或 "/trc"
if (message.Type == MessageType.Text && (message.Text.Equals("查询余额", StringComparison.OrdinalIgnoreCase) || message.Text.StartsWith("/trc")))
{
    // 无论用户是否是管理员，都执行以下方法
    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id, 
        text: "请发送您要查询的<b>TRC-20(波场)地址：</b> ", 
        parseMode: ParseMode.Html
    );
}
// 使用正则表达式来匹配命令，允许命令后面跟随 "@机器人用户名"
var moreCommandRegex = new Regex(@"^/more(@\w+)?$", RegexOptions.IgnoreCase);
if (moreCommandRegex.IsMatch(message.Text) || message.Text.Equals("更多功能", StringComparison.OrdinalIgnoreCase))
{
    // 创建内联键盘
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new [] // 第一行按钮
        {
            InlineKeyboardButton.WithCallbackData("会员代开", "membershipOptions"),
            InlineKeyboardButton.WithCallbackData("会员表情", "memberEmojis"),
            InlineKeyboardButton.WithCallbackData("联系作者", "contactAdmin")
        },
        new [] // 第二行按钮
        {
            InlineKeyboardButton.WithCallbackData("短信接码", "smsVerification"),
            InlineKeyboardButton.WithCallbackData("靓号地址", "fancyNumbers"),
            InlineKeyboardButton.WithCallbackData("简体中文", "send_chinese")
        },
        new [] // 第三行按钮
        {
            InlineKeyboardButton.WithCallbackData("汇率换算", "send_huansuan"),
            InlineKeyboardButton.WithCallbackData("指令大全", "commandList"),
            InlineKeyboardButton.WithCallbackData("使用帮助", "send_help")
        },
        new [] // 新增第四行按钮
        {
            InlineKeyboardButton.WithCallbackData("指数行情", "indexMarket"),
            InlineKeyboardButton.WithCallbackData("在线音频", "onlineAudio"),
            InlineKeyboardButton.WithCallbackData("在线阅读", "onlineReading")
        },
        new [] // 新增第四行按钮
        {
            InlineKeyboardButton.WithCallbackData("老澳门彩", "laoaomen"),
            InlineKeyboardButton.WithCallbackData("新澳门彩", "xinaomen"),
            InlineKeyboardButton.WithCallbackData("香港六合", "xianggang")
        },
        new [] // 新增第五行按钮
        {
            InlineKeyboardButton.WithCallbackData("免实名-USDT消费卡", "energy_introo")
        }
    });

    // 向用户发送一条消息，告知他们可以选择下方按钮操作
    _ = botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: "欢迎使用本机器人，请选择下方按钮操作：",
        replyMarkup: inlineKeyboard
    );
}	    
// 新增检查是否是"/erc"命令且发送者是指定管理员
if (message.Type == MessageType.Text && message.Text.StartsWith("/erc") && message.From.Id == AdminUserId)
{
    // 如果用户是管理员，执行 HandleGetFollowersCommandAsync 方法
    await HandleGetFollowersCommandAsync(botClient, message);
}
if (messageText.Equals("/chaxun", StringComparison.OrdinalIgnoreCase))
{
    timer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(30)); // 启动定时器
    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: "自动查询中...",
        parseMode: ParseMode.Html
    );
}
// 修改正则表达式来同时匹配 "/faxian" 命令和 "龙虎榜单" 文本
var faxianCommandRegex = new Regex(@"^(/faxian(@\w+)?|龙虎榜单)$", RegexOptions.IgnoreCase);
if (faxianCommandRegex.IsMatch(message.Text))
{
    var userId = message.From.Id;
    var today = DateTime.UtcNow.AddHours(8).Date; // 转换为北京时间并获取日期部分
    bool allowQuery = true;

    if (faxianUserQueries.ContainsKey(userId))
    {
        var (count, lastQueryDate) = faxianUserQueries[userId];
        if (lastQueryDate == today && count >= 1)
        {
            try
            {
                var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                if (member.Status == ChatMemberStatus.Left || member.Status == ChatMemberStatus.Kicked)
                {
                    // 用户不在群组中
                    var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                    {
                        InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                    });

                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可不限制查询！",
                        replyMarkup: keyboard
                    );
                    return;
                }
            }
            catch
            {
                // 发生异常，可能是因为机器人不在群组中或群组ID错误，允许查询
                allowQuery = true;
            }
        }
        else if (lastQueryDate != today)
        {
            faxianUserQueries[userId] = (1, today);
        }
        else
        {
            faxianUserQueries[userId] = (count + 1, today);
        }
    }
    else
    {
        faxianUserQueries[userId] = (1, today);
    }

    if (allowQuery)
    {
    IEnumerable<CoinInfo> topRise;
    IEnumerable<CoinInfo> topFall;

    // 如果是指定管理员，不过滤TRX
    if (message.From.Id == AdminUserId)
    {
        topRise = riseList.OrderByDescending(x => x.Days).Take(5);
        topFall = fallList.OrderByDescending(x => x.Days).Take(5);
    }
    else
    {
        // 过滤出不包含TRX的上涨列表
        topRise = riseList.Where(coin => !coin.Symbol.Equals("TRXUSDT")).OrderByDescending(x => x.Days).Take(5);
        // 过滤出不包含TRX的下跌列表
        topFall = fallList.Where(coin => !coin.Symbol.Equals("TRXUSDT")).OrderByDescending(x => x.Days).Take(5);
    }

var reply = "<b>币安连续上涨TOP5：</b>\n";
List<InlineKeyboardButton[]> rows = new List<InlineKeyboardButton[]>();
InlineKeyboardButton[] row = new InlineKeyboardButton[5];
int index = 0; // 用于计数和显示数字

// 上涨币种
foreach (var coin in topRise)
{
    reply += $"{index}️⃣ <code>{coin.Symbol.Replace("USDT", "")}</code>/USDT 连涨{coin.Days}天   ${coin.Price.ToString("0.####")}\n";
    row[index % 5] = InlineKeyboardButton.WithCallbackData($"{index}️⃣", $"查{coin.Symbol.ToLower().Replace("usdt", "")}");
    if ((index + 1) % 5 == 0 || index == topRise.Count() - 1)
    {
        rows.Add(row);
        row = new InlineKeyboardButton[5]; // 为下一排按钮准备新的数组
    }
    index++;
}

reply += "\n<b>币安连续下跌TOP5：</b>\n";
// 下跌币种
foreach (var coin in topFall)
{
    reply += $"{index}️⃣ <code>{coin.Symbol.Replace("USDT", "")}</code>/USDT 连跌{coin.Days}天   ${coin.Price.ToString("0.####")}\n";
    row[index % 5] = InlineKeyboardButton.WithCallbackData($"{index}️⃣", $"查{coin.Symbol.ToLower().Replace("usdt", "")}");
    if ((index + 1) % 5 == 0 || index == topFall.Count() + topRise.Count() - 1)
    {
        rows.Add(row);
        row = new InlineKeyboardButton[5]; // 为下一排按钮准备新的数组
    }
    index++;
}

var inlineKeyboard = new InlineKeyboardMarkup(rows);

await botClient.SendTextMessageAsync(
    chatId: message.Chat.Id,
    text: reply,
    parseMode: ParseMode.Html,
    replyMarkup: inlineKeyboard
);
    }
}
// 获取涨跌天数统计
if (messageText.Equals("/jihui", StringComparison.OrdinalIgnoreCase))
{
    var url = "https://api.binance.com/api/v3/ticker/price"; // 获取所有交易对

    using (var httpClient = new HttpClient())
    {
        try
        {
            var response = await httpClient.GetStringAsync(url); // 调用API
            var allSymbols = JsonSerializer.Deserialize<List<SymbolInfo>>(response); // 使用System.Text.Json解析API返回的JSON数据

            // 过滤出以USDT结尾的交易对
            var usdtSymbols = allSymbols.Where(symbol => symbol.symbol.EndsWith("USDT")).ToList();

            var riseList = new List<CoinInfo>();
            var fallList = new List<CoinInfo>();

            foreach (var symbol in usdtSymbols)
            {
                var currentPriceResponse = await httpClient.GetStringAsync($"https://api.binance.com/api/v3/ticker/24hr?symbol={symbol.symbol}");
                var currentPrice = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(currentPriceResponse);
                if (decimal.Parse(currentPrice["lastPrice"].GetString()) == 0)
                {
                    continue; // 如果当前价格为0，那么跳过这个币种
                }

                var klineResponse = await httpClient.GetAsync($"https://api.binance.com/api/v3/klines?symbol={symbol.symbol}&interval=1d&limit=1000");
                var klineDataRaw = JsonSerializer.Deserialize<List<List<JsonElement>>>(await klineResponse.Content.ReadAsStringAsync());

                var klineData = klineDataRaw.Select(item => new HistoricalKlineDataItem
                {
                    OpenTime = item[0].GetInt64(),
                    Open = item[1].GetString(),
                    High = item[2].GetString(),
                    Low = item[3].GetString(),
                    Close = item[4].GetString()
                }).ToList();

                (int riseDays, int fallDays) = AnalysisHelper.GetContinuousRiseFallDays(klineData);

                if (riseDays > 0)
                {
                    riseList.Add(new CoinInfo { Symbol = symbol.symbol, Days = riseDays, Price = decimal.Parse(klineData.Last().Close) });
                }

                if (fallDays > 0)
                {
                    fallList.Add(new CoinInfo { Symbol = symbol.symbol, Days = fallDays, Price = decimal.Parse(klineData.Last().Close) });
                }
            }

    // 过滤出不包含TRX的上涨列表
    var topRise = riseList.Where(coin => !coin.Symbol.Equals("TRXUSDT")).OrderByDescending(x => x.Days).Take(5);

    // 过滤出不包含TRX的下跌列表
    var topFall = fallList.Where(coin => !coin.Symbol.Equals("TRXUSDT")).OrderByDescending(x => x.Days).Take(5);

            var reply = "<b>币安连续上涨TOP5：</b>\n";
            foreach (var coin in topRise)
            {
                reply += $"{coin.Symbol.Replace("USDT", "/USDT")} 连涨{coin.Days}天  ${coin.Price.ToString("0.####")}\n";
            }

            reply += "\n<b>币安连续下跌TOP5：</b>\n";
            foreach (var coin in topFall)
            {
                reply += $"{coin.Symbol.Replace("USDT", "/USDT")} 连跌{coin.Days}天  ${coin.Price.ToString("0.####")}\n";
            }

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: reply,
                parseMode: ParseMode.Html
            );
        }
        catch (Exception ex)
        {
            // 记录错误信息
            Console.WriteLine($"Error when calling API: {ex.Message}");
        }
    }
}
// 检查是否接收到了 /bangdingdizhi 消息，如果是管理员发送的，则返回所有绑定的地址信息
if (message.Text.StartsWith("/bangdingdizhi") && message.From.Id == 1427768220)
{
    var _bindRepository = provider.GetRequiredService<IBaseRepository<TokenBind>>();
    await SendAllBindingsInBatches(botClient, message.Chat.Id, _bindRepository);
}
// 处理添加群聊信息的命令
try
{
    if (message.Type == MessageType.Text && message.Text.StartsWith("添加群聊：") && message.From.Id == 1427768220)
    {
        Console.WriteLine($"收到添加群聊指令，管理员ID：{message.From.Id}");
        // 解析消息文本以获取群聊信息
        var messageParts = message.Text.Split(new[] { "群名字：", "群ID：", "群链接：", "指令：" }, StringSplitOptions.RemoveEmptyEntries);
        if (messageParts.Length >= 2)
        {
            string groupName = messageParts[1].Trim();
            if (long.TryParse(messageParts[2].Trim(), out long groupId))
            {
                string groupLink = messageParts.Length > 3 ? messageParts[3].Trim() : null;
		string command = messageParts.Length > 4 ? messageParts[4].Trim().ToLower() : null;    
                // 检查是否已存在该群聊信息
                var existingGroupChat = GroupChats.FirstOrDefault(gc => gc.Id == groupId);
                if (existingGroupChat != null)
                {
                    // 如果已存在，则更新群聊信息
                    existingGroupChat.Title = groupName;
                    existingGroupChat.InviteLink = groupLink;
                    Console.WriteLine($"更新群聊信息，群ID：{groupId}");
                }
                else
                {
                    // 如果不存在，则添加新的群聊信息
                    GroupChats.Add(new GroupChat { Id = groupId, Title = groupName, InviteLink = groupLink });
                    Console.WriteLine($"保存新的群聊信息，群ID：{groupId}");
                }
                // 根据指令处理兑换通知黑名单
                if (command == "开启")
                {
                    GroupManager.BlacklistedGroupIds.Remove(groupId);
                    Console.WriteLine($"群ID：{groupId} 已从兑换通知黑名单中移除");
                }
                else if (command == "关闭")
                {
                    GroupManager.BlacklistedGroupIds.Add(groupId);
                    Console.WriteLine($"群ID：{groupId} 已添加到兑换通知黑名单");
                }		    
                // 将群ID添加到GroupManager中
                GroupManager.AddGroupId(groupId);
                Console.WriteLine($"群ID：{groupId} 已添加到广告群组列表");

                // 回复管理员确认信息已保存
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "群聊资料已添加！"
                );
            }
            else
            {
                Console.WriteLine("无法解析群ID");
                // 回复管理员群ID无效
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "无法添加群聊资料：无效的群ID。"
                );
            }
        }
        else
        {
            Console.WriteLine("指令格式错误");
            // 回复管理员指令格式错误
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "无法添加群聊资料：指令格式错误。"
            );
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"处理添加群聊指令时发生异常：{ex.Message}");
}
// 检查是否接收到了 /gongtongqunzu 消息，收到就启动查询
if (messageText.StartsWith("/gongtongqunzu"))
{
    var chatId = message.Chat.Id;
    var userId = message.From.Id;
    var targetGroupId = -1001862069013; // 指定的群组ID

    try
    {
        var member = await botClient.GetChatMemberAsync(targetGroupId, userId);
        // 检查用户的状态，如果状态不是 left 或 kicked，表示用户在群组中
        if (member.Status != ChatMemberStatus.Left && member.Status != ChatMemberStatus.Kicked)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "存在共同群！"
            );
        }
        else
        {
            // 用户不在群组中
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "暂无共同群！"
            );
        }
    }
    catch (Exception ex)
    {
        // 如果出现异常，可能是因为机器人没有足够的权限或其他原因
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "无法检查您的群组状态，请确保机器人具有查看群组成员信息的权限。"
        );
    }
}   
// 检查消息是否为纯数字，如果是，则计算上涨和下跌的数据
if (decimal.TryParse(messageText, out decimal number))
{
    var responseText = new StringBuilder($"{number} 涨跌 1-10% 数据\n\n");

    for (int i = 1; i <= 10; i++)
    {
        decimal downPercentage = 1m - (i / 100m);
        decimal upPercentage = 1m + (i / 100m);
        decimal down = Math.Round(number * downPercentage, 8, MidpointRounding.AwayFromZero); // 下跌
        decimal up = Math.Round(number * upPercentage, 8, MidpointRounding.AwayFromZero); // 上涨
        responseText.AppendLine($"`- {i}%  {down} | {up}  +{i}%`");
    }

    _ = botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: responseText.ToString(),
        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown
	//replyToMessageId: message.MessageId // 这里引用用户的消息ID    
    );
}
else if (messageText.Contains("~") || messageText.Contains("～"))
{
    var parts = messageText.Split(new[] { '~', '～' }, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 2 && decimal.TryParse(parts[0], out decimal start) && decimal.TryParse(parts[1], out decimal end))
    {
        string responseMessage;
        if (start < end)
        {
            // 计算上涨百分比
            decimal increasePercentage = Math.Round((end - start) / start * 100, 2);
            responseMessage = $"从{start}到{end}，上涨 {increasePercentage}%";
        }
        else
        {
            // 计算下跌百分比
            decimal decreasePercentage = Math.Round((start - end) / start * 100, 2);
            responseMessage = $"从{start}到{end}，下跌 {decreasePercentage}%";
        }

        _ = botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: responseMessage
	    //replyToMessageId: message.MessageId // 这里引用用户的消息ID	
        );
    }
}
// 在处理消息的地方，当机器人收到 /jisuzhangdie 消息或者 "市场异动" 文本时
if (messageText.StartsWith("/jisuzhangdie") || messageText.Contains("市场异动"))
{
    var userId = message.From.Id;
    var today = DateTime.UtcNow.AddHours(8).Date; // 转换为北京时间并获取日期部分
    bool allowQuery = true; // 默认允许查询

    // 检查用户是否已经查询过
    if (userJisuZhangdieLimits.ContainsKey(userId))
    {
        var (count, lastQueryDate) = userJisuZhangdieLimits[userId]; // 取出元组
        if (lastQueryDate == today && count >= 1)
        {
            try
            {
                var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                if (member.Status == ChatMemberStatus.Left || member.Status == ChatMemberStatus.Kicked)
                {
                    // 用户不在群组中
                    var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                    {
                        InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                    });

                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可不限制查询！",
                        replyMarkup: keyboard,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                    );
                    return;
                }
                // 如果用户在群组中，不需要更新查询次数，直接进行查询
            }
            catch (Exception)
            {
                // 发生异常，可能是因为机器人不在群组中或群组ID错误，允许查询
                allowQuery = true;
            }
        }
        else if (lastQueryDate != today)
        {
            // 如果今天是用户第一次查询，重置查询次数和日期
            userJisuZhangdieLimits[userId] = (1, today);
        }
        else
        {
            // 如果用户今天的查询次数还没有用完，增加查询次数
            userJisuZhangdieLimits[userId] = (count + 1, today);
        }
    }
    else
    {
        // 如果用户之前没有查询过，添加新的记录
        userJisuZhangdieLimits[userId] = (1, today);
    }

    if (allowQuery)
    {
        // 执行查询逻辑
        await CryptoPriceMonitor.StartMonitoringAsync(botClient, message.Chat.Id);
    }
}
// 检查是否接收到了 /ucard 消息或文本包含特定关键词，收到就回复用户
if (messageText.StartsWith("/ucard") || messageText.Contains("银行卡") || messageText.Contains("yhk") || messageText.Contains("消费卡") || messageText.Contains("信用卡") || messageText.Contains("虚拟"))
{
    // 首先发送一张图片
    var imageUrl = "https://i.postimg.cc/GhQHdgVp/Dupay-Card.webp";
    var inlineKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(
        new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[][]
        {
            new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[]
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithUrl("立即开卡", "https://dupay.one/web-app/register-h5?invitCode=625174&lang=zh-cn")
            }
        }
    );

    _ = botClient.SendPhotoAsync(
        chatId: message.Chat.Id,
        photo: imageUrl,
        caption: "年轻人的第一张u卡，<b>免实名  无冻卡风险</b> ！\n充值 <b>USDT</b> 即可绑定美团/微信/支付宝消费！！\n同时支持包括苹果商店/谷歌商店等一切平台！！！\n\n注册邀请码： <b>625174</b>\n注册链接：https://dupay.one/web-app/register-h5?invitCode=625174&lang=zh-cn\n\n使用邀请码或链接注册，即可享受 <b>0手续费！</b> 随用随充，随心所欲！",
        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
        replyMarkup: inlineKeyboard
    );
}
// 检查是否接收到了 /feixiaohao 消息，收到就启动数据获取
if (messageText.StartsWith("/feixiaohao"))
{
    await CoinDataCache.EnsureCacheInitializedAsync(); // 确保缓存已初始化
    var cryptoData = CryptoDataFetcher.FetchAndFormatCryptoDataAsync(1, 50); // 注意这里不再使用 await
    var replyMarkup = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithCallbackData("TOP51-100数据", "xiaohao"),
        InlineKeyboardButton.WithCallbackData("返回", "back")
    });
    _ = botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: cryptoData,
        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
        replyMarkup: replyMarkup
    ).ConfigureAwait(false); // 保持异步调用的非阻塞性
}
else if (messageText.StartsWith("/xiaohao"))
{
    await CoinDataCache.EnsureCacheInitializedAsync(); // 确保缓存已初始化
    var cryptoData = CryptoDataFetcher.FetchAndFormatCryptoDataAsync(51, 100); // 注意这里不再使用 await
    var replyMarkup = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithCallbackData("TOP1-50数据", "feixiaohao"),
        InlineKeyboardButton.WithCallbackData("返回", "back")
    });
    _ = botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: cryptoData,
        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
        replyMarkup: replyMarkup
    ).ConfigureAwait(false); // 保持异步调用的非阻塞性
}
// 在处理消息的地方，当机器人收到 /caifu 消息或者 "财富密码" 文本时
if (messageText.StartsWith("/caifu") || messageText.Equals("财富密码"))
{
    var userId = message.From.Id;
    var today = DateTime.UtcNow.AddHours(8).Date; // 转换为北京时间并获取日期部分
    bool allowQuery = true; // 默认允许查询

    // 检查用户是否已经查询过
    if (userShizhiLimits.ContainsKey(userId))
    {
        var (count, lastQueryDate) = userShizhiLimits[userId]; // 取出元组
        if (lastQueryDate == today && count >= 1)
        {
            try
            {
                var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                if (member.Status == ChatMemberStatus.Left || member.Status == ChatMemberStatus.Kicked)
                {
                    // 用户不在群组中
                    var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                    {
                        InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                    });

                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可不限制查询！",
                        replyMarkup: keyboard,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                    );
                    return;
                }
                // 如果用户在群组中，不需要更新查询次数，直接进行查询
            }
            catch (Exception)
            {
                // 发生异常，可能是因为机器人不在群组中或群组ID错误，允许查询
                allowQuery = true;
            }
        }
        else if (lastQueryDate != today)
        {
            // 如果今天是用户第一次查询，重置查询次数和日期
            userShizhiLimits[userId] = (1, today);
        }
        else
        {
            // 如果用户今天的查询次数还没有用完，增加查询次数
            userShizhiLimits[userId] = (count + 1, today);
        }
    }
    else
    {
        // 如果用户之前没有查询过，添加新的记录
        userShizhiLimits[userId] = (1, today);
    }

    if (allowQuery)
    {
        // 执行查询逻辑
        await CryptoMarketAnalyzer.AnalyzeAndReportAsync(botClient, message.Chat.Id);
    }
}
// 检查是否接收到了查询特定币种信息的消息，例如"查btc"
if (messageText.StartsWith("查"))
{
    string coinSymbol = messageText.Substring(1).Trim(); // 从消息文本中提取币种简称
    _ = QueryCoinInfoAsync(botClient, message.Chat.Id, coinSymbol);
}
//根据时间查询币种数据
// 在处理消息的地方，当机器人收到 /1hshuju 消息时
if (messageText.StartsWith("/1hshuju"))
{
    var userId = message.From.Id;
    var today = DateTime.UtcNow.AddHours(8).Date; // 转换为北京时间并获取日期部分
    bool allowQuery = true; // 默认允许查询

    // 检查用户是否已经查询过
    if (user1hShujuLimits.ContainsKey(userId))
    {
        var (count, lastQueryDate) = user1hShujuLimits[userId]; // 取出元组
        if (lastQueryDate == today && count >= 1)
        {
            try
            {
                var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                if (member.Status == ChatMemberStatus.Left || member.Status == ChatMemberStatus.Kicked)
                {
                    // 用户不在群组中
                    var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                    {
                        InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                    });

                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可不限制查询！",
                        replyMarkup: keyboard,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                    );
                    return;
                }
                // 如果用户在群组中，不需要更新查询次数，直接进行查询
            }
            catch (Exception)
            {
                // 发生异常，可能是因为机器人不在群组中或群组ID错误，允许查询
                allowQuery = true;
            }
        }
        else if (lastQueryDate != today)
        {
            // 如果今天是用户第一次查询，重置查询次数和日期
            user1hShujuLimits[userId] = (1, today);
        }
        else
        {
            // 如果用户今天的查询次数还没有用完，增加查询次数
            user1hShujuLimits[userId] = (count + 1, today);
        }
    }
    else
    {
        // 如果用户之前没有查询过，添加新的记录
        user1hShujuLimits[userId] = (1, today);
    }

    if (allowQuery)
    {
        // 执行查询逻辑
        var (replyMessage, inlineKeyboard) = await CoinDataCache.GetTopMoversAsync("1h");
        await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: replyMessage, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, replyMarkup: inlineKeyboard);
    }
}
// 在处理消息的地方，当机器人收到 /24hshuju 消息时
if (messageText.StartsWith("/24hshuju"))
{
    var userId = message.From.Id;
    var today = DateTime.UtcNow.AddHours(8).Date; // 转换为北京时间并获取日期部分
    bool allowQuery = true; // 默认允许查询

    // 检查用户是否已经查询过
    if (user24hQueryLimits.ContainsKey(userId))
    {
        var (count, lastQueryDate) = user24hQueryLimits[userId]; // 取出元组
        if (lastQueryDate == today && count >= 1)
        {
            try
            {
                var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                if (member.Status == ChatMemberStatus.Left || member.Status == ChatMemberStatus.Kicked)
                {
                    // 用户不在群组中
                    var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                    {
                        InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                    });

                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可不限制查询！",
                        replyMarkup: keyboard,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                    );
                    return;
                }
                // 如果用户在群组中，不需要更新查询次数，直接进行查询
            }
            catch (Exception)
            {
                // 发生异常，可能是因为机器人不在群组中或群组ID错误，允许查询
                allowQuery = true;
            }
        }
        else if (lastQueryDate != today)
        {
            // 如果今天是用户第一次查询，重置查询次数和日期
            user24hQueryLimits[userId] = (1, today);
        }
        else
        {
            // 如果用户今天的查询次数还没有用完，增加查询次数
            user24hQueryLimits[userId] = (count + 1, today);
        }
    }
    else
    {
        // 如果用户之前没有查询过，添加新的记录
        user24hQueryLimits[userId] = (1, today);
    }

    if (allowQuery)
    {
        // 执行查询逻辑
        var (replyMessage, inlineKeyboard) = await CoinDataCache.GetTopMoversAsync("24h");
        await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: replyMessage, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, replyMarkup: inlineKeyboard);
    }
}
else if (messageText.StartsWith("/7dshuju"))
{
    var userId = message.From.Id;
    var today = DateTime.UtcNow.AddHours(8).Date; // 转换为北京时间并获取日期部分
    bool allowQuery = true; // 默认允许查询

    // 检查用户是否已经查询过
    if (user7dQueryLimits.ContainsKey(userId))
    {
        var (count, lastQueryDate) = user7dQueryLimits[userId]; // 取出元组
        if (lastQueryDate == today && count >= 1)
        {
            try
            {
                var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                if (member.Status == ChatMemberStatus.Left || member.Status == ChatMemberStatus.Kicked)
                {
                    // 用户不在群组中
                    var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                    {
                        InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                    });

                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可不限制查询！",
                        replyMarkup: keyboard,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                    );
                    return;
                }
                // 如果用户在群组中，不需要更新查询次数，直接进行查询
            }
            catch (Exception)
            {
                // 发生异常，可能是因为机器人不在群组中或群组ID错误，允许查询
                allowQuery = true;
            }
        }
        else if (lastQueryDate != today)
        {
            // 如果今天是用户第一次查询，重置查询次数和日期
            user7dQueryLimits[userId] = (1, today);
        }
        else
        {
            // 如果用户今天的查询次数还没有用完，增加查询次数
            user7dQueryLimits[userId] = (count + 1, today);
        }
    }
    else
    {
        // 如果用户之前没有查询过，添加新的记录
        user7dQueryLimits[userId] = (1, today);
    }

    if (allowQuery)
    {
        // 执行查询逻辑
        var (replyMessage, inlineKeyboard) = await CoinDataCache.GetTopMoversAsync("7d");
        await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: replyMessage, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, replyMarkup: inlineKeyboard);
    }
}
if (messageText.StartsWith("/genjuzhiding"))
{
    string replyMessage = "选择指定时间，返回币种上涨下跌数据：";
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new[] // 第一排按钮
        {
            InlineKeyboardButton.WithCallbackData("1小时", "/1hshuju"),
            InlineKeyboardButton.WithCallbackData("24小时", "/24hshuju"),
            InlineKeyboardButton.WithCallbackData("7天", "/7dshuju")
        },
        new[] // 第二排按钮
        {
            InlineKeyboardButton.WithCallbackData("关闭", "back")
        }
    });

    await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: replyMessage, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, replyMarkup: inlineKeyboard);
}	    
// 检查是否接收到了 /xuni 消息，收到就启动广告
if (messageText.StartsWith("/xuni"))
{
    // 如果虚拟广告没有在运行，就启动虚拟广告
    if (!isVirtualAdvertisementRunning)
    {
        isVirtualAdvertisementRunning = true; // 将变量设置为 true，表示虚拟广告正在运行

        virtualAdCancellationTokenSource = new CancellationTokenSource(); // 更新类级别的 CancellationTokenSource
        var rateRepository = provider.GetRequiredService<IBaseRepository<TokenRate>>();
        _ = SendVirtualAdvertisement(botClient, virtualAdCancellationTokenSource.Token, rateRepository, FeeRate)
            .ContinueWith(_ => isVirtualAdvertisementRunning = false); // 广告结束后将变量设置为 false

        // 向用户发送一条消息，告知他们虚拟广告已经启动
        _ = botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "兑换通知已启动！"
        );
    }
}
// 检查是否为指定用户并执行相应的操作
//if (message.From.Id == 1427768220 && (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup))
//任何人都可以开启关闭
if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
{
    var groupId = message.Chat.Id;
    var command = messageText.ToLower();
    var userMessageId = message.MessageId; // 用户消息的ID
    Message botResponseMessage = null; // 用于存储机器人发送的消息

    if (command == "关闭兑换通知")
    {
        GroupManager.BlacklistedGroupIds.Add(groupId);
        botResponseMessage = await botClient.SendTextMessageAsync(groupId, "兑换通知已关闭。");
    }
    else if (command == "开启兑换通知")
    {
        GroupManager.BlacklistedGroupIds.Remove(groupId);
        botResponseMessage = await botClient.SendTextMessageAsync(groupId, "兑换通知已开启。"); // 发送确认消息

        if (!isVirtualAdvertisementRunning)
        {
            virtualAdCancellationTokenSource = new CancellationTokenSource(); // 创建新的 CancellationTokenSource
            isVirtualAdvertisementRunning = true; // 更新运行状态
            var rateRepository = provider.GetRequiredService<IBaseRepository<TokenRate>>();
            _ = SendVirtualAdvertisement(botClient, virtualAdCancellationTokenSource.Token, rateRepository, FeeRate)
                .ContinueWith(_ => isVirtualAdvertisementRunning = false); // 广告结束后更新运行状态
        }
    }
    // ... 其他代码 ...

    // 如果机器人发送了消息，则等待1秒后尝试撤回
    if (botResponseMessage != null)
    {
        await Task.Delay(1000); // 等待1秒
        await botClient.DeleteMessageAsync(groupId, botResponseMessage.MessageId); // 尝试撤回机器人的消息
        try
        {
            await botClient.DeleteMessageAsync(groupId, userMessageId); // 尝试撤回用户的消息
        }
        catch
        {
            // 如果撤回用户消息失败，则不做任何事情
        }
    }
}
// 监控币种行情变动
if (messageText.StartsWith("/jkbtc") || messageText.Contains("行情监控"))
{
    if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
    {
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "请私聊机器人启动监控！"
        );
    }
    else
    {
        string baseResponseText = "发送 监控+数字货币 例如发送：监控 BTC\n则启动监控任务，当币价涨跌超过5%会触发提醒\n\n发送 取消监控+数字货币 例如发送： 取消监控 BTC\n则停止监控任务，后续涨跌不再下发币价波动提醒！";

        if (PriceMonitor.monitorInfos.ContainsKey(message.Chat.Id) && PriceMonitor.monitorInfos[message.Chat.Id].Count > 0)
        {
            int monitoredCount = PriceMonitor.monitorInfos[message.Chat.Id].Count;
            string monitoringListText = "\n\n监控列表：\n\n";
            monitoringListText += $"您当前监控 <b>{monitoredCount}</b> 个加密货币价格变动！\n\n";

            // 获取监控信息列表的最后一个元素的索引
            int lastIndex = PriceMonitor.monitorInfos[message.Chat.Id].Count - 1;

            foreach (var monitorInfo in PriceMonitor.monitorInfos[message.Chat.Id].Select((value, index) => new { value, index }))
            {
                decimal? currentPrice = await PriceMonitor.GetLatestPricee(monitorInfo.value.Symbol); // 使用新方法获取最新价格
                if (currentPrice.HasValue)
                {
                    var priceChangeRecords = PriceMonitor.priceAlertInfos[message.Chat.Id][monitorInfo.value.Symbol];
                    if (priceChangeRecords.Count > 0)
                    {
                        var initialPriceRecord = priceChangeRecords.First();
                        string priceChangeText = "";
                        foreach (var record in priceChangeRecords.Skip(1)) // 跳过第一条记录，因为它是初始价格
                        {
                            string changeSymbol = record.ChangePercentage > 0 ? "\U0001F4C8" : "\U0001F4C9";
                            priceChangeText += $"{record.Time:yyyy/MM/dd HH:mm} {changeSymbol} {Math.Abs(record.ChangePercentage):0.00}% $ {record.Price}\n";
                        }

                        string formattedInitialPrice = initialPriceRecord.Price >= 1 ? initialPriceRecord.Price.ToString("F2") : initialPriceRecord.Price.ToString("0.00000000");
                        string formattedCurrentPrice = currentPrice.Value >= 1 ? currentPrice.Value.ToString("F2") : currentPrice.Value.ToString("0.00000000");
                        decimal priceChangePercent = ((currentPrice.Value - initialPriceRecord.Price) / initialPriceRecord.Price) * 100;
                        string priceChangeDirection = priceChangePercent > 0 ? "\U0001F4C8" : "\U0001F4C9";
                        monitoringListText += $"<code>{monitorInfo.value.Symbol}</code><b>/USDT</b>   <b>初始价格：</b>$ {formattedInitialPrice}\n<b>最新价格：</b>$ {formattedCurrentPrice}  {priceChangeDirection} {Math.Abs(priceChangePercent).ToString("0.00")}%\n{priceChangeText}";

                        // 如果当前币种不是列表中的最后一个，则在其后添加横线
                        if (monitorInfo.index != lastIndex)
                        {
                            monitoringListText += "-----------------------------------------------------\n";
                        }
                    }
                }
            }

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: baseResponseText + monitoringListText,
                parseMode: ParseMode.Html
            );
        }
        else
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: baseResponseText,
                parseMode: ParseMode.Html
            );
        }
    }
}
//加密货币 监控和取消监控任务
if (Regex.IsMatch(messageText, @"^监控\s*\S+", RegexOptions.IgnoreCase))
{
    if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
    {
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "请私聊机器人启动监控！"
        );
    }
    else
    {
        var match = Regex.Match(messageText, @"^监控\s*(\S+)$", RegexOptions.IgnoreCase);
        var symbol = match.Groups[1].Value.Trim();
        await PriceMonitor.Monitor(botClient, message.Chat.Id, symbol);
    }
}
else if (Regex.IsMatch(messageText, @"^取消监控\s*\S+", RegexOptions.IgnoreCase))
{
    if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
    {
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "请私聊机器人取消监控！"
        );
    }
    else
    {
        var match = Regex.Match(messageText, @"^取消监控\s*(\S+)$", RegexOptions.IgnoreCase);
        var symbol = match.Groups[1].Value.Trim();
        await PriceMonitor.Unmonitor(botClient, message.Chat.Id, symbol);
    }
}
// 检查是否接收到了 "时间"、"shijian"、"日期" 或 "sj" 中的任意一个消息，收到就返回当前北京时间
if (messageText.Contains("时间") || messageText.Contains("shijian") || messageText.Contains("日期") || messageText.Contains("sj"))
{
    // 获取当前北京时间（UTC+8）
    DateTime beijingTime = DateTime.UtcNow.AddHours(8);
    string weekDay = beijingTime.ToString("dddd", new System.Globalization.CultureInfo("zh-CN"));
    string responseText = $"<b>北京时间：</b>\n\n{beijingTime:yyyy/MM/dd HH:mm:ss} {weekDay}\n\n" +
                          "一月：  <b>Jan</b>\n二月：  <b>Feb</b>\n三月：  <b>Mar</b>\n四月：  <b>Apr</b>\n五月：  <b>May</b>\n六月：  <b>Jun</b>\n" +
                          "七月：  <b>Jul</b>\n八月：  <b>Aug</b>\n九月：  <b>Sep</b>\n十月：  <b>Oct</b>\n十一月：  <b>Nov</b>\n十二月：  <b>Dec</b>";

    // 向用户发送当前北京时间和月份对照表，使用HTML格式以支持加粗
    _ = botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: responseText,
        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
	replyToMessageId: message.MessageId//回复用户的文本    
    );
}
// 检查是否接收到了包含“绑定”和“备注”的消息
if (messageText.Contains("绑定") && messageText.Contains("备注"))
{
    var parts = messageText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    // 确保消息格式正确，并且包含波场地址
    if (parts.Length >= 3 && Regex.IsMatch(parts[1], @"^T[A-Za-z0-9]{33}$"))
    {
        var tronAddress = parts[1];
        var note = string.Empty;
        // 查找以“备注”开头的段落
        var noteKeywordIndex = Array.FindIndex(parts, part => part.StartsWith("备注"));
        if (noteKeywordIndex != -1 && noteKeywordIndex < parts.Length - 1)
        {
            // 提取“备注”之后的所有文本作为备注信息
            note = string.Join(" ", parts.Skip(noteKeywordIndex + 1));
        }

        // 如果备注信息超过10个字符，只保留前10个字符，并添加"..."
        if (note.Length > 10)
        {
            note = note.Substring(0, 10) + "...";
        }

        // 存储用户的地址和备注信息
        userAddressNotes[(message.From.Id, tronAddress)] = note;

        // 向用户发送一条消息，告知他们地址和备注已经更新
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "地址备注已更新！"
        );
    }
}
//查询所有币价        
if (messageText.Equals("TRX", StringComparison.OrdinalIgnoreCase) || messageText.Equals("trx", StringComparison.OrdinalIgnoreCase))
{
    // 如果消息是"TRX"或"trx"，则返回特殊的消息
    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: "<b>TRX能量兑换地址</b>：\n\n<code>TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv</code>",
        parseMode: ParseMode.Html
    );
}
else if (messageText.Contains("#")) // 检查消息是否包含#
{
    // 提取加密货币标识
    var match = Regex.Match(messageText, @"#([a-zA-Z0-9]+)");
    if (match.Success)
    {
        var symbol = match.Groups[1].Value.ToUpper(); // 加密货币标识，转大写
        
        // 特殊处理#TRX
        if (symbol == "TRX")
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
            });

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "TRX价格走势请进交流群查看！",
                replyMarkup: inlineKeyboard
            );
        }
        else
        {
            var beijingTime = DateTime.UtcNow.AddHours(8); // 将当前UTC时间转换为北京时间
            var formattedTime = beijingTime.ToString("yyyy/MM/dd HH.mm"); // 格式化时间字符串

            // 构造查询文本
            var queryText = $"{symbol} {formattedTime}";
            // 调用查询加密货币价格趋势的方法
            await QueryCryptoPriceTrendAsync(botClient, message.Chat.Id, queryText);
        }
    }
}	    
else if (Regex.IsMatch(messageText, @"^trx\s+\d{4}/\d{2}/\d{2}\s+\d{2}\.\d{2}$", RegexOptions.IgnoreCase)) // 检查消息是否为"TRX+时间"的格式，允许多个空格
{
    // 如果消息是"TRX+时间"的格式，直接回复用户
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
    });

    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: "TRX价格走势请进交流群查看！",
        replyMarkup: inlineKeyboard
    );
}	    
else if (Regex.IsMatch(messageText, @"^[a-zA-Z0-9]{2,}\s+\d{4}/\d{2}/\d{2}\s+\d{2}\.\d{2}$")) // 检查消息是否符合币种和时间的格式，允许多个空格
{
    // 如果消息符合币种和时间的格式，调用查询加密货币价格趋势的方法
    await QueryCryptoPriceTrendAsync(botClient, message.Chat.Id, messageText);
}
else if (Regex.IsMatch(messageText, @"^[a-zA-Z0-9]+$")) // 检查消息是否包含字母和数字的组合
{
    var symbol = messageText.ToUpper(); // 将消息转换为大写
    var url = $"https://api.binance.com/api/v3/ticker/24hr?symbol={symbol}USDT"; // 构造API URL

    using (var httpClient = new HttpClient())
    {
        try
        {
            var response = await httpClient.GetStringAsync(url); // 调用API
            var json = JObject.Parse(response); // 解析API返回的JSON数据

            if (json != null)
            {
                if (json["lastPrice"] != null && json["highPrice"] != null && json["lowPrice"] != null && json["priceChangePercent"] != null)
                {
// 获取历史K线数据
var klineResponse = await httpClient.GetAsync($"https://api.binance.com/api/v3/klines?symbol={symbol}USDT&interval=1d&limit=1000");
var klineDataRaw = JsonSerializer.Deserialize<List<List<JsonElement>>>(await klineResponse.Content.ReadAsStringAsync());

var klineData = klineDataRaw.Select(item => new KlineDataItem
{
    OpenTime = item[0].GetInt64(),
    Open = item[1].GetString(),
    High = item[2].GetString(),
    Low = item[3].GetString(),
    Close = item[4].GetString()
    // 其他字段...
}).ToList();

// 计算连续上涨或下跌的天数
var (riseDays, fallDays) = GetContinuousRiseFallDays(klineData);
			
// 尝试从本地缓存获取市值、流通量和图片URL
string imageUrl = null;
decimal marketCap = 0;
decimal circulatingSupply = 0;
string formattedMarketCap = "未收录";
string formattedCirculatingSupply = "未收录";
string rankText = null; // 用于存储币种排名信息
			
var coinInfo = await CoinDataCache.GetCoinInfoAsync(symbol);
if (coinInfo != null && coinInfo.Count > 0)
{
    //Console.WriteLine($"从本地缓存获取到数据：{symbol}");
    // 从缓存获取信息
    imageUrl = coinInfo.TryGetValue("logo_png", out JsonElement logoElement) ? logoElement.GetString() : null;
    if (imageUrl != null) //Console.WriteLine($"从本地缓存获取到图片: {imageUrl}");

    marketCap = coinInfo.TryGetValue("market_cap_usd", out JsonElement marketCapElement) && marketCapElement.TryGetDecimal(out decimal mc) ? mc : 0;
    circulatingSupply = coinInfo.TryGetValue("available_supply", out JsonElement supplyElement) && supplyElement.TryGetDecimal(out decimal cs) ? cs : 0;

    formattedMarketCap = marketCap > 0 ? string.Format("{0:N0}", marketCap) : "未收录";
    if (marketCap > 100000000)
    {
        var marketCapInBillion = marketCap / 100000000;
        formattedMarketCap += $" ≈ {marketCapInBillion:N2}亿";
    }
   // Console.WriteLine($"从本地缓存获取到市值: {formattedMarketCap}");
    formattedCirculatingSupply = circulatingSupply > 0 ? string.Format("{0:N0}", circulatingSupply) : "未收录";
   // Console.WriteLine($"从本地缓存获取到流通量: {formattedCirculatingSupply}");
    // 尝试获取币种排名
    if (coinInfo.TryGetValue("rank", out JsonElement rankElement) && rankElement.TryGetInt32(out int rank))
    {
        rankText = $"<b>  |  No.{rank}   </b>"; // 格式化排名信息，加粗显示
    }	
}
else
{
   // Console.WriteLine($"本地缓存中未找到数据，从API获取：{symbol}");
    // 如果本地缓存没有数据，从API获取
    try
    {
        var marketCapUrl = $"https://min-api.cryptocompare.com/data/pricemultifull?fsyms={symbol}&tsyms=USD";
        var marketCapResponse = await httpClient.GetStringAsync(marketCapUrl);
        var marketCapJson = JObject.Parse(marketCapResponse);
        marketCap = marketCapJson["RAW"][symbol]["USD"]["CIRCULATINGSUPPLYMKTCAP"].Value<decimal>();
        circulatingSupply = marketCapJson["RAW"][symbol]["USD"]["CIRCULATINGSUPPLY"].Value<decimal>();
        formattedMarketCap = string.Format("{0:N0}", marketCap);
        formattedCirculatingSupply = string.Format("{0:N0}", circulatingSupply);

        if (marketCap > 100000000)
        {
            var marketCapInBillion = marketCap / 100000000;
            formattedMarketCap += $" ≈ {marketCapInBillion:N2}亿";
        }
        if (marketCap == 0)
        {
            formattedMarketCap = "未收录";
        }
        if (circulatingSupply == 0)
        {
            formattedCirculatingSupply = "未收录";
        }

        // 获取图片URL
        imageUrl = marketCapJson["DISPLAY"][symbol]["USD"]["IMAGEURL"]?.ToString();
        if (!string.IsNullOrEmpty(imageUrl))
        {
            imageUrl = $"https://www.cryptocompare.com{imageUrl}";
        }
        //Console.WriteLine($"从API获取到图片: {imageUrl}");
       // Console.WriteLine($"从API获取到市值: {formattedMarketCap}");
       // Console.WriteLine($"从API获取到流通量: {formattedCirculatingSupply}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error when getting market cap, circulating supply, and image URL from API: {ex.Message}");
    }
}

// 如果连续上涨或下跌的天数大于2，就添加到返回的消息中
string reply;
if (riseDays > 2)
{
    reply = $"<b> <code>{symbol}</code>/USDT 数据 {rankText} 连续上涨{riseDays}天！</b>\n\n";
}
else if (fallDays > 2)
{
    reply = $"<b> <code>{symbol}</code>/USDT 数据 {rankText} 连续下跌{fallDays}天！</b>\n\n";
}
else
{
    reply = $"<b> <code>{symbol}</code>/USDT 数据 {rankText}   </b>\n\n";
}

// 构建回复消息
reply += $"<b>\U0001F4B0总市值：</b>{formattedMarketCap}\n";
reply += $"<b>\U0001F4B0流通量：</b>{formattedCirculatingSupply}\n"; // 添加流通量信息
			
// 获取永续合约价格
string futuresPrice = "该币种未上线永续合约";
try
{
    var futuresPriceResponse = await httpClient.GetAsync($"https://fapi.binance.com/fapi/v1/ticker/price?symbol={symbol}USDT");
    var futuresPriceData = JsonSerializer.Deserialize<FuturesPrice>(await futuresPriceResponse.Content.ReadAsStringAsync());
    if (futuresPriceData != null && !string.IsNullOrEmpty(futuresPriceData.price))
    {
        futuresPrice = FormatPrice(decimal.Parse(futuresPriceData.price));
    }
}
catch (Exception)
{
    // 如果获取永续合约价格失败，假设该币种没有上架永续合约
    // 不显示任何信息
}

string upSymbol = "\U0001F4C8"; // 📈
string downSymbol = "\U0001F4C9"; // 📉
var lastPrice = FormatPrice(decimal.Parse((string)json["lastPrice"]));
var highPrice = FormatPrice(decimal.Parse((string)json["highPrice"]));
var lowPrice = FormatPrice(decimal.Parse((string)json["lowPrice"]));

// 判断涨跌幅正负，选择相应的符号，并决定是否添加+号
decimal priceChangePercent = decimal.Parse((string)json["priceChangePercent"]);
string priceChangeSymbol = priceChangePercent >= 0 ? upSymbol : downSymbol;
string priceChangeSign = priceChangePercent > 0 ? "+" : ""; // 如果涨跌幅大于0，添加+号

reply += $"<b>\U0001F4B0现货价格：</b>{lastPrice}\n" +  
        $"<b>\U0001F4B0合约价格：</b>{futuresPrice}\n";

// 尝试从Coinbase获取价格
string coinbasePrice = null;
try
{
    var coinbaseUrl = $"https://api.pro.coinbase.com/products/{symbol}-USD/ticker";
    var coinbaseResponse = await httpClient.GetStringAsync(coinbaseUrl);
    var coinbaseJson = JObject.Parse(coinbaseResponse);
    coinbasePrice = coinbaseJson["price"]?.ToString();
}
catch (Exception ex)
{
    Console.WriteLine($"Error when calling Coinbase API: {ex.Message}");
    // 如果获取失败，coinbasePrice保持为null
}

// 根据是否获取到Coinbase的价格动态添加到消息中
if (!string.IsNullOrEmpty(coinbasePrice))
{
    reply += $"<b>\U0001F4B0coinbase：</b>{coinbasePrice}\n";
}

// 继续构建剩余的回复消息...
reply += $"<b>⬆️今日最高价：</b>{highPrice}\n" +
         $"<b>⬇️今日最低价：</b>{lowPrice}\n" +
         $"<b>全天涨跌幅：</b>{priceChangeSymbol} {priceChangeSign}{json["priceChangePercent"]}%\n";

// 计算历史最高价和最低价
var historicalHighItem = klineData.OrderByDescending(x => decimal.Parse(x.High)).First(); // 最高价
var historicalLowItem = klineData.OrderBy(x => decimal.Parse(x.Low)).First(); // 最低价

// 格式化历史最高价和最低价
var formattedHistoricalHigh = FormatPrice(decimal.Parse(historicalHighItem.High));
var formattedHistoricalLow = FormatPrice(decimal.Parse(historicalLowItem.Low));

// 获取历史最高价和最低价的日期
var historicalHighDate = DateTimeOffset.FromUnixTimeMilliseconds(historicalHighItem.OpenTime).DateTime.ToString("yyyy/MM/dd");
var historicalLowDate = DateTimeOffset.FromUnixTimeMilliseconds(historicalLowItem.OpenTime).DateTime.ToString("yyyy/MM/dd");               

                    // 添加历史最高价和最低价到返回的消息中
                   // reply += $"<b>历史最高价：</b>{formattedHistoricalHigh}\n";
                   // reply += $"<b>历史最低价：</b>{formattedHistoricalLow}\n";                    

                    // 获取资金费
                    var fundingRateResponse = await httpClient.GetAsync($"https://fapi.binance.com/fapi/v1/premiumIndex?symbol={symbol}USDT");
                    var fundingRateData = JsonSerializer.Deserialize<FundingRate>(await fundingRateResponse.Content.ReadAsStringAsync());
                    if (fundingRateData != null && !string.IsNullOrEmpty(fundingRateData.lastFundingRate))
                    {
                        reply += $"<b>合约资金费：</b>{Math.Round(double.Parse(fundingRateData.lastFundingRate) * 100, 3)}%\n";
                    }

                    // 获取现货交易量
                    var spotVolumeResponse = await httpClient.GetAsync($"https://api.binance.com/api/v3/ticker/24hr?symbol={symbol}USDT");
                    var spotVolumeData = JsonSerializer.Deserialize<SpotVolume>(await spotVolumeResponse.Content.ReadAsStringAsync());
                    if (spotVolumeData != null && !string.IsNullOrEmpty(spotVolumeData.quoteVolume))
                    {
                        var formattedSpotVolume = string.Format("{0:N2}", double.Parse(spotVolumeData.quoteVolume));
                        reply += $"<b>现货成交量：</b>{formattedSpotVolume}\n";
                    }

                    // 获取合约交易量
                    var futuresVolumeResponse = await httpClient.GetAsync($"https://fapi.binance.com/fapi/v1/ticker/24hr?symbol={symbol}USDT");
                    var futuresVolumeData = JsonSerializer.Deserialize<FuturesVolume>(await futuresVolumeResponse.Content.ReadAsStringAsync());
                    if (futuresVolumeData != null && !string.IsNullOrEmpty(futuresVolumeData.quoteVolume))
                    {
                        var formattedFuturesVolume = string.Format("{0:N2}", double.Parse(futuresVolumeData.quoteVolume));
                        reply += $"<b>合约成交量：</b>{formattedFuturesVolume}\n";
                    }
                    // 尝试获取未平仓合约的数量
                    try
                    {
                        var openInterestResponse = await httpClient.GetAsync($"https://fapi.binance.com/fapi/v1/openInterest?symbol={symbol}USDT");
                        var openInterestData = JsonSerializer.Deserialize<OpenInterest>(await openInterestResponse.Content.ReadAsStringAsync());
                        if (openInterestData != null && !string.IsNullOrEmpty(openInterestData.openInterest))
                        {
                            var formattedOpenInterest = string.Format("{0:N2}", double.Parse(openInterestData.openInterest));
                            var openInterestValue = decimal.Parse(openInterestData.openInterest) * decimal.Parse((string)json["lastPrice"]);
                            var formattedOpenInterestValue = string.Format("{0:N2}", openInterestValue);
                            reply += $"<b>未平仓合约：</b>{formattedOpenInterestValue} \n";
                        }
                    }
                    catch (Exception)
                    {
                        // 如果获取未平仓合约的数量失败，假设该币种没有上架合约
                        // 不显示任何信息
                    } 
// 获取大户持仓量多空比信息
try
{
    var topTradersResponse = await httpClient.GetAsync($"https://fapi.binance.com/futures/data/topLongShortPositionRatio?symbol={symbol}USDT&period=1h");
    var topTradersData = JsonSerializer.Deserialize<List<TopTradersRatio>>(await topTradersResponse.Content.ReadAsStringAsync());
    if (topTradersData != null && topTradersData.Count > 0)
    {
        var latestData = topTradersData.Last();
        var longAccount = Math.Round(double.Parse(latestData.longAccount) * 100, 2);
        var shortAccount = Math.Round(double.Parse(latestData.shortAccount) * 100, 2);
        reply += $"<b>大户多空比：</b>{longAccount}% / {shortAccount}%\n";
    }
    else
    {
        //Console.WriteLine("No data returned from the API.");
    }
}
catch (Exception ex)
{
    // 如果获取大户持仓量多空比信息失败，假设该币种没有上架合约
    // 不显示任何信息
    Console.WriteLine($"Error when calling API: {ex.Message}");
}                    
// 添加历史最高价和最低价到返回的消息中
reply += $"<b>↗️历史最高：</b>{historicalHighDate}   {formattedHistoricalHigh}\n";
reply += $"<b>↘️历史最低：</b>{historicalLowDate}   {formattedHistoricalLow}\n";                     

                    reply += "-----------------------------------------------\n";

                    // 获取压力位和阻力位信息
                    var priceInfo = await BinancePriceInfo.GetPriceInfo(symbol);
                    reply += priceInfo;

// 创建内联键盘按钮
    string botUsername = "yifanfubot"; // 替换为你的机器人的用户名
    string startParameter = ""; // 如果你希望机器人在被添加到群组时收到一个特定的消息，可以设置这个参数
    string shareLink = $"https://t.me/{botUsername}?startgroup={startParameter}";
                    
// 构造推特搜索链接，根据用户查询的币种动态生成
string twitterSearchUrl = $"https://twitter.com/search?q={symbol.ToLower()}&src=typed_query";
                    
var inlineKeyboard = new InlineKeyboardMarkup(new[]
{
    new [] // 第一行
    {
        InlineKeyboardButton.WithUrl("技术分析", $"https://cn.tradingview.com/symbols/{symbol}USD/technicals/?exchange=CRYPTO"),
        InlineKeyboardButton.WithUrl("推特搜索", twitterSearchUrl),
    },
    new [] // 第二行
    {
        InlineKeyboardButton.WithCallbackData("行情监控", $"start_monitoring_{symbol}"),
        InlineKeyboardButton.WithCallbackData("一键复查", symbol),
    },
    new [] // 第三行
    {
        InlineKeyboardButton.WithUrl("行情走势", $"https://www.binance.com/zh-CN/trade/{symbol}_USDT?_from=markets&type=spot"), // 根据用户查询的币种动态生成链接
        InlineKeyboardButton.WithUrl("进群使用", shareLink) // 添加机器人到群组的链接
    }    
});

// 根据是否获取到图片URL决定发送消息的方式
if (!string.IsNullOrEmpty(imageUrl))
{
    // 如果有图片URL，则发送图片和币种信息作为图片的说明
    await botClient.SendPhotoAsync(
        chatId: message.Chat.Id,
        photo: imageUrl,
        caption: reply,
        parseMode: ParseMode.Html,
        replyMarkup: inlineKeyboard
    );
}
else
{
    // 如果没有图片URL，只发送文本消息
    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: reply,
        parseMode: ParseMode.Html,
        replyMarkup: inlineKeyboard
    );
}
                }
            }
        }
        catch (Exception ex)
        {
            // 记录错误信息
            Console.WriteLine($"Error when calling API: {ex.Message}");
        }
    }
}       
// 监控名字和用户名变更
if (message.Type == MessageType.Text || message.Type == MessageType.ChatMembersAdded)
{
    await MonitorUsernameAndNameChangesAsync(botClient, message);
} 
if (messageText.StartsWith("谷歌 "))
{
    var query = messageText.Substring(2); // 去掉 "谷歌 " 前缀

    // 发送提示消息
    var infoMessage = await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: "正在搜索，请稍后..."
    );

    var searchResults = await GoogleSearchHelper.SearchAndFormatResultsAsync(query);

    // 创建内联键盘按钮
    var openGoogleSearchButton = InlineKeyboardButton.WithUrl(
        text: "在 Google 中搜索",
        url: $"https://www.google.com/search?q={Uri.EscapeDataString(query)}"
    );

    // 创建内联键盘
    var inlineKeyboard = new InlineKeyboardMarkup(openGoogleSearchButton);

    // 编辑提示消息，附加搜索结果和内联键盘
    await botClient.EditMessageTextAsync(
        chatId: message.Chat.Id,
        messageId: infoMessage.MessageId,
        text: searchResults,
        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
        disableWebPagePreview: true, // 禁用链接预览
        replyMarkup: inlineKeyboard // 添加内联键盘
    );
}
    // 检查是否接收到了 /qdgg 消息，收到就启动广告
    if (messageText.StartsWith("/qdgg"))
    {
        // 如果广告没有在运行，就启动广告
        if (!isAdvertisementRunning)
        {
            isAdvertisementRunning = true; // 将变量设置为 true，表示广告正在运行

            var cancellationTokenSource = new CancellationTokenSource();
            var rateRepository = provider.GetRequiredService<IBaseRepository<TokenRate>>();
            _ = SendAdvertisement(botClient, cancellationTokenSource.Token, rateRepository, FeeRate)
                .ContinueWith(_ => isAdvertisementRunning = false); // 广告结束后将变量设置为 false
        // 向用户发送一条消息，告知他们广告已经启动
        _ = botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "群广告已启动！"
        );     
        }
    }

// 检查是否为指定用户并执行相应的操作
if (message.From.Id == 1427768220 && (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup))
{
    var groupId = message.Chat.Id;
    var command = messageText.ToLower();
    Message botResponseMessage = null; // 用于存储机器人发送的消息

    if (command == "关闭广告")
    {
        GroupManager.ToggleAdvertisement(groupId, false);
        botResponseMessage = await botClient.SendTextMessageAsync(groupId, "已关闭广告功能。");
    }
    else if (command == "开启广告")
    {
        GroupManager.ToggleAdvertisement(groupId, true);
        botResponseMessage = await botClient.SendTextMessageAsync(groupId, "已开启广告功能。");
    }

    // 如果机器人发送了消息，则等待1秒后尝试撤回
    if (botResponseMessage != null)
    {
        await Task.Delay(1000); // 等待1秒
        await botClient.DeleteMessageAsync(groupId, botResponseMessage.MessageId); // 尝试撤回机器人的消息
        try
        {
            await botClient.DeleteMessageAsync(groupId, message.MessageId); // 尝试撤回用户的消息
        }
        catch
        {
            // 如果撤回用户消息失败，则不做任何事情
        }
    }
}
//if (message.Text.StartsWith("@") || 
//    message.Text.StartsWith("https://t.me/") || 
//    message.Text.StartsWith("http://t.me/") || 
//    message.Text.StartsWith("t.me/") ||
//    message.Text.Trim().ToLower() == "查id" || 
//    message.Text.Trim().ToLower() == "查id")
//{
//    await HandleUsernameOrUrlMessageAsync(botClient, message);
//}
if (messageText.StartsWith("/yccl"))
{
    // 添加全局异常处理器
    AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

    // 使用 Telegram.Bot 的方法来发送消息
    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: "全局异常处理已启动！"
    );
}   
// 数字加货币代码查询汇率信息 ！！
// 合并CurrencyMappings和CurrencyAliases，同时确保货币代码也被识别
var nameToCodeMappings = CurrencyMappings
    .ToDictionary(kvp => kvp.Value.Name, kvp => kvp.Key) // 正式名称到代码
    .Concat(CurrencyAliases) // 别称到代码
    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

// 确保货币代码本身也被识别
foreach (var code in CurrencyMappings.Keys)
{
    if (!nameToCodeMappings.ContainsKey(code))
    {
        nameToCodeMappings[code] = code;
    }
}

// 尝试匹配输入中的金额和中文货币名称、别称或货币代码
var regex = new Regex(@"^((\d+|[零一二两三四五六七八九十百千万亿]+)+)\s*(([a-zA-Z]{3}|[\u4e00-\u9fa5]+)\s*)+$");
var currencyMatch = regex.Match(messageText); // 将变量名从 match 改为 currencyMatch
if (currencyMatch.Success)
{
    string inputAmountStr = currencyMatch.Groups[1].Value;
    decimal amount;

    // 检查输入值是否为中文数字，并进行转换
    if (inputAmountStr.Any(c => c >= 0x4e00 && c <= 0x9fa5))
    {
        int convertedAmount = ChineseToArabic(inputAmountStr);
        amount = convertedAmount;
    }
    else
    {
        amount = decimal.Parse(inputAmountStr);
    }

    string inputCurrency = currencyMatch.Groups[3].Value.Trim(); // 使用新的变量名 currencyMatch
    string currencyCode = nameToCodeMappings.FirstOrDefault(kvp => inputCurrency.ToUpper().Contains(kvp.Key.ToUpper())).Value;

    if (!string.IsNullOrEmpty(currencyCode))
    {
        var exchangeRates = await GetExchangeRatesAsync(amount, currencyCode);
        string currencyDisplayName = CurrencyMappings.ContainsKey(currencyCode) ? CurrencyMappings[currencyCode].Name : currencyCode;
        string buttonText = $"完整的 {amount} {currencyDisplayName} 兑换汇率表";

        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData(buttonText, $"full_ratess,{amount},{currencyCode}")
        });

        _ = botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: exchangeRates,
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard // 添加内联键盘
        );
    }
}
if (Regex.IsMatch(message.Text, @"用户名：|ID："))
{
    await HandleStoreCommandAsync(botClient, message);
}       
// 修改启动方法以匹配任何数字后跟任何字母组合的币种符号
if (Regex.IsMatch(message.Text, @"^\d+(\.\d+)?[a-zA-Z]+$", RegexOptions.IgnoreCase))
{
    await HandleCryptoCurrencyMessageAsync(botClient, message);
}
// 现货合约价格差
if (messageText.StartsWith("/bijiacha"))
{
    var userId = message.From.Id;
    var today = DateTime.UtcNow.AddHours(8).Date; // 转换为北京时间并获取日期部分
    bool allowQuery = true; // 默认允许查询

    // 检查用户是否已经查询过
    if (userQueryLimits.ContainsKey(userId))
    {
        var (count, lastQueryDate) = userQueryLimits[userId]; // 取出元组
        if (lastQueryDate == today && count >= 1)
        {
            try
            {
                var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                if (member.Status == ChatMemberStatus.Left || member.Status == ChatMemberStatus.Kicked)
                {
                    // 用户不在群组中
                    var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                    {
                        InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                    });

                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可不限制查询！",
                        replyMarkup: keyboard,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                    );
                    return;
                }
                // 如果用户在群组中，不需要更新查询次数，直接进行查询
            }
            catch (Exception)
            {
                // 发生异常，可能是因为机器人不在群组中或群组ID错误，允许查询
                allowQuery = true;
            }
        }
        else if (lastQueryDate != today)
        {
            // 如果今天是用户第一次查询，重置查询次数和日期
            userQueryLimits[userId] = (1, today);
        }
        else
        {
            // 如果用户今天的查询次数还没有用完，增加查询次数
            userQueryLimits[userId] = (count + 1, today);
        }
    }
    else
    {
        // 如果用户之前没有查询过，添加新的记录
        userQueryLimits[userId] = (1, today);
    }

    if (allowQuery)
    {
        // 执行查询逻辑
        var responseMessage = await CryptoPriceChecker.CheckPriceDifferencesAsync();
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: responseMessage,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
        );
    }
}
// 检查是否是管理员发送的 "群发" 消息
if (message.From.Id == 1427768220 && message.Text.StartsWith("群发 "))
{
    // 正确初始化 originalMessage 变量
    var originalMessage = message.Text.Substring(3); // 去掉 "群发 " 前缀
    var messageToSend = originalMessage; // 基于 originalMessage 初始化 messageToSend

    // 解析并处理多个按钮
    var buttonPattern = @"[\(\（]按钮，(.*?)[，,](.*?)[\)\）]";
    var buttonMatches = Regex.Matches(messageToSend, buttonPattern);
    List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();

foreach (Match match in buttonMatches)
{
    var buttonText = match.Groups[1].Value.Trim();
    var buttonAction = match.Groups[2].Value.Trim();
    InlineKeyboardButton button;

    // 更严格地判断按钮动作是URL还是回调数据
    // 如果buttonAction包含"."，则认为它是一个URL
    if (buttonAction.Contains(".") || Uri.IsWellFormedUriString(buttonAction, UriKind.Absolute))
    {
        // 确保URL以http://或https://开头
        if (!buttonAction.StartsWith("http://") && !buttonAction.StartsWith("https://"))
        {
            buttonAction = "http://" + buttonAction;
        }
        button = InlineKeyboardButton.WithUrl(buttonText, buttonAction);
    }
    else
    {
        button = InlineKeyboardButton.WithCallbackData(buttonText, buttonAction);
    }

    buttons.Add(button);
}

    // 从原始消息中移除所有按钮标记
    messageToSend = Regex.Replace(messageToSend, buttonPattern, "");

    // 创建内联键盘
    InlineKeyboardMarkup inlineKeyboard = null;
    if (buttons.Count > 0)
    {
        inlineKeyboard = new InlineKeyboardMarkup(buttons.Select(b => new[] { b }).ToArray());
    }

    // 处理加粗效果和链接
    // 首先处理加粗效果 如（你好，加粗）
    messageToSend = Regex.Replace(messageToSend, @"[\(\（](.*?)[，,]加粗[\)\）]", m =>
    {
        var textToBold = m.Groups[1].Value.Trim();
        return $"<b>{textToBold}</b>";
    });

    // 然后处理链接 如（你好，www.google.cn）
    messageToSend = Regex.Replace(messageToSend, @"[\(\（](.*?)[，,](.*?)[\)\）]", m =>
    {
        var text = m.Groups[1].Value.Trim();
        var url = m.Groups[2].Value.Trim();
        return $"<a href='{url}'>{text}</a>";
    });

    int total = 0, success = 0, fail = 0;
    int batchSize = 200; // 每批次群发的用户数量
    Random random = new Random();

    try
    {
        for (int i = 0; i < Followers.Count; i += batchSize)
        {
            var currentBatch = Followers.Skip(i).Take(batchSize).ToList();
            foreach (var follower in currentBatch)
            {
                total++;
                try
                {
                    await botClient.SendTextMessageAsync(
                        chatId: follower.Id, 
                        text: messageToSend, 
                        parseMode: ParseMode.Html,
                        disableWebPagePreview: true,// 关闭链接预览
			replyMarkup: inlineKeyboard); // 添加内联键盘    
                    success++;
                }
                catch (ApiRequestException e)
                {
                    // 用户不存在或已经屏蔽了机器人
                    // 在这里记录异常，然后继续向下一个用户发送消息
                    Log.Error($"Failed to send message to {follower.Id}: {e.Message}");
                    fail++;

                    // 检查错误消息以确定是否应该删除用户
                    if (e.Message.Contains("bot can't send messages to bots") ||
                        e.Message.Contains("bot was blocked by the user") ||
                        e.Message.Contains("user is deactivated") ||
                        e.Message.Contains("chat not found")||
                        e.Message.Contains("bot can't initiate conversation with a user"))
                    {
                        // 从存储库中删除用户
                        Followers.Remove(follower);
                    }
                }
            }

            // 在批次之间等待随机时间 1-2 秒
            await Task.Delay(random.Next(1000, 2001));
        }
    }
    catch (Exception ex)
    {
        // 通用异常处理，取消剩余的群发任务
        Log.Error($"An error occurred, stopping broadcast: {ex.Message}");
    }

    // 发送统计信息
    await botClient.SendTextMessageAsync(
        chatId: message.From.Id, 
        text: $"群发总数：<b>{total}</b>   成功：<b>{success}</b>  失败：<b>{fail}</b>", 
        parseMode: ParseMode.Html,
        disableWebPagePreview: true); // 关闭链接预览
}
if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
{
    var groupId = message.Chat.Id;
    var command = messageText.ToLower();

    if (command == "关闭翻译")
    {
        groupTranslationSettings[groupId] = false;
        var sentMessage1 = await botClient.SendTextMessageAsync(groupId, "已关闭翻译功能。");
        await Task.Delay(1000); // 等待5秒
        await botClient.DeleteMessageAsync(groupId, sentMessage1.MessageId); // 撤回机器人的消息
        await botClient.DeleteMessageAsync(groupId, message.MessageId); // 尝试撤回关闭翻译命令
    }
    else if (command == "开启翻译")
    {
        groupTranslationSettings[groupId] = true;
        var sentMessage2 = await botClient.SendTextMessageAsync(groupId, "已开启翻译功能。");
        await Task.Delay(1000); // 等待5秒
        await botClient.DeleteMessageAsync(groupId, sentMessage2.MessageId); // 撤回机器人的消息
        await botClient.DeleteMessageAsync(groupId, message.MessageId); // 尝试撤回开启翻译命令
    }
}
if (messageText.StartsWith("代绑") && message.From.Id == 1427768220)
{
    var parts = messageText.Split(' ');
    if (parts.Length >= 3)
    {
        var userId = long.Parse(parts[1]);
        string username = null;
        var addressIndex = 2; // 默认地址索引为2
        string address;
        string note = null;

        // 检查第三个部分是否符合地址格式，如果不符合，则认为是用户名
        if (!(parts[2].StartsWith("T") && parts[2].Length == 34))
        {
            // 第三部分不是地址，认为是用户名
            username = parts[2];
            addressIndex = 3; // 调整地址索引为3
        }

        // 根据调整后的索引获取地址
        address = parts[addressIndex];

        // 如果存在备注信息，提取备注
        if (parts.Length > addressIndex + 1)
        {
            note = string.Join(" ", parts.Skip(addressIndex + 1));
        }

        // 构造伪造的绑定命令文本
        var fakeMessageText = $"绑定 {address}" + (note != null ? $" 备注 {note}" : "");
        var fakeMessage = new Message
        {
            Chat = new Chat { Id = userId },
            From = new Telegram.Bot.Types.User { Id = userId, Username = username },
            Text = fakeMessageText
        };

        try
        {
            await BindAddress(botClient, fakeMessage, isProxyBinding: true);
            // 检查是否有备注信息，并按照格式存储
            if (note != null)
            {
                // 解析备注信息
                var noteParts = note.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var noteKeywordIndex = Array.FindIndex(noteParts, part => part.StartsWith("备注"));
                if (noteKeywordIndex != -1 && noteKeywordIndex < noteParts.Length - 1)
                {
                    var actualNote = string.Join(" ", noteParts.Skip(noteKeywordIndex + 1));
                    if (actualNote.Length > 10)
                    {
                        actualNote = actualNote.Substring(0, 10) + "...";
                    }
                    // 存储用户的地址和备注信息
                    userAddressNotes[(userId, address)] = actualNote;
                }
                // 向管理员发送一条消息，告知地址和备注已经更新
                await botClient.SendTextMessageAsync(1427768220, $"代绑成功，用户ID：<code>{userId}</code> 的地址备注已更新！", parseMode: ParseMode.Html);
            }
            else
            {
                await botClient.SendTextMessageAsync(1427768220, "代绑成功。");
            }
        }
        catch (ApiRequestException ex) when (ex.Message.Contains("bot was blocked by the user"))
        {
            Console.WriteLine($"地址：{address} 代绑失败，机器人被用户：{userId} 阻止了。");
            await botClient.SendTextMessageAsync(1427768220, $"代绑失败，\n机器人被用户：<code>{userId}</code> 阻止了！", parseMode: ParseMode.Html);
        }
	catch (ApiRequestException ex) when (ex.Message.Contains("chat not found"))
        {
              Console.WriteLine($"代绑失败，因为找不到用户：{userId} 的聊天。可能是因为用户没有开始与机器人的对话。");
              await botClient.SendTextMessageAsync(1427768220, $"代绑失败，找不到用户：<code>{userId}</code> 的聊天。请确保用户已经开始与机器人的对话。", parseMode: ParseMode.Html);
        }		
        catch (Exception ex)
        {
            Console.WriteLine($"代绑失败，发生异常：{ex.Message}");
	    // 如果因为其他任何原因发送失败，则取消操作，并通知管理员	
	    await botClient.SendTextMessageAsync(1427768220, $"代绑失败，尝试向用户：<code>{userId}</code> 发送消息时发生错误。", parseMode: ParseMode.Html);	
        }
    }
    else
    {
        Console.WriteLine($"代绑请求格式错误，接收到的消息：{messageText}");
    }
}
// 批量代绑地址
if (message.From.Id == 1427768220)
{
    var qregex = new Regex(@"用户名: @?(?<username>\S*)\s+ID: (?<id>\d+)\s+绑定地址: (?<address>T\w+)\s+备注\s*(?<note>[^\-]*)", RegexOptions.Singleline);
    var matches = qregex.Matches(messageText);

    foreach (Match match in matches)
    {
        var userId = long.Parse(match.Groups["id"].Value);
        var username = match.Groups["username"].Value.Trim();
        var address = match.Groups["address"].Value.Trim();
        var note = match.Groups["note"].Value.Trim();

        // 构造伪造的绑定命令文本
        var fakeMessageText = $"绑定 {address}" + (!string.IsNullOrEmpty(note) ? $" 备注 {note}" : "");
        var fakeMessage = new Message
        {
            Chat = new Chat { Id = userId },
            From = new Telegram.Bot.Types.User { Id = userId, Username = username },
            Text = fakeMessageText
        };

        try
        {
            // 执行绑定操作
            await BindAddress(botClient, fakeMessage, isProxyBinding: true);

            try
            {
                // 尝试向用户发送绑定成功的消息
                var sentBindSuccessMessage = await botClient.SendTextMessageAsync(userId, "7*24小时监控中...");
                // 等待千分之1秒后尝试撤回消息
                await Task.Delay(1);
                await botClient.DeleteMessageAsync(userId, sentBindSuccessMessage.MessageId);
            }
            catch (ApiRequestException ex)
            {
                Console.WriteLine($"向用户 {userId} 发送消息失败，原因：{ex.Message}");
                // 发送消息失败时，执行解绑操作
                var fakeUnbindMessage = new Message
                {
                    Chat = new Chat { Id = userId },
                    From = new Telegram.Bot.Types.User { Id = userId, Username = username },
                    Text = $"解绑 {address}"
                };
                await UnBindAddress(botClient, fakeUnbindMessage);
                // 根据错误原因向管理员发送失败消息
                string failureReason = ex.Message.Contains("chat not found") ? "找不到聊天窗口" :
                                       ex.Message.Contains("bot was blocked by the user") ? "机器人被用户阻止" :
                                       ex.Message;
                await botClient.SendTextMessageAsync(1427768220, $"用户名：@{username}  用户ID： {userId}\n{address} 代绑失败，已解绑！\n失败原因：{failureReason}");
                continue; // 继续处理下一个地址
            }

            // 存储地址和备注信息
            if (!string.IsNullOrEmpty(note))
            {
                if (note.Length > 10)
                {
                    note = note.Substring(0, 10) + "...";
                }
                userAddressNotes[(userId, address)] = note;
                Console.WriteLine($"地址备注已更新：{address} 备注：{note}");
            }
            // 向管理员发送成功消息
            await botClient.SendTextMessageAsync(1427768220, $"{address} 代绑成功！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"代绑失败，用户ID：{userId} 地址：{address}。错误：{ex.Message}");
            // 处理其他类型的绑定失败情况
            await botClient.SendTextMessageAsync(1427768220, $"用户名：@{username}  用户ID： {userId}\n{address} 代绑失败。\n失败原因：{ex.Message}");
        }
    }
}
if (messageText.StartsWith("代解") && message.From.Id == 1427768220)
{
    var parts = messageText.Split(' ');
    if (parts.Length >= 3)
    {
        var userId = long.Parse(parts[1]);
        var username = parts.Length > 3 ? parts[2] : null;
        var address = parts[parts.Length - 1]; // 地址总是最后一个部分
        var fakeMessage = new Message
        {
            Chat = new Chat { Id = userId },
            From = new Telegram.Bot.Types.User { Id = userId, Username = username },
            Text = $"解绑 {address}" // 在这里添加"解绑"关键字
        };

        try
        {
            await UnBindAddress(botClient, fakeMessage); // 使用您已有的UnBindAddress方法
            await botClient.SendTextMessageAsync(1427768220, "代解成功！");
        }
        catch (ApiRequestException ex) when (ex.Message.Contains("bot was blocked by the user"))
        {
            Console.WriteLine($"地址：{address}\n代解失败，机器人被用户：{userId} 阻止了。"); // 添加调试输出
            await botClient.SendTextMessageAsync(1427768220, $"地址：<code>{address}</code>\n代解失败，机器人被用户：<code>{userId}</code> 阻止了！", parseMode: ParseMode.Html);
        }
	catch (ApiRequestException ex) when (ex.Message.Contains("chat not found"))
        {
              Console.WriteLine($"代解失败，因为找不到用户：{userId} 的聊天。可能是因为用户没有开始与机器人的对话。");
              await botClient.SendTextMessageAsync(1427768220, $"代解失败，找不到用户：<code>{userId}</code> 的聊天。请确保用户已经开始与机器人的对话。", parseMode: ParseMode.Html);
        }		
        catch (Exception ex)
        {
            Console.WriteLine($"代绑失败，发生异常：{ex.Message}");
	    // 如果因为其他任何原因发送失败，则取消操作，并通知管理员	
	    await botClient.SendTextMessageAsync(1427768220, $"代解失败，尝试向用户：<code>{userId}</code> 发送消息时发生错误。", parseMode: ParseMode.Html);	
        }
    }
    else
    {
        Console.WriteLine($"代解请求格式错误，接收到的消息：{messageText}"); // 添加调试输出
    }
}
// 检查是否接收到了 "预支" 消息，收到就发送指定文本
if (messageText.StartsWith("预支"))
{
    string adminUsername = "yifanfu";
    string adminLink = $"https://t.me/{adminUsername}";
    string responseText = "请发送需要预支TRX的钱包地址查询是否满足要求：\n同时满足2点即可预支：\n⚠️仅限累计兑换 50 USDT 以上地址，\n⚠️地址余额大于50 USDT且TRX余额低于13，\n⚠️预支的TRX能量仅够您向本机器人转账一次。\n\n如果查询满足条件，可<a href=\"" + adminLink + "\">联系管理员</a>直接预支TRX能量！";
    await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: responseText, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, disableWebPagePreview: true);
}  
if (messageText.StartsWith("/zjdh"))
{
    var transferHistoryText = await TronscanHelper.GetTransferHistoryAsync();
    
    // 创建内联键盘按钮
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new[] // 第一行按钮
        {
            InlineKeyboardButton.WithUrl("承兑地址详情", "https://www.oklink.com/cn/trx/address/TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv")
        }
    });

    // 发送带有内联按钮的消息
    await botClient.SendTextMessageAsync(
        message.Chat.Id,
        transferHistoryText,
        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
        replyMarkup: inlineKeyboard
    );
}  
// 如果用户发送的文本包含"多签"两个字
if (messageText.Contains("多签") || messageText.Contains("多重签名"))
{
    // 向用户发送多签介绍
    string multisigText = @"什么是多签功能？

在了解多签之前，先来看一下单签功能。

在加密数字货币领域，一般的交易，比如转账、授权、买卖等行为都需签名，这种理解为单签。因为只需要使用者一个人签名授权即可完成交易。

这就不难理解多签功能了，是指在交易的过程中需要多人完成签名后，该笔交易才能执行成功，否则就会失败。比如张三、李四、王麻子三个人共同管理了一个多签钱包A，张三想从A钱包转1000TRX到B钱包，此时如果李四或王麻子不同意，那张三就无法转走这笔资产。只有在李四、王麻子都同意并签名的情况下，该笔资产才能顺利转出。

TRX（波场币）多重签名（Multisig）是一种安全机制，允许多个签名者共同控制一个地址。在多重签名地址中，执行交易需要一定数量的签名者的私钥签名才能完成。这种方法可以提高资产安全性，防止因单个私钥被盗用而导致资产损失。

简单的说就是由一个或多个地址来控制你要多签的地址，原地址秘钥失效，无法再进行转账等功能，从而提高安全性！

如果需要开通多签功能，可联系管理员协助开通！";

    var contactButton = InlineKeyboardButton.WithCallbackData("联系管理", "contactAdmin");
    var inlineKeyboard = new InlineKeyboardMarkup(new[] { contactButton });

    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: multisigText,
        replyMarkup: inlineKeyboard
    );
}
// 检查是否接收到了 /cny 消息或 "合约助手"，收到就在当前聊天中发送广告
else if (messageText.StartsWith("/cny") || messageText.StartsWith("\U0001F947合约助手"))
{
    var cancellationTokenSource = new CancellationTokenSource();
    var rateRepository = provider.GetRequiredService<IBaseRepository<TokenRate>>();
    _ = SendAdvertisementOnce(botClient, cancellationTokenSource.Token, rateRepository, FeeRate, message.Chat.Id);
}        
// 添加这部分代码以处理 /crypto 和 /btc 指令
if (messageText.StartsWith("加密货币", StringComparison.OrdinalIgnoreCase) || messageText.StartsWith("/btc", StringComparison.OrdinalIgnoreCase))
{
    await SendCryptoPricesAsync(botClient, message, 1, false);
}
else
{
    // 修改正则表达式以检测至少一个运算符
    var calculatorPattern = @"^[-+]?\d+(\.\d+)?\s*([-+*/]\s*[-+]?\d+(\.\d+)?)+$";
    if (Regex.IsMatch(messageText, calculatorPattern) && messageText.IndexOfAny(new[] { '+', '-', '*', '/' }) != -1)
    {
        // 原始问题备份
        var originalQuestion = messageText;

        // 使用自定义的 EvaluateExpression 方法计算表达式
        double result = EvaluateExpression(messageText);

        // 获取用户发送的最大小数点位数
        var decimalMatches = Regex.Matches(messageText, @"\.\d+");
        int maxDecimalPlaces = 2;
        foreach (Match match in decimalMatches)
        {
            maxDecimalPlaces = Math.Max(maxDecimalPlaces, match.Value.Length - 1);
        }

        // 根据结果是否为整数选择适当的格式字符串
        string formatString = (result == (int)result) ? "{0:n0}" : "{0:n" + maxDecimalPlaces + "}";

        // 将结果转换为包含逗号分隔符的字符串
        string formattedResult = string.Format(CultureInfo.InvariantCulture, formatString, result);

        // 发送最终计算结果
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            // 使用 HTML 语法加粗结果，并附带原始问题
            text: $"<code>{System.Net.WebUtility.HtmlEncode(originalQuestion)}={formattedResult}</code>",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
        );
    }
}
// 使用正则表达式来匹配命令，允许命令后面跟随 "@机器人用户名"
var commandRegex = new Regex(@"^/usd(@\w+)?$", RegexOptions.IgnoreCase);
if (commandRegex.IsMatch(message.Text) || message.Text == "外汇助手")
{
    await HandleCurrencyRatesCommandAsync(botClient, message, 1);
}
else
{
    // 注释掉以下代码以禁用数字加货币代码的处理功能
    /*
    var regex = new Regex(@"^((\d+|[零一二两三四五六七八九十百千万亿]+)+)\s*(([a-zA-Z]{3}|[\u4e00-\u9fa5]+)\s*)+$"); // 修改这里: 添加中文数字匹配
    var match = regex.Match(message.Text);
    if (match.Success)
    {
        string inputAmountStr = match.Groups[1].Value;
        int inputAmount;

        // 检查输入值是否为中文数字
        if (inputAmountStr.Any(c => c >= 0x4e00 && c <= 0x9fa5))
        {
            inputAmount = ChineseToArabic(inputAmountStr);
        }
        else
        {
            inputAmount = int.Parse(inputAmountStr);
        }

        string inputCurrency = match.Groups[3].Value;

        string inputCurrencyCode = null;
        if (CurrencyFullNames.ContainsValue(inputCurrency))
        {
            inputCurrencyCode = CurrencyFullNames.FirstOrDefault(x => x.Value == inputCurrency).Key;
        }
        else
        {
            inputCurrencyCode = inputCurrency.ToUpper();
        }

        var rates = await GetCurrencyRatesAsync();
        if (TryGetRateByCurrencyCode(rates, inputCurrencyCode, out var rate))
        {
            decimal convertedAmount = inputAmount / rate.Value.Item1;
            string currencyFullName = CurrencyFullNames.ContainsKey(inputCurrencyCode) ? CurrencyFullNames[inputCurrencyCode] : inputCurrencyCode;
            string text = $"<b>{inputAmount.ToString("N0")}{currencyFullName} ≈ {convertedAmount.ToString("N2")}元人民币</b>";
            await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                 text: text,
                                                 parseMode: ParseMode.Html);
        }
    }
    */
}
        messageText = messageText.Replace($"@{BotUserName}", "");
        var action = messageText.Split(' ')[0] switch
        {
            "/start" => Start(botClient, message),
            "/fu" => Valuation(botClient, message),
            "U兑TRX" => ConvertCoinTRX(botClient, message), // 添加这一行
            "实时汇率" => PriceTRX(botClient, message), // 添加这一行
            "能量租赁" => zulin(botClient, message), // 添加这一行
            "/yi" => ConvertCoinTRX(botClient, message),
            "/fan" => PriceTRX(botClient, message),
            "绑定" => BindAddress(botClient, message),
            "解绑" => UnBindAddress(botClient, message),
            //"更多功能" => QueryAccount(botClient, message),
            "/vip" => QueryAccount(botClient, message), // 添加这一行
            "关闭键盘" => guanbi(botClient, message),
            _ => Usage(botClient, message)
        };
async Task<decimal> GetTotalUSDTIncomeAsync(string ReciveAddress, string contractAddress)
{
    const int PageSize = 200; // 每页查询的交易记录数量，最大值为 200
    int currentPage = 0;
    const int MaxPages = 10; // 假设最多查询10页，防止无限循环
    decimal usdtIncome = 0;
    bool hasMoreData = true;

    using (var httpClient = new HttpClient())
    {
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        while (hasMoreData && currentPage < MaxPages)
        {
            string apiEndpoint = $"https://api.trongrid.io/v1/accounts/{ReciveAddress}/transactions/trc20?only_confirmed=true&only_to=true&contract_address={contractAddress}&limit={PageSize}&start={(currentPage * PageSize) + 1}";
            try
            {
                var response = await httpClient.GetAsync(apiEndpoint);

                if (!response.IsSuccessStatusCode)
                {
                    // 请求失败，记录错误，返回当前累计的收入
                    Console.WriteLine($"API Request Failed: {response.StatusCode}");
                    break;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                JObject transactions = JObject.Parse(jsonResponse);

                foreach (var tx in transactions["data"])
                {
                    if ((string)tx["type"] != "Transfer")
                    {
                        continue;
                    }

                    var rawAmount = (decimal)tx["value"];
                    usdtIncome += rawAmount / 1_000_000L; // 假设value是以最小单位（如wei）表示的
                }

                hasMoreData = transactions["data"].Count() == PageSize;
                currentPage++;
            }
            catch (Exception ex)
            {
                // 处理异常，记录错误，然后跳出循环
                Console.WriteLine($"Error while fetching transactions: {ex.Message}");
                break;
            }
        }
    }

    return usdtIncome;
}
async Task<decimal> GetMonthlyUSDTIncomeAsync(string ReciveAddress, string contractAddress)
{
    const int PageSize = 200; // 每页查询的交易记录数量，最大值为 200
    int currentPage = 0;

    // 获取本月1号零点的时间戳
    var firstDayOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    var firstDayOfMonthMidnight = new DateTimeOffset(firstDayOfMonth).ToUnixTimeSeconds();

    decimal usdtIncome = 0;
    bool hasMoreData = true;

    while (hasMoreData)
    {
        // 调用TronGrid API以获取交易记录
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        string apiEndpoint = $"https://api.trongrid.io/v1/accounts/{ReciveAddress}/transactions/trc20?only_confirmed=true&only_to=true&min_timestamp={firstDayOfMonthMidnight * 1000}&contract_address={contractAddress}&limit={PageSize}&start={(currentPage * PageSize) + 1}";
        var response = await httpClient.GetAsync(apiEndpoint);

        if (!response.IsSuccessStatusCode)
        {
            // 请求失败，返回0
            return 0;
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        JObject transactions = JObject.Parse(jsonResponse);

        // 遍历交易记录并累计 USDT 收入
        foreach (var tx in transactions["data"])
        {
            // 只统计 type 为 "Transfer" 的交易
            if ((string)tx["type"] != "Transfer")
            {
                continue;
            }            
            
            var rawAmount = (decimal)tx["value"];
            usdtIncome += rawAmount / 1_000_000L;
        }

        // 判断是否还有更多数据
        hasMoreData = transactions["data"].Count() == PageSize;
        currentPage++;
    }

    return usdtIncome;
}
async Task<decimal> GetYearlyUSDTIncomeAsync(string ReciveAddress, string contractAddress)
{
    const int PageSize = 200; // 每页查询的交易记录数量，最大值为 200
    int currentPage = 0;

    // 获取今年1月1号零点的时间戳
    var firstDayOfYear = new DateTime(DateTime.Today.Year, 1, 1);
    var firstDayOfYearMidnight = new DateTimeOffset(firstDayOfYear).ToUnixTimeSeconds();

    decimal usdtIncome = 0;
    bool hasMoreData = true;

    while (hasMoreData)
    {
        // 调用TronGrid API以获取交易记录
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        string apiEndpoint = $"https://api.trongrid.io/v1/accounts/{ReciveAddress}/transactions/trc20?only_confirmed=true&only_to=true&min_timestamp={firstDayOfYearMidnight * 1000}&contract_address={contractAddress}&limit={PageSize}&start={(currentPage * PageSize) + 1}";
        var response = await httpClient.GetAsync(apiEndpoint);

        if (!response.IsSuccessStatusCode)
        {
            // 请求失败，返回0
            return 0;
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        JObject transactions = JObject.Parse(jsonResponse);

        // 遍历交易记录并累计 USDT 收入
        foreach (var tx in transactions["data"])
        {
            // 只统计 type 为 "Transfer" 的交易
            if ((string)tx["type"] != "Transfer")
            {
                continue;
            }            
            
            var rawAmount = (decimal)tx["value"];
            usdtIncome += rawAmount / 1_000_000L;
        }

        // 判断是否还有更多数据
        hasMoreData = transactions["data"].Count() == PageSize;
        currentPage++;
    }

    return usdtIncome;
}
async Task<decimal> GetTodayUSDTIncomeAsync(string ReciveAddress, string contractAddress)
{
    const int PageSize = 200; // 每页查询的交易记录数量，最大值为 200
    int currentPage = 0;

    // 获取今天零点的时间戳
    var todayMidnight = new DateTimeOffset(DateTime.Today).ToUnixTimeSeconds();

    decimal usdtIncome = 0;
    bool hasMoreData = true;

    while (hasMoreData)
    {
        // 调用TronGrid API以获取交易记录
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        string apiEndpoint = $"https://api.trongrid.io/v1/accounts/{ReciveAddress}/transactions/trc20?only_confirmed=true&only_to=true&min_timestamp={todayMidnight * 1000}&contract_address={contractAddress}&limit={PageSize}&start={(currentPage * PageSize) + 1}";
        var response = await httpClient.GetAsync(apiEndpoint);

        if (!response.IsSuccessStatusCode)
        {
            // 请求失败，返回0
            return 0;
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        JObject transactions = JObject.Parse(jsonResponse);

        // 遍历交易记录并累计 USDT 收入
        foreach (var tx in transactions["data"])
        {
            // 只统计 type 为 "Transfer" 的交易
            if ((string)tx["type"] != "Transfer")
            {
                continue;
            }            
            
            var rawAmount = (decimal)tx["value"];
            usdtIncome += rawAmount / 1_000_000L;
        }

        // 判断是否还有更多数据
        hasMoreData = transactions["data"].Count() == PageSize;
        currentPage++;
    }

    return usdtIncome;
}
//获取今日TRX转账记录
async Task<decimal> GetTodayTRXOutAsync(string ReciveAddress)
{
    const int PageSize = 200; // 每页查询的交易记录数量，最大值为 200
    int currentPage = 0;

    // 获取今天零点的时间戳
    var todayMidnight = new DateTimeOffset(DateTime.Today).ToUnixTimeSeconds();

    decimal trxOut = 0;
    bool hasMoreData = true;

    while (hasMoreData)
    {
        try
        {
            // 调用Tronscan API以获取交易记录
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string apiEndpoint = $"https://apilist.tronscanapi.com/api/new/transfer?sort=-timestamp&count=true&limit={PageSize}&start={(currentPage * PageSize)}&address={ReciveAddress}&filterTokenValue=1";
            var response = await httpClient.GetAsync(apiEndpoint);

            if (!response.IsSuccessStatusCode)
            {
                // 请求失败，返回0
                return 0;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            JObject transactions = JObject.Parse(jsonResponse);

// 遍历交易记录并累计 TRX 转出
foreach (var tx in transactions["data"])
{
    // 只统计今日的转出记录
    var timestamp = (long)tx["timestamp"];
    var dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
    var localDateTime = dateTimeOffset.ToOffset(TimeSpan.FromHours(8)).DateTime; // 转换为北京时间

    if (localDateTime.Date != DateTime.Today)
    {
        hasMoreData = false;
        break;
    }

    // 检查是否为支出记录
    var transferFromAddress = (string)tx["transferFromAddress"];
    if (transferFromAddress == ReciveAddress)
    {
        var rawAmount = (decimal)tx["amount"];
        trxOut += rawAmount / 1_000_000L; // TRX的数量需要除以10^6，因为API返回的是最小单位
    }
}

            currentPage++;
        }
        catch (Exception ex)
        {
            // 记录错误
            Console.WriteLine($"Error getting TRX out records: {ex.Message}");
            return 0;
        }
    }

    return trxOut;
}          
        Message sentMessage = await action;
        async Task<Message> QueryAccount(ITelegramBotClient botClient, Message message)
        {
            if (message.From == null) return message;
            var from = message.From;
            var UserId = message.Chat.Id;

if (UserId != AdminUserId)
{
    // 创建内联键盘
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new [] // 第一行按钮
        {
            InlineKeyboardButton.WithCallbackData("会员代开", "membershipOptions"),
            InlineKeyboardButton.WithCallbackData("会员表情", "memberEmojis"),
            InlineKeyboardButton.WithCallbackData("联系作者", "contactAdmin")
        },
        new [] // 第二行按钮
        {
            InlineKeyboardButton.WithCallbackData("短信接码", "smsVerification"),
            InlineKeyboardButton.WithCallbackData("靓号地址", "fancyNumbers"),
            InlineKeyboardButton.WithCallbackData("简体中文", "send_chinese")
        },
        new [] // 第三行按钮
        {
            InlineKeyboardButton.WithCallbackData("汇率换算", "send_huansuan"),
            InlineKeyboardButton.WithCallbackData("指令大全", "commandList"),
            InlineKeyboardButton.WithCallbackData("使用帮助", "send_help")
        },
        new [] // 新增第四行按钮
        {
            InlineKeyboardButton.WithCallbackData("指数行情", "indexMarket"),
            InlineKeyboardButton.WithCallbackData("在线音频", "onlineAudio"),
            InlineKeyboardButton.WithCallbackData("在线阅读", "onlineReading")
        },
        new [] // 新增第四行按钮
        {
            InlineKeyboardButton.WithCallbackData("老澳门彩", "laoaomen"),
            InlineKeyboardButton.WithCallbackData("新澳门彩", "xinaomen"),
            InlineKeyboardButton.WithCallbackData("香港六合", "xianggang")
        },
        new [] // 新增第五行按钮
        {
            InlineKeyboardButton.WithCallbackData("免实名-USDT消费卡", "energy_introo")
        }
    });

    // 向用户发送一条消息，告知他们可以选择下方按钮操作
    _ = botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: "欢迎使用本机器人，请选择下方按钮操作：",
        replyMarkup: inlineKeyboard
    );

    return message;
}
var _myTronConfig = provider.GetRequiredService<IOptionsSnapshot<MyTronConfig>>();
var _wallet = provider.GetRequiredService<IWalletClient>();
var _transactionClient = provider.GetRequiredService<ITransactionClient>();
var _contractClientFactory = provider.GetRequiredService<IContractClientFactory>();
var protocol = _wallet.GetProtocol();
var Address = _myTronConfig.Value.Address;
var addr = _wallet.ParseAddress(Address);

// 这两个变量需要在使用它们的任务之前声明
string targetReciveAddress = "TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv";
var contractAddress = _myTronConfig.Value.USDTContractAddress;            

// 同时运行获取账户资源和账户信息的任务
Task<TronNet.Protocol.AccountResourceMessage> resourceTask = protocol.GetAccountResourceAsync(new TronNet.Protocol.Account
{
    Address = addr
}).ResponseAsync;
Task<TronNet.Protocol.Account> accountTask = protocol.GetAccountAsync(new TronNet.Protocol.Account
{
    Address = addr
}).ResponseAsync;

// 同时运行获取剩余的质押能量的任务
var bandwidthTask = GetBandwidthAsync(Address);

// 同时运行获取账户余额的任务
var contractClient = _contractClientFactory.CreateClient(ContractProtocol.TRC20);
Task<decimal> USDTTask = contractClient.BalanceOfAsync(contractAddress, _wallet.GetAccount(_myTronConfig.Value.PrivateKey));

// 同时运行获取今日、本月和总收入的任务
Task<decimal> todayIncomeTask = GetTodayUSDTIncomeAsync(targetReciveAddress, contractAddress);
Task<decimal> monthlyIncomeTask = GetMonthlyUSDTIncomeAsync(targetReciveAddress, contractAddress);
//Task<decimal> totalIncomeTask = GetTotalUSDTIncomeAsync(targetReciveAddress, contractAddress);  累计收入注释掉了
Task<decimal> yearlyIncomeTask = GetYearlyUSDTIncomeAsync(targetReciveAddress, contractAddress); // 同时运行获取今年收入的任务   
Task<decimal> todayTRXOutTask = GetTodayTRXOutAsync(Address);//获取TRX今日支出            

// 等待所有任务完成
await Task.WhenAll(resourceTask, accountTask, bandwidthTask, USDTTask, todayIncomeTask, monthlyIncomeTask, yearlyIncomeTask, todayTRXOutTask);   //totalIncomeTask,  这个是累计收入



// 获取任务的结果
var resource = resourceTask.Result;
var account = accountTask.Result;
var (freeNetRemaining, freeNetLimit, netRemaining, netLimit, energyRemaining, energyLimit, transactions, transactionsIn, transactionsOut, isError) = bandwidthTask.Result;
var TRX = Convert.ToDecimal(account.Balance) / 1_000_000L;
var USDT = USDTTask.Result;
decimal todayIncome = Math.Round(todayIncomeTask.Result, 2);
decimal monthlyIncome = Math.Round(monthlyIncomeTask.Result, 2);
//decimal totalIncome = Math.Round(totalIncomeTask.Result - 30284, 2); 累计收入注释掉了
decimal yearlyIncome = Math.Round(yearlyIncomeTask.Result, 2); // 新增年度收入结果            

decimal requiredEnergy1 = 31895;
decimal requiredEnergy2 = 64895;
decimal energyPer100TRX = resource.TotalEnergyLimit * 1.0m / resource.TotalEnergyWeight * 100;
decimal requiredTRX1 = Math.Floor(requiredEnergy1 / (energyPer100TRX / 100)) + 1;
decimal requiredTRX2 = Math.Floor(requiredEnergy2 / (energyPer100TRX / 100)) + 1;  
decimal requiredBandwidth = 345;
decimal bandwidthPer100TRX = resource.TotalNetLimit * 1.0m / resource.TotalNetWeight * 100;
decimal requiredTRXForBandwidth = Math.Floor(requiredBandwidth / (bandwidthPer100TRX / 100)) + 1;
decimal todayTRXOut = Math.Round(todayTRXOutTask.Result, 2);            

// 从_rateRepository获取USDT到TRX的汇率
var _rateRepository = provider.GetRequiredService<IBaseRepository<TokenRate>>();
var rate = await _rateRepository.Where(x => x.Currency == Currency.USDT && x.ConvertCurrency == Currency.TRX).FirstAsync(x => x.Rate);
// 计算手续费后的兑换汇率
decimal usdtToTrxRateAfterFees = rate * (1 - FeeRate);

decimal TRXInUSDT;
if (usdtToTrxRateAfterFees != 0)
{
    TRXInUSDT = TRX / usdtToTrxRateAfterFees;
}
else
{
    // 根据你的需求处理这种情况，例如设置为0，或者抛出一个异常
    TRXInUSDT = 0; // 或者其他逻辑处理
    // throw new InvalidOperationException("usdtToTrxRateAfterFees cannot be zero.");
}

//累计承兑：<b>{totalIncome} USDT</b>    注释掉了 需要可以放到下面
		
var msg = @$"当前账户资源如下：
地址： <code>{Address}</code>
TRX余额： <b>{TRX}</b> | 可兑：<b>{TRXInUSDT:0.00} USDT</b>
USDT余额： <b>{USDT}</b>
免费带宽： <b>{resource.FreeNetLimit - resource.FreeNetUsed}/{resource.FreeNetLimit}</b>
质押带宽： <b>{resource.NetLimit - resource.NetUsed}/{resource.NetLimit}</b>
质押能量： <b>{energyRemaining}/{resource.EnergyLimit}</b>    
——————————————————————    
带宽质押比：<b>100 TRX = {resource.TotalNetLimit * 1.0m / resource.TotalNetWeight * 100:0.000}  带宽</b>
能量质押比：<b>100 TRX = {resource.TotalEnergyLimit * 1.0m / resource.TotalEnergyWeight * 100:0.000} 能量</b>       
 
质押 {requiredTRXForBandwidth} TRX = 345 带宽   
质押 {requiredTRX1} TRX = 31895 能量
质押 {requiredTRX2} TRX = 64895 能量     
——————————————————————    
今日承兑：<b>{todayIncome} USDT  | {todayTRXOut} TRX</b>
本月承兑：<b>{monthlyIncome} USDT</b>
年度承兑：<b>{yearlyIncome} USDT</b>                  
";
    // 创建包含三行，每行4个按钮的虚拟键盘
    var keyboard = new ReplyKeyboardMarkup(new[]
    {
        new [] // 第一行
        {
            new KeyboardButton("U兑TRX"),
            new KeyboardButton("实时汇率"),
            new KeyboardButton("查询余额"),
            new KeyboardButton("能量租赁"),
        },   
        new [] // 第二行
        {
            new KeyboardButton("外汇助手"),
            new KeyboardButton("加密货币"),
            new KeyboardButton("行情监控"),
            new KeyboardButton("地址监听"),
        },   
        new [] // 第三行
        {
            new KeyboardButton("财富密码"),
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("更多功能"),
        }
    });		
            keyboard.ResizeKeyboard = true;           
            keyboard.OneTimeKeyboard = false;
            return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                        text: msg,
                                                        parseMode: ParseMode.Html,
                                                        replyMarkup: keyboard);
        }
        async Task<Message> BindAddress(ITelegramBotClient botClient, Message message, bool isProxyBinding = false)
        {
            if (message.From == null) return message;
            if (message.Text is not { } messageText)
                return message;
    // 分割消息文本
    var parts = messageText.Split(' ');
    if (parts.Length < 2)
        return message; // 如果没有足够的部分，则返回原消息

    // 尝试提取地址
    var address = parts[1]; // 默认取第一个空格后的字符串作为地址

    // 如果存在第三部分，检查第二部分是否符合地址格式
    if (parts.Length > 2 && (!address.StartsWith("T") || address.Length != 34))
    {
        // 如果第二部分不符合地址格式，发送错误消息
        return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: $"您输入的波场地址<b>{address}</b>有误！", parseMode: ParseMode.Html);
    }

    // 如果消息来自群聊，不进行绑定
   // if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
  //  {
   //     return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "绑定失败，请私聊机器人进行绑定！");
 //   }
// 检查是否包含"TRX"，如果包含则不启动TRX余额检查
bool skipTRXMonitoring = parts.Any(part => part.Equals("TRX", StringComparison.OrdinalIgnoreCase));
            
            if (address.StartsWith("T") && address.Length == 34)
            {
        // 检查地址是否为"TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv"
        if (address == "TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv")
        {
            return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "此为机器人收款地址，绑定失败，请绑定您的钱包地址！");
        }                
                var from = message.From;
                var UserId = from.Id; // 使用发送消息的用户的ID

                var _bindRepository = provider.GetRequiredService<IBaseRepository<TokenBind>>();
                var bind = await _bindRepository.Where(x => x.UserId == UserId && x.Address == address).FirstAsync();
                if (bind == null)
                {
                    bind = new TokenBind();
                    bind.Currency = Currency.TRX;
                    bind.UserId = UserId;
                    bind.Address = address;
                    bind.UserName = $"@{from.Username}";
                    bind.FullName = $"{from.FirstName} {from.LastName}";
                    await _bindRepository.InsertAsync(bind);
    if (!skipTRXMonitoring)
    {
        // 启动定时器来监控这个地址的TRX余额
        StartMonitoring(botClient, UserId, address);
    }
                     // 启动定时器来监控这个地址的交易
                    StartUSDTMonitoring(botClient, UserId, address);
                    Console.WriteLine($"用户 {UserId} 绑定地址 {address} 成功，开始监控USDT交易记录。");

                }
                else
                {
                    bind.Currency = Currency.TRX;
                    bind.UserId = UserId;
                    bind.Address = address;
                    bind.UserName = $"@{from.Username}";
                    bind.FullName = $"{from.FirstName} {from.LastName}";
                    await _bindRepository.UpdateAsync(bind);
    if (!skipTRXMonitoring)
    {
        // 启动定时器来监控这个地址的TRX余额
        StartMonitoring(botClient, UserId, address);
    }
                   // 启动定时器来监控这个地址的交易
                   StartUSDTMonitoring(botClient, UserId, address);
                   Console.WriteLine($"用户 {UserId} 绑定地址 {address} 成功，开始监控USDT交易记录。");

                }
    // 创建包含三行，每行4个按钮的虚拟键盘
    var keyboard = new ReplyKeyboardMarkup(new[]
    {
        new [] // 第一行
        {
            new KeyboardButton("U兑TRX"),
            new KeyboardButton("实时汇率"),
            new KeyboardButton("查询余额"),
            new KeyboardButton("能量租赁"),
        },   
        new [] // 第二行
        {
            new KeyboardButton("外汇助手"),
            new KeyboardButton("加密货币"),
            new KeyboardButton("行情监控"),
            new KeyboardButton("地址监听"),
        },   
        new [] // 第三行
        {
            new KeyboardButton("财富密码"),
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("更多功能"),
        }
    });		
                keyboard.ResizeKeyboard = true; // 调整键盘高度
                keyboard.OneTimeKeyboard = false;
        // 查询USDT和TRX的余额
        var (usdtBalance, trxBalance, _) = await GetBalancesAsync(address);
        var (_, _, _, _, _, _, transactions, _, _, _) = await GetBandwidthAsync(address); // 交易笔数             

try
{
    // 在发送绑定成功消息之前检查是否是代绑操作
    if (!isProxyBinding)
    {
        // 发送绑定成功和余额的消息
        string bindSuccessMessage = $"您已成功绑定：<code>{address}</code>\n" +
                                    $"余额：<b>{usdtBalance.ToString("#,##0.##")} USDT  |  {trxBalance.ToString("#,##0.##")} TRX</b>\n" +
                                    "当我们向您的钱包转账时，您将收到通知！";
        await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: bindSuccessMessage, parseMode: ParseMode.Html, replyMarkup: keyboard);
    }

    // 等待0.5秒
    await Task.Delay(500);

    // 根据余额和交易笔数判断发送哪条文本消息
    if (usdtBalance > 10000000m || transactions > 300000)
    {
        // 如果超过阈值，先发送TRX余额监控启动的消息
        if (!skipTRXMonitoring)
        {
            await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "TRX余额监控已启动...", parseMode: ParseMode.Html);
        }
        // 等待0.5秒
        await Task.Delay(500);
        // 然后发送疑似交易所地址的警告消息
        string warningMessage = $"疑似交易所地址：\n" +
                                $"余额：<b>{usdtBalance.ToString("#,##0.##")} USDT，" +
                                $"{transactions}次交易</b>\n暂不支持监听交易所地址！";
        await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: warningMessage, parseMode: ParseMode.Html);
    }
    else
    {
        // 如果没有超过阈值，发送USDT交易监听启动的消息
        await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "USDT交易监听已启动...", parseMode: ParseMode.Html);
        // 等待0.5秒
        await Task.Delay(500);
        // 然后发送TRX余额监控启动的消息，如果没有跳过TRX监控
        if (!skipTRXMonitoring)
        {
            await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "TRX余额监控已启动...", parseMode: ParseMode.Html);
        }
    }
}
catch (Telegram.Bot.Exceptions.ApiRequestException ex)
{
    Console.WriteLine($"发送消息失败，可能的原因：{ex.Message}");
    // 这里可以添加更多的错误处理逻辑，比如记录日志等
    return null; // 发生异常时退出方法，不再继续尝试发送其他消息
}
    // 这里返回一个消息对象或者null
    return await Task.FromResult<Message>(null);
            }
            else
            {
    // 创建包含三行，每行4个按钮的虚拟键盘
    var keyboard = new ReplyKeyboardMarkup(new[]
    {
        new [] // 第一行
        {
            new KeyboardButton("U兑TRX"),
            new KeyboardButton("实时汇率"),
            new KeyboardButton("查询余额"),
            new KeyboardButton("能量租赁"),
        },   
        new [] // 第二行
        {
            new KeyboardButton("外汇助手"),
            new KeyboardButton("加密货币"),
            new KeyboardButton("行情监控"),
            new KeyboardButton("地址监听"),
        },   
        new [] // 第三行
        {
            new KeyboardButton("财富密码"),
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("更多功能"),
        }
    });		
                keyboard.ResizeKeyboard = true; // 调整键盘高度
                keyboard.OneTimeKeyboard = false;
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: $"您输入的波场地址<b>{address}</b>有误！", parseMode: ParseMode.Html, replyMarkup: keyboard);
            }
        }
async Task<Message> UnBindAddress(ITelegramBotClient botClient, Message message)
{
    if (message.From == null) return message;
    if (message.Text is not { } messageText)
        return message;
    var address = messageText.Split(' ').Last();

    var _bindRepository = provider.GetRequiredService<IBaseRepository<TokenBind>>();
    var from = message.From;
    var UserId = message.Chat.Id;
    var bind = await _bindRepository.Where(x => x.UserId == UserId && x.Address == address).FirstAsync();
    if (bind != null)
    {
        await _bindRepository.DeleteAsync(bind);
    }
    // 停止向用户发送 TRX 余额不足的提醒
    var key = (UserId, address);
    lock (timerLock)
    {
        if (userTimers.TryGetValue(key, out var timer))
        {
            timer.Dispose();
            userTimers.Remove(key);
        }
    }

    if (userTronAddresses.TryGetValue(UserId, out var addresses))
    {
        addresses.Remove(address);
        if (addresses.Count == 0)
        {
            userTronAddresses.Remove(UserId);
        }
    }
    // 停止USDT监控
    StopUSDTMonitoring(UserId, address);
    Console.WriteLine($"用户 {UserId} 解绑地址 {address} 成功，取消监控USDT交易记录。");        
    // 创建包含三行，每行4个按钮的虚拟键盘
    var keyboard = new ReplyKeyboardMarkup(new[]
    {
        new [] // 第一行
        {
            new KeyboardButton("U兑TRX"),
            new KeyboardButton("实时汇率"),
            new KeyboardButton("查询余额"),
            new KeyboardButton("能量租赁"),
        },   
        new [] // 第二行
        {
            new KeyboardButton("外汇助手"),
            new KeyboardButton("加密货币"),
            new KeyboardButton("行情监控"),
            new KeyboardButton("地址监听"),
        },   
        new [] // 第三行
        {
            new KeyboardButton("财富密码"),
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("更多功能"),
        }
    });		
    keyboard.ResizeKeyboard = true; // 调整键盘高度
    keyboard.OneTimeKeyboard = false;
    return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: $"您已成功解绑：<b>{address}</b> ", parseMode: ParseMode.Html, replyMarkup: keyboard);
}
        async Task<Message> ConvertCoinTRX(ITelegramBotClient botClient, Message message)
        {
            if (message.From == null) return message;
            var from = message.From;
            var UserId = message.From.Id;
            var _rateRepository = provider.GetRequiredService<IBaseRepository<TokenRate>>();
            var rate = await _rateRepository.Where(x => x.Currency == Currency.USDT && x.ConvertCurrency == Currency.TRX).FirstAsync(x => x.Rate);
            string adminLink = "t.me/yifanfu"; // 替换为你的管理员的Telegram链接
            string adminText = $"<a href=\"http://{adminLink}\">联系管理</a>";
            string leftPointingIndex = char.ConvertFromUtf32(0x1F448);
            

            var addressArray = configuration.GetSection("Address:USDT-TRC20").Get<string[]>();
            if (addressArray.Length == 0)
            {

                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                            text: $"管理员还未配置收款地址，请联系管理员： {AdminUserUrl}",
                                                            parseMode: ParseMode.Html,
                                                            replyMarkup: new ReplyKeyboardRemove());
            }
            var ReciveAddress = addressArray[UserId % addressArray.Length];
            var msg = @$"<b>请向此地址转入任意金额，机器人自动回款TRX</b>
            
机器人收款地址： <code>{ReciveAddress}</code>

手续费说明：手续费用于支付转账所消耗的资源，及机器人运行成本。
当前手续费：<b>兑换金额的 1% 或 1 USDT，取大者</b>

示例：
<code>转入金额：<b>10 USDT</b>
手续费：<b>1 USDT</b>
实时汇率：<b>1 USDT = {1m.USDT_To_TRX(rate, FeeRate, 0):#.####} TRX</b>
获得TRX：<b>(10 - 1) * {1m.USDT_To_TRX(rate, FeeRate, 0):#.####} = {10m.USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00} TRX</b></code>

注意：<b>只支持{MinUSDT} USDT以上的金额兑换。</b>

转帐前，推荐您使用以下命令来接收入账通知
<code>绑定 Txxxxxxx</code>(您的钱包地址)
";
            if (USDTFeeRate == 0)
            {
                msg = @$"
<b>机器人收款地址:(↓点击自动复制↓</b>):
                
<code>{ReciveAddress}</code>    

操作示例：
<code>转入金额：<b>100 USDT</b>
实时汇率：</code><del>100 USDT = {95m.USDT_To_TRX(rate, FeeRate, 0):#.####} TRX</del>
<code>您的优惠汇率：<b>100 USDT = {100m.USDT_To_TRX(rate, FeeRate, 0):#.####} TRX</b>    
可获得TRX：<b>100 * {1m.USDT_To_TRX(rate, FeeRate, 0):#.####} = {100m.USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00} TRX</b></code>
    
注意：<b>只支持{MinUSDT} USDT以上的金额兑换！</b>    
如果TRX余额不足可发送 预支 领取一次转账能量！
只限钱包转账，自动原地址返TRX，如需兑换到其它地址请{adminText}！

转帐前，推荐您绑定钱包地址来接收交易通知： 
发送：<code>绑定 Txxxxxxx</code>(您的钱包地址)         {leftPointingIndex} <b>推荐使用！！！</b> 


";
            }
    // 创建包含三行，每行4个按钮的虚拟键盘
    var keyboard = new ReplyKeyboardMarkup(new[]
    {
        new [] // 第一行
        {
            new KeyboardButton("U兑TRX"),
            new KeyboardButton("实时汇率"),
            new KeyboardButton("查询余额"),
            new KeyboardButton("能量租赁"),
        },   
        new [] // 第二行
        {
            new KeyboardButton("外汇助手"),
            new KeyboardButton("加密货币"),
            new KeyboardButton("行情监控"),
            new KeyboardButton("地址监听"),
        },   
        new [] // 第三行
        {
            new KeyboardButton("财富密码"),
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("更多功能"),
        }
    });		
            keyboard.ResizeKeyboard = true; // 将键盘高度设置为最低
            keyboard.OneTimeKeyboard = false; // 添加这一行，确保虚拟键盘在用户与其交互后保持可见            
            return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                        text: msg,
                                                        parseMode: ParseMode.Html,
                                                        disableWebPagePreview: true, // 添加这一行来禁用链接预览
                                                        replyMarkup: keyboard);
        }
async Task<Message> PriceTRX(ITelegramBotClient botClient, Message message)
{
    if (message.From == null) return message;
    var from = message.From;
    var UserId = message.From.Id;
    var _rateRepository = provider.GetRequiredService<IBaseRepository<TokenRate>>();
    var rate = await _rateRepository.Where(x => x.Currency == Currency.USDT && x.ConvertCurrency == Currency.TRX).FirstAsync(x => x.Rate);
    string adminLink = "t.me/yifanfu"; // 替换为你的管理员的Telegram链接
    string adminText = $"<a href=\"http://{adminLink}\">联系管理</a>";
    string leftPointingIndex = char.ConvertFromUtf32(0x1F448);

     // 获取 USDT 的 OTC 价格
    var usdtPrice = await GetOkxPriceAsync("usdt", "cny", "otc");

    var addressArray = configuration.GetSection("Address:USDT-TRC20").Get<string[]>();
    var ReciveAddress = addressArray.Length == 0 ? "未配置" : addressArray[UserId % addressArray.Length];

   // if (message.Chat.Id == AdminUserId) //管理直接返回资金费率  取消的话注释 5687-5708以及5764
   // {
   //     try
   //     {
  //          var fundingRates = await BinanceFundingRates.GetFundingRates();
     //       await botClient.SendTextMessageAsync(
    //            chatId: message.Chat.Id,
    //            text: fundingRates,
    //            parseMode: ParseMode.Html
   //         );
   //     }
      //  catch (Exception ex)
      //  {
      //      await botClient.SendTextMessageAsync(
      //          chatId: message.Chat.Id,
     //           text: $"获取资金费率时发生错误：{ex.Message}"
     //       );
    //    }
    //    return await Task.FromResult<Message>(null);
   // }
   // else
   // {
        var msg = @$"<b>实时汇率表：</b>
<b><del>100 USDT = {95m.USDT_To_TRX(rate, FeeRate, 0):#.####} TRX</del></b>   
            
<b>您的优惠汇率：</b>                
<b>100 USDT = {100m.USDT_To_TRX(rate, FeeRate, 0):#.####} TRX</b>            
————————————————————<code>
  10 USDT = {(5m * 2).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00}  TRX ≈ {(5m * 2) * usdtPrice}   CNY
  20 USDT = {(5m * 4).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00} TRX ≈ {(5m * 4) * usdtPrice}  CNY
  50 USDT = {(5m * 10).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00} TRX ≈ {(5m * 10) * usdtPrice}  CNY
 100 USDT = {(5m * 20).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00} TRX ≈ {(5m * 20) * usdtPrice}  CNY
 500 USDT = {(5m * 100).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00}TRX ≈ {(5m * 100) * usdtPrice} CNY
1000 USDT = {(5m * 200).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00}TRX ≈ {(5m * 200) * usdtPrice} CNY
</code>
<b>机器人收款地址:(↓点击自动复制↓</b>):
        
<code>{ReciveAddress}</code>      
    
<b>注意：只支持{MinUSDT} USDT以上的金额兑换！</b>   
<b>给机器人收款地址转u自动原地址秒回TRX！</b> 
————————————————————    
转账费用：（浮动）
对方地址有u：13.3959 TRX - 13.7409 TRX 
对方地址无u：27.2559 TRX - 27.6009 TRX 

{adminText} 租赁能量更划算：
对方地址有u：仅需7.00 TRX，节省 6.39 TRX (节省约48%)
对方地址无u：仅需13.00 TRX，节省 14.25 TRX (节省约53%)            


";

    // 创建包含三行，每行4个按钮的虚拟键盘
    var keyboard = new ReplyKeyboardMarkup(new[]
    {
        new [] // 第一行
        {
            new KeyboardButton("U兑TRX"),
            new KeyboardButton("实时汇率"),
            new KeyboardButton("查询余额"),
            new KeyboardButton("能量租赁"),
        },   
        new [] // 第二行
        {
            new KeyboardButton("外汇助手"),
            new KeyboardButton("加密货币"),
            new KeyboardButton("行情监控"),
            new KeyboardButton("地址监听"),
        },   
        new [] // 第三行
        {
            new KeyboardButton("财富密码"),
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("更多功能"),
        }
    });		

        keyboard.ResizeKeyboard = true; // 将键盘高度设置为最低
        keyboard.OneTimeKeyboard = false; // 添加这一行，确保虚拟键盘在用户与其交互后不会消失。

    // 发送带有回复键盘的消息
    var sentMessage = await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: msg,
        replyMarkup: keyboard,
        parseMode: ParseMode.Html,
        disableWebPagePreview: true
    );

    // 等待 0.01 秒
    //await Task.Delay(1);

    // 创建内联键盘
   // var inlineKeyboard = new InlineKeyboardMarkup(new[]
   // {
   //     new [] // 第一行按钮
   //     {
   //         InlineKeyboardButton.WithCallbackData("更多关于波场地址转账手续费介绍", "energy_intro") // 新增的按钮
   //     }
   // });

    // 发送带有内联键盘的消息
   // await botClient.SendTextMessageAsync(
  //      chatId: message.Chat.Id,
   //     text: "转账手续费与转账金额无关，主要看对方地址是否有USDT！",
  //      replyMarkup: inlineKeyboard
  //  );
   // }

    // 在这里添加一个返回空消息的语句
    return await Task.FromResult<Message>(null);
}
//通用回复
static async Task<Message> Start(ITelegramBotClient botClient, Message message)
{
    long userId = message.From.Id;
    var userProfilePhotos = await botClient.GetUserProfilePhotosAsync(userId);
    if (userProfilePhotos.Photos.Length > 0 && userProfilePhotos.Photos[0].Length > 0)
    {
        // 选择最小尺寸的头像版本
        var smallestPhotoSize = userProfilePhotos.Photos[0][0];
        await botClient.SendPhotoAsync(
            chatId: message.Chat.Id,
            photo: new InputOnlineFile(smallestPhotoSize.FileId)
        );
    }
    else
    {
        // 用户没有头像或无法获取，发送默认GIF
        string gifUrl = "https://i.postimg.cc/0QKYJ0Cb/333.gif";
        await botClient.SendAnimationAsync(
            chatId: message.Chat.Id,
            animation: gifUrl
        );
    }

    // 发送欢迎消息和键盘
    string username = message.From.FirstName;
    string botUsername = "yifanfubot"; // 替换为你的机器人的用户名
    string startParameter = ""; // 如果你希望机器人在被添加到群组时收到一个特定的消息，可以设置这个参数
    string shareLink = $"https://t.me/{botUsername}?startgroup={startParameter}";
    string groupFunctionText = $"<a href=\"{shareLink}\">⚠️ 点击拉我进群，有人修改资料将播报提醒！</a>";
    string uCardText = $"\U0001F4B3 免实名USDT消费卡-享全球消费\U0001F449 /ucard ";

    string usage = @$"<b>{username}</b> 你好，欢迎使用TRX自助兑换机器人！

使用方法：
   点击菜单 选择 <b>U兑TRX</b>
   转账USDT到指定地址，即可秒回TRX！
   如需了解机器人功能介绍，直接发送：<code>帮助</code> 
   
{groupFunctionText}
{uCardText}
";

    // 创建包含三行，每行4个按钮的虚拟键盘
    var keyboard = new ReplyKeyboardMarkup(new[]
    {
        new [] // 第一行
        {
            new KeyboardButton("U兑TRX"),
            new KeyboardButton("实时汇率"),
            new KeyboardButton("查询余额"),
            new KeyboardButton("能量租赁"),
        },   
        new [] // 第二行
        {
            new KeyboardButton("外汇助手"),
            new KeyboardButton("加密货币"),
            new KeyboardButton("行情监控"),
            new KeyboardButton("地址监听"),
        },   
        new [] // 第三行
        {
            new KeyboardButton("财富密码"),
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("更多功能"),
        }
    });		

        keyboard.ResizeKeyboard = true; // 将键盘高度设置为最低
        keyboard.OneTimeKeyboard = false; // 添加这一行，确保虚拟键盘在用户与其交互后不会消失。

    return await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: usage,
        parseMode: ParseMode.Html,
        disableWebPagePreview: true,
        replyMarkup: keyboard
    );
}
        //估价
       static async Task<Message> Valuation(ITelegramBotClient botClient, Message message)
{
    string usage = @$"如需换算请直接发送<b>金额+币种</b>
如发送： <code>10 USDT</code>
回复：<b>10 USDT = xxx TRX</b>

如发送： <code>100 TRX</code>
回复：<b>100 TRX = xxx USDT</b>

查外汇直接发送<b>金额+货币或代码</b>
如发送： <code>100美元</code>或<code>100usd</code>
回复：<b>100美元 ≈  xxx 元人民币</b>

查数字货币价值直接发送<b>金额+代码</b>
如发送： <code>1btc</code>或<code>1比特币</code>
回复：<b>1枚比特币的价值是：****</b>        

数字计算<b>直接对话框发送</b>
如发送：1+1
回复： <code>1+1=2</code>
        
<b>注：群内使用需要回复机器人或设置机器人为管理</b>

";

   // if (message.Chat.Id == AdminUserId)
   // {
   //     return await ExecuteZjdhMethodAsync(botClient, message);
   // }
   // else
   // {
    // 创建包含三行，每行4个按钮的虚拟键盘
    var keyboard = new ReplyKeyboardMarkup(new[]
    {
        new [] // 第一行
        {
            new KeyboardButton("U兑TRX"),
            new KeyboardButton("实时汇率"),
            new KeyboardButton("查询余额"),
            new KeyboardButton("能量租赁"),
        },   
        new [] // 第二行
        {
            new KeyboardButton("外汇助手"),
            new KeyboardButton("加密货币"),
            new KeyboardButton("行情监控"),
            new KeyboardButton("地址监听"),
        },   
        new [] // 第三行
        {
            new KeyboardButton("财富密码"),
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("更多功能"),
        }
    });		

        keyboard.ResizeKeyboard = true; // 将键盘高度设置为最低
        keyboard.OneTimeKeyboard = false; // 添加这一行，确保虚拟键盘在用户与其交互后保持可见

        return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                    text: usage,
                                                    parseMode: ParseMode.Html,
                                                    replyMarkup: keyboard);
   // }
}
//能量租赁
static async Task<Message> zulin(ITelegramBotClient botClient, Message message)
{
    // 如果你不想发送任何提示文本，可以使用空字符串，或者提供一段简短的文本
    string promptText = " ";

    // 创建包含三行，每行4个按钮的虚拟键盘
    var keyboard = new ReplyKeyboardMarkup(new[]
    {
        new [] // 第一行
        {
            new KeyboardButton("U兑TRX"),
            new KeyboardButton("实时汇率"),
            new KeyboardButton("查询余额"),
            new KeyboardButton("能量租赁"),
        },   
        new [] // 第二行
        {
            new KeyboardButton("外汇助手"),
            new KeyboardButton("加密货币"),
            new KeyboardButton("行情监控"),
            new KeyboardButton("地址监听"),
        },   
        new [] // 第三行
        {
            new KeyboardButton("财富密码"),
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("更多功能"),
        }
    });		

        keyboard.ResizeKeyboard = true; // 将键盘高度设置为最低
        keyboard.OneTimeKeyboard = false; // 添加这一行，确保虚拟键盘在用户与其交互后不会消失。

    return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                text: promptText,
                                                parseMode: ParseMode.Html,
                                                replyMarkup: keyboard);
}
static async Task<Message> ExecuteZjdhMethodAsync(ITelegramBotClient botClient, Message message)
{
    var transferHistoryText = await TronscanHelper.GetTransferHistoryAsync();

    // 创建内联键盘按钮
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new[] // 第一行按钮
        {
            InlineKeyboardButton.WithUrl("承兑地址详情", "https://www.oklink.com/cn/trx/address/TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv")
        }
    });

    // 发送带有内联按钮的消息
    return await botClient.SendTextMessageAsync(
        message.Chat.Id,
        transferHistoryText,
        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
        replyMarkup: inlineKeyboard
    );
}
        //关闭虚拟键盘
        static async Task<Message> guanbi(ITelegramBotClient botClient, Message message)
        {
            string usage = @$"键盘已关闭
";

            return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                        text: usage,
                                                        parseMode: ParseMode.Html,
                                                        replyMarkup: new ReplyKeyboardRemove());
        }
        //通用回复
        static async Task<Message> Usage(ITelegramBotClient botClient, Message message)
        {
            var text = (message.Text ?? "").ToUpper().Trim();
            // 如果输入以 "U" 结尾，将 "U" 替换为 "USDT"
            if (text.EndsWith("U"))
            {
                  text = text.Replace("U", "USDT");
            }            
            if (text.EndsWith("USDT") && decimal.TryParse(text.Replace("USDT", ""), out var usdtPrice))
            {
                return await ValuationAction(botClient, message, usdtPrice, Currency.USDT, Currency.TRX);
            }
            if (text.EndsWith("TRX") && decimal.TryParse(text.Replace("TRX", ""), out var trxPrice))
            {
                return await ValuationAction(botClient, message, trxPrice, Currency.TRX, Currency.USDT);
            }
            return message;
        }
        static async Task<Message> ValuationAction(ITelegramBotClient botClient, Message message, decimal price, Currency fromCurrency, Currency toCurrency)
        {
            var scope = serviceScopeFactory.CreateScope();
            var provider = scope.ServiceProvider;
            var _rateRepository = provider.GetRequiredService<IBaseRepository<TokenRate>>();
            var rate = await _rateRepository.Where(x => x.Currency == Currency.USDT && x.ConvertCurrency == Currency.TRX).FirstAsync(x => x.Rate);
            var msg = $"<b>{price} {fromCurrency} = {price} {fromCurrency}</b>";
            if (fromCurrency == Currency.USDT && toCurrency == Currency.TRX)
            {
                if (price < MinUSDT)
                {
                    msg = $"仅支持大于{MinUSDT} USDT 的兑换";
                }
                else
                {
                    var toPrice = price.USDT_To_TRX(rate, FeeRate, USDTFeeRate);
                    msg = $"<b>{price} {fromCurrency} = {toPrice} {toCurrency}</b>";
                }
            }
            if (fromCurrency == Currency.TRX && toCurrency == Currency.USDT)
            {
                var toPrice = price.TRX_To_USDT(rate, FeeRate, USDTFeeRate);
                if (toPrice < MinUSDT)
                {
                    msg = $"仅支持大于{MinUSDT} USDT 的兑换";
                }
                else
                {
                    msg = $"<b>{price} {fromCurrency} = {toPrice} {toCurrency}</b>";
                }
            }
    // 创建包含三行，每行4个按钮的虚拟键盘
    var keyboard = new ReplyKeyboardMarkup(new[]
    {
        new [] // 第一行
        {
            new KeyboardButton("U兑TRX"),
            new KeyboardButton("实时汇率"),
            new KeyboardButton("查询余额"),
            new KeyboardButton("能量租赁"),
        },   
        new [] // 第二行
        {
            new KeyboardButton("外汇助手"),
            new KeyboardButton("加密货币"),
            new KeyboardButton("行情监控"),
            new KeyboardButton("地址监听"),
        },   
        new [] // 第三行
        {
            new KeyboardButton("财富密码"),
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("更多功能"),
        }
    });		
            keyboard.ResizeKeyboard = true; // 将键盘高度设置为最低
            keyboard.OneTimeKeyboard = false; // 添加这一行，确保虚拟键盘在用户与其交互后保持可见

            return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                        text: msg,
                                                        parseMode: ParseMode.Html,
                                                        replyMarkup: keyboard);
        }
        async Task InsertOrUpdateUserAsync(ITelegramBotClient botClient, Message message)
        {
            if (message.From == null) return;
            var curd = provider.GetRequiredService<IBaseRepository<Users>>();
            var from = message.From;
            var UserId = message.Chat.Id;
            Log.Information("{user}: {message}", $"{from.FirstName} {from.LastName}", message.Text);

            var user = await curd.Where(x => x.UserId == UserId).FirstAsync();
            if (user == null)
            {
                user = new Users
                {
                    UserId = UserId,
                    UserName = from.Username,
                    FirstName = from.FirstName,
                    LastName = from.LastName
                };
                await curd.InsertAsync(user);
                return;
            }
            user.UserId = UserId;
            user.UserName = from.Username;
            user.FirstName = from.FirstName;
            user.LastName = from.LastName;
            await curd.UpdateAsync(user);
        }
    }

    private static Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
    {
        Log.Information($"Unknown update type: {update.Type}");
        return Task.CompletedTask;
    }
}
