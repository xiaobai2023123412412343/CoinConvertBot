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


namespace Telegram.CoinConvertBot.BgServices.BotHandler;


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
    
static async Task SendAdvertisement(ITelegramBotClient botClient, CancellationToken cancellationToken, IBaseRepository<TokenRate> rateRepository, decimal FeeRate)
{
    // 将多个群的群组 ID 存储在一个集合中
    long[] groupIds = {
        -1001862069013, // 群组 ID 1
        //-838363978, // 群组 ID 2
        // 更多群组 ID
    };

    while (!cancellationToken.IsCancellationRequested)
    {
        var rate = await rateRepository.Where(x => x.Currency == Currency.USDT && x.ConvertCurrency == Currency.TRX).FirstAsync(x => x.Rate);
        decimal usdtToTrx = 100m.USDT_To_TRX(rate, FeeRate, 0);

        string advertisementText = $"\U0001F4B9实时汇率：<b>100 USDT = {usdtToTrx:#.####} TRX</b>\n\n\n" +
            "机器人收款地址:\n (<b>点击自动复制</b>):<code>TGUJoKVqzT7igyuwPfzyQPtcMFHu76QyaC</code>\n\n\n" + //手动输入地址
            "\U0000267B进U即兑,全自动返TRX,10U起兑!\n" +
            "\U0000267B请勿使用交易所或中心化钱包转账!\n" +
            "\U0000267B有任何问题,请私聊联系群主!\n\n\n" +
            "<b>另代开TG会员</b>:\n\n" +
            "\u2708三月高级会员   24.99 u\n" +
            "\u2708六月高级会员   39.99 u\n" +
            "\u2708一年高级会员   70.99 u\n" +
            "(<b>需要开通会员请联系群主,切记不要转TRX兑换地址!!!</b>)";
            
        // 创建 InlineKeyboardButton 并设置文本和回调数据
        var visitButton1 = new InlineKeyboardButton("\U0000267B 开始兑换")
        {
            Url = "https://t.me/yifanfubot" // 将此链接替换为你想要跳转的左侧链接
        };

        var visitButton2 = new InlineKeyboardButton("\U0001F5E3 私聊群主")
        {
            Url = "https://t.me/Yifanfu" // 将此链接替换为你想要跳转的右侧链接
        };
        
        // 创建 InlineKeyboardMarkup 并添加按钮
        var inlineKeyboard = new InlineKeyboardMarkup(new[] { new[] { visitButton1, visitButton2 } });

        // 用于存储已发送消息的字典
        var sentMessages = new Dictionary<long, Message>();

        // 遍历群组 ID 并发送广告消息
        foreach (var groupId in groupIds)
        {
            Message sentMessage = await botClient.SendTextMessageAsync(groupId, advertisementText, parseMode: ParseMode.Html, replyMarkup: inlineKeyboard);
            sentMessages[groupId] = sentMessage;
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
            _ => UnknownUpdateHandlerAsync(botClient, update)
        };

        try
        {
            await handler;
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
        Log.Information($"Receive message type: {message.Type}");
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
        // 检查是否接收到了 /gg 消息，收到就启动广告
        if (messageText.StartsWith("/gg"))
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var rateRepository = provider.GetRequiredService<IBaseRepository<TokenRate>>();
            _ = SendAdvertisement(botClient, cancellationTokenSource.Token, rateRepository, FeeRate);
        }

        messageText = messageText.Replace($"@{BotUserName}", "");
        var action = messageText.Split(' ')[0] switch
        {
            "/start" => Start(botClient, message),
            "/fu" => Valuation(botClient, message),
            "\U0001F4B0U兑TRX" => ConvertCoinTRX(botClient, message), // 添加这一行
            "\U0001F570实时汇率" => PriceTRX(botClient, message), // 添加这一行
            "\U0001F4B9估算价值" => Valuation(botClient, message), // 添加这一行
            "/yi" => ConvertCoinTRX(botClient, message),
            "/fan" => PriceTRX(botClient, message),
            "绑定波场地址" => BindAddress(botClient, message),
            "解绑波场地址" => UnBindAddress(botClient, message),
            "\u260E联系管理" => QueryAccount(botClient, message),
            "关闭键盘" => guanbi(botClient, message),
            _ => Usage(botClient, message)
        };
        async Task<decimal> GetMonthlyUSDTIncomeAsync(string ReciveAddress, string contractAddress)
{
    // 获取本月1号零点的时间戳
    var firstDayOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    var firstDayOfMonthMidnight = new DateTimeOffset(firstDayOfMonth).ToUnixTimeSeconds();

    // 调用TronGrid API以获取交易记录
    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    string apiEndpoint = $"https://api.trongrid.io/v1/accounts/{ReciveAddress}/transactions/trc20?only_confirmed=true&only_to=true&min_timestamp={firstDayOfMonthMidnight * 1000}&contract_address={contractAddress}";
    var response = await httpClient.GetAsync(apiEndpoint);

    if (!response.IsSuccessStatusCode)
    {
        // 请求失败，返回0
        return 0;
    }

    var jsonResponse = await response.Content.ReadAsStringAsync();
    JObject transactions = JObject.Parse(jsonResponse);

    // 遍历交易记录并累计 USDT 收入
    decimal usdtIncome = 0;
    foreach (var tx in transactions["data"])
    {
        var rawAmount = (decimal)tx["value"];
        usdtIncome += rawAmount / 1_000_000L;
    }

    return usdtIncome;
}
        async Task<decimal> GetTodayUSDTIncomeAsync(string ReciveAddress, string contractAddress)
{
    // 获取今天零点的时间戳
    var todayMidnight = new DateTimeOffset(DateTime.Today).ToUnixTimeSeconds();

    // 调用TronGrid API以获取交易记录
    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    string apiEndpoint = $"https://api.trongrid.io/v1/accounts/{ReciveAddress}/transactions/trc20?only_confirmed=true&only_to=true&min_timestamp={todayMidnight * 1000}&contract_address={contractAddress}";
    var response = await httpClient.GetAsync(apiEndpoint);

    if (!response.IsSuccessStatusCode)
    {
        // 请求失败，返回0
        return 0;
    }

    var jsonResponse = await response.Content.ReadAsStringAsync();
    JObject transactions = JObject.Parse(jsonResponse);

    // 遍历交易记录并累计 USDT 收入
    decimal usdtIncome = 0;
    foreach (var tx in transactions["data"])
    {
        var rawAmount = (decimal)tx["value"];
        usdtIncome += rawAmount / 1_000_000L;
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
              // 发送第一个链接
              await botClient.SendTextMessageAsync(
                  chatId: message.Chat.Id,
                  text: "管理员：https://t.me/Yifanfu"
              );

              // 发送第二个链接
              await botClient.SendTextMessageAsync(
                  chatId: message.Chat.Id,
                  text: "讨论群组:https://t.me/+b4NunT6Vwf0wZWI1" // 更改为您需要发送的第二个链接
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

            var resource = await protocol.GetAccountResourceAsync(new TronNet.Protocol.Account
            {
                Address = addr
            });
            var account = await protocol.GetAccountAsync(new TronNet.Protocol.Account
            {
                Address = addr
            });
            var TRX = Convert.ToDecimal(account.Balance) / 1_000_000L;
            var contractAddress = _myTronConfig.Value.USDTContractAddress;
            var contractClient = _contractClientFactory.CreateClient(ContractProtocol.TRC20);
            //Log.Information("查询 USDT 余额...");
            var USDT = await contractClient.BalanceOfAsync(contractAddress, _wallet.GetAccount(_myTronConfig.Value.PrivateKey));
            //Log.Information($"查询 USDT 余额: 合约地址: {contractAddress}, 查询地址: {_wallet.GetAccount(_myTronConfig.Value.PrivateKey).Address}, 余额: {USDT}");
            
             // 调用新方法获取今日收入
            //Log.Information("查询今日收入...");
            string targetReciveAddress = "TGUJoKVqzT7igyuwPfzyQPtcMFHu76QyaC";//填写你想要监控收入的地址
            decimal todayIncome = await GetTodayUSDTIncomeAsync(targetReciveAddress, contractAddress);
            //Log.Information($"今日收入: {todayIncome}");
            
            // 调用新方法获取本月收入
            decimal monthlyIncome = await GetMonthlyUSDTIncomeAsync(targetReciveAddress, contractAddress);

            var msg = @$"当前账户资源如下：
地址： <code>{Address}</code>
TRX余额： <b>{TRX}</b>
USDT余额： <b>{USDT}</b>
免费带宽： <b>{resource.FreeNetLimit - resource.FreeNetUsed}/{resource.FreeNetLimit}</b>
质押带宽： <b>{resource.NetLimit - resource.NetUsed}/{resource.NetLimit}</b>
质押能量： <b>{resource.EnergyUsed}/{resource.EnergyLimit}</b>
————————————————————
带宽质押比：<b>100 TRX = {resource.TotalNetLimit * 1.0m / resource.TotalNetWeight * 100:0.000} 带宽</b>
能量质押比：<b>100 TRX = {resource.TotalEnergyLimit * 1.0m / resource.TotalEnergyWeight * 100:0.000} 能量</b>
————————————————————
今日承兑：<b>{todayIncome} USDT</b>
本月承兑：<b>{monthlyIncome} USDT</b>
";

            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new [] // 第一行
                {
                    new KeyboardButton("\U0001F4B0U兑TRX"),
                    new KeyboardButton("\U0001F570实时汇率"),
                },
                new [] // 第二行
                {
                    new KeyboardButton("\U0001F4B9估算价值"),
                    new KeyboardButton("\u260E联系管理"),
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
                }
                else
                {
                    bind.Currency = Currency.TRX;
                    bind.UserId = UserId;
                    bind.Address = address;
                    bind.UserName = $"@{from.Username}";
                    bind.FullName = $"{from.FirstName} {from.LastName}";
                    await _bindRepository.UpdateAsync(bind);
                }
                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new [] // 第一行
                    {
                        new KeyboardButton("\U0001F4B0U兑TRX"),
                        new KeyboardButton("\U0001F570实时汇率"),
                    },
                    new [] // 第二行
                    {
                        new KeyboardButton("\U0001F4B9估算价值"),
                        new KeyboardButton("\u260E联系管理"),
                    }                                             
                });
                keyboard.ResizeKeyboard = true; // 调整键盘高度
                keyboard.OneTimeKeyboard = false;
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: @$"您已成功绑定<b>{address}</b>！
当我们向您的钱包转账时，您将收到通知！
如需解绑，请发送
<code>解绑波场地址 Txxxxxxx</code>(您的钱包地址)", parseMode: ParseMode.Html, replyMarkup: keyboard);
            }
            else
            {
                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new [] // 第一行
                    {
                        new KeyboardButton("\U0001F4B0U兑TRX"),
                        new KeyboardButton("\U0001F570实时汇率"),
                    },
                    new [] // 第二行
                    {
                        new KeyboardButton("\U0001F4B9估算价值"),
                        new KeyboardButton("\u260E联系管理"),
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
                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new [] // 第一行
                    {
                        new KeyboardButton("\U0001F4B0U兑TRX"),
                        new KeyboardButton("\U0001F570实时汇率"),
                    },
                    new [] // 第二行
                    {
                        new KeyboardButton("\U0001F4B9估算价值"),
                        new KeyboardButton("\u260E联系管理"),
                    }                                             
                });
                keyboard.ResizeKeyboard = true; // 调整键盘高度
                keyboard.OneTimeKeyboard = false;
            return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: $"您已成功解绑<b>{address}</b>！", parseMode: ParseMode.Html, replyMarkup: keyboard);

        }
        async Task<Message> ConvertCoinTRX(ITelegramBotClient botClient, Message message)
        {
            if (message.From == null) return message;
            var from = message.From;
            var UserId = message.From.Id;
            var _rateRepository = provider.GetRequiredService<IBaseRepository<TokenRate>>();
            var rate = await _rateRepository.Where(x => x.Currency == Currency.USDT && x.ConvertCurrency == Currency.TRX).FirstAsync(x => x.Rate);

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
<code>绑定波场地址 Txxxxxxx</code>(您的钱包地址)
";
            if (USDTFeeRate == 0)
            {
                msg = @$"<b>请向此地址转入任意金额，机器人自动回款TRX</b>
机器人收款地址:(<b>↓点击自动复制↓</b>):<code>{ReciveAddress}</code>

示例：
<code>转入金额：<b>100 USDT</b>
实时汇率：<b>1 USDT = {1m.USDT_To_TRX(rate, FeeRate, 0):#.####} TRX</b>
获得TRX：<b>100 * {1m.USDT_To_TRX(rate, FeeRate, 0):#.####} = {100m.USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00} TRX</b></code>

注意：<b>只支持{MinUSDT} USDT以上的金额兑换。</b>
<b>只限钱包转账，收到u自动返回TRX，如需兑换到其它地址请联系管理!</b>

转帐前，推荐您使用以下命令来接收入账通知
<code>绑定波场地址 Txxxxxxx</code>(您的钱包地址)


<b>限时福利：</b>
<code>单笔兑换：<b>666 USDT或以上金额,电报会员免费送!!!</b></code>
<code>单笔兑换：<b>666 USDT或以上金额,电报会员免费送!!!</b></code>
<code>单笔兑换：<b>666 USDT或以上金额,电报会员免费送!!!</b></code>
";
            }
            // 创建包含两行，每行两个按钮的虚拟键盘
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new [] // 第一行
                {
                    new KeyboardButton("\U0001F4B0U兑TRX"),
                    new KeyboardButton("\U0001F570实时汇率"),
                },   
                new [] // 第二行
                {
                    new KeyboardButton("\U0001F4B9估算价值"),
                    new KeyboardButton("\u260E联系管理"),
                }    
                
            });
            keyboard.ResizeKeyboard = true; // 将键盘高度设置为最低
            keyboard.OneTimeKeyboard = false; // 添加这一行，确保虚拟键盘在用户与其交互后保持可见            
            return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                        text: msg,
                                                        parseMode: ParseMode.Html,
                                                        replyMarkup: keyboard);
        }
        async Task<Message> PriceTRX(ITelegramBotClient botClient, Message message)
        {
            if (message.From == null) return message;
            var from = message.From;
            var UserId = message.From.Id;
            var _rateRepository = provider.GetRequiredService<IBaseRepository<TokenRate>>();
            var rate = await _rateRepository.Where(x => x.Currency == Currency.USDT && x.ConvertCurrency == Currency.TRX).FirstAsync(x => x.Rate);

            var addressArray = configuration.GetSection("Address:USDT-TRC20").Get<string[]>();
            var ReciveAddress = addressArray.Length == 0 ? "未配置" : addressArray[UserId % addressArray.Length];
            var msg = @$"<b>实时价目表</b>

实时汇率：<b>100 USDT = {100m.USDT_To_TRX(rate, FeeRate, 0):#.####} TRX</b>
————————————————————<code>
   5 USDT = {(5m * 1).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00} TRX
  10 USDT = {(5m * 2).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00} TRX
  20 USDT = {(5m * 4).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00} TRX
  50 USDT = {(5m * 10).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00} TRX
 100 USDT = {(5m * 20).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00} TRX
 500 USDT = {(5m * 100).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00} TRX
1000 USDT = {(5m * 200).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00} TRX
</code>

机器人收款地址:(<b>↓点击自动复制↓</b>):<code>{ReciveAddress}</code>

注意：<b>暂时只支持{MinUSDT} USDT以上(不含{MinUSDT} USDT)的金额兑换，若转入{MinUSDT} USDT及以下金额，将无法退还！！！</b>

转帐前，推荐您使用以下命令来接收入账通知
<code>绑定波场地址 Txxxxxxx</code>(您的钱包地址)


<b>限时福利：</b>
<code>单笔兑换：<b>666 USDT或以上金额,电报会员免费送!!!</b></code>
<code>单笔兑换：<b>666 USDT或以上金额,电报会员免费送!!!</b></code>
<code>单笔兑换：<b>666 USDT或以上金额,电报会员免费送!!!</b></code>
";
            // 创建包含两行，每行两个按钮的虚拟键盘
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new [] // 第一行
                {
                    new KeyboardButton("\U0001F4B0U兑TRX"),
                    new KeyboardButton("\U0001F570实时汇率"),
                },   
                new [] // 第二行
                {
                    new KeyboardButton("\U0001F4B9估算价值"),
                    new KeyboardButton("\u260E联系管理"),
                }    
                
            });
            keyboard.ResizeKeyboard = true; // 将键盘高度设置为最低
            keyboard.OneTimeKeyboard = false; // 添加这一行，确保虚拟键盘在用户与其交互后保持可见

            return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                        text: msg,
                                                        parseMode: ParseMode.Html,
                                                        replyMarkup: keyboard);
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
            string username = message.From.FirstName;
            string usage = @$"<b>{username}</b> 你好，欢迎使用TRX自助兑换机器人！
使用方法：
   点击菜单 选择&#x1F4B0;U兑TRX
   转账USDT到指定地址，即可秒回TRX
   如有需要，请联系管理员： {AdminUserUrl}
";
            // 创建包含两行，每行两个按钮的虚拟键盘
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new [] // 第一行
                {
                    new KeyboardButton("\U0001F4B0U兑TRX"),
                    new KeyboardButton("\U0001F570实时汇率"),
                },   
                new [] // 第二行
                {
                    new KeyboardButton("\U0001F4B9估算价值"),
                    new KeyboardButton("\u260E联系管理"),
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
            string usage = @$"如需估价请直接发送<b>金额+币种</b>
如发送： <code>10 USDT</code>
回复：<b>10 USDT = xxx TRX</b>

如发送： <code>100 TRX</code>
回复：<b>100 TRX = xxx USDT</b>

如有需要，请联系管理员： {AdminUserUrl}
";
    
              // 创建包含两行，每行两个按钮的虚拟键盘
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new [] // 第一行
                {
                    new KeyboardButton("\U0001F4B0U兑TRX"),
                    new KeyboardButton("\U0001F570实时汇率"),
                },   
                new [] // 第二行
                {
                    new KeyboardButton("\U0001F4B9估算价值"),
                    new KeyboardButton("\u260E联系管理"),
                }    
                
            });
            keyboard.ResizeKeyboard = true; // 将键盘高度设置为最低
            keyboard.OneTimeKeyboard = false; // 添加这一行，确保虚拟键盘在用户与其交互后保持可见  
            return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                        text: usage,
                                                        parseMode: ParseMode.Html,
                                                        replyMarkup: keyboard);
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
                },   
                new [] // 第二行
                {
                    new KeyboardButton("\U0001F4B9估算价值"),
                    new KeyboardButton("\u260E联系管理"),
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
