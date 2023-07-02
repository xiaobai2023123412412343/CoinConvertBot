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


namespace Telegram.CoinConvertBot.BgServices.BotHandler;

//yifanfu或@yifanfu或t.me/yifanfu为管理员ID
//yifanfubot或t.me/yifanfubot或@yifanfubot为机器人ID
//TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv为监控的收款地址
//TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv为监控的转账地址
// 将这个值替换为目标群组的ID
//const long TARGET_CHAT_ID = -894216057;//指定群聊转发用户对机器人发送的信息
// 将这个值替换为你的机器人用户名
//const string BOT_USERNAME = "yifanfubot";//机器人用户名
// 指定管理员ID
//const int ADMIN_ID = 1427768220;//指定管理员ID不转发
// 将这个值替换为目标群组的ID
//const long TARGET_CHAT_ID = -894216057;//指定群聊转发用户对机器人发送的信息
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
//decimal totalIncome = Math.Round(totalIncomeTask.Result - 18157, 2); 累计承兑-21639USDT  如果是新号可以不用减
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
//个人中心    
public static async Task HandlePersonalCenterCommandAsync(ITelegramBotClient botClient, Message message, IServiceProvider provider)
{
    try
    {
        var userId = message.From.Id;

        // 获取_bindRepository
        var _bindRepository = provider.GetRequiredService<IBaseRepository<TokenBind>>();
        // 查询是否存在一个与当前用户ID匹配的TokenBind对象
        var bindList = _bindRepository.Where(x => x.UserId == userId).ToList();
        var bind = bindList.FirstOrDefault();

        if (bind == null)
        {
            // 修改了提示消息的文本，并使用HTML模式发送消息
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id, 
                text: "您还未绑定地址，发送：<code>绑定 Txxxxxxx</code>(您的钱包地址) 即可绑定！",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
            );
        }
        else
        {
            var userAddress = bind.Address; // 获取用户的地址
            message.Text = userAddress; // 将用户的地址设置为消息的文本，以便在下面的方法中使用
            await HandleQueryCommandAsync(botClient, message);
        }
    }
    catch (Exception)
    {
        // 捕获到异常时，发送一个提示消息
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

            await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: $"用户 {name} (@{username}, ID: {id}) 已经被添加到关注者列表中。");
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
        await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: $"用户 @{username} 已经被添加到关注者列表中。");
    }
    else if (message.Text.StartsWith("存 ID："))
    {
        string idText = message.Text.Substring("存 ID：".Length).Trim();
        if (long.TryParse(idText, out long id))
        {
            var user = new User { Id = id, FollowTime = DateTime.UtcNow.AddHours(8) };
            Followers.Add(user);
            await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: $"用户 ID: {id} 已经被添加到关注者列表中。");
        }
        else
        {
            await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: $"无法解析ID: {idText}。请确保你输入的是一个有效的数字。");
        }
    }
}
//计算数字+数字货币的各地货币价值    
private static async Task HandleCryptoCurrencyMessageAsync(ITelegramBotClient botClient, Message message)
{
    var cryptoNames = new List<(string, string, string)>
    {
        ("tether", "USDT", "泰达币"),
        ("bitcoin", "比特币", "btc"),
        ("bitcoin-cash", "比特现金", "bch"),
        ("ethereum", "以太坊", "eth"),
        ("ethereum-classic", "以太经典", "etc"),
        ("binancecoin", "币安币", "bnb"),
        ("bitget-token", "币记-BGB", "bgb"),
        ("okb", "欧易-okb", "okb"),
        ("huobi-token", "火币积分-HT", "ht"),
        ("the-open-network", "电报币", "ton"),
        ("ripple", "瑞波币", "xrp"),
        ("cardano", "艾达币", "ada"),
        ("uniswap", "uni", "uni"),
        ("dogecoin", "狗狗币", "doge"),
        ("shiba-inu", "shib", "shib"),
        ("solana", "Sol", "sol"),
        ("avalanche-2", "AVAX", "avax"),
        ("litecoin", "莱特币", "ltc"),
        ("monero", "门罗币", "xmr"),
        ("chainlink", "link", "link")
    };

    var match = Regex.Match(message.Text, @"^(\d+(\.\d+)?)(btc|比特币|eth|以太坊|usdt|泰达币|币安币|bnb|bgb|币记-BGB|okb|欧易-okb|ht|火币积分-HT|瑞波币|xrp|艾达币|ada|狗狗币|doge|shib|sol|莱特币|ltc|link|电报币|ton|比特现金|bch|以太经典|etc|uni|avax|门罗币|xmr)$", RegexOptions.IgnoreCase);

    
    if (!match.Success)
    {
        return;
    }

    var amount = decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    var currencyName = match.Groups[3].Value.ToLower();

    var currencyTuple = cryptoNames.FirstOrDefault(x => x.Item2.ToLower() == currencyName.ToLower() || x.Item3.ToLower() == currencyName.ToLower());
    var currency = currencyTuple.Item1;
    if (currency == null)
    {
        return;
    }

    var (prices, changes) = await GetCryptoPricesAsync(new[] { currency });
    var cryptoPriceInUsdt = prices[0] * amount;

    var cnyPerUsdt = await GetOkxPriceAsync("usdt", "cny", "all");
    var cryptoPriceInCny = cryptoPriceInUsdt * cnyPerUsdt;
    var cryptoToCnyRate = cryptoPriceInUsdt * cnyPerUsdt;

    var rates = await GetCurrencyRatesAsync();
var responseText = $"<b>{amount} 枚 {currencyTuple.Item2}</b> 的价值是：\n\n<code>{cryptoToCnyRate:N2} 人民币 (CNY)</code>\n—————————————————\n";
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

var inlineKeyboardButton1 = new InlineKeyboardButton($"完整的 {currencyTuple.Item2} 价值表")
{
    CallbackData = $"full_rates,{cryptoPriceInCny},{amount},{currencyTuple.Item2},{cryptoPriceInCny}"
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

    // 尝试向用户发送一条消息，告知他们查询失败
    try
    {
        await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                             text: "查询失败，请稍后再试！",
                                             parseMode: ParseMode.Html);
    }
    catch (Exception sendEx)
    {
        // 如果向用户发送消息也失败，那么记录这个异常，但不再尝试发送消息
        Log.Error($"向用户发送失败消息也失败了: {sendEx.Message}");
    }

    return;
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
private static Dictionary<long, string> userTronAddresses = new Dictionary<long, string>();
// 存储用户ID和定时器的字典
private static Dictionary<long, Timer> userTimers = new Dictionary<long, Timer>();    
private static void StartMonitoring(ITelegramBotClient botClient, long userId, string tronAddress)
{
    // 创建一个定时器来定期检查地址的TRX余额
    Timer timer = null;
    timer = new Timer(async _ =>
    {
        var balance = await GetTronBalanceAsync(tronAddress);
        var roundedBalance = Math.Round(balance, 2); // 四舍五入到小数点后两位
        // 计算可供转账次数，这是新添加的代码
        var transferTimes = Math.Floor(balance / (decimal)13.3959);  // 计算转账次数为用户余额除以13.3959 
        if (balance < 100)
        {
            try
            {
                await botClient.SendTextMessageAsync(
                    chatId: userId,
                    text: $"<b>温馨提示：</b>\n您绑定的地址：<code>{tronAddress}</code>\n\n⚠️ TRX余额只剩：{roundedBalance}，剩余可供转账：{transferTimes}次 ⚠️\n为了不影响您的转账，建议您立即向本机器人兑换TRX！",
                    parseMode: ParseMode.Html
                );
            }
            catch (ApiRequestException ex)
            {
                if (ex.Message == "Forbidden: bot was blocked by the user" || ex.Message.Contains("user is deactivated"))
                {
                    // 用户阻止了机器人，或者用户注销了机器人，取消定时器任务
                    timer.Dispose();
                    // 从字典中移除该用户的定时器和地址
                    userTimers.Remove(userId);
                    userTronAddresses.Remove(userId);
                    return;
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
                // 从字典中移除该用户的定时器和地址
                userTimers.Remove(userId);
                userTronAddresses.Remove(userId);
                return;
            }
            finally
            {
                // 如果下发提醒失败或查询失败，过10秒重新启动
                timer.Change(TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1));
            }
            // 余额不足，停止480分钟 8小时
            timer.Change(TimeSpan.FromMinutes(480), TimeSpan.FromMinutes(480));
        }
        else
        {
            // 余额充足，每分钟检查一次
            timer.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }
    }, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

    // 将定时器和用户ID存储起来
    userTimers[userId] = timer;
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

                // 将地址和用户ID存储起来
                userTronAddresses[userId] = tronAddress;

                // 创建一个定时器来定期检查地址的TRX余额
                StartMonitoring(botClient, userId, tronAddress);
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
                throw new Exception("无法获取波场地址的TRX余额。");
            }
        }
        catch (Exception ex)
        {
            // 记录异常信息，但不中断程序运行
            Console.WriteLine($"Error getting Tron balance: {ex.Message}");
            return 0;
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

private static async Task HandleBlacklistAndWhitelistCommands(ITelegramBotClient botClient, Message message)
{
    try
    {
        // 检查 message 和 message.Text 是否为 null
        if (message == null || message.Text == null)
        {
            return;
        }

        // 检查消息是否来自指定的管理员
        if (message.From.Id != 1427768220)//管理员
        {
            return;
        }

        // 检查消息是否包含拉黑或拉白命令
        var commandParts = message.Text.Split(' ');
        if (commandParts.Length != 2)
        {
            return;
        }

        var command = commandParts[0];
        if (!long.TryParse(commandParts[1], out long userId))
        {
            return;
        }

        switch (command)
        {
            case "拉黑":
                blacklistedUserIds.Add(userId);
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"用户 {userId} 已被拉黑。"
                );
                break;
            case "拉白":
                blacklistedUserIds.Remove(userId);
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"用户 {userId} 已被拉白。"
                );
                break;
        }
    }
    catch (Exception ex)
    {
        // 在这里处理异常，例如记录错误日志或发送错误消息
        Console.WriteLine($"处理拉黑和拉白命令时发生错误: {ex.Message}");
    }
}
//监控信息变更提醒    
private static Dictionary<long, Timer> _timers = new Dictionary<long, Timer>();
private static Dictionary<long, int> _errorCounts = new Dictionary<long, int>();    
public static async void StartMonitoring(ITelegramBotClient botClient, long chatId)
{
    int retryCount = 0; // 添加一个重试计数器

    while (retryCount < 3) // 如果重试次数小于3次，继续尝试
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
                    await Task.Delay(TimeSpan.FromSeconds(1));
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
            var timer = new Timer(async _ => await CheckUserChangesAsync(botClient, chatId), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
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

            // 如果存在定时器，停止并移除
            if (_timers.ContainsKey(chatId))
            {
                _timers[chatId].Dispose(); // 停止现有的定时器
                _timers.Remove(chatId); // 从字典中移除
            }

            retryCount++; // 增加重试计数器

            if (retryCount < 3) // 如果重试次数小于3次，等待5秒后再次尝试
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
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
                await Task.Delay(TimeSpan.FromSeconds(1));
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
        if (ex.ErrorCode == 400 && ex.Message == "Bad Request: group chat was upgraded to a supergroup chat")
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
        if (ex.ErrorCode == 400 && ex.Message == "Bad Request: chat not found")
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

        // 如果错误次数达到3次，取消任务并发送通知
        if (_errorCounts[chatId] >= 3)
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
                await Task.Delay(TimeSpan.FromSeconds(1));                
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

        // 如果错误次数达到3次，取消任务并发送通知
        if (_errorCounts[chatId] >= 3)
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

        if (message.Chat.Type == ChatType.Private)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"您的用户ID是：<code>{userId}</code>",
                parseMode: ParseMode.Html
            );
        }
        else if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
        {
            var replyToMessageId = message.MessageId;

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"您的用户ID是：<code>{userId}</code>\n当前群聊ID是：<code>{chatId}</code>",
                parseMode: ParseMode.Html,
                replyToMessageId: replyToMessageId
            );
        }
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
    var followersPerPage = Followers.Skip(page * 15).Take(15);
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
            InlineKeyboardButton.WithCallbackData("兑换记录", "show_transaction_recordds")
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
            return "API接口维护中，请稍后重试！";
        }
    }

public async static Task<string> GetTransferBalancesAsync(List<TransferRecord> transfers)
{
    string apiUrlTemplate = "https://apilist.tronscan.org/api/account?address={0}";
    string resultText = $"<b> 承兑地址：</b><code>TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv</code>\n\n";

    try
    {
        // 创建一个任务列表来存储所有的查询任务
        List<Task<(int index, AccountInfo accountInfo)>> tasks = new List<Task<(int index, AccountInfo accountInfo)>>();

        // 为每个转账记录创建一个查询任务并添加到任务列表中
        for (int i = 0; i < transfers.Count; i++)
        {
            string apiUrl = string.Format(apiUrlTemplate, transfers[i].TransferToAddress);
            tasks.Add(GetAccountInfoAsync(httpClient, apiUrl, i));
        }

        // 等待所有任务完成
        var results = await Task.WhenAll(tasks);

        // 将查询结果按索引排序
        var accountInfos = results.OrderBy(r => r.index).Select(r => r.accountInfo).ToList();

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
        resultText = "API接口维护中，请稍后重试！";
    }

    return resultText;
}

private static async Task<(int index, AccountInfo accountInfo)> GetAccountInfoAsync(HttpClient httpClient, string apiUrl, int index)
{
    await semaphore.WaitAsync(); // 限制并发数
    try
    {
        var response = await httpClient.GetAsync(apiUrl);
        if (response.IsSuccessStatusCode)
        {
            string jsonResult = await response.Content.ReadAsStringAsync();
            var accountInfo = JsonSerializer.Deserialize<AccountInfo>(jsonResult);
            return (index, accountInfo);
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
    if (message.Text.Contains("帮助"))
    {
        string adminLink = "https://t.me/yifanfu";
        string adminLinkText = $"<a href=\"{adminLink}\">管理员！</a>";

        string helpText = "更改电报语言：在机器人对话框直接发送：<code>中文</code> 自动返回包括原zh_cn等众多简体中文语言包，点击任意链接即可更改界面语言！\n\n" +
                          "关于兑换：<code>点击U兑TRX，给收款地址转10u以上的任意金额，机器人自动返回TRX到原付款地址，过程全自动，无人工干预！(点击机器人任意菜单只要有反应即可正常兑换，无需联系管理二次确认)</code>\n\n" +
                          "实时汇率：<code>TRX能量跟包括比特币在内的所有数字货币一样，价格起起落落有涨有跌，受市场行情影响，机器人的兑换汇率自动跟随市场行情进行波动！</code>\n\n" +
                          "自动提醒：<code>使用前推荐发送：绑定 Txxxxxxx(您的钱包地址，中间有空格)绑定钱包地址，当TRX余额不足时机器人会自动下发提醒！</code>\n\n" +
                          "防骗助手：<code>把机器人拉进群聊并设置为管理员，发送指令：</code><code>/jiankong</code> <code>当群内成员更改名字或用户名后，机器人会发送资料变更提醒，以防被骗！</code>\n\n" +
                          //"谷歌搜索：<code>发送：谷歌+空格+搜索词自动启动谷歌搜索并返回，例如发送：</code><code>谷歌 上海天气</code>\n\n" +
                          "汇率计算：<code>发送数字+币种(支持货币代码及数字货币)自动计算并返回对应的人民币价值，例如发送1000美元或1000usd 自动按实时汇率计算并返回1000美元 ≈ ****元人民币</code>\n\n" +
                          "查询地址：<code>发送任意TRC20波场地址自动查询地址详情并返回近期USDT交易记录！</code>\n\n" +
                          "关于翻译：<code>发送任意外文自动翻译成简体中文并返回(本功能调用谷歌翻译) 群里不想使用可以发送：关闭翻译 或：开启翻译 </code>\n\n" +
                          "中文转外文：<code>发送例如：\"转英语 你好\" 自动将你好翻译成英语：hello （附带的文件为mp3格式的外语发音）</code>\n\n" +
                          "实时查看：<code>如果想自动获取TRX-比特币-美元-USDT等在内的所有汇率，把机器人拉到群里即可，24小时自动推送！（注：如果发现推送停止，把机器人移出群重新拉群即可恢复推送！）</code>\n\n" +
                          "关于ID：<code>直接发送id自动返回用户ID，群内发送会返回用户ID以及本群群ID！如需查其它用户直接发送用户名即可返回该用户的ID！ </code>\n\n" +
                          "群里使用：<code>所有功能都可在机器人私聊使用，如果在群里，需要设置机器人为管理或者回复机器人消息才可使用！</code>\n\n" +
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
    // 回复用户正在统计
    var responseMessage = await botClient.SendTextMessageAsync(message.Chat.Id, "正在统计，请稍后...");
    var responseMessageId = responseMessage.MessageId;
    
    try
    {
        string outcomeAddress = "TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv";
        string outcomeUrl = $"https://apilist.tronscan.org/api/transaction?address={outcomeAddress}&token=TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t&limit=50&page=1";

        string usdtUrl = $"https://api.trongrid.io/v1/accounts/TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv/transactions/trc20?only_confirmed=true&limit=20&token_id=TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t&min_timestamp=0&max_timestamp={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        using (var httpClient = new HttpClient())
        {
            var outcomeResponseTask = httpClient.GetStringAsync(outcomeUrl);
            var usdtResponseTask = httpClient.GetStringAsync(usdtUrl);

            await Task.WhenAll(outcomeResponseTask, usdtResponseTask);

            var outcomeTransactions = ParseTransactions(outcomeResponseTask.Result, "TRX");
            var usdtTransactions = ParseTransactions(usdtResponseTask.Result, "USDT");

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
                await botClient.EditMessageTextAsync(message.Chat.Id, responseMessageId, transactionRecords, replyMarkup: inlineKeyboard);
            }
            catch (Exception ex)
            {
                // 如果替换消息失败，这里可以处理这个异常
                // 例如，你可以记录这个错误，或者忽略它
                Console.WriteLine($"替换消息失败：{ex.Message}");
            }            

            return transactionRecords;
        }
    }
    catch (Exception ex)
    {
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
                if (data["ownerAddress"] != null && data["ownerAddress"].ToString() == "TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv" &&
                    data["timestamp"] != null && data["contractData"] != null && data["contractData"]["amount"] != null)
                {
                    var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)data["timestamp"]).LocalDateTime;
                    var amount = decimal.Parse(data["contractData"]["amount"].ToString()) / 1000000;

                    if (amount > 1) // 添加条件，只添加金额大于1的交易记录
                    {
                        transactions.Add((timestamp, token, amount));
                    }
                }
            }
            else if (token == "USDT")
            {
                // 只统计 type 为 "Transfer" 的交易
                if (data["type"] == null || data["type"].ToString() != "Transfer")
                {
                    continue;
                }

                // 添加条件，只统计名字为 "Tether USD" 的交易且合约地址为 "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t"
                bool isTetherUSD = false;
                if (data["token_info"] != null && data["token_info"]["name"] != null && data["token_info"]["address"] != null)
                {
                    isTetherUSD = data["token_info"]["name"].ToString() == "Tether USD" && data["token_info"]["address"].ToString() == "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t";
                }

                if (!isTetherUSD)
                {
                    continue;
                }

                if (data["to"] != null && data["to"].ToString() == "TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv" &&
                    data["block_timestamp"] != null && data["value"] != null)
                {
                    var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)data["block_timestamp"]).LocalDateTime;
                    var amount = decimal.Parse(data["value"].ToString()) / 1000000;

                    transactions.Add((timestamp, token, amount));
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

    int numOfIncomeRecords = 0;
    int numOfOutcomeRecords = 0;
    int totalPairs = 0;

    while (numOfIncomeRecords < incomeTransactions.Count || numOfOutcomeRecords < outcomeTransactions.Count)
    {
        if (numOfIncomeRecords < incomeTransactions.Count)
        {
            sb.AppendLine($"收入：{incomeTransactions[numOfIncomeRecords].timestamp:yyyy-MM-dd HH:mm:ss} 收入{incomeTransactions[numOfIncomeRecords].token} {incomeTransactions[numOfIncomeRecords].amount}");
            numOfIncomeRecords++;
        }

        if (numOfOutcomeRecords < outcomeTransactions.Count)
        {
            sb.AppendLine($"支出：{outcomeTransactions[numOfOutcomeRecords].timestamp:yyyy-MM-dd HH:mm:ss} 支出{outcomeTransactions[numOfOutcomeRecords].token} {outcomeTransactions[numOfOutcomeRecords].amount}");
            numOfOutcomeRecords++;
        }

        if (numOfIncomeRecords < incomeTransactions.Count || numOfOutcomeRecords < outcomeTransactions.Count)
        {
            sb.AppendLine("—————————————————————");
            totalPairs++;
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
public static async Task<(decimal TotalIncome, decimal MonthlyIncome, decimal DailyIncome, bool IsError)> GetTotalIncomeAsync(string address, bool isTrx)
{
    try
    {
        var apiUrl = $"https://api.trongrid.io/v1/accounts/{address}/transactions/trc20?only_confirmed=true&only_to=true&limit=200&contract_address=TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t";
        using var httpClient = new HttpClient();

        decimal totalIncome = 0m;
        decimal monthlyIncome = 0m;
        decimal dailyIncome = 0m;
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

                if (!transactionElement.TryGetProperty("to", out var toAddressElement))
                {
                    continue;
                }
                var toAddress = toAddressElement.GetString();

                if (toAddress != address)
                {
                    continue;
                }

                if (!transactionElement.TryGetProperty("value", out var valueElement))
                {
                    continue;
                }
                var value = valueElement.GetString();

                decimal income = decimal.Parse(value) / 1_000_000; // 假设USDT有6位小数
                totalIncome += income;

                // 获取交易时间
                if (transactionElement.TryGetProperty("block_timestamp", out var timestampElement))
                {
                    var timestamp = timestampElement.GetInt64();
                    DateTime transactionTimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
                    DateTime transactionTimeInBeijing = transactionTimeUtc.AddHours(8);

                    // 判断是否属于当月和今天的收入
                    if (transactionTimeInBeijing >= firstDayOfMonth)
                    {
                        monthlyIncome += income;

                        if (transactionTimeInBeijing >= today)
                        {
                            dailyIncome += income;
                        }
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
        return (totalIncome, monthlyIncome, dailyIncome, false);
    }
    catch (Exception ex)
    {
        // 发生错误时，返回默认值和IsError=true
        Console.WriteLine($"Error in method {nameof(GetTotalIncomeAsync)}: {ex.Message}");
        return (0m, 0m, 0m, true);
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
                    type = "支出 ";
                }
                else
                {
                    type = "收入 ";
                }

                // 获取交易金额，并转换为USDT
                string value = (string)transaction["value"];
                decimal usdtAmount = decimal.Parse(value) / 1_000_000;
                // 输出API返回的数据，在解析JSON响应之后添加
                //Console.WriteLine(jsonString);

                // 构建交易文本并添加链接
                transactionTextBuilder.AppendLine($"<a href=\"https://tronscan.org/#/transaction/{txHash}\">{transactionTimeBeijing:yyyy-MM-dd HH:mm:ss}   {type}{usdtAmount:N2} USDT</a>");
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

// 等待所有任务完成
await Task.WhenAll(getUsdtTransferTotalTask, getBalancesTask, getAccountCreationTimeTask, getLastTransactionTimeTask, getTotalIncomeTask, getBandwidthTask, getLastFiveTransactionsTask, getOwnerPermissionTask);

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
var (usdtTotalIncome, monthlyIncome, dailyIncome, isErrorGetTotalIncome) = getTotalIncomeResult;

var getOwnerPermissionResult = getOwnerPermissionTask.Result;
var (ownerPermissionAddress, isErrorGetOwnerPermission) = getOwnerPermissionResult;
    
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
// 判断 TRX 余额是否小于100
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
    $"———————<b>最近交易</b>———————\n" +
    $"{lastFiveTransactions}\n"+
    $"USDT总收：<b>{usdtTotalIncome.ToString("N2")}</b> | 本月：<b>{monthlyIncome.ToString("N2")}</b> | 今日：<b>{dailyIncome.ToString("N2")}</b>\n" ;
    //$"USDT今日收入：<b>{dailyIncome.ToString("N2")}</b>\n" ;    
}
else
{
    resultText =  $"查询地址：<code>{tronAddress}</code>\n" +
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
    $"———————<b>最近交易</b>———————\n" +
    $"{lastFiveTransactions}\n"+
    $"USDT总收：<b>{usdtTotalIncome.ToString("N2")}</b> | 本月：<b>{monthlyIncome.ToString("N2")}</b> | 今日：<b>{dailyIncome.ToString("N2")}</b>\n" ;
    //$"USDT今日收入：<b>{dailyIncome.ToString("N2")}</b>\n" ;    
}


        // 创建内联键盘
    string botUsername = "yifanfubot"; // 替换为你的机器人的用户名
    string startParameter = ""; // 如果你希望机器人在被添加到群组时收到一个特定的消息，可以设置这个参数
    string shareLink = $"https://t.me/{botUsername}?startgroup={startParameter}";

    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new [] // 第一行按钮
        {
            InlineKeyboardButton.WithUrl("详细信息", $"https://tronscan.org/#/address/{tronAddress}"), // 链接到Tron地址的详细信息
            InlineKeyboardButton.WithUrl("链上天眼", $"https://www.oklink.com/cn/trx/address/{tronAddress}"), // 链接到欧意地址的详细信息
            InlineKeyboardButton.WithUrl("进群使用", shareLink) // 添加机器人到群组的链接
        }
    });

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
            //Console.WriteLine($"Raw Data: {rawData}");//打印api

            var csvDataStartIndex = rawData.IndexOf("fng_value");
            var csvData = rawData.Substring(csvDataStartIndex);
            //Console.WriteLine($"CSV Data: {csvData}");//打印api

            var rows = csvData.Split('\n');
            var dataList = new List<FngData>();

            for (int i = 1; i < rows.Length; i++)
            {
                var columns = rows[i].Split(',');

                if (columns.Length >= 3)
                {
                    dataList.Add(new FngData
                    {
                        Date = DateTime.Parse(columns[0]),
                        FngValue = int.Parse(columns[1]),
                        FngClassification = columns[2]
                    });
                }
            }

            var today = dataList[0].FngValue;
            var yesterday = dataList[1].FngValue;

            // 计算上周和上月的日期范围
            var endOfWeek = dataList[0].Date.AddDays(-((int)dataList[0].Date.DayOfWeek + 6) % 7);
            var startOfWeek = endOfWeek.AddDays(-6);
            var startOfMonth = new DateTime(dataList[0].Date.Year, dataList[0].Date.Month - 1, 1);
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

        // 创建包含两行，每行两个按钮的虚拟键盘
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new [] // 第一行
            {
                new KeyboardButton("\U0001F4B0U兑TRX"),
                new KeyboardButton("\U0001F570实时汇率"),
                new KeyboardButton("\U0001F4B9汇率换算"),
            },   
            new [] // 第二行
            {
                new KeyboardButton("\U0001F4B8币圈行情"),
                new KeyboardButton("\U0001F310外汇助手"),
                new KeyboardButton("\u2B50会员代开"),
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
    new [] // 第二行按钮
    {
        InlineKeyboardButton.WithUrl("\U0001F4B9 一起穿越币圈牛熊 \U0001F4B9", "https://t.me/+b4NunT6Vwf0wZWI1"),
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
    var rateTask = rateRepository.Where(x => x.Currency == Currency.USDT && x.ConvertCurrency == Currency.TRX).FirstAsync(x => x.Rate);
    var fearAndGreedIndexTask = GetFearAndGreedIndexAsync();
    var cryptoPricesTask = GetCryptoPricesAsync(new[] { "bitcoin", "ethereum" });
    var currencyRatesTask = GetCurrencyRatesAsync();
    var okxPriceTask = GetOkxPriceAsync("USDT", "CNY", "all");
    var h24TotalVolUsdTask = GetH24TotalVolUsdAsync("https://open-api.coinglass.com/public/v2/liquidation_info?time_type=h24&symbol=all", "9e8ff0ca25f14355a015972f21f162de");
    var btcLongShortTask = GetH24LongShortAsync("https://open-api.coinglass.com/public/v2/long_short?time_type=h24&symbol=BTC", "9e8ff0ca25f14355a015972f21f162de");
    var ethLongShortTask = GetH1EthLongShortAsync("https://open-api.coinglass.com/public/v2/long_short?time_type=h1&symbol=ETH", "9e8ff0ca25f14355a015972f21f162de");

    await Task.WhenAll(rateTask, fearAndGreedIndexTask, cryptoPricesTask, currencyRatesTask, okxPriceTask, h24TotalVolUsdTask, btcLongShortTask, ethLongShortTask);

    var rate = await rateTask;
    var (today, yesterday, weekly, monthly) = await fearAndGreedIndexTask;
    var (prices, changes) = await cryptoPricesTask;
    var currencyRates = await currencyRatesTask;
    var okxPrice = await okxPriceTask;
    var h24TotalVolUsd = await h24TotalVolUsdTask;
    var (btcLongRate, btcShortRate) = await btcLongShortTask;
    var (ethLongRate, ethShortRate) = await ethLongShortTask;

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
    $"<b>\U0001F4B0 兑换汇率：100 USDT = {usdtToTrx:#.####} TRX</b>\n\n" +
    $"<code>\U0001F4B8 币圈今日恐惧与贪婪指数：{today} {fearGreedDescription}</code>\n" +                 
    $"<code>\U0001F4B8 比特币价格 ≈ {bitcoinPrice} USDT    {(bitcoinChange >= 0 ? "+" : "")}{bitcoinChange:0.##}% </code>\n" +
    $"<code>\U0001F4B8 以太坊价格 ≈ {ethereumPrice} USDT  {(ethereumChange >= 0 ? "+" : "")}{ethereumChange:0.##}% </code>\n" +
    $"<code>\U0001F4B8 全网24小时合约爆仓 ≈ {h24TotalVolUsd:#,0} USDT</code>\n" +     
    $"<code>\U0001F4B8 以太坊1小时合约： {ethLongRate:#.##}% 做多  {ethShortRate:#.##}% 做空</code>\n" +
    $"<code>\U0001F4B8 比特币24小时合约：{btcLongRate:#.##}% 做多  {btcShortRate:#.##}% 做空</code>\n" ;
            
            
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
            "\U0000267B进U即兑,全自动返TRX,10U起兑!\n" +
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
                if (message.Text.StartsWith("/gzz"))
                {
                    await HandleGetFollowersCommandAsync(botClient, message);
                }    
    
    // 检查输入文本是否为 Tron 地址
    var isTronAddress = Regex.IsMatch(message.Text, @"^(T[A-Za-z0-9]{33})$");

    if (isTronAddress)
    {
        await HandleQueryCommandAsync(botClient, message); // 当满足条件时，调用查询方法
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
        var isBindOrUnbindCommand = Regex.IsMatch(inputText, @"^(绑定|解绑)");

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
        
        // 添加新正则表达式以检查输入文本是否仅为 'id' 或 'ID'
        var isIdOrID = Regex.IsMatch(inputText, @"^\b(id|ID)\b$", RegexOptions.IgnoreCase);
        // 添加新正则表达式以检查输入文本是否包含 "查id"、"查ID" 或 "t.me/"
        var containsIdOrTme = Regex.IsMatch(inputText, @"查id|查ID|t\.me/", RegexOptions.IgnoreCase);

        // 如果输入文本包含 "查id"、"查ID" 或 "t.me/"，则不执行翻译
        if (containsIdOrTme)
        {
            return;
        }
        
        if (!string.IsNullOrWhiteSpace(inputText))
        {
            // 修改正则表达式以匹配带小数点的数字计算
            var containsKeywordsOrCommandsOrNumbersOrAtSign = Regex.IsMatch(inputText, @"^\/(start|yi|fan|qdgg|yccl|fu|btc|usd|vip|cny|jiankong)|会员代开|汇率换算|实时汇率|U兑TRX|币圈行情|外汇助手|^[\d\+\-\*/\.\s]+$|^@");

            // 检查输入文本是否为数字+货币的组合
            var isNumberCurrency = Regex.IsMatch(inputText, @"(^\d+\s*[A-Za-z\u4e00-\u9fa5]+$)|(^\d+(\.\d+)?(btc|比特币|eth|以太坊|usdt|泰达币|币安币|bnb|bgb|币记-BGB|okb|欧易-okb|ht|火币积分-HT|瑞波币|xrp|艾达币|ada|狗狗币|doge|shib|sol|莱特币|ltc|link|电报币|ton|比特现金|bch|以太经典|etc|uni|avax|门罗币|xmr)$)", RegexOptions.IgnoreCase);

            // 检查输入文本是否为纯中文文本带空格
            var isChineseTextWithSpaces = Regex.IsMatch(inputText, @"^[\u4e00-\u9fa5\s]+$");

            // 检查输入文本是否为 Tron 地址
            var isTronAddress = Regex.IsMatch(inputText, @"^(T[A-Za-z0-9]{33})$");

            // 检查输入文本是否仅包含表情符号
            var isOnlyEmoji = EmojiHelper.IsOnlyEmoji(inputText);
            
            // 如果输入文本仅为 'id' 或 'ID'，则不执行翻译
            if (isIdOrID)
            {
                return;
            }

            if (!containsKeywordsOrCommandsOrNumbersOrAtSign && !isTronAddress && !isOnlyEmoji && !isNumberCurrency && !isChineseTextWithSpaces)
            {
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

            await botClient.EditMessageTextAsync(
                chatId: update.CallbackQuery.Message.Chat.Id,
                messageId: update.CallbackQuery.Message.MessageId,
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
                text: "<b>收款地址</b>：<code>TJ4c6esQYEM7jn5s8DD5zk2DBYJTLHnFR3</code>",
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
            // 返回上一级菜单
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new [] // 第一行按钮
                {
                    //InlineKeyboardButton.WithUrl("管理员", "https://t.me/Yifanfu"),
                    InlineKeyboardButton.WithCallbackData("\u2B50 会员代开", "membershipOptions"),
                    InlineKeyboardButton.WithUrl("\U0001F449 进群交流", "https://t.me/+b4NunT6Vwf0wZWI1")
                }
            });

            await botClient.EditMessageTextAsync(
                chatId: update.CallbackQuery.Message.Chat.Id,
                messageId: update.CallbackQuery.Message.MessageId,
                text: "欢迎使用本机器人,请选择下方按钮操作：",
                replyMarkup: inlineKeyboard
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
    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: "受限用户！"
    );
    return;
}        
// 将这个值替换为目标群组的ID
const long TARGET_CHAT_ID = -894216057;//指定群聊转发用户对机器人发送的信息
// 将这个值替换为你的机器人用户名
const string BOT_USERNAME = "yifanfubot";//机器人用户名
// 指定管理员ID
const int ADMIN_ID = 1427768220;//指定管理员ID不转发

// 存储机器人的所有命令
string[] botCommands = { "/start", "/yi","/fan","/fu","/btc","/usd","/boss","中文","帮助","兑换记录","\U0001F4B0U兑TRX","\U0001F570实时汇率","\U0001F4B9汇率换算","\U0001F4B8币圈行情","\U0001F310外汇助手","\u2B50会员代开", "/cny" };       

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
Telegram 官方只开放了语言包翻译接口, 官方没有提供中文语言包
目前所有的中文语言包都是非官方人员翻译的, 都是用户翻译的
觉得好用可以推荐朋友使用~~~";

    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: languagePackMessage
    );
}    
    var timestamp = message.Date.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
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
            await botClient.SendTextMessageAsync(
                chatId: TARGET_CHAT_ID,
                text: forwardedMessage,
                parseMode: ParseMode.Html
            );
        }
    }
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
if (messageText.Equals("个人中心", StringComparison.OrdinalIgnoreCase))
{
    await HandlePersonalCenterCommandAsync(botClient, message, provider);
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
// 检查是否是"查询余额"命令
if (message.Type == MessageType.Text && message.Text.Equals("查询余额", StringComparison.OrdinalIgnoreCase))
{
    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id, 
        text: "请发送您要查询的<b>TRC-20(波场)地址：</b> ", 
        parseMode: ParseMode.Html
    );
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

        if (command == "关闭广告")
        {
            GroupManager.ToggleAdvertisement(groupId, false);
            await botClient.SendTextMessageAsync(groupId, "已关闭广告功能。");
        }
        else if (command == "开启广告")
        {
            GroupManager.ToggleAdvertisement(groupId, true);
            await botClient.SendTextMessageAsync(groupId, "已开启广告功能。");
        }
    }
if (message.Text.StartsWith("@") || 
    message.Text.StartsWith("https://t.me/") || 
    message.Text.StartsWith("http://t.me/") || 
    message.Text.StartsWith("t.me/") ||
    message.Text.Trim().ToLower() == "查id" || 
    message.Text.Trim().ToLower() == "查id")
{
    await HandleUsernameOrUrlMessageAsync(botClient, message);
}
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
if (Regex.IsMatch(message.Text, @"用户名：|ID："))
{
    await HandleStoreCommandAsync(botClient, message);
}       
if (Regex.IsMatch(message.Text, @"^\d+(\.\d+)?(btc|比特币|eth|以太坊|usdt|泰达币|币安币|bnb|bgb|币记-BGB|okb|欧易-okb|ht|火币积分-HT|瑞波币|xrp|艾达币|ada|狗狗币|doge|shib|sol|莱特币|ltc|link|电报币|ton|比特现金|bch|以太经典|etc|uni|avax|门罗币|xmr)$", RegexOptions.IgnoreCase))
{
    await HandleCryptoCurrencyMessageAsync(botClient, message);
}  
// 检查是否是管理员发送的 "群发" 消息
if (message.From.Id == 1427768220 && message.Text.StartsWith("群发 "))
{
    var messageToSend = message.Text.Substring(3); // 去掉 "群发 " 前缀

    // 向所有关注者发送消息
    foreach (var follower in Followers)
    {
        try
        {
            await botClient.SendTextMessageAsync(chatId: follower.Id, text: messageToSend);
        }
        catch (ApiRequestException e)
        {
            // 用户不存在或已经屏蔽了机器人
            // 在这里记录异常，然后继续向下一个用户发送消息
            Log.Error($"Failed to send message to {follower.Id}: {e.Message}");
        }
    }
}  
if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
{
    var groupId = message.Chat.Id;
    var command = messageText.ToLower();

    if (command == "关闭翻译")
    {
        groupTranslationSettings[groupId] = false;
        await botClient.SendTextMessageAsync(groupId, "已关闭翻译功能。");
    }
    else if (command == "开启翻译")
    {
        groupTranslationSettings[groupId] = true;
        await botClient.SendTextMessageAsync(groupId, "已开启翻译功能。");
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

如果需要开通多签功能，可联系管理员协助开通！";
    await botClient.SendTextMessageAsync(message.Chat.Id, multisigText);
}        
    // 检查是否接收到了 /cny 消息，收到就在当前聊天中发送广告
    else if (messageText.StartsWith("/cny"))
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var rateRepository = provider.GetRequiredService<IBaseRepository<TokenRate>>();
        _ = SendAdvertisementOnce(botClient, cancellationTokenSource.Token, rateRepository, FeeRate, message.Chat.Id);
    }        
// 添加这部分代码以处理 /crypto 和 /btc 指令
if (messageText.StartsWith("\U0001F4B8币圈行情", StringComparison.OrdinalIgnoreCase) || messageText.StartsWith("/btc", StringComparison.OrdinalIgnoreCase))
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
if (message.Text == "\U0001F310外汇助手" || message.Text == "/usd") // 添加 /usd 条件
{
    await HandleCurrencyRatesCommandAsync(botClient, message, 1);
}

else
{
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
}
        messageText = messageText.Replace($"@{BotUserName}", "");
        var action = messageText.Split(' ')[0] switch
        {
            "/start" => Start(botClient, message),
            "/fu" => Valuation(botClient, message),
            "\U0001F4B0U兑TRX" => ConvertCoinTRX(botClient, message), // 添加这一行
            "\U0001F570实时汇率" => PriceTRX(botClient, message), // 添加这一行
            "\U0001F4B9汇率换算" => Valuation(botClient, message), // 添加这一行
            "/yi" => ConvertCoinTRX(botClient, message),
            "/fan" => PriceTRX(botClient, message),
            "绑定" => BindAddress(botClient, message),
            "解绑" => UnBindAddress(botClient, message),
            "\u2B50会员代开" => QueryAccount(botClient, message),
            "/vip" => QueryAccount(botClient, message), // 添加这一行
            "关闭键盘" => guanbi(botClient, message),
            _ => Usage(botClient, message)
        };
async Task<decimal> GetTotalUSDTIncomeAsync(string ReciveAddress, string contractAddress)
{
    const int PageSize = 200; // 每页查询的交易记录数量，最大值为 200
    int currentPage = 0;

    decimal usdtIncome = 0;
    bool hasMoreData = true;

    while (hasMoreData)
    {
        // 调用TronGrid API以获取交易记录
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        string apiEndpoint = $"https://api.trongrid.io/v1/accounts/{ReciveAddress}/transactions/trc20?only_confirmed=true&only_to=true&contract_address={contractAddress}&limit={PageSize}&start={(currentPage * PageSize) + 1}";
        var response = await httpClient.GetAsync(apiEndpoint);

        if (!response.IsSuccessStatusCode)
        {
            // 请求失败，返回0
            return 0;
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        JObject transactions = JObject.Parse(jsonResponse);

        // 将以下一行代码注释掉，以禁用 API 响应日志输出到控制台
        // Console.WriteLine($"API Response: {jsonResponse}");

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
        Message sentMessage = await action;
        async Task<Message> QueryAccount(ITelegramBotClient botClient, Message message)
        {
            if (message.From == null) return message;
            var from = message.From;
            var UserId = message.Chat.Id;

if (UserId != AdminUserId)
{
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new [] // 第一行按钮
        {
            //InlineKeyboardButton.WithUrl("管理员", "https://t.me/Yifanfu"),
            InlineKeyboardButton.WithCallbackData("\u2B50 会员代开", "membershipOptions"),
            InlineKeyboardButton.WithUrl("\U0001F449 进群交流", "https://t.me/+b4NunT6Vwf0wZWI1")
        }
    });

    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: "欢迎使用本机器人,请选择下方按钮操作：",
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
var contractAddress = _myTronConfig.Value.USDTContractAddress;
var contractClient = _contractClientFactory.CreateClient(ContractProtocol.TRC20);
Task<decimal> USDTTask = contractClient.BalanceOfAsync(contractAddress, _wallet.GetAccount(_myTronConfig.Value.PrivateKey));

// 同时运行获取今日、本月和总收入的任务
string targetReciveAddress = "TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv";
Task<decimal> todayIncomeTask = GetTodayUSDTIncomeAsync(targetReciveAddress, contractAddress);
Task<decimal> monthlyIncomeTask = GetMonthlyUSDTIncomeAsync(targetReciveAddress, contractAddress);
Task<decimal> totalIncomeTask = GetTotalUSDTIncomeAsync(targetReciveAddress, contractAddress);

// 等待所有任务完成
await Task.WhenAll(resourceTask, accountTask, bandwidthTask, USDTTask, todayIncomeTask, monthlyIncomeTask, totalIncomeTask);

// 获取任务的结果
var resource = resourceTask.Result;
var account = accountTask.Result;
var (freeNetRemaining, freeNetLimit, netRemaining, netLimit, energyRemaining, energyLimit, transactions, transactionsIn, transactionsOut, isError) = bandwidthTask.Result;
var TRX = Convert.ToDecimal(account.Balance) / 1_000_000L;
var USDT = USDTTask.Result;
decimal todayIncome = Math.Round(todayIncomeTask.Result, 2);
decimal monthlyIncome = Math.Round(monthlyIncomeTask.Result, 2);
decimal totalIncome = Math.Round(totalIncomeTask.Result - 17957, 2);
            
var msg = @$"当前账户资源如下：
地址： <code>{Address}</code>
TRX余额： <b>{TRX}</b>
USDT余额： <b>{USDT}</b>
免费带宽： <b>{resource.FreeNetLimit - resource.FreeNetUsed}/{resource.FreeNetLimit}</b>
质押带宽： <b>{resource.NetLimit - resource.NetUsed}/{resource.NetLimit}</b>
质押能量： <b>{energyRemaining}/{resource.EnergyLimit}</b>
————————————————————
带宽质押比：<b>100 TRX = {resource.TotalNetLimit * 1.0m / resource.TotalNetWeight * 100:0.000} 带宽</b>
能量质押比：<b>100 TRX = {resource.TotalEnergyLimit * 1.0m / resource.TotalEnergyWeight * 100:0.000} 能量</b>
————————————————————
今日承兑：<b>{todayIncome} USDT</b>
本月承兑：<b>{monthlyIncome} USDT</b>
累计承兑：<b>{totalIncome} USDT</b>                
";
            // 创建包含两行，每行两个按钮的虚拟键盘
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new [] // 第一行
                {
                    new KeyboardButton("\U0001F4B0U兑TRX"),
                    new KeyboardButton("\U0001F570实时汇率"),
                    new KeyboardButton("\U0001F4B9汇率换算"),
                },   
                new [] // 第二行
                {
                    new KeyboardButton("\U0001F4B8币圈行情"),
                    new KeyboardButton("\U0001F310外汇助手"),
                    new KeyboardButton("\u2B50会员代开"),
                }    
            });
            keyboard.ResizeKeyboard = true;           
            keyboard.OneTimeKeyboard = false;
            return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                        text: msg,
                                                        parseMode: ParseMode.Html,
                                                        replyMarkup: keyboard);
        }
        async Task<Message> BindAddress(ITelegramBotClient botClient, Message message)
        {
            if (message.From == null) return message;
            if (message.Text is not { } messageText)
                return message;
            var address = messageText.Split(' ').Last();
            if (address.StartsWith("T") && address.Length == 34)
            {
                var from = message.From;
                var UserId = message.Chat.Id;

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
                    // 启动定时器来监控这个地址的TRX余额
                    StartMonitoring(botClient, UserId, address);
                }
                else
                {
                    bind.Currency = Currency.TRX;
                    bind.UserId = UserId;
                    bind.Address = address;
                    bind.UserName = $"@{from.Username}";
                    bind.FullName = $"{from.FirstName} {from.LastName}";
                    await _bindRepository.UpdateAsync(bind);
                    // 启动定时器来监控这个地址的TRX余额
                    StartMonitoring(botClient, UserId, address);
                }
// 创建包含两行，每行两个按钮的虚拟键盘
var keyboard = new ReplyKeyboardMarkup(new[]
{
    new [] // 第一行
    {
        new KeyboardButton("\U0001F4B0U兑TRX"),
        new KeyboardButton("\U0001F570实时汇率"),
        new KeyboardButton("\U0001F4B9汇率换算"),
    },   
    new [] // 第二行
    {
        new KeyboardButton("\U0001F4B8币圈行情"),
        new KeyboardButton("\U0001F310外汇助手"),
        new KeyboardButton("\u2B50会员代开"),
    }    
});
                keyboard.ResizeKeyboard = true; // 调整键盘高度
                keyboard.OneTimeKeyboard = false;
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: @$"您已成功绑定：<code>{address}</code>
当我们向您的钱包转账或钱包TRX余额不足时，您将收到通知！
", parseMode: ParseMode.Html, replyMarkup: keyboard);
            }
            else
            {
// 创建包含两行，每行两个按钮的虚拟键盘
var keyboard = new ReplyKeyboardMarkup(new[]
{
    new [] // 第一行
    {
        new KeyboardButton("\U0001F4B0U兑TRX"),
        new KeyboardButton("\U0001F570实时汇率"),
        new KeyboardButton("\U0001F4B9汇率换算"),
    },   
    new [] // 第二行
    {
        new KeyboardButton("\U0001F4B8币圈行情"),
        new KeyboardButton("\U0001F310外汇助手"),
        new KeyboardButton("\u2B50会员代开"),
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
    if (userTimers.TryGetValue(UserId, out var timer))
    {
        timer.Dispose();
        userTimers.Remove(UserId);
    }            
// 创建包含两行，每行两个按钮的虚拟键盘
var keyboard = new ReplyKeyboardMarkup(new[]
{
    new [] // 第一行
    {
        new KeyboardButton("\U0001F4B0U兑TRX"),
        new KeyboardButton("\U0001F570实时汇率"),
        new KeyboardButton("\U0001F4B9汇率换算"),
    },   
    new [] // 第二行
    {
        new KeyboardButton("\U0001F4B8币圈行情"),
        new KeyboardButton("\U0001F310外汇助手"),
        new KeyboardButton("\u2B50会员代开"),
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
实时汇率：<b>100 USDT = {100m.USDT_To_TRX(rate, FeeRate, 0):#.####} TRX</b>
获得TRX：<b>100 * {1m.USDT_To_TRX(rate, FeeRate, 0):#.####} = {100m.USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00} TRX</b></code>
    
注意：<b>只支持{MinUSDT} USDT以上的金额兑换！</b>    
如果TRX余额不足可发送 预支 领取一次转账能量！
只限钱包转账，自动原地址返TRX，如需兑换到其它地址请{adminText}！

转帐前，推荐您绑定钱包地址来接收入账通知及TRX余额不足提醒： 
发送：<code>绑定 Txxxxxxx</code>(您的钱包地址)         {leftPointingIndex} <b>推荐使用！！！</b> 


";
            }
// 创建包含两行，每行两个按钮的虚拟键盘
var keyboard = new ReplyKeyboardMarkup(new[]
{
    new [] // 第一行
    {
        new KeyboardButton("\U0001F4B0U兑TRX"),
        new KeyboardButton("\U0001F570实时汇率"),
        new KeyboardButton("\U0001F4B9汇率换算"),
    },   
    new [] // 第二行
    {
        new KeyboardButton("\U0001F4B8币圈行情"),
        new KeyboardButton("\U0001F310外汇助手"),
        new KeyboardButton("\u2B50会员代开"),
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

    if (message.Chat.Id == AdminUserId)
    {
        await HandleGetFollowersCommandAsync(botClient, message);
    }
    else
    {
        var msg = @$"<b>实时汇率表：</b>

<b>100 USDT = {100m.USDT_To_TRX(rate, FeeRate, 0):#.####} TRX   ≈ {100m * usdtPrice} CNY</b>
————————————————————<code>
  10 USDT = {(5m * 2).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00} TRX  ≈ {(5m * 2) * usdtPrice}  CNY
  20 USDT = {(5m * 4).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00} TRX  ≈ {(5m * 4) * usdtPrice} CNY
  50 USDT = {(5m * 10).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00} TRX  ≈ {(5m * 10) * usdtPrice} CNY
 100 USDT = {(5m * 20).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00} TRX ≈ {(5m * 20) * usdtPrice} CNY
 500 USDT = {(5m * 100).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00}TRX ≈ {(5m * 100) * usdtPrice} CNY
1000 USDT = {(5m * 200).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00}TRX≈ {(5m * 200) * usdtPrice} CNY
</code>
<b>机器人收款地址:(↓点击自动复制↓</b>):
        
<code>{ReciveAddress}</code>

注意：<b>只支持{MinUSDT} USDT以上的金额兑换！</b>    
如果TRX余额不足可发送 预支 领取一次转账能量！
只限钱包转账，自动原地址返TRX，如需兑换到其它地址请{adminText}！

转帐前，推荐您绑定钱包地址来接收入账通知及TRX余额不足提醒： 
发送：<code>绑定 Txxxxxxx</code>(您的钱包地址)         {leftPointingIndex} <b>推荐使用！！！</b> 



";

        // 创建包含两行，每行两个按钮的虚拟键盘
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new [] // 第一行
            {
                new KeyboardButton("\U0001F4B0U兑TRX"),
                new KeyboardButton("\U0001F570实时汇率"),
                new KeyboardButton("\U0001F4B9汇率换算"),
            },   
            new [] // 第二行
            {
                new KeyboardButton("\U0001F4B8币圈行情"),
                new KeyboardButton("\U0001F310外汇助手"),
                new KeyboardButton("\u2B50会员代开"),
            }    
        });

        keyboard.ResizeKeyboard = true; // 将键盘高度设置为最低
        keyboard.OneTimeKeyboard = false; // 添加这一行，确保虚拟键盘在用户与其交互后不会消失。

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: msg,
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            disableWebPagePreview: true // 添加这一行来禁用链接预览
        );
    }

    // 在这里添加一个返回空消息的语句
    return await Task.FromResult<Message>(null);
}
        //通用回复
        static async Task<Message> Start(ITelegramBotClient botClient, Message message)
        {
            // 先发送GIF
            string gifUrl = "https://i.postimg.cc/0QKYJ0Cb/333.gif"; // 替换为您的GIF URL
            await botClient.SendAnimationAsync(
                chatId: message.Chat.Id,
                animation: gifUrl
            );
            long userId = message.From.Id; // 更改为 long 类型
            string username = message.From.FirstName;
            string botUsername = "yifanfubot"; // 替换为你的机器人的用户名
            string startParameter = ""; // 如果你希望机器人在被添加到群组时收到一个特定的消息，可以设置这个参数
            string shareLink = $"https://t.me/{botUsername}?startgroup={startParameter}";
            string groupFunctionText = $"<a href=\"{shareLink}\">防骗助手：点击拉我进群，群成员修改资料会发送提醒哦！</a>";
            
            //1带ID  2不带
            //string usage = @$"<b>{username}</b> (ID:<code>{userId}</code>) 你好，欢迎使用TRX自助兑换机器人！
            string usage = @$"<b>{username}</b> 你好，欢迎使用TRX自助兑换机器人！
            
使用方法：
   点击菜单 选择&#x1F4B0;U兑TRX
   转账USDT到指定地址，即可秒回TRX！
   如需了解机器人功能介绍，直接发送：<code>帮助</code> 
   
   {groupFunctionText}
   
";
// 创建包含两行，每行两个按钮的虚拟键盘
var keyboard = new ReplyKeyboardMarkup(new[]
{
    new [] // 第一行
    {
        new KeyboardButton("\U0001F4B0U兑TRX"),
        new KeyboardButton("\U0001F570实时汇率"),
        new KeyboardButton("\U0001F4B9汇率换算"),
    },   
    new [] // 第二行
    {
        new KeyboardButton("\U0001F4B8币圈行情"),
        new KeyboardButton("\U0001F310外汇助手"),
        new KeyboardButton("\u2B50会员代开"),
    }    
});
            keyboard.ResizeKeyboard = true; // 将键盘高度设置为最低
            keyboard.OneTimeKeyboard = false; // 添加这一行，确保虚拟键盘在用户与其交互后保持可见
            return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                        text: usage,
                                                        parseMode: ParseMode.Html,
                                                        disableWebPagePreview: true,
                                                        replyMarkup: keyboard);
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
注：<b>群内计算需要回复机器人或设置机器人为管理</b>

";

    if (message.Chat.Id == AdminUserId)
    {
        return await ExecuteZjdhMethodAsync(botClient, message);
    }
    else
    {
        // 创建包含两行，每行两个按钮的虚拟键盘
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new [] // 第一行
            {
                new KeyboardButton("\U0001F4B0U兑TRX"),
                new KeyboardButton("\U0001F570实时汇率"),
                new KeyboardButton("\U0001F4B9汇率换算"),
            },   
            new [] // 第二行
            {
                new KeyboardButton("\U0001F4B8币圈行情"),
                new KeyboardButton("\U0001F310外汇助手"),
                new KeyboardButton("\u2B50会员代开"),
            }    
        });

        keyboard.ResizeKeyboard = true; // 将键盘高度设置为最低
        keyboard.OneTimeKeyboard = false; // 添加这一行，确保虚拟键盘在用户与其交互后保持可见

        return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                    text: usage,
                                                    parseMode: ParseMode.Html,
                                                    replyMarkup: keyboard);
    }
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
// 创建包含两行，每行两个按钮的虚拟键盘
var keyboard = new ReplyKeyboardMarkup(new[]
{
    new [] // 第一行
    {
        new KeyboardButton("\U0001F4B0U兑TRX"),
        new KeyboardButton("\U0001F570实时汇率"),
        new KeyboardButton("\U0001F4B9汇率换算"),
    },   
    new [] // 第二行
    {
        new KeyboardButton("\U0001F4B8币圈行情"),
        new KeyboardButton("\U0001F310外汇助手"),
        new KeyboardButton("\u2B50会员代开"),
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
