
using Flurl.Http;
using FreeSql;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Telegram.Bot;
using Telegram.CoinConvertBot.BgServices.Base;
using Telegram.CoinConvertBot.BgServices.BotHandler;
using Telegram.CoinConvertBot.Domains.Tables;
using Telegram.CoinConvertBot.Helper;
using Telegram.CoinConvertBot.Models;
using TronNet;
using TronNet.Contracts;

namespace Telegram.CoinConvertBot.BgServices
{
    public class TransferTrxService : BaseScheduledService
    {
        private readonly ILogger<TransferTrxService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ITelegramBotClient _botClient;
        private readonly IServiceProvider _serviceProvider;
        //备注开始收手续费了，去掉
        //private string TransferMemo => _configuration.GetValue("TransferMemo", "Transfer From Github CoinConvertBot");
        private long SendTo => _configuration.GetValue<long>("SendTo");

        public TransferTrxService(ILogger<TransferTrxService> logger,
            IConfiguration configuration,
            ITelegramBotClient botClient,
            IServiceProvider serviceProvider) : base("TRX转账", TimeSpan.FromSeconds(15), logger)
        {
            _logger = logger;
            _configuration = configuration;
            _botClient = botClient;
            _serviceProvider = serviceProvider;
        }

       /*
       //旧版兑换操作，每个金额换的TRX都固定汇率，新版阶梯汇率
       
        protected override async Task ExecuteAsync()
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            var provider = scope.ServiceProvider;
            var _repository = provider.GetRequiredService<IBaseRepository<TokenRecord>>();
            IHostEnvironment hostEnvironment = provider.GetRequiredService<IHostEnvironment>();
            var _bindRepository = provider.GetRequiredService<IBaseRepository<TokenBind>>();

            var _rateRepository = provider.GetRequiredService<IBaseRepository<TokenRate>>();
            var rate = await _rateRepository.Where(x => x.Currency == Currency.USDT && x.ConvertCurrency == Currency.TRX).FirstAsync(x => x.Rate);
            if (rate == 0)
            {
                return;
            }
            var BlackList = _configuration.GetSection("BlackList").Get<string[]>() ?? new string[0];
            var Orders = await _repository
                .Where(x => x.OriginalCurrency == Currency.USDT)
                .Where(x => x.Status == Status.Pending)
                .Where(x => x.OriginalAmount >= UpdateHandlers.MinUSDT)
                .WhereIf(BlackList.Length > 0, x => !BlackList.Contains(x.FromAddress))
                .ToListAsync();
            if (Orders.Count > 0)
                _logger.LogInformation("待转账订单检测，订单数：{c}", Orders.Count);
            foreach (var order in Orders)
            {
                _logger.LogInformation("开始处理待转账订单: {c}", order.BlockTransactionId);
                order.ConvertAmount = order.OriginalAmount.USDT_To_TRX(rate, UpdateHandlers.FeeRate, UpdateHandlers.USDTFeeRate);
                try
                {
                    var result = await TransferTrxAsync(scope.ServiceProvider, order.FromAddress, order.ConvertAmount);
                    if (result.Success)
                    {
                        order.Status = Status.Paid;
                        order.PayTime = DateTime.Now;
                        order.Txid = result.Data;
                    }
                    else
                    {
                        order.Status = Status.Error;
                        order.Error = result.Message;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "转账TRX出错！");
                    order.Status = Status.Error;
                    order.Error = e.Message;
                }

                await _repository.UpdateAsync(order);
                var AdminUserId = _configuration.GetValue<long>("BotConfig:AdminUserId");

                try
                {
                    var record = order;
                    var binds = await _bindRepository.Where(x => x.Currency == Currency.TRX && x.Address == record.FromAddress).ToListAsync();
                    if (order.Status == Status.Paid)
                    {
                        var viewUrl = $"https://shasta.tronscan.org/#/transaction/{order.Txid}";
                        if (hostEnvironment.IsProduction())
                        {
                            viewUrl = $"https://tronscan.org/#/transaction/{order.Txid}";
                        }
                        Bot.Types.ReplyMarkups.InlineKeyboardMarkup inlineKeyboard = new(
                            new[]
                            {
                                            new []
                                            {
                                                Bot.Types.ReplyMarkups.InlineKeyboardButton.WithUrl("查看交易",viewUrl),
                                            },
                            });
                        if (SendTo != 0)
                        {
                            try
                            {
                                await _botClient.SendTextMessageAsync(SendTo, $@"<b>新交易 {"\U0001F4B8"} 兑换 <b>{record.ConvertAmount:#.######} {record.ConvertCurrency}</b></b>

兑换金额：<b>{record.OriginalAmount:#.######} {record.OriginalCurrency}</b>
兑换时间：<b>{record.ReceiveTime:yyyy-MM-dd HH:mm:ss}</b>
兑换地址：<code>{record.FromAddress.Substring(0, 5)}****{record.FromAddress.Substring(record.FromAddress.Length - 5, 5)}</code>
兑换时间：<b>{record.PayTime:yyyy-MM-dd HH:mm:ss}</b>
", Bot.Types.Enums.ParseMode.Html);  //", Bot.Types.Enums.ParseMode.Html, replyMarkup: inlineKeyboard); //去掉按钮
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, $"给指定目标发送通知失败！目标ID：{SendTo}");
                            }
                        }
                        if (binds.Count > 0)
                        {
                            foreach (var bind in binds)
                            {
                                try
                                {
                                    await _botClient.SendTextMessageAsync(bind.UserId, $@"<b>我们已向您发送{record.ConvertCurrency}</b>
入账金额：<b>{record.OriginalAmount:#.######} {record.OriginalCurrency}</b>
入账哈希：<code>{record.BlockTransactionId}</code>
入账时间：<b>{record.ReceiveTime:yyyy-MM-dd HH:mm:ss}</b>
入账地址：<code>{record.FromAddress}</code>
出账金额：<b>{record.ConvertAmount:#.######} {record.ConvertCurrency}</b>
出账哈希：<code>{record.Txid}</code>
出账时间：<b>{record.PayTime:yyyy-MM-dd HH:mm:ss}</b>

您的兑换已完成！
", Bot.Types.Enums.ParseMode.Html, replyMarkup: inlineKeyboard);
                                }
                                catch (Exception e)
                                {
                                    _logger.LogError(e, $"给用户发送通知失败！用户ID：{bind.UserId}");
                                }
                            }
                        }
                        if (AdminUserId > 0)
                        {
                            var _myTronConfig = provider.GetRequiredService<IOptionsSnapshot<MyTronConfig>>();
                            var _wallet = provider.GetRequiredService<IWalletClient>();
                            var protocol = _wallet.GetProtocol();
                            var addr = _wallet.ParseAddress(_myTronConfig.Value.Address);
                            var account = await protocol.GetAccountAsync(new TronNet.Protocol.Account
                            {
                                Address = addr
                            });
                            var TRX = Convert.ToDecimal(account.Balance) / 1_000_000L;

                            await _botClient.SendTextMessageAsync(AdminUserId, $@"<b>{record.ConvertCurrency}出账通知！({record.OriginalAmount:#.######} {record.OriginalCurrency} -> {record.ConvertAmount:#.######} {record.ConvertCurrency})</b>

订单：<code>{record.BlockTransactionId}</code>
转入：<b>{record.OriginalAmount:#.######} {record.OriginalCurrency}</b>
转出：<b>{record.ConvertAmount:#.######} {record.ConvertCurrency}</b>
时间：<b>{record.PayTime:yyyy-MM-dd HH:mm:ss}</b>
地址：<code>{record.FromAddress}</code>
-----------------------------
余额：<b>{TRX} TRX</b>
已用带宽：<b>{account.FreeNetUsage + account.NetUsage}</b>
", Bot.Types.Enums.ParseMode.Html, replyMarkup: inlineKeyboard);
                        }
                    }
                    else
                    {
                        if (binds.Count > 0)
                        {
                            foreach (var bind in binds)
                            {
                                try
                                {
                                    await _botClient.SendTextMessageAsync(bind.UserId, $@"<b>订单处理中！</b>
入账金额：<b>{record.OriginalAmount:#.######} {record.OriginalCurrency}</b>
入账哈希：<code>{record.BlockTransactionId}</code>
入账时间：<b>{record.ReceiveTime:yyyy-MM-dd HH:mm:ss}</b>
入账地址：<code>{record.FromAddress}</code>
出账金额：<b>{record.ConvertAmount:#.######} {record.ConvertCurrency}</b>
出账哈希：<code>{record.Txid}</code>
出账时间：<b>{record.PayTime:yyyy-MM-dd HH:mm:ss}</b>


如超时未到账请联系管理员处理！
管理员： {UpdateHandlers.AdminUserUrl}
", Bot.Types.Enums.ParseMode.Html);
                                }
                                catch (Exception e)
                                {
                                    _logger.LogError(e, $"给用户发送通知失败！用户ID：{bind.UserId}");
                                }
                            }
                        }
                        if (AdminUserId > 0)
                            await _botClient.SendTextMessageAsync(AdminUserId, $@"<b>{record.ConvertCurrency}出账失败！({record.OriginalAmount:#.######} {record.OriginalCurrency} -> {record.ConvertAmount:#.######} {record.ConvertCurrency})</b>

订单：<code>{record.BlockTransactionId}</code>
转入：<b>{record.OriginalAmount:#.######} {record.OriginalCurrency}</b>
地址：<code>{record.FromAddress}</code>
转出：<b>{record.ConvertAmount:#.######} {record.ConvertCurrency}</b>
时间：<b>{record.PayTime:yyyy-MM-dd HH:mm:ss}</b>
原因：<b>{record.Error}</b>
", Bot.Types.Enums.ParseMode.Html);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "发送TG通知失败！");
                }
            }


        }
        */
//新版阶梯兑换汇率        
protected override async Task ExecuteAsync()
{
    using IServiceScope scope = _serviceProvider.CreateScope();
    var provider = scope.ServiceProvider;
    var _repository = provider.GetRequiredService<IBaseRepository<TokenRecord>>();
    IHostEnvironment hostEnvironment = provider.GetRequiredService<IHostEnvironment>();
    var _bindRepository = provider.GetRequiredService<IBaseRepository<TokenBind>>();

    var _rateRepository = provider.GetRequiredService<IBaseRepository<TokenRate>>();
    var rate = await _rateRepository.Where(x => x.Currency == Currency.USDT && x.ConvertCurrency == Currency.TRX).FirstAsync(x => x.Rate);
    if (rate == 0)
    {
        return;
    }
    var BlackList = _configuration.GetSection("BlackList").Get<string[]>() ?? new string[0];
    var Orders = await _repository
        .Where(x => x.OriginalCurrency == Currency.USDT)
        .Where(x => x.Status == Status.Pending)
        .Where(x => x.OriginalAmount >= UpdateHandlers.MinUSDT)
        .WhereIf(BlackList.Length > 0, x => !BlackList.Contains(x.FromAddress))
        .ToListAsync();
    if (Orders.Count > 0)
        _logger.LogInformation("待转账订单检测，订单数：{c}", Orders.Count);
    foreach (var order in Orders)
    {
        _logger.LogInformation("开始处理待转账订单: {c}", order.BlockTransactionId);

        // 根据 USDT 金额调整汇率
        decimal adjustedRate = rate; // 原始汇率
        string rateInfo = string.Empty; // 原始汇率不加提示
        if (order.OriginalAmount <= 20)
        {
            adjustedRate = rate; // ≤ 20 USDT：使用原始汇率
            _logger.LogInformation("订单 {id} 使用原始汇率: {rate}", order.BlockTransactionId, adjustedRate);
        }
        else if (order.OriginalAmount <= 99)
        {
            adjustedRate = rate * 1.01m; // 20 < USDT ≤ 99：增加 1%
            rateInfo = "加赠 1% \U00002705";
            _logger.LogInformation("订单 {id} 使用调整汇率 (+1%): {rate}", order.BlockTransactionId, adjustedRate);
        }
        else if (order.OriginalAmount <= 299)
        {
            adjustedRate = rate * 1.02m; // 100 ≤ USDT ≤ 299：增加 2%
            rateInfo = "加赠 2% \U00002705\U00002705";
            _logger.LogInformation("订单 {id} 使用调整汇率 (+2%): {rate}", order.BlockTransactionId, adjustedRate);
        }
        else if (order.OriginalAmount <= 799)
        {
            adjustedRate = rate * 1.03m; // 300 ≤ USDT ≤ 799：增加 3%
            rateInfo = "加赠 3% \U00002705\U00002705\U00002705";
            _logger.LogInformation("订单 {id} 使用调整汇率 (+3%): {rate}", order.BlockTransactionId, adjustedRate);
        }
        else // order.OriginalAmount >= 800
        {
            adjustedRate = rate * 1.05m; // ≥ 800 USDT：增加 5%
            rateInfo = "加赠 5% \U00002705\U00002705\U00002705";
            _logger.LogInformation("订单 {id} 使用调整汇率 (+5%): {rate}", order.BlockTransactionId, adjustedRate);
        }

        // 计算 TRX 金额，使用调整后的汇率
        order.ConvertAmount = order.OriginalAmount.USDT_To_TRX(adjustedRate, UpdateHandlers.FeeRate, UpdateHandlers.USDTFeeRate);

        try
        {
            var result = await TransferTrxAsync(scope.ServiceProvider, order.FromAddress, order.ConvertAmount);
            if (result.Success)
            {
                order.Status = Status.Paid;
                order.PayTime = DateTime.Now;
                order.Txid = result.Data;
            }
            else
            {
                order.Status = Status.Error;
                order.Error = result.Message;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "转账TRX出错！");
            order.Status = Status.Error;
            order.Error = e.Message;
        }

        await _repository.UpdateAsync(order);
        var AdminUserId = _configuration.GetValue<long>("BotConfig:AdminUserId");

        try
        {
            var record = order;
            var binds = await _bindRepository.Where(x => x.Currency == Currency.TRX && x.Address == record.FromAddress).ToListAsync();
            if (order.Status == Status.Paid)
            {
                var viewUrl = $"https://shasta.tronscan.org/#/transaction/{order.Txid}";
                if (hostEnvironment.IsProduction())
                {
                    viewUrl = $"https://tronscan.org/#/transaction/{order.Txid}";
                }
                Bot.Types.ReplyMarkups.InlineKeyboardMarkup inlineKeyboard = new(
                    new[]
                    {
                        new []
                        {
                            Bot.Types.ReplyMarkups.InlineKeyboardButton.WithUrl("查看交易", viewUrl),
                        },
                    });
                if (SendTo != 0)
                {
                    try
                    {
                        await _botClient.SendTextMessageAsync(SendTo, $@"<b>新交易 {"\U0001F4B8"} 兑换 <b>{record.ConvertAmount:#.######} {record.ConvertCurrency}{(string.IsNullOrEmpty(rateInfo) ? "" : $" | {rateInfo}")}</b></b>

兑换金额：<b>{record.OriginalAmount:#.######} {record.OriginalCurrency}</b>
兑换时间：<b>{record.ReceiveTime:yyyy-MM-dd HH:mm:ss}</b>
兑换地址：<code>{record.FromAddress.Substring(0, 5)}****{record.FromAddress.Substring(record.FromAddress.Length - 5, 5)}</code>
兑换时间：<b>{record.PayTime:yyyy-MM-dd HH:mm:ss}</b>
", Bot.Types.Enums.ParseMode.Html);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"给指定目标发送通知失败！目标ID：{SendTo}");
                    }
                }
                if (binds.Count > 0)
                {
                    foreach (var bind in binds)
                    {
                        try
                        {
                            await _botClient.SendTextMessageAsync(bind.UserId, $@"<b>我们已向您发送{record.ConvertCurrency}</b>

入账金额：<b>{record.OriginalAmount:#.######} {record.OriginalCurrency}</b>
入账哈希：<code>{record.BlockTransactionId}</code>
入账时间：<b>{record.ReceiveTime:yyyy-MM-dd HH:mm:ss}</b>
入账地址：<code>{record.FromAddress}</code>
出账金额：<b>{record.ConvertAmount:#.######} {record.ConvertCurrency}{(string.IsNullOrEmpty(rateInfo) ? "" : $" | {rateInfo}")}</b>
出账哈希：<code>{record.Txid}</code>
出账时间：<b>{record.PayTime:yyyy-MM-dd HH:mm:ss}</b>

您的兑换已完成！
", Bot.Types.Enums.ParseMode.Html, replyMarkup: inlineKeyboard);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, $"给用户发送通知失败！用户ID：{bind.UserId}");
                        }
                    }
                }
                if (AdminUserId > 0)
                {
                    var _myTronConfig = provider.GetRequiredService<IOptionsSnapshot<MyTronConfig>>();
                    var _wallet = provider.GetRequiredService<IWalletClient>();
                    var protocol = _wallet.GetProtocol();
                    var addr = _wallet.ParseAddress(_myTronConfig.Value.Address);
                    var account = await protocol.GetAccountAsync(new TronNet.Protocol.Account
                    {
                        Address = addr
                    });
                    var TRX = Convert.ToDecimal(account.Balance) / 1_000_000L;

                    await _botClient.SendTextMessageAsync(AdminUserId, $@"<b>{record.ConvertCurrency}出账通知！({record.OriginalAmount:#.######} {record.OriginalCurrency} -> {record.ConvertAmount:#.######} {record.ConvertCurrency}{(string.IsNullOrEmpty(rateInfo) ? "" : $" | {rateInfo}")})</b>

订单：<code>{record.BlockTransactionId}</code>
转入：<b>{record.OriginalAmount:#.######} {record.OriginalCurrency}</b>
转出：<b>{record.ConvertAmount:#.######} {record.ConvertCurrency}{(string.IsNullOrEmpty(rateInfo) ? "" : $" | {rateInfo}")}</b>
时间：<b>{record.PayTime:yyyy-MM-dd HH:mm:ss}</b>
地址：<code>{record.FromAddress}</code>
-----------------------------
余额：<b>{TRX} TRX{(string.IsNullOrEmpty(rateInfo) ? "" : $" | {rateInfo}")}</b>
已用带宽：<b>{account.FreeNetUsage + account.NetUsage}</b>
", Bot.Types.Enums.ParseMode.Html, replyMarkup: inlineKeyboard);
                }
            }
            else
            {
                if (binds.Count > 0)
                {
                    foreach (var bind in binds)
                    {
                        try
                        {
                            await _botClient.SendTextMessageAsync(bind.UserId, $@"<b>订单处理中！</b>
                            
入账金额：<b>{record.OriginalAmount:#.######} {record.OriginalCurrency}</b>
入账哈希：<code>{record.BlockTransactionId}</code>
入账时间：<b>{record.ReceiveTime:yyyy-MM-dd HH:mm:ss}</b>
入账地址：<code>{record.FromAddress}</code>
出账金额：<b>{record.ConvertAmount:#.######} {record.ConvertCurrency}{(string.IsNullOrEmpty(rateInfo) ? "" : $" | {rateInfo}")}</b>
出账哈希：<code>{record.Txid}</code>
出账时间：<b>{record.PayTime:yyyy-MM-dd HH:mm:ss}</b>

如超时未到账请联系管理员处理！
管理员： {UpdateHandlers.AdminUserUrl}
", Bot.Types.Enums.ParseMode.Html);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, $"给用户发送通知失败！用户ID：{bind.UserId}");
                        }
                    }
                }
                if (AdminUserId > 0)
                    await _botClient.SendTextMessageAsync(AdminUserId, $@"<b>{record.ConvertCurrency}出账失败！({record.OriginalAmount:#.######} {record.OriginalCurrency} -> {record.ConvertAmount:#.######} {record.ConvertCurrency}{(string.IsNullOrEmpty(rateInfo) ? "" : $" | {rateInfo}")})</b>

订单：<code>{record.BlockTransactionId}</code>
转入：<b>{record.OriginalAmount:#.######} {record.OriginalCurrency}</b>
地址：<code>{record.FromAddress}</code>
转出：<b>{record.ConvertAmount:#.######} {record.ConvertCurrency}{(string.IsNullOrEmpty(rateInfo) ? "" : $" | {rateInfo}")}</b>
时间：<b>{record.PayTime:yyyy-MM-dd HH:mm:ss}</b>
原因：<b>{record.Error}</b>
", Bot.Types.Enums.ParseMode.Html);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "发送TG通知失败！");
        }
    }
}        
        public static async Task<TransactionResult> TransferTrxAsync(IServiceProvider provider, string ToAddress, decimal Amount, string? Memo = null)
        {
            var Result = new TransactionResult()
            {
                Success = false
            };
            var _options = provider.GetRequiredService<IOptions<TronNetOptions>>();
            var _myTronConfig = provider.GetRequiredService<IOptionsSnapshot<MyTronConfig>>();
            var _wallet = provider.GetRequiredService<IWalletClient>();
            var _transactionClient = provider.GetRequiredService<ITransactionClient>();
            var privateKey = _myTronConfig.Value.PrivateKey;
            var account = _wallet.GetAccount(privateKey);
            var ecKey = new TronECKey(privateKey, _options.Value.Network);
            var from = ecKey.GetPublicAddress();
            var to = ToAddress;
            var amount = Amount * 1_000_000L;
            var transactionExtension = await _transactionClient.CreateTransactionAsync(from, to, (long)amount);
            if (!transactionExtension.Result.Result)
            {
                Result.Message = transactionExtension.Result.Message.ToStringUtf8();
                Log.Logger.Warning($"[transfer]transfer failed, message={transactionExtension.Result.Message.ToStringUtf8()}.");
                return Result;
            }
            var transaction = transactionExtension.Transaction;
            if (!string.IsNullOrEmpty(Memo))
            {
                transaction.RawData.Data = ByteString.CopyFromUtf8(Memo);
            }
            var transactionSigned = _transactionClient.GetTransactionSign(transactionExtension.Transaction, privateKey);

            var result = await _transactionClient.BroadcastTransactionAsync(transactionSigned);
            Log.Logger.Information("[transfer]broadcast result: {@msg}", result);
            if (!result.Result)
            {
                Result.Message = result.Message.ToStringUtf8();
                Log.Logger.Warning($"[transfer]broadcast failed, message={result.Message.ToStringUtf8()}.");
                return Result;
            }
            Result.Data = transactionSigned.GetTxid();
            Result.Success = result.Result;
            return Result;
        }
    }
}
