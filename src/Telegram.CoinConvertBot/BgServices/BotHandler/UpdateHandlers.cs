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
using System.Threading;

namespace Telegram.CoinConvertBot.BgServices.BotHandler;


/*
//备忘录

1：  管理员ID: 1427768220
2：  播报群ID： -1001862069013
3：  双向用户群ID: -1002006327353
4：  收款地址： TCL7X3bbPYAY8ppCgHWResGdR8pXc38Uu6
5：  oklink 免费api修改  //API目前已暂停
6：  U兑TRX  按钮  修改 收款二维码
7：  /ucard  消费u卡 链接可修改
8：  /start 开始菜单 固定GIF链接修改
9：  群广告固定汇率手动调整
10： 波场api修改： 10609102-669a-4cf4-8c36-cc3ed97f9a30    2f9385ef-2820-4caa-9f74-e720e1a39a75    https://www.trongrid.io/dashboard   都是免费的api，随便注册即可
11： 波场官网api修改：  369e85e5-68d3-4299-a602-9d8d93ad026a   0c138945-fd9f-4390-b015-6b93368de1fd   https://tronscan.org/#/myaccount/apiKeys  都是免费的api，随便注册即可
12：  以太坊api： WR9Z9H4MRK5CP8817WF4RDAI15PGRI2WV4   DIPNHXE6J4IA1NS57ZFYRGRMSWVVCM9GXI    https://etherscan.io/apidashboard   都是免费的api，随便注册即可
13： 防盗版授权
14： 替换管理员链接： t.me/yifanfu 或 @yifanfu
15： 替换机器人链接： t.me/yifanfubot 或 @yifanfubot
16： 会员价格如有需要也可以修改
*/

//yifanfu或@yifanfu或t.me/yifanfu为管理员ID
//yifanfubot或t.me/yifanfubot或@yifanfubot为机器人ID
//TCL7X3bbPYAY8ppCgHWResGdR8pXc38Uu6为监控的收款地址
//TCL7X3bbPYAY8ppCgHWResGdR8pXc38Uu6为监控的转账地址
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
//                "4f37a8b5-870b-4a02-a33f-7dc41cb8ed8d",
//                "f49353bd-db65-4719-a56c-064b2eb231bf",
//                 "587f64a1-43d5-40f2-9115-7d3c66b0459a",
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
    private static bool isAuthorized = false; 	
    /// <summary>
    /// 错误处理
    /// </summary>
    /// <param name="botClient"></param>
    /// <param name="exception"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>

// 处理用户发送的媒体（贴图或 GIF/动画） 18.0库不支持自定义emoji表情，升级后可支持
private static async Task HandleMediaDownload(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken = default)
{
    // 检查消息是否为空
    if (message == null)
    {
        //Log.Warning("Received null message in HandleMediaDownload.");
        return;
    }

    try
    {
        // 获取媒体信息（贴图或 GIF）
        var (fileId, fileExtension, fileName, mediaType) = await GetMediaInfo(botClient, message);
        if (fileId == null || mediaType == null)
        {
            // 没有支持的媒体类型，忽略处理
            //Log.Information($"No supported media (Sticker or Animation) found in message from chat {message.Chat.Id}");
            return;
        }

        // 发送提示消息
        string promptMessage = mediaType.Contains("GIF") ? "正在为您下载GIF..." : "正在为您下载贴纸...";
        var sentMessage = await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: promptMessage,
            replyToMessageId: message.MessageId,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );

        // 生成临时文件路径
        string tempFilePath = Path.Combine(Path.GetTempPath(), fileName);

        // 下载文件到临时路径
        Telegram.Bot.Types.File file = await botClient.GetFileAsync(fileId, cancellationToken);
        await using (var fileStream = new System.IO.FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await botClient.DownloadFileAsync(file.FilePath, fileStream, cancellationToken);
        }

        // 发送文件给用户
        await using (var fileStream = new System.IO.FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            await botClient.SendDocumentAsync(
                chatId: message.Chat.Id,
                document: new InputOnlineFile(fileStream, fileName),
                replyToMessageId: message.MessageId,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
        }

        // 撤回提示消息
        await botClient.DeleteMessageAsync(
            chatId: message.Chat.Id,
            messageId: sentMessage.MessageId,
            cancellationToken: cancellationToken
        );

        // 立即删除临时文件
        System.IO.File.Delete(tempFilePath);
        //Log.Information($"Media processed. FileId: {fileId}, Type: {mediaType}, ChatId: {message.Chat.Id}");
    }
    catch (ApiRequestException ex)
    {
        //Log.Error(ex, $"Telegram API error while handling media download for chat {message.Chat.Id}");
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "处理媒体文件时发生 Telegram API 错误，请稍后重试。",
            replyToMessageId: message.MessageId,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );
    }
    catch (IOException ex)
    {
        //Log.Error(ex, $"File I/O error while handling media download for chat {message.Chat.Id}");
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "处理媒体文件时发生文件错误，请稍后重试。",
            replyToMessageId: message.MessageId,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );
    }
    catch (Exception ex)
    {
        //Log.Error(ex, $"Unexpected error while handling media download for chat {message.Chat.Id}");
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "处理媒体文件时发生未知错误，请稍后重试。",
            replyToMessageId: message.MessageId,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );
    }
}

// 获取媒体信息（贴图或 GIF）
private static async Task<(string? FileId, string? FileExtension, string? FileName, string? MediaType)> GetMediaInfo(ITelegramBotClient botClient, Message message)
{
    // 检查贴图
    if (message.Sticker != null)
    {
        var sticker = message.Sticker;
        string fileExtension = GetStickerFileExtension(sticker);
        string mediaType = sticker.IsAnimated ? "Animated Sticker (TGS)" :
                          sticker.IsVideo ? "Video Sticker (WebM)" : "Static Sticker (WebP)";
        return (sticker.FileId, fileExtension, $"{sticker.FileId}{fileExtension}", mediaType);
    }

    // 检查 GIF/动画
    if (message.Animation != null)
    {
        var animation = message.Animation;
        string fileExtension = animation.MimeType == "image/gif" ? ".gif" : ".mp4";
        return (animation.FileId, fileExtension, $"{animation.FileId}{fileExtension}", "GIF/Animation");
    }

    // 不支持其他类型（例如文本中的自定义 Emoji）
    return (null, null, null, null);
}

// 获取贴图文件扩展名
private static string GetStickerFileExtension(Sticker sticker)
{
    if (sticker.IsAnimated) return ".tgs"; // 动画贴图（TGS 格式）
    if (sticker.IsVideo) return ".webm"; // 视频贴图（WebM 格式）
    return ".webp"; // 静态贴图（WebP 格式）
}
	
//群发消息代码
public static class BroadcastHelper
{
    public static async Task BroadcastMessageAsync(ITelegramBotClient botClient, Message message, List<User> Followers, object _followersLock, string photoFileId = null)
    {
        // 确保消息来自指定管理员且以“群发 ”开头
        if (message.From.Id != 1427768220 || string.IsNullOrEmpty(message.Text) || !message.Text.StartsWith("群发 "))
        {
            return;
        }

        // 去掉“群发 ”前缀，获取原始消息内容
        var prefixLength = "群发 ".Length; // 前缀长度
        var originalMessage = message.Text.Substring(prefixLength).Trim();
        var messageToSend = originalMessage;

        // 处理按钮
        var buttonPattern = @"[\(\（]按钮，(.*?)[，,](.*?)[\)\）]";
        var buttonMatches = Regex.Matches(messageToSend, buttonPattern);
        List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();

        foreach (Match match in buttonMatches)
        {
            var buttonText = match.Groups[1].Value.Trim();
            var buttonAction = match.Groups[2].Value.Trim();
            InlineKeyboardButton button;

            // 判断按钮动作是 URL 还是回调数据
            if (buttonAction.Contains(".") || Uri.IsWellFormedUriString(buttonAction, UriKind.Absolute))
            {
                // 确保 URL 以 http:// 或 https:// 开头
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

        // 从消息中移除按钮标记
        messageToSend = Regex.Replace(messageToSend, buttonPattern, "").Trim();

        // 调试：记录原始消息和按钮
        //Log.Information($"Original message: {originalMessage}");
        //Log.Information($"Message after button removal: {messageToSend}");
        //Log.Information($"Buttons found: {buttons.Count}");

        // 处理消息中的格式（加粗、斜体、链接等）
        if (message.Entities != null && message.Entities.Any())
        {
            // 调试：记录实体信息
            //foreach (var entity in message.Entities)
            //{
            //    Log.Information($"Entity: Type={entity.Type}, Offset={entity.Offset}, Length={entity.Length}, Url={entity.Url}");
            //}

            // 过滤与“群发 ”前缀无关的实体
            var relevantEntities = message.Entities
                .Where(e => e.Offset >= prefixLength)
                .Select(e => new MessageEntity
                {
                    Type = e.Type,
                    Offset = e.Offset - prefixLength, // 调整偏移量
                    Length = e.Length,
                    Url = e.Url
                })
                .ToArray();

            if (relevantEntities.Any())
            {
                messageToSend = ConvertEntitiesToHtml(messageToSend, relevantEntities);
            }

            // 调试：记录转换后的消息
            //Log.Information($"Message after entity conversion: {messageToSend}");
        }

        // 如果消息为空，添加默认文本以避免发送空消息
        if (string.IsNullOrWhiteSpace(messageToSend))
        {
            messageToSend = " ";
            //Log.Information("Message was empty, set to single space.");
        }

        // 创建内联键盘
        InlineKeyboardMarkup inlineKeyboard = null;
        if (buttons.Count > 0)
        {
            inlineKeyboard = new InlineKeyboardMarkup(buttons.Select(b => new[] { b }).ToArray());
        }

        // 分批发送消息
        int total = 0, success = 0, fail = 0;
        int batchSize = 200;
        Random random = new Random();
        var failedUsers = new List<User>();
        List<User> currentBatch;

        try
        {
            // 获取所有需要发送的用户并移除机器人用户
            lock (_followersLock)
            {
                var botUsers = Followers.Where(u => u.IsBot).ToList();
                foreach (var botUser in botUsers)
                {
                    Followers.RemoveAll(u => u.Id == botUser.Id);
                }
                currentBatch = Followers.ToList();
            }

            // 分批处理用户
            for (int i = 0; i < currentBatch.Count; i += batchSize)
            {
                var batch = currentBatch.Skip(i).Take(batchSize).ToList();
                foreach (var follower in batch)
                {
                    total++;
                    try
                    {
                        if (photoFileId != null)
                        {
                            // 发送图片消息，确保使用 HTML 格式解析 caption
                            await botClient.SendPhotoAsync(
                                chatId: follower.Id,
                                photo: photoFileId,
                                caption: messageToSend,
                                parseMode: ParseMode.Html, // 明确指定 HTML 格式
                                replyMarkup: inlineKeyboard);
                        }
                        else
                        {
                            // 发送文本消息
                            await botClient.SendTextMessageAsync(
                                chatId: follower.Id,
                                text: messageToSend,
                                parseMode: ParseMode.Html,
                                disableWebPagePreview: true,
                                replyMarkup: inlineKeyboard);
                        }
                        success++;
                    }
                    catch (ApiRequestException e)
                    {
                        Log.Error($"Failed to send message to {follower.Id}: {e.Message}");
                        fail++;

                        if (e.Message.Contains("bot can't send messages to bots") ||
                            e.Message.Contains("bot was blocked by the user") ||
                            e.Message.Contains("user is deactivated") ||
                            e.Message.Contains("chat not found") ||
                            e.Message.Contains("bot can't initiate conversation with a user"))
                        {
                            failedUsers.Add(follower);
                        }
                    }
                }

                await Task.Delay(random.Next(1000, 2001));
            }

            // 统一移除失败的用户
            if (failedUsers.Count > 0)
            {
                lock (_followersLock)
                {
                    foreach (var failedUser in failedUsers)
                    {
                        Followers.RemoveAll(u => u.Id == failedUser.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"An error occurred, stopping broadcast: {ex.Message}");
        }

        // 发送统计信息给管理员
        await botClient.SendTextMessageAsync(
            chatId: message.From.Id,
            text: $"群发总数：<b>{total}</b>   成功：<b>{success}</b>  失败：<b>{fail}</b>",
            parseMode: ParseMode.Html,
            disableWebPagePreview: true);
    }

    private static string ConvertEntitiesToHtml(string text, MessageEntity[] entities)
    {
        // 如果没有实体，返回原始文本
        if (entities == null || !entities.Any())
        {
            //Log.Information("No entities found, returning original text.");
            return text;
        }

        // 按偏移量和长度排序，确保正确处理嵌套实体
        var sortedEntities = entities
            .OrderBy(e => e.Offset)
            .ThenByDescending(e => e.Length) // 较长的实体（外层）优先
            .ToList();

        var result = new StringBuilder(text);
        int offsetAdjustment = 0; // 跟踪因插入标签导致的偏移变化

        foreach (var entity in sortedEntities)
        {
            // 确保偏移量和长度有效
            if (entity.Offset + entity.Length > text.Length || entity.Offset < 0)
            {
                //Log.Warning($"Invalid entity: Type={entity.Type}, Offset={entity.Offset}, Length={entity.Length}, TextLength={text.Length}");
                continue;
            }

            string entityText = text.Substring(entity.Offset, entity.Length);
            string openTag = string.Empty, closeTag = string.Empty;

            switch (entity.Type)
            {
                case MessageEntityType.Bold:
                    openTag = "<b>";
                    closeTag = "</b>";
                    break;
                case MessageEntityType.Italic:
                    openTag = "<i>";
                    closeTag = "</i>";
                    break;
                case MessageEntityType.TextLink:
                    if (Uri.IsWellFormedUriString(entity.Url, UriKind.Absolute))
                    {
                        openTag = $"<a href='{entity.Url}'>";
                        closeTag = "</a>";
                    }
                    else
                    {
                        //Log.Warning($"Invalid URL in TextLink entity: {entity.Url}");
                        continue;
                    }
                    break;
                case MessageEntityType.Underline:
                    openTag = "<u>";
                    closeTag = "</u>";
                    break;
                case MessageEntityType.Strikethrough:
                    openTag = "<s>";
                    closeTag = "</s>";
                    break;
                case MessageEntityType.Code:
                    openTag = "<code>";
                    closeTag = "</code>";
                    break;
                case MessageEntityType.Pre:
                    openTag = "<pre>";
                    closeTag = "</pre>";
                    break;
                case MessageEntityType.Spoiler: // 添加对防剧透格式的支持
                    openTag = "<span class=\"tg-spoiler\">";
                    closeTag = "</span>";
                    break;
                default:
                    //Log.Information($"Unsupported entity type: {entity.Type}");
                    continue;
            }

            // 插入结束标签（从后向前）
            result.Insert(entity.Offset + entity.Length + offsetAdjustment, closeTag);
            // 插入开始标签
            result.Insert(entity.Offset + offsetAdjustment, openTag);

            // 更新偏移调整量
            offsetAdjustment += openTag.Length + closeTag.Length;

            //Log.Information($"Applied entity: Type={entity.Type}, Offset={entity.Offset}, Length={entity.Length}, Text={entityText}, Result={result}");
        }

        return result.ToString();
    }
}
// 新增一个类来管理价格涨跌计算的黑名单
public static class PriceCalculationBlacklistManager
{
    private static readonly HashSet<long> _blacklistedIds = new();
    private static readonly object _lock = new();

    // 检查是否在黑名单中
    public static bool IsBlacklisted(long id)
    {
        lock (_lock)
        {
            return _blacklistedIds.Contains(id);
        }
    }

    // 添加到黑名单
    public static bool AddToBlacklist(long id)
    {
        lock (_lock)
        {
            try
            {
                return _blacklistedIds.Add(id); // 返回 true 表示添加成功，false 表示已存在
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"添加黑名单失败（ID: {id}）：{ex.Message}");
                return false;
            }
        }
    }

    // 从黑名单移除
    public static bool RemoveFromBlacklist(long id)
    {
        lock (_lock)
        {
            try
            {
                return _blacklistedIds.Remove(id); // 返回 true 表示移除成功，false 表示不存在
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"移除黑名单失败（ID: {id}）：{ex.Message}");
                return false;
            }
        }
    }
}
// 查询币安智能链余额
public static class BscQuery
{
    private static readonly string[] BscRpcUrls = new[]
    {
        "https://bsc-dataseed.binance.org/",
        "https://bsc-dataseed1.defibit.io/",
        "https://bsc-dataseed1.ninicoin.io/"
    };
    private static readonly string UsdtContractAddress = "0x55d398326f99059ff775485246999027b3197955"; // BSC USDT
    private static readonly string UsdcContractAddress = "0x8ac76a51cc950d9822d68b83fe1ad97b32cd580d"; // BSC USDC
    private static readonly decimal WeiToToken = 1_000_000_000_000_000_000m; // 10^18
    private static readonly HttpClient HttpClient; // 静态 HttpClient

    static BscQuery()
    {
        var handler = new HttpClientHandler { MaxConnectionsPerServer = 50 };
        HttpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(3) };
        foreach (var url in BscRpcUrls)
        {
            ServicePointManager.FindServicePoint(new Uri(url)).ConnectionLeaseTimeout = 60 * 1000; // DNS 刷新
        }
    }

    public static async Task<(decimal bnbBalance, decimal usdtBalance, decimal usdcBalance, decimal cnyUsdtBalance, decimal cnyUsdcBalance, bool isError)> QueryBscAddressAsync(string address)
    {
        try
        {
            // 并行查询余额和价格
            var bnbBalanceTask = GetBnbBalanceAsync(address);
            var usdtBalanceTask = GetErc20BalanceAsync(address, UsdtContractAddress);
            var usdcBalanceTask = GetErc20BalanceAsync(address, UsdcContractAddress);
            var okxPriceTask = GetOkxPriceAsync("usdt", "cny", "alipay");

            await Task.WhenAll(bnbBalanceTask, usdtBalanceTask, usdcBalanceTask, okxPriceTask);

            // 获取结果
            var bnbBalance = (await bnbBalanceTask) / WeiToToken; // 转换为 BNB
            var usdtBalance = (await usdtBalanceTask) / WeiToToken; // 转换为 USDT
            var usdcBalance = (await usdcBalanceTask) / WeiToToken; // 转换为 USDC
            decimal okxPrice = await okxPriceTask;

            if (okxPrice == 0)
            {
                Console.WriteLine("无法获取 OKX USDT/CNY 价格");
                return (0m, 0m, 0m, 0m, 0m, true);
            }

            // 计算人民币余额
            decimal cnyUsdtBalance = usdtBalance * okxPrice;
            decimal cnyUsdcBalance = usdcBalance * okxPrice; // 假设 USDC 价格与 USDT 相同

            // 验证余额非负
            if (bnbBalance < 0 || usdtBalance < 0 || usdcBalance < 0)
            {
                //Console.WriteLine($"BSC 余额解析错误：BNB={bnbBalance}, USDT={usdtBalance}, USDC={usdcBalance}");
                return (0m, 0m, 0m, 0m, 0m, true);
            }

            return (bnbBalance, usdtBalance, usdcBalance, cnyUsdtBalance, cnyUsdcBalance, false);
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"BSC 地址查询异常：{ex.Message}");
            return (0m, 0m, 0m, 0m, 0m, true);
        }
    }

    private static async Task<decimal> GetBnbBalanceAsync(string address)
    {
        var request = new
        {
            jsonrpc = "2.0",
            method = "eth_getBalance",
            @params = new object[] { address, "latest" },
            id = 1
        };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // 并行请求所有节点
        var tasks = BscRpcUrls.Select(url => HttpClient.PostAsync(url, content)).ToList();
        var firstResponse = await Task.WhenAny(tasks);
        var response = await firstResponse;
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(json);

        var result = jsonDoc.RootElement.GetProperty("result").GetString();
        if (string.IsNullOrEmpty(result))
        {
            throw new Exception("BNB 余额查询失败：无效的响应结果");
        }

        // 解析十六进制为 BigInteger
        var balanceWei = BigInteger.Parse("0" + result.Substring(2), System.Globalization.NumberStyles.HexNumber);
        return (decimal)balanceWei; // 返回 Wei 单位
    }

    private static async Task<decimal> GetErc20BalanceAsync(string address, string contractAddress)
    {
        // balanceOf 方法签名：0x70a08231 + 补齐的地址（去掉 0x，填充到 64 位）
        var paddedAddress = address.Substring(2).PadLeft(64, '0');
        var data = $"0x70a08231{paddedAddress}";

        var request = new
        {
            jsonrpc = "2.0",
            method = "eth_call",
            @params = new object[]
            {
                new
                {
                    to = contractAddress,
                    data = data
                },
                "latest"
            },
            id = 1
        };

        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // 并行请求所有节点
        var tasks = BscRpcUrls.Select(url => HttpClient.PostAsync(url, content)).ToList();
        var firstResponse = await Task.WhenAny(tasks);
        var response = await firstResponse;
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(json);

        var result = jsonDoc.RootElement.GetProperty("result").GetString();
        if (string.IsNullOrEmpty(result))
        {
            throw new Exception("ERC-20 余额查询失败：无效的响应结果");
        }

        // 解析十六进制为 BigInteger
        var balanceWei = BigInteger.Parse("0" + result.Substring(2), System.Globalization.NumberStyles.HexNumber);
        return (decimal)balanceWei; // 返回 Wei 单位
    }
}

// 查询以太坊主网代币信息
public static class EthereumQuery
{
    private static readonly string EtherscanBaseUrl = "https://api.etherscan.io/api";
    private static readonly string[] EtherscanApiKeys = new[]
    {
        "WR9Z9H4MRK5CP8817WF4RDAI15PGRI2WV4",
        "DIPNHXE6J4IA1NS57ZFYRGRMSWVVCM9GXI"
    };
    private static readonly string UsdtContractAddress = "0xdac17f958d2ee523a2206206994597c13d831ec7";
    private static readonly string UsdcContractAddress = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48";
    private static readonly Random _random = new Random();
    private static readonly HttpClient HttpClient;
    private static readonly Dictionary<string, (int SuccessCount, int FailureCount)> _apiKeyStats = new();

    static EthereumQuery()
    {
        var handler = new HttpClientHandler { MaxConnectionsPerServer = 50 };
        HttpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(3) };
        ServicePointManager.FindServicePoint(new Uri(EtherscanBaseUrl)).ConnectionLeaseTimeout = 60 * 1000; // DNS 刷新
    }

    // 辅助方法：选择成功率高的 API 密钥
    private static string GetRandomApiKey()
    {
        lock (_apiKeyStats)
        {
            var availableKeys = EtherscanApiKeys
                .OrderByDescending(k => _apiKeyStats.TryGetValue(k, out var stats) ? stats.SuccessCount / (double)(stats.SuccessCount + stats.FailureCount + 1) : 1)
                .ToList();
            return availableKeys[_random.Next(availableKeys.Count)];
        }
    }

public static async Task<(decimal ethBalance, decimal usdtBalance, decimal usdcBalance, decimal cnyUsdtBalance, decimal cnyUsdcBalance, decimal gasPriceGwei, decimal gasPriceUsd, DateTime? lastTxTime, bool isError)> QueryEthAddressAsync(string address)
{
    const int maxRetries = 2;
    const int retryDelaySeconds = 2; // 优化：重试间隔从 5 秒减至 2 秒
    int attempt = 0;

    while (attempt <= maxRetries)
    {
        try
        {
            // 为每次查询选择一个高成功率的 API 密钥
            string apiKey = GetRandomApiKey();
            //Console.WriteLine($"使用 API 密钥：{apiKey.Substring(0, 6)}... 进行第 {attempt + 1} 次查询");

            // 启动所有查询任务
            var ethBalanceTask = GetEthBalanceAsync(address, apiKey);
            var usdtBalanceTask = GetErc20BalanceAsync(address, UsdtContractAddress, apiKey);
            var usdcBalanceTask = GetErc20BalanceAsync(address, UsdcContractAddress, apiKey);
            var gasPriceTask = GetGasPriceAsync(apiKey);
            var okxPriceTask = GetOkxPriceAsync("usdt", "cny", "alipay");
            var lastTxTimeTask = GetLastTransactionTimeAsync(address, apiKey);

            // 等待所有任务完成
            await Task.WhenAll(ethBalanceTask, usdtBalanceTask, usdcBalanceTask, gasPriceTask, okxPriceTask, lastTxTimeTask);

            // 获取结果
            var (ethBalance, isErrorEth, ethErrorMessage) = ethBalanceTask.Result;
            var (usdtBalance, isErrorUsdt, usdtErrorMessage) = usdtBalanceTask.Result;
            var (usdcBalance, isErrorUsdc, usdcErrorMessage) = usdcBalanceTask.Result;
            var (gasPriceGwei, gasPriceUsd, isErrorGas, gasErrorMessage) = gasPriceTask.Result;
            var (lastTxTime, isErrorTxTime, txErrorMessage) = lastTxTimeTask.Result;
            decimal okxPrice = okxPriceTask.Result;

            // 检查是否有错误或价格为 0（不检查 isErrorTxTime）
            if (isErrorEth || isErrorUsdt || isErrorUsdc || isErrorGas || okxPrice == 0)
            {
                // 检查是否是 API 限流错误
                bool isRateLimitError = await CheckRateLimitError(ethBalanceTask, usdtBalanceTask, usdcBalanceTask, gasPriceTask, lastTxTimeTask);
                if (isRateLimitError && attempt < maxRetries)
                {
                    lock (_apiKeyStats)
                    {
                        var stats = _apiKeyStats.TryGetValue(apiKey, out var s) ? s : (SuccessCount: 0, FailureCount: 0);
                        _apiKeyStats[apiKey] = (stats.SuccessCount, stats.FailureCount + 1); // 修复：显式命名元组
                    }
                    Console.WriteLine($"检测到 Etherscan API 限流，第 {attempt + 1} 次重试，等待 {retryDelaySeconds} 秒...");
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                    attempt++;
                    continue;
                }

                //Console.WriteLine($"以太坊地址查询失败：ETH={isErrorEth} ({ethErrorMessage ?? "无错误消息"}), USDT={isErrorUsdt} ({usdtErrorMessage ?? "无错误消息"}), USDC={isErrorUsdc} ({usdcErrorMessage ?? "无错误消息"}), Gas={isErrorGas} ({gasErrorMessage ?? "无错误消息"}), LastTx={isErrorTxTime} ({txErrorMessage ?? "无错误消息"}), OKX Price={okxPrice}");
                return (0m, 0m, 0m, 0m, 0m, 0m, 0m, null, true);
            }

            lock (_apiKeyStats)
            {
                var stats = _apiKeyStats.TryGetValue(apiKey, out var s) ? s : (SuccessCount: 0, FailureCount: 0);
                _apiKeyStats[apiKey] = (stats.SuccessCount + 1, stats.FailureCount); // 修复：显式命名元组
            }

            // 计算人民币余额
            decimal cnyUsdtBalance = usdtBalance * okxPrice;
            decimal cnyUsdcBalance = usdcBalance * okxPrice; // 假设 USDC 价格与 USDT 相同

            // 即使交易时间查询失败（isErrorTxTime = true），仍返回余额
            return (ethBalance, usdtBalance, usdcBalance, cnyUsdtBalance, cnyUsdcBalance, gasPriceGwei, gasPriceUsd, lastTxTime, false);
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"以太坊地址查询异常：{ex.Message}");
            return (0m, 0m, 0m, 0m, 0m, 0m, 0m, null, true);
        }
    }

    Console.WriteLine($"以太坊地址查询失败：达到最大重试次数 ({maxRetries})，可能是 API 限流");
    return (0m, 0m, 0m, 0m, 0m, 0m, 0m, null, true);
}

    private static async Task<(decimal balance, bool isError, string errorMessage)> GetEthBalanceAsync(string address, string apiKey)
    {
        try
        {
            var url = $"{EtherscanBaseUrl}?module=account&action=balance&address={address}&tag=latest&apikey={apiKey}";
            var response = await HttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(json);
            var status = jsonDoc.RootElement.GetProperty("status").GetString();
            if (status != "1")
            {
                var errorMessage = jsonDoc.RootElement.GetProperty("message").GetString() + ": " + jsonDoc.RootElement.GetProperty("result").GetString();
                //Console.WriteLine($"ETH 余额查询失败：{errorMessage}");
                return (0m, true, errorMessage);
            }

            var balanceWei = jsonDoc.RootElement.GetProperty("result").GetString();
            var balanceEth = decimal.Parse(balanceWei) / 1_000_000_000_000_000_000m; // 转换为 ETH (18 位小数)
            return (balanceEth, false, null);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            //Console.WriteLine($"查询 ETH 余额限流：HTTP 429，API 密钥：{apiKey.Substring(0, 6)}...");
            return (0m, true, "HTTP 429: Too Many Requests");
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"查询 ETH 余额异常：{ex.Message}");
            return (0m, true, ex.Message);
        }
    }

    private static async Task<(decimal balance, bool isError, string errorMessage)> GetErc20BalanceAsync(string address, string contractAddress, string apiKey)
    {
        try
        {
            var url = $"{EtherscanBaseUrl}?module=account&action=tokenbalance&contractaddress={contractAddress}&address={address}&tag=latest&apikey={apiKey}";
            var response = await HttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(json);
            var status = jsonDoc.RootElement.GetProperty("status").GetString();
            if (status != "1")
            {
                var errorMessage = jsonDoc.RootElement.GetProperty("message").GetString() + ": " + jsonDoc.RootElement.GetProperty("result").GetString();
                Console.WriteLine($"ERC-20 余额查询失败：{errorMessage}");
                return (0m, true, errorMessage);
            }

            var balanceWei = jsonDoc.RootElement.GetProperty("result").GetString();
            var balance = decimal.Parse(balanceWei) / 1_000_000m; // USDT 和 USDC 均为 6 位小数
            return (balance, false, null);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            //Console.WriteLine($"查询 ERC-20 余额限流：HTTP 429，API 密钥：{apiKey.Substring(0, 6)}...");
            return (0m, true, "HTTP 429: Too Many Requests");
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"查询 ERC-20 余额异常：{ex.Message}");
            return (0m, true, ex.Message);
        }
    }

    private static async Task<(DateTime? lastTxTime, bool isError, string errorMessage)> GetLastTransactionTimeAsync(string address, string apiKey)
    {
        try
        {
            var url = $"{EtherscanBaseUrl}?module=account&action=txlist&address={address}&page=1&offset=1&sort=desc&apikey={apiKey}";
            var response = await HttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(json);
            var status = jsonDoc.RootElement.GetProperty("status").GetString();
            var resultElement = jsonDoc.RootElement.GetProperty("result");

            if (status != "1")
            {
                var errorMessage = jsonDoc.RootElement.GetProperty("message").GetString() + (resultElement.ValueKind == JsonValueKind.String ? ": " + resultElement.GetString() : "");
                //Console.WriteLine($"最新交易时间查询失败：{errorMessage}");
                return (null, false, errorMessage); // API 错误，返回 false 不影响余额查询
            }

            if (resultElement.ValueKind != JsonValueKind.Array)
            {
                //Console.WriteLine($"最新交易时间查询失败：result 不是数组，类型为 {resultElement.ValueKind}");
                return (null, false, $"Unexpected result type: {resultElement.ValueKind}");
            }

            var resultArray = resultElement.EnumerateArray();
            if (!resultArray.Any())
            {
                //Console.WriteLine($"地址 {address} 无交易记录");
                return (null, false, null); // 无交易记录，返回 null 且 isError = false
            }

            var timeStamp = resultArray.First().GetProperty("timeStamp").GetString();
            var unixTime = long.Parse(timeStamp);
            var utcTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
            var beijingTime = utcTime.AddHours(8); // 转换为北京时间 (UTC+8)
            return (beijingTime, false, null);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            //Console.WriteLine($"查询最新交易时间限流：HTTP 429，API 密钥：{apiKey.Substring(0, 6)}...");
            return (null, true, "HTTP 429: Too Many Requests"); // 限流错误，触发重试
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"查询最新交易时间异常：{ex.Message}");
            return (null, false, ex.Message); // 其他异常，返回 false 不影响余额查询
        }
    }

    private static async Task<(decimal gasPriceGwei, decimal gasPriceUsd, bool isError, string errorMessage)> GetGasPriceAsync(string apiKey)
    {
        try
        {
            // 查询 Gas 价格
            var gasUrl = $"{EtherscanBaseUrl}?module=gastracker&action=gasoracle&apikey={apiKey}";
            var gasResponse = await HttpClient.GetAsync(gasUrl);
            gasResponse.EnsureSuccessStatusCode();

            var gasJson = await gasResponse.Content.ReadAsStringAsync();
            var gasJsonDoc = JsonDocument.Parse(gasJson);
            var gasStatus = gasJsonDoc.RootElement.GetProperty("status").GetString();
            if (gasStatus != "1")
            {
                var errorMessage = gasJsonDoc.RootElement.GetProperty("message").GetString() + ": " + gasJsonDoc.RootElement.GetProperty("result").GetString();
                //Console.WriteLine($"Gas 价格查询失败：{errorMessage}");
                return (0m, 0m, true, errorMessage);
            }

            var gasPriceGwei = decimal.Parse(gasJsonDoc.RootElement.GetProperty("result").GetProperty("ProposeGasPrice").GetString()); // 使用 Average/ProposeGasPrice，单位为 Gwei

            // 从 CoinDataCache 获取 ETH 美元价格
            string[] possibleEthSymbols = new[] { "ETH", "ETHUSDT", "ETH/USDT" };
            decimal ethPriceUsd = 0m;
            foreach (var symbol in possibleEthSymbols)
            {
                var coinInfo = await CoinDataCache.GetCoinInfoAsync(symbol);
                if (coinInfo != null && coinInfo.TryGetValue("price_usd", out JsonElement priceElement) && priceElement.TryGetDouble(out double price))
                {
                    ethPriceUsd = (decimal)price;
                    break;
                }
            }

            if (ethPriceUsd == 0m)
            {
                var errorMessage = "无法从 CoinDataCache 获取 ETH 美元价格";
                //Console.WriteLine(errorMessage);
                return (gasPriceGwei, 0m, true, errorMessage);
            }

            // 计算标准交易（21,000 Gas）的美元成本：Gas Price (Gwei) * 21,000 * ETH Price (USD/ETH) / 1,000,000,000
            var gasPriceUsd = gasPriceGwei * 21000 * ethPriceUsd / 1_000_000_000m;

            return (gasPriceGwei, gasPriceUsd, false, null);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            //Console.WriteLine($"查询 Gas 价格限流：HTTP 429，API 密钥：{apiKey.Substring(0, 6)}...");
            return (0m, 0m, true, "HTTP 429: Too Many Requests");
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"查询 Gas 价格异常：{ex.Message}");
            return (0m, 0m, true, ex.Message);
        }
    }

    private static async Task<bool> CheckRateLimitError(
        Task<(decimal balance, bool isError, string errorMessage)> ethTask,
        Task<(decimal balance, bool isError, string errorMessage)> usdtTask,
        Task<(decimal balance, bool isError, string errorMessage)> usdcTask,
        Task<(decimal gasPriceGwei, decimal gasPriceUsd, bool isError, string errorMessage)> gasPriceTask,
        Task<(DateTime? lastTxTime, bool isError, string errorMessage)> lastTxTimeTask)
    {
        try
        {
            // 检查 HTTP 429 错误
            if (ethTask.IsFaulted && ethTask.Exception?.InnerException is HttpRequestException ethEx && ethEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return true;
            }
            if (usdtTask.IsFaulted && usdtTask.Exception?.InnerException is HttpRequestException usdtEx && usdtEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return true;
            }
            if (usdcTask.IsFaulted && usdcTask.Exception?.InnerException is HttpRequestException usdcEx && usdcEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return true;
            }
            if (gasPriceTask.IsFaulted && gasPriceTask.Exception?.InnerException is HttpRequestException gasEx && gasEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return true;
            }
            if (lastTxTimeTask.IsFaulted && lastTxTimeTask.Exception?.InnerException is HttpRequestException txEx && txEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return true;
            }

            // 检查 JSON 响应中的限流消息
            Task[] tasks = new Task[] { ethTask, usdtTask, usdcTask, gasPriceTask, lastTxTimeTask };
            foreach (var task in tasks)
            {
                string errorMessage = task switch
                {
                    Task<(decimal, bool, string)> balanceTask => balanceTask.Result.Item3,
                    Task<(decimal, decimal, bool, string)> gasTask => gasTask.Result.Item4,
                    Task<(DateTime?, bool, string)> txTask => txTask.Result.Item3,
                    _ => null
                };

                if (errorMessage != null &&
                    (errorMessage.ToLower().Contains("max rate limit reached") ||
                     errorMessage.ToLower().Contains("maximum rate limit reached") ||
                     errorMessage.ToLower().Contains("rate limit of 1/5sec applied")))
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"检查限流错误时异常：{ex.Message}");
        }
        return false;
    }
}
// 新增一个类来管理查询冷却
public static class QueryCooldownManager
{
    private static readonly Dictionary<long, (DateTime LastQueryTime, int? MessageId, CancellationTokenSource Cts)> _userCooldowns = new();
    private static readonly TimeSpan CooldownPeriod = TimeSpan.FromSeconds(5);
    private const long AdminUserId = 1427768220; // 管理员 ID

    // 检查用户是否在冷却期，并返回剩余秒数（管理员免冷却）
    public static (bool IsInCooldown, double RemainingSeconds) CheckCooldown(long userId)
    {
        if (userId == AdminUserId)
        {
            return (false, 0); // 管理员不触发冷却
        }

        if (_userCooldowns.TryGetValue(userId, out var cooldownInfo))
        {
            var timeSinceLastQuery = DateTime.UtcNow - cooldownInfo.LastQueryTime;
            var remainingSeconds = CooldownPeriod.TotalSeconds - timeSinceLastQuery.TotalSeconds;
            if (remainingSeconds > 0)
            {
                return (true, remainingSeconds);
            }
            // 冷却期已结束，移除记录
            _userCooldowns.Remove(userId);
        }
        return (false, 0);
    }

    // 记录查询时间（首次查询调用）
    public static void RecordQueryTime(long userId)
    {
        if (userId == AdminUserId)
        {
            return; // 管理员不记录查询时间
        }

        var cts = new CancellationTokenSource();
        _userCooldowns[userId] = (DateTime.UtcNow, null, cts);
    }

    // 开始冷却并处理倒计时消息（第二次查询调用，非管理员）
    public static async Task StartCooldownAsync(ITelegramBotClient botClient, long chatId, long userId)
    {
        if (userId == AdminUserId)
        {
            return; // 管理员不触发冷却提示
        }

        // 如果已有倒计时消息，直接返回
        if (_userCooldowns.TryGetValue(userId, out var existingCooldown) && existingCooldown.MessageId.HasValue)
        {
            return;
        }

        // 创建新的 CancellationTokenSource
        var cts = new CancellationTokenSource();
        try
        {
            // 发送初始倒计时消息
            Telegram.Bot.Types.Message? message = null;
            try
            {
                message = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "操作频繁，请 5 秒后重试！",
                    parseMode: ParseMode.Html,
                    cancellationToken: cts.Token
                );
            }
            catch (Exception ex)
            {
               // Console.WriteLine($"发送冷却提示消息失败：{ex.Message}");
                return; // 发送失败，直接退出
            }

            // 更新记录，包含消息ID
            _userCooldowns[userId] = (DateTime.UtcNow, message.MessageId, cts);

            // 倒计时逻辑
            for (int seconds = 4; seconds >= 1; seconds--)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
                if (cts.Token.IsCancellationRequested) return;

                try
                {
                    await botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: message.MessageId,
                        text: $"操作频繁，请 {seconds} 秒后重试！",
                        parseMode: ParseMode.Html,
                        cancellationToken: cts.Token
                    );
                }
                catch (Exception ex)
                {
                   // Console.WriteLine($"编辑冷却提示消息失败（剩余 {seconds} 秒）：{ex.Message}");
                    return; // 编辑失败，退出倒计时
                }
            }

            // 最后一秒后撤回消息
            await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
            if (cts.Token.IsCancellationRequested) return;

            try
            {
                await botClient.DeleteMessageAsync(
                    chatId: chatId,
                    messageId: message.MessageId,
                    cancellationToken: cts.Token
                );
            }
            catch (Exception ex)
            {
               // Console.WriteLine($"撤回冷却提示消息失败：{ex.Message}");
                // 撤回失败，仅记录日志，继续清理
            }
        }
        catch (OperationCanceledException)
        {
            // 取消操作，不记录错误
        }
        catch (Exception ex)
        {
           // Console.WriteLine($"倒计时消息处理异常：{ex.Message}");
        }
        finally
        {
            // 清理记录
            if (_userCooldowns.ContainsKey(userId))
            {
                _userCooldowns.Remove(userId);
            }
            cts.Dispose();
        }
    }

    // 取消现有的倒计时（管理员无需取消）
    public static void CancelCooldown(long userId)
    {
        if (userId == AdminUserId)
        {
            return; // 管理员不触发冷却
        }

        if (_userCooldowns.TryGetValue(userId, out var cooldownInfo))
        {
            cooldownInfo.Cts?.Cancel();
            _userCooldowns.Remove(userId);
        }
    }
}
//主方法查询eth bsc并返回
public static async Task HandleEthQueryAsync(ITelegramBotClient botClient, Message message)
{
    var chatId = message.Chat.Id;
    var userId = message.From?.Id ?? 0;
    var ethAddress = message.Text;

    // 检查冷却状态
    var (isInCooldown, remainingSeconds) = QueryCooldownManager.CheckCooldown(userId);
    if (isInCooldown)
    {
        await QueryCooldownManager.StartCooldownAsync(botClient, chatId, userId);
        return;
    }

    // 检查是否为 USDT 或 USDC 智能合约地址
    string warningText = null;
    if (ethAddress.Equals("0xdAC17F958D2ee523a2206206994597C13D831ec7", StringComparison.OrdinalIgnoreCase))
    {
        warningText = "此地址为 Tether（泰达）公司在以太坊网络的 <b>USDT</b> 智能合约地址！\n" +
                      "该地址非用户地址，向该地址转账任意币种将造成资金永久丢失！";
    }
    else if (ethAddress.Equals("0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48", StringComparison.OrdinalIgnoreCase))
    {
        warningText = "此地址为 Circle 公司在以太坊网络的 <b>USDC</b> 智能合约地址！\n" +
                      "该地址非用户地址，向该地址转账任意币种将造成资金永久丢失！";
    }
    else if (ethAddress.Equals("0x55d398326f99059fF775485246999027B3197955", StringComparison.OrdinalIgnoreCase))
    {
        warningText = "此地址为 Tether（泰达）公司在 BNB Smart Chain 网络的 <b>USDT</b> 智能合约地址！\n" +
                      "该地址非用户地址，向该地址转账任意币种将造成资金永久丢失！";
    }
    else if (ethAddress.Equals("0x8AC76a51cc950d9822D68b83fE1Ad97B32Cd580d", StringComparison.OrdinalIgnoreCase))
    {
        warningText = "此地址为 Circle 公司在 BNB Smart Chain 网络的 <b>USDC</b> 智能合约地址！\n" +
                      "该地址非用户地址，向该地址转账任意币种将造成资金永久丢失！";
    }
    else if (ethAddress.Equals("0xc2132D05D31c914a87C6611C10748AEb04B58e8F", StringComparison.OrdinalIgnoreCase))
    {
        warningText = "此地址为 Tether（泰达）公司在 Polygon 网络的 <b>USDT</b> 智能合约地址！\n" +
                      "该地址非用户地址，向该地址转账任意币种将造成资金永久丢失！";
    }
    else if (ethAddress.Equals("0x9702230A8Ea53601f5cD2dc00fDBc13d4dF4A8c7", StringComparison.OrdinalIgnoreCase))
    {
        warningText = "此地址为 Tether（泰达）公司在 Avalanche C-Chain 网络的 <b>USDT</b> 智能合约地址！\n" +
                      "该地址非用户地址，向该地址转账任意币种将造成资金永久丢失！";
    }
    else if (ethAddress.Equals("0xFd086bC7CD5C481DCC9C85ebE478A1C0b69FCbb9", StringComparison.OrdinalIgnoreCase))
    {
        warningText = "此地址为 Tether（泰达）公司在 Arbitrum One 网络的 <b>USDT</b> 智能合约地址！\n" +
                      "该地址非用户地址，向该地址转账任意币种将造成资金永久丢失！";
    }
    else if (ethAddress.Equals("0x94b008aA00579c1307B0EF2c499aD98a8ce58e58", StringComparison.OrdinalIgnoreCase))
    {
        warningText = "此地址为 Tether（泰达）公司在 Optimism 网络的 <b>USDT</b> 智能合约地址！\n" +
                      "该地址非用户地址，向该地址转账任意币种将造成资金永久丢失！";
    }
    else if (ethAddress.Equals("0xC931f61B070E9bdfa63E7f2a02d39F4B3B75ED16", StringComparison.OrdinalIgnoreCase))
    {
        warningText = "此地址为 Tether（泰达）公司在 Fantom 网络的 <b>USDT</b> 智能合约地址！\n" +
                      "该地址非用户地址，向该地址转账任意币种将造成资金永久丢失！";
    }
    else if (ethAddress.Equals("0x2791Bca1f2de4661ED88A30C99A7a9449Aa84174", StringComparison.OrdinalIgnoreCase))
    {
        warningText = "此地址为 Circle 公司在 Polygon 网络的 <b>USDC</b> 智能合约地址！\n" +
                      "该地址非用户地址，向该地址转账任意币种将造成资金永久丢失！";
    }
    else if (ethAddress.Equals("0xB97EF9Ef8734C71904D8002F8b6Bc66Dd9c48a6E", StringComparison.OrdinalIgnoreCase))
    {
        warningText = "此地址为 Circle 公司在 Avalanche C-Chain 网络的 <b>USDC</b> 智能合约地址！\n" +
                      "该地址非用户地址，向该地址转账任意币种将造成资金永久丢失！";
    }
    else if (ethAddress.Equals("0xFF970A61A04b1cA14834A43f5dE4533eBDDB5CC8", StringComparison.OrdinalIgnoreCase))
    {
        warningText = "此地址为 Circle 公司在 Arbitrum One 网络的 <b>USDC</b> 智能合约地址！\n" +
                      "该地址非用户地址，向该地址转账任意币种将造成资金永久丢失！";
    }
    else if (ethAddress.Equals("0x7F5c764cBc14f9669B88837ca1490cCa17c31607", StringComparison.OrdinalIgnoreCase))
    {
        warningText = "此地址为 Circle 公司在 Optimism 网络的 <b>USDC</b> 智能合约地址！\n" +
                      "该地址非用户地址，向该地址转账任意币种将造成资金永久丢失！";
    }
    else if (ethAddress.Equals("0x04068DA6C83AFCFA0e13ba15A6696662335D5B75", StringComparison.OrdinalIgnoreCase))
    {
        warningText = "此地址为 Circle 公司在 Fantom 网络的 <b>USDC</b> 智能合约地址！\n" +
                      "该地址非用户地址，向该地址转账任意币种将造成资金永久丢失！";
    }

    // 如果是智能合约地址，直接返回警告信息
    if (warningText != null)
    {
        try
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: warningText,
                parseMode: ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    new InlineKeyboardButton[]
                    {
                        InlineKeyboardButton.WithCallbackData("什么是智能合约地址？", "智能合约地址")
                    }
                })
            );
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"发送智能合约警告消息失败：{ex.Message}");
        }
        // 记录查询时间，即使是智能合约地址也记录
        QueryCooldownManager.RecordQueryTime(userId);
        return;
    }

    // 发送正在查询的消息
    Telegram.Bot.Types.Message infoMessage;
    try
    {
        infoMessage = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "正在查询以太坊主网和币安智能链地址，请稍后...",
            parseMode: ParseMode.Html,
            replyToMessageId: message.MessageId
        );
    }
    catch (Exception ex)
    {
        //Console.WriteLine($"发送查询提示消息失败：{ex.Message}");
        return;
    }

    // 并行查询以太坊和 BSC
    var ethTask = EthereumQuery.QueryEthAddressAsync(ethAddress);
    var bscTask = BscQuery.QueryBscAddressAsync(ethAddress);

    await Task.WhenAll(ethTask, bscTask);

    var (ethBalance, usdtBalanceEth, usdcBalanceEth, cnyUsdtBalanceEth, cnyUsdcBalanceEth, gasPriceGweiEth, gasPriceUsdEth, lastTxTime, isErrorEth) = ethTask.Result;
    var (bnbBalance, usdtBalanceBsc, usdcBalanceBsc, cnyUsdtBalanceBsc, cnyUsdcBalanceBsc, isErrorBsc) = bscTask.Result;

    // 取消冷却
    QueryCooldownManager.CancelCooldown(userId);

    // 如果两条链都查询失败，报错
    if (isErrorEth && isErrorBsc)
    {
        try
        {
            await botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: infoMessage.MessageId,
                text: "查询以太坊和 BSC 地址均失败，请稍后重试！",
                parseMode: ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    new InlineKeyboardButton[]
                    {
                        InlineKeyboardButton.WithCallbackData("再查一次", $"eth_query:{ethAddress}"),
                        InlineKeyboardButton.WithUrl("ETH 详细信息", $"https://etherscan.io/address/{ethAddress}"),
                        InlineKeyboardButton.WithUrl("BSC 详细信息", $"https://bscscan.com/address/{ethAddress}"),
                        InlineKeyboardButton.WithUrl("进群使用", "https://t.me/yifanfuBot?startgroup=true")
                    }
                })
            );
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"编辑错误提示消息失败：{ex.Message}");
        }
        return;
    }

    // 获取 USDT/CNY 价格
    decimal okxPrice = await GetOkxPriceAsync("usdt", "cny", "alipay");
    if (okxPrice == 0)
    {
        //Console.WriteLine("无法获取 OKX USDT/CNY 价格，使用默认值");
        okxPrice = 7.17m; // 默认值（根据示例）
    }

    // 获取 ETH 和 BNB 美元价格
    decimal ethPriceUsd = 0m, bnbPriceUsd = 0m;
    string[] ethSymbols = { "ETH", "ETHEREUM" };
    string[] bnbSymbols = { "BNB", "BNB SMART CHAIN" };

    foreach (var symbol in ethSymbols)
    {
        var coinInfo = await CoinDataCache.GetCoinInfoAsync(symbol);
        if (coinInfo != null && coinInfo.TryGetValue("price_usd", out JsonElement priceElement) && priceElement.TryGetDouble(out double price))
        {
            ethPriceUsd = (decimal)price;
            break;
        }
    }

    foreach (var symbol in bnbSymbols)
    {
        var coinInfo = await CoinDataCache.GetCoinInfoAsync(symbol);
        if (coinInfo != null && coinInfo.TryGetValue("price_usd", out JsonElement priceElement) && priceElement.TryGetDouble(out double price))
        {
            bnbPriceUsd = (decimal)price;
            break;
        }
    }

    // 如果未获取到价格，记录日志
    if (ethPriceUsd == 0m)
    {
       // Console.WriteLine("无法获取 ETH 美元价格，人民币价值将不显示");
    }
    if (bnbPriceUsd == 0m)
    {
      //  Console.WriteLine("无法获取 BNB 美元价格，人民币价值将不显示");
    }

    // 计算 ETH 和 BNB 人民币价值
    decimal ethCnyValue = ethBalance * ethPriceUsd * okxPrice;
    decimal bnbCnyValue = bnbBalance * bnbPriceUsd * okxPrice;

    // 构建查询结果文本
    var captionText = new StringBuilder();
var fromUser = message.From;
string userLink = "匿名用户"; // 默认值

if (fromUser != null)
{
    string fromFirstName = fromUser.FirstName ?? "";
    string fromUsername = fromUser.Username ?? "";
    string fromLastName = fromUser.LastName ?? "";

    if (fromUsername == "GroupAnonymousBot")
    {
        // 群组匿名用户，直接显示匿名用户，无链接
        userLink = "<b>匿名用户</b>";
    }
    else if (!string.IsNullOrEmpty(fromUsername))
    {
        // 有用户名（非群组匿名），名字链接到 t.me/Username
        string displayName = !string.IsNullOrEmpty(fromFirstName) ? fromFirstName : "用户";
        userLink = $"<a href=\"https://t.me/{fromUsername.TrimStart('@')}\">{displayName}</a>";
    }
    else if (!string.IsNullOrEmpty(fromFirstName) || !string.IsNullOrEmpty(fromLastName))
    {
        // 无用户名，显示纯文本名字
        userLink = $"{fromFirstName} {fromLastName}".Trim();
    }
    else
    {
        // 名字和姓氏为空，显示匿名用户
        userLink = "匿名用户";
    }
}


    captionText.AppendLine($"<b>来自 </b>{userLink}<b> 的查询</b>\n");
    captionText.AppendLine($"查询地址：<code>{ethAddress}</code>");
    captionText.AppendLine("——————————————");
    captionText.AppendLine($"<b>Ethereum</b>（以太坊主网）");
    if (lastTxTime.HasValue)
    {
        captionText.AppendLine($"最后活跃  ：<b>{lastTxTime.Value:yyyy-MM-dd HH:mm:ss}</b>");
    }
    captionText.AppendLine($"当前 Gas  ：<b>{gasPriceGweiEth:N3} Gwei ≈ ${gasPriceUsdEth:N2}</b>\n");
    captionText.AppendLine($"  ETH 余额：<b>{ethBalance:N4} ETH</b>{(ethCnyValue > 0 ? $" ≈ <b>{ethCnyValue:N2}元人民币</b>" : "")}");
    captionText.AppendLine($"USDT余额：<b>{usdtBalanceEth:N2} USDT</b>{(usdtBalanceEth > 0 ? $" ≈ <b>{cnyUsdtBalanceEth:N2}元人民币</b>" : "")}");
    captionText.AppendLine($"USDC余额：<b>{usdcBalanceEth:N2} USDC</b>{(usdcBalanceEth > 0 ? $" ≈ <b>{cnyUsdcBalanceEth:N2}元人民币</b>" : "")}");
    captionText.AppendLine("——————————————");
    captionText.AppendLine($"<b>BNB Smart Chain</b>（币安智能链）\n");
    captionText.AppendLine($"  BNB余额：<b>{bnbBalance:N4} BNB</b>{(bnbCnyValue > 0 ? $" ≈ <b>{bnbCnyValue:N2}元人民币</b>" : "")}");
    captionText.AppendLine($"USDT余额：<b>{usdtBalanceBsc:N2} USDT</b>{(usdtBalanceBsc > 0 ? $" ≈ <b>{cnyUsdtBalanceBsc:N2}元人民币</b>" : "")}");
    captionText.AppendLine($"USDC余额：<b>{usdcBalanceBsc:N2} USDC</b>{(usdcBalanceBsc > 0 ? $" ≈ <b>{cnyUsdcBalanceBsc:N2}元人民币</b>" : "")}");
    captionText.AppendLine($"\n<a href=\"t.me/yifanfu\">代开会员 | TRX兑换 | 点击购买：\nTRC-20、ERC-20、BEP-20 能量！ </a>");

    var shareLink = "https://t.me/yifanfuBot?startgroup=true";
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new InlineKeyboardButton[]
        {
            InlineKeyboardButton.WithCallbackData("再查一次", $"eth_query:{ethAddress}"),
            InlineKeyboardButton.WithUrl("ETH 详细信息", $"https://etherscan.io/address/{ethAddress}"),
            InlineKeyboardButton.WithUrl("BSC 详细信息", $"https://bscscan.com/address/{ethAddress}"),
            InlineKeyboardButton.WithUrl("进群使用", shareLink)
        }
    });

    // 尝试编辑为媒体消息
    const string imageUrl = "https://i.postimg.cc/vm1W2cRw/111.png";
    bool mediaEditSuccess = false;
    try
    {
        await botClient.EditMessageMediaAsync(
            chatId: chatId,
            messageId: infoMessage.MessageId,
            media: new InputMediaPhoto(imageUrl)
            {
                Caption = captionText.ToString(),
                ParseMode = ParseMode.Html
            },
            replyMarkup: inlineKeyboard
        );
        mediaEditSuccess = true;
    }
    catch (Exception ex)
    {
       // Console.WriteLine($"编辑查询结果为媒体消息失败：{ex.Message}");
    }

    // 如果编辑媒体消息失败，尝试发送新图片消息
    if (!mediaEditSuccess)
    {
        try
        {
            await botClient.SendPhotoAsync(
                chatId: chatId,
                photo: new InputOnlineFile(imageUrl),
                caption: captionText.ToString(),
                parseMode: ParseMode.Html,
                replyMarkup: inlineKeyboard
            );
            try
            {
                await botClient.DeleteMessageAsync(chatId, infoMessage.MessageId);
            }
            catch (Exception ex)
            {
              //  Console.WriteLine($"删除初始消息失败：{ex.Message}");
            }
        }
        catch (Exception sendEx)
        {
          //  Console.WriteLine($"发送图片消息失败：{sendEx.Message}");
            try
            {
                await botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: infoMessage.MessageId,
                    text: captionText.ToString(),
                    parseMode: ParseMode.Html,
                    replyMarkup: inlineKeyboard,
		    disableWebPagePreview: true // 添加：关闭链接预览以避免显示图片链接预览	
                );
            }
            catch (Exception textEx)
            {
              //  Console.WriteLine($"编辑为文本消息：{textEx.Message}");
                try
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: captionText.ToString(),
                        parseMode: ParseMode.Html,
                        replyMarkup: inlineKeyboard,
			disableWebPagePreview: true // 添加：关闭链接预览以避免显示图片链接预览  
                    );
                }
                catch (Exception finalEx)
                {
                  //  Console.WriteLine($"发送文本消息失败：{finalEx.Message}");
                }
            }
        }
    }

    // 记录查询时间
    QueryCooldownManager.RecordQueryTime(userId);
}
//查询获取hyperliquid资金费率 /zijinhy
public static class FundingRateMonitor
{
    private static readonly HttpClient httpClient = new HttpClient();

    public static async Task<string> GetHyperliquidFundingRates()
    {
        try
        {
            // 发送 metaAndAssetCtxs 请求
            var requestBody = new { type = "metaAndAssetCtxs" };
            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );
            var response = await httpClient.PostAsync("https://api.hyperliquid.xyz/info", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            // 调试：输出 JSON
            // Console.WriteLine($"metaAndAssetCtxs JSON: {json}");

            // 解析 JSON
            var jsonDocument = JsonDocument.Parse(json);

            // 获取 universe（币种列表）和资金费率数据
            var universe = jsonDocument.RootElement[0].GetProperty("universe").EnumerateArray();
            var fundingData = jsonDocument.RootElement[1].EnumerateArray();

            var fundingRates = new List<(string Symbol, decimal FundingRate)>();
            int index = 0;
            foreach (var coin in universe)
            {
                string symbol = coin.TryGetProperty("name", out var nameProp)
                    ? nameProp.GetString()?.ToUpper() ?? "未知"
                    : "未知";
                bool isDelisted = coin.TryGetProperty("isDelisted", out var delistedProp) && delistedProp.GetBoolean();

                // 跳过已下架币种、未知币种或 TRX
                if (isDelisted || symbol == "未知" || symbol == "TRX")
                {
                    index++;
                    continue;
                }

                // 从 fundingData 获取对应资金费率
                if (index < fundingData.Count())
                {
                    var fundingItem = fundingData.ElementAt(index);
                    decimal fundingRate = fundingItem.TryGetProperty("funding", out var rateProp)
                        ? decimal.Parse(rateProp.GetString()!)
                        : 0;

                    fundingRates.Add((symbol, fundingRate));
                }
                index++;
            }

            // 排序正资金费率（前 5）
            var positiveRates = fundingRates
                .Where(r => r.FundingRate > 0)
                .OrderByDescending(r => r.FundingRate)
                .Take(5)
                .ToList();

            // 排序负资金费率（前 5，负数越大越靠前）
            var negativeRates = fundingRates
                .Where(r => r.FundingRate < 0)
                .OrderBy(r => r.FundingRate) // 负数越大（数值越小）排前面
                .Take(5)
                .ToList();

            // 构建 HTML 格式返回消息
            var sb = new StringBuilder();
            sb.AppendLine("<b>Hyperliquid 正资金费率 TOP5：</b>");
            if (positiveRates.Any())
            {
                foreach (var rate in positiveRates)
                {
                    // 币种用 <code> 包裹，加 /USDT 后缀，币种后加 4 个空格
                    sb.AppendLine($"<code>{rate.Symbol}</code>/USDT    {rate.FundingRate:P4}");
                }
            }
            else
            {
                sb.AppendLine("暂无正资金费率数据");
            }

            sb.AppendLine("\n<b>Hyperliquid 负资金费率 TOP5：</b>");
            if (negativeRates.Any())
            {
                foreach (var rate in negativeRates)
                {
                    // 币种用 <code> 包裹，加 /USDT 后缀，币种后加 4 个空格
                    sb.AppendLine($"<code>{rate.Symbol}</code>/USDT    {rate.FundingRate:P4}");
                }
            }
            else
            {
                sb.AppendLine("暂无负资金费率数据");
            }

            return sb.ToString();
        }
        catch (HttpRequestException ex)
        {
            return $"查询资金费率失败：网络错误 - {ex.Message} (Status: {ex.StatusCode})";
        }
        catch (JsonException ex)
        {
            return $"查询资金费率失败：JSON 解析错误 - {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"查询资金费率失败：{ex.Message}";
        }
    }
}
	
// 定义全局变量  租能量价格
public static decimal TransactionFee = 7.00m;

// 对方地址有u的费用计算，这里固定14TRX
static decimal fixedCost = 14.00m;
static decimal savings = fixedCost - TransactionFee;
static decimal savingsPercentage = Math.Ceiling((savings / fixedCost) * 100);

// 对方地址无u的费用计算
static decimal noUFee = TransactionFee * 2 - 1;
static decimal noUSavings = fixedCost * 2 - noUFee;
static decimal noUSavingsPercentage = Math.Ceiling((noUSavings / (fixedCost * 2)) * 100);
	
// 通知字典，用于资金费异常通知
private static Dictionary<long, bool> fundingRateNotificationUserIds = new Dictionary<long, bool>
{
    { 1427768220, true } // 初始用户ID
};

// 字典，用于存储币安资金费数据
private static Dictionary<string, double> fundingRates = new Dictionary<string, double>();
// 字典，用于存储Hyperliquid资金费数据
private static Dictionary<string, double> hyperliquidFundingRates = new Dictionary<string, double>();

// 定时器，用于定期检查资金费
private static System.Timers.Timer fundingRateTimer;

// 初始化定时器并设置首次和后续的触发逻辑
public static void InitializeFundingRateTimer(ITelegramBotClient botClient)
{
    if (fundingRateTimer == null)
    {
        fundingRateTimer = new System.Timers.Timer();
        FetchAndUpdateFundingRates(botClient).Wait();
        SetRandomTimerInterval();
        fundingRateTimer.Elapsed += async (sender, e) => 
        {
            SetRandomTimerInterval();
            await FetchAndUpdateFundingRates(botClient);
        };
        fundingRateTimer.Start();
        //Console.WriteLine("定时器初始化并启动成功。");
    }
    else
    {
        //Console.WriteLine("定时器已经初始化，不需要重复启动。");
    }
}

private static void SetRandomTimerInterval()
{
    var random = new Random();
    var interval = random.Next(600000, 1200001); // 随机10-20分钟更新  600-1200秒
    fundingRateTimer.Interval = interval;
}

// 从Binance和Hyperliquid API获取资金费数据并更新字典
private static async Task FetchAndUpdateFundingRates(ITelegramBotClient botClient)
{
    try
    {
        // 获取币安资金费率
        var httpClient = new HttpClient();
        var binanceResponse = await httpClient.GetAsync("https://fapi.binance.com/fapi/v1/premiumIndex");
        if (binanceResponse.IsSuccessStatusCode)
        {
            var content = await binanceResponse.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<List<FundingRate>>(content);
            //Console.WriteLine("成功获取币安资金费数据。");

            // 更新币安字典，排除特定币种
            foreach (var item in data)
            {
                if (item.symbol != "TRXUSDT") // 排除 TRX/USDT
                {
                    fundingRates[item.symbol] = double.Parse(item.lastFundingRate);
                }
            }
        }
        else
        {
            //Console.WriteLine($"币安API调用失败: 状态码 {binanceResponse.StatusCode}");
            throw new Exception("币安API调用失败");
        }

        // 获取Hyperliquid资金费率
        var hyperliquidResult = await FundingRateMonitor.GetHyperliquidFundingRates();
        if (!hyperliquidResult.StartsWith("查询资金费率失败"))
        {
            // 解析Hyperliquid返回的HTML格式数据
            hyperliquidFundingRates.Clear();
            var lines = hyperliquidResult.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("/USDT"))
                {
                    // 示例行：<code>BTC</code>/USDT    0.0123
                    var parts = line.Split(new[] { "    " }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        var symbolPart = parts[0].Replace("<code>", "").Replace("</code>", "").Replace("/USDT", "");
                         // 添加过滤逻辑：排除 TRX
                        if (symbolPart != "TRX")
                       {
                          var ratePart = parts[1].TrimEnd('%'); // 去掉百分比符号
                          if (double.TryParse(ratePart, out var rate))
                          {
                              hyperliquidFundingRates[$"{symbolPart}_hy"] = rate / 100; // 转换为小数
                          }
                       }
                    }
                }
            }
            //Console.WriteLine("成功获取Hyperliquid资金费数据。");
        }
        else
        {
            //Console.WriteLine($"Hyperliquid API调用失败: {hyperliquidResult}");
            // 不抛出异常，继续处理币安数据
        }

        // 检查并通知用户
        CheckAndNotifyUsers(botClient);
    }
    catch (Exception ex)
    {
        //Console.WriteLine($"获取资金费数据时发生错误: {ex.Message}");
        // 停止定时器并清空字典
        fundingRateTimer.Stop();
        fundingRates.Clear();
        hyperliquidFundingRates.Clear();
        // 向用户发送通知
        await botClient.SendTextMessageAsync(
            chatId: 1427768220,
            text: "资金费率定时检查已停止，请重启！",
            parseMode: ParseMode.Html
        );
        throw; // 可选：重新抛出异常，如果需要在调用栈上层进一步处理
    }
}

// 字典，用于跟踪最后一次通知时间，针对每个用户和币种的组合
private static Dictionary<(long, string), DateTime> lastNotifiedTimes = new Dictionary<(long, string), DateTime>();

// 检查并通知用户
private static void CheckAndNotifyUsers(ITelegramBotClient botClient)
{
    try
    {
        TimeZoneInfo chinaZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
        DateTime beijingTimeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, chinaZone);

        List<long> usersToRemove = new List<long>(); // 用于存储需要移除的用户ID

        //Console.WriteLine("开始检查用户和发送通知...");

        foreach (var userId in fundingRateNotificationUserIds.Keys.ToList()) // 使用ToList确保在迭代时可以修改原字典
        {
            // VIP 状态检查
            if (VipAuthorizationHandler.TryGetVipExpiryTime(userId, out var expiryTime))
            {
                DateTime beijingTimeExpiry = TimeZoneInfo.ConvertTimeFromUtc(expiryTime, chinaZone);

                if (beijingTimeNow > beijingTimeExpiry)
                {
                    usersToRemove.Add(userId); // 添加到移除列表
                    //Console.WriteLine($"用户 {userId} 的VIP已过期，将被移除。");
                    continue;
                }
            }
            else
            {
                usersToRemove.Add(userId);
                //Console.WriteLine($"无法确认用户 {userId} 的VIP状态，将被移除。");
                continue;
            }

            List<(string symbol, double rate, bool isHyperliquid)> ratesToNotify = new List<(string symbol, double rate, bool isHyperliquid)>();
            List<InlineKeyboardButton[]> buttons = new List<InlineKeyboardButton[]>();

            // 检查币安资金费率
            foreach (var rate in fundingRates)
            {
                if (Math.Abs(rate.Value) >= 0.015) // 资金费正负 1.5%
                {
                    var key = (userId, rate.Key);
                    if (!lastNotifiedTimes.ContainsKey(key) || beijingTimeNow - lastNotifiedTimes[key] > TimeSpan.FromHours(1))
                    {
                        ratesToNotify.Add((rate.Key.Replace("USDT", ""), rate.Value, false));
                        lastNotifiedTimes[key] = beijingTimeNow; // 更新通知时间
                    }
                }
            }

            // 检查Hyperliquid资金费率
            foreach (var rate in hyperliquidFundingRates)
            {
                if (Math.Abs(rate.Value) >= 0.015) // 资金费正负 1.5%
                {
                    var key = (userId, rate.Key);
                    if (!lastNotifiedTimes.ContainsKey(key) || beijingTimeNow - lastNotifiedTimes[key] > TimeSpan.FromHours(1))
                    {
                        ratesToNotify.Add((rate.Key, rate.Value, true));
                        lastNotifiedTimes[key] = beijingTimeNow; // 更新通知时间
                    }
                }
            }

            if (ratesToNotify.Count > 0)
            {
                // 对资金费率进行排序，正数从大到小，负数从大到小
                var sortedRates = ratesToNotify
                    .OrderByDescending(r => r.rate > 0)
                    .ThenByDescending(r => Math.Abs(r.rate))
                    .ToList();

                string message = "<b>资金费率异常提醒：</b>\n\n";
                foreach (var (symbol, rate, isHyperliquid) in sortedRates)
                {
                    var displaySymbol = isHyperliquid ? symbol : symbol;
                    message += $"<code>{displaySymbol}</code>/USDT    {Math.Round(rate * 100, 3)}%\n";
                    // 仅添加币种查询按钮
                    buttons.Add(new InlineKeyboardButton[] {
                        InlineKeyboardButton.WithCallbackData(displaySymbol, $"查{displaySymbol}")
                    });
                }

                // 添加单独的“取消异常提醒”按钮作为最后一行
                buttons.Add(new InlineKeyboardButton[] {
                    InlineKeyboardButton.WithCallbackData("取消异常提醒", "/quxiaozijinfei")
                });

                var keyboard = new InlineKeyboardMarkup(buttons.ToArray());
                botClient.SendTextMessageAsync(
                    chatId: userId,
                    text: message,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard
                ).Wait();
                //Console.WriteLine($"通知已发送给用户 {userId}。");
            }
        }

        // 从播报字典中移除非VIP或VIP已过期的用户
        foreach (var user in usersToRemove)
        {
            fundingRateNotificationUserIds.Remove(user);
            //Console.WriteLine($"用户 {user} 已从通知字典中移除。");
        }
    }
    catch (Exception ex)
    {
        //Console.WriteLine($"在发送通知时发生错误: {ex.Message}");
    }
}
// 15分钟K线数据监控
private static Dictionary<string, List<(DateTime time, decimal price)>> coinKLineData = new Dictionary<string, List<(DateTime time, decimal price)>>();

public class KLineMonitor
{
    private static Timer kLineUpdateTimer;
    private static bool isKLineMonitoringStarted = false;
    private static bool isKLineDataCollectionStarted = false; // 新增字段，用于跟踪K线数据收集定时器是否已启动
    private static readonly int MaxDataPoints = 4; // 最多存储4条数据，用于计算连续3根K线的上涨

public static void BatchStartCoinMonitoring(long userId, string messageText, ITelegramBotClient botClient, long chatId)
{
    var lines = messageText.Split('\n');
    foreach (var line in lines)
    {
        var match = Regex.Match(line, @"#\s*([A-Z]+)\s*\|");
        if (match.Success)
        {
            string coin = match.Groups[1].Value.ToUpper();
            StartCoinMonitoring(userId, coin, botClient, chatId);
        }
    }
    botClient.SendTextMessageAsync(chatId, "批量买入完成，相关币种监控已启动。", Telegram.Bot.Types.Enums.ParseMode.Html);
}
public static void BatchStopCoinMonitoring(long userId, string messageText, ITelegramBotClient botClient, long chatId)
{
    var lines = messageText.Split('\n');
    foreach (var line in lines)
    {
        // 更新正则表达式以匹配行首的币种名称，直到 "|" 字符
        var match = Regex.Match(line, @"^([A-Z]+)\s*\|");
        if (match.Success)
        {
            string coin = match.Groups[1].Value.ToUpper();
            StopCoinMonitoring(userId, coin);
        }
    }
    botClient.SendTextMessageAsync(chatId, "批量卖出完成，相关币种监控已停止。", Telegram.Bot.Types.Enums.ParseMode.Html);
}
public static bool StopCoinMonitoring(long userId, string coin)
{
    if (userMonitoredCoins.ContainsKey(userId) && userMonitoredCoins[userId].ContainsKey(coin))
    {
        userMonitoredCoins[userId][coin].Dispose(); // 停止定时器
        userMonitoredCoins[userId].Remove(coin);
        return true;
    }
    return false;
}
// 启动特定币种的监控
public static Dictionary<long, Dictionary<string, Timer>> userMonitoredCoins = new Dictionary<long, Dictionary<string, Timer>>();	
    public static void StartCoinMonitoring(long userId, string coin, ITelegramBotClient botClient, long chatId)
    {
        if (!userMonitoredCoins.ContainsKey(userId))
        {
            userMonitoredCoins[userId] = new Dictionary<string, Timer>();
        }

        if (userMonitoredCoins[userId].ContainsKey(coin))
        {
            //Console.WriteLine($"用户 {userId} 已经在监控币种 {coin}，无需重复添加。");
            return; // 如果已经在监控这个币种，就不再添加新的定时器
        }

        // 检查字典中是否有足够的K线数据，如果没有且定时器未启动，则启动K线数据收集定时器
        if (!coinKLineData.ContainsKey(coin) || coinKLineData[coin].Count < MaxDataPoints)
        {
            if (!isKLineDataCollectionStarted)
            {
                //Console.WriteLine($"字典中没有足够的K线数据，币种：{coin}，启动K线数据收集定时器。");
                StartKLineMonitoringAsync(botClient, chatId);
                isKLineDataCollectionStarted = true; // 标记为已启动
            }
        }

        // 设置定时器，首次检查后，如果数据足够，每分钟检查一次
        Timer timer = new Timer(async _ => await CheckAndNotifyAsync(userId, coin, botClient), null, 0, 60000); // 每分钟检查一次
        userMonitoredCoins[userId].Add(coin, timer);
        //Console.WriteLine($"接收到监控币种 {coin}，为用户 {userId} 启动查询最新价格。");
    }

// 检查币种价格并通知用户
private static Dictionary<string, DateTime> lastNotificationTime = new Dictionary<string, DateTime>();

private static async Task CheckAndNotifyAsync(long userId, string coin, ITelegramBotClient botClient)
{
    //Console.WriteLine($"启动对比字典里的4根K线数据，币种：{coin}。");

    if (!coinKLineData.ContainsKey(coin) || coinKLineData[coin].Count < MaxDataPoints)
    {
        //Console.WriteLine($"字典没有足够的K线数据，币种：{coin}，等待下一次数据更新。");
        return; // 数据不足
    }

    // 获取最新的价格信息
    var currentPrices = await FetchDetailedCurrentPricesAsync();
    if (!currentPrices.ContainsKey(coin))
    {
        //Console.WriteLine($"无法获取币种 {coin} 的最新价格信息。");
        return;
    }

    var currentPrice = currentPrices[coin].price;
    var kLines = coinKLineData[coin];
    var lastKLinePrice = kLines.Last().price;

    // 检查是否满足连续上涨且最新价格大于最后一根K线的价格
    if (kLines.Count == MaxDataPoints &&
        kLines.Zip(kLines.Skip(1), (first, second) => second.price > first.price).All(x => x) &&
        currentPrice > lastKLinePrice)
    {
        // 检查是否在15分钟内已经发送过通知
        if (lastNotificationTime.ContainsKey(coin) && (DateTime.Now - lastNotificationTime[coin]).TotalMinutes < 15)
        {
            //Console.WriteLine($"币种 {coin} 在15分钟内已经发送过通知，跳过此次通知。");
            return;
        }

// 构建消息文本，按照从最新到最旧的顺序显示K线数据
StringBuilder message = new StringBuilder();
message.AppendLine($"符合连续上涨：最新价：{currentPrice}，币种：<code>{coin}</code>");

// 从最新到最旧排序K线数据并格式化输出
for (int i = kLines.Count - 1; i >= 1; i--)
{
    decimal increase = (kLines[i].price - kLines[i - 1].price) / kLines[i - 1].price * 100;
    message.AppendLine($"{kLines[i].time:yyyy/MM/dd HH:mm} | $:{kLines[i].price} | 上涨：{increase:F2}%");
}

// 添加一个空行作为分隔
message.AppendLine();

        // 获取成交量信息
        string volumeInfo = await BinancePriceInfo.GetHourlyTradingVolume(coin);
        if (!string.IsNullOrEmpty(volumeInfo) && !volumeInfo.Contains("查询失败，该币种暂未上架币安/欧意交易所"))
        {
            message.AppendLine(volumeInfo);
        }
        else
        {
            message.AppendLine("⚠️注意：该币种未上架 币安 | 欧易 交易所！");
        }

//Console.WriteLine($"准备向用户ID：{userId} 播报！");
    // 创建内联按钮
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithCallbackData($"监控 {coin}", $"监控 {coin}"),
        InlineKeyboardButton.WithCallbackData($"查 {coin} 详情", $"查 {coin}")		
    });

    try
    {
        await botClient.SendTextMessageAsync(userId, message.ToString(), Telegram.Bot.Types.Enums.ParseMode.Html, replyMarkup: inlineKeyboard);
        lastNotificationTime[coin] = DateTime.Now; // 成功发送后更新通知时间
    }
    catch (Exception ex)
    {
        Console.WriteLine($"发送消息失败，原因：{ex.Message}。将在5秒后重试。");
        await Task.Delay(5000); // 等待5秒

        try
        {
            await botClient.SendTextMessageAsync(userId, message.ToString(), Telegram.Bot.Types.Enums.ParseMode.Html, replyMarkup: inlineKeyboard);
            lastNotificationTime[coin] = DateTime.Now; // 重试成功后更新通知时间
        }
        catch (Exception retryEx)
        {
            Console.WriteLine($"重试发送消息失败，原因：{retryEx.Message}。取消本次发送。");
        }
    }
    }
    else
    {
        //Console.WriteLine($"没有符合连续上涨的要求：最新价：{currentPrice}，币种：{coin}");
        //Console.WriteLine($"详细K线数据：");
        for (int i = 0; i < kLines.Count; i++)
        {
            //Console.WriteLine($"第{i+1}根K线价格：{kLines[i].price}，时间：{kLines[i].time:yyyy/MM/dd HH:mm}");
        }
    }
}
private static Timer monitoringTimer; // 新增：用于监控K线监控任务的定时器	
public static async Task StartKLineMonitoringAsync(ITelegramBotClient botClient, long chatId)
{
    if (!isKLineMonitoringStarted)
    {
        isKLineMonitoringStarted = true;
        var now = DateTime.Now;
        // 计算到下一个整15分钟的总秒数（包括秒）
        int secondsToNextQuarter = (15 - (now.Minute % 15)) * 60 - now.Second;
        var nextTargetTime = now.AddSeconds(secondsToNextQuarter);
        var timeToNextTarget = (int)(nextTargetTime - now).TotalMilliseconds;
        kLineUpdateTimer = new Timer(async _ => await UpdateKLineDataAsync(), null, timeToNextTarget, 900000); // 每15分钟更新一次
        //Console.WriteLine($"[{DateTime.Now}] K线数据监控启动，下一次数据获取将在 {secondsToNextQuarter / 60} 分钟 {secondsToNextQuarter % 60} 秒后.");

        // 新增：启动或重置监控定时器
        monitoringTimer?.Dispose(); // 如果已存在，则先释放
        monitoringTimer = new Timer(CheckAndRestartMonitoringTask, botClient, 60000, 60000); // 每分钟检查一次
	//Console.WriteLine($"[{DateTime.Now}] 监控定时器启动成功，将每分钟检查一次监控任务状态。");    
	    
        await botClient.SendTextMessageAsync(chatId, "K线数据监控启动，数据收集中...");
    }
    else
    {
        // 检查是否有足够的数据点
        if (coinKLineData.Values.All(list => list.Count >= 3))
        {
            //Console.WriteLine($"[{DateTime.Now}] 数据已满足条件，正在处理请求.");
            await SendTopRisingCoinsAsync(botClient, chatId);
        }
        else
        {
            var now = DateTime.Now;
            int secondsToNextQuarter = (15 - (now.Minute % 15)) * 60 - now.Second;
            var nextTargetTime = now.AddSeconds(secondsToNextQuarter);
            // 计算需要等待的完整周期数（每周期15分钟）
            int cyclesNeeded = 3 - coinKLineData.Values.Min(list => list.Count); // 需要的周期数
            var completeStorageTime = nextTargetTime.AddMinutes(15 * cyclesNeeded);
            var retryTime = completeStorageTime.AddMinutes(1); // 加1分钟确保数据完全更新

            //Console.WriteLine($"[{DateTime.Now}] 数据未储存完成，需要等待更多数据.");
            await botClient.SendTextMessageAsync(chatId, $"数据未储存完成，请在 {retryTime:yyyy/MM/dd HH:mm:ss} 后重试！");
        }
    }
}
// 新增：定义一个方法来检查和重启监控任务
private static void CheckAndRestartMonitoringTask(object state)
{
    var botClient = (ITelegramBotClient)state;
    if (!isKLineMonitoringStarted) // 如果监控任务未在运行
    {
        //Console.WriteLine($"[{DateTime.Now}] 检测到15分钟K线定时器已停止，重新启动中...");
        try
        {
            StartKLineMonitoringAsync(botClient, 1427768220).Wait();
            //Console.WriteLine($"[{DateTime.Now}] k线监控任务重新启动成功。");
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"[{DateTime.Now}] k线监控任务重新启动失败：{ex.Message}");
        }
    }
    else
    {
        //Console.WriteLine($"[{DateTime.Now}] k线监控任务正在运行。");
    }
}
private static int consecutiveUpdateFailures = 0; // 追踪连续更新失败的次数

private static async Task UpdateKLineDataAsync()
{
    try
    {
        await CoinDataCache.EnsureCacheInitializedAsync(); // 确保缓存已初始化
        var prices = await FetchCurrentPricesAsync(); // 从本地缓存获取当前价格
        var now = DateTime.UtcNow.AddHours(8); // 北京时间

        Dictionary<string, List<(DateTime, decimal)>> tempData = new Dictionary<string, List<(DateTime, decimal)>>();
        foreach (var price in prices)
        {
            if (!coinKLineData.ContainsKey(price.Key))
            {
                tempData[price.Key] = new List<(DateTime, decimal)>();
            }
            else
            {
                tempData[price.Key] = new List<(DateTime, decimal)>(coinKLineData[price.Key]);
            }
            if (tempData[price.Key].Count >= MaxDataPoints)
            {
                tempData[price.Key].RemoveAt(0);
            }
            tempData[price.Key].Add((now, price.Value));
        }

        // 更新成功，替换旧数据
        coinKLineData = tempData;
        //Console.WriteLine($"[{DateTime.Now}] K线数据已更新.");
        consecutiveUpdateFailures = 0; // 重置失败计数
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[{DateTime.Now}] 更新K线数据失败：{ex.Message}");
        consecutiveUpdateFailures++;  // 增加失败计数
        if (consecutiveUpdateFailures >= 2)
        {
            consecutiveUpdateFailures = 0;  // 重置失败计数
            try
            {
                await StartKLineMonitoringAsync(botClient, 1427768220);  // 异步重启主任务
                Console.WriteLine($"[{DateTime.Now}] K线监控任务重新启动成功。");
                await botClient.SendTextMessageAsync(1427768220, "K线监控任务已自动重启！");
            }
            catch (Exception restartEx)
            {
                Console.WriteLine($"[{DateTime.Now}] K线监控任务重新启动失败：{restartEx.Message}");
            }
        }
    }
}

private static async Task SendFailureNotificationAsync(ITelegramBotClient botClient)
{
    await botClient.SendTextMessageAsync(1427768220, "15分钟k线数据更新失败，请检查！");
}

    private static async Task<Dictionary<string, decimal>> FetchCurrentPricesAsync()
    {
        var allCoinsData = CoinDataCache.GetAllCoinsData();
        return allCoinsData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value["price_usd"].GetDecimal());
    }

private static async Task SendTopRisingCoinsAsync(ITelegramBotClient botClient, long chatId)
{
    var currentPrices = await FetchDetailedCurrentPricesAsync(); // 获取最新的详细价格信息
    var topRisingCoins = coinKLineData
        .Where(kvp => kvp.Value.Count == MaxDataPoints && 
                      kvp.Value.Zip(kvp.Value.Skip(1), (first, second) => second.price > first.price).All(x => x) &&
                      currentPrices[kvp.Key].price > kvp.Value.Last().price)
        .Select(kvp => new
        {
            Coin = kvp.Key,
            CurrentPrice = currentPrices[kvp.Key].price,
            MarketCap = currentPrices[kvp.Key].MarketCap,
            Volume24h = currentPrices[kvp.Key].Volume24h,
            Rank = currentPrices[kvp.Key].Rank,
            Increases = kvp.Value
                .Select((data, index) => index == 0 ? 0 : (data.price - kvp.Value[index - 1].price) / kvp.Value[index - 1].price * 100)
                .ToList(),
            //TotalIncrease = kvp.Value
               // .Select((data, index) => index == 0 ? 0 : (data.price - kvp.Value[index - 1].price) / kvp.Value[index - 1].price * 100)
             //   .Sum() // 计算总上涨百分比 计算每根k线上涨幅度相加
                TotalIncrease = kvp.Value.Count == MaxDataPoints ?
                    (kvp.Value.Last().price - kvp.Value.First().price) / kvp.Value.First().price * 100 : 0     // 计算总上涨百分比 最早的k线价格和现在的价格做对比，直接计算涨幅
        })
        .OrderByDescending(kvp => kvp.Increases.Sum())
        .Take(5)
        .ToList(); // 确保执行查询并获取结果

    if (topRisingCoins.Count == 0)
    {
        await botClient.SendTextMessageAsync(chatId, "没有连续15分钟上涨币种，请稍等重试！", ParseMode.Html);
    }
    else
    {
        var message = new StringBuilder("<b>15分钟连续上涨TOP5：</b>\n\n");
        for (int i = 0; i < topRisingCoins.Count; i++)
        {
            var coin = topRisingCoins[i];
            message.AppendLine($"<code>{coin.Coin}</code> $：{coin.CurrentPrice} | No.{coin.Rank} | 总\U0001F4C8：{coin.TotalIncrease:F2}%");
            message.AppendLine($"市值: {FormatNumber(coin.MarketCap)} | 24h成交：{FormatNumber(coin.Volume24h)}");
            for (int j = 1; j < coin.Increases.Count; j++)
            {
                message.AppendLine($"{coinKLineData[coin.Coin][j].time:yyyy/MM/dd HH:mm} 上涨：{coin.Increases[j]:F2}% $：{coinKLineData[coin.Coin][j].price}");
            }
            if (i < topRisingCoins.Count - 1)
            {
                message.AppendLine("-----------------------------------");
            }
        }
        // 新增：为消息添加关闭按钮
        var keyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("关闭", "back"));
        await botClient.SendTextMessageAsync(chatId, message.ToString(), ParseMode.Html, replyMarkup: keyboard);
    }
}
private static string FormatNumber(decimal number)
{
    if (number >= 100000000)
    {
        return $"{number / 100000000:F2}亿";
    }
    else if (number >= 1000000)
    {
        return $"{number / 1000000:F2}m";
    }
    return number.ToString("F2");
}

public static async Task<Dictionary<string, (decimal price, decimal MarketCap, decimal Volume24h, int Rank)>> FetchDetailedCurrentPricesAsync()
{
    // 从本地缓存获取所有币种的数据
    var allCoinsData = CoinDataCache.GetAllCoinsData();

    // 将获取的数据转换为包含更多详细信息的字典
    return allCoinsData.ToDictionary(kvp => kvp.Key, kvp => (
        price: kvp.Value["price_usd"].GetDecimal(), // 获取最新价格
        MarketCap: kvp.Value["market_cap_usd"].GetDecimal(), // 获取市值
        Volume24h: kvp.Value["volume_24h_usd"].GetDecimal(), // 获取24小时成交量
        Rank: int.Parse(kvp.Value["rank"].ToString()) // 获取市值排名
    ));
}
}

//定时监控 RSI 值	
public static class TimerManager
{
    private static Timer timerToSendCommand;
    private static Timer timerToMonitor;
    private static ITelegramBotClient botClient;
    private static long userId = 1427768220;

    public static void Initialize(ITelegramBotClient client)
    {
        botClient = client;
        StartOrRestartTimers();
    }

    private static void StartOrRestartTimers()
    {
        if (timerToSendCommand == null)
        {
            var now = DateTime.Now;
            // 计算下一个4小时周期的小时数  首先检查距离下一个周期还多久，后续在 0点 4点 8点 12点 16点 20点 启动
            int nextHour = ((now.Hour / 4) + 1) * 4;
            DateTime nextTargetTime;
             
            // 如果计算的小时数达到或超过24，表示第二天的00:00
            if (nextHour >= 24) {
                nextHour -= 24; // 调整小时数为0
                nextTargetTime = now.AddDays(1).Date.AddHours(nextHour); // 使用AddDays确保日期正确增加
            } else {
                nextTargetTime = new DateTime(now.Year, now.Month, now.Day, nextHour, 0, 0);
            }

            if (nextTargetTime < now) // 如果计算出的时间已经过去，则加4小时
            {
                nextTargetTime = nextTargetTime.AddHours(4);
            }
            var timeToNextTarget = (int)(nextTargetTime - now).TotalMilliseconds;
            timerToSendCommand = new Timer(async _ => await SendCommandAsync(), null, timeToNextTarget, 14400000); // 设置首次触发时间和4小时的周期
            Console.WriteLine($"定时器已启动，距离下一个目标时间{nextTargetTime}还有{timeToNextTarget / 60000}分钟");
        }

        if (timerToMonitor == null)
        {
            timerToMonitor = new Timer(_ => MonitorAndRestartTimer(), null, 0, 60000); //每分钟（60000毫秒）监控一次定时器是否正常
            //Console.WriteLine("定时监控定时器已启动，持续监控中.....");
        }
    }

    private static async Task SendCommandAsync()
    {
        try
        {
            // 构造一个模拟的Message对象
            var fakeMessage = new Message
            {
                Text = "/charsi",
                Chat = new Chat { Id = userId },
                From = new Telegram.Bot.Types.User { Id = userId }
            };

            // 调用BotOnMessageReceived来处理这个模拟的消息
            await BotOnMessageReceived(botClient, fakeMessage);
            Console.WriteLine($"已模拟用户ID：{userId} 发送指令：/charsi");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发送指令失败：{ex.Message}");
            await HandleErrorAsync("发送指令失败，尝试重启...");
        }
    }

    private static void MonitorAndRestartTimer()
    {
        if (timerToSendCommand == null)
        {
            Console.WriteLine("监控到定时器停止，正在尝试重启...");
            StartOrRestartTimers();
        }
    }

    private static async Task HandleErrorAsync(string errorMessage)
    {
        Console.WriteLine(errorMessage);
        await botClient.SendTextMessageAsync(userId, "自动定时查询RSI值已停止，请检查 /zdcrsi 功能！");
        timerToSendCommand?.Dispose();
        timerToSendCommand = null;
        await Task.Delay(5000); // 等待5秒后重启
        StartOrRestartTimers();
    }
}
// 通知用户ID字典 以及查询 rsi指数
private static HashSet<long> notificationUserIds = new HashSet<long> { 1427768220 };
// 订阅通知
public static async Task HandleDingYuErSiCommand(ITelegramBotClient botClient, Message message)
{
    var userId = message.From.Id;
    try
    {
        // 使用新的公共方法检查VIP状态
        if (VipAuthorizationHandler.TryGetVipExpiryTime(userId, out var expiryTime))
        {
            TimeZoneInfo chinaZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
            DateTime beijingTimeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, chinaZone);
            DateTime beijingTimeExpiry = TimeZoneInfo.ConvertTimeFromUtc(expiryTime, chinaZone);

            // 现在使用北京时间进行比较
            if (beijingTimeNow <= beijingTimeExpiry)
            {
                // 用户是VIP
                bool added = notificationUserIds.Add(userId);
                if (added)
                {
                    // 更新字典成功，回复用户
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "订阅成功！ \U00002705\n\n" +
                              "您已成功订阅超卖信号，当价格出现超卖时，机器人将提前通知您！\n" +
                              "币价出现超卖后，通常短时间内会拉升；提前买入，致富快人一步！",
                        parseMode: ParseMode.Html
                    );
                }
                else
                {
                    // 用户ID已存在，无需重复添加
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "您已订阅超卖信号，无需重复订阅。",
                        parseMode: ParseMode.Html
                    );
                }
            }
            else
            {
                // 用户不是VIP或会员已过期，提供订阅选项
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData("订阅 FF Pro会员", "/provip"),
                });

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "订阅失败！ \U0000274C\n\n" +
                          "您还不是 FF Pro会员，请在订阅会员后重试！\n" +
                          "订阅超卖信号，当价格出现超卖时，机器人将提前通知您！\n" +
                          "币价出现超卖后，通常短时间内会拉升；提前买入，致富快人一步！",
                    parseMode: ParseMode.Html,
                    replyMarkup: inlineKeyboard
                );
            }
        }
        else
        {
            // 用户不是VIP，提供订阅选项
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("订阅 FF Pro会员", "/provip"),
            });

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "订阅失败！ \U0000274C\n\n" +
                      "您还不是 FF Pro会员，请在订阅会员后重试！\n" +
                      "订阅超卖信号，当价格出现超卖时，机器人将提前通知您！\n" +
                      "币价出现超卖后，通常短时间内会拉升；提前买入，致富快人一步！",
                parseMode: ParseMode.Html,
                replyMarkup: inlineKeyboard
            );
        }
    }
    catch (Exception ex)
    {
        // 处理异常
        Console.WriteLine($"处理 /dingyuersi 命令时发生异常: {ex.Message}");
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "订阅异常，请稍后重试！",
            parseMode: ParseMode.Html
        );
    }
}
//取消订阅通知
public static async Task HandleCancelDingYuErSiCommand(ITelegramBotClient botClient, Message message)
{
    var userId = message.From.Id;
    try
    {
        // 尝试从通知字典中移除用户ID
        if (notificationUserIds.Remove(userId))
        {
            // 如果用户ID存在于字典中并成功移除
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "取消成功！",
                parseMode: ParseMode.Html
            );
        }
        else
        {
            // 如果用户ID不在字典中，不需要移除，直接回复取消成功
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "取消成功！",
                parseMode: ParseMode.Html
            );
        }
    }
    catch (Exception ex)
    {
        // 记录或处理异常
        Console.WriteLine($"尝试取消订阅时发生异常: {ex.Message}");
        // 发生异常时回复用户
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "操作失败，请稍后重试...",
            parseMode: ParseMode.Html
        );
    }
}
//查rsi值	
public static class CoinDataAnalyzer
{
    private static readonly Random random = new Random();

// 辅助方法：获取上涨的币种
private static IEnumerable<string> GetTopRisers(Dictionary<string, Dictionary<string, JsonElement>> coinData, string percentChangeKey)
{
    return coinData
        .Select(kv => new
        {
            Symbol = kv.Key,
            PercentChange = kv.Value.TryGetValue(percentChangeKey, out JsonElement percentChangeElement) && percentChangeElement.TryGetDouble(out double percentChange) ? percentChange : double.MinValue
        })
        .Where(x => x.PercentChange > 0) // 筛选出上涨的币种
        .OrderByDescending(x => x.PercentChange) // 按上涨幅度排序
        .Select(x => x.Symbol); // 选择币种符号
}

// 获取近1小时、24小时和7天内上涨幅度最高的币种，并检查是否超买
public static async Task<(List<string>, List<InlineKeyboardMarkup>)> GetTopOverboughtCoinsAsync()
{
    await CoinDataCache.EnsureCacheInitializedAsync(); // 确保缓存已初始化
    var allCoinsData = CoinDataCache.GetAllCoinsData();

    // 获取上涨的前20名
    var topRisers1h = GetTopRisers(allCoinsData, "percent_change_1h").Take(20).ToList();
    var topRisers24h = GetTopRisers(allCoinsData, "percent_change_24h").Take(20).ToList();
    var topRisers7d = GetTopRisers(allCoinsData, "percent_change_7d").Take(20).ToList();

    // 合并并去重
    var uniqueSymbols = new HashSet<string>(topRisers1h.Concat(topRisers24h).Concat(topRisers7d));

    List<string> messages = new List<string>();
    List<List<InlineKeyboardButton[]>> allButtonRows = new List<List<InlineKeyboardButton[]>>();
    StringBuilder messageBuilder = new StringBuilder("发现超买：\n\n");
    List<InlineKeyboardButton[]> buttonRows = new List<InlineKeyboardButton[]>();
    List<InlineKeyboardButton> currentRow = new List<InlineKeyboardButton>();
    int coinIndex = 0;
    int messageCount = 0;

    foreach (var symbol in uniqueSymbols)
    {
        if (coinIndex % 20 == 0 && coinIndex > 0)
        {
            messages.Add(messageBuilder.ToString());
            allButtonRows.Add(buttonRows);
            messageBuilder = new StringBuilder("发现超买：\n\n");
            buttonRows = new List<InlineKeyboardButton[]>();
            currentRow = new List<InlineKeyboardButton>();
            messageCount++;
        }

        await RandomDelay(); // 随机时间间隔，防止API限制
        string additionalInfo = await BinancePriceInfo.GetPriceInfo(symbol);
        var coinData = allCoinsData[symbol];

        var rsi6Match = Regex.Match(additionalInfo, @"RSI6:</b>\s*(\d+\.?\d*)");
        var rsi14Match = Regex.Match(additionalInfo, @"RSI14:</b>\s*(\d+\.?\d*)");
        var m10Match = Regex.Match(additionalInfo, @"m10：</b>\s*(\d+\.?\d*)");

        if (double.TryParse(rsi6Match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double rsi6) &&
            double.TryParse(rsi14Match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double rsi14) &&
            double.TryParse(m10Match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double m10) &&
            rsi6 > 85 && rsi14 > 75) // 确保RSI值符合超买条件
        {
            double price = coinData["price_usd"].GetDouble();
            double volume24hUsd = coinData["volume_24h_usd"].GetDouble();
            double marketCapUsd = coinData["market_cap_usd"].GetDouble();
            int rank = coinData["rank"].GetInt32();
            double percentChange1h = coinData["percent_change_1h"].GetDouble();
            double percentChange24h = coinData["percent_change_24h"].GetDouble();
            double percentChange7d = coinData["percent_change_7d"].GetDouble();

            string marketCapDisplay = marketCapUsd >= 100_000_000 ? $"{Math.Round(marketCapUsd / 100_000_000, 2)}亿" : $"{Math.Round(marketCapUsd / 1_000_000, 2)}m";
            string volume24hDisplay = volume24hUsd >= 100_000_000 ? $"{Math.Round(volume24hUsd / 100_000_000, 2)}亿" : $"{Math.Round(volume24hUsd / 1_000_000, 2)}m";

            string change1hSymbol = percentChange1h >= 0 ? "\U0001F4C8" : "\U0001F4C9";
            string change24hSymbol = percentChange24h >= 0 ? "\U0001F4C8" : "\U0001F4C9";
            string change7dSymbol = percentChange7d >= 0 ? "\U0001F4C8" : "\U0001F4C9";

            string indexEmoji = GetIndexEmoji(coinIndex);
            messageBuilder.AppendLine($"{indexEmoji} #{symbol}  |  <code>{symbol}</code>   |   价格：${price:F2}   |   No.{rank}");
            messageBuilder.AppendLine($"流通市值：{marketCapDisplay}  |  24小时交易：{volume24hDisplay}");
            messageBuilder.AppendLine($"RSI6: {rsi6:F2}  |  RSI14: {rsi14:F2}  |  m10： {m10:F2}");
            messageBuilder.AppendLine($"1h：{change1hSymbol}{percentChange1h:F2}%  |  24h：{change24hSymbol}{percentChange24h:F2}%  |  7d：{change7dSymbol}{percentChange7d:F2}%");
            if (coinIndex < uniqueSymbols.Count - 1) {
                messageBuilder.AppendLine("————————————————————");
            }

            currentRow.Add(InlineKeyboardButton.WithCallbackData(indexEmoji, $"查{symbol}"));
            if (currentRow.Count == 5)
            {
                buttonRows.Add(currentRow.ToArray());
                currentRow = new List<InlineKeyboardButton>();
            }
            coinIndex++;
        }
    }

    if (currentRow.Count > 0)
    {
        buttonRows.Add(currentRow.ToArray());
    }
    if (buttonRows.Count > 0)
    {
        allButtonRows.Add(buttonRows);
    }

    if (messageBuilder.Length > 0)
    {
        messages.Add(messageBuilder.ToString());
    }

    var inlineKeyboards = allButtonRows.Select(rows => new InlineKeyboardMarkup(rows)).ToList();

    return (messages, inlineKeyboards);
}
	
    // 获取RSI6和RSI14最低的前三个币种，但先筛选出近1小时、24小时和7天内下跌的币种
    public static async Task<(List<string>, List<InlineKeyboardMarkup>)> GetLowestRSICoinsAsync()
    {
        await CoinDataCache.EnsureCacheInitializedAsync(); // 确保缓存已初始化
        var allCoinsData = CoinDataCache.GetAllCoinsData();

        // 分别获取1小时、24小时和7天内下跌的前20名
        var topFallers1h = GetTopFallers(allCoinsData, "percent_change_1h").Take(20).ToList();
        var topFallers24h = GetTopFallers(allCoinsData, "percent_change_24h").Take(20).ToList();
        var topFallers7d = GetTopFallers(allCoinsData, "percent_change_7d").Take(20).ToList();

        // 合并并去重
        var uniqueSymbols = new HashSet<string>(topFallers1h.Concat(topFallers24h).Concat(topFallers7d));

        var rsi6Values = new List<(string Symbol, double RSI6)>();
        var rsi14Values = new List<(string Symbol, double RSI14)>();

        foreach (var symbol in uniqueSymbols)
        {
            await RandomDelay(); // 随机时间间隔，防止API限制
            string additionalInfo = await BinancePriceInfo.GetPriceInfo(symbol);

            var rsi6Match = Regex.Match(additionalInfo, @"RSI6:</b>\s*(\d+\.?\d*)");
            var rsi14Match = Regex.Match(additionalInfo, @"RSI14:</b>\s*(\d+\.?\d*)");

            if (double.TryParse(rsi6Match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double rsi6))
            {
                rsi6Values.Add((symbol, rsi6));
            }
            if (double.TryParse(rsi14Match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double rsi14))
            {
                rsi14Values.Add((symbol, rsi14));
            }
        }

        // 选择RSI6和RSI14最低的前三个
        var lowestRSI6 = rsi6Values.OrderBy(x => x.RSI6).Take(3).ToList();
        var lowestRSI14 = rsi14Values.OrderBy(x => x.RSI14).Take(3).ToList();

        // 合并并去重
        var finalSymbols = new HashSet<string>(lowestRSI6.Select(x => x.Symbol).Concat(lowestRSI14.Select(x => x.Symbol)));

        // 构建消息和键盘标记
        List<string> messages = new List<string>();
        List<List<InlineKeyboardButton[]>> allButtonRows = new List<List<InlineKeyboardButton[]>>();
        StringBuilder messageBuilder = new StringBuilder("⚠️ RSI最低值：\n\n");
        List<InlineKeyboardButton[]> buttonRows = new List<InlineKeyboardButton[]>();
        List<InlineKeyboardButton> currentRow = new List<InlineKeyboardButton>();
        int coinIndex = 0;

        foreach (var symbol in finalSymbols)
        {
            var coinData = allCoinsData[symbol];
            double rsi6 = rsi6Values.FirstOrDefault(x => x.Symbol == symbol).RSI6;
            double rsi14 = rsi14Values.FirstOrDefault(x => x.Symbol == symbol).RSI14;

            string indexEmoji = GetIndexEmoji(coinIndex);
            messageBuilder.AppendLine($"{indexEmoji} #{symbol} | <code>{symbol}</code> | RSI6: {rsi6:F2} | RSI14: {rsi14:F2}");

            currentRow.Add(InlineKeyboardButton.WithCallbackData(indexEmoji, $"查{symbol}"));
            if (currentRow.Count == 5)
            {
                buttonRows.Add(currentRow.ToArray());
                currentRow = new List<InlineKeyboardButton>();
            }
            coinIndex++;
        }

        if (currentRow.Count > 0)
        {
            buttonRows.Add(currentRow.ToArray());
        }
        if (buttonRows.Count > 0)
        {
            allButtonRows.Add(buttonRows);
        }

        if (messageBuilder.Length > 0)
        {
            messages.Add(messageBuilder.ToString());
        }

        var inlineKeyboards = allButtonRows.Select(rows => new InlineKeyboardMarkup(rows)).ToList();

        return (messages, inlineKeyboards);
    }
	
    // 获取近1小时和24小时内最多下跌的前20名币种
    public static async Task<(List<string>, List<InlineKeyboardMarkup>)> GetTopOversoldCoinsAsync()
    {
        await CoinDataCache.EnsureCacheInitializedAsync(); // 确保缓存已初始化

        var allCoinsData = CoinDataCache.GetAllCoinsData();
        var oversoldCoins = new List<(string Symbol, double Price, double PercentChange1h, double PercentChange24h, double PercentChange7d, double Volume24hUsd, double MarketCapUsd, double RSI6, double RSI14, double M10, int Rank)>();

        // 分别获取1小时和24小时和近7天内内下跌的前20名
        var topFallers1h = GetTopFallers(allCoinsData, "percent_change_1h").Take(20).ToDictionary(x => x, x => allCoinsData[x]["percent_change_1h"].GetDouble());
        var topFallers24h = GetTopFallers(allCoinsData, "percent_change_24h").Take(20).ToDictionary(x => x, x => allCoinsData[x]["percent_change_24h"].GetDouble());
	var topFallers7d = GetTopFallers(allCoinsData, "percent_change_7d").Take(20).ToDictionary(x => x, x => allCoinsData[x]["percent_change_7d"].GetDouble());   

        // 合并并去重
        var uniqueSymbols = new HashSet<string>(topFallers1h.Keys.Concat(topFallers24h.Keys).Concat(topFallers7d.Keys));

        foreach (var symbol in uniqueSymbols)
        {
            // 如果币种是TRX，则跳过不处理
            if (symbol.Equals("TRX", StringComparison.OrdinalIgnoreCase))
            {
                //Console.WriteLine($"跳过币种: {symbol}");
                continue;
            }

            // 随机时间间隔，防止API限制 200-300毫秒
            await Task.Delay(random.Next(200, 300));

            try
            {
                string additionalInfo = await BinancePriceInfo.GetPriceInfo(symbol);
                //Console.WriteLine($"处理币种: {symbol}, 获取到的数据: {additionalInfo}");

                var rsi6Match = Regex.Match(additionalInfo, @"RSI6:</b>\s*(\d+\.?\d*)");
                var rsi14Match = Regex.Match(additionalInfo, @"RSI14:</b>\s*(\d+\.?\d*)");
                var m10Match = Regex.Match(additionalInfo, @"m10：</b>\s*(\d+\.?\d*)");

                if (!double.TryParse(rsi6Match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double rsi6) ||
                    !double.TryParse(rsi14Match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double rsi14) ||
                    !double.TryParse(m10Match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double m10))
                {
                    throw new FormatException($"解析错误: 无法解析RSI6, RSI14, 或m10的值。币种: {symbol}");
                }

                if (rsi6 < 15 && rsi14 < 25)  //相对强弱指数  6天低于30 14天大于35  >
                {
                    var coinData = allCoinsData[symbol];
		    double percentChange1h = coinData["percent_change_1h"].GetDouble();
                    double percentChange24h = coinData["percent_change_24h"].GetDouble();	
                    double percentChange7d = coinData["percent_change_7d"].GetDouble();
                    double volume24hUsd = coinData["volume_24h_usd"].GetDouble();
                    double marketCapUsd = coinData["market_cap_usd"].GetDouble();
		    int rank = coinData["rank"].GetInt32();	
                    if (coinData.TryGetValue("price_usd", out JsonElement priceElement) && priceElement.TryGetDouble(out double priceValue))
                    {
                        oversoldCoins.Add((symbol, priceValue, percentChange1h, percentChange24h, percentChange7d, volume24hUsd, marketCapUsd, rsi6, rsi14, m10, rank));
                    }
                }
            }
            catch (FormatException ex)
            {
                //Console.WriteLine(ex.Message);
                continue;
            }
        }

    List<string> messages = new List<string>();
    List<List<InlineKeyboardButton[]>> allButtonRows = new List<List<InlineKeyboardButton[]>>();
    StringBuilder messageBuilder = new StringBuilder();
    int count = 0;

    if (oversoldCoins.Count > 0)
    {
        messageBuilder.Append("发现超卖：\n\n");
    }

    List<InlineKeyboardButton[]> buttonRows = new List<InlineKeyboardButton[]>();
    List<InlineKeyboardButton> currentRow = new List<InlineKeyboardButton>();
    int coinIndex = 0;

    foreach (var coin in oversoldCoins)
    {
        if (count % 20 == 0 && count > 0)
        {
            messages.Add(messageBuilder.ToString());
            allButtonRows.Add(buttonRows);
            messageBuilder = new StringBuilder();
            messageBuilder.Append("发现超卖：\n\n");
            buttonRows = new List<InlineKeyboardButton[]>();
            currentRow = new List<InlineKeyboardButton>();
        }

        string marketCapDisplay = coin.MarketCapUsd >= 100_000_000 ? $"{Math.Round(coin.MarketCapUsd / 100_000_000, 2)}亿" : $"{Math.Round(coin.MarketCapUsd / 1_000_000, 2)}m";
        string volume24hDisplay = coin.Volume24hUsd >= 100_000_000 ? $"{Math.Round(coin.Volume24hUsd / 100_000_000, 2)}亿" : $"{Math.Round(coin.Volume24hUsd / 1_000_000, 2)}m";

        string change1hSymbol = coin.PercentChange1h >= 0 ? "\U0001F4C8" : "\U0001F4C9";
        string change24hSymbol = coin.PercentChange24h >= 0 ? "\U0001F4C8" : "\U0001F4C9";
        string change7dSymbol = coin.PercentChange7d >= 0 ? "\U0001F4C8" : "\U0001F4C9";
	    
        string indexEmoji = GetIndexEmoji(coinIndex);
        messageBuilder.AppendLine($"{indexEmoji} #{coin.Symbol}  |  <code>{coin.Symbol}</code>   |   价格：${coin.Price}   |   <b>No.{coin.Rank}</b>");
        messageBuilder.AppendLine($"流通市值：{marketCapDisplay}  |  24小时交易：{volume24hDisplay}");
        messageBuilder.AppendLine($"RSI6: {coin.RSI6}  |  RSI14: {coin.RSI14}  |  m10： {coin.M10}");
        messageBuilder.AppendLine($"1h：{change1hSymbol}{coin.PercentChange1h:F2}%  |  24h：{change24hSymbol}{coin.PercentChange24h:F2}%  |  7d：{change7dSymbol}{coin.PercentChange7d:F2}%");	    

        if (coinIndex < oversoldCoins.Count - 1)
        {
            messageBuilder.AppendLine("————————————————————");
        }

        currentRow.Add(InlineKeyboardButton.WithCallbackData(indexEmoji, $"查{coin.Symbol}"));
        if (currentRow.Count == 5)
        {
            buttonRows.Add(currentRow.ToArray());
            currentRow = new List<InlineKeyboardButton>();
        }
        coinIndex++;
        count++;
    }

    if (currentRow.Count > 0)
    {
        buttonRows.Add(currentRow.ToArray());
    }
    if (buttonRows.Count > 0)
    {
        allButtonRows.Add(buttonRows);
    }

    if (messageBuilder.Length > 0)
    {
        messages.Add(messageBuilder.ToString());
    }

    var inlineKeyboards = allButtonRows.Select(rows => new InlineKeyboardMarkup(rows)).ToList();

    return (messages, inlineKeyboards);
    }
	
	
    private static string GetIndexEmoji(int index)
    {
        string[] emojis = { "0️⃣", "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "1️⃣0️⃣", "1️⃣1️⃣", "1️⃣2️⃣", "1️⃣3️⃣", "1️⃣4️⃣", "1️⃣5️⃣", "1️⃣6️⃣", "1️⃣7️⃣", "1️⃣8️⃣", "1️⃣9️⃣" };
        return emojis[index % 20]; // 循环使用表情符号
    }	
    // 辅助方法：获取下跌的币种
    private static IEnumerable<string> GetTopFallers(Dictionary<string, Dictionary<string, JsonElement>> coinData, string percentChangeKey)
    {
        return coinData
            .Select(kv => new
            {
                Symbol = kv.Key,
                PercentChange = kv.Value.TryGetValue(percentChangeKey, out JsonElement percentChangeElement) && percentChangeElement.TryGetDouble(out double percentChange) ? percentChange : double.MaxValue
            })
            .Where(x => x.PercentChange < 0) // 筛选出下跌的币种
            .OrderBy(x => x.PercentChange) // 按下跌幅度排序
            .Select(x => x.Symbol); // 选择币种符号
    }

    // 生成随机时间间隔的方法
    private static Task RandomDelay()
    {
        return Task.Delay(random.Next(200, 300)); // 设置随机时间间隔在200到300毫秒之间
    }
}

// 当机器人收到用户发：/duihuandbvip 时的处理逻辑，不扣除积分，只模拟发送"作者"消息
public static async Task SimulateSendingAuthorMessage(ITelegramBotClient botClient, Message message)
{
    long userId = message.From.Id;
    // 首先查询用户的积分
    if (userSignInInfo.TryGetValue(userId, out var userInfo))
    {
        int userPoints = userInfo.Points;
        // 如果积分低于99积分，则回复用户
        if (userPoints < 99)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("直接购买电报会员", "作者"));
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"兑换失败，您的积分为：{userPoints}，最低需要99积分方可兑换电报会员！\n" +
                      "您可以直接订阅电报会员：\n" +
                      "三个月电报会员 24.99 u\n" +
                      "六个月电报会员 39.99 u\n" +
                      "一年电报会员 70.99 u",
                replyMarkup: inlineKeyboard
            );
        }
else
{
    // 如果用户积分大于等于99积分，发送带有联系信息和按钮的消息
    string contactText = @"双向用户可以直接私聊机器人，作者会第一时间回复您！";
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        // 创建两个按钮：直接联系作者和由作者联系您
        InlineKeyboardButton.WithUrl("直接联系作者", "https://t.me/yifanfu"),
        InlineKeyboardButton.WithCallbackData("由作者联系您", "authorContactRequest")
    });

    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: contactText,
        replyMarkup: inlineKeyboard
    );
}
    }
    else
    {
        // 如果找不到用户的积分信息，可能是用户未签到
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "兑换失败，找不到您的积分信息，请确保已经通过签到获取积分。"
        );
    }
}
// 当机器人收到用户发：/duihuanprovip 时的处理逻辑
public static async Task ExchangeForProVip(ITelegramBotClient botClient, Message message)
{
    long userId = message.From.Id;
    // 首先启动查询用户的积分
    if (userSignInInfo.TryGetValue(userId, out var userInfo))
    {
        int userPoints = userInfo.Points;
        // 如果积分低于2积分，则回复用户
        if (userPoints < 2)
        {
            await botClient.SendTextMessageAsync(message.Chat.Id, $"兑换失败，您的积分为：{userPoints}，最低需要2积分方可兑换FF Pro会员！");
            return;
        }

        // 如果用户积分大于2积分，则计算最大的可被2整除的数字
        int maxDivisibleByTwo = userPoints - (userPoints % 2);
        int hoursToAuthorize = maxDivisibleByTwo / 2;

        // 立即扣除用户相应积分
        userSignInInfo[userId] = (userPoints - maxDivisibleByTwo, userInfo.LastSignInTime);

        // 模拟用户ID：1427768220 发送信息：授权 ***（用户ID） X小时
        string fakeAuthorizeCommand = $"授权 {userId} {hoursToAuthorize}小时";
        var fakeMessage = new Message
        {
            Chat = new Chat { Id = 1427768220 },
            From = new Telegram.Bot.Types.User { Id = 1427768220 },
            Text = fakeAuthorizeCommand
        };

        // 调用授权方法
        await VipAuthorizationHandler.AuthorizeVipUser(botClient, fakeMessage, 1427768220);

        // 回复用户兑换成功消息
        await botClient.SendTextMessageAsync(message.Chat.Id, $"兑换成功，您已获取 {hoursToAuthorize}小时 FF Pro会员！");
    }
    else
    {
        // 如果找不到用户的积分信息，可能是因为用户从未签到
        await botClient.SendTextMessageAsync(message.Chat.Id, "兑换失败，未找到您的积分信息，请确保已进行过签到操作。");
    }
}	
// 用户签到信息字典
private static ConcurrentDictionary<long, (int Points, DateTime LastSignInTime)> userSignInInfo = new ConcurrentDictionary<long, (int, DateTime)>();
	
// VIP用户字典		
private static Dictionary<long, bool> vipUsers = new Dictionary<long, bool>(); // VIP用户字典	
public static class VipAuthorizationHandler
{
    static VipAuthorizationHandler()
    {
        // 设置用户1427768220为永久VIP
        long permanentVipUserId = 1427768220;
        vipUsers[permanentVipUserId] = true;
        vipUserExpiryTimes[permanentVipUserId] = DateTime.MaxValue;
    }	
    private static Dictionary<long, bool> vipUsers = new Dictionary<long, bool>();
    private static ConcurrentDictionary<long, DateTime> vipUserExpiryTimes = new ConcurrentDictionary<long, DateTime>();
    private static ConcurrentDictionary<long, CancellationTokenSource> vipUserTimers = new ConcurrentDictionary<long, CancellationTokenSource>();
	
    // 公共静态方法，用于检查用户的VIP状态和到期时间
    public static bool TryGetVipExpiryTime(long userId, out DateTime expiryTime)
    {
        return vipUserExpiryTimes.TryGetValue(userId, out expiryTime);
    }
    // 新的公共静态方法，用于获取所有VIP用户的到期时间
    public static IDictionary<long, DateTime> GetAllVipUsersExpiryTime()
    {
        return vipUserExpiryTimes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }	
public static async Task AuthorizeVipUser(ITelegramBotClient botClient, Message message, long authorizedById)
{
    const long authorizingUserId = 1427768220; // 指定可以授权的用户ID
    if (message.From.Id != authorizingUserId)
    {
        return; // 如果消息不是来自指定的授权用户，则不进行任何操作
    }

    var parts = message.Text.Split(' ');
    if (parts.Length < 3)
    {
        return; // 确保命令格式正确
    }

    if (!long.TryParse(parts[1], out long userIdToAuthorize))
    {
        await botClient.SendTextMessageAsync(message.Chat.Id, "无效的用户ID。");
        return;
    }
    // 检查是否为“永久”授权
    if (parts[2].Equals("永久", StringComparison.OrdinalIgnoreCase))
    {
        // 设置用户为永久VIP
        SetPermanentVip(userIdToAuthorize);

        await botClient.SendTextMessageAsync(message.Chat.Id, $"用户 {userIdToAuthorize} 现在是永久VIP会员。");
        
        // 向被授权的用户发送永久VIP会员的消息
        try
        {
            await botClient.SendTextMessageAsync(
                chatId: userIdToAuthorize,
                text: "您已升级永久VIP会员！"
            );
        }
        catch (Exception ex)
        {
            // 发送失败，记录或处理异常
            //Console.WriteLine($"尝试向用户 {userIdToAuthorize} 发送永久VIP会员消息失败: {ex.Message}");
        }
        return;
    }
    var duration = ParseDuration(parts[2]);
    if (duration == TimeSpan.Zero)
    {
        await botClient.SendTextMessageAsync(message.Chat.Id, "无效的时间格式。");
        return;
    }

    DateTime newExpiryTime;
    TimeSpan additionalTime = duration;

    try
    {
        if (vipUserExpiryTimes.TryGetValue(userIdToAuthorize, out var existingExpiryTime) && DateTime.UtcNow < existingExpiryTime)
        {
            // 用户已是VIP且存在倒计时，累加时间
            newExpiryTime = existingExpiryTime.Add(additionalTime);
        }
        else
        {
            // 用户不是VIP或倒计时已结束，设置新的倒计时
            newExpiryTime = DateTime.UtcNow.Add(additionalTime);
        }
    }
    catch (ArgumentOutOfRangeException)
    {
        // 如果计算的新到期时间超出范围，则设置为DateTime的最大值
        newExpiryTime = DateTime.MaxValue;
        // 向用户发送消息，通知他们VIP状态已设置为最大可能值
        await botClient.SendTextMessageAsync(message.Chat.Id, "授权时间过长，已调整为最大可能值。");
        // 注意：这里可以选择直接返回，也可以继续执行后续逻辑
    }

    // 使用TimeZoneInfo将UTC时间转换为北京时间（UTC+8）
    TimeZoneInfo chinaZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
    DateTime beijingTime = TimeZoneInfo.ConvertTimeFromUtc(newExpiryTime, chinaZone);

    // 更新或设置新的倒计时
    if (vipUserTimers.TryGetValue(userIdToAuthorize, out var existingCts))
    {
        existingCts.Cancel();
    }
    existingCts = new CancellationTokenSource();
    vipUserTimers[userIdToAuthorize] = existingCts;

    vipUserExpiryTimes[userIdToAuthorize] = newExpiryTime; // 存储的是UTC时间
    vipUsers[userIdToAuthorize] = true; // 标记为VIP

    existingCts.CancelAfter(additionalTime);
    _ = Task.Delay(additionalTime, existingCts.Token).ContinueWith(task =>
    {
        if (!task.IsCanceled)
        {
            vipUsers.Remove(userIdToAuthorize);
            DateTime removedDateTime;
            vipUserExpiryTimes.TryRemove(userIdToAuthorize, out removedDateTime);
            CancellationTokenSource removedCts;
            vipUserTimers.TryRemove(userIdToAuthorize, out removedCts);
        }
    }, TaskScheduler.Default);
	
    // 向作者发送确认消息
    await botClient.SendTextMessageAsync(message.Chat.Id, $"用户 {userIdToAuthorize} 现在是VIP，授权时间累加至：{beijingTime:yyyy/MM/dd HH:mm:ss}。");
	
    // 尝试向被授权的用户发送消息
    try
    {
        await botClient.SendTextMessageAsync(
            chatId: userIdToAuthorize,
            text: $"您已成功升级vip，到期时间为：{beijingTime:yyyy/MM/dd HH:mm:ss}。"
        );
    }
    catch (Exception ex)
    {
        // 发送失败，记录或处理异常
        //Console.WriteLine($"尝试向用户 {userIdToAuthorize} 发送消息失败: {ex.Message}");
    }	
}
private static void SetPermanentVip(long userId)
{
    vipUsers[userId] = true;
    vipUserExpiryTimes[userId] = DateTime.MaxValue;

    // 如果存在计时器，则取消并移除
    if (vipUserTimers.TryGetValue(userId, out var existingCts))
    {
        existingCts.Cancel();
        vipUserTimers.TryRemove(userId, out _);
    }
}
    private static TimeSpan ParseDuration(string durationText)
    {
        // 使用正则表达式匹配数字和单位
        var match = Regex.Match(durationText, @"(\d+)\s*(分钟|小时|天)");
        if (!match.Success) return TimeSpan.Zero;

        int amount = int.Parse(match.Groups[1].Value);
        string unit = match.Groups[2].Value;

        switch (unit)
        {
            case "分钟":
                return TimeSpan.FromMinutes(amount);
            case "小时":
                return TimeSpan.FromHours(amount);
            case "天":
                return TimeSpan.FromDays(amount);
            default:
                return TimeSpan.Zero;
        }
    }
}
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
                    coin["volume_24h_usd"].GetDecimal() >= coin["market_cap_usd"].GetDecimal() * 0.5m && //24小时成交量占比市值>50%
                    coin["percent_change_24h"].GetDecimal() > 5m && //24小时涨幅大于5%
                    coin["percent_change_24h"].GetDecimal() <= 20m && //24小时涨幅小于20%
                    //coin["percent_change_1h"].GetDecimal() > 0m &&  //近1小时涨幅大于0%  不想要比特币数据直接： 0m) //近1小时涨幅大于0%
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
                  new[] // 第一排按钮
                  {
                      InlineKeyboardButton.WithCallbackData("查BTC", "查BTC"),
                      //InlineKeyboardButton.WithCallbackData("查ETH", "查ETH"), // 这行被注释掉了
                      InlineKeyboardButton.WithCallbackData("自定义查询", "/genjuzhiding"),
                  },
                  new[] // 第二排按钮
                  {
                      InlineKeyboardButton.WithCallbackData("订阅超卖信号", "/dingyuersi"),
                      InlineKeyboardButton.WithCallbackData("查询突破信号", "/shiwukxian")			      
                  }		    
                });		    
		    
                string noDataMessage = "暂未发现财富密码，持续监控中...\n\n" +
                           "判断标准：\n" +
                           //"近1小时涨幅大于0%\n" +
                           "24小时成交量占比市值>50%\n" +
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

                string message = $"#{coin.Symbol} | <code>{coin.Symbol}</code>  价格:$ {coin.PriceUsd} 排名：No.{coin.Rank} {fundingRateDisplay}\n" +
                                 $"市值：{marketCapDisplay}，24小时成交：{volume24hDisplay}，占比：{Math.Round(coin.VolumePercentage, 2)}%\n" +
                                 $"1h{change1hSymbol}：{coin.PercentChange1h}% | 24h{change24hSymbol}：{coin.PercentChange24h}% | 7d{change7dSymbol}：{coin.PercentChange7d}%";

                // 创建内联键盘
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] // 第一排按钮
                    {
                        InlineKeyboardButton.WithUrl("合约数据", "https://www.coinglass.com/zh/BitcoinOpenInterest"),
                        InlineKeyboardButton.WithCallbackData($"{coin.Symbol}详细数据", $"查 {coin.Symbol}")
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
                                $"1小时涨幅榜：\n{string.Join(" | ", top3CoinsBy1hChange.Select((coin, index) => $"{index + 1}️⃣ <code>{coin.Symbol}</code> ：{coin.Change:F2}%"))}\n\n" +
                                $"24小时涨幅榜：\n{string.Join(" | ", top3CoinsBy24hChange.Select((coin, index) => $"{index + 4}️⃣ <code>{coin.Symbol}</code> ：{coin.Change:F2}%"))}";

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
                //InlineKeyboardButton.WithCallbackData("查ETH", "查ETH"),		    
                InlineKeyboardButton.WithCallbackData("自定义查询", "/genjuzhiding"),
		//InlineKeyboardButton.WithCallbackData("订阅超卖信号", "/dingyuersi")		
            },
            new[] // 第二排按钮
            {
                InlineKeyboardButton.WithCallbackData("订阅超卖信号", "/dingyuersi"),
                InlineKeyboardButton.WithCallbackData("查询突破信号", "/shiwukxian")
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
	
//非小号查币  本地缓存系统  
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
public static async Task QueryCoinInfoAsync(ITelegramBotClient botClient, long chatId, string coinSymbol, bool sendNotFoundMessage = true)
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
            if (sendNotFoundMessage)
            {
		//Console.WriteLine("No data found for the requested symbol.");
                await botClient.SendTextMessageAsync(chatId, "未查到该币种的信息！", ParseMode.Html);
            }
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
            string additionalInfo = await BinancePriceInfo.GetPriceInfo(coinSymbol);

            string marketCapDisplay = marketCapUsd >= 100_000_000 ? $"{Math.Round(marketCapUsd / 100_000_000, 2)}亿" : $"{Math.Round(marketCapUsd / 1_000_000, 2)}m";
            string volume24hDisplay = volume24hUsd >= 100_000_000 ? $"{Math.Round(volume24hUsd / 100_000_000, 2)}亿" : $"{Math.Round(volume24hUsd / 1_000_000, 2)}m";

            string change1hSymbol = percentChange1h >= 0 ? "\U0001F4C8" : "\U0001F4C9";
            string change24hSymbol = percentChange24h >= 0 ? "\U0001F4C8" : "\U0001F4C9";
            string change7dSymbol = percentChange7d >= 0 ? "\U0001F4C8" : "\U0001F4C9";

            string message = $"<code>{symbol}</code> 价格：$ {priceUsd}  No.<b>{rank}</b>\n" +
                             $"流通总市值：${marketCapDisplay}  \n" +
                             $"24小时成交：${volume24hDisplay}\n" +
                             $"1小时{change1hSymbol}：{percentChange1h}%\n" +
                             $"2 4 时{change24hSymbol}：{percentChange24h}%\n" +
                             $"近7天{change7dSymbol}：{percentChange7d}%\n\n"+
	                     additionalInfo; // 将额外信息拼接到消息中

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
	InlineKeyboardButton.WithCallbackData($"监控 {symbol}", $"监控 {symbol}"), // 添加监控按钮   
	InlineKeyboardButton.WithCallbackData("成交量查询", $"成交量 {symbol}"),
	InlineKeyboardButton.WithCallbackData("关闭", "back")
    }
});

        await botClient.SendTextMessageAsync(chatId, message, ParseMode.Html,disableWebPagePreview: true,replyMarkup: keyboard);
        
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
//监控用户进出群消息	
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
                    string usernameOrId = newUser.Username != null ? "@" + newUser.Username : "ID:" + newUser.Id.ToString();
                    string msg = $"{displayName} {usernameOrId} 欢迎进群！";
                    var sentMessage = await botClient.SendTextMessageAsync(message.Chat.Id, msg);
                    await Task.Delay(1000); // 等待1秒
                    await botClient.DeleteMessageAsync(message.Chat.Id, sentMessage.MessageId); // 尝试撤回消息
                }
            }
        }
        else if (message.Type == MessageType.ChatMemberLeft)
        {
            var leftUser = message.LeftChatMember;
            if (leftUser != null && !leftUser.IsBot) // 确保不是机器人离开
            {
                string displayName = leftUser.FirstName + (leftUser.LastName != null ? " " + leftUser.LastName : "");
                string usernameOrId = leftUser.Username != null ? "@" + leftUser.Username : "ID:" + leftUser.Id.ToString();
                string msg = $"{displayName} {usernameOrId} 离开群组！";
                var sentMessage = await botClient.SendTextMessageAsync(message.Chat.Id, msg);
                await Task.Delay(1000); // 等待1秒
                await botClient.DeleteMessageAsync(message.Chat.Id, sentMessage.MessageId); // 尝试撤回消息
            }
        }
    }
    catch (Telegram.Bot.Exceptions.ApiRequestException ex)
    {
        Console.WriteLine($"无法发送或撤回消息: {ex.Message}");
    }
    catch (Exception ex)
    {
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
+ string.Join("\n", finalTopGainers.Select((g, index) => $"{index}️⃣  #{g.Symbol} | <code>{g.Symbol}</code> \U0001F4C8：{g.ChangePercent:F2}%，${FormatPrice(g.CurrentPrice.ToString())}").Take(5))
+ "\n\n<b>急速下跌：</b>\n" 
+ string.Join("\n", finalTopLosers.Select((l, index) => $"{index + 5}️⃣  #{l.Symbol} | <code>{l.Symbol}</code> \U0001F4C9{l.ChangePercent:F2}%，${FormatPrice(l.CurrentPrice.ToString())}").Take(5))
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

// 添加原有的按钮和新按钮
rows.Add(new InlineKeyboardButton[] {
    InlineKeyboardButton.WithCallbackData("订阅超卖信号", "/dingyuersi"),
    InlineKeyboardButton.WithCallbackData("查询突破信号", "/shiwukxian")
});

rows.Add(new InlineKeyboardButton[] {
    InlineKeyboardButton.WithCallbackData("市值TOP50 大数据", "feixiaohao")
});

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
        InlineKeyboardButton.WithCallbackData($"成交量查询", $"成交量 {symbol}"),
        InlineKeyboardButton.WithCallbackData($"监控 {symbol} ", $"监控 {symbol}")
    }
});

// 使用内联键盘发送消息
await botClient.SendTextMessageAsync(chatId, reply, ParseMode.Html, replyMarkup: inlineKeyboard);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"查询时发生错误：{ex.Message}");
        //await botClient.SendTextMessageAsync(chatId, $"查询时发生错误：{ex.Message}");
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
	
public static async Task<Stream> FetchImageAsync(string url)
{
    using (var httpClient = new HttpClient())
    {
        try
        {
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                if (stream.Length == 0)
                {
                    Console.WriteLine("Received empty stream from image URL.");
                    return null;
                }
                return stream;
            }
            else
            {
                Console.WriteLine($"Failed to fetch image. Status code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception when fetching image: {ex.Message}");
        }
    }
    return null;
}
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
/*   
//旧版香港六合彩获取api
public static async Task<string> FetchHongKongLotteryResultAsync()
{
    try
    {
        var response = await client.GetAsync("https://kclm.site/api/trial/drawResult?code=hk6&format=json&rows=50");
        if (!response.IsSuccessStatusCode)
        {
            return "error_hk"; // 统一返回错误标识
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
        return "error_hk"; // 统一返回错误标识
    }
}
*/
public static async Task<string> FetchHongKongLotteryResultAsync()
{
    try
    {
        // 使用新的API地址
        var response = await client.GetAsync("https://www.macaumarksix.com/api/hkjc.com");
        if (!response.IsSuccessStatusCode)
        {
            return "error_hk"; // 如果请求失败，返回错误标识
        }

        var jsonString = await response.Content.ReadAsStringAsync();
        var jsonArray = JArray.Parse(jsonString);
        var latestResult = jsonArray[0];

        // 解析新的API返回的数据
        var issue = latestResult["expect"].ToString();
        var drawTime = DateTime.Parse(latestResult["openTime"].ToString());
        var drawResult = latestResult["openCode"].ToString().Split(',');
        var zodiacs = latestResult["zodiac"].ToString().Split(',');
        var colors = latestResult["wave"].ToString().Split(',');

        // 格式化开奖号码，确保双位数显示，并将最后一个号码用逗号分隔
        var formattedDrawResult = string.Join("  ", drawResult.Take(drawResult.Length - 1).Select(x => x.PadLeft(2, '0'))) + " ，" + drawResult.Last().PadLeft(2, '0');
        var formattedZodiacs = string.Join("  ", zodiacs);

        // 将颜色英文单词转换为emoji
        var colorEmojiMap = new Dictionary<string, string>
        {
            {"red", "\uD83D\uDD34"},
            {"green", "\uD83D\uDFE2"},
            {"blue", "\uD83D\uDD35"}
        };
        var formattedColors = string.Join("  ", colors.Select(color => colorEmojiMap[color]));

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
        return "error_hk"; // 如果发生异常，返回错误标识
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
                return "error_new"; // 特定于新澳门六合彩的错误标识
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
            return "error_new"; // 特定于新澳门六合彩的错误标识
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
                    return "error"; 
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
                return "error"; 
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

    // 移除用户的备注信息
    userAddressNotes.Remove((userId, tronAddress));

    // 移除失败计数器
    userNotificationFailures.Remove((userId, tronAddress));

    Console.WriteLine($"已停止监控并清理用户 {userId} 的所有相关数据。");
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
// 定义一个全局变量来存储下一次请求的间隔时间
private static TimeSpan nextRequestInterval = TimeSpan.FromSeconds(5);	
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

    // 启动定时器，使用动态时间间隔检查新的交易记录
    Timer timer = new Timer(async _ =>
    {
        await CheckForNewTransactions(botClient, userId, tronAddress);
    }, null, nextRequestInterval, nextRequestInterval);

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

        // 获取对方地址的风险标签
        string counterpartyAddress = isOutgoing ? transaction.To : transaction.From;
        string riskLabel = await FetchAddressLabelAsync(counterpartyAddress);
        string riskMessage = string.IsNullOrEmpty(riskLabel) 
            ? "对方标签：<b>无风险</b>" 
            : $"对方标签：<b>{riskLabel}</b>";

		var transactionUrl = $"https://tronscan.org/#/transaction/{transaction.TransactionId}";    

                var message = $"<b>新交易   \U0001F4B0  {transactionSign}{amount} USDT</b> \n\n" +
                              $"交易类型：<b>{transactionType}</b>\n" +
                              $"{transactionType}金额：<b>{amount}</b>\n" +
                              $"交易时间：<b>{transactionTime}</b>\n" +
                              $"监听地址： <code>{address}</code>\n" +
                              $"地址备注：<b>{note}</b>\n" + // 插入备注信息
                              $"地址余额：<b>{userUsdtBalance.ToString("#,##0.##")} USDT</b><b>  |  </b><b>{userTrxBalance.ToString("#,##0.##")} TRX</b>\n" +
                              $"------------------------------------------------------------------------\n" +
                              $"对方地址： <code>{(isOutgoing ? transaction.To : transaction.From)}</code>\n" +
                              $"对方余额：<b>{counterUsdtBalance.ToString("#,##0.##")} USDT</b><b>  |  </b><b>{counterTrxBalance.ToString("#,##0.##")} TRX</b>\n" +   
                              $"{riskMessage}\n\n" + // 添加风险提示 
			      //$"------------------------------------------------------------------------\n" +
                              $"<a href=\"{transactionUrl}\">交易详情：</a><b>{transactionFee.ToString("#,##0.######")} TRX    {feePayer}</b>\n\n" + // 根据交易方向调整文本
			      $"<a href=\"https://t.me/lianghaonet/8\">1️⃣一个独特的靓号地址是您个性与财富的象征！</a>\n" +
                              $"<a href=\"https://dupay.one/web-app/register-h5?invitCode=625174&lang=zh-cn\">2️⃣USDT消费卡,无需实名即可使用,免冻卡风险！</a>\n" +
                              $"<a href=\"https://t.me/yifanfubot\">3️⃣提前租赁能量，交易费用即刻降至 {TransactionFee} TRX！</a>\n"; // 修改后的两行文字
		    
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new [] // first row
                    {
			//InlineKeyboardButton.WithCallbackData("地址备注", $"set_note,{address}"),    
                        //InlineKeyboardButton.WithUrl("交易详情", transactionUrl)
                        InlineKeyboardButton.WithCallbackData("查自己", $"query_self,{address}"),
                        InlineKeyboardButton.WithCallbackData("查对方", $"query_other,{(isOutgoing ? transaction.To : transaction.From)}"), 	
			InlineKeyboardButton.WithCallbackData("地址备注", $"set_note,{address}") 	
                    },
                    //new [] // first row
                   // {
                   //     //InlineKeyboardButton.WithCallbackData("查自己", $"query_self,{address}"),
                  //      //InlineKeyboardButton.WithCallbackData("查对方", $"query_other,{(isOutgoing ? transaction.To : transaction.From)}")
		//	InlineKeyboardButton.WithCallbackData("地址备注", $"set_note,{address}"),    
                  //      InlineKeyboardButton.WithUrl("交易详情", transactionUrl)				
                 //   },   
                    new [] // first row
                    {
                        InlineKeyboardButton.WithCallbackData("消费U卡", "energy_introo"), // 新增的按钮				    
                        InlineKeyboardButton.WithCallbackData("租赁能量", "energy_intro"), // 新增的按钮	
                        InlineKeyboardButton.WithUrl("靓号地址", "https://t.me/lianghaonet/8")				
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
    Console.WriteLine($"发送通知失败：{ex.Message}. 用户 {userId} 已阻止机器人，即将停止监控并移除相关数据。");

    // 停止监控并清理资源
    StopUSDTMonitoring(userId, tronAddress);

    // 从失败计数器字典中移除该用户
    userNotificationFailures.Remove((userId, tronAddress));
}
    catch (ApiRequestException ex) when (ex.Message.Contains("Too Many Requests: retry after"))
    {
        var match = Regex.Match(ex.Message, @"Too Many Requests: retry after (\d+)");
        if (match.Success)
        {
            var retryAfterSeconds = int.Parse(match.Groups[1].Value);
            //Console.WriteLine($"发送通知失败：{ex.Message}. 将在{retryAfterSeconds}秒后重试。");
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
        // 重置请求间隔为默认值
        nextRequestInterval = TimeSpan.FromSeconds(5);	    
    }
    catch (Exception ex)
    {
        //Console.WriteLine($"Error in method {nameof(CheckForNewTransactions)}: {ex.Message}");
        HandleException(ex);
    }
}
private static void HandleException(Exception ex)
{
    if (ex.Message.Contains("request rate exceeded"))
    {
        var match = Regex.Match(ex.Message, @"suspended for (\d+) s");
        if (match.Success)
        {
            int suspendTime = int.Parse(match.Groups[1].Value);
            nextRequestInterval = TimeSpan.FromSeconds(suspendTime + 1);
            //Console.WriteLine($"请求频率过高，调整请求间隔为 {nextRequestInterval.TotalSeconds} 秒。");
        }
    }
    else
    {
        // 处理其他类型的异常
        Console.WriteLine($"处理异常时发生错误：{ex.Message}");
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
    private static Dictionary<string, string> priceCache = new Dictionary<string, string>();
    private static DateTime lastUpdateTime = DateTime.MinValue;

    public static async Task<string> GetUsdtPriceAsync(string userCommand)
    {
        try
        {
            // 检查缓存是否有效
            if (DateTime.Now - lastUpdateTime < TimeSpan.FromSeconds(600) && priceCache.ContainsKey("result"))
            {
                //Console.WriteLine("仓库数据有效，使用仓库数据...");
                return priceCache["result"];
            }

            //Console.WriteLine("仓库无数据，立即从api获取...");
            using (var httpClient = new HttpClient())
            {
                //Console.WriteLine("正在获取买入价格...");
                var buyResponse = await httpClient.GetStringAsync(BuyApi);
                var buyData = JsonDocument.Parse(buyResponse).RootElement.GetProperty("data").GetProperty("buy").EnumerateArray().Take(5);

                //Console.WriteLine("正在获取售出价格...");
                var sellResponse = await httpClient.GetStringAsync(SellApi);
                var sellData = JsonDocument.Parse(sellResponse).RootElement.GetProperty("data").GetProperty("sell").EnumerateArray().Take(5);

                string result = "<b>okx实时U价 TOP5</b> \n\n";
                result += "<b>buy：</b>\n";
                string[] emojis = new string[] { "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣" };
                int count = 0;
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
                {
                    string publicUserId = item.GetProperty("publicUserId").GetString();
                    string merchantUrl = $"https://www.okx.com/cn/p2p/ads-merchant?publicUserId={publicUserId}";
                    result += $"{emojis[count]}：{item.GetProperty("price")}   <a href=\"{merchantUrl}\">{item.GetProperty("nickName")}</a>\n";
                    count++;
                }

                // 添加当前查询时间（北京时间）
                var beijingTime = DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(8));
                result += $"\n查询时间：{beijingTime:yyyy-MM-dd HH:mm:ss}";

                // 更新缓存
                priceCache["result"] = result;
                lastUpdateTime = DateTime.Now;

                // 设置随机时间更新缓存
                int delaySeconds = new Random().Next(550, 600);
                //Console.WriteLine($"设置倒计时{delaySeconds}秒更新获取数据");
                Task.Delay(delaySeconds * 1000).ContinueWith(t => priceCache.Clear());

                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            // 清空缓存并停止倒计时
            priceCache.Clear();
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
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
        });
        await botClient.SendTextMessageAsync(
            chatId: userId,
            text: "TRX价格变动请进交流群查看！",
            replyMarkup: inlineKeyboard
        );
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

                // 获取RSI6值
                var rsiInfo = await BinancePriceInfo.GetPriceInfo(monitorInfo.Symbol);
                var rsi6Match = Regex.Match(rsiInfo, @"RSI6:</b>\s*(\d+\.?\d*)");
                string rsi6 = rsi6Match.Success ? rsi6Match.Groups[1].Value : null;
		    
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
                messageBuilder.AppendLine($"<b>价格变动</b>：{(change > 0 ? "\U0001F4C8" : "\U0001F4C9")}  {change:P}\n");

                // 添加RSI6信息
                if (rsi6 != null)
                {
                    messageBuilder.AppendLine($"<b>RSI6指标</b>：{rsi6}");
                }
		    
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
                    InlineKeyboardButton.WithCallbackData($"取消监控 {monitorInfo.Symbol}", $"unmonitor_{monitorInfo.Symbol}"),
                    InlineKeyboardButton.WithCallbackData($"详情", $"查 {monitorInfo.Symbol}")			    
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
    var amounts = new decimal[] { 50, 80, 100, 150, 200, 250, 300, 400, 500, 800, 1000 };
    var addressChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    bool hasSentAdInQuietHours = false;
    while (!cancellationToken.IsCancellationRequested)
    {
        cancellationToken.ThrowIfCancellationRequested(); // 检查取消请求

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
        var rate = await rateRepository.Where(x => x.Currency == Currency.USDT && x.ConvertCurrency == Currency.TRX).FirstAsync(x => x.Rate, cancellationToken); // 确保查询支持取消
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
                // 如果您想发送带有按钮的消息，取消下面这行的注释
                // await botClient.SendTextMessageAsync(chatId: groupId, text: advertisementText, parseMode: ParseMode.Html, replyMarkup: inlineKeyboard, cancellationToken: cancellationToken); // 发送带按钮的消息

                // 如果您不需要发送按钮，使用下面这行
                await botClient.SendTextMessageAsync(chatId: groupId, text: advertisementText, parseMode: ParseMode.Html, replyMarkup: null, cancellationToken: cancellationToken); // 确保发送消息支持取消
            }
            catch
            {
                // 如果在尝试发送消息时出现错误，就从 groupIds 列表中移除这个群组
                GroupManager.RemoveGroupId(groupId);
                // 然后继续下一个群组，而不是停止整个任务
                continue;
            }
        }

        // 在3600-4000秒内随机等待
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

    // 获取币安近48小时的成交量信息
    public static async Task<string> GetHourlyTradingVolume(string symbol)
    {
        string futuresUrl = $"https://fapi.binance.com/fapi/v1/klines?symbol={symbol.ToUpper()}USDT&interval=1h&limit=48";
        string spotUrl = $"https://api.binance.com/api/v3/klines?symbol={symbol.ToUpper()}USDT&interval=1h&limit=48";
        StringBuilder result = new StringBuilder();
        string dataSource = "币安合约"; // 默认数据来源

        try
        {
            var response = await httpClient.GetAsync(futuresUrl);
            if (!response.IsSuccessStatusCode)
            {
                // 尝试从现货市场获取数据
                response = await httpClient.GetAsync(spotUrl);
                dataSource = "币安现货"; // 更改数据来源
            }
        if (!response.IsSuccessStatusCode)
        {
            // 如果币安合约和现货都无法获取数据，尝试从欧易合约获取
            string okxResult = await GetOkxHourlyTradingVolume(symbol);
            if (string.IsNullOrEmpty(okxResult))
            {
                Console.WriteLine("从欧易API也未获取到数据");
                return null; // 确保这里返回null，以便后续处理
            }
            return okxResult;
        }
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var klines = JsonSerializer.Deserialize<List<List<JsonElement>>>(content);
                if (klines != null && klines.Count == 48)
                {
                    // 添加表头
                    result.AppendLine(" |            时间            |   成交量  |  涨跌幅");

                    for (int i = 47; i >= 36; i--) // 只返回最近12小时的数据
                    {
                        decimal currentVolume = decimal.Parse(klines[i][7].GetString());
                        decimal previousVolume = decimal.Parse(klines[i - 1][7].GetString());
                        decimal changePercent = (currentVolume - previousVolume) / previousVolume * 100;
                        string emoji = changePercent >= 0 ? "\U0001F4C8" : "\U0001F4C9";

                        string formattedVolume = FormatLargeNumber(currentVolume);
                        string timeLabel = DateTimeOffset.FromUnixTimeMilliseconds(klines[i][0].GetInt64()).AddHours(8).ToString("yyyy/MM/dd HH:mm"); // 转换为北京时间

                        result.AppendLine($"{timeLabel} | {formattedVolume} | {emoji} {changePercent:F2}%");
                    }

                    // 添加一个空行作为分隔
                    result.AppendLine();

// 添加计算4/8/12/24小时的成交量和涨幅
result.AppendLine($"4h：{FormatLargeNumber(CalculatePeriodVolume(klines, 4, 47))} | 同比：{FormatLargeNumber(CalculatePeriodVolume(klines, 4, 43))} | {CalculatePeriodChange(klines, 4, 47, 43)}");
result.AppendLine($"8h：{FormatLargeNumber(CalculatePeriodVolume(klines, 8, 47))} | 同比：{FormatLargeNumber(CalculatePeriodVolume(klines, 8, 39))} | {CalculatePeriodChange(klines, 8, 47, 39)}");
result.AppendLine($"12h：{FormatLargeNumber(CalculatePeriodVolume(klines, 12, 47))} | 同比：{FormatLargeNumber(CalculatePeriodVolume(klines, 12, 35))} | {CalculatePeriodChange(klines, 12, 47, 35)}");
result.AppendLine($"24h：{FormatLargeNumber(CalculatePeriodVolume(klines, 24, 47))} | 同比：{FormatLargeNumber(CalculatePeriodVolume(klines, 24, 23))} | {CalculatePeriodChange(klines, 24, 47, 23)}");

                    // 添加一个空行作为分隔
                    result.AppendLine();
			
                    // 添加数据来源和查询时间
                    string queryTime = DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy/MM/dd HH:mm:ss");
                    result.AppendLine($"数据来源：{dataSource} | {queryTime}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"从币安API获取数据失败: {ex.Message}");
            return "查询失败，该币种暂未上架币安/欧意交易所";
        }

        return result.ToString();
    }
// 获取欧意近48小时的成交量信息
public static async Task<string> GetOkxHourlyTradingVolume(string symbol)
{
    string swapUrl = $"https://www.okx.com/api/v5/market/candles?instId={symbol.ToUpper()}-USDT-SWAP&bar=1H&limit=48";
    string spotUrl = $"https://www.okx.com/api/v5/market/candles?instId={symbol.ToUpper()}-USDT&bar=1H&limit=48";
    StringBuilder result = new StringBuilder();
    string dataSource = "欧易合约"; // 数据来源

    try
    {
        HttpResponseMessage response = await httpClient.GetAsync(swapUrl);
        //Console.WriteLine($"欧易合约API尝试: {swapUrl}"); // 调试输出
        string content = await response.Content.ReadAsStringAsync();
       // Console.WriteLine($"API响应: {response.StatusCode}"); // 调试输出
        //Console.WriteLine($"欧易合约API返回: {content}"); // 调试输出

        var jsonDoc = JsonDocument.Parse(content);
        if (jsonDoc.RootElement.GetProperty("code").GetString() == "51001")
        {
            //Console.WriteLine($"欧易合约查询不到，尝试使用欧意现货API: {spotUrl}"); // 调试输出
            response = await httpClient.GetAsync(spotUrl);
            content = await response.Content.ReadAsStringAsync();
            dataSource = "欧易现货"; // 更新数据来源
           // Console.WriteLine($"欧意现货API返回: {content}"); // 调试输出
        }

        if (response.IsSuccessStatusCode)
        {
            jsonDoc = JsonDocument.Parse(content);
            var elements = jsonDoc.RootElement.GetProperty("data").EnumerateArray();
            List<List<JsonElement>> klines = elements.Select(element => element.EnumerateArray().Select(x => x).ToList()).Reverse().ToList();

            if (klines.Count >= 48) // 确保有足够的数据点
            {
                // 添加表头
                result.AppendLine(" |            时间            |   成交量  |  涨跌幅");

                // 只返回最近12小时的数据，倒序输出
                for (int i = 47; i >= 36; i--)
                {
                    decimal currentVolume = decimal.Parse(klines[i][7].ToString());
                    decimal previousVolume = decimal.Parse(klines[i - 1][7].ToString());
                    decimal changePercent = (currentVolume - previousVolume) / previousVolume * 100;
                    string emoji = changePercent >= 0 ? "\U0001F4C8" : "\U0001F4C9";

                    string formattedVolume = FormatLargeNumber(currentVolume);
                    string timeLabel = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(klines[i][0].ToString())).AddHours(8).ToString("yyyy/MM/dd HH:mm"); // 转换为北京时间

                    result.AppendLine($"{timeLabel} | {formattedVolume} | {emoji} {changePercent:F2}%");
                }

                // 添加一个空行作为分隔
                result.AppendLine();

// 添加计算4/8/12/24小时的成交量和涨幅
result.AppendLine($"4h：{FormatLargeNumber(CalculatePeriodVolume(klines, 4, 47))} | 同比：{FormatLargeNumber(CalculatePeriodVolume(klines, 4, 43))} | {CalculatePeriodChange(klines, 4, 47, 43)}");
result.AppendLine($"8h：{FormatLargeNumber(CalculatePeriodVolume(klines, 8, 47))} | 同比：{FormatLargeNumber(CalculatePeriodVolume(klines, 8, 39))} | {CalculatePeriodChange(klines, 8, 47, 39)}");
result.AppendLine($"12h：{FormatLargeNumber(CalculatePeriodVolume(klines, 12, 47))} | 同比：{FormatLargeNumber(CalculatePeriodVolume(klines, 12, 35))} | {CalculatePeriodChange(klines, 12, 47, 35)}");
result.AppendLine($"24h：{FormatLargeNumber(CalculatePeriodVolume(klines, 24, 47))} | 同比：{FormatLargeNumber(CalculatePeriodVolume(klines, 24, 23))} | {CalculatePeriodChange(klines, 24, 47, 23)}");

                // 添加一个空行作为分隔
                result.AppendLine();

                // 添加数据来源和查询时间
                string queryTime = DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy/MM/dd HH:mm:ss");
                result.AppendLine($"数据来源：{dataSource} | {queryTime}");
            }
            else
            {
                Console.WriteLine("查询失败，该币种暂未上架币安/欧意交易所");
                return "查询失败，该币种暂未上架币安/欧意交易所";
            }
        }
        else
        {
            Console.WriteLine("API响应失败");
            return "查询失败，该币种暂未上架币安/欧意交易所";
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"从欧易API获取数据失败: {ex.Message}");
        return "查询失败，该币种暂未上架币安/欧意交易所";
    }

    return result.ToString();
}
// 计算指定时间段的成交量
private static decimal CalculatePeriodVolume(List<List<JsonElement>> klines, int hours, int endIndex)
{
    int startIndex = endIndex - hours + 1;
    decimal periodVolume = 0;

    for (int i = startIndex; i <= endIndex; i++)
    {
        periodVolume += decimal.Parse(klines[i][7].GetString());
    }

    return periodVolume;
}

// 计算指定时间段的涨跌幅
private static string CalculatePeriodChange(List<List<JsonElement>> klines, int hours, int currentEndIndex, int previousEndIndex)
{
    decimal currentPeriodVolume = CalculatePeriodVolume(klines, hours, currentEndIndex);
    decimal previousPeriodVolume = CalculatePeriodVolume(klines, hours, previousEndIndex);
    decimal changePercent = ((currentPeriodVolume - previousPeriodVolume) / previousPeriodVolume) * 100;
    string emoji = changePercent >= 0 ? "\U0001F4C8" : "\U0001F4C9";

    return $"{emoji} {changePercent:F2}%";
}
// 获取昨日和今日的交易量
public static async Task<string> GetTradingVolumeInfo(string symbol)
{
    string url = $"https://fapi.binance.com/fapi/v1/klines?symbol={symbol.ToUpper()}USDT&interval=1d&limit=2";
    try
    {
        var response = await httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var klines = JsonSerializer.Deserialize<List<List<JsonElement>>>(content);
            if (klines != null && klines.Count == 2)
            {
                // 将时间戳和成交量从object转换为适当的类型
                long yesterdayTimestamp = klines[0][0].GetInt64();
                decimal yesterdayVolume = decimal.Parse(klines[0][7].GetString());
                long todayTimestamp = klines[1][0].GetInt64();
                decimal todayVolume = decimal.Parse(klines[1][7].GetString());
                decimal changePercent = (todayVolume - yesterdayVolume) / yesterdayVolume * 100;

                // 使用辅助方法格式化输出
                string formattedYesterdayVolume = FormatLargeNumber(yesterdayVolume);
                string formattedTodayVolume = FormatLargeNumber(todayVolume);

                // 根据涨幅正负选择表情符号
                string emoji = changePercent >= 0 ? "\U0001F4C8" : "\U0001F4C9";

                return $"<b>昨日成交：</b>{formattedYesterdayVolume} | <b>今日成交：</b>{formattedTodayVolume} | <b>涨幅：</b>{emoji}{changePercent.ToString("F2")}%";
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"从币安合约获取数据失败: {ex.Message}");
    }

    return "无法获取数据"; // 如果API调用失败或数据解析失败
}
// 辅助方法，用于格式化大数值
private static string FormatLargeNumber(decimal number)
{
    if (number >= 100000000) // 大于等于1亿
    {
        return (number / 100000000).ToString("F2") + "亿";
    }
    else if (number >= 10000) // 大于等于1万
    {
        return (number / 10000).ToString("F2") + "万";
    }
    return number.ToString("F2");
}
// 交易量响应类
public class TradingVolumeResponse
{
    public string volume { get; set; } // 昨日交易量
    public string quoteVolume { get; set; } // 今日交易量
}	

    public static async Task<string> GetPriceInfo(string symbol)
    {
        string result = "";
        bool dataFetched = false;
        List<KlineDataItem> klineData = null; // 移动到这里以扩大作用域
	string dataSource = ""; // 添加数据来源变量    

        // 尝试从币安获取数据
        try
        {
            var binanceResponse = await httpClient.GetAsync($"https://api.binance.com/api/v3/ticker/price?symbol={symbol.ToUpper()}USDT");
            if (binanceResponse.IsSuccessStatusCode)
            {
                var currentPriceData = JsonSerializer.Deserialize<CurrentPrice>(await binanceResponse.Content.ReadAsStringAsync());
                decimal currentPrice = decimal.Parse(currentPriceData.price);

                var klineResponse = await httpClient.GetAsync($"https://api.binance.com/api/v3/klines?symbol={symbol.ToUpper()}USDT&interval=1d&limit=200");
                var klineDataRaw = JsonSerializer.Deserialize<List<List<JsonElement>>>(await klineResponse.Content.ReadAsStringAsync());

                klineData = ProcessKlineData(klineDataRaw); // 直接赋值给klineData
                dataFetched = true;
		dataSource = $"<a href=\"https://www.binance.com/zh-CN/trade/{symbol.ToUpper()}_USDT?type=spot\">币安</a>"; // 设置数据来源为币安    

                //Console.WriteLine("数据来自币安");    
            }
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"从币安获取数据失败: {ex.Message}");
        }

// 如果从币安获取数据失败，尝试从欧易获取数据
if (!dataFetched)
{
    try
    {
        var okexResponse = await httpClient.GetAsync($"https://www.okx.com/api/v5/market/history-index-candles?instId={symbol.ToUpper()}-USD&bar=1D&limit=200");
        if (okexResponse.IsSuccessStatusCode)
        {
            var responseString = await okexResponse.Content.ReadAsStringAsync();
            var okexResponseObject = JsonSerializer.Deserialize<OkexResponse>(responseString);
            // 确保okexResponseObject.data不仅存在，还包含有效数据
            if (okexResponseObject != null && okexResponseObject.data != null && okexResponseObject.data.Any() && okexResponseObject.data.First().Any())
            {
                var klineDataRaw = okexResponseObject.data.Select(kline => kline.Select(JsonElementFromString).ToList()).ToList();
                klineData = ProcessKlineData(klineDataRaw, isOkex: true);
                dataFetched = true; // 只有在确实获取到有效数据时才设置为true
		dataSource = $"<a href=\"https://www.okx.com/zh-hans/trade-spot/{symbol.ToUpper()}-usdt\">欧易</a>"; // 设置数据来源为欧易    
                //Console.WriteLine("数据来自欧易");
            }
            else
            {
                //Console.WriteLine("欧易没有返回有效数据。");
                // 不设置dataFetched = true，以便尝试从抹茶获取数据
            }
        }
    }
    catch (Exception ex)
    {
        //Console.WriteLine($"从欧易获取数据失败: {ex.Message}");
        // 不设置dataFetched = true，以便尝试从抹茶获取数据
    }
}
// 如果从币安和欧易获取数据都失败，尝试从抹茶获取数据
if (!dataFetched)
{
    //Console.WriteLine("尝试从抹茶获取数据..."); // 添加调试输出
    try
    {
        var mexcResponse = await httpClient.GetAsync($"https://api.mexc.com/api/v3/klines?symbol={symbol.ToUpper()}USDT&interval=1d&limit=200");
        if (mexcResponse.IsSuccessStatusCode)
        {
	    var responseContent = await mexcResponse.Content.ReadAsStringAsync();
            //Console.WriteLine($"抹茶API返回的原始数据: {responseContent}"); // 输出API返回的原始数据
            var klineDataRaw = JsonSerializer.Deserialize<List<List<JsonElement>>>(await mexcResponse.Content.ReadAsStringAsync());

            klineData = klineDataRaw.Select(item => new KlineDataItem
            {
                OpenTime = item[0].GetInt64(),
                Open = item[1].GetString(),
                High = item[2].GetString(),
                Low = item[3].GetString(),
                Close = item[4].GetString()
                // 其他字段...
            }).ToList();

            dataFetched = true;
	    dataSource = $"<a href=\"https://www.mexc.com/zh-CN/exchange/{symbol.ToUpper()}_USDT?_from=header\">抹茶</a>"; // 设置数据来源为抹茶	

            //Console.WriteLine("数据来自抹茶");
        }
        else
        {
            //Console.WriteLine($"抹茶API响应失败，状态码：{mexcResponse.StatusCode}"); // 如果响应状态码不是成功状态，添加调试输出
        }
    }
    catch (Exception ex)
    {
        //Console.WriteLine($"从抹茶获取数据失败: {ex.Message}");
	//Console.WriteLine($"异常详情: {ex.ToString()}"); // 输出异常的详细信息，以便于调试    
    }
}
// 辅助方法，将字符串转换为JsonElement
JsonElement JsonElementFromString(string value)
{
    using (var doc = JsonDocument.Parse($"\"{value}\""))
    {
        return doc.RootElement.Clone();
    }
}

// 在调用CalculateAndFormatResult之前计算RSI
if (dataFetched && klineData != null)
{
    var rsi6 = CalculateRSI(klineData, 6);
    var rsi14 = CalculateRSI(klineData, 14);
    var rsiResult = $"<b>{dataSource} | 相对强弱指数： RSI6:</b> {rsi6:F2}  |  <b>RSI14:</b> {rsi14:F2}\n\n";

    // 获取交易量信息
    string tradingVolumeInfo = await GetTradingVolumeInfo(symbol);
    
    // 如果交易量信息是 "无法获取数据"，则不添加到结果中
    if (tradingVolumeInfo != "无法获取数据")
    {
        result += tradingVolumeInfo + "\n\n";
    }
    
    // 调用CalculateAndFormatResult获取压力位和阻力位信息
    var priceInfo = CalculateAndFormatResult(klineData);

    // 将RSI结果和压力位阻力位信息拼接后返回
    result = rsiResult + result + priceInfo;
}
else
{
    result = "";  // 修改这里，获取数据失败时返回空字符串
}

return result;
    }

private static List<KlineDataItem> ProcessKlineData(List<List<JsonElement>> klineDataRaw, bool isOkex = false)
{
    if (isOkex)
    {
        // 对于欧易数据，处理字符串到长整型的转换
        return klineDataRaw.Select(item => new KlineDataItem
        {
            OpenTime = long.Parse(item[0].GetString()),
            Open = item[1].GetString(),
            High = item[2].GetString(),
            Low = item[3].GetString(),
            Close = item[4].GetString()
            // 其他字段...
        }).ToList();
    }
    else
    {
        // 币安数据的处理逻辑保持不变
        return klineDataRaw.Select(item => new KlineDataItem
        {
            OpenTime = item[0].GetInt64(),
            Open = item[1].GetString(),
            High = item[2].GetString(),
            Low = item[3].GetString(),
            Close = item[4].GetString()
            // 其他字段...
        }).ToList();
    }
}

    private static double CalculateRSI(List<KlineDataItem> klineData, int period = 14)
{
    double? previousGain = null;
    double? previousLoss = null;
    double averageGain = 0;
    double averageLoss = 0;

    for (int i = 1; i < klineData.Count; i++)
    {
        var currentClose = decimal.Parse(klineData[i].Close);
        var previousClose = decimal.Parse(klineData[i - 1].Close);
        var change = currentClose - previousClose;

        double gain = change > 0 ? (double)change : 0;
        double loss = change < 0 ? -(double)change : 0;

        if (previousGain == null)
        {
            previousGain = gain;
            previousLoss = loss;
        }
        else
        {
            // 使用指数移动平均（EMA）计算平均增益和平均损失
            previousGain = (previousGain * (period - 1) + gain) / period;
            previousLoss = (previousLoss * (period - 1) + loss) / period;
        }
    }

    if (previousGain.HasValue && previousLoss.HasValue)
    {
        averageGain = previousGain.Value;
        averageLoss = previousLoss.Value;
    }

    double rs = averageLoss == 0 ? double.MaxValue : averageGain / averageLoss;
    double rsi = 100 - (100 / (1 + rs));

    return rsi;
}

private static string CalculateAndFormatResult(List<KlineDataItem> klineData)
{
    var result = "";
    var periods = new[] { 10, 30, 90, 200 };
    foreach (var period in periods)
    {
        var recentData = klineData.TakeLast(period);
        decimal resistance = recentData.Max(x => TryParseDecimal(x.High)); // 最高价
        decimal support = recentData.Min(x => TryParseDecimal(x.Low)); // 最低价
        decimal movingAverage = recentData.Average(x => TryParseDecimal(x.Close)); // 计算平均收盘价作为MA指标

        string formatResistance = FormatPrice(resistance);
        string formatSupport = FormatPrice(support);
        string formattedMA = FormatPrice(movingAverage); // 格式化MA指标的值

        result += $"<b>{period}D压力位：</b> {formatSupport}   <b>阻力位：</b> {formatResistance}   <b>m{period}：</b> {formattedMA}\n\n";
    }
    // 确保在处理完所有周期后返回结果
    return result;
}

static decimal TryParseDecimal(string value)
{
    if(decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
    {
        return result;
    }
    return 0; // 或者根据您的需要返回一个合理的默认值
}

static string FormatPrice(decimal price)
{
    string formattedPrice;

    if (price >= 1)
    {
        // 数值大于等于1时，精确到小数点后两位
        formattedPrice = price.ToString("F2", CultureInfo.InvariantCulture);
    }
    else
    {
        // 数值小于1时，保留到小数点后最多8位，避免科学记数法，并去除末尾无用的零
        formattedPrice = price.ToString("F8", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
    }

    return formattedPrice;
}
} 
public class OkexResponse
{
    public string code { get; set; }
    public string msg { get; set; }
    public List<List<string>> data { get; set; }
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
        try
        {
            // 检查本地缓存是否有数据
            if (!fundingRates.Any())
            {
                //Console.WriteLine("本地缓存无数据，立即获取数据");
                await FetchAndUpdateFundingRates(); // 从API获取数据并更新本地缓存
            }
            else
            {
                //Console.WriteLine("从本地缓存获取数据成功！");
            }
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"获取资金费数据失败: {ex.Message}");
            return "获取资金费数据失败，请稍后再试。";
        }

        var negativeFundingRates = fundingRates
            .Select(x => new { symbol = x.Key.Replace("USDT", "/USDT"), lastFundingRate = x.Value })
            .Where(x => Math.Abs(x.lastFundingRate) >= 0.00001)
            .OrderBy(x => x.lastFundingRate)
            .Take(5);

        var positiveFundingRates = fundingRates
            .Select(x => new { symbol = x.Key.Replace("USDT", "/USDT"), lastFundingRate = x.Value })
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

            // 尝试获取 Hyperliquid 资金费率
            string hyperliquidResult = await FundingRateMonitor.GetHyperliquidFundingRates();
            if (!hyperliquidResult.StartsWith("查询资金费率失败"))
            {
                // 如果 Hyperliquid 数据获取成功，追加分隔线和数据
                result += "--------------------------\n";
                result += hyperliquidResult;
            }
            // 如果 Hyperliquid 数据失败，不追加任何内容

            return result;
    }

    // 从API获取资金费数据并更新本地缓存
    private static async Task FetchAndUpdateFundingRates()
    {
        try
        {
            var response = await httpClient.GetAsync("https://fapi.binance.com/fapi/v1/premiumIndex");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<List<FundingRate>>(content);
                foreach (var item in data)
                {
                    if (item.symbol != "TRXUSDT") // 排除 TRX/USDT
                    {
                        fundingRates[item.symbol] = double.Parse(item.lastFundingRate);
                    }
                }
            }
            else
            {
                //Console.WriteLine($"API调用失败: 状态码 {response.StatusCode}");
                throw new Exception("API调用失败");
            }
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"在更新资金费数据时发生错误: {ex.Message}");
            throw; // 可选：重新抛出异常，如果需要在调用栈上层进一步处理
        }
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
                    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(buttonText, $"query_again,{bind.Address}") });
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
               "<b>全网独家</b>：<u>机器人除了能播报交易信息，还能查询对方地址的余额以及标签！</u>\n\n" +
               "示例：  <b>新交易   \U0001F4B0  -10 USDT</b>\n\n" +
               "交易类型：<b>出账</b>\n" +
               "出账金额：<b>10</b>\n" +
               "交易时间：<b>2024-01-23 20:23:18</b>\n" +
               "监听地址：<code>TU4vEruvZwLLkSfV9bNw12EJTPvNr7Pvaa</code>\n" +
               "地址备注：<b>地址1</b>\n" +
               "地址余额：<b>609,833.06 USDT  |  75,860.52 TRX</b>\n" +
               "------------------------------------------------------------------------\n" +
               "对方地址：<code>TAQt2mCvsGtAFi9uY36X7MriJKQr2Pndhx</code>\n" +
               "对方余额：<b>40,633.97 USDT  |  526.16 TRX</b>\n" +
               "对方标签：<b>无风险</b>\n\n" +
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
    // 检查消息是否来自指定的用户 ID（管理员）
    if (message.From.Id != 1427768220)
    {
        return;
    }

    // 将消息文本转换为小写
    var lowerCaseMessage = message.Text.ToLower();

    // 使用正则表达式匹配用户信息
    var regex = new Regex(@"(.*?)用户名：@(.*?)\s+id：(\d+)", RegexOptions.IgnoreCase);
    var matches = regex.Matches(lowerCaseMessage);

    lock (_followersLock) // 添加线程锁保护 Followers 操作
    {
        foreach (Match match in matches)
        {
            try
            {
                string name = match.Groups[1].Value.Trim();
                string username = match.Groups[2].Value.Trim();
                long id = long.Parse(match.Groups[3].Value.Trim());

                // 检查是否在黑名单中
                if (BlacklistedUserIds.Contains(id) || BlacklistedUsernames.Contains(username))
                {
                    // 如果在黑名单中，移除已存在的用户
                    var existingBlacklistedUser = Followers.FirstOrDefault(u => u.Id == id);
                    if (existingBlacklistedUser != null)
                    {
                        Followers.Remove(existingBlacklistedUser);
                    }
                    continue; // 跳过黑名单用户
                }

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
                    var user = new User
                    {
                        Name = name,
                        Username = username,
                        Id = id,
                        FollowTime = DateTime.UtcNow.AddHours(8)
                    };
                    Followers.Add(user);
                }
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
            // 检查用户名是否在黑名单中
            if (BlacklistedUsernames.Contains(username))
            {
                // 移除已存在的黑名单用户（如果有）
                var existingBlacklistedUser = Followers.FirstOrDefault(u => u.Username == username);
                if (existingBlacklistedUser != null)
                {
                    Followers.Remove(existingBlacklistedUser);
                }
                return;
            }
            var user = new User { Username = username, FollowTime = DateTime.UtcNow.AddHours(8) };
            Followers.Add(user);
        }
        else if (message.Text.StartsWith("存 ID："))
        {
            string idText = message.Text.Substring("存 ID：".Length).Trim();
            if (long.TryParse(idText, out long id))
            {
                // 检查ID是否在黑名单中
                if (BlacklistedUserIds.Contains(id))
                {
                    // 移除已存在的黑名单用户（如果有）
                    var existingBlacklistedUser = Followers.FirstOrDefault(u => u.Id == id);
                    if (existingBlacklistedUser != null)
                    {
                        Followers.Remove(existingBlacklistedUser);
                    }
                    return;
                }
                var user = new User { Id = id, FollowTime = DateTime.UtcNow.AddHours(8) };
                Followers.Add(user);
            }
        }
    }

    // 在处理完所有用户信息后发送一条消息
    await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "已储存用户资料！");
}
//计算数字+数字货币的各地货币价值    
private static async Task HandleCryptoCurrencyMessageAsync(ITelegramBotClient botClient, Message message)
{
    var match = Regex.Match(message.Text, @"^(\d+(\.\d+)?)\s*([a-zA-Z]+)$", RegexOptions.IgnoreCase);

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
// 存储用户-地址对最后提醒时间的字典
private static Dictionary<(long UserId, string Address), DateTime> lastReminderTimes = new Dictionary<(long UserId, string Address), DateTime>();
private static readonly object timerLock = new object();
private static readonly Random random = new Random(); // 用于生成随机检查间隔

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

        // 获取地址备注信息
        string note = userAddressNotes.TryGetValue((userId, tronAddress), out var userNote) ? userNote : "";
        string noteMessagePart = !string.IsNullOrEmpty(note) ? $"地址备注信息：<b>{note}</b>\n" : "";

        if (balance < 100)
        {
            // 检查是否在过去8小时内发送过提醒
            bool shouldSendReminder;
            lock (timerLock)
            {
                var key = (userId, tronAddress);
                if (lastReminderTimes.TryGetValue(key, out var lastReminderTime))
                {
                    shouldSendReminder = (DateTime.UtcNow - lastReminderTime).TotalHours >= 8;
                }
                else
                {
                    shouldSendReminder = true; // 首次检测到余额不足，发送提醒
                }
            }

            if (shouldSendReminder)
            {
                try
                {
                    await botClient.SendTextMessageAsync(
                        chatId: userId,
                        text: $"<b>温馨提示：</b>\n您绑定的地址：<code>{tronAddress}</code>\n{noteMessagePart}\n⚠️ TRX余额只剩：{roundedBalance}，剩余可供转账：{transferTimes}次 ⚠️\n为了不影响您的转账，建议您立即向本机器人兑换TRX！",
                        parseMode: ParseMode.Html
                    );

                    // 记录本次提醒时间
                    lock (timerLock)
                    {
                        lastReminderTimes[(userId, tronAddress)] = DateTime.UtcNow;
                    }
                }
                catch (ApiRequestException ex)
                {
                    if (ex.Message.Contains("Too Many Requests"))
                    {
                        // 解析重试时间
                        var match = Regex.Match(ex.Message, @"retry after (\d+)");
                        if (match.Success)
                        {
                            int retryAfter = int.Parse(match.Groups[1].Value);
                            // 等待重试时间+1秒
                            await Task.Delay((retryAfter + 1) * 1000);
                            // 重新尝试发送消息
                            await botClient.SendTextMessageAsync(
                                chatId: userId,
                                text: $"<b>温馨提示：</b>\n您绑定的地址：<code>{tronAddress}</code>\n{noteMessagePart}\n⚠️ TRX余额只剩：{roundedBalance}，剩余可供转账：{transferTimes}次 ⚠️\n为了不影响您的转账，建议您立即向本机器人兑换TRX！",
                                parseMode: ParseMode.Html
                            );

                            // 记录本次提醒时间
                            lock (timerLock)
                            {
                                lastReminderTimes[(userId, tronAddress)] = DateTime.UtcNow;
                            }
                        }
                    }
                    else if (ex.Message == "Forbidden: bot was blocked by the user" || ex.Message.Contains("user is deactivated") || ex.Message.Contains("Bad Request: chat not found"))
                    {
                        // 用户阻止了机器人，或者用户注销了机器人，取消定时器任务
                        timer.Dispose();
                        timer = null;
                        // 从字典中移除该用户的定时器和地址
                        var key = (userId, tronAddress);
                        lock (timerLock)
                        {
                            userTimers.Remove(key);
                            lastReminderTimes.Remove(key);
                        }
                        RemoveAddressFromUser(userId, tronAddress);
                    }
                    else
                    {
                        // 其他错误继续抛出
                        throw;
                    }
                }
                catch (Exception ex)  // 捕获所有异常
                {
                    // 取消定时器任务
                    timer.Dispose();
                    timer = null;
                    // 从字典中移除该用户的定时器和地址
                    var key = (userId, tronAddress);
                    lock (timerLock)
                    {
                        userTimers.Remove(key);
                        lastReminderTimes.Remove(key);
                    }
                    RemoveAddressFromUser(userId, tronAddress);
                }
            }

            // 余额不足，继续每45-60秒检查一次
            if (timer != null)
            {
                int nextCheckSeconds = random.Next(45, 61); // 随机45-60秒
                timer.Change(TimeSpan.FromSeconds(nextCheckSeconds), TimeSpan.FromSeconds(nextCheckSeconds));
            }
        }
        else
        {
            // 余额充足，每45-60秒检查一次
            if (timer != null)
            {
                int nextCheckSeconds = random.Next(45, 61); // 随机45-60秒
                timer.Change(TimeSpan.FromSeconds(nextCheckSeconds), TimeSpan.FromSeconds(nextCheckSeconds));
            }
        }
    }, null, TimeSpan.Zero, TimeSpan.FromSeconds(random.Next(45, 61)));

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
        int retryCount = 0;
        const int maxRetries = 3;

        while (retryCount < maxRetries)
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
                if (ex.Message.Contains("无法获取波场地址的TRX余额"))
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        return 0; // 达到最大重试次数，返回 0
                    }
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
        return 0; // 确保循环外有默认返回值
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
    var chatId = message.Chat.Id;
    var messageId = message.MessageId;
    var userId = message.From.Id;

    // 先发送一个正在查询的消息
    var infoMessage = await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: "正在查询，请稍后...",
        replyToMessageId: messageId
    );

    try
    {
        var userName = message.From.Username != null ? "@" + message.From.Username : "未设置";
        var firstName = message.From.FirstName;
        var lastName = message.From.LastName ?? ""; // 如果没有姓氏，使用空字符串
        var language = message.From.LanguageCode;
        var fullName = $"{firstName} {lastName}".Trim();
        var chatName = message.Chat.Title; // 群聊名称
        var chatType = message.Chat.Type == ChatType.Supergroup ? "超级群组" : "普通群组";    
        var dcId = userName != "未设置" ? await FetchDcIdFromUsername(userName.TrimStart('@'), userId) : null;
        var dcLocation = GetDcLocation(dcId);

        var responseText = "";

        if (message.Chat.Type == ChatType.Private)
        {
            responseText = $"用户ID：<code>{userId}</code>\n用户名：{userName}\n姓名：{fullName}\n语言：{language}";
            if (dcLocation != null) responseText += $"\n数据中心：{dcLocation}";
        }
        else if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
        {
            responseText = $"群组ID：<code>{chatId}</code>\n群组名：{chatName}\n群类型：{chatType}\n\n用户ID：<code>{userId}</code>\n用户名：{userName}\n姓名：{fullName}\n语言：{language}";
            if (dcLocation != null) responseText += $"\n数据中心：{dcLocation}";
        }

        // 使用编辑消息功能来更新查询结果
        await botClient.EditMessageTextAsync(
            chatId: chatId,
            messageId: infoMessage.MessageId,
            text: responseText,
            parseMode: ParseMode.Html
        );
    }
    catch (ApiRequestException ex)
    {
        Console.WriteLine($"发送消息时发生错误: {ex.Message}");
        // 如果出错，编辑消息显示错误信息
        await botClient.EditMessageTextAsync(
            chatId: chatId,
            messageId: infoMessage.MessageId,
            text: $"查询失败: {ex.Message}"
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine($"发生意外错误: {ex.Message}");
        await botClient.EditMessageTextAsync(
            chatId: chatId,
            messageId: infoMessage.MessageId,
            text: $"发生意外错误: {ex.Message}"
        );
    }
}
//dc映射表
private static string GetDcLocation(string dcId)
{
    var dcMap = new Dictionary<string, string>
    {
        {"DC1", "美国 迈阿密"},
        {"DC2", "荷兰 阿姆斯特丹"},
        {"DC3", "美国 迈阿密"},
        {"DC4", "荷兰 阿姆斯特丹"},
        {"DC5", "新加坡"}
    };

    if (dcId != null && dcMap.ContainsKey(dcId))
    {
        return $"{dcMap[dcId]}（{dcId}）";
    }
    return null;
}
// 全局HttpClient实例
private static readonly HttpClient httpClient = new HttpClient();

// 用户ID与数据中心代码的缓存字典
private static Dictionary<long, string> userDcCache = new Dictionary<long, string>();

// 获取注册地区
private static async Task<string> FetchDcIdFromUsername(string username, long userId)
{
    // 检查缓存中是否已有数据中心信息
    if (userDcCache.TryGetValue(userId, out var cachedDcId))
    {
        return cachedDcId;
    }

    var url = $"https://t.me/{username}";
    try
    {
        var response = await httpClient.GetAsync(url);
        var pageContent = await response.Content.ReadAsStringAsync();

        var srcIndex = pageContent.IndexOf("src=\"https://cdn");
        if (srcIndex != -1)
        {
            var startIndex = pageContent.IndexOf("cdn", srcIndex) + 3;
            var endIndex = pageContent.IndexOf('.', startIndex);
            if (startIndex != -1 && endIndex != -1)
            {
                var dcId = pageContent.Substring(startIndex, endIndex - startIndex);
                var result = $"DC{dcId}";
                // 更新缓存
                userDcCache[userId] = result;
                return result;
            }
        }
    }
    catch (HttpRequestException e)
    {
        Console.WriteLine($"请求失败: {e.Message}");
    }
    return null;
}

private static readonly object _followersLock = new object(); // 添加线程锁，用于保护 Followers 列表的并发访问
private static readonly List<User> Followers = new List<User>();

//黑名单，指定用户不储存
private static readonly HashSet<long> BlacklistedUserIds = new HashSet<long>
{
    1087968824,
    // 在这里添加更多用户ID，每行一个
};

private static readonly HashSet<string> BlacklistedUsernames = new HashSet<string>
{
    "GroupAnonymousBot",
    // 在这里添加更多用户名，每行一个
};
//完整列表
private static async Task HandleFullListCallbackAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
{
    List<User> followers;
    int followersCount;

    // 使用线程锁保护数据读取操作
    lock (_followersLock)
    {
        followers = Followers.OrderByDescending(f => f.FollowTime).ToList();
        followersCount = Followers.Count;
    }

    for (int i = 0; i < followers.Count; i += 100)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"机器人目前在用人数：<b>{followersCount}</b>\n");

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
    // 检查是否在黑名单中
    if (BlacklistedUserIds.Contains(message.From.Id) ||
        BlacklistedUsernames.Contains(message.From.Username ?? ""))
    {
        // 如果在黑名单中，从仓库中移除（如果存在）
        lock (_followersLock)
        {
            var existingUser = Followers.FirstOrDefault(x => x.Id == message.From.Id);
            if (existingUser != null)
            {
                Followers.Remove(existingUser);
            }
        }
        return;
    }

    lock (_followersLock)
    {
        var user = Followers.FirstOrDefault(x => x.Id == message.From.Id);
        if (user == null)
        {
            Followers.Add(new User 
            { 
                Name = message.From.FirstName, 
                Username = message.From.Username, 
                Id = message.From.Id, 
                FollowTime = DateTime.UtcNow.AddHours(8),
                IsBot = message.From.IsBot
            });
        }
    }
}


private static async Task HandleGetFollowersCommandAsync(ITelegramBotClient botClient, Message message, int page = 0, bool edit = false)
{
    AddFollower(message);

    // 使用线程锁保护数据读取操作
    int followersCount;
    int todayFollowers;
    List<User> followersPerPage;

    lock (_followersLock)
    {
        followersCount = Followers.Count;
        todayFollowers = Followers.Count(f => f.FollowTime.Date == DateTime.UtcNow.AddHours(8).Date);
        // 每页显示15条数据，按关注时间倒序排列
        followersPerPage = Followers.OrderByDescending(f => f.FollowTime).Skip(page * 15).Take(15).ToList();
    }

    var sb = new StringBuilder();
    sb.AppendLine($"机器人目前在用人数：<b>{followersCount}</b>   今日新增关注：<b>{todayFollowers}</b>\n");

    // 遍历当前页的用户数据并添加到消息中
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
public class User
{
    public string Name { get; set; }
    public string Username { get; set; }
    public long Id { get; set; }
    public DateTime FollowTime { get; set; }
    public bool IsBot { get; set; }  // 添加 IsBot 属性
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
        string apiUrlTemplate = "https://apilist.tronscan.org/api/transfer?address=TCL7X3bbPYAY8ppCgHWResGdR8pXc38Uu6&token=TRX&only_confirmed=true&limit=50&start={0}";

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
                // 检查转账金额是否大于10 TRX（这里假设API返回的金额单位是最小单位，即sun，1 TRX = 1,000,000 sun）
                if (transfer.TransferFromAddress == "TCL7X3bbPYAY8ppCgHWResGdR8pXc38Uu6" &&
                    !uniqueTransfers.ContainsKey(transfer.TransferToAddress) &&
                    transfer.Amount > 10_000_000) // 10 TRX
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
    string resultText = $"<b> 承兑地址：</b><code>TCL7X3bbPYAY8ppCgHWResGdR8pXc38Uu6</code>\n\n";

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
            text: "<code>TCL7X3bbPYAY8ppCgHWResGdR8pXc38Uu6</code>",
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
                          "<blockquote expandable>实时汇率：<code>TRX能量跟包括比特币在内的所有数字货币一样，价格起起落落有涨有跌，受市场行情影响，机器人的兑换汇率自动跟随市场行情进行波动！</code>\n\n" +                
                          "地址监听：<code>绑定您的钱包地址，即可开启交易通知！一有交易就提示，假U，假截图，完全不起作用。发送：绑定 Txxxxxxx(您的钱包地址，，中间有个空格)即可完成绑定！ 注：为了不浪费服务器资源，暂不支持监听交易所地址，判定标准为：钱包余额大于1000万USDT或累计交易笔数大于30万笔！同时0.01USDT以下的交易将会被过滤掉！ </code>\n\n" +
                          "能量监控：<code>绑定钱包地址后，机器人将持续监控TRX余额，当TRX余额不足100时机器人会自动下发提醒！</code>\n\n" +            
                          "防骗助手：<code>把机器人拉进群聊并设置为管理员，当群内成员更改名字或用户名后，机器人会发送资料变更提醒，以防被骗！</code>\n\n" +
                          "资金费率：<code>发送 /zijin 即可查询币安永续合约资金费正负前五币种以及资金费率！</code>\n\n" +      
                          "涨跌榜单：<code>发送 /faxian 即可查询币安加密货币连续上涨或下跌榜单TOP5</code>\n\n" +  
                          "授权查询：<code>在任意群组发送波场地址即可查询该地址授权情况，支持查询USDT和USDC授权！</code>\n\n" +    
                          "实时u价：<code>发送 z0 或者 /usdt 返回okx实时usdt买入卖出价格表</code>\n\n" +        
                          "群聊管理：<code>将机器人拉到任意群聊并设置管理员，机器人将自动删除用户进出群消息！</code>\n\n" +             
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
                          "机器人兑换过程公平公正公开，交易记录全开放，发送：<code>兑换记录</code> 自动返回近期USDT收入以及TRX转出记录，欢迎监督！</blockquote>\n\n" +
                          "\U0001F449        本机器人源码出售，如有需要可联系" + adminLinkText + "      \U0001F448";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("关闭", "back")
        });
	    
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: helpText,
            parseMode: ParseMode.Html,
            disableWebPagePreview: true,
	    replyMarkup: keyboard	
        );
    }
}
public static async Task<string> GetTransactionRecordsAsync(ITelegramBotClient botClient, Message message)
{
    var responseMessage = await botClient.SendTextMessageAsync(message.Chat.Id, "正在统计，请稍后...");
    var responseMessageId = responseMessage.MessageId;
    
    try
    {
        string outcomeAddress = "TCL7X3bbPYAY8ppCgHWResGdR8pXc38Uu6";
        string usdtUrl = $"https://apilist.tronscan.org/api/token_trc20/transfers?relatedAddress={outcomeAddress}&contract=TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t&direction=to&limit=100&start=0&sort=-timestamp";

        using (var httpClient = new HttpClient())
        {
            // 添加 TRON API 密钥到请求头
            httpClient.DefaultRequestHeaders.Add("TRON-PRO-API-KEY", "0c138945-fd9f-4390-b015-6b93368de1fd");

            var usdtResponse = await httpClient.GetStringAsync(usdtUrl);
            var usdtTransactions = ParseTransactions(usdtResponse, "USDT")
                .Where(t => t.amount >= 10)
                .Take(10)
                .ToList();

            var trxTransactions = new List<(DateTime timestamp, string token, decimal amount)>();
            int start = 0;
            int limit = 200;
            while (trxTransactions.Count < 10)
            {
                string trxUrl = $"https://apilist.tronscanapi.com/api/transfer?sort=-timestamp&count=true&limit={limit}&start={start}&address={outcomeAddress}&filterTokenValue=10000000"; // 10 TRX in sun
                var trxResponse = await httpClient.GetStringAsync(trxUrl);
                var newTransactions = ParseTransactions(trxResponse, "TRX")
                    .Where(t => t.amount > 10)
                    .ToList();
                
                trxTransactions.AddRange(newTransactions);
                if (newTransactions.Count == 0) break; // 如果没有新的交易，退出循环
                start += limit;
                if (start > 1000) break; // 设置一个上限，避免无限循环
            }

            trxTransactions = trxTransactions.Take(10).ToList();

            var transactionRecords = FormatTransactionRecords(usdtTransactions, trxTransactions);

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
                Console.WriteLine($"替换消息失败：{ex.Message}");
                Console.WriteLine($"交易记录内容：{transactionRecords}");  // 调试输出
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
        await botClient.SendTextMessageAsync(message.Chat.Id, "查询超时，请进交易群查看！", replyMarkup: new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithUrl("点击加入交易群", "https://t.me/+b4NunT6Vwf0wZWI1")
        }));
        return "服务器超时，请进交易群查看！";
    }
} 

private static List<(DateTime timestamp, string token, decimal amount)> ParseTransactions(string jsonResponse, string token)
{
    var transactions = new List<(DateTime timestamp, string token, decimal amount)>();

    var json = JObject.Parse(jsonResponse);
    var dataArray = token == "USDT" ? json["token_transfers"] as JArray : json["data"] as JArray;

    if (dataArray != null)
    {
        foreach (var data in dataArray)
        {
            if (token == "USDT")
            {
                if (data["to_address"] != null && data["to_address"].ToString() == "TCL7X3bbPYAY8ppCgHWResGdR8pXc38Uu6" &&
                    data["block_ts"] != null && data["quant"] != null)
                {
                    var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)data["block_ts"]).LocalDateTime;
                    var amount = decimal.Parse(data["quant"].ToString()) / 1000000;
                    transactions.Add((timestamp, token, amount));
                }
            }
            else if (token == "TRX")
            {
                if (data["transferFromAddress"] != null && data["transferFromAddress"].ToString() == "TCL7X3bbPYAY8ppCgHWResGdR8pXc38Uu6" &&
                    data["timestamp"] != null && data["amount"] != null)
                {
                    var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)data["timestamp"]).LocalDateTime;
                    var amount = decimal.Parse(data["amount"].ToString()) / 1000000; // 确保这里的转换逻辑正确
                    transactions.Add((timestamp, "TRX", amount));
                }
            }
        }
    }

    return transactions;
}

private static string FormatTransactionRecords(List<(DateTime timestamp, string token, decimal amount)> usdtTransactions, List<(DateTime timestamp, string token, decimal amount)> trxTransactions)
{
    var sb = new StringBuilder();

    int maxCount = Math.Max(usdtTransactions.Count, trxTransactions.Count);

    for (int i = 0; i < maxCount; i++)
    {
        if (i < usdtTransactions.Count)
        {
            sb.AppendLine($"{usdtTransactions[i].timestamp:yyyy-MM-dd HH:mm:ss}  收入 {usdtTransactions[i].amount:F2} {usdtTransactions[i].token}");
        }

        if (i < trxTransactions.Count)
        {
            sb.AppendLine($"{trxTransactions[i].timestamp:yyyy-MM-dd HH:mm:ss}  支出 {trxTransactions[i].amount:F2} {trxTransactions[i].token}");
        }

        // 只有在不是最后一条记录时才添加横线
        if (i < maxCount - 1)
        {
            sb.AppendLine("—————————————————————");
        }
    }

    return sb.ToString();
}
//以上3个方法是监控收款地址以及出款地址的交易记录并返回！   
//谷歌翻译
public static class TranslationSettingsManager
{
    private static readonly Dictionary<long, bool> TranslationSettings = new Dictionary<long, bool>();

    // 检查是否允许翻译（群组或用户）
    public static bool IsTranslationEnabled(long id)
    {
        return !TranslationSettings.TryGetValue(id, out var isDisabled) || isDisabled;
    }

    // 设置翻译状态（true 为启用，false 为禁用）
    public static void SetTranslationStatus(long id, bool isEnabled)
    {
        TranslationSettings[id] = isEnabled;
    }
} 
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
                // 清理可能包含的HTML标签
                translatedText = System.Web.HttpUtility.HtmlEncode(translatedText);

                // 添加内联按钮
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData("关闭自动翻译", "关闭翻译")
                });

                await botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    $"翻译结果：\n\n<code>{translatedText}</code>",
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard
                );

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
/*
  // 年度 本月 今日 收入统计  从oklin获取 太慢 放弃
public static async Task<(decimal TotalIncome, decimal TotalOutcome, decimal MonthlyIncome, decimal MonthlyOutcome, decimal DailyIncome, decimal DailyOutcome, bool IsError)> GetTotalIncomeAsync(string address, bool isTrx)
{
    try
    {
        string[] apiKeys = new string[] {
            "4f37a8b5-870b-4a02-a33f-7dc41cb8ed8d",
            "f49353bd-db65-4719-a56c-064b2eb231bf",
            "587f64a1-43d5-40f2-9115-7d3c66b0459a",		
            "92854974-68da-4fd8-9e50-3948c1e6fa7e"
        };

        decimal totalIncome = 0m;
        decimal totalOutcome = 0m;
        decimal monthlyIncome = 0m;
        decimal monthlyOutcome = 0m;
        decimal dailyIncome = 0m;
        decimal dailyOutcome = 0m;

        DateTime nowInBeijing = ConvertToBeijingTime(DateTime.UtcNow);
        DateTime firstDayOfMonth = new DateTime(nowInBeijing.Year, nowInBeijing.Month, 1);
        DateTime today = nowInBeijing.Date;
        DateTime firstDayOfYear = new DateTime(nowInBeijing.Year, 1, 1);

        using var httpClient = new HttpClient();
        int totalPage = await GetTotalPages(httpClient, address, apiKeys[0]);
        bool continueFetching = true;
        int currentPage = 1;

        while (continueFetching && currentPage <= totalPage)
        {
            var tasks = new List<Task<JsonDocument>>();
            for (int i = currentPage; i < currentPage + 20 && i <= totalPage; i++)
            {
                int page = i;
                tasks.Add(FetchPageData(httpClient, address, page, apiKeys));
            }

            var results = await Task.WhenAll(tasks);
            currentPage += 20;

            foreach (var result in results)
            {
                if (result == null)
                {
                    Console.WriteLine("所有API密钥尝试失败，停止后续页面处理。");
                    continueFetching = false; // 停止处理后续页面
                    break;
                }
                
                if (result.RootElement.TryGetProperty("data", out JsonElement dataElement) && 
                    dataElement.GetArrayLength() > 0)
                {
                    var transactionsData = dataElement[0];
                    if (transactionsData.TryGetProperty("transactionLists", out JsonElement transactionLists))
                    {
                        foreach (var item in transactionLists.EnumerateArray())
                        {
                            if (item.TryGetProperty("amount", out JsonElement amountElement) &&
                                item.TryGetProperty("transactionTime", out JsonElement transactionTimeElement) &&
                                item.TryGetProperty("from", out JsonElement fromElement) &&
                                item.TryGetProperty("to", out JsonElement toElement))
                            {
                                decimal amount = decimal.Parse(amountElement.GetString());
                                DateTime transactionTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(transactionTimeElement.GetString())).UtcDateTime;
                                transactionTime = ConvertToBeijingTime(transactionTime);

                                if (transactionTime < firstDayOfYear) // 检查是否包含去年的数据
                                {
                                    continueFetching = false;
                                    break;
                                }

                                string fromAddress = fromElement.GetString();
                                string toAddress = toElement.GetString();

                                if (toAddress == address)
                                {
                                    if (transactionTime >= firstDayOfYear)
                                    {
                                        totalIncome += amount;
                                    }
                                    if (transactionTime >= firstDayOfMonth)
                                    {
                                        monthlyIncome += amount;
                                    }
                                    if (transactionTime.Date == today)
                                    {
                                        dailyIncome += amount;
                                    }
                                }
                                else if (fromAddress == address)
                                {
                                    if (transactionTime >= firstDayOfYear)
                                    {
                                        totalOutcome += amount;
                                    }
                                    if (transactionTime >= firstDayOfMonth)
                                    {
                                        monthlyOutcome += amount;
                                    }
                                    if (transactionTime.Date == today)
                                    {
                                        dailyOutcome += amount;
                                    }
                                }
                            }
                        }
                    }
                    if (!continueFetching) break;
                }
            }
            if (!continueFetching) break; // 如果发现去年的数据，停止处理后续页面
        }

        return (totalIncome, totalOutcome, monthlyIncome, monthlyOutcome, dailyIncome, dailyOutcome, false);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in method {nameof(GetTotalIncomeAsync)}: {ex.Message}");
        return (0m, 0m, 0m, 0m, 0m, 0m, true);
    }
}

private static async Task<JsonDocument> FetchPageData(HttpClient httpClient, string address, int page, string[] apiKeys)
{
    for (int keyIndex = 0; keyIndex < apiKeys.Length; keyIndex++)
    {
        string apiKey = apiKeys[keyIndex];
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("OK-ACCESS-KEY", apiKey);

        var apiUrl = $"https://oklink.com/api/v5/explorer/address/transaction-list?chainShortName=TRON&address={address}&limit=100&page={page}&tokenContractAddress=TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t&protocolType=token_20";
        HttpResponseMessage response;
        int retryCount = 0;

        do
        {
            response = await httpClient.GetAsync(apiUrl);
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                if (++retryCount > 3) // 尝试次数改为3次
                {
                    //Console.WriteLine($"方法 FetchPageData 错误: 多次尝试后因429错误（请求过多）失败。");
                    break; // 跳出循环，尝试下一个密钥
                }
                Random rnd = new Random();
                await Task.Delay(rnd.Next(500, 1001)); // 随机延迟0.5到1秒
            }
            else if (!response.IsSuccessStatusCode)
            {
                break; // 尝试下一个API密钥
            }
            else
            {
                return JsonDocument.Parse(await response.Content.ReadAsStringAsync()); // 成功响应
            }
        } while (true);
    }
    return null; // 所有密钥尝试失败
}

private static async Task<int> GetTotalPages(HttpClient httpClient, string address, string apiKey)
{
    var firstPageUrl = $"https://oklink.com/api/v5/explorer/address/transaction-list?chainShortName=TRON&address={address}&limit=100&page=1&tokenContractAddress=TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t&protocolType=token_20";
    httpClient.DefaultRequestHeaders.Clear();
    httpClient.DefaultRequestHeaders.Add("OK-ACCESS-KEY", apiKey);
    var response = await httpClient.GetAsync(firstPageUrl);
    var json = await response.Content.ReadAsStringAsync();
    var document = JsonDocument.Parse(json);
    return int.Parse(document.RootElement.GetProperty("data")[0].GetProperty("totalPage").GetString());
}

public static DateTime ConvertToBeijingTime(DateTime utcDateTime)
{
    var timeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
    return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
}
*/  // 年度 本月 今日 收入统计  从oklin获取 太慢 放弃   


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
//获取最后活跃时间

/*  //新版在下面
public static async Task<(DateTime LastTransactionTime, bool IsError)> GetLastTransactionTimeAsync(string address)
{
    string[] apiKeys = new string[] {
        "4f37a8b5-870b-4a02-a33f-7dc41cb8ed8d",
        "f49353bd-db65-4719-a56c-064b2eb231bf",
        "587f64a1-43d5-40f2-9115-7d3c66b0459a",	    
        "92854974-68da-4fd8-9e50-3948c1e6fa7e"
    };

    using var httpClient = new HttpClient();

    foreach (var apiKey in apiKeys)
    {
        try
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("OK-ACCESS-KEY", apiKey);

            // 修改API地址以获取交易列表
            var response = await httpClient.GetAsync($"https://www.oklink.com/api/v5/explorer/address/transaction-list?chainShortName=tron&address={address}&limit=1");

            var json = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var jsonDocument = JsonDocument.Parse(json);
                var lastTransactionTime = 0L;

                if (jsonDocument.RootElement.TryGetProperty("data", out JsonElement dataElement) && 
                    dataElement.GetArrayLength() > 0 &&
                    dataElement[0].TryGetProperty("transactionLists", out JsonElement transactionListsElement) &&
                    transactionListsElement.GetArrayLength() > 0)
                {
                    var transaction = transactionListsElement[0];
                    if (transaction.TryGetProperty("transactionTime", out JsonElement transactionTimeElement))
                    {
                        var transactionTimeString = transactionTimeElement.GetString();
                        lastTransactionTime = long.Parse(transactionTimeString);
                    }
                }

                if (lastTransactionTime > 0)
                {
                    var utcDateTime = DateTimeOffset.FromUnixTimeMilliseconds(lastTransactionTime).DateTime;
                    return (ConvertToBeijingTime(utcDateTime), false); // 如果没有发生错误，返回结果和IsError=false
                }
                else
                {
                    // 如果没有找到交易，返回最小时间和错误标志
                    return (DateTime.MinValue, true);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error with API Key {apiKey}: {ex.Message}");
            // 如果是最后一个密钥，返回错误
            if (apiKey == apiKeys.Last())
            {
                return (DateTime.MinValue, true);
            }
        }
    }

    // 如果所有密钥都失败，返回错误
    return (DateTime.MinValue, true);
}
*/  // 旧版从oklin获取 

public static async Task<(DateTime LastTransactionTime, bool IsError)> GetLastTransactionTimeAsync(string address)
{
    string apiKey = "369e85e5-68d3-4299-a602-9d8d93ad026a";
    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Add("TRON-PRO-API-KEY", apiKey);

    try
    {
        // 同时发起两个API请求
        var trc20Task = httpClient.GetAsync($"https://api.trongrid.io/v1/accounts/{address}/transactions/trc20?only_confirmed=true&limit=1");
        var trc10Task = httpClient.GetAsync($"https://apilist.tronscanapi.com/api/transfer?sort=-timestamp&count=true&limit=1&start=0&address={address}");

        // 等待两个请求完成
        await Task.WhenAll(trc20Task, trc10Task);

        var trc20Response = await trc20Task;
        var trc10Response = await trc10Task;

        long trc20LatestTime = 0;
        long trc10LatestTime = 0;

        // 处理TRC20响应
        if (trc20Response.IsSuccessStatusCode)
        {
            var trc20Json = await trc20Response.Content.ReadAsStringAsync();
            //Console.WriteLine("TRC20 API Response:");
            //Console.WriteLine(trc20Json);
            
            var trc20Document = JsonDocument.Parse(trc20Json);
            
            if (trc20Document.RootElement.TryGetProperty("data", out JsonElement trc20Data) && 
                trc20Data.GetArrayLength() > 0)
            {
                var firstTrc20Transaction = trc20Data[0];
                if (firstTrc20Transaction.TryGetProperty("block_timestamp", out JsonElement trc20Timestamp))
                {
                    trc20LatestTime = trc20Timestamp.GetInt64();
                   // Console.WriteLine($"TRC20 Latest Transaction Time: {DateTimeOffset.FromUnixTimeMilliseconds(trc20LatestTime).DateTime}");
                }
            }
            else
            {
               // Console.WriteLine("No TRC20 transactions found");
            }
        }
        else
        {
           // Console.WriteLine($"TRC20 API Error: {trc20Response.StatusCode}");
        }

        // 处理TRC10响应
        if (trc10Response.IsSuccessStatusCode)
        {
            var trc10Json = await trc10Response.Content.ReadAsStringAsync();
            //Console.WriteLine("\nTRC10 API Response:");
           // Console.WriteLine(trc10Json);
            
            var trc10Document = JsonDocument.Parse(trc10Json);
            
            if (trc10Document.RootElement.TryGetProperty("data", out JsonElement trc10Data) && 
                trc10Data.GetArrayLength() > 0)
            {
                var firstTrc10Transaction = trc10Data[0];
                if (firstTrc10Transaction.TryGetProperty("timestamp", out JsonElement trc10Timestamp))
                {
                    trc10LatestTime = trc10Timestamp.GetInt64();
                    //Console.WriteLine($"TRC10 Latest Transaction Time: {DateTimeOffset.FromUnixTimeMilliseconds(trc10LatestTime).DateTime}");
                }
            }
            else
            {
              //  Console.WriteLine("No TRC10 transactions found");
            }
        }
        else
        {
           // Console.WriteLine($"TRC10 API Error: {trc10Response.StatusCode}");
        }

        // 比较两个时间戳，取最新的
        long latestTimestamp = Math.Max(trc20LatestTime, trc10LatestTime);

       // Console.WriteLine($"\nFinal Results:");
       // Console.WriteLine($"TRC20 Latest Time: {trc20LatestTime}");
       // Console.WriteLine($"TRC10 Latest Time: {trc10LatestTime}");
       // Console.WriteLine($"Latest Timestamp: {latestTimestamp}");

        if (latestTimestamp > 0)
        {
            var utcDateTime = DateTimeOffset.FromUnixTimeMilliseconds(latestTimestamp).DateTime;
            var beijingTime = ConvertToBeijingTime(utcDateTime);
          //  Console.WriteLine($"Final Beijing Time: {beijingTime}");
            return (beijingTime, false);
        }
        else
        {
           // Console.WriteLine("No valid transaction time found");
            return (DateTime.MinValue, true);
        }
    }
    catch (Exception ex)
    {
       // Console.WriteLine($"Error getting last transaction time: {ex.Message}");
       // Console.WriteLine($"Stack Trace: {ex.StackTrace}");
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
    // 定义API密钥，如果一个失效就换下一个
    string[] apiKeys = new string[] {
        "4f37a8b5-870b-4a02-a33f-7dc41cb8ed8d",
        "f49353bd-db65-4719-a56c-064b2eb231bf",
        "587f64a1-43d5-40f2-9115-7d3c66b0459a",
        "92854974-68da-4fd8-9e50-3948c1e6fa7e"
    };

    try
    {
        using var httpClient = new HttpClient();
        // 设置请求头部，使用第一个有效的API密钥
        httpClient.DefaultRequestHeaders.Add("OK-ACCESS-KEY", apiKeys[0]);

        // 获取TRX余额
        HttpResponseMessage trxResponse;
        string trxJson;
        int retryCount = 0;
        do
        {
            trxResponse = await httpClient.GetAsync($"https://www.oklink.com/api/v5/explorer/address/address-summary?chainShortName=tron&address={address}");
            trxJson = await trxResponse.Content.ReadAsStringAsync();
            if (trxJson.Contains("\"code\":\"50011\"") && retryCount < 2) // 检查是否达到API速率限制
            {
                //Console.WriteLine("Rate limit exceeded. Retrying after delay...");
                await Task.Delay(new Random().Next(1000, 1500)); // 随机延迟1到1.5秒
                retryCount++;
            }
            else
            {
                break;
            }
        } while (true);

        //Console.WriteLine($"TRX API Response: {trxJson}");  // 调试输出TRX API返回的JSON
        var trxJsonDocument = JsonDocument.Parse(trxJson);

        decimal trxBalance = 0m;
        if (trxJsonDocument.RootElement.GetProperty("code").GetString() == "0" && trxJsonDocument.RootElement.GetProperty("data").GetArrayLength() > 0)
        {
            trxBalance = decimal.Parse(trxJsonDocument.RootElement.GetProperty("data")[0].GetProperty("balance").GetString());
	    //Console.WriteLine("TRX余额来自主API");	
        }

        // 获取USDT余额
        HttpResponseMessage usdtResponse;
        string usdtJson;
        retryCount = 0; // 重置重试计数器
        do
        {
            usdtResponse = await httpClient.GetAsync($"https://www.oklink.com/api/v5/explorer/address/address-balance-fills?chainShortName=tron&address={address}&tokenContractAddress=TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t&limit=1");
            usdtJson = await usdtResponse.Content.ReadAsStringAsync();
            if (usdtJson.Contains("\"code\":\"50011\"") && retryCount < 2) // 检查是否达到API速率限制
            {
                //Console.WriteLine("Rate limit exceeded. Retrying after delay...");
                await Task.Delay(new Random().Next(1000, 1500)); // 随机延迟1到1.5秒
                retryCount++;
            }
            else
            {
                break;
            }
        } while (true);

        //Console.WriteLine($"USDT API Response: {usdtJson}");  // 调试输出USDT API返回的JSON
        var usdtJsonDocument = JsonDocument.Parse(usdtJson);

        decimal usdtBalance = 0m;
        if (usdtJsonDocument.RootElement.GetProperty("code").GetString() == "0" && usdtJsonDocument.RootElement.GetProperty("data").GetArrayLength() > 0 && usdtJsonDocument.RootElement.GetProperty("data")[0].GetProperty("tokenList").GetArrayLength() > 0)
        {
            usdtBalance = decimal.Parse(usdtJsonDocument.RootElement.GetProperty("data")[0].GetProperty("tokenList")[0].GetProperty("holdingAmount").GetString());
	    //Console.WriteLine("USDT余额来自主API");	
        }

// 如果任一余额为0，尝试使用备用API
if (trxBalance == 0m || usdtBalance == 0m)
{
    // 添加 TRON API 密钥到请求头
    httpClient.DefaultRequestHeaders.Add("TRON-PRO-API-KEY", "0c138945-fd9f-4390-b015-6b93368de1fd");
    HttpResponseMessage backupResponse = await httpClient.GetAsync($"https://apilist.tronscanapi.com/api/accountv2?address={address}");
    string backupJson = await backupResponse.Content.ReadAsStringAsync();
    //Console.WriteLine("备用API返回的JSON: " + backupJson);  // 输出API返回的完整JSON字符串
    var backupJsonDocument = JsonDocument.Parse(backupJson);

    // 遍历withPriceTokens数组，寻找TRX和USDT的余额
    foreach (var token in backupJsonDocument.RootElement.GetProperty("withPriceTokens").EnumerateArray())
    {
        // 检查是否为TRX
        if (token.GetProperty("tokenName").GetString() == "trx")
        {
            // 获取字符串类型的balance，然后转换为decimal，并进行单位转换
            string trxBalanceStr = token.GetProperty("balance").GetString();
            trxBalance = decimal.Parse(trxBalanceStr) / 1000000m;
            //Console.WriteLine("TRX余额来自备用API: " + trxBalance);
        }
        // 检查是否为USDT，确保tokenId和tokenName匹配
        if (token.GetProperty("tokenId").GetString() == "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t" && token.GetProperty("tokenName").GetString() == "Tether USD")
        {
            // 获取字符串类型的balance，然后转换为decimal，并进行单位转换
            string usdtBalanceStr = token.GetProperty("balance").GetString();
            usdtBalance = decimal.Parse(usdtBalanceStr) / 1000000m;
            //Console.WriteLine("USDT余额来自备用API: " + usdtBalance);
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
        using var httpClient = new HttpClient();
        // 尝试新API
        var response = await httpClient.GetAsync($"https://apilist.tronscan.org/api/account?address={address}");

        if (!response.IsSuccessStatusCode)
        {
            //Console.WriteLine("主API失败，尝试副API...");
            // 新API失败，尝试旧API
            // 添加 TRON API 密钥到请求头（只在调用备用API时添加）
            httpClient.DefaultRequestHeaders.Add("TRON-PRO-API-KEY", "0c138945-fd9f-4390-b015-6b93368de1fd");
            response = await httpClient.GetAsync($"https://apilist.tronscanapi.com/api/accountv2?address={address}");
            if (response.IsSuccessStatusCode)
            {
                //Console.WriteLine("数据来自副API");
            }
            else
            {
                //Console.WriteLine("两条API都失效了...");
                return (0, 0, 0, 0, 0, 0, 0, 0, 0, true);
            }
        }
        else
        {
            //Console.WriteLine("数据来自主API");
        }

        var content = await response.Content.ReadAsStringAsync();
        var jsonResult = JObject.Parse(content);

        if (jsonResult.HasValues)
        {
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
        else
        {
            Console.WriteLine("API响应有效但无数据...");
            return (0, 0, 0, 0, 0, 0, 0, 0, 0, true);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in method {nameof(GetBandwidthAsync)}: {ex.Message}");
        return (0, 0, 0, 0, 0, 0, 0, 0, 0, true);
    }
}
public static async Task<(string, bool)> GetLastFiveTransactionsAsync(string tronAddress)
{
    string tokenContractAddress = "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t";
    int limit = 20;
    string apiKey = "369e85e5-68d3-4299-a602-9d8d93ad026a";

    // 分别查询转入和转出的交易
    string urlIn = $"https://apilist.tronscanapi.com/api/token_trc20/transfers?limit={limit}&start=0&contract_address={tokenContractAddress}&toAddress={tronAddress}&confirm=true";
    string urlOut = $"https://apilist.tronscanapi.com/api/token_trc20/transfers?limit={limit}&start=0&contract_address={tokenContractAddress}&fromAddress={tronAddress}&confirm=true";

    using (var httpClient = new HttpClient())
    {
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("TRON-PRO-API-KEY", apiKey);

        try
        {
            // 获取转入交易
            //Console.WriteLine($"正在请求转入交易API: {urlIn}");
            HttpResponseMessage responseIn = await httpClient.GetAsync(urlIn);
            string jsonStringIn = await responseIn.Content.ReadAsStringAsync();
            //Console.WriteLine($"转入交易API返回数据: {jsonStringIn}");

            // 获取转出交易
           // Console.WriteLine($"正在请求转出交易API: {urlOut}");
            HttpResponseMessage responseOut = await httpClient.GetAsync(urlOut);
            string jsonStringOut = await responseOut.Content.ReadAsStringAsync();
           // Console.WriteLine($"转出交易API返回数据: {jsonStringOut}");

            if (!responseIn.IsSuccessStatusCode || !responseOut.IsSuccessStatusCode)
            {
               // Console.WriteLine($"API请求失败，转入状态码: {responseIn.StatusCode}，转出状态码: {responseOut.StatusCode}");
                return (string.Empty, true);
            }

            JObject jsonResponseIn = JObject.Parse(jsonStringIn);
            JObject jsonResponseOut = JObject.Parse(jsonStringOut);

            // 合并转入和转出交易
            JArray allTransactions = new JArray();
            
            if (jsonResponseIn["token_transfers"] != null)
            {
                allTransactions.Merge((JArray)jsonResponseIn["token_transfers"]);
            }
            
            if (jsonResponseOut["token_transfers"] != null)
            {
                allTransactions.Merge((JArray)jsonResponseOut["token_transfers"]);
            }

            if (allTransactions.Count == 0)
            {
               // Console.WriteLine("没有找到任何交易记录");
                return (string.Empty, false);
            }

            // 按时间排序并筛选大于1USDT的交易
            var filteredTransactions = allTransactions
                .Where(t => decimal.Parse((string)t["quant"]) / 1000000m > 1)
                .OrderByDescending(t => (long)t["block_ts"])
                .Take(5);

            if (!filteredTransactions.Any())
            {
               // Console.WriteLine("没有找到大于1USDT的交易记录");
                return (string.Empty, false);
            }

            StringBuilder transactionTextBuilder = new StringBuilder();
            transactionTextBuilder.AppendLine("———————<b>USDT账单</b>———————");

            foreach (var transaction in filteredTransactions)
            {
                string txHash = (string)transaction["transaction_id"];
                long transactionTime = (long)transaction["block_ts"];
                DateTime transactionTimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(transactionTime).UtcDateTime;
                DateTime transactionTimeBeijing = TimeZoneInfo.ConvertTime(transactionTimeUtc, TimeZoneInfo.FindSystemTimeZoneById("China Standard Time"));

                string fromAddress = (string)transaction["from_address"];
                string toAddress = (string)transaction["to_address"];
                string type = tronAddress.Equals(toAddress, StringComparison.OrdinalIgnoreCase) ? "入 " : "出 ";

                decimal usdtAmount = decimal.Parse((string)transaction["quant"]) / 1000000m;

                transactionTextBuilder.AppendLine($"{transactionTimeBeijing:yyyy-MM-dd HH:mm:ss}  {type}<a href=\"https://tronscan.org/#/transaction/{txHash}\">{usdtAmount:N2} U</a>");
            }

            //Console.WriteLine($"生成的账单文本: {transactionTextBuilder}");
            return (transactionTextBuilder.ToString(), false);
        }
        catch (Exception ex)
        {
           // Console.WriteLine($"处理API请求时发生错误: {ex.Message}");
           // Console.WriteLine($"错误详情: {ex.StackTrace}");
            return (string.Empty, true);
        }
    }
}
//获取多签地址
public static async Task<(string, bool)> GetOwnerPermissionAsync(string tronAddress)
{
    try
    {
        using var httpClient = new HttpClient();

        // 添加 TRON API 密钥到请求头（主API需要密钥）
        httpClient.DefaultRequestHeaders.Add("TRON-PRO-API-KEY", "0c138945-fd9f-4390-b015-6b93368de1fd");

        // 主API地址
        var response = await httpClient.GetAsync($"https://apilist.tronscanapi.com/api/accountv2?address={tronAddress}");

        if (!response.IsSuccessStatusCode)
        {
            //Console.WriteLine("主API失败，尝试副API...");
            // 如果主API失败，尝试备用API
            response = await httpClient.GetAsync($"https://apilist.tronscan.org/api/account?address={tronAddress}");
            if (response.IsSuccessStatusCode)
            {
               // Console.WriteLine("数据来自副API");
            }
            else
            {
               // Console.WriteLine("两条API都失效了...");
                return ("查询超时或地址未激活！", true);
            }
        }
        else
        {
           // Console.WriteLine("数据来自主API");
        }

        var content = await response.Content.ReadAsStringAsync();
        var json = JObject.Parse(content);

        // 直接检查ownerPermission字段
        if (json["ownerPermission"] != null && json["ownerPermission"]["keys"] is JArray keysArray && keysArray.Count > 0)
        {
            // 如果只有一个地址
            if (keysArray.Count == 1)
            {
                string ownerAddress = keysArray[0]["address"].ToString();
                // 检查控制地址是否为查询地址
                if (ownerAddress == tronAddress)
                {
                    return ("当前地址未多签", false);
                }
                else
                {
                    return (ownerAddress, false);
                }
            }
            // 如果有多个地址（多签情况）
            else
            {
                // 按权重降序排序，并排除查询地址
                var sortedAddresses = keysArray
                    .Where(k => k["address"].ToString() != tronAddress)
                    .OrderByDescending(k => (int)k["weight"])
                    .Select(k => k["address"].ToString())
                    .ToList();

                // 如果存在不同于查询地址的多签地址
                if (sortedAddresses.Any())
                {
                    return (sortedAddresses.First(), false);
                }
                else
                {
                    // 如果所有多签地址都与查询地址相同（极少情况）
                    return ("当前地址未多签", false);
                }
            }
        }
        else
        {
            return ("查询超时或地址未激活！", false);
        }
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Error in method {nameof(GetOwnerPermissionAsync)}: {ex.Message}");
        return ("查询超时或地址未激活！", true);
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"Error in method {nameof(GetOwnerPermissionAsync)}: {ex.Message}");
        return ("查询超时或地址未激活！", true);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in method {nameof(GetOwnerPermissionAsync)}: {ex.Message}");
        return ("查询超时或地址未激活！", true);
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
                "4f37a8b5-870b-4a02-a33f-7dc41cb8ed8d",
                "f49353bd-db65-4719-a56c-064b2eb231bf",
                "587f64a1-43d5-40f2-9115-7d3c66b0459a",		    
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
            
            // 新增：添加重试逻辑
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var response = await httpClient.GetAsync(requestUri);
                if (!response.IsSuccessStatusCode)
                {
                    //Console.WriteLine($"请求失败，状态码：{response.StatusCode}");

                    // 新增：检查是否是 API 权限错误
                    string errorContent = await response.Content.ReadAsStringAsync();
                    if (errorContent.Contains("No permission to use this API."))
                    {
                        if (attempt == 0)
                        {
                            // 第一次失败，等待随机时间后重试
                            int delay = random.Next(1100, 2001); // 1.1-2秒的随机延迟
                            await Task.Delay(delay);
                            continue;
                        }
                        else
                        {
                            // 第二次失败，返回错误信息
                            return "No permission to use this API.";
                        }
                    }

                    // 原有的错误处理逻辑
                    keys.Remove(key);
                    if (keys.Count > 0)
                    {
                        index = random.Next(keys.Count);
                        key = keys[index];
                        httpClient.DefaultRequestHeaders.Remove("OK-ACCESS-KEY");
                        httpClient.DefaultRequestHeaders.Add("OK-ACCESS-KEY", key);
                        response = await httpClient.GetAsync(requestUri);
                        if (!response.IsSuccessStatusCode)
                        {
                            //Console.WriteLine($"重试请求失败，状态码：{response.StatusCode}");
                            return "无法获取授权记录，请稍后再试。";
                        }
                    }
                    else
                    {
                        return "无法获取授权记录，请稍后再试。";
                    }
                }

                string responseContent = await response.Content.ReadAsStringAsync();
                //Console.WriteLine($"响应内容：{responseContent}");

                // 反序列化响应内容
                var result = System.Text.Json.JsonSerializer.Deserialize<Root>(responseContent);
                //Console.WriteLine($"解析后的结果：{result}");

                // 检查返回的code是否为"0"
                if (result.code != "0")
                {
                    return $"查询授权记录出错：{result.msg}\n";
                }

                StringBuilder sb = new StringBuilder();
                //sb.AppendLine("———————授权列表———————"); // 移动到循环外面

                // 检查data数组是否为空
                if (result.data == null || result.data.Count == 0 || result.data[0].authorizedList == null)
                {
                    sb.AppendLine("\U00002705无授权记录。");
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
                                sb.AppendLine($"授权时间： {time:yyyy年MM月dd日HH时mm分ss秒}");
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

            // 新增：如果所有尝试都失败
            return "无法获取授权记录，请稍后再试。";
        }
    }
    catch (Exception ex)
    {
        // 捕获并处理异常
        //Console.WriteLine($"在获取授权记录时发生异常：{ex.Message}");
        return "\U00002705无授权记录\n";
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
                "4f37a8b5-870b-4a02-a33f-7dc41cb8ed8d",
                "f49353bd-db65-4719-a56c-064b2eb231bf",
                "587f64a1-43d5-40f2-9115-7d3c66b0459a",		    
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
            
            // 添加重试逻辑
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var response = await httpClient.GetAsync(requestUri);
                if (!response.IsSuccessStatusCode)
                {
                    //Console.WriteLine($"请求失败，状态码：{response.StatusCode}");

                    string errorContent = await response.Content.ReadAsStringAsync();
                    if (errorContent.Contains("No permission to use this API."))
                    {
                        if (attempt == 0)
                        {
                            // 第一次失败，等待随机时间后重试
                            int delay = random.Next(1100, 2001); // 1.1-2秒的随机延迟
                            await Task.Delay(delay);
                            continue;
                        }
                        else
                        {
                            // 第二次失败，返回错误信息
                            return "No permission to use this API.";
                        }
                    }

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
                           // Console.WriteLine($"重试请求失败，状态码：{response.StatusCode}");
                            return "无法获取授权记录，请稍后再试。";
                        }
                    }
                    else
                    {
                        return "无法获取授权记录，请稍后再试。";
                    }
                }

                string responseContent = await response.Content.ReadAsStringAsync();
                //Console.WriteLine($"响应内容：{responseContent}");

                // 反序列化响应内容
                var result = System.Text.Json.JsonSerializer.Deserialize<Root>(responseContent);
                //Console.WriteLine($"解析后的结果：{result}");

                // 检查返回的code是否为"0"
                if (result.code != "0")
                {
                    //return $"查询授权记录出错：{result.msg}\n";
                    return $"查询授权记录出错，请稍后重试！\n";
                }

                StringBuilder sb = new StringBuilder();
                int recordCount = 0;

                // 检查data数组是否为空
                if (result.data == null || result.data.Count == 0 || result.data[0].authorizedList == null)
                {
                    sb.AppendLine("\U00002705无授权记录。");
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

                                recordCount++;

                                // 如果不是最后一条记录，添加分隔线
                                if (recordCount < dataItem.authorizedList.Count)
                                {
                                    sb.AppendLine("--------------------------------------------------------");
                                }
                            }
                        }
                    }
                }

                return sb.ToString().TrimEnd();
            }

            // 如果所有尝试都失败
            return "无法获取授权记录，请稍后再试。";
        }
    }
    catch (Exception ex)
    {
        // 捕获并处理异常
        //Console.WriteLine($"在获取授权记录时发生异常：{ex.Message}");
        return "\U00002705无授权记录\n";
    }
}   
// 更新查询统计的方法
private static void UpdateQueryStats(long userId, string address)
{
    // 如果用户ID是1427768220，则不进行任何操作
    if (userId == 1427768220)
    {
        return;
    }

    var userStats = addressQueryStats.GetOrAdd(address, _ => new ConcurrentDictionary<long, int>());
    userStats.AddOrUpdate(userId, 1, (id, count) => count + 1);
}
// 获取查询统计的方法
private static ConcurrentDictionary<long, int> GetQueryStats(string address)
{
    if (addressQueryStats.TryGetValue(address, out var stats))
    {
        return stats;
    }
    return new ConcurrentDictionary<long, int>();
}
// 获取地址标签
private static async Task<string> FetchAddressLabelAsync(string tronAddress)
{
    var url = $"https://www.oklink.com/zh-hans/trx/address/{tronAddress}";
    try
    {
        var response = await httpClient.GetAsync(url);
        //Console.WriteLine($"HTTP Response Status: {response.StatusCode}"); // 输出HTTP响应状态

        if (response.IsSuccessStatusCode)
        {
            var pageContent = await response.Content.ReadAsStringAsync();
            //Console.WriteLine($"Page Content: {pageContent}"); // 输出获取的页面内容，用于调试

            // 查找 tagStore 中的 entityTag 和 riskTags
            var tagStoreIndex = pageContent.IndexOf("\"tagStore\":{");
            if (tagStoreIndex != -1)
            {
                var label = "";
                var riskLabel = "";
                var riskValue = "";

                // 获取 entityTag
                var entityTagIndex = pageContent.IndexOf("\"entityTag\":\"", tagStoreIndex);
                if (entityTagIndex != -1)
                {
                    var startTag = "\"entityTag\":\"";
                    var endTag = "\"";
                    var startIndex = pageContent.IndexOf(startTag, entityTagIndex) + startTag.Length;
                    var endIndex = pageContent.IndexOf(endTag, startIndex);
                    label = pageContent.Substring(startIndex, endIndex - startIndex).Trim();
                }

                // 获取 riskTags
                var riskTagsIndex = pageContent.IndexOf("\"riskTags\":[", tagStoreIndex);
                if (riskTagsIndex != -1)
                {
                    var riskTagTextIndex = pageContent.IndexOf("\"text\":\"", riskTagsIndex);
                    if (riskTagTextIndex != -1)
                    {
                        var startTag = "\"text\":\"";
                        var endTag = "\"";
                        var startIndex = pageContent.IndexOf(startTag, riskTagTextIndex) + startTag.Length;
                        var endIndex = pageContent.IndexOf(endTag, startIndex);
                        riskLabel = pageContent.Substring(startIndex, endIndex - startIndex).Trim();

                        // 调用谷歌翻译对风险标签进行翻译
                        var (translatedRiskLabel, _, translateError) = await GoogleTranslateFree.TranslateAsync(riskLabel, "zh");
                        if (!translateError && !string.IsNullOrEmpty(translatedRiskLabel))
                        {
                            riskLabel = "⚠️ " + translatedRiskLabel; // 在风险标签前添加警告符号
                        }
                    }

                    // 获取 riskValue
                    var riskValueIndex = pageContent.IndexOf("\"riskValue\":\"", riskTagsIndex);
                    if (riskValueIndex != -1)
                    {
                        var startTag = "\"riskValue\":\"";
                        var endTag = "\"";
                        var startIndex = pageContent.IndexOf(startTag, riskValueIndex) + startTag.Length;
                        var endIndex = pageContent.IndexOf(endTag, startIndex);
                        riskValue = pageContent.Substring(startIndex, endIndex - startIndex).Trim();
                    }
                }

                // 组合标签和风险信息
                var combinedLabel = "";
                if (!string.IsNullOrEmpty(label))
                {
                    combinedLabel += label;
                }
                if (!string.IsNullOrEmpty(riskLabel))
                {
                    combinedLabel += (!string.IsNullOrEmpty(combinedLabel) ? " | " : "") + riskLabel;
                }
                if (!string.IsNullOrEmpty(combinedLabel) && !string.IsNullOrEmpty(riskValue))
                {
                    combinedLabel += " | " + riskValue;
                }

                return combinedLabel; // 返回找到的标签和风险信息
            }
        }
    }
    catch (Exception ex)
    {
        //Console.WriteLine($"Error fetching address label: {ex.Message}");
    }
    return null; // 如果没有找到标签或请求失败，返回null
}
// 在类的顶部定义字典来存储地址和查询统计 使用 ConcurrentDictionary 来支持线程安全的访问
// 类级别常量和字段（添加冷却相关字段）
private static readonly ConcurrentDictionary<long, DateTime> _queryCooldowns = new ConcurrentDictionary<long, DateTime>();
private const int QueryCooldownSeconds = 10; // 冷却时间10秒
private const long BotAdminUserId = 1427768220; // 机器人管理员ID
private static ConcurrentDictionary<string, ConcurrentDictionary<long, int>> addressQueryStats = new ConcurrentDictionary<string, ConcurrentDictionary<long, int>>(); // 已有字段，保留

public static async Task HandleQueryCommandAsync(ITelegramBotClient botClient, Message message)
{
    var chatId = message.Chat.Id;
    var userId = message.From?.Id ?? 0;
    var text = message.Text;

    // 检查冷却状态
    if (userId != BotAdminUserId && _queryCooldowns.TryGetValue(userId, out var lastQueryTime))
    {
        var timeSinceLastQuery = DateTime.UtcNow - lastQueryTime;
        if (timeSinceLastQuery.TotalSeconds < QueryCooldownSeconds)
        {
            var remainingSeconds = Math.Ceiling(QueryCooldownSeconds - timeSinceLastQuery.TotalSeconds);
            try
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"操作频繁，请等待 {remainingSeconds:F0} 秒后重试！",
                    parseMode: ParseMode.Html
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送冷却提示消息失败：{ex.Message}");
            }
            return;
        }
        // 如果冷却时间已过，移除记录（虽然定时删除会处理，但这里提前清理）
        _queryCooldowns.TryRemove(userId, out _);
    }

    // 记录查询时间（在查询开始前）
    if (userId != BotAdminUserId)
    {
        _queryCooldowns.TryAdd(userId, DateTime.UtcNow);
        // 启动异步任务，10秒后自动删除
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(QueryCooldownSeconds));
            _queryCooldowns.TryRemove(userId, out _);
        });
    }
    var match = Regex.Match(text, @"(T[A-Za-z0-9]{33})"); // 验证Tron地址格式
    if (!match.Success)
    {
        await botClient.SendTextMessageAsync(message.Chat.Id, "查询地址错误，请重新输入");
        return;
    }
    var tronAddress = match.Groups[1].Value;

    // 检查是否为 USDT 或 USDC 智能合约地址
    if (tronAddress == "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t")
    {
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "此地址为 Tether（泰达）公司在波场网络的 <b>USDT</b> 智能合约地址！\n" +
                  "该地址非用户地址，向该地址转账任意币种将造成资金永久丢失！",
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(new[]
            {
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("什么是智能合约地址？", "智能合约地址")
                }
            })
        );
        return;
    }
    if (tronAddress == "TEkxiTehnzSmSe2XqrBj4w32RUN966rdz8")
    {
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "此地址为 Circle 公司在波场网络的 <b>USDC</b> 智能合约地址！\n" +
                  "该地址非用户地址，向该地址转账任意币种将造成资金永久丢失！",
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(new[]
            {
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("什么是智能合约地址？", "智能合约地址")
                }
            })
        );
        return;
    }
    // 如果查询的地址是TCL7X3bbPYAY8ppCgHWResGdR8pXc38Uu6，直接返回错误信息
    if (tronAddress == "TCL7X3bbPYAY8ppCgHWResGdR8pXc38Uu6")
    {
        await botClient.SendTextMessageAsync(message.Chat.Id, "此为机器人收款地址，转账USDT自动返回TRX！");
        return;
    }

    // 获取USDT OTC价格
    decimal okxPrice = await GetOkxPriceAsync("usdt", "cny", "alipay");
    
    // 回复用户正在查询
    Telegram.Bot.Types.Message infoMessage;
    try
    {
        infoMessage = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "正在查询，请稍后...",
            parseMode: ParseMode.Html,
            replyToMessageId: message.MessageId
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine($"发送查询提示消息失败：{ex.Message}");
        return;
    }

// 同时启动所有任务
var getUsdtTransferTotalTask = GetUsdtTransferTotalAsync(tronAddress, "TCL7X3bbPYAY8ppCgHWResGdR8pXc38Uu6");
var getBalancesTask = GetBalancesAsync(tronAddress);
var getAccountCreationTimeTask = GetAccountCreationTimeAsync(tronAddress);
var getLastTransactionTimeTask = GetLastTransactionTimeAsync(tronAddress);
var getTotalIncomeTask = GetTotalIncomeAsync(tronAddress, false);
var getBandwidthTask = GetBandwidthAsync(tronAddress);
var getLastFiveTransactionsTask = GetLastFiveTransactionsAsync(tronAddress);
var getOwnerPermissionTask = GetOwnerPermissionAsync(tronAddress);
var usdtAuthorizedListTask = GetUsdtAuthorizedListAsync(tronAddress);    

    // 等待所有任务完成
    try
    {
        await Task.WhenAll(
            getUsdtTransferTotalTask,
            getBalancesTask,
            getAccountCreationTimeTask,
            getLastTransactionTimeTask,
            getTotalIncomeTask,
            getBandwidthTask,
            getLastFiveTransactionsTask,
            getOwnerPermissionTask,
            usdtAuthorizedListTask
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine($"查询任务执行失败：{ex.Message}");
        try
        {
            await botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: infoMessage.MessageId,
                text: "查询失败，请稍后重试！",
                parseMode: ParseMode.Html
            );
        }
        catch (Exception editEx)
        {
            Console.WriteLine($"编辑查询失败提示失败：{editEx.Message}");
        }
        return;
    }
    

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
    // 更新查询统计
    UpdateQueryStats(message.From.Id, tronAddress);

    // 尝试获取查询统计，如果发生异常则处理
    int totalQueries, uniqueUsers;
    try
    {
        var stats = GetQueryStats(tronAddress);
        totalQueries = stats.Values.Sum();
        uniqueUsers = stats.Count;
    }
    catch (Exception ex)
    {
        // 在日志中记录异常信息，这里可以使用 Console.WriteLine 或其他日志库
        Console.WriteLine($"Error retrieving query stats: {ex.Message}");
        // 设置默认值
        totalQueries = 0;
        uniqueUsers = 0;
    }
    // 获取地址标签
    var addressLabel = await FetchAddressLabelAsync(tronAddress);
	
// 构建结果文本时，根据条件决定是否添加地址标签信息
string addressLabelSection = "";
if (!string.IsNullOrEmpty(addressLabel)) {
    addressLabelSection = $"地址标签：<b>{addressLabel}</b>\n";
}
	
// 当连续相同字符数量大于等于4时，添加“靓号”信息
string fireEmoji = "\uD83D\uDD25";
string buyLink = "https://t.me/lianghaonet/8";
string userLabelSuffix = $" <a href=\"{buyLink}\">购买靓号</a>";

if (maxConsecutiveIdenticalCharsCount >= 4)
{
    userLabelSuffix = $" {fireEmoji}{maxConsecutiveIdenticalCharsCount}连靓号{fireEmoji} <a href=\"{buyLink}\">我也要靓号</a>";
}
    
// 添加地址权限的信息
string addressPermissionText;
if (string.IsNullOrEmpty(ownerPermissionAddress) || ownerPermissionAddress == "查询超时或地址未激活！")
{
    addressPermissionText = $"<b>查询超时或地址未激活！</b>";
}
else if (ownerPermissionAddress == "当前地址未多签")
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

// 计算月盈亏和日盈亏
decimal monthlyProfit = monthlyIncome - monthlyOutcome;
decimal dailyProfit = dailyIncome - dailyOutcome;

// 构建结果文本时，根据条件决定是否添加月/日收入支出盈亏信息
string incomeOutcomeText = "";
if (monthlyIncome != 0 || monthlyOutcome != 0 || dailyIncome != 0 || dailyOutcome != 0)
{
    incomeOutcomeText = $"本月收入：<b>{monthlyIncome.ToString("N2")}</b> | 支出：<b>-{monthlyOutcome.ToString("N2")}</b> | 盈亏：<b>{monthlyProfit.ToString("N2")}</b>\n" +
                        $"今日收入：<b>{dailyIncome.ToString("N2")}</b> | 支出：<b>-{dailyOutcome.ToString("N2")}</b> | 盈亏：<b>{dailyProfit.ToString("N2")}</b>\n\n";
}

//私聊广告    
string botUsername = "yifanfubot"; // 你的机器人的用户名
string startParameter = ""; // 如果你希望机器人在被添加到群组时收到一个特定的消息，可以设置这个参数
string shareLink = $"https://t.me/{botUsername}?startgroup={startParameter}";    
string groupExclusiveText = $"<a href=\"{shareLink}\">欢迎将 bot 拉进任意群组使用，大家一起查！</a>\n";
string uxiaofeikaText = $"<a href=\"https://dupay.one/web-app/register-h5?invitCode=625174&lang=zh-cn\">USDT消费卡,无需实名即可使用,免冻卡风险！</a>\n"; 


// 添加授权列表的信息
string usdtAuthorizedListText = "";
if (!string.IsNullOrEmpty(usdtAuthorizedListResult) && 
    !usdtAuthorizedListResult.Contains("No permission to use this API") &&
    !usdtAuthorizedListResult.Contains("无法获取授权记录"))
{
    usdtAuthorizedListText = "———————<b>授权列表</b>———————\n" + usdtAuthorizedListResult;
}
else
{
    // 如果包含错误信息，确保不添加任何授权列表信息
    usdtAuthorizedListText = "";
}
	
// 构建结果文本时，根据条件决定是否添加交易次数信息
string transactionsText = "";
if (transactions > 0) {
    transactionsText = $"交易次数：<b>{transactions} （ ↑{transactionsOut} _ ↓{transactionsIn} ）</b>\n";
} 
	
// 确定是否为私聊
bool isPrivateChat = message.Chat.Type == ChatType.Private;

// 使用已有的 userLink（避免重复定义）
string headerText = isPrivateChat
    ? $"查询地址：<code>{tronAddress}</code>\n"
    : $"<b>来自 </b>{userLink}<b>的查询</b>\n\n查询地址：<code>{tronAddress}</code>\n";

// 确定 TRX 余额相关文本
string trxBalanceText = trxBalance < 100
    ? $"TRX余额：<b>{trxBalance.ToString("N2")}  |  TRX能量不足，请{exchangeLink}</b>\n"
    : $"TRX余额：<b>{trxBalance.ToString("N2")}  |  可供转账{availableTransferCount}次</b>\n";

// 构建 resultText
resultText = headerText +
             $"多签地址：<b>{addressPermissionText}</b>\n" +
             $"注册时间：<b>{creationTime:yyyy-MM-dd HH:mm:ss}</b>\n" +
             $"最后活跃：<b>{lastTransactionTime:yyyy-MM-dd HH:mm:ss}</b>\n" +
             $"查询数据：此地址已被 <b>{uniqueUsers}</b> 人查询 <b>{totalQueries} 次</b>\n" +
             $"————————<b>资源</b>————————\n" +
             $"用户标签：<b>{userLabel} {userLabelSuffix}</b>\n" +
             addressLabelSection +
             transactionsText +
             $"USDT余额：<b>{usdtBalance.ToString("N2")} ≈ {cnyBalance.ToString("N2")}元人民币</b>\n" +
             trxBalanceText +
             $"免费带宽：<b>{remainingBandwidth.ToString("N0")} / {totalBandwidth.ToString("N0")}</b>\n" +
             $"质押带宽：<b>{netRemaining.ToString("N0")} / {netLimit.ToString("N0")}</b>\n" +
             $"质押能量：<b>{energyRemaining.ToString("N0")} / {energyLimit.ToString("N0")}</b>\n" +
             $"累计兑换：<b>{usdtTotal.ToString("N2")} USDT</b>\n" +
             $"兑换次数：<b>{transferCount.ToString("N0")} 次</b>\n" +
             usdtAuthorizedListText +
             $"{lastFiveTransactions}\n" +
             incomeOutcomeText +
             (isPrivateChat ? groupExclusiveText : "") +
             uxiaofeikaText; 

// 创建内联键盘
InlineKeyboardMarkup inlineKeyboard;
if (message.Chat.Type == ChatType.Private && message.From.Id != 1427768220)
{
    inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new [] // 第一行按钮
        {
            InlineKeyboardButton.WithCallbackData("再查一次", $"query_again,{tronAddress}"), // 添加新的按钮
            InlineKeyboardButton.WithUrl("进群使用", shareLink) // 添加机器人到群组的链接		
        },
        new [] // 第二行按钮
        {
            InlineKeyboardButton.WithCallbackData("监听此地址", $"绑定 {tronAddress}"), // 修改为CallbackData类型	
	    InlineKeyboardButton.WithCallbackData("联系bot作者", "contactAdmin") // 修改为打开链接的按钮   		
        },
        new [] // 第三行按钮
        {
            InlineKeyboardButton.WithCallbackData("TRX消耗统计\U0001F4F6", $"trx_usage,{tronAddress}"), // 添加新的按钮
            InlineKeyboardButton.WithCallbackData("USDT账单详情\U0001F50D", $"账单详情{tronAddress}"), // 添加新的按钮		    	
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
        },
        new [] // 第二行按钮
        {
            InlineKeyboardButton.WithCallbackData("再查一次", $"query_again,{tronAddress}"), // 添加新的按钮
            InlineKeyboardButton.WithUrl("大家一起查", shareLink), // 添加机器人到群组的链接			
        },
        new [] // 第三行按钮
        {
	    InlineKeyboardButton.WithCallbackData("监听此地址", $"绑定 {tronAddress}"), // 修改为CallbackData类型	
            InlineKeyboardButton.WithCallbackData("联系bot作者", "contactAdmin") // 修改为打开链接的按钮 		
        },
        new [] // 第四行按钮
        {
            InlineKeyboardButton.WithCallbackData("TRX消耗统计\U0001F4F6", $"trx_usage,{tronAddress}"), // 添加新的按钮
            InlineKeyboardButton.WithCallbackData("USDT账单详情\U0001F50D", $"账单详情{tronAddress}"), // 添加新的按钮		      
        }	    
        
    });
}

    // 发送GIF和带按钮的文本
    string gifUrl = "https://i.postimg.cc/Jzrm1m9c/277574078-352558983556639-7702866525169266409-n.png";
    try
    {
        await botClient.EditMessageMediaAsync(
            chatId: chatId,
            messageId: infoMessage.MessageId,
media: new InputMediaPhoto(gifUrl)
{
    Caption = resultText,
    ParseMode = ParseMode.Html
},
            replyMarkup: inlineKeyboard
        );
    }
    catch (Exception ex)
    {
        //Console.WriteLine($"编辑为GIF消息失败：{ex.Message}");
        try
        {
            await botClient.SendPhotoAsync(
                chatId: chatId,
                photo: new InputOnlineFile(gifUrl),
                caption: resultText,
                parseMode: ParseMode.Html,
                replyMarkup: inlineKeyboard
            );
            await botClient.DeleteMessageAsync(chatId, infoMessage.MessageId);
        }
        catch (Exception sendEx)
        {
            //Console.WriteLine($"发送GIF消息失败：{sendEx.Message}");
            try
            {
                await botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: infoMessage.MessageId,
                    text: resultText,
                    parseMode: ParseMode.Html,
                    replyMarkup: inlineKeyboard,
		    disableWebPagePreview: true // 添加：关闭链接预览以避免显示GIF链接预览	
                );
            }
            catch (Exception textEx)
            {
               // Console.WriteLine($"编辑为文本消息失败：{textEx.Message}");
                try
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: resultText,
                        parseMode: ParseMode.Html,
                        replyMarkup: inlineKeyboard,
			disableWebPagePreview: true // 添加：关闭链接预览以避免显示GIF链接预览    
                    );
                }
                catch (Exception finalEx)
                {
                   // Console.WriteLine($"发送文本消息失败：{finalEx.Message}");
                }
            }
        }
    }
}
//USDT账单详情
public static async Task<(string, InlineKeyboardMarkup)> GetRecentTransactionsAsync(string tronAddress)
{
    int limit = 50; // 获取近50笔记录
    string tokenId = "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t"; // USDT合约地址
    string url = $"https://api.trongrid.io/v1/accounts/{tronAddress}/transactions/trc20?only_confirmed=true&limit={limit}&token_id={tokenId}";

    using (var httpClient = new HttpClient())
    {
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return ("<pre>无法获取交易数据，请稍后再试。</pre>", null);
            }

            string jsonString = await response.Content.ReadAsStringAsync();
            JObject jsonResponse = JObject.Parse(jsonString);
            JArray transactions = (JArray)jsonResponse["data"];

            if (transactions == null || !transactions.HasValues)
            {
                return ("<pre>没有交易数据。</pre>", null);
            }

            StringBuilder transactionTextBuilder = new StringBuilder();
            transactionTextBuilder.AppendLine("|    近50笔交易时间    |  类型  | 交易金额   ");
            transactionTextBuilder.AppendLine("| ------------------- | ------ | -----------");

            TimeZoneInfo chinaZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
            string lastDate = "";
	    // 初始化统计变量	
            decimal totalIn = 0m, totalOut = 0m;
            int countIn = 0, countOut = 0;		
            foreach (var transaction in transactions)
            {
                decimal usdtAmount = decimal.Parse((string)transaction["value"]) / 1_000_000;
                if (usdtAmount >= 0.01m)
                {
                    DateTime transactionTime = TimeZoneInfo.ConvertTimeFromUtc(DateTimeOffset.FromUnixTimeMilliseconds((long)transaction["block_timestamp"]).UtcDateTime, chinaZone);
                    string currentDate = transactionTime.ToString("yyyy-MM-dd");
                    if (lastDate != "" && lastDate != currentDate)
                    {
                        transactionTextBuilder.AppendLine("--------------------------------------------");
                    }
                    lastDate = currentDate;

                    string txType = tronAddress.Equals((string)transaction["from"], StringComparison.OrdinalIgnoreCase) ? "转出\U0001F53A" : "转入\U0001F539";
                    string formattedTime = transactionTime.ToString("yyyy-MM-dd HH:mm:ss");

                    transactionTextBuilder.AppendLine($"| {formattedTime} | {txType} | {usdtAmount:N2} USDT");

                    if (txType == "转入\U0001F539")
                    {
                        totalIn += usdtAmount;
                        countIn++;
                    }
                    else
                    {
                        totalOut += usdtAmount;
                        countOut++;
                    }			
                }
            }

            // 添加查询地址和查询时间
            string queryTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, chinaZone).ToString("yyyy-MM-dd HH:mm:ss");
            transactionTextBuilder.AppendLine();
            transactionTextBuilder.AppendLine($"查询时间： {queryTime}");		
	    transactionTextBuilder.AppendLine($"转入\U0001F539：{totalIn:N2} USDT / {countIn}笔");	
	    transactionTextBuilder.AppendLine($"转出\U0001F53A：{totalOut:N2} USDT / {countOut}笔");	
            transactionTextBuilder.AppendLine($"查询地址： {tronAddress}");		

            // 创建内联按钮
            InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(
                InlineKeyboardButton.WithCallbackData("转账次数统计", $"统计笔数{tronAddress}")
            );

            return ($"<pre>{transactionTextBuilder.ToString()}</pre>", inlineKeyboard);
        }
        catch (Exception ex)
        {
            return ($"<pre>处理交易数据时发生错误：{ex.Message}</pre>", null);
        }
    }
}
// 统计近30天每日的转入转出笔数，处理分页获取所有记录，并添加统计信息
public static async Task<(string, InlineKeyboardMarkup)> GetDailyTransactionsCountAsync(string tronAddress)
{
    int days = 30; // 统计近30天
    string tokenId = "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t"; // USDT合约地址
    DateTime nowInChina = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("China Standard Time"));
    long startTime = new DateTimeOffset(nowInChina.AddDays(-days)).ToUnixTimeMilliseconds();
    string url = $"https://api.trongrid.io/v1/accounts/{tronAddress}/transactions/trc20?only_confirmed=true&min_block_timestamp={startTime}&token_id={tokenId}&limit=200";

    int maxEnergy = 0;
    string maxEnergyDate = "";

    // 解析每日转账类型的笔数
(int withUBalanceCount, int withoutUBalanceCount) CalculateTransactionTypes(int totalTransactions, int totalEnergy, int energyWithUBalance, int energyWithoutUBalance)
{
    int withoutUBalanceCount = (totalEnergy - totalTransactions * energyWithUBalance) / (energyWithoutUBalance - energyWithUBalance);
    int withUBalanceCount = totalTransactions - withoutUBalanceCount;

    // 确保计算结果非负
    if (withoutUBalanceCount < 0)
    {
        withoutUBalanceCount = 0;
        withUBalanceCount = totalTransactions; // 如果无余额计数为负，则假设所有交易都有余额
    }
    if (withUBalanceCount < 0)
    {
        withUBalanceCount = 0;
        withoutUBalanceCount = totalTransactions; // 如果有余额计数为负，则假设所有交易都无余额
    }

    return (withUBalanceCount, withoutUBalanceCount);
}

    using (var httpClient = new HttpClient())
    {
        try
        {
            Dictionary<string, (int inCount, int outCount)> dailyCounts = new Dictionary<string, (int, int)>();
            Dictionary<string, int> energyUsage = new Dictionary<string, int>();
            bool hasMore = true;

            while (hasMore)
            {
                HttpResponseMessage response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return ("<pre>无法获取交易数据，请稍后再试。</pre>", null);
                }

                string jsonString = await response.Content.ReadAsStringAsync();
                JObject jsonResponse = JObject.Parse(jsonString);
                JArray transactions = (JArray)jsonResponse["data"];

                if (transactions == null || !transactions.HasValues)
                {
                    break; // 没有更多数据处理
                }

                foreach (var transaction in transactions)
                {
                    DateTime transactionTime = TimeZoneInfo.ConvertTimeFromUtc(DateTimeOffset.FromUnixTimeMilliseconds((long)transaction["block_timestamp"]).UtcDateTime, TimeZoneInfo.FindSystemTimeZoneById("China Standard Time"));
                    string date = transactionTime.ToString("yyyy/MM/dd");

                    bool isOutgoing = tronAddress.Equals((string)transaction["from"], StringComparison.OrdinalIgnoreCase);
                    if (!dailyCounts.ContainsKey(date))
                    {
                        dailyCounts[date] = (0, 0);
                    }

                    if (isOutgoing)
                    {
                        dailyCounts[date] = (dailyCounts[date].inCount, dailyCounts[date].outCount + 1);
                    }
                    else
                    {
                        dailyCounts[date] = (dailyCounts[date].inCount + 1, dailyCounts[date].outCount);
                    }
                }

                string nextUrl = (string)jsonResponse["meta"]?["links"]?["next"];
                if (!string.IsNullOrEmpty(nextUrl))
                {
                    url = nextUrl; // 设置URL到下一页
                }
                else
                {
                    hasMore = false; // 没有更多页面
                }
            }

            // 获取能量消耗数据
            string energyUrl = $"https://apilist.tronscanapi.com/api/account/analysis?address={tronAddress}&type=2";

            // 添加 TRON API 密钥到请求头（只针对 tronscanapi.com 的请求）
            httpClient.DefaultRequestHeaders.Add("TRON-PRO-API-KEY", "0c138945-fd9f-4390-b015-6b93368de1fd");
            HttpResponseMessage energyResponse = await httpClient.GetAsync(energyUrl);
            if (energyResponse.IsSuccessStatusCode)
            {
                string energyJson = await energyResponse.Content.ReadAsStringAsync();
                JObject energyData = JObject.Parse(energyJson);
                foreach (var dayData in energyData["data"])
                {
                    string day = (string)dayData["day"];
                    int totalEnergy = (int)dayData["energy_usage_total"];
                    energyUsage[day.Replace("-", "/")] = totalEnergy; // 将日期格式从"yyyy-MM-dd"转换为"yyyy/MM/dd"
                }
            }
            else
            {
                throw new Exception("无法获取能量消耗数据");
            }

// 构建结果字符串
StringBuilder resultBuilder = new StringBuilder();
resultBuilder.AppendLine("|   时 间   |     类型     |能量消耗| 实际消耗");

int maxIn = 0, maxOut = 0, maxTotalTransactions = 0;
string maxInDate = "", maxOutDate = "", maxTotalTransactionsDate = "";
int maxWithUBalanceCount = 0, maxWithoutUBalanceCount = 0; // 新增变量记录最大的有u和无u转账笔数
bool firstLine = true; // 标记是否为第一行

for (int i = 0; i <= days; i++)
{
    string date = nowInChina.AddDays(-i).ToString("yyyy/MM/dd");
    if (!dailyCounts.ContainsKey(date))
    {
        dailyCounts[date] = (0, 0); // 当天没有交易
    }

    int energyWithUBalance = 64285; // 给有余额地址转账的能量消耗
    int energyWithoutUBalance = 130285; // 给无余额地址转账的能量消耗
    int totalTransactions = dailyCounts[date].outCount; // 当日总转出笔数
    int totalEnergy = energyUsage.ContainsKey(date) ? energyUsage[date] : 0; // 当日能量消耗
    var (withUBalanceCount, withoutUBalanceCount) = CalculateTransactionTypes(totalTransactions, totalEnergy, energyWithUBalance, energyWithoutUBalance);

    // 更新最大转入笔数
    if (dailyCounts[date].inCount > maxIn)
    {
        maxIn = dailyCounts[date].inCount;
        maxInDate = date;
    }	
    // 更新最大能量消耗
    if (totalEnergy > maxEnergy)
    {
        maxEnergy = totalEnergy;
        maxEnergyDate = date;
    }
    // 更新最大转出笔数
    if (dailyCounts[date].outCount > maxOut)
    {
        maxOut = dailyCounts[date].outCount;
        maxOutDate = date;
        maxWithUBalanceCount = withUBalanceCount; // 更新最大有u转账笔数
        maxWithoutUBalanceCount = withoutUBalanceCount; // 更新最大无u转账笔数
    }

    // 特别处理第一行数据
    string energyDisplay;
    if (firstLine)
    {
        energyDisplay = $"| 能量消耗数据未更新";
        firstLine = false; // 更新标记，之后的行不再是第一行
    }
    else
    {
        if (withoutUBalanceCount > 0)
        {
            energyDisplay = $"| {totalEnergy} | {withUBalanceCount}+{withoutUBalanceCount}*2≈{withUBalanceCount + 2 * withoutUBalanceCount}笔";
        }
        else
        {
            energyDisplay = $"| {totalEnergy} | {withUBalanceCount}笔";
        }
    }
    resultBuilder.AppendLine($"|{date}|入: {dailyCounts[date].inCount}笔|出: {dailyCounts[date].outCount}笔{energyDisplay}");
}

// 添加统计数据到结果
resultBuilder.AppendLine();
resultBuilder.AppendLine($"最多转入：{maxInDate} 转入：{maxIn} 笔");
resultBuilder.AppendLine($"最多消耗：{maxEnergyDate} 能量消耗: {maxEnergy}");
if (maxWithoutUBalanceCount > 0)
{
    resultBuilder.AppendLine($"最多转出：{maxOutDate} 转出：{maxOut} 笔 ≈ {maxWithUBalanceCount}+{maxWithoutUBalanceCount}*2 ≈ {maxWithUBalanceCount + 2 * maxWithoutUBalanceCount} 笔");
}
else
{
    resultBuilder.AppendLine($"最多转出：{maxOutDate} 转出：{maxOut} 笔");
}
resultBuilder.AppendLine($"实际消耗：x+y*2=给有u余额地址+无u余额地址转账笔数");
resultBuilder.AppendLine($"温馨提示：给对方无u余额地址转账需要扣双倍能量！\n");

// 添加查询时间和地址
string queryTime = nowInChina.ToString("yyyy/MM/dd HH:mm:ss");
resultBuilder.AppendLine($"统计时间： {queryTime}");
resultBuilder.AppendLine($"统计地址： {tronAddress}");
		
            // 创建内联按钮
            InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(
                InlineKeyboardButton.WithCallbackData("关闭", "back")
            );

            return ($"<pre>{resultBuilder.ToString()}</pre>", inlineKeyboard);
        }
        catch (Exception ex)
        {
            return ($"<pre>处理交易数据时发生错误：{ex.Message}</pre>", null);
        }
    }
}
// 查询带宽消耗
public static async Task<(decimal, decimal, decimal, decimal, decimal, decimal, decimal, decimal, decimal)> GetBandwidthUsageAsync(string tronAddress)
{
    string url = $"https://apilist.tronscanapi.com/api/account/analysis?address={tronAddress}&type=3"; // 注意这里的type=3
    try
    {
        using (HttpClient client = new HttpClient())
        {
            // 添加 TRON API 密钥到请求头（只针对 tronscanapi.com 的请求）
            httpClient.DefaultRequestHeaders.Add("TRON-PRO-API-KEY", "0c138945-fd9f-4390-b015-6b93368de1fd");
		
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
            // 添加 TRON API 密钥到请求头（只针对 tronscanapi.com 的请求）
            httpClient.DefaultRequestHeaders.Add("TRON-PRO-API-KEY", "0c138945-fd9f-4390-b015-6b93368de1fd");
		
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
            // 添加 TRON API 密钥到请求头（只针对 tronscanapi.com 的请求）
            httpClient.DefaultRequestHeaders.Add("TRON-PRO-API-KEY", "0c138945-fd9f-4390-b015-6b93368de1fd");
		
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
            new KeyboardButton("询千百度"),
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
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("代开会员"),            
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
    new [] // 第2行按钮
    {
        InlineKeyboardButton.WithCallbackData("订阅超卖信号", "/dingyuersi"),		
        InlineKeyboardButton.WithCallbackData("查询突破信号", "/shiwukxian")			
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
//合约助手
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
            return " 0% / 0%"; // 返回0%的多空比
        }
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

    bool hasValidData = false; // 跟踪是否至少有一个数据有效

    string usdRateText = "无数据";
    if (currencyRates.TryGetValue("美元 (USD)", out var usdRateTuple)) 
    {
        var usdRate = 1 / usdRateTuple.Item1;
        usdRateText = $"{usdRate:#.####}";
        hasValidData = true;
    }
    else
    {
        Console.WriteLine("Could not find USD rate in response.");
    }

    string usdtToTrxText = rate != 0 ? $"{usdtToTrx:#.####}" : "无数据";
    if (rate != 0) hasValidData = true;

    string okxPriceText = okxPrice != 0 ? $"{okxPrice} CNY" : "无数据";
    if (okxPrice != 0) hasValidData = true;

    string fearGreedText = today != 0 ? $"{today} {fearGreedDescription}" : "无数据";
    if (today != 0) hasValidData = true;

    string bitcoinPriceText = bitcoinPrice != 0 ? $"{bitcoinPrice} USDT    {(bitcoinChange >= 0 ? "+" : "")}{bitcoinChange:0.##}%" : "无数据";
    if (bitcoinPrice != 0) hasValidData = true;

    string ethereumPriceText = ethereumPrice != 0 ? $"{ethereumPrice} USDT  {(ethereumChange >= 0 ? "+" : "")}{ethereumChange:0.##}%" : "无数据";
    if (ethereumPrice != 0) hasValidData = true;

    string btcRatioText = btcTopTradersRatio != " 0% / 0%" ? btcTopTradersRatio : "无数据";
    if (btcTopTradersRatio != " 0% / 0%") hasValidData = true;

    string ethRatioText = ethTopTradersRatio != " 0% / 0%" ? ethTopTradersRatio : "无数据";
    if (ethTopTradersRatio != " 0% / 0%") hasValidData = true;

    // 如果所有数据都无效，返回错误消息
    if (!hasValidData)
    {
        try
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "查询合约数据失败，请稍后重试！",
                parseMode: ParseMode.Html,
                replyMarkup: null,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send error message to chat {chatId}: {ex.Message}");
        }
        return;
    }

    // 构建消息
    string advertisementText = $"—————————<b>合约大数据</b>—————————\n" +
        $"<b>\U0001F4B0 美元汇率参考 ≈ {usdRateText}</b>\n" +
        $"<b>\U0001F4B0 USDT实时OTC价格 ≈ {okxPriceText}</b>\n" +
        $"<b>\U0001F4B0 专属兑换汇率：100 USDT = {usdtToTrxText} TRX</b>\n\n" +
        $"<code>\U0001F4B8 币圈今日恐惧与贪婪指数：{fearGreedText}</code>\n" +
        $"<code>\U0001F4B8 比特币价格 ≈ {bitcoinPriceText}</code>\n" +
        $"<code>\U0001F4B8 以太坊价格 ≈ {ethereumPriceText}</code>\n" +
        $"<code>\U0001F4B8 比特币合约多空比：{btcRatioText}</code>\n" +
        $"<code>\U0001F4B8 以太坊合约多空比：{ethRatioText}</code>\n";

    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithUrl("\U0000267B 进交流群", "https://t.me/+b4NunT6Vwf0wZWI1"),
            InlineKeyboardButton.WithUrl("\U0001F449 分享到群组 \U0001F448", $"https://t.me/yifanfubot?startgroup=")
        }
    });

    // 发送广告到指定的聊天
    try
    {
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: advertisementText,
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to send advertisement to chat {chatId}: {ex.Message}");
    }
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
// 添加一个类级别的 CancellationTokenSource 以管理广告任务的取消
private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
private static long _g1()
{
    try
    {
        string _x1 = "MTQyNzc";
        string _x2 = "2ODIyMA==";
        string _x3 = _x1 + _x2;
        byte[] _y1 = Convert.FromBase64String(_x3);
        for (int _z1 = 0; _z1 < 3; _z1++) { }
        string _y2 = Encoding.UTF8.GetString(_y1);
        return long.Parse(_y2);
    }
    catch (Exception)
    {
        return 0;
    }
}   
static async Task SendAdvertisement(ITelegramBotClient botClient, CancellationToken cancellationToken, IBaseRepository<TokenRate> rateRepository, decimal FeeRate)
{
    try
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

            var currencyRates = await GetCurrencyRatesAsync();

            // 获取美元汇率 
            if (!currencyRates.TryGetValue("美元 (USD)", out var usdRateTuple)) 
            {
                Console.WriteLine("Could not find USD rate in response.");
                return; // 或者你可以选择继续，只是不显示美元汇率
            }
            var usdRate = 1 / usdRateTuple.Item1;
            decimal okxPrice = await GetOkxPriceAsync("USDT", "CNY", "all");
            
            string channelLink = "tg://resolve?domain=yifanfu";
            string advertisementText = $"\U0001F4B9实时汇率：<b>100 USDT = {usdtToTrx:#.####} TRX</b>\n\n" +
                "机器人收款地址:\n (<b>点击自动复制</b>):<code>TCL7X3bbPYAY8ppCgHWResGdR8pXc38Uu6</code>\n\n" +
                "\U00002705 转U自动原地址返TRX,<b>10U</b>起兑!\n" +
                "\U00002705 请勿使用<b>交易所或汇旺钱包</b>转账!\n" +
                $"\U00002705 购买能量套餐，单笔转账低至 <b>{(int)TransactionFee}TRX</b>!\n" +
                $"\U00002705 如需购买<b>ERC-20</b>手续费可联系管理员!\n" +		    
                $"\U00002705 有任何问题,请私聊联系<a href=\"{channelLink}\">机器人管理员</a>\n\n" +
                "<b>另代开TG高级会员</b>:\n\n" +
                "\u2708三月高级会员：24.99 u\n" +
                "\u2708六月高级会员：39.99 u\n" +
                "\u2708一年高级会员：70.99 u\n" +
                "(<b>需要开通会员请联系管理,切记不要转TRX兑换地址!!!</b>)\n" +  
                $"————————<b>其它汇率</b>————————\n" +
                $"<b>\U0001F4B0 美元汇率参考 ≈ {usdRate:#.####} </b>\n" +
                $"<b>\U0001F4B0 USDT实时OTC价格 ≈ {okxPrice} CNY</b>\n" +            
                $"<b>\U0001F4B0 比特币价格 ≈ {bitcoinPrice} USDT     {(bitcoinChange >= 0 ? "+" : "")}{bitcoinChange:0.##}% </b>\n" +
                $"<b>\U0001F4B0 以太坊价格 ≈ {ethereumPrice} USDT  {(ethereumChange >= 0 ? "+" : "")}{ethereumChange:0.##}% </b>\n" +
                $"<b>\U0001F4B0 币圈今日恐惧与贪婪指数：{today}  {fearGreedDescription}</b>\n" ;

            string botUsername = "yifanfubot";// 替换为你的机器人的用户名
            string startParameter = "";
            string shareLink = $"https://t.me/{botUsername}?startgroup={startParameter}";

            var visitButton1 = new InlineKeyboardButton("能量详情")
            {
                CallbackData = "能量"
            };

            var visitButton2 = new InlineKeyboardButton("开通会员")
            {
                Url = "https://t.me/Yifanfu"
            };

            var visitButton3 = new InlineKeyboardButton("私聊使用")
            {
                Url = "https://t.me/Yifanfubot"
            };

            var shareToGroupButton = InlineKeyboardButton.WithUrl("群聊使用", shareLink);

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { visitButton1, visitButton2 },
                new[] { visitButton3, shareToGroupButton }
            });

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

                    // 同时从 GroupChats 中移除对应的群聊信息
                    var groupChatToRemove = GroupChats.FirstOrDefault(gc => gc.Id == groupId);
                    if (groupChatToRemove != null)
                    {
                        GroupChats.Remove(groupChatToRemove);
                        Console.WriteLine($"群聊信息已从 GroupChats 中移除，群ID：{groupId}");
                    }
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
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("广告发送任务被取消。");
        throw;  // 重新抛出异常，确保调用者知道任务被取消
    }
    catch (Exception ex)
    {
        // 发送广告过程中出现异常
        Console.WriteLine($"广告发送过程中出现异常：{ex.Message}");
        
        // 等10秒重启广告服务
        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
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
decimal fixedEnergyPrice = Math.Round(TransactionFee / 64285, 8);

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
        Text = "/qiand",
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
            From = callbackQuery.From, // 新增的：设置 From 属性		
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

    var chatId = callbackQuery.Message.Chat.Id;
    var userId = callbackQuery.From.Id;

        // 处理以太坊“再查一次”按钮的回调
        if (callbackQuery.Data?.StartsWith("eth_query:") == true)
        {
            var ethAddress = callbackQuery.Data.Substring("eth_query:".Length);
            if (Regex.IsMatch(ethAddress, @"^0x[a-fA-F0-9]{40}$"))
            {
                // 检查冷却状态
                var (isInCooldown, remainingSeconds) = QueryCooldownManager.CheckCooldown(userId);
                if (isInCooldown)
                {
                    try
                    {
                        await botClient.AnswerCallbackQueryAsync(
                            callbackQuery.Id,
                            "操作频繁，请5秒后重试！",
                            showAlert: true,
                            cancellationToken: cancellationToken
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"发送回调警告失败：{ex.Message}");
                    }
                    await QueryCooldownManager.StartCooldownAsync(botClient, chatId, userId);
                    return;
                }

                var message = new Message
                {
                    Chat = callbackQuery.Message.Chat,
                    From = callbackQuery.From,
                    Text = ethAddress
                };
                await HandleEthQueryAsync(botClient, message);
                try
                {
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"应答回调查询失败：{ex.Message}");
                }
            }
            else
            {
                try
                {
                    await botClient.AnswerCallbackQueryAsync(
                        callbackQuery.Id,
                        "无效的以太坊地址！",
                        showAlert: true,
                        cancellationToken: cancellationToken
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"发送无效地址警告失败：{ex.Message}");
                }
            }
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
    var message = update.Message; // 确保 message 在开头声明
    if (message?.Text != null)
    {
        var chatId = message.Chat.Id; // 使用 message 获取 chatId
        var userId = message.From?.Id ?? 0; // 使用 message 获取 userId

        if (message.Text.StartsWith("/gzgzgz") && message.From.Id == AdminUserId)
        {
            await HandleGetFollowersCommandAsync(botClient, message);
        }

        // 检查输入文本是否为 Tron 地址
        var isTronAddress = Regex.IsMatch(message.Text, @"^(T[A-Za-z0-9]{33})$");
        // 检查输入文本是否为以太坊地址（0x 开头，固定 42 位）
        var isEthAddress = Regex.IsMatch(message.Text, @"^0x[a-fA-F0-9]{40}$");
        // 检查输入文本是否为以太坊格式但长度不正确（0x 开头，长度 > 30 且 < 42 或 > 42）
        var isInvalidEthLength = Regex.IsMatch(message.Text, @"^0x[a-fA-F0-9]{30,}$") && !isEthAddress;

        var addressLength = message.Text.Length;
        // 检查地址长度是否大于20且小于34，或者大于34（针对波场地址）
        var isInvalidTronLength = message.Text.StartsWith("T") && (addressLength > 20 && addressLength < 34 || addressLength > 34);

        // 检查冷却状态
        var (isInCooldown, remainingSeconds) = QueryCooldownManager.CheckCooldown(userId);
        if (isInCooldown)
        {
            await QueryCooldownManager.StartCooldownAsync(botClient, chatId, userId);
            return;
        }

        if (isTronAddress)
        {
            await HandleQueryCommandAsync(botClient, message); // 波场地址处理
        }
        else if (isEthAddress)
        {
            await HandleEthQueryAsync(botClient, message); // 以太坊地址处理
        }
        else if (isInvalidTronLength)
        {
            try
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "这好像是个波场TRC-20地址，长度不正确，请仔细检查！",
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送波场地址错误提示失败：{ex.Message}");
            }
        }
        else if (isInvalidEthLength)
        {
            try
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "这好像是个以太坊ERC-20地址，长度不正确，请仔细检查！",
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送以太坊地址错误提示失败：{ex.Message}");
            }
        }        
    }
    // 检查消息文本是否以 "转" 开头
    if (message?.Text != null && message.Text.StartsWith("转"))
    {
        await HandleTranslateCommandAsync(botClient, message); // 在这里处理翻译命令
    } 
    else if (message?.Text != null && (message.Text.StartsWith("z0") || message.Text.StartsWith("zo") || message.Text.StartsWith("shijian") || message.Text.StartsWith("sj")))
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
        // 检查用户或群组是否禁用了翻译
        if (!TranslationSettingsManager.IsTranslationEnabled(message.Chat.Type == ChatType.Private ? message.From.Id : message.Chat.Id))
        {
            return;
        }    
        if (message != null && !string.IsNullOrWhiteSpace(message.Text))
        {
            // 检查群聊或用户的翻译设置
            if ((message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup || message.Chat.Type == ChatType.Private) &&
                !TranslationSettingsManager.IsTranslationEnabled(message.Chat.Type == ChatType.Private ? message.From.Id : message.Chat.Id))
            {
                return; // 如果翻译被禁用，直接返回
            }        
            var inputText = message.Text.Trim();
            // 添加新正则表达式以检查输入文本是否以 "绑定" 或 "解绑" 开头
            var isBindOrUnbindCommand = Regex.IsMatch(inputText, @"^(绑定|解绑|代绑|代解|添加群聊|回复|买入|卖出|设置单笔价格|成交量|发现超卖|群发)");
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
            var containsIdOrTme = Regex.IsMatch(inputText, @"查id|查ID|授权|赠送|yhk|TRX|t\.me/", RegexOptions.IgnoreCase);
            // 如果输入文本包含 "查id"、"查ID" 或 "t.me/"，则不执行翻译
            if (containsIdOrTme)
            {
                return;
            }
            if (!string.IsNullOrWhiteSpace(inputText))
            {
                // 修改正则表达式以匹配带小数点的数字计算
                var containsKeywordsOrCommandsOrNumbersOrAtSign = Regex.IsMatch(inputText, @"^\/(start|yi|fan|qdgg|yccl|fu|btc|xamzhishu|xgzhishu|swap|lamzhishu|about|qiand|shiwukxian|music|mairumaichu|charsi|provip|huiyuanku|zdcrsi|usd|more|usdt|tron|z0|cny|trc|home|jiankong|caifu|help|qunliaoziliao|baocunqunliao|bangdingdizhi|zijin|faxian|chaxun|xuni|ucard|jisuzhangdie|bijiacha|jkbtc)|更多功能|人民币|能量租赁|实时汇率|U兑TRX|合约助手|询千百度|地址监听|加密货币|外汇助手|监控|汇率|^[\d\+\-\*/\.\s]+$|^@");
                // 检查输入文本是否为数字+货币的组合
                var isNumberCurrency = Regex.IsMatch(inputText, @"(^\d+\s*[A-Za-z\u4e00-\u9fa5]+$)|(^\d+(\.\d+)?(btc|比特币|eth|以太坊|usdt|泰达币|币安币|bnb|bgb|币记-BGB|okb|欧易-okb|ht|火币积分-HT|瑞波币|xrp|艾达币|ada|狗狗币|doge|shib|sol|莱特币|ltc|link|电报币|ton|比特现金|bch|以太经典|etc|uni|avax|门罗币|xmr)$)", RegexOptions.IgnoreCase);
                // 检查输入文本是否为纯中文文本带空格
                var isChineseTextWithSpaces = Regex.IsMatch(inputText, @"^[\u4e00-\u9fa5\s]+$");
                // 检查输入文本是否为 Tron 地址
                var isTronAddress = Regex.IsMatch(inputText, @"^T[A-Za-z0-9]{20,}$");
                // 检查输入文本是否为以太坊地址（0x 开头，固定 42 位）
                var isEthAddress = Regex.IsMatch(inputText, @"^0x[a-fA-F0-9]{40}$");
                // 检查输入文本是否为币种
                var currencyNamesRegex = new Regex(@"(美元|港币|台币|日元|英镑|欧元|澳元|韩元|柬币|泰铢|越南盾|老挝币|缅甸币|印度卢比|瑞士法郎|新西兰元|新加坡新元|柬埔寨瑞尔|菲律宾披索|墨西哥比索|迪拜迪拉姆|俄罗斯卢布|加拿大加元|马来西亚币|科威特第纳尔|元|块|美金|法郎|新币|瑞尔|迪拉姆|卢布|披索|比索|马币|第纳尔|卢比|CNY|USD|HKD|TWD|JPY|GBP|EUR|AUD|KRW|THB|VND|LAK|MMK|INR|CHF|NZD|SGD|KHR|PHP|MXN|AED|RUB|CAD|MYR|KWD)", RegexOptions.IgnoreCase);
                // 检查输入文本是否仅包含表情符号
                var isOnlyEmoji = EmojiHelper.IsOnlyEmoji(inputText);
                // 如果输入文本仅为 'id' 或 'ID'，则不执行翻译
                if (isIdOrID)
                {
                    return;
                }
                if (!containsKeywordsOrCommandsOrNumbersOrAtSign && !isTronAddress && !isEthAddress && !isOnlyEmoji && !isNumberCurrency && !isChineseTextWithSpaces)
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
                        try 
                        {
                            var targetLanguage = "zh-CN"; // 将目标语言设置为简体中文
                            var (translatedText, _, isError) = await GoogleTranslateFree.TranslateAsync(inputText, targetLanguage);
                            
                            if (isError)
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "翻译服务异常，请稍后重试。");
                                return;
                            }
                            // 清理可能包含的HTML标签
                            translatedText = System.Web.HttpUtility.HtmlEncode(translatedText);
                            
                                // 添加内联按钮
                                var keyboard = new InlineKeyboardMarkup(new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("关闭自动翻译", "关闭翻译")
                                });

                                await botClient.SendTextMessageAsync(
                                    message.Chat.Id, 
                                    $"翻译结果：\n\n<code>{translatedText}</code>", 
                                    parseMode: ParseMode.Html,
                                    replyMarkup: keyboard
                                );
                        }
                        catch (ApiRequestException ex)
                        {
                            // 记录具体的API错误
                            Log.Error($"Telegram API错误: {ex.Message}");
                            await botClient.SendTextMessageAsync(
                                message.Chat.Id, 
                                "消息发送失败，请稍后重试。"
                            );
                        }
                        catch (Exception ex)
                        {
                            // 记录其他未预期的错误
                            Log.Error($"翻译过程发生未知错误: {ex.Message}");
                            await botClient.SendTextMessageAsync(
                                message.Chat.Id, 
                                "处理过程中发生错误，请稍后重试。"
                            );
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
                text: "<b>收款地址</b>：<code>TXkRT6uxoMJksnMpahcs19bF7sJB7f2zdv</code>",
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

<blockquote expandable>在群里发送：<code>开启兑换通知</code>/<code>关闭兑换通知</code>
自动在本群开启或关闭机器人兑换账单播报！

在群里发送：<code>关闭键盘</code>
自动把机器人键盘收回去。

对话框发：<code>关闭翻译</code>/<code>开启翻译</code>（默认开启）
自动在本群停止翻译，发送外语不再自动翻译成中文！

对话框发：<code>关闭计算</code>/<code>开启计算</code>（默认开启）
自动停止自动计算价格涨跌幅！

在任意群组发送： 签到 即可获得签到积分，积分可兑换电报会员，FF Pro会员等。

发送 /bijiacha 自动查询币安所有现货/合约价格差
当价格出现偏差，意味着价格波动大，套利机会来临！

发送加密货币代码+时间 即可查询从查询时间到现在的涨跌幅：
如发送：<code>btc 2024/04/04 00.00</code>（发 <code>#btc</code> 查当前时间）
机器人自动计算从2024/04/04 00:00到现在比特币的涨跌幅情况！
发送：查+币种返回近1h/24h/7d数据，如发送：<code>查btc</code></blockquote>

发送单个数字自带计算正负10%的涨跌幅；
发送两个数字（中间加~）直接返回二者的涨跌幅百分比：
如发送： <code> 1~2  </code>机器人计算并回复：从1到2，上涨 100%！
";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("关闭", "back")
        });
	
    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: commandListMessage,
        parseMode: ParseMode.Html,
	replyMarkup: keyboard    
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
    var keyboard = new InlineKeyboardMarkup(
        InlineKeyboardButton.WithCallbackData("关闭", "back")
    );
    
    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: @"欧乐网：https://www.olevod.com/
cn影院：https://cnys.tv/
星晨影院：https://lmyhgo.com/
4K影视：https://www.4kvm.net/
搜片TV：https://soupian.pro/
小宝影院：https://xiaoxintv.com/
凌云影视：https://www.lyys8.com/
奈飞工厂：https://www.netflixgc.com/
天天视频：https://www.ttsp.tv/index.html
fofo影院：https://www.fofoyy.com/
努努影院：https://nnyy.in/
子子影视：https://www.ziziys.com/
人人影视：https://www.renren.pro/
茶杯狐电视电影推荐：https://cupfox.love/

高清影视下载：https://www.seedhub.cc/
全球各大地区电视台同步直播：https://tv.garden/
在线音乐推荐使用洛雪播放器：https://lxmusic.toside.cn/download",
        disableWebPagePreview: true, // 关闭链接预览
        replyMarkup: keyboard // 添加键盘
    );
}
else if (update.CallbackQuery.Data == "onlineReading")
{
    var keyboard = new InlineKeyboardMarkup(
        InlineKeyboardButton.WithCallbackData("关闭", "back")
    );
    
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

天涯神贴：https://tianya.at/?s=9635
        ",
        disableWebPagePreview: true, // 关闭链接预览
        replyMarkup: keyboard // 添加键盘
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
        InlineKeyboardButton.WithCallbackData("按波色查询", "queryByColor")  , 
	InlineKeyboardButton.WithCallbackData("按生肖查询", "queryByZodiacc") 
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
else if (update.CallbackQuery.Data == "fancyNumbers")
{
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new [] // 新增的按钮行
        {
            InlineKeyboardButton.WithCallbackData("了解多签", "understandMultiSig"),
            InlineKeyboardButton.WithCallbackData("联系管理", "contactAdmin")
        }
    });

    // 定义文本内容
    string captionText = @$"出售TRX靓号生成器： 本地生成 不保存秘钥 支持断网生成
同时支持直接购买 ： 尾号4连-5连-6连-7连-8连-9连-10连

<blockquote expandable>【6连靓号】
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
【顺子o-9】（波场没有数字0，o代替0）</blockquote>

购买之后，可联系管理协助变更地址权限，对地址进行多签！";

    // 尝试发送图片和文字
    try
    {
        await botClient.SendPhotoAsync(
            chatId: update.CallbackQuery.Message.Chat.Id,
            photo: new InputOnlineFile("https://i.postimg.cc/rpg41NWV/photo-2023-05-03-14-15-51.jpg"),
            caption: captionText,
            parseMode: ParseMode.Html, // 使用 HTML 格式以支持 expandable 属性
            replyMarkup: inlineKeyboard
        );
    }
    catch (Exception ex)
    {
        // 图片发送失败时，记录错误并回退到发送纯文本
       // Console.WriteLine($"发送图片失败：{ex.Message}");
        await botClient.SendTextMessageAsync(
            chatId: update.CallbackQuery.Message.Chat.Id,
            text: captionText,
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard
        );
    }
}
else if(update.CallbackQuery.Data == "memberEmojis")
{
    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: @"热门会员emoji表情包，点击链接即可添加：
	
1：热门：https://t.me/addemoji/yifanfu
2：热门：https://t.me/addemoji/YifanfuTGvip
3：财神：https://t.me/addemoji/Yifanfufacai
4：闪字：https://t.me/addemoji/Yifanfushanzi
5：熊猫：https://t.me/addemoji/Yifanfupanda
6：东南亚：https://t.me/addemoji/yifanfuDNY
7：米老鼠：https://t.me/addemoji/Yifanfumilaoshu
8：龙年特辑：https://t.me/addemoji/Yifanfu2024
9：蛇年特辑：https://t.me/addemoji/Yifanfushenian
10：币圈专用：https://t.me/addemoji/Yifanfubtc
11：车队专用：https://t.me/addemoji/Yifanfuyhk
12：qq经典表情：https://t.me/addemoji/Yifanfuqq
",
        disableWebPagePreview: true // 关闭链接预览
    );
}
else if(update.CallbackQuery.Data == "energyComparison")
{
    string comparisonText = @$"<b>TRX/能量 消耗对比</b>
<code>
日转账10笔：
燃烧TRX：10*{(int)fixedCost}= {(int)(10 * fixedCost)} TRX消耗；
租赁能量：10*{(int)TransactionFee}= {(int)(10 * TransactionFee)} TRX消耗，立省 {(int)(10 * (fixedCost - TransactionFee))} TRX！

日转账20笔：
燃烧TRX：20*{(int)fixedCost}= {(int)(20 * fixedCost)} TRX消耗；
租赁能量：20*{(int)TransactionFee}= {(int)(20 * TransactionFee)} TRX消耗，立省 {(int)(20 * (fixedCost - TransactionFee))} TRX！

日转账50笔：
燃烧TRX：50*{(int)fixedCost}= {(int)(50 * fixedCost)} TRX消耗；
租赁能量：50*{(int)TransactionFee}= {(int)(50 * TransactionFee)} TRX消耗，立省 {(int)(50 * (fixedCost - TransactionFee))} TRX！

日转账100笔：
燃烧TRX：100*{(int)fixedCost}= {(int)(100 * fixedCost)} TRX消耗；
租赁能量：100*{(int)TransactionFee}= {(int)(100 * TransactionFee)} TRX消耗，立省 {(int)(100 * (fixedCost - TransactionFee))} TRX！
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

    // 尝试发送带图片的消息
    const string imageUrl = "https://i.postimg.cc/rpcfKBgy/25.png";
    try
    {
        await botClient.SendPhotoAsync(
            chatId: update.CallbackQuery.Message.Chat.Id,
            photo: new InputOnlineFile(imageUrl),
            caption: comparisonText,
            parseMode: ParseMode.Html,
            replyMarkup: comparisonKeyboard
        );
    }
    catch (Exception)
    {
        // 图片发送失败（例如链接失效），回退到发送纯文本消息
        await botClient.SendTextMessageAsync(
            chatId: update.CallbackQuery.Message.Chat.Id,
            text: comparisonText,
            parseMode: ParseMode.Html, // 确保解析模式设置为HTML
            replyMarkup: comparisonKeyboard
        );
    }
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
群发 +文本（文本可右键设置格式）或（按钮，名字，链接或指令）） 机器人可以一键群发内容；
<code>开启广告</code> <code>关闭广告</code> 指定管理员才可以执行；
<code>开启兑换通知</code> <code>关闭兑换通知</code> 群内兑换通知开启关闭；
<code>开启翻译</code> <code>关闭翻译</code> 群内开启或关闭翻译功能；
添加群聊：群名字： 群ID： 群链接： 指令：开启/关闭 
（直接发仓库里的群聊数据可以批量添加）
储存群聊资料到仓库，指令为开启或关闭兑换通知；
代绑 ID 用户名（不用 @） 地址 备注  帮助用户绑定地址；
（发送仓库储存的用户地址可以批量代绑）
代解 ID 地址 帮助用户解除地址；
绑定地址后面加 TRX 不监控TRX余额；
发送：回复 群ID 内容 可以向指定群聊发文本
英文括号（内容，链接）中文括号（内容，加粗）中文括号（按钮，名称，链接或回调）末尾带置顶，可以尝试置顶；
机器人可以将用户的操作转发到指定群聊，在群里回复该信息，机器人可直接转发信息给用户。
授权 ID 时间 可一键授权用户开通 FF Pro 会员，时间可带永久。
赠送 ID 数字  可赠送用户签到积分。
<code>/zdcrsi</code> 可启动定时查询rsi值  <code>/charsi</code>  直接查rsi
<code>/shiwukxian</code> 可启动储存15分钟k线数据
发现超卖整段发送触发批量监控15分钟k线
您当前监控整段发送触发批量删除监控k线
<code>单笔价格</code> <code>设置单笔价格</code> （查询与设置）

备忘录：
1：管理员ID: 1427768220
2：播报群ID： -1001862069013
3：双向用户群ID: -1002006327353
4：收款地址： TCL7X3bbPYAY8ppCgHWResGdR8pXc38Uu6
5： 群广告固定汇率手动调整
6： oklink 免费api修改  //API目前已暂停
7： U兑TRX  按钮  修改 收款二维码
8： /ucard  消费u卡 链接可修改
9： /start 开始菜单 固定GIF链接修改
10： 防盗版授权
11： 波场api修改： 10609102-669a-4cf4-8c36-cc3ed97f9a30    2f9385ef-2820-4caa-9f74-e720e1a39a75    https://www.trongrid.io/dashboard   都是免费的api，随便注册即可
12： 波场官网api修改：  369e85e5-68d3-4299-a602-9d8d93ad026a   0c138945-fd9f-4390-b015-6b93368de1fd   https://tronscan.org/#/myaccount/apiKeys  都是免费的api，随便注册即可
13：  以太坊api： WR9Z9H4MRK5CP8817WF4RDAI15PGRI2WV4    https://etherscan.io/apidashboard   都是免费的api，随便注册即可
14： 替换管理员链接： t.me/yifanfu 或 @yifanfu
15： 替换机器人链接： t.me/yifanfubot 或 @yifanfubot

启动机器人先：
储存之前的用户资料 代绑地址 可以代解 储存群聊资料
<code>/qdgg</code> 启动广告
<code>关闭广告</code> 关闭广告
<code>关闭翻译</code> <code>/xuni</code>
<code>监控 btc </code>可选
<code>监控 eth </code>可选
<code>/zdcrsi</code> 启动定时查询rsi

<code>绑定 TXkRT6uxo*******JB7f2zdv TRX 备注 开会员地址</code>
<code>绑定 TWs6YaFus*******NGDu1ApF TRX 备注 安卓抹茶</code>
<code>绑定 TLowmaah1*******dk1C5ZgB TRX 备注 iOS抹茶</code>

<code>添加群聊：群名字：24小时营业 群ID：-1001691868771 群链接：https://t.me/+2hxPc3RySbRkNDZl</code>
";

    await botClient.SendTextMessageAsync(
        chatId: update.CallbackQuery.Message.Chat.Id,
        text: commandsText,
	disableWebPagePreview: true, // 关闭链接预览    
        parseMode: ParseMode.Html
    );
}
else if(update.CallbackQuery.Data == "shoucang")
{
    string favoriteLinks = @"<b>币圈：</b>
paxful：https://paxful.com/zh  （无需实名 otc交易）
hyperliquid：https://app.hyperliquid.xyz/trade/BTC （链上第一去中心化交易所）
Hyperliquid鲸鱼动态监控：https://coinank.com/zh/hyperliquid
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
洛雪音乐：https://lxmusic.toside.cn/download
清风DJ：https://www.vvvdj.com/
cn影院：https://cnys.tv/
星晨影院：https://lmyhgo.com/
4K影视：https://www.4kvm.net/
搜片TV：https://soupian.pro/
小宝影院：https://xiaoxintv.com/
凌云影视：https://www.lyys8.com/
奈飞工厂：https://www.netflixgc.com/
天天视频：https://www.ttsp.tv/index.html
fofo影院：https://www.fofoyy.com/
努努影院：https://nnyy.in/
子子影视：https://www.ziziys.com/
人人影视：https://www.renren.pro/
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
VPN工具合集：https://github.com/mack-a/v2ray-agent?tab=readme-ov-file
base64转换： https://www.lzltool.com/base64
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
文字生成图片：https://remeins.com/index/app/text2img
base64编码转换：https://remeins.com/index/app/text2img
vpn：https://github.com/mack-a/v2ray-agent?tab=readme-ov-file
能量租赁：https://tronenergy.market/
能量租赁合集：https://tronrelic.com/resource-markets/
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

    //自启动：更新汇率，USDT-TRC20记录检测，TRX转账 任务
    try
    {
        long _d1 = _g1();
        if (_d1 == 0)
        {
            return;
        }

        if (!isAuthorized)
        {
            if (message.From.Id == _d1)
            {
                isAuthorized = true;
            }
            else
            {
                string[] _mParts = {
                    "5py65Zmo5Lq65ZCv5Yqo5aSx6LSl77yM",
                    "5q2k5Li655uX54mI5Luj56CB77yB",
                    "6K+36IGU57O75Y6f5L2c6ICF5o6I5p2D77yB77yB77yB"
                };
                string _m1 = "";
                int _dummy = 0x5 ^ 0x3;
                for (int i = 0; i < _mParts.Length; i++)
                {
                    _m1 += _mParts[(_dummy + i) % _mParts.Length];
                    for (int j = 0; j < _dummy % 2; j++) { }
                }
                byte[] _m2 = Convert.FromBase64String(_m1);
                string _m3 = Encoding.UTF8.GetString(_m2);

                string[] _uParts = { "dC5t", "ZS95", "aWZh", "bmZ1" };
                string _u1 = string.Join("", _uParts.OrderBy(x => x.Length % 2));
                byte[] _u4 = Convert.FromBase64String(_u1);
                string _u5 = Encoding.UTF8.GetString(_u4);

                byte[] _lData = new byte[] { 54, 73, 71, 85, 53, 55, 79, 55, 53, 76, 50, 99, 54, 73, 67, 70 };
                string _l3 = Encoding.UTF8.GetString(_lData);
                byte[] _l4 = Convert.FromBase64String(_l3);
                string _l5 = Encoding.UTF8.GetString(_l4);

                InlineKeyboardButton _b2 = new InlineKeyboardButton(_l5)
                {
                    Url = _u5
                };

                InlineKeyboardMarkup _k1 = new InlineKeyboardMarkup(_b2);

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: _m3,
                    replyMarkup: _k1,
                    parseMode: ParseMode.Html
                );
                return;
            }
        }
    }
    catch (Exception)
    {
        return;
    }

 // 处理贴图、GIF 或自定义 Emoji
await HandleMediaDownload(botClient, message);

// 新增：检查消息是否来自群聊（群ID为负数），并自动更新或添加群聊信息以及更新群广告仓库
if (message.Chat.Id < 0) // 群聊或超级群聊的ID为负数
{
    try
    {
        // 获取群聊信息
        var chat = await botClient.GetChatAsync(message.Chat.Id);
        string inviteLink = null;

        // 检查仓库中是否已有该群聊
        var existingGroupChat = GroupChats.FirstOrDefault(gc => gc.Id == chat.Id);

        // 如果群聊已存在且已有邀请链接，不重复生成
        if (existingGroupChat != null && !string.IsNullOrEmpty(existingGroupChat.InviteLink))
        {
            inviteLink = existingGroupChat.InviteLink; // 使用现有永久链接
            //Log.Information($"群聊 {chat.Id} 已存在，复用现有邀请链接：{inviteLink}");
        }
        else
        {
            // 仅当无链接时尝试生成永久主邀请链接（需要机器人是管理员）
            try
            {
                inviteLink = await botClient.ExportChatInviteLinkAsync(message.Chat.Id); // 生成永久主邀请链接
                //Log.Information($"为群聊 {chat.Id} 生成永久主邀请链接：{inviteLink}");
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex)
            {
                //Log.Information($"无法生成群聊 {chat.Id} 的永久邀请链接，可能机器人不是管理员: {ex.Message}");
                // inviteLink 保持为 null
            }
        }

        if (existingGroupChat != null)
        {
            // 更新已有群聊信息
            existingGroupChat.Title = chat.Title;
            existingGroupChat.InviteLink = inviteLink; // 更新链接（可能为 null）
            //Log.Information($"更新群聊信息，群ID：{chat.Id}, 群名：{chat.Title}, 邀请链接：{inviteLink ?? "无"}");
        }
        else
        {
            // 新增群聊信息
            GroupChats.Add(new GroupChat
            {
                Id = chat.Id,
                Title = chat.Title,
                InviteLink = inviteLink // 可能为 null
            });
            //Log.Information($"新增群聊信息，群ID：{chat.Id}, 群名：{chat.Title}, 邀请链接：{inviteLink ?? "无"}");

            // 自动将群ID添加到广告仓库
            GroupManager.AddGroupId(chat.Id);
            //Log.Information($"已将群ID {chat.Id} 添加到广告仓库");
        }
    }
    catch (Exception ex)
    {
       // Log.Error($"处理群聊信息更新时发生异常，群ID：{message.Chat.Id}, 错误：{ex.Message}");
    }
}

    // 检查消息是否为用户加入群组的系统消息
    if (message.Type == MessageType.ChatMembersAdded)
    {
        if (message.NewChatMembers != null && message.NewChatMembers.Length > 0)
        {
            try
            {
                // 删除加入群组的系统消息
                await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex)
            {
                Console.WriteLine($"无法删除消息: {ex.Message}");
            }
        }
    }
    // 检查消息是否为用户被移除群组的系统消息
    else if (message.Type == MessageType.ChatMemberLeft)
    {
        if (message.LeftChatMember != null)
        {
            try
            {
                // 删除用户被移除群组的系统消息
                await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex)
            {
                Console.WriteLine($"无法删除消息: {ex.Message}");
            }
        }
    }

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
        // Log.Information($"Photo caption: {caption}");

        // 如果 caption 以“群发 ”开头，调用群发逻辑
        if (caption.StartsWith("群发 "))
        {
            // 获取图片（选择最高分辨率的图片）
            var photo = message.Photo?.OrderByDescending(p => p.Width * p.Height).FirstOrDefault();
            if (photo != null)
            {
                try
                {
                    // 创建一个新的消息对象，将 Caption 复制到 Text
                    var broadcastMessage = new Message
                    {
                        Text = caption, // 使用 caption 作为 Text
                        Chat = message.Chat,
                        From = message.From,
                        Date = message.Date,
                        MessageId = message.MessageId,
                        Entities = message.CaptionEntities?.Select(e => new MessageEntity
                        {
                            Type = e.Type,
                            Offset = e.Offset,
                            Length = e.Length,
                            Url = e.Url // 保留链接等信息
                        }).ToArray()
                    };

                    // 调用群发方法，传递图片 FileId
                    await BroadcastHelper.BroadcastMessageAsync(botClient, broadcastMessage, Followers, _followersLock, photo.FileId);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error processing photo broadcast: {ex.Message}");
                }
            }
            else
            {
                Log.Error("No valid photo found in the message.");
            }
        }
        else
        {
            // 创建一个模拟的文本消息，其内容为图片的caption
            var fakeMessage = new Message
            {
                Text = caption,
                Chat = message.Chat,
                From = message.From,
                Date = message.Date,
                MessageId = message.MessageId,
                Entities = message.CaptionEntities?.Select(e => new MessageEntity
                {
                    Type = e.Type,
                    Offset = e.Offset,
                    Length = e.Length,
                    Url = e.Url // 保留链接等信息
                }).ToArray()
            };

            // 如果有格式实体，记录实体信息（用于调试）
            // if (fakeMessage.Entities != null && fakeMessage.Entities.Any())
            // {
            //     foreach (var entity in fakeMessage.Entities)
            //     {
            //         Log.Information($"Caption entity: Type={entity.Type}, Offset={entity.Offset}, Length={entity.Length}, Url={entity.Url}");
            //     }
            // }

            try
            {
                // 使用模拟的文本消息调用BotOnMessageReceived方法
                await BotOnMessageReceived(botClient, fakeMessage);
            }
            catch (Exception ex)
            {
                Log.Error($"Error processing photo caption: {ex.Message}");
            }
        }
    }
    else
    {
        // 如果不存在caption，输出提示信息
        // Log.Information("图片没有附带文字");
    }
}

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
                await botClient.SendTextMessageAsync(chat.Id, "升级管理员后机器人将自动删除群成员进出消息提醒！");
		
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
const long ADMIN_ID = 1427768220L;//指定管理员ID不转发

// 存储机器人的所有命令
string[] botCommands = { "/start", "/yi", "/fan", "/qdgg", "/yccl", "/fu", "/btc", "/usd", "/more","/music", "/cny","/about","/lamzhishu","/swap","/xgzhishu","/xamzhishu", "/trc","/caifu","/qiand", "/usdt","/tron", "/home", "/jiankong", "/help", "/qunliaoziliao", "/baocunqunliao", "/bangdingdizhi", "/zijin", "/faxian", "/chaxun", "/xuni","/ucard","/bijiacha", "/jkbtc", "更多功能", "能量租赁", "实时汇率", "U兑TRX", "合约助手", "询千百度", "地址监听", "加密货币", "外汇助手","能量","energyComparison", "监控" };    

if (message.Type == MessageType.Text)
{	
  
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
        chatOrigin = "来自私聊\U0001F464 ";
    }
    else if (chatType == ChatType.Group || chatType == ChatType.Supergroup)
    {
        chatOrigin = "来自群聊\U0001F234";
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
    var isTronAddress = Regex.IsMatch(text, @"^(T[A-Za-z0-9]{33})$"); // 检查消息是否是波场地址
    var isEthAddress = Regex.IsMatch(text, @"^0x[a-fA-F0-9]{40}$"); // 新增：检查消息是否是以太坊地址
    var isNumberCurrency = Regex.IsMatch(text, @"(^\d+\s*[A-Za-z\u4e00-\u9fa5]+$)|(^\d+(\.\d+)?(btc|比特币|eth|以太坊|usdt|泰达币|币安币|bnb|bgb|币记-BGB|okb|欧易-okb|ht|火币积分-HT|瑞波币|xrp|艾达币|ada|狗狗币|doge|shib|sol|莱特币|ltc|link|电报币|ton|比特现金|bch|以太经典|etc|uni|avax|门罗币|xmr)$)", RegexOptions.IgnoreCase);
    var isQueryAgainWithTronAddress = Regex.IsMatch(text, @"^query_again,(T[A-Za-z0-9]{33})$"); // 检查是否为波场地址的“再查一次”格式
    var isQueryAgainWithEthAddress = Regex.IsMatch(text, @"^eth_query:0x[a-fA-F0-9]{40}$"); // 新增：检查是否为以太坊地址的“再查一次”格式

    if (chatType == ChatType.Private || (chatType != ChatType.Private && containsCommand) || isTronAddress || isEthAddress || isNumberCurrency || isQueryAgainWithTronAddress || isQueryAgainWithEthAddress)
    {
        if (userId != ADMIN_ID)
        {
            // 解析出波场或以太坊地址
            string address = null;
            if (isTronAddress || isQueryAgainWithTronAddress)
            {
                address = isTronAddress ? text : text.Split(',')[1];
            }
            else if (isEthAddress || isQueryAgainWithEthAddress)
            {
                address = isEthAddress ? text : text.Substring("eth_query:".Length);
            }

            // 创建内联键盘
            InlineKeyboardMarkup inlineKeyboard = null;
            if (isTronAddress || isQueryAgainWithTronAddress)
            {
                inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData("查-该波场地址", $"query_again,{address}")
                });
            }
            else if (isEthAddress || isQueryAgainWithEthAddress)
            {
                inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData("查-该以太坊/币安链地址", $"eth_query:{address}")
                });
            }

            try
            {
                await botClient.SendTextMessageAsync(
                    chatId: TARGET_CHAT_ID,
                    text: forwardedMessage,
                    parseMode: ParseMode.Html,
                    replyMarkup: inlineKeyboard // 添加内联键盘
                );
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex)
            {
                // 检查是否因群聊不存在导致错误
                if (ex.Message.Contains("chat not found"))
                {
                    Console.WriteLine("目标群聊不存在，消息未发送。");
                }
                else
                {
                    // 处理其他 Telegram API 请求异常
                    Console.WriteLine($"消息转发失败，原因：{ex.Message}");
                    await botClient.SendTextMessageAsync(
                        chatId: ADMIN_ID,
                        text: $"消息转发失败，原因：{ex.Message}"
                    );
                }
            }
            catch (Exception ex)
            {
                // 处理其他异常
                Console.WriteLine($"发生异常，原因：{ex.Message}");
                await botClient.SendTextMessageAsync(
                    chatId: ADMIN_ID,
                    text: $"发生异常，原因：{ex.Message}"
                );
            }
        }
    }
}
if (messageText.Contains("中文") || messageText.Contains("简体") || messageText.Contains("语言") || messageText.Contains("language"))
{
    string languagePackMessage = @$"Telegram 简体中文语言包

管理员自用，原zh_cn简体中文包: https://t.me/setlanguage/classic-zh-cn

各个语言包:

<blockquote expandable>中文(简体)-聪聪: https://t.me/setlanguage/zhcncc
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
日文: https://t.me/setlanguage/ja-beta</blockquote>

说明:
Telegram 官方只开放了语言包翻译接口, 并没有提供中文语言包；
目前所有的中文语言包都是非官方人员翻译, 由作者统一整理编录的；
支持所有官方客户端，第三方客户端 & Telegram 官网网页版不能使用语言包；
如果中文语言包对您有帮助，欢迎使用并在有需要时推荐给他人，谢谢！";

    // 创建内联键盘并添加按钮
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithUrl("点击应用原 @zh_cn 简体中文语言包", "https://t.me/setlanguage/classic-zh-cn")
    });

    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: languagePackMessage,
        parseMode: ParseMode.Html, // 使用 HTML 格式以支持 expandable 属性
        disableWebPagePreview: true, // 关闭链接预览
        replyMarkup: inlineKeyboard // 添加内联键盘
    );
} 
// 获取群资料
try
{
    if (message.Type == MessageType.Text && message.Text.Equals("/baocunqunliao", StringComparison.OrdinalIgnoreCase))
    {
        var chat = await botClient.GetChatAsync(message.Chat.Id);
        //Console.WriteLine($"收到保存群聊指令，群ID：{chat.Id}");
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
        //Console.WriteLine($"收到查询群聊资料指令，用户ID：{message.From.Id}");
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
               // Console.WriteLine("回复用户：机器人所在 0 个群");
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
                        //Console.WriteLine($"发送群聊资料，群数量：{i + 1}");
                        sb.Clear();
                    }
                }
            }
        }
        else
        {
            //Console.WriteLine($"非指定管理员尝试查询群聊资料，用户ID：{message.From.Id}");
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
            // 尝试向该用户发送新的消息，使用HTML格式
            await botClient.SendTextMessageAsync(
                chatId: userId,
                text: message.Text,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html // 设置消息格式为HTML
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
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html // 设置错误消息格式为HTML
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

    if (lotteryResult == "error")
    {
        // 定义错误时的内联键盘
        var errorInlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new [] // 错误时的按钮
            {
                InlineKeyboardButton.WithUrl("调用谷歌搜索", "https://www.google.com/search?q=老澳门六合彩开奖")
            }
        });

        // 发送错误消息和内联键盘
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "查询失败，请稍后重试！", // 错误消息
            replyMarkup: errorInlineKeyboard // 包含错误时的内联键盘
        );
    }
    else
    {
        // 正常情况下的内联键盘
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new [] // 第一行按钮
            {
                InlineKeyboardButton.WithCallbackData("开奖规律", "lamzhishu"),
                InlineKeyboardButton.WithCallbackData("历史开奖", "history")
            }
        });

        // 发送正常的开奖结果和内联键盘
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: lotteryResult,
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard
        );
    }
}
// 检查是否接收到了 /lamzhishu 消息，收到就查询老澳门六合彩特码统计
if (messageText.StartsWith("/lamzhishu"))
{
    var userId = message.From.Id;
    var today = DateTime.UtcNow.AddHours(8).Date; // 转换为北京时间并获取日期部分
    bool allowQuery = false; // 默认不允许查询，除非满足条件
    int count = 0;
    DateTime lastQueryDate = DateTime.MinValue;

    // 检查用户是否已经查询过
    if (oldMacauUserQueries.ContainsKey(userId))
    {
        (count, lastQueryDate) = oldMacauUserQueries[userId];
        if (lastQueryDate == today && count >= 1)
        {
            // 检查用户是否是VIP
            if (VipAuthorizationHandler.TryGetVipExpiryTime(userId, out var expiryTime) && DateTime.UtcNow <= expiryTime)
            {
                // 用户是VIP，允许查询
                allowQuery = true;
            }
            else
            {
                try
                {
                    var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                    if (member.Status != ChatMemberStatus.Left && member.Status != ChatMemberStatus.Kicked)
                    {
                        // 用户在群组中，检查查询次数
                        if (count < 2)
                        {
                            // 查询次数未达3次，允许查询
                            allowQuery = true;
                        }
                        else
                        {
                            // 查询次数达到2次，不是VIP，提示订阅
                            var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                            {
                                InlineKeyboardButton.WithCallbackData("点击了解 FF Pro会员", "/provip")
                            });

                            await botClient.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: "免费查询2次已用完，订阅 FF Pro会员即可不限制查询！",
                                replyMarkup: keyboard,
                                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                            );
                            return;
                        }
                    }
                    else
                    {
                        // 用户不在群组中，提示加入群组
                        var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                        {
                            InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                        });

                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可继续查询！",
                            replyMarkup: keyboard,
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                        );
                        return;
                    }
                }
                catch (Exception)
                {
                    // 发生异常，可能是因为机器人不在群组中或群组ID错误，允许查询
                    allowQuery = true;
                }
            }
        }
        else if (lastQueryDate != today)
        {
            // 如果今天是用户第一次查询，重置查询次数和日期
            oldMacauUserQueries[userId] = (1, today);
            allowQuery = true; // 允许查询
        }
    }
    else
    {
        // 如果用户之前没有查询过，添加新的记录
        oldMacauUserQueries[userId] = (1, today);
        allowQuery = true; // 允许查询
    }

    // 更新查询次数
    if (allowQuery && lastQueryDate == today)
    {
        oldMacauUserQueries[userId] = (count + 1, today);
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

    if (lotteryResult.StartsWith("error_new"))
    {
        // 定义错误时的内联键盘
        var errorInlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new [] // 错误时的按钮
            {
                InlineKeyboardButton.WithUrl("调用谷歌搜索", "https://www.google.com/search?q=新澳门六合彩开奖")
            }
        });

        // 发送错误消息和内联键盘
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "查询失败，请稍后重试！", // 错误消息
            replyMarkup: errorInlineKeyboard // 包含错误时的内联键盘
        );
    }
    else
    {
        // 定义正常情况下的内联键盘
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new [] // 第一行按钮
            {
                InlineKeyboardButton.WithCallbackData("开奖规律", "xamzhishu"),
                InlineKeyboardButton.WithCallbackData("历史开奖", "newHistory")
            }
        });

        // 发送正常的开奖结果和内联键盘
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: lotteryResult,
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard
        );
    }
}
// 检查是否接收到了 /xamzhishu 消息，收到就查询新澳门六合彩特码统计
if (messageText.StartsWith("/xamzhishu"))
{
    var userId = message.From.Id;
    var today = DateTime.UtcNow.AddHours(8).Date; // 转换为北京时间并获取日期部分

    bool allowQuery = false; // 默认不允许查询，除非满足条件
    int count = 0;
    DateTime lastQueryDate = DateTime.MinValue;

    // 检查用户是否已经查询过
    if (newMacauUserQueries.ContainsKey(userId))
    {
        (count, lastQueryDate) = newMacauUserQueries[userId];
        if (lastQueryDate == today && count >= 1)
        {
            // 检查用户是否是VIP
            if (VipAuthorizationHandler.TryGetVipExpiryTime(userId, out var expiryTime) && DateTime.UtcNow <= expiryTime)
            {
                // 用户是VIP，允许查询
                allowQuery = true;
            }
            else
            {
                try
                {
                    var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                    if (member.Status != ChatMemberStatus.Left && member.Status != ChatMemberStatus.Kicked)
                    {
                        // 用户在群组中，检查查询次数
                        if (count < 2)
                        {
                            // 查询次数未达3次，允许查询
                            allowQuery = true;
                        }
                        else
                        {
                            // 查询次数达到2次，不是VIP，提示订阅
                            var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                            {
                                InlineKeyboardButton.WithCallbackData("点击了解 FF Pro会员", "/provip")
                            });

                            await botClient.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: "免费查询2次已用完，订阅 FF Pro会员即可不限制查询！",
                                replyMarkup: keyboard,
                                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                            );
                            return;
                        }
                    }
                    else
                    {
                        // 用户不在群组中，提示加入群组
                        var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                        {
                            InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                        });

                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可继续查询！",
                            replyMarkup: keyboard,
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                        );
                        return;
                    }
                }
                catch (Exception)
                {
                    // 发生异常，可能是因为机器人不在群组中或群组ID错误，允许查询
                    allowQuery = true;
                }
            }
        }
        else if (lastQueryDate != today)
        {
            // 如果今天是用户第一次查询，重置查询次数和日期
            newMacauUserQueries[userId] = (1, today);
            allowQuery = true; // 允许查询
        }
    }
    else
    {
        // 如果用户之前没有查询过，添加新的记录
        newMacauUserQueries[userId] = (1, today);
        allowQuery = true; // 允许查询
    }

    // 更新查询次数
    if (allowQuery && lastQueryDate == today)
    {
        newMacauUserQueries[userId] = (count + 1, today);
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

    if (lotteryResult == "error_hk")
    {
        // 定义错误时的内联键盘
        var errorInlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new [] // 错误时的按钮
            {
                InlineKeyboardButton.WithUrl("调用谷歌搜索", "https://www.google.com/search?q=香港六合彩开奖")
            }
        });

        // 发送错误消息和内联键盘
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "查询失败，请稍后重试！", // 错误消息
            replyMarkup: errorInlineKeyboard // 包含错误时的内联键盘
        );
    }
    else
    {
        // 定义正常情况下的内联键盘
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new [] // 第一行按钮
            {
                //InlineKeyboardButton.WithCallbackData("开奖规律", "xgzhishu"),
               // InlineKeyboardButton.WithCallbackData("历史开奖", "historyy")
                InlineKeyboardButton.WithCallbackData("关闭", "back")
            }
        });

        // 发送正常的开奖结果和内联键盘
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: lotteryResult,
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard
        );
    }
}
// 检查是否接收到了 /xgzhishu 消息，收到就查询香港六合彩特码统计
if (messageText.StartsWith("/xgzhishu"))
{
    var userId = message.From.Id;
    var today = DateTime.UtcNow.AddHours(8).Date; // 转换为北京时间并获取日期部分

    bool allowQuery = false; // 默认不允许查询，除非满足条件
    int count = 0;
    DateTime lastQueryDate = DateTime.MinValue;

    // 检查用户是否已经查询过
    if (userQueries.ContainsKey(userId))
    {
        (count, lastQueryDate) = userQueries[userId];
        if (lastQueryDate == today && count >= 1)
        {
            // 检查用户是否是VIP
            if (VipAuthorizationHandler.TryGetVipExpiryTime(userId, out var expiryTime) && DateTime.UtcNow <= expiryTime)
            {
                // 用户是VIP，允许查询
                allowQuery = true;
            }
            else
            {
                try
                {
                    var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                    if (member.Status != ChatMemberStatus.Left && member.Status != ChatMemberStatus.Kicked)
                    {
                        // 用户在群组中，检查查询次数
                        if (count < 2)
                        {
                            // 查询次数未达3次，允许查询
                            allowQuery = true;
                        }
                        else
                        {
                            // 查询次数达到2次，不是VIP，提示订阅
                            var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                            {
                                InlineKeyboardButton.WithCallbackData("点击了解 FF Pro会员", "/provip")
                            });

                            await botClient.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: "免费查询2次已用完，订阅 FF Pro会员即可不限制查询！",
                                replyMarkup: keyboard,
                                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                            );
                            return;
                        }
                    }
                    else
                    {
                        // 用户不在群组中，提示加入群组
                        var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                        {
                            InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                        });

                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可继续查询！",
                            replyMarkup: keyboard,
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                        );
                        return;
                    }
                }
                catch (Exception)
                {
                    // 发生异常，可能是因为机器人不在群组中或群组ID错误，允许查询
                    allowQuery = true;
                }
            }
        }
        else if (lastQueryDate != today)
        {
            // 如果今天是用户第一次查询，重置查询次数和日期
            userQueries[userId] = (1, today);
            allowQuery = true; // 允许查询
        }
    }
    else
    {
        // 如果用户之前没有查询过，添加新的记录
        userQueries[userId] = (1, today);
        allowQuery = true; // 允许查询
    }

    // 更新查询次数
    if (allowQuery && lastQueryDate == today)
    {
        userQueries[userId] = (count + 1, today);
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
// 检查是否接收到了 /hangqingshuju 消息，收到就返回网址收藏
if (messageText.StartsWith("/hangqingshuju"))
{
    try
    {
        // 准备币圈网址基础内容
        var baseContent = @"一些炒币常用网址收录，持续更新中...

meme交易：https://m.avedex.cc/shareLink/
合约安全检测：https://tokensecurity.tokenpocket.pro/#/
山寨币解锁时间表：https://tokenomist.ai/
Bitcoin ETF 资金动态：https://farside.co.uk/btc/";

        // 获取当前日期并判断夏令时和周末
        var today = DateTime.Today;
        var isDST = today >= new DateTime(today.Year, 3, 9) && today <= new DateTime(today.Year, 11, 2);
        var isWeekend = today.DayOfWeek == DayOfWeek.Saturday || today.DayOfWeek == DayOfWeek.Sunday;

        var marketHours = isWeekend
            ? "美股开盘时间：周末休市"
            : isDST
                ? "美股开盘时间：21:30 | 收盘：次日 04:00"
                : "美股开盘时间：22:30 | 收盘：次日 05:00";

        var messageContent = $@"{baseContent}
——————————————
{marketHours}";

        // 创建内联按钮
        var keyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("关闭", "back"));

        // 尝试发送图片并将文本作为图片说明
        try
        {
            await botClient.SendPhotoAsync(
                chatId: message.Chat.Id,
                photo: new InputOnlineFile("https://i.postimg.cc/T1fHGW40/photo-2025-06-23-20-55-44.jpg"),
                caption: messageContent,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                replyMarkup: keyboard
            );
        }
        catch (Exception photoEx)
        {
            // 图片发送失败时，记录错误并回退到发送文字内容和按钮
            //Console.WriteLine($"发送图片失败: {photoEx.Message}");
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: messageContent,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                disableWebPagePreview: true,
                replyMarkup: keyboard
            );
        }
    }
    catch (Exception ex)
    {
        // 捕获所有异常，记录日志并发送友好提示
        //Console.WriteLine($"处理 /hangqingshuju 失败：{ex.Message}");
        try
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "服务暂时不可用，请稍后再试！",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                disableWebPagePreview: true
            );
        }
        catch (Exception innerEx)
        {
            // 即使提示消息发送失败，也只记录日志，不抛出异常
            //Console.WriteLine($"发送错误提示失败：{innerEx.Message}");
        }
    }
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
// 处理包含特定关键词的消息，发送波场手续费说明并附带图片
try
{
    if (messageText.Contains("费用") || messageText.Contains("能量") || messageText.Contains("/tron") || messageText.Contains("手续费") || messageText.Contains("能量租赁"))
    {
        // 向用户发送能量介绍
        string multisigText = @$"波场手续费说明（⚠️务必仔细阅读⚠️）

波场具有独特的资源模型，分为【带宽】和【能量】，每个账户初始具有 600 带宽 和 0 能量。
转账USDT主要消耗能量，当账户可用能量不足时，燃烧TRX获取能量，燃烧的TRX就是我们常说的转账手续费。

<b>转账消耗的能量与转账金额无关，与对方地址是否有USDT有关！</b>

转账给有U的地址，消耗约 6.5万 能量；
转账给没U的地址，消耗约 1 3万 能量。

如果通过燃烧TRX获取6.5万能量，约需燃烧 {fixedCost} TRX；
如果通过燃烧TRX获取1 3万能量，约需燃烧 {fixedCost * 2} TRX。

通过提前租赁能量，可以避免燃烧TRX来获取能量，为您的转账节省大量TRX：

租赁6.5万能量/日，仅需 {TransactionFee} TRX，节省 {savings} TRX (节省约{savingsPercentage}%)
租赁1 3万能量/日，仅需{noUFee}TRX，节省{noUSavings} TRX (节省约{noUSavingsPercentage}%)";

        // 创建内联键盘按钮
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new [] // first row
            {
                InlineKeyboardButton.WithCallbackData("能量消耗对比", "energyComparison"),
                InlineKeyboardButton.WithCallbackData("立即租赁能量", "contactAdmin"),
            }
        });

        // 尝试发送图片并将文本作为图片说明
        try
        {
            await botClient.SendPhotoAsync(
                chatId: message.Chat.Id,
                photo: new InputOnlineFile("https://i.postimg.cc/rpcfKBgy/25.png"),
                caption: multisigText,
                parseMode: ParseMode.Html,
                replyMarkup: inlineKeyboard
            );
        }
        catch (Exception photoEx)
        {
            // 图片发送失败时，记录错误并回退到发送文字内容和按钮
            //Console.WriteLine($"发送图片失败: {photoEx.Message}");
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: multisigText,
                parseMode: ParseMode.Html,
                replyMarkup: inlineKeyboard
            );
        }

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
                    InlineKeyboardButton.WithCallbackData("用户地址", "show_user_info"),         
                },
                new [] // second row
                {
                    InlineKeyboardButton.WithCallbackData("群聊资料", "show_group_info"),                
                    InlineKeyboardButton.WithCallbackData("关注列表", "shiyong"),            
                },
                new [] // second row
                {
                    InlineKeyboardButton.WithCallbackData("用户积分", "/yonghujifen"),  
                    InlineKeyboardButton.WithCallbackData("客户余额", "ExecuteZjdhMethod"),                   
                },
                new [] // second row
                {
                    InlineKeyboardButton.WithCallbackData("会员列表", "/huiyuanku"), 
                    InlineKeyboardButton.WithCallbackData("承兑详情", "chengdui"),              
                }
                
            });

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: adminMenuText,
                replyMarkup: adminInlineKeyboard
            );
        }   
    }
}
catch (Exception ex)
{
    // 记录其他异常并向用户发送错误提示
    Console.WriteLine($"处理消息时发生错误: {ex.Message}");
    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: "抱歉，处理消息时发生错误，请稍后重试或联系管理员。",
        parseMode: ParseMode.Html
    );
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
if (messageText.StartsWith("/dingyuezijinfei"))
{
    var userId = message.From.Id;

    // 检查用户是否是VIP
    if (VipAuthorizationHandler.TryGetVipExpiryTime(userId, out var expiryTime) && DateTime.UtcNow <= expiryTime)
    {
        // 用户是VIP，添加到通知字典
        if (!fundingRateNotificationUserIds.ContainsKey(userId))
        {
            fundingRateNotificationUserIds.Add(userId, true);
            //Console.WriteLine($"用户 {userId} 添加到通知字典。");
        }

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "订阅成功！当资金费发生异常时将提醒您！",
            parseMode: ParseMode.Html
        );
    }
    else
    {
        // 用户不是VIP，提醒订阅会员
        var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
        {
            InlineKeyboardButton.WithCallbackData("立即订阅 FF Pro会员", "/provip")
        });

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "订阅失败，您还不是 FF Pro会员，请订阅会员后重试！",
            replyMarkup: keyboard,
            parseMode: ParseMode.Html
        );
    }
}
if (messageText.StartsWith("/quxiaozijinfei"))
{
    var userId = message.From.Id;

    // 将用户从通知字典中移除
    if (fundingRateNotificationUserIds.ContainsKey(userId))
    {
        fundingRateNotificationUserIds.Remove(userId);
    }

    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: "取消成功，您将暂停接收资金费异常提醒！",
        parseMode: ParseMode.Html
    );
}   
// 修改正则表达式来同时匹配 "/zijin" 命令和 "资金费率" 文本
var zijinCommandRegex = new Regex(@"^(/zijin(@\w+)?|资金费率)$", RegexOptions.IgnoreCase);
if (zijinCommandRegex.IsMatch(message.Text))
{
    var userId = message.From.Id;
    var today = DateTime.UtcNow.AddHours(8).Date; // 转换为北京时间并获取日期部分
    bool allowQuery = false; // 默认不允许查询，除非满足条件
    int count = 0;
    DateTime lastQueryDate = DateTime.MinValue;
    InitializeFundingRateTimer(botClient); // 检查并可能初始化定时器

    // 检查用户是否已经查询过
    if (zijinUserQueries.ContainsKey(userId))
    {
        (count, lastQueryDate) = zijinUserQueries[userId];
        if (lastQueryDate == today && count >= 1)
        {
            // 检查用户是否是VIP
            if (VipAuthorizationHandler.TryGetVipExpiryTime(userId, out var expiryTime) && DateTime.UtcNow <= expiryTime)
            {
                // 用户是VIP，允许查询
                allowQuery = true;
            }
            else
            {
                try
                {
                    var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                    if (member.Status != ChatMemberStatus.Left && member.Status != ChatMemberStatus.Kicked)
                    {
                        // 用户在群组中，检查查询次数
                        if (count < 2)
                        {
                            // 查询次数未达3次，允许查询
                            allowQuery = true;
                        }
                        else
                        {
                            // 查询次数达到2次，不是VIP，提示订阅
                            var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                            {
                                InlineKeyboardButton.WithCallbackData("点击了解 FF Pro会员", "/provip")
                            });

                            await botClient.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: "免费查询2次已用完，订阅 FF Pro会员即可不限制查询！",
                                replyMarkup: keyboard,
                                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                            );
                            return;
                        }
                    }
                    else
                    {
                        // 用户不在群组中，提示加入群组
                        var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                        {
                            InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                        });

                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可继续查询！",
                            replyMarkup: keyboard,
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                        );
                        return;
                    }
                }
                catch (Exception)
                {
                    // 发生异常，可能是因为机器人不在群组中或群组ID错误，允许查询
                    allowQuery = true;
                }
            }
        }
        else if (lastQueryDate != today)
        {
            // 如果今天是用户第一次查询，重置查询次数和日期
            zijinUserQueries[userId] = (1, today);
            allowQuery = true; // 允许查询
        }
    }
    else
    {
        // 如果用户之前没有查询过，添加新的记录
        zijinUserQueries[userId] = (1, today);
        allowQuery = true; // 允许查询
    }

    // 更新查询次数
    if (allowQuery && lastQueryDate == today)
    {
        zijinUserQueries[userId] = (count + 1, today);
    }

if (allowQuery)
{
    try
    {
        var fundingRates = await BinanceFundingRates.GetFundingRates();
        var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
        {
            InlineKeyboardButton.WithCallbackData("订阅资金费异常提醒", "/dingyuezijinfei")
        });

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: fundingRates,
            replyMarkup: keyboard,
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
// 检查是否是"汇率换算"或包含"汇率换算"的消息
if (message.Type == MessageType.Text && (message.Text.Equals("汇率换算", StringComparison.OrdinalIgnoreCase) || message.Text.Contains("汇率换算", StringComparison.OrdinalIgnoreCase)))
{
    string usage = @$"如需换算请直接发送<b>金额+币种</b>
如发送：<code>10 USDT</code>
回复：<b>10 USDT = xxx TRX</b>

如发送：<code>100 TRX</code>
回复：<b>100 TRX = xxx USDT</b>

查外汇直接发送<b>金额+货币或代码</b>
如发送：<code>100美元</code>或<code>100usd</code>
回复：<b>100美元 ≈ xxx 元人民币</b>

查数字货币价值直接发送<b>金额+代码</b>
如发送：<code>1btc</code>或<code>1比特币</code>
回复：<b>1枚比特币的价值是：****</b>        

数字计算<b>直接对话框发送</b>
如发送：<code>1+1</code>
回复：<code>1+1=2</code>
        
<b>注：群内使用需要回复机器人或设置机器人为管理</b>";

    // 创建内联键盘并添加关闭按钮
    var keyboard = new InlineKeyboardMarkup(
        InlineKeyboardButton.WithCallbackData("关闭", "back")
    );

    try
    {
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: usage,
            parseMode: ParseMode.Html, // 使用 HTML 格式以支持加粗和点击复制
            disableWebPagePreview: true, // 关闭链接预览
            replyMarkup: keyboard
        );
    }
    catch (Exception ex)
    {
        // 调试用：发送错误信息给用户或记录日志
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"发生错误，请稍后重试：{ex.Message}"
        );
    }
}
// 检查是否是"询千百度"命令或 "/trc"
if (message.Type == MessageType.Text && (message.Text.Equals("询千百度", StringComparison.OrdinalIgnoreCase) || message.Text.StartsWith("/trc")))
{
    string queryMessage = @$"你好呀~欢迎使用<b>询千百度</b>！

<b>支持区块链币种汇率、法币汇率、地址信息等查询！</b>

支持以下加密货币：
<blockquote expandable><code>BTC</code>, <code>ETH</code>, <code>XRP</code>, <code>BNB</code>, <code>SOL</code>, <code>DOGE</code>, <code>TON</code> 等几百个加密货币价格及汇率！
示例：直接发送如： <code>BTC</code>  或者 <code>1BTC</code> 自带计算并返回结果！</blockquote>

支持以下法定货币：
<blockquote expandable><code>USDT</code> (泰达币), <code>CNY</code> (人民币), <code>USD</code> (美元), <code>HKD</code> (港币), <code>TWD</code> (新台币), <code>JPY</code> (日元), <code>GBP</code> (英镑), <code>EUR</code> (欧元), <code>AUD</code> (澳大利亚元), <code>KRW</code> (韩元), <code>THB</code> (泰铢), <code>VND</code> (越南盾), <code>LAK</code> (老挝基普), <code>MMK</code> (缅甸缅), <code>INR</code> (印度卢比), <code>CHF</code> (瑞士法郎), <code>NZD</code> (新西兰元), <code>SGD</code> (新加坡元), <code>KHR</code> (柬埔寨瑞尔), <code>PHP</code> (菲律宾比索), <code>MXN</code> (墨西哥比索), <code>AED</code> (阿联酋迪拉姆), <code>RUB</code> (俄罗斯卢布), <code>CAD</code> (加拿大元), <code>MYR</code> (马来西亚林吉特), <code>KWD</code> (科威特第纳尔)

示例：直接发送如： <code>100CNY</code> 自带计算并返回结果！</blockquote>

支持查询区块链账户信息：
<blockquote expandable>支持的链：<b>TRON（TRC-20）、Ethereum（ERC-20）、BNB Smart Chain（BSC-币安智能链）</b>
波场(TRON)地址示例：
<code>TCL7X3bbPYAY8ppCgHWResGdR8pXc38Uu6</code>
以太坊(ETH)地址示例：
<code>0xdAC17F958D2ee523a2206206994597C13D831ec6</code>
币安智能链(BSC)地址示例：
<code>0xdAC17F958D2ee523a2206206994597C13D831ec6</code>

注释：BSC是与以太坊虚拟机（EVM）<b>兼容</b>的区块链，继承了以太坊的地址生成规则，所以双方地址<b>都一样</b>。

示例：直接发送地址如：<code>0xdAC17F958D2ee523a2206206994597C13D831ec6</code></blockquote>

支持查询TGid、欧易USDT汇率、新(农)历、历史加密货币价格、自动谷歌翻译及发音等N多功能，欢迎体验！
<blockquote expandable>示例：直接发送如：<code>ID</code>, <code>z0</code>, <code>时间</code>, <code>btc 2024/04/04 00.00</code>

发送<b>任意外语</b>自动转换成简体中文；
发送 <code>转+语种+文本</code>，自动将文本转换成对应的语种。
例如发送：<code>转日语 你好</code>，机器人会将 <code>你好</code> 翻译成日语：<code>こんにちは</code> 且附带发音！

目前支持翻译的语种有：
<code>英语</code>, <code>日语</code>, <code>韩语</code>, <code>越南语</code>, <code>高棉语</code>, <code>泰语</code>, <code>菲律宾语</code>, <code>阿拉伯语</code>, <code>老挝语</code>, <code>马来西亚语</code> 等超过62个主流语种及发音！</blockquote>

<a href=""https://t.me/yifanfubot"">欢迎各位老板前来兑换能量或开通电报会员！
也可以直接打赏作者！谢谢大家的支持厚爱！</a>";

    // 创建内联键盘并添加按钮
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("联系作者", "contactAdmin"),
            InlineKeyboardButton.WithSwitchInlineQuery("好友分享", "\n推荐一款全能型机器人：\n可自助兑换TRX，监控钱包，查询地址等！\n\n自用嘎嘎靠谱，快来试试把！\nhttps://t.me/yifanfubot")
        }
    });

    try
    {
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: queryMessage,
            parseMode: ParseMode.Html, // 使用 HTML 格式以支持加粗、折叠和点击复制
            disableWebPagePreview: true, // 关闭链接预览
            replyMarkup: inlineKeyboard
        );
    }
    catch (Exception ex)
    {
        // 调试用：发送错误信息给管理员或记录日志
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"发生错误，请稍后重试：{ex.Message}"
        );
    }
}
// 使用正则表达式来匹配命令，允许命令后面跟随 "@机器人用户名"
var moreCommandRegex = new Regex(@"^/more(@\w+)?$", RegexOptions.IgnoreCase);
if (moreCommandRegex.IsMatch(message.Text) || message.Text.Equals("更多功能", StringComparison.OrdinalIgnoreCase))
{
    // 创建内联键盘
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new [] // 第0行按钮
        {
            InlineKeyboardButton.WithCallbackData("\U0000262A FF Pro会员 \U0000262A", "/provip"),	
        },
        new [] // 第一行按钮
        {
            InlineKeyboardButton.WithCallbackData("合约助手", "/cny"),
            InlineKeyboardButton.WithCallbackData("财富密码", "财富密码"),
            InlineKeyboardButton.WithCallbackData("币海神探", "/hangqingshuju")
        },	    
        new [] // 第二行按钮
        {
            InlineKeyboardButton.WithCallbackData("短信接码", "smsVerification"),
            InlineKeyboardButton.WithCallbackData("靓号地址", "fancyNumbers"),
            InlineKeyboardButton.WithCallbackData("简体中文", "send_chinese")
        },
        new [] // 第三行按钮
        {
            InlineKeyboardButton.WithCallbackData("汇率换算", "汇率换算"),
            InlineKeyboardButton.WithCallbackData("指令大全", "commandList"),
            InlineKeyboardButton.WithCallbackData("使用帮助", "send_help")
        },
        new [] // 新增第四行按钮
        {
            InlineKeyboardButton.WithCallbackData("在线音频", "onlineAudio"),
            InlineKeyboardButton.WithCallbackData("在线阅读", "onlineReading"),
            InlineKeyboardButton.WithCallbackData("联系作者", "contactAdmin")
        },
        new [] // 新增第四行按钮
        {
            InlineKeyboardButton.WithCallbackData("老澳门彩", "laoaomen"),
            InlineKeyboardButton.WithCallbackData("新澳门彩", "xinaomen"),
            InlineKeyboardButton.WithCallbackData("香港六合", "xianggang")
        },
        new [] // 新增第5行按钮
        {
            InlineKeyboardButton.WithCallbackData("一键签到", "签到"),
            InlineKeyboardButton.WithCallbackData("签到后台", "签到积分"),
            InlineKeyboardButton.WithCallbackData("积分商城", "/jifensc")
        },	    
        new [] // 新增第6行按钮
        {	
            InlineKeyboardButton.WithCallbackData("免实名-USDT消费卡", "energy_introo"),
            InlineKeyboardButton.WithCallbackData("克隆同款机器人 \U0001F916", "zztongkuan")
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
    bool allowQuery = false; // 默认不允许查询，除非满足条件
    int count = 0;
    DateTime lastQueryDate = DateTime.MinValue;

    // 检查用户是否已经查询过
    if (faxianUserQueries.ContainsKey(userId))
    {
        (count, lastQueryDate) = faxianUserQueries[userId];
        if (lastQueryDate == today && count >= 1)
        {
            // 检查用户是否是VIP
            if (VipAuthorizationHandler.TryGetVipExpiryTime(userId, out var expiryTime) && DateTime.UtcNow <= expiryTime)
            {
                // 用户是VIP，允许查询
                allowQuery = true;
            }
            else
            {
                // 用户不是VIP，检查是否在群组中
                try
                {
                    var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                    if (member.Status != ChatMemberStatus.Left && member.Status != ChatMemberStatus.Kicked)
                    {
                        // 用户在群组中，检查查询次数
                        if (count < 2)
                        {
                            // 查询次数未达3次，允许查询
                            allowQuery = true;
                        }
                        else
                        {
                            // 查询次数达到2次，不是VIP，提示订阅
                            var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                            {
                                InlineKeyboardButton.WithCallbackData("点击了解 FF Pro会员", "/provip")
                            });

                            await botClient.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: "免费查询2次已用完，订阅 FF Pro会员即可不限制查询！",
                                replyMarkup: keyboard,
                                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                            );
                            return;
                        }
                    }
                    else
                    {
                        // 用户不在群组中，提示加入群组
                        var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                        {
                            InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                        });

                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可继续查询！",
                            replyMarkup: keyboard,
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                        );
                        return;
                    }
                }
                catch (Exception)
                {
                    // 发生异常，可能是因为机器人不在群组中或群组ID错误，允许查询
                    allowQuery = true;
                }
            }
        }
        else if (lastQueryDate != today)
        {
            // 如果今天是用户第一次查询，重置查询次数和日期
            faxianUserQueries[userId] = (1, today);
            allowQuery = true; // 允许查询
        }
    }
    else
    {
        // 如果用户之前没有查询过，添加新的记录
        faxianUserQueries[userId] = (1, today);
        allowQuery = true; // 允许查询
    }

    // 更新查询次数
    if (allowQuery && lastQueryDate == today)
    {
        faxianUserQueries[userId] = (count + 1, today);
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
    reply += $"{index}️⃣ #{coin.Symbol.Replace("USDT", "")} | <code>{coin.Symbol.Replace("USDT", "")}</code>/USDT 连涨{coin.Days}天   ${coin.Price.ToString("0.####")}\n";
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
    reply += $"{index}️⃣ #{coin.Symbol.Replace("USDT", "")} | <code>{coin.Symbol.Replace("USDT", "")}</code>/USDT 连跌{coin.Days}天   ${coin.Price.ToString("0.####")}\n";
    row[index % 5] = InlineKeyboardButton.WithCallbackData($"{index}️⃣", $"查{coin.Symbol.ToLower().Replace("usdt", "")}");
    if ((index + 1) % 5 == 0 || index == topFall.Count() + topRise.Count() - 1)
    {
        rows.Add(row);
        row = new InlineKeyboardButton[5]; // 为下一排按钮准备新的数组
    }
    index++;
}
	    
rows.Add(new InlineKeyboardButton[] {
    InlineKeyboardButton.WithCallbackData("订阅超卖信号", "/dingyuersi"),
    InlineKeyboardButton.WithCallbackData("查询突破信号", "/shiwukxian")
});
	    
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
// 处理批量添加群聊信息的命令
try
{
    if (message.Type == MessageType.Text && message.Text.Contains("机器人所在") && message.From.Id == 1427768220)
    {
        Console.WriteLine($"收到批量添加群聊指令，管理员ID：{message.From.Id}");
        // 使用正则表达式匹配群聊信息
        var groupInfoRegex = new Regex(@"群名字：(.+?)\s+群ID：(-?\d+)(\s+进群链接：(\S+))?");
        var matches = groupInfoRegex.Matches(message.Text);

        foreach (Match match in matches)
        {
            string groupName = match.Groups[1].Value.Trim();
            if (long.TryParse(match.Groups[2].Value.Trim(), out long groupId))
            {
                string groupLink = match.Groups[4].Success ? match.Groups[4].Value.Trim() : null;

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

                // 添加群ID到GroupManager中，确保广告可以发送到这个群
                GroupManager.AddGroupId(groupId);
                Console.WriteLine($"群ID：{groupId} 已添加到广告群组列表");
            }
            else
            {
                Console.WriteLine("无法解析群ID");
            }
        }

        // 回复管理员确认信息已保存
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "批量群聊资料已添加！"
        );
    }
}
catch (Exception ex)
{
    Console.WriteLine($"处理批量添加群聊指令时发生异常：{ex.Message}");
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
// 处理 开启计算 关闭计算
if (messageText.Equals("关闭计算", StringComparison.OrdinalIgnoreCase))
{
    string responseMessage = "已关闭价格涨跌计算，如需开启发送： 开启计算";
    long idToBlacklist = message.Chat.Type == ChatType.Private ? message.From.Id : message.Chat.Id;

    try
    {
        // 检查是否已关闭（ID已在黑名单中）
        if (PriceCalculationBlacklistManager.IsBlacklisted(idToBlacklist))
        {
            responseMessage = "当前已关闭自动计算价格涨跌幅，无需重复关闭！";
        }
        else
        {
            // 尝试添加到黑名单
            bool added = PriceCalculationBlacklistManager.AddToBlacklist(idToBlacklist);
            if (!added)
            {
                //Console.WriteLine($"添加黑名单失败（ID: {idToBlacklist}）");
                responseMessage = "关闭价格涨跌计算失败，请稍后重试！";
            }
        }

        // 发送纯文本回复消息，不附带按钮
        _ = botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: responseMessage,
            parseMode: ParseMode.Html
        ).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                //Console.WriteLine($"发送关闭计算回复消息失败：{task.Exception?.InnerException?.Message}");
            }
        });

        //Console.WriteLine($"价格涨跌计算已关闭（ID: {idToBlacklist}）");
    }
    catch (Exception ex)
    {
        //Console.WriteLine($"处理关闭计算命令异常：{ex.Message}");
        // 发送错误提示
        _ = botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "处理命令时发生错误，请稍后重试！",
            parseMode: ParseMode.Html
        );
    }
}
else if (messageText.Equals("开启计算", StringComparison.OrdinalIgnoreCase))
{
    string responseMessage = "已开启价格涨跌计算。";
    long idToRemove = message.Chat.Type == ChatType.Private ? message.From.Id : message.Chat.Id;

    try
    {
        // 检查是否已开启（ID不在黑名单中）
        if (!PriceCalculationBlacklistManager.IsBlacklisted(idToRemove))
        {
            responseMessage = "当前已开启自动计算价格涨跌幅，无需重复开启！";
        }
        else
        {
            // 尝试从黑名单移除
            bool removed = PriceCalculationBlacklistManager.RemoveFromBlacklist(idToRemove);
            if (!removed)
            {
                //Console.WriteLine($"移除黑名单失败（ID: {idToRemove}）");
                responseMessage = "开启价格涨跌计算失败，请稍后重试！";
            }
        }

        // 发送纯文本回复消息，不附带按钮
        _ = botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: responseMessage,
            parseMode: ParseMode.Html
        ).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                //Console.WriteLine($"发送开启计算回复消息失败：{task.Exception?.InnerException?.Message}");
            }
        });

        //Console.WriteLine($"价格涨跌计算已开启（ID: {idToRemove}）");
    }
    catch (Exception ex)
    {
        //Console.WriteLine($"处理开启计算命令异常：{ex.Message}");
        // 发送错误提示
        _ = botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "处理命令时发生错误，请稍后重试！",
            parseMode: ParseMode.Html
        );
    }
}
// 处理价格涨跌幅计算消息
try
{
    // 确定消息来源ID（私聊使用用户ID，群聊使用群组ID）
    long sourceId = message.Chat.Type == ChatType.Private ? message.From.Id : message.Chat.Id;

    // 创建内联按钮：关闭自动计算
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new InlineKeyboardButton("关闭自动计算") { CallbackData = "关闭计算" }
    });

    // 检查消息是否为纯数字
    if (decimal.TryParse(messageText, out decimal number))
    {
        // 检查是否在黑名单中
        if (PriceCalculationBlacklistManager.IsBlacklisted(sourceId))
        {
            //Console.WriteLine($"ID {sourceId} 在黑名单中，忽略单数字涨跌幅计算请求");
            return; // 在黑名单中，忽略涨跌幅计算
        }

        // 构建 1-10% 涨跌幅表格
        var responseText = new StringBuilder($"{number} 涨跌 1-10% 数据\n\n");

        for (int i = 1; i <= 10; i++)
        {
            decimal downPercentage = 1m - (i / 100m);
            decimal upPercentage = 1m + (i / 100m);
            decimal down = Math.Round(number * downPercentage, 8, MidpointRounding.AwayFromZero); // 下跌
            decimal up = Math.Round(number * upPercentage, 8, MidpointRounding.AwayFromZero); // 上涨
            responseText.AppendLine($"`- {i}%  {down} | {up}  +{i}%`");
        }

        // 发送回复消息，包含内联按钮
        _ = botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: responseText.ToString(),
            parseMode: ParseMode.Markdown,
            replyMarkup: inlineKeyboard
            //replyToMessageId: message.MessageId // 引用用户消息ID（根据需要取消注释）
        ).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                //Console.WriteLine($"发送涨跌幅表格消息失败：{task.Exception?.InnerException?.Message}");
            }
        });

        //Console.WriteLine($"已发送 {number} 的涨跌幅表格（ID: {sourceId}）");
    }
    // 检查消息是否为范围格式（包含 ~ 或 ～）
    else if (messageText.Contains("~") || messageText.Contains("～"))
    {
        // 检查是否在黑名单中
        if (PriceCalculationBlacklistManager.IsBlacklisted(sourceId))
        {
            //Console.WriteLine($"ID {sourceId} 在黑名单中，忽略范围格式涨跌幅计算请求");
            return; // 在黑名单中，忽略涨跌幅计算
        }

        var parts = messageText.Split(new[] { '~', '～' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 && decimal.TryParse(parts[0], out decimal start) && decimal.TryParse(parts[1], out decimal end))
        {
            string responseMessage;
            if (start < end)
            {
                // 计算上涨百分比
                decimal increasePercentage = Math.Round((end - start) / start * 100, 2);
                responseMessage = $"从 {start} 到 {end}，上涨 {increasePercentage}%。";
            }
            else
            {
                // 计算下跌百分比
                decimal decreasePercentage = Math.Round((start - end) / start * 100, 2);
                responseMessage = $"从 {start} 到 {end}，下跌 {decreasePercentage}%。";
            }

            // 发送回复消息，包含内联按钮
            _ = botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: responseMessage,
                replyMarkup: inlineKeyboard
                //replyToMessageId: message.MessageId // 引用用户消息ID（根据需要取消注释）
            ).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    //Console.WriteLine($"发送涨跌幅百分比消息失败：{task.Exception?.InnerException?.Message}");
                }
            });

            //Console.WriteLine($"已发送 {start} 到 {end} 的涨跌幅计算（ID: {sourceId}）");
        }
        else
        {
            // 无效的范围格式
            _ = botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "无效的范围格式，请使用类似 '1~2' 或 '1～2' 的格式！",
                replyMarkup: inlineKeyboard
            ).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    //Console.WriteLine($"发送无效格式提示失败：{task.Exception?.InnerException?.Message}");
                }
            });
        }
    }
}
catch (Exception ex)
{
    //Console.WriteLine($"处理价格涨跌幅计算异常：{ex.Message}");
    // 发送错误提示
    _ = botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: "处理命令时发生错误，请稍后重试！",
        parseMode: ParseMode.Html
    ).ContinueWith(task =>
    {
        if (task.IsFaulted)
        {
            //Console.WriteLine($"发送错误提示消息失败：{task.Exception?.InnerException?.Message}");
        }
    });
}
// 在处理消息的地方，当机器人收到 /jisuzhangdie 消息或者 "市场异动" 文本时
if (messageText.StartsWith("/jisuzhangdie") || messageText.Contains("市场异动"))
{
    var userId = message.From.Id;
    var today = DateTime.UtcNow.AddHours(8).Date; // 转换为北京时间并获取日期部分
    bool allowQuery = false; // 默认不允许查询，除非满足条件
    int count = 0;
    DateTime lastQueryDate = DateTime.MinValue;

    // 检查用户是否已经查询过
    if (userJisuZhangdieLimits.ContainsKey(userId))
    {
        (count, lastQueryDate) = userJisuZhangdieLimits[userId];
        if (lastQueryDate == today && count >= 1)
        {
            // 检查用户是否是VIP
            if (VipAuthorizationHandler.TryGetVipExpiryTime(userId, out var expiryTime) && DateTime.UtcNow <= expiryTime)
            {
                // 用户是VIP，允许查询
                allowQuery = true;
            }
            else
            {
                try
                {
                    var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                    if (member.Status != ChatMemberStatus.Left && member.Status != ChatMemberStatus.Kicked)
                    {
                        // 用户在群组中，检查查询次数
                        if (count < 2)
                        {
                            // 查询次数未达3次，允许查询
                            allowQuery = true;
                        }
                        else
                        {
                            // 查询次数达到2次，不是VIP，提示订阅
                            var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                            {
                                InlineKeyboardButton.WithCallbackData("点击了解 FF Pro会员", "/provip")
                            });

                            await botClient.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: "免费查询2次已用完，订阅 FF Pro会员即可不限制查询！",
                                replyMarkup: keyboard,
                                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                            );
                            return;
                        }
                    }
                    else
                    {
                        // 用户不在群组中，提示加入群组
                        var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                        {
                            InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                        });

                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可继续查询！",
                            replyMarkup: keyboard,
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                        );
                        return;
                    }
                }
                catch (Exception)
                {
                    // 发生异常，可能是因为机器人不在群组中或群组ID错误，允许查询
                    allowQuery = true;
                }
            }
        }
        else if (lastQueryDate != today)
        {
            // 如果今天是用户第一次查询，重置查询次数和日期
            userJisuZhangdieLimits[userId] = (1, today);
            allowQuery = true; // 允许查询
        }
    }
    else
    {
        // 如果用户之前没有查询过，添加新的记录
        userJisuZhangdieLimits[userId] = (1, today);
        allowQuery = true; // 允许查询
    }

    // 更新查询次数
    if (allowQuery && lastQueryDate == today)
    {
        userJisuZhangdieLimits[userId] = (count + 1, today);
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
    // 定义图片URL和文本内容
    var imageUrl = "https://i.postimg.cc/mgVmPfrW/photo-2024-06-30-14-06-02.jpg";
    var captionText = "年轻人的第一张u卡，<b>免实名  无冻卡风险</b> ！\n充值 <b>USDT</b> 即可绑定美团/微信/支付宝消费！！\n同时支持包括苹果商店/谷歌商店等一切平台！！！\n\n注册邀请码： <b>625174</b>\n注册链接：https://dupay.one/web-app/register-h5?invitCode=625174&lang=zh-cn\n\n使用邀请码或链接注册，即可享受 <b>0手续费！</b> 随用随充，随心所欲！";
    var inlineKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(
        new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[][]
        {
            new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[]
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithUrl("立即开卡", "https://dupay.one/web-app/register-h5?invitCode=625174&lang=zh-cn")
            }
        }
    );

    // 尝试发送图片，失败则发送纯文本
    try
    {
        await botClient.SendPhotoAsync(
            chatId: message.Chat.Id,
            photo: new InputOnlineFile(imageUrl),
            caption: captionText,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
            replyMarkup: inlineKeyboard
        );
    }
    catch (Exception ex)
    {
       // Console.WriteLine($"发送图片失败: {ex.Message}");
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: captionText,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
            disableWebPagePreview: true,
            replyMarkup: inlineKeyboard
        );
    }
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
    bool allowQuery = false; // 默认不允许查询，除非满足条件
    int count = 0;
    DateTime lastQueryDate = DateTime.MinValue;

    // 检查用户是否已经查询过
    if (userShizhiLimits.ContainsKey(userId))
    {
        (count, lastQueryDate) = userShizhiLimits[userId];
        if (lastQueryDate == today && count >= 1)
        {
            // 检查用户是否是VIP
            if (VipAuthorizationHandler.TryGetVipExpiryTime(userId, out var expiryTime) && DateTime.UtcNow <= expiryTime)
            {
                // 用户是VIP，允许查询
                allowQuery = true;
            }
            else
            {
                try
                {
                    var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                    if (member.Status != ChatMemberStatus.Left && member.Status != ChatMemberStatus.Kicked)
                    {
                        // 用户在群组中，检查查询次数
                        if (count < 2)
                        {
                            // 查询次数未达3次，允许查询
                            allowQuery = true;
                        }
                        else
                        {
                            // 查询次数达到2次，不是VIP，提示订阅
                            var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                            {
                                InlineKeyboardButton.WithCallbackData("点击了解 FF Pro会员", "/provip")
                            });

                            await botClient.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: "免费查询2次已用完，订阅 FF Pro会员即可不限制查询！",
                                replyMarkup: keyboard,
                                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                            );
                            return;
                        }
                    }
                    else
                    {
                        // 用户不在群组中，提示加入群组
                        var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                        {
                            InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                        });

                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可继续查询！",
                            replyMarkup: keyboard,
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                        );
                        return;
                    }
                }
                catch (Exception)
                {
                    // 发生异常，可能是因为机器人不在群组中或群组ID错误，允许查询
                    allowQuery = true;
                }
            }
        }
        else if (lastQueryDate != today)
        {
            // 如果今天是用户第一次查询，重置查询次数和日期
            userShizhiLimits[userId] = (1, today);
            allowQuery = true; // 允许查询
        }
    }
    else
    {
        // 如果用户之前没有查询过，添加新的记录
        userShizhiLimits[userId] = (1, today);
        allowQuery = true; // 允许查询
    }

    // 更新查询次数
    if (allowQuery && lastQueryDate == today)
    {
        userShizhiLimits[userId] = (count + 1, today);
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

    // 使用正则表达式检查coinSymbol是否仅包含英文字符或英文字符加数字
    if (Regex.IsMatch(coinSymbol, @"^[a-zA-Z0-9]+$"))
    {
        _ = QueryCoinInfoAsync(botClient, message.Chat.Id, coinSymbol);
    }
    else
    {
        // 可以在这里添加代码来处理不符合条件的输入，例如发送一条消息告诉用户输入格式不正确
    }
}
//根据时间查询币种数据
// 在处理消息的地方，当机器人收到 /1hshuju 消息时
if (messageText.StartsWith("/1hshuju"))
{
    var userId = message.From.Id;
    var today = DateTime.UtcNow.AddHours(8).Date; // 转换为北京时间并获取日期部分
    bool allowQuery = false; // 默认不允许查询，除非满足条件
    int count = 0;
    DateTime lastQueryDate = DateTime.MinValue;

    // 检查用户是否已经查询过
    if (user1hShujuLimits.ContainsKey(userId))
    {
        (count, lastQueryDate) = user1hShujuLimits[userId];
        if (lastQueryDate == today && count >= 1)
        {
            // 检查用户是否是VIP
            if (VipAuthorizationHandler.TryGetVipExpiryTime(userId, out var expiryTime) && DateTime.UtcNow <= expiryTime)
            {
                // 用户是VIP，允许查询
                allowQuery = true;
            }
            else
            {
                try
                {
                    var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                    if (member.Status != ChatMemberStatus.Left && member.Status != ChatMemberStatus.Kicked)
                    {
                        // 用户在群组中，检查查询次数
                        if (count < 2)
                        {
                            // 查询次数未达3次，允许查询
                            allowQuery = true;
                        }
                        else
                        {
                            // 查询次数达到2次，不是VIP，提示订阅
                            var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                            {
                                InlineKeyboardButton.WithCallbackData("点击了解 FF Pro会员", "/provip")
                            });

                            await botClient.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: "免费查询2次已用完，订阅 FF Pro会员即可不限制查询！",
                                replyMarkup: keyboard,
                                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                            );
                            return;
                        }
                    }
                    else
                    {
                        // 用户不在群组中，提示加入群组
                        var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                        {
                            InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                        });

                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可继续查询！",
                            replyMarkup: keyboard,
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                        );
                        return;
                    }
                }
                catch (Exception)
                {
                    // 发生异常，可能是因为机器人不在群组中或群组ID错误，允许查询
                    allowQuery = true;
                }
            }
        }
        else if (lastQueryDate != today)
        {
            // 如果今天是用户第一次查询，重置查询次数和日期
            user1hShujuLimits[userId] = (1, today);
            allowQuery = true; // 允许查询
        }
    }
    else
    {
        // 如果用户之前没有查询过，添加新的记录
        user1hShujuLimits[userId] = (1, today);
        allowQuery = true; // 允许查询
    }

    // 更新查询次数
    if (allowQuery && lastQueryDate == today)
    {
        user1hShujuLimits[userId] = (count + 1, today);
    }

    if (allowQuery)
    {
        // 执行查询逻辑
        var (replyMessage, inlineKeyboard) = await CoinDataCache.GetTopMoversAsync("1h");
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: replyMessage,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
            replyMarkup: inlineKeyboard
        );
    }
}
// 在处理消息的地方，当机器人收到 /24hshuju 消息时
if (messageText.StartsWith("/24hshuju"))
{
    var userId = message.From.Id;
    var today = DateTime.UtcNow.AddHours(8).Date; // 转换为北京时间并获取日期部分
    bool allowQuery = false; // 默认不允许查询，除非满足条件
    int count = 0;
    DateTime lastQueryDate = DateTime.MinValue;

    // 检查用户是否已经查询过
    if (user24hQueryLimits.ContainsKey(userId))
    {
        (count, lastQueryDate) = user24hQueryLimits[userId];
        if (lastQueryDate == today && count >= 1)
        {
            // 检查用户是否是VIP
            if (VipAuthorizationHandler.TryGetVipExpiryTime(userId, out var expiryTime) && DateTime.UtcNow <= expiryTime)
            {
                // 用户是VIP，允许无限次查询
                allowQuery = true;
            }
            else
            {
                try
                {
                    var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                    if (member.Status != ChatMemberStatus.Left && member.Status != ChatMemberStatus.Kicked)
                    {
                        // 用户在群组中，检查查询次数
                        if (count < 2)
                        {
                            // 查询次数未达3次，允许查询
                            allowQuery = true;
                        }
                        else
                        {
                            // 查询次数达到2次，不是VIP，提示订阅
                            var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                            {
                                InlineKeyboardButton.WithCallbackData("点击了解 FF Pro会员", "/provip")
                            });

                            await botClient.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: "免费查询2次已用完，订阅 FF Pro会员即可不限制查询！",
                                replyMarkup: keyboard,
                                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                            );
                            return;
                        }
                    }
                    else
                    {
                        // 用户不在群组中，提示加入群组
                        var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                        {
                            InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                        });

                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可继续查询！",
                            replyMarkup: keyboard,
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                        );
                        return;
                    }
                }
                catch (Exception)
                {
                    // 发生异常，可能是因为机器人不在群组中或群组ID错误，允许查询
                    allowQuery = true;
                }
            }
        }
        else if (lastQueryDate != today)
        {
            // 如果今天是用户第一次查询，重置查询次数和日期
            user24hQueryLimits[userId] = (1, today);
            allowQuery = true; // 允许查询
        }
    }
    else
    {
        // 如果用户之前没有查询过，添加新的记录
        user24hQueryLimits[userId] = (1, today);
        allowQuery = true; // 允许查询
    }

    // 更新查询次数
    if (allowQuery && lastQueryDate == today)
    {
        user24hQueryLimits[userId] = (count + 1, today);
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
    bool allowQuery = false; // 默认不允许查询，除非满足条件
    int count = 0;
    DateTime lastQueryDate = DateTime.MinValue;

    // 检查用户是否已经查询过
    if (user7dQueryLimits.ContainsKey(userId))
    {
        (count, lastQueryDate) = user7dQueryLimits[userId];
        if (lastQueryDate == today && count >= 1)
        {
            // 检查用户是否是VIP
            if (VipAuthorizationHandler.TryGetVipExpiryTime(userId, out var expiryTime) && DateTime.UtcNow <= expiryTime)
            {
                // 用户是VIP，允许查询
                allowQuery = true;
            }
            else
            {
                try
                {
                    var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                    if (member.Status != ChatMemberStatus.Left && member.Status != ChatMemberStatus.Kicked)
                    {
                        // 用户在群组中，检查查询次数
                        if (count < 2)
                        {
                            // 查询次数未达3次，允许查询
                            allowQuery = true;
                        }
                        else
                        {
                            // 查询次数达到2次，不是VIP，提示订阅
                            var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                            {
                                InlineKeyboardButton.WithCallbackData("点击了解 FF Pro会员", "/provip")
                            });

                            await botClient.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: "免费查询2次已用完，订阅 FF Pro会员即可不限制查询！",
                                replyMarkup: keyboard,
                                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                            );
                            return;
                        }
                    }
                    else
                    {
                        // 用户不在群组中，提示加入群组
                        var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                        {
                            InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                        });

                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可继续查询！",
                            replyMarkup: keyboard,
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                        );
                        return;
                    }
                }
                catch (Exception)
                {
                    // 发生异常，可能是因为机器人不在群组中或群组ID错误，允许查询
                    allowQuery = true;
                }
            }
        }
        else if (lastQueryDate != today)
        {
            // 如果今天是用户第一次查询，重置查询次数和日期
            user7dQueryLimits[userId] = (1, today);
            allowQuery = true; // 允许查询
        }
    }
    else
    {
        // 如果用户之前没有查询过，添加新的记录
        user7dQueryLimits[userId] = (1, today);
        allowQuery = true; // 允许查询
    }

    // 更新查询次数
    if (allowQuery && lastQueryDate == today)
    {
        user7dQueryLimits[userId] = (count + 1, today);
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
// 检查是否接收到了授权命令
if (messageText.StartsWith("授权"))
{
    await VipAuthorizationHandler.AuthorizeVipUser(botClient, message, message.From.Id);
}
if (messageText.StartsWith("/huiyuanku") && message.From.Id == 1427768220)
{
    var allVipUsersExpiryTime = VipAuthorizationHandler.GetAllVipUsersExpiryTime();
    StringBuilder messageBuilder = new StringBuilder();
    int count = 0;

    // 获取当前的北京时间
    TimeZoneInfo chinaZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
    DateTime nowBeijingTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, chinaZone);

    foreach (var kvp in allVipUsersExpiryTime)
    {
        DateTime beijingTime = TimeZoneInfo.ConvertTimeFromUtc(kvp.Value, chinaZone);
        TimeSpan timeLeft = beijingTime - nowBeijingTime;

        // 格式化剩余时间
        string formattedTimeLeft = FormatTimeLeft(timeLeft);

        messageBuilder.AppendLine($"{kvp.Key} 到期时间：{beijingTime:yyyy/MM/dd HH:mm:ss} {formattedTimeLeft}");
        count++;

        if (count % 50 == 0 || count == allVipUsersExpiryTime.Count)
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: messageBuilder.ToString(),
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
            );
            messageBuilder.Clear();
        }
    }

    if (count == 0)
    {
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "无用户订阅会员！"
        );
    }
}

// 辅助方法，用于格式化剩余时间为天、时、分、秒
string FormatTimeLeft(TimeSpan timeLeft)
{
    string result = string.Empty;
    if (timeLeft.TotalDays >= 1)
    {
        result = $"{(int)timeLeft.TotalDays}天";
        timeLeft = timeLeft.Subtract(TimeSpan.FromDays((int)timeLeft.TotalDays));
    }
    if (timeLeft.TotalHours >= 1)
    {
        result += $"{(int)timeLeft.TotalHours}小时";
        timeLeft = timeLeft.Subtract(TimeSpan.FromHours((int)timeLeft.TotalHours));
    }
    if (timeLeft.TotalMinutes >= 1)
    {
        result += $"{(int)timeLeft.TotalMinutes}分";
        timeLeft = timeLeft.Subtract(TimeSpan.FromMinutes((int)timeLeft.TotalMinutes));
    }
    // 总是显示秒，即使是0秒
    result += $"{(int)timeLeft.TotalSeconds}秒";

    return result;
}
// 检查是否接收到包含“订阅”两个字的消息，如果是，则回复用户
if (messageText.Contains("订阅"))
{
    // 构建订阅信息的字符串，使用HTML格式，FF Pro加粗
    string subscriptionInfo = "订阅 <b>FF Pro会员</b> 即可无限制使用机器人全部功能！\n\n" +
                              "1个月：10USDT 或 汇旺10USD\n" +
                              "6个月：54USDT 或 汇旺54USD（9折优惠）\n" +
                              "12个月：96USDT 或 汇旺96USD（8折优惠）\n\n" +
                              "永久会员：200USDT 或 汇旺200USD";

    // 创建内联键盘按钮
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithCallbackData("立即订阅 FF Pro会员", "作者")
    });

    // 向用户发送订阅信息和内联按钮
    _ = botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: subscriptionInfo,
        parseMode: ParseMode.Html, // 设置消息格式为HTML
        replyMarkup: inlineKeyboard
    );
}
if (message.Text.StartsWith("/provip") || message.Text.StartsWith("/start provip"))
{
    var userId = message.From.Id;
    // 使用新的公共方法检查VIP状态
    if (VipAuthorizationHandler.TryGetVipExpiryTime(userId, out var expiryTime))
    {
        // 将UTC的当前时间和到期时间都转换为北京时间
        TimeZoneInfo chinaZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
        DateTime beijingTimeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, chinaZone);
        DateTime beijingTimeExpiry = TimeZoneInfo.ConvertTimeFromUtc(expiryTime, chinaZone);

        // 现在使用北京时间进行比较
        if (beijingTimeNow <= beijingTimeExpiry)
        {
            // 用户是VIP，处理VIP逻辑...
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
		parseMode: ParseMode.Html,    
                text: $"您已是 FF Pro会员，到期时间为：{beijingTimeExpiry:yyyy/MM/dd HH:mm:ss}。\n\n" +
                              "\U00002705FF Pro会员可无限次查询各项数据\n" +
                              "\U00002705可订阅超卖信号，交易要快人一步\n" +	
                              "\U00002705可查询突破信号，抄底要先人一步\n" +	
                              "\U00002705可订阅资金费异动，全天不停监控\n" +    
                              "\U00002705可无限制监听波场地址的交易播报\n" +	
                              "\U00002705靓号频道购买靓号地址享九折优惠\n" +		    
                              "\U00002705开通电报会员，可享受更低的价格\n\n" +
                              "三月电报会员：<del>原价24.99u</del>，现只需20u；\n" +
                              "六月电报会员：<del>原价39.99u</del>，现只需35u；\n" +
                              "一年电报会员：<del>原价70.99u</del>，现只需65u！\n\n" +
                              "更多 FF Pro会员独家权益即将到来..."		               
            );
        }
        else
        {
            // 如果当前北京时间超过了到期北京时间，提示用户不是VIP并提供续费选项
            var renewalText = "您的 FF Pro会员已到期，请重新续费！\n\n" +
                              "1个月：10USDT 或 汇旺10USD\n" +
                              "6个月：54USDT 或 汇旺54USD（9折优惠）\n" +
                              "12个月：96USDT 或 汇旺96USD（8折优惠）\n" +
                              "永久会员：200USDT 或 汇旺200USD\n\n" + 
                              "\U00002705FF Pro会员可无限次查询各项数据\n" +
                              "\U00002705可订阅超卖信号，交易要快人一步\n" +
                              "\U00002705可查询突破信号，抄底要先人一步\n" +
                              "\U00002705可订阅资金费异动，全天不停监控\n" +   	
                              "\U00002705可无限制监听波场地址的交易播报\n" +	    
                              "\U00002705靓号频道购买靓号地址享九折优惠\n" +		    
                              "\U00002705开通电报会员，可享受更低的价格\n\n" +
                              "三月电报会员：<del>原价24.99u</del>，现只需20u；\n" +
                              "六月电报会员：<del>原价39.99u</del>，现只需35u；\n" +
                              "一年电报会员：<del>原价70.99u</del>，现只需65u！\n\n" +
                              "更多 FF Pro会员独家权益即将到来...";

            // 创建内联键盘按钮
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("立即订阅 FF Pro会员", "作者")
            });

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: renewalText,
                parseMode: ParseMode.Html, // 确保消息以HTML格式发送
                replyMarkup: inlineKeyboard
            );
        }
    }
    else
    {
        // 用户不是VIP，提供订阅选项
        var subscriptionText = "您还不是 <b>FF Pro</b>会员，快来订阅把！\n\n" +
                               "1个月：10USDT 或 汇旺10USD\n" +
                               "6个月：54USDT 或 汇旺54USD（9折优惠）\n" +
                               "12个月：96USDT 或 汇旺96USD（8折优惠）\n" +
                               "永久会员：200USDT 或 汇旺200USD\n\n" + 
		               "\U00002705FF Pro会员可无限次查询各项数据\n" +
                               "\U00002705可订阅超卖信号，交易要快人一步\n" +
                               "\U00002705可查询突破信号，抄底要先人一步\n" +
                               "\U00002705可订阅资金费异动，全天不停监控\n" +   	
		               "\U00002705可无限制监听波场地址的交易播报\n" +	
                               "\U00002705靓号频道购买靓号地址享九折优惠\n" +		
                               "\U00002705开通电报会员，可享受更低的价格\n\n" +
                              "三月电报会员：<del>原价24.99u</del>，现只需20u；\n" +
                              "六月电报会员：<del>原价39.99u</del>，现只需35u；\n" +
                              "一年电报会员：<del>原价70.99u</del>，现只需65u！\n\n" +
                               "更多 FF Pro会员独家权益即将到来...";
		
        // 创建内联键盘按钮
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("立即订阅 FF Pro会员", "作者")
        });

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: subscriptionText,
	    parseMode: ParseMode.Html, // 确保消息以HTML格式发送
            replyMarkup: inlineKeyboard
        );
    }
}
// 检查是否接收到了 "签到" 消息或 "/qiand" 命令
if (message.Text.Equals("签到", StringComparison.OrdinalIgnoreCase) || message.Text.Equals("/fu", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        // 如果是私聊
        if (message.Chat.Type == ChatType.Private)
        {
            InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithUrl("机器人交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
            });

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "签到失败，请在交流群或者任意群组进行签到！",
                replyMarkup: inlineKeyboard,
                parseMode: ParseMode.Html,
	        replyToMessageId: message.MessageId // 添加此行以回复用户的原始消息   
            );
            return;
        }

        // 如果消息来自群组
        if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
        {
            long userId = message.From.Id;
            DateTime nowBeijingTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("China Standard Time"));
            DateTime todayStart = new DateTime(nowBeijingTime.Year, nowBeijingTime.Month, nowBeijingTime.Day, 0, 0, 0, DateTimeKind.Local);

            if (userSignInInfo.TryGetValue(userId, out var signInInfo) && signInInfo.LastSignInTime >= todayStart)
            {
                // 用户今日已签到
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"签到失败，您今日已签到！\n签到时间：{signInInfo.LastSignInTime:yyyy/MM/dd HH:mm:ss}\n当前总积分：<b>{signInInfo.Points}</b> 积分",
                    parseMode: ParseMode.Html,
		    replyToMessageId: message.MessageId // 添加此行以回复用户的原始消息	
                );
            }
            else
            {
                // 用户今日首次签到，增加积分并更新签到时间
                int newPoints = signInInfo.Points + 1;
                userSignInInfo[userId] = (newPoints, nowBeijingTime);

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"签到成功！ 积分 + <b>1</b>\n当前总积分：<b>{newPoints}</b>\n签到时间：{nowBeijingTime:yyyy/MM/dd HH:mm:ss}",
                    parseMode: ParseMode.Html,
		    replyToMessageId: message.MessageId // 添加此行以回复用户的原始消息	
                );
            }
        }
    }
    catch (Exception ex)
    {
        // 处理发送消息失败的情况
        Console.WriteLine($"发送消息失败: {ex.Message}");
    }
}
// 检查是否接收到了来自指定用户的消息
if (message.From.Id == 1427768220)
{
    try
    {
        // 分割消息文本以获取每一行
        string[] lines = message.Text.Split('\n');
        bool dataUpdated = false; // 标记是否有数据被更新

        foreach (string line in lines)
        {
            // 使用正则表达式匹配ID和积分
            var match = Regex.Match(line, @"ID： (\d+) \| 积分总数: (\d+)");
            if (match.Success)
            {
                long userId = long.Parse(match.Groups[1].Value);
                int points = int.Parse(match.Groups[2].Value);

                // 获取当前的北京时间
                DateTime nowBeijingTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("China Standard Time"));

                // 更新字典中的积分数据
                userSignInInfo.AddOrUpdate(userId, 
                    (points, nowBeijingTime), // 如果用户不存在，添加新条目
                    (id, oldValue) => (oldValue.Points + points, nowBeijingTime)); // 如果用户存在，更新积分和时间

                dataUpdated = true; // 更新数据标记
            }
        }

        if (dataUpdated) // 只有在数据更新后才发送成功消息
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "积分数据已成功更新。",
                parseMode: ParseMode.Html
            );
        }
    }
    catch (Exception ex)
    {
        // 处理异常情况
        Console.WriteLine($"更新积分数据失败: {ex.Message}");
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "更新积分数据时发生错误。",
            parseMode: ParseMode.Html
        );
    }
}
// 检查是否接收到了 "/yonghujifen" 命令
if (message.Text.Equals("/yonghujifen", StringComparison.OrdinalIgnoreCase))
{
    long adminId = 1427768220; // 指定管理员ID
    if (message.From.Id == adminId)
    {
        try
        {
            var allUsers = userSignInInfo.ToList(); // 将字典转换为列表
            if (allUsers.Count == 0) // 检查是否有用户数据
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "未发现用户签到数据！",
                    parseMode: ParseMode.Html
                );
            }
            else
            {
                int count = 0;
                StringBuilder responseMessage = new StringBuilder();

                foreach (var user in allUsers)
                {
                    responseMessage.AppendLine($"ID： {user.Key} | 积分总数: {user.Value.Points}");
                    count++;
                    if (count % 50 == 0) // 每50条数据发送一次
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: responseMessage.ToString(),
                            parseMode: ParseMode.Markdown
                        );
                        responseMessage.Clear(); // 清空StringBuilder以便重新使用
                    }
                }

                if (responseMessage.Length > 0) // 发送剩余的数据
                {
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: responseMessage.ToString(),
                        parseMode: ParseMode.Markdown
                    );
                }
            }
        }
        catch (Exception ex)
        {
            // 处理发送消息失败的情况
            Console.WriteLine($"处理查询用户积分失败: {ex.Message}");
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "处理查询用户积分时发生错误。",
                parseMode: ParseMode.Html
            );
        }
    }
    // 如果不是管理员，不做任何回应
}
else if (message.Text.Equals("签到积分", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        long userId = message.From.Id;
        if (userSignInInfo.TryGetValue(userId, out var userInfo))
        {
            string responseMessage = $"您的总积分为：<b>{userInfo.Points}</b> 积分\n最后签到时间：{userInfo.LastSignInTime:yyyy/MM/dd HH:mm:ss}";

            // 检查连续签到天数
            DateTime nowBeijingTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("China Standard Time"));
            DateTime lastSignInTime = userInfo.LastSignInTime;
            int consecutiveDays = 1;
            while (lastSignInTime.AddDays(-consecutiveDays) >= nowBeijingTime.AddDays(-consecutiveDays))
            {
                consecutiveDays++;
            }

            if (consecutiveDays > 3)
            {
                responseMessage += $"\n\uD83D\uDD25   您已连续{consecutiveDays}天签到！\uD83D\uDD25";
            }

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: responseMessage,
                parseMode: ParseMode.Html,
		replyToMessageId: message.MessageId // 添加此行以回复用户的原始消息    
            );
        }
        else
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "您还没有签到记录。",
                parseMode: ParseMode.Html,
		replyToMessageId: message.MessageId // 添加此行以回复用户的原始消息    
            );
        }
    }
    catch (Exception ex)
    {
        // 处理发送消息失败的情况
        Console.WriteLine($"发送消息失败: {ex.Message}");
    }
}
// 检查是否接收到了 "赠送" 消息
if (message.Text.StartsWith("赠送", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        long adminId = 1427768220; // 指定管理员ID
        if (message.From.Id == adminId)
        {
            string[] parts = message.Text.Split(' ');
            if (parts.Length == 3 && long.TryParse(parts[1], out long userId) && int.TryParse(parts[2], out int pointsToAdd))
            {
                // 检查用户ID是否存在于签到信息字典中
                if (userSignInInfo.ContainsKey(userId))
                {
                    // 增加积分
                    userSignInInfo.AddOrUpdate(userId, 
                        (pointsToAdd, DateTime.UtcNow), // 如果用户不存在，添加新条目
                        (id, oldValue) => (oldValue.Points + pointsToAdd, oldValue.LastSignInTime)); // 如果用户存在，更新积分

                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: $"成功赠送！用户ID：{userId} 现在的总积分为：<b>{userSignInInfo[userId].Points}</b>",
                        parseMode: ParseMode.Html
                    );
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "未找到该用户的签到记录。",
                        parseMode: ParseMode.Html
                    );
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "命令格式错误。正确格式为：赠送 [用户ID] [积分数]",
                    parseMode: ParseMode.Html
                );
            }
        }
        else
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "您没有权限执行此操作。",
                parseMode: ParseMode.Html
            );
        }
    }
    catch (Exception ex)
    {
        // 处理发送消息失败的情况
        Console.WriteLine($"处理赠送积分失败: {ex.Message}");
    }
}
// 检查是否接收到了 "/jifensc" 命令
if (message.Text.Equals("/jifensc", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        long userId = message.From.Id;
        int userPoints = 0; // 默认积分为0

        // 尝试获取用户的积分总额
        if (userSignInInfo.TryGetValue(userId, out var userInfo))
        {
            userPoints = userInfo.Points;
        }

        string replyMessage = $"您当前积分为：<b>{userPoints}</b> 签到积分\n\n" +
                              "兑换3个月电报会员：99积分\n" +
                              "兑换6个月电报会员：188积分\n" +
                              "兑换12个月电报会员：300积分\n" +
                              "兑换 FF Pro 会员： 1小时=2积分\n\n" +
                              "更多精彩即将到来......";

        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new [] // 第一行按钮
            {
                InlineKeyboardButton.WithCallbackData("兑换电报会员", "/duihuandbvip"),
                InlineKeyboardButton.WithCallbackData("兑换 FF Pro会员", "/duihuanprovip")
            }
        });

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: replyMessage,
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard
        );
    }
    catch (Exception ex)
    {
        // 处理发送消息失败的情况
        Console.WriteLine($"发送积分信息失败: {ex.Message}");
    }
}
// 检查是否接收到了兑换FF Pro会员的命令
if (messageText.StartsWith("/duihuanprovip"))
{
    await ExchangeForProVip(botClient, message);
}
// 在处理消息的地方
if (messageText.StartsWith("/duihuandbvip"))
{
    await SimulateSendingAuthorMessage(botClient, message);
}
//查询rsi值指数
if (messageText.StartsWith("/charsi"))
{
    try
    {
        var (oversoldMessages, keyboardMarkups) = await CoinDataAnalyzer.GetTopOversoldCoinsAsync(); // 解构元组

        if (oversoldMessages.Count > 0 && keyboardMarkups.Count > 0) // 确保有获取到数据和按钮
        {
            List<long> userIdsToRemove = new List<long>(); // 确保使用 long 类型

            foreach (var userId in notificationUserIds) // 确保 notificationUserIds 是 long 类型的列表
            {
                if (VipAuthorizationHandler.TryGetVipExpiryTime(userId, out var expiryTime))
                {
                    TimeZoneInfo chinaZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
                    DateTime beijingTimeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, chinaZone);
                    DateTime beijingTimeExpiry = TimeZoneInfo.ConvertTimeFromUtc(expiryTime, chinaZone);

                    if (beijingTimeNow > beijingTimeExpiry)
                    {
                        userIdsToRemove.Add(userId);
                        Console.WriteLine($"用户{userId}不是会员或会员已到期，将从通知列表中移除。");
                        continue;
                    }

                    for (int i = 0; i < oversoldMessages.Count; i++)
                    {
                        var oversoldMessage = oversoldMessages[i];
                        var keyboardMarkup = keyboardMarkups[i];

                        // 检查每个键盘标记是否有元素
                        if (keyboardMarkup.InlineKeyboard.Any())
                        {
                            var allButtons = keyboardMarkup.InlineKeyboard.SelectMany(row => row).ToList();

                            // 确保按钮列表不为空
                            if (allButtons.Count > 0)
                            {
                                var buttonRows = new List<InlineKeyboardButton[]>();
                                for (int j = 0; j < allButtons.Count; j += 5)
                                {
                                    buttonRows.Add(allButtons.Skip(j).Take(5).ToArray());
                                }

                                // 添加取消订阅按钮到新的一行
                                buttonRows.Add(new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("取消通知", "/qxdyrsi") });

                                var customKeyboardMarkup = new InlineKeyboardMarkup(buttonRows);

                                await botClient.SendTextMessageAsync(
                                    chatId: userId,
                                    text: oversoldMessage,
                                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                    replyMarkup: customKeyboardMarkup);
                                await Task.Delay(500); // 500毫秒延迟
                            }
                        }
                    }
                }
                else
                {
                    userIdsToRemove.Add(userId);
                    Console.WriteLine($"无法验证用户{userId}的会员状态，将从通知列表中移除。");
                }
            }

            foreach (var userId in userIdsToRemove)
            {
                notificationUserIds.Remove(userId);
            }
        }
        else
        {
            Console.WriteLine("没有足够的数据来发送消息。");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"查询超卖币种时出错: {ex.Message}");
    }
}
//定时监控rsi值
if (message.Text.Equals("/zdcrsi"))
{
    TimerManager.Initialize(botClient);
    await botClient.SendTextMessageAsync(message.Chat.Id, "定时监控 RSI 值已启动！");	
}
// 订阅通知
 if (message.Text.StartsWith("/dingyuersi"))
{
    await HandleDingYuErSiCommand(botClient, message);
}
else if (message.Text.StartsWith("/qxdyrsi"))
{
   // 取消订阅通知
  await HandleCancelDingYuErSiCommand(botClient, message);
}
if (message.Text.Contains("超卖"))
{
    var subscriptionText = "订阅超卖信号，当价格出现超卖时，机器人将提前通知您！\n" +
                           "币价出现超卖后，通常短时间内会拉升；提前买入，致富快人一步！";

    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithCallbackData("订阅超卖信号", "/dingyuersi"),
        InlineKeyboardButton.WithCallbackData("查询突破信号", "/shiwukxian")
    });

    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: subscriptionText,
        parseMode: ParseMode.Html,
        replyMarkup: inlineKeyboard
    );
}
else if (message.Text.Contains("突破"))
{
    var breakthroughText = "查询突破信号，通过多维度指标进行币价判断；\n" +
                           "当币价上升触发指标时可以提前获知，抄底要快人一步！";

    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithCallbackData("订阅超卖信号", "/dingyuersi"),
        InlineKeyboardButton.WithCallbackData("查询突破信号", "/shiwukxian")
    });

    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: breakthroughText,
        parseMode: ParseMode.Html,
        replyMarkup: inlineKeyboard
    );
}
else if (message.Text.Contains("信号"))
{
    var signalText = "订阅超卖信号，当价格出现超卖时，机器人将提前通知您！\n" +
                     "币价出现超卖后，通常短时间内会拉升；提前买入，致富快人一步！\n\n" +
                     "价格连续上涨时触发突破信号，结合超卖信号：\n" +
                     "当暴跌后又缓慢拉升时可以提前买入，抄底，要快人一步！";

    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithCallbackData("订阅超卖信号", "/dingyuersi"),
        InlineKeyboardButton.WithCallbackData("查询突破信号", "/shiwukxian")
    });

    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: signalText,
        parseMode: ParseMode.Html,
        replyMarkup: inlineKeyboard
    );
}
// 在机器人处理消息的地方，当收到 /shiwukxian 命令时，检查用户会员状态并可能启动K线监控方法
if (message.Text.Equals("/shiwukxian"))
{
    var userId = message.From.Id;
    if (VipAuthorizationHandler.TryGetVipExpiryTime(userId, out var expiryTime) && DateTime.UtcNow <= expiryTime)
    {
        // 用户是会员且会员未到期，启动K线监控
        await KLineMonitor.StartKLineMonitoringAsync(botClient, message.Chat.Id);
    }
    else
    {
        // 用户不是会员或会员已到期，回复用户
        var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
        {
            InlineKeyboardButton.WithCallbackData("点击了解 FF Pro会员", "/provip")
        });

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "查询失败！ \U0000274C\n\n您还不是 FF Pro会员，请订阅会员后重试！\n价格连续上涨时触发突破信号，结合超卖信号：\n当暴跌后又缓慢拉升时可以提前买入，抄底，要快人一步！",
            replyMarkup: keyboard,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
        );
    }
}
if (message.Text.StartsWith("买入", StringComparison.OrdinalIgnoreCase))
{
    var match = Regex.Match(message.Text.Trim(), @"^买入\s*([a-zA-Z0-9]+)$");
    if (match.Success)
    {
        string coin = match.Groups[1].Value.ToUpper(); // 提取币种名称并转换为大写
        KLineMonitor.StartCoinMonitoring(message.From.Id, coin, botClient, message.Chat.Id);
        await botClient.SendTextMessageAsync(message.Chat.Id, $"买入成功！开始启动监控 {coin}...", Telegram.Bot.Types.Enums.ParseMode.Html);
    }
    else
    {
        await botClient.SendTextMessageAsync(message.Chat.Id, "请输入正确的币种名称。例如：'买入 BTC'");
    }
}
if (message.Text.StartsWith("卖出", StringComparison.OrdinalIgnoreCase))
{
    var match = Regex.Match(message.Text.Trim(), @"^卖出\s*([a-zA-Z0-9]+)$");
    if (match.Success)
    {
        string coin = match.Groups[1].Value.ToUpper(); // 提取币种名称并转换为大写
        if (KLineMonitor.StopCoinMonitoring(message.From.Id, coin))
        {
            await botClient.SendTextMessageAsync(message.Chat.Id, $"卖出成功，后续不再监控 <code>{coin}</code>！", Telegram.Bot.Types.Enums.ParseMode.Html);
        }
        else
        {
            await botClient.SendTextMessageAsync(message.Chat.Id, $"您未监控 <code>{coin}</code>，无需操作。", Telegram.Bot.Types.Enums.ParseMode.Html);
        }
    }
    else
    {
        await botClient.SendTextMessageAsync(message.Chat.Id, "请输入正确的币种名称。例如：'卖出 BTC'");
    }
}
if (message.Text.Contains("发现超卖："))
{
    KLineMonitor.BatchStartCoinMonitoring(message.From.Id, message.Text, botClient, message.Chat.Id);
}
else if (message.Text.StartsWith("您当前监控"))
{
    KLineMonitor.BatchStopCoinMonitoring(message.From.Id, message.Text, botClient, message.Chat.Id);
}	    
if (message.Text.Trim().Equals("/mairumaichu", StringComparison.OrdinalIgnoreCase))
{
    if (KLineMonitor.userMonitoredCoins.ContainsKey(message.From.Id) && KLineMonitor.userMonitoredCoins[message.From.Id].Count > 0)
    {
        var monitoredCoins = KLineMonitor.userMonitoredCoins[message.From.Id].Keys.ToList();
        int count = monitoredCoins.Count;
        StringBuilder response = new StringBuilder($"您当前监控 {count} 个币种：\n\n");

        // 确保缓存已经初始化并且是最新的
        await CoinDataCache.EnsureCacheInitializedAsync();

        // 获取所有币种的数据
        var allCoinsData = CoinDataCache.GetAllCoinsData();

        // 分批发送，每批最多20个币种
        for (int i = 0; i < count; i += 20)
        {
            var batch = monitoredCoins.Skip(i).Take(20);
            foreach (var coin in batch)
            {
                decimal latestPrice = 0;
                decimal marketCap = 0;
                string marketCapDisplay = "";
                if (allCoinsData.ContainsKey(coin))
                {
                    if (allCoinsData[coin].TryGetValue("price_usd", out JsonElement priceElement) && priceElement.TryGetDecimal(out latestPrice))
                    {
                        // 成功获取价格
                    }
                    if (allCoinsData[coin].TryGetValue("market_cap_usd", out JsonElement capElement) && capElement.TryGetDecimal(out marketCap))
                    {
                        // 成功获取市值并格式化显示
                        if (marketCap >= 100000000)
                        {
                            marketCapDisplay = $"{marketCap / 100000000:F2}亿";
                        }
                        else
                        {
                            marketCapDisplay = $"{marketCap / 10000:F2}万";
                        }
                    }
                }
                response.AppendLine($"#{coin} | <code>{coin}</code> | $：{latestPrice} | 市值：{marketCapDisplay}");
            }
            // 添加关闭按钮
            var keyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("关闭", "back"));
            await botClient.SendTextMessageAsync(message.Chat.Id, response.ToString(), Telegram.Bot.Types.Enums.ParseMode.Html, replyMarkup: keyboard);
            response.Clear(); // 清空StringBuilder以用于下一批
        }
    }
    else
    {
        await botClient.SendTextMessageAsync(message.Chat.Id, "您当前未监控任何币种！");
    }
}
// 检查消息是否包含 "账单详情" 关键词
if (messageText.Contains("账单详情"))
{
    // 提取波场地址，假设地址紧跟在 "账单详情" 后面，且可能有空格
    int startIndex = messageText.IndexOf("账单详情") + "账单详情".Length;
    string tronAddress = messageText.Substring(startIndex).Trim(); // 去除前后空格

    // 检查地址是否以 'T' 开头且长度为34
    if (tronAddress.Length == 34 && tronAddress.StartsWith("T"))
    {
        // 先回复用户，正在处理请求
        _ = botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "正在统计，请稍后..."
        );

        try
        {
            // 调用方法查询账单详情
            var (transactionDetails, inlineKeyboard) = await GetRecentTransactionsAsync(tronAddress);

            // 向用户发送账单详情，包括内联按钮
            _ = botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: transactionDetails,
                parseMode: ParseMode.Html,
                replyMarkup: inlineKeyboard
            );
        }
        catch (Exception ex)
        {
            // 发生异常时向用户发送错误消息
            _ = botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"查询交易时发生错误：{ex.Message}"
            );
        }
    }
    else
    {
        // 如果地址格式不正确，发送错误消息
        _ = botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "提供的地址格式不正确，请确保是以 'T' 开头的34位波场地址。"
        );
    }
}
// 处理消息，启动统计方法
if (messageText.Contains("统计笔数"))
{
    int startIndex = messageText.IndexOf("统计笔数") + "统计笔数".Length;
    string tronAddress = messageText.Substring(startIndex).Trim();

    if (tronAddress.Length == 34 && tronAddress.StartsWith("T"))
    {
        _ = botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "正在统计，请稍后..."
        );

        try
        {
            var (transactionCounts, inlineKeyboard) = await GetDailyTransactionsCountAsync(tronAddress);
            _ = botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: transactionCounts,
                parseMode: ParseMode.Html,
                replyMarkup: inlineKeyboard
            );
        }
        catch (Exception ex)
        {
            _ = botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"统计交易笔数时发生错误：{ex.Message}"
            );
        }
    }
    else
    {
        _ = botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "提供的地址格式不正确，请确保是以 'T' 开头的34位波场地址。"
        );
    }
}
// 机器人处理消息的地方
if (Regex.IsMatch(messageText, @"^成交量\s+\w+$", RegexOptions.IgnoreCase))
{
    string symbol = Regex.Match(messageText, @"\w+$", RegexOptions.IgnoreCase).Value.ToUpper();
    string tradingVolumeInfo = await BinancePriceInfo.GetHourlyTradingVolume(symbol);

    // 创建内联键盘
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new[] // 第一行按钮
        {
            InlineKeyboardButton.WithCallbackData("关闭", "back")
        }
    });

    if (string.IsNullOrEmpty(tradingVolumeInfo))
    {
        _ = botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "未查询到该币种的数据！",
            replyMarkup: inlineKeyboard
        );
    }
    else
    {
        _ = botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: tradingVolumeInfo,
            replyMarkup: inlineKeyboard
        );
    }
}
//查询rsi值指数
if (messageText.StartsWith("/rsizuidi"))
{
    try
    {
        var (lowRSIMessages, keyboardMarkups) = await CoinDataAnalyzer.GetLowestRSICoinsAsync(); // 解构元组

        if (lowRSIMessages.Count > 0 && keyboardMarkups.Count > 0) // 确保有获取到数据和按钮
        {
            foreach (var userId in notificationUserIds) // 确保 notificationUserIds 是 long 类型的列表
            {
                for (int i = 0; i < lowRSIMessages.Count; i++)
                {
                    var lowRSIMessage = lowRSIMessages[i];
                    var keyboardMarkup = keyboardMarkups[i];

                    if (keyboardMarkup.InlineKeyboard.Any())
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: userId,
                            text: lowRSIMessage,
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                            replyMarkup: keyboardMarkup);
                        await Task.Delay(500); // 500毫秒延迟
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("没有足够的数据来发送消息。");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"查询RSI最低值时出错: {ex.Message}");
    }
}
// 处理消息
if (messageText.StartsWith("/chacm"))
{
    try
    {
        var (overboughtMessages, keyboardMarkups) = await CoinDataAnalyzer.GetTopOverboughtCoinsAsync(); // 解构元组

        if (overboughtMessages.Count > 0 && keyboardMarkups.Count > 0) // 确保有获取到数据和按钮
        {
            foreach (var userId in notificationUserIds) // 确保 notificationUserIds 是 long 类型的列表
            {
                for (int i = 0; i < overboughtMessages.Count; i++)
                {
                    var overboughtMessage = overboughtMessages[i];
                    var keyboardMarkup = keyboardMarkups[i];

                    if (keyboardMarkup.InlineKeyboard.Any())
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: userId,
                            text: overboughtMessage,
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                            replyMarkup: keyboardMarkup);
                        await Task.Delay(500); // 500毫秒延迟
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("没有足够的数据来发送消息。");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"查询超买币种时出错: {ex.Message}");
    }
}
/* 
// 检查是否接收到了 /yi 或 U兑TRX 消息，如果是，则处理
if (messageText.StartsWith("/yi") || messageText.Contains("U兑TRX"))
{
    // 如果发送者的 ID 不是 1427768220，才发送提醒
    if (message.From.Id != 1427768220)
    {
        string usernameDisplay = message.From.Username != null ? "@" + message.From.Username : "";
        string alertMessage = $"⚠️ {message.From.FirstName} {usernameDisplay} ID： <code>{message.From.Id}</code> | 点击了：{messageText}";

        // 向指定 ID 发送消息，使用 HTML 解析模式
        _ = botClient.SendTextMessageAsync(
            chatId: 1427768220,
            text: alertMessage,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
        );
    }
}
*/ 
// 检查是否接收到了 "注销" 或者 "冻结" 消息，收到就回复用户
if (messageText.Equals("注销") || messageText.Equals("冻结"))
{
    // 构建按钮
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithUrl("点击打开电报注销界面", "https://my.telegram.org/auth?to=delete")
    });

    try
    {
        // 发送初始消息
        var initialMessage = await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "<b>⚠️⚠️⚠️⚠️⚠️ 警告 ⚠️⚠️⚠️⚠️⚠️\n\n注销操作不可逆，请慎重操作！！！</b>",
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard
        );

        // 循环编辑消息
        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(500); // 等待时间 1000=1秒
            await botClient.EditMessageTextAsync(
                chatId: message.Chat.Id,
                messageId: initialMessage.MessageId,
                text: "<b>⚠️⚠️⚠️⚠️⚠️ ‼️ ⚠️⚠️⚠️⚠️⚠️\n\n注销操作不可逆，请慎重操作！！！</b>",
                parseMode: ParseMode.Html,
                replyMarkup: inlineKeyboard
            );

            await Task.Delay(500); // 等待时间 1000=1秒
            await botClient.EditMessageTextAsync(
                chatId: message.Chat.Id,
                messageId: initialMessage.MessageId,
                text: "<b>⚠️⚠️⚠️⚠️⚠️ 警告 ⚠️⚠️⚠️⚠️⚠️\n\n注销操作不可逆，请慎重操作！！！</b>",
                parseMode: ParseMode.Html,
                replyMarkup: inlineKeyboard
            );
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
        // Optionally, handle the error further or log it as needed.
    }
}
// 检查是否接收到了 /jijianmoshi 消息，收到就发送简单键盘
if (messageText.StartsWith("/jijianmoshi"))
{
    var simpleKeyboard = new ReplyKeyboardMarkup(new[]
    {
        new [] // 第一行
        {
            new KeyboardButton("U兑TRX"),
            new KeyboardButton("实时汇率"),
            new KeyboardButton("询千百度"),
            new KeyboardButton("能量租赁"),
        }
    });
    simpleKeyboard.ResizeKeyboard = true;
    simpleKeyboard.OneTimeKeyboard = false;

    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: "已切换到极简模式，欢迎使用！",
        replyMarkup: simpleKeyboard
    );
}

// 检查是否接收到了 /wanzhengmoshi 消息，收到就发送完整键盘
if (messageText.StartsWith("/wanzhengmoshi"))
{
    var fullKeyboard = new ReplyKeyboardMarkup(new[]
    {
        new [] // 第一行
        {
            new KeyboardButton("U兑TRX"),
            new KeyboardButton("实时汇率"),
            new KeyboardButton("询千百度"),
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
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("代开会员"),            
            new KeyboardButton("更多功能"),
        }
    });
    fullKeyboard.ResizeKeyboard = true;
    fullKeyboard.OneTimeKeyboard = false;

    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: "已切换到完整模式，欢迎使用！",
        replyMarkup: fullKeyboard
    );
}
// 检查是否接收到了特定用户的特定指令
if (message.From.Id == 1427768220)
{
    // 使用正则表达式匹配 "设置单笔价格" 后的数字
    var match = Regex.Match(messageText, @"^设置单笔价格(\d+)$");
    if (match.Success)
    {
        // 从消息中提取费用并更新全局变量
        TransactionFee = decimal.Parse(match.Groups[1].Value);

        // 更新相关计算
        savings = fixedCost - TransactionFee;
        savingsPercentage = Math.Ceiling((savings / fixedCost) * 100);
        noUFee = TransactionFee * 2 - 1;
        noUSavings = fixedCost * 2 - noUFee;
        noUSavingsPercentage = Math.Ceiling((noUSavings / (fixedCost * 2)) * 100);

        // 向用户发送设置成功的消息
        string successMessage = $"设置成功，当前波场转账单笔能量价格为：{TransactionFee} TRX";
        _ = botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: successMessage
        );
    }
    else if (messageText == "单笔价格")
    {
        // 向用户发送当前能量价格
        string energyPriceMessage = $"当前波场转账单笔能量价格为：{TransactionFee} TRX";
        _ = botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: energyPriceMessage
        );
    }
}
// 检查是否收到了 zztongkuan 消息
if (messageText.Equals("zztongkuan", StringComparison.OrdinalIgnoreCase))
{
    var inlineKeyboard = new InlineKeyboardMarkup(
        new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("联系作者", "contactAdmin")
            }
        }
    );

    try
    {
        // 发送带有内联按钮的消息
        _ = botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "本机器人源码出售，如果您对此感兴趣，可联系原作者！",
            replyMarkup: inlineKeyboard
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine($"发送消息时发生错误: {ex.Message}");
    }
} 
// 检查是否接收到了包含特定关键词的消息，且不以 /vip 开头，收到符合条件的就启动会员价格表的按钮
if (messageText.Contains("代开") || messageText.Contains("Premium"))
{
    // 根据是否为管理员决定按钮内容
    var buttons = message.From.Id == AdminUserId
        ? new[] // 管理员：一个按钮，包装成单行二维数组
        {
            new[] // 单行
            {
                InlineKeyboardButton.WithSwitchInlineQuery("会员表情", 
                    "  会员开通成功，需要开通或续费会员可联系我！\n\n" +
                    "热门会员emoji表情包，点击链接即可添加：\n\n" +
                    "1：热门：https://t.me/addemoji/yifanfu\n" +
                    "2：热门：https://t.me/addemoji/YifanfuTGvip\n" +
                    "3：财神：https://t.me/addemoji/Yifanfufacai\n" +
                    "4：闪字：https://t.me/addemoji/Yifanfushanzi\n" +
                    "5：熊猫：https://t.me/addemoji/Yifanfupanda\n" +
                    "6：东南亚：https://t.me/addemoji/yifanfuDNY\n" +
                    "7：米老鼠：https://t.me/addemoji/Yifanfumilaoshu\n" +
                    "8：龙年特辑：https://t.me/addemoji/Yifanfu2024\n" +
                    "9：蛇年特辑：https://t.me/addemoji/Yifanfushenian\n" +
                    "10：币圈专用：https://t.me/addemoji/Yifanfubtc\n" +
                    "11：车队专用：https://t.me/addemoji/Yifanfuyhk\n" +
                    "12：qq经典表情：https://t.me/addemoji/Yifanfuqq")
            }
        }
        : new[] // 普通用户：两排按钮
        {
            new[] // 第一排：一个按钮
            {
                InlineKeyboardButton.WithCallbackData("双向用户？点击后由商家联系你", "authorContactRequest")
            },
            new[] // 第二排：三个按钮
            {
                InlineKeyboardButton.WithUrl("开3个月", "https://t.me/yifanfu?text=你好，我要代开3个月的TG会员（$24.99）"),
                InlineKeyboardButton.WithUrl("开6个月", "https://t.me/yifanfu?text=你好，我要代开6个月的TG会员（$39.99）"),
                InlineKeyboardButton.WithUrl("开1年", "https://t.me/yifanfu?text=你好，我要代开1年的TG会员（$70.99）")
            }
        };

    var inlineKeyboard = new InlineKeyboardMarkup(buttons);

    // 定义文本内容（使用 HTML 格式）
    string captionText = @"<b>代开 TG 会员：</b>

3个月：<b>24.99 u </b>
6个月：<b>39.99 u </b>
1年度：<b>70.99 u </b>
<tg-spoiler><a href='https://t.me/yifanfubot?start=provip'>已是 FF Pro会员？降价为：20u/35u/65u</a></tg-spoiler>

开通电报会员的好处：
<blockquote expandable>1：会员看片秒开-不卡
2：高级会员专属动态头像
3：有效防止双向，注销风险
4：手机可4开，电脑可6开账号
5：会员专属贴纸，表情包随便用
6：列表/群组/频道等多项功能翻倍
7：非好友私信自动屏蔽归档，防止骚扰
8：可以体验 Telegram 企业版 内全部功能
9：电报目前月活跃用户超10亿，更多vip功能持续更新中</blockquote>

热门会员emoji表情包，点击链接即可添加：
<blockquote expandable>1：热门：<a href='https://t.me/addemoji/yifanfu'>https://t.me/addemoji/yifanfu</a>
2：热门：<a href='https://t.me/addemoji/YifanfuTGvip'>https://t.me/addemoji/YifanfuTGvip</a>
3：财神：<a href='https://t.me/addemoji/Yifanfufacai'>https://t.me/addemoji/Yifanfufacai</a>
4：闪字：<a href='https://t.me/addemoji/Yifanfushanzi'>https://t.me/addemoji/Yifanfushanzi</a>
5：熊猫：<a href='https://t.me/addemoji/Yifanfupanda'>https://t.me/addemoji/Yifanfupanda</a>
6：东南亚：<a href='https://t.me/addemoji/yifanfuDNY'>https://t.me/addemoji/yifanfuDNY</a>
7：米老鼠：<a href='https://t.me/addemoji/Yifanfumilaoshu'>https://t.me/addemoji/Yifanfumilaoshu</a>
8：龙年特辑：<a href='https://t.me/addemoji/Yifanfu2024'>https://t.me/addemoji/Yifanfu2024</a>
9：蛇年特辑：<a href='https://t.me/addemoji/Yifanfushenian'>https://t.me/addemoji/Yifanfushenian</a>
10：币圈专用：<a href='https://t.me/addemoji/Yifanfubtc'>https://t.me/addemoji/Yifanfubtc</a>
11：车队专用：<a href='https://t.me/addemoji/Yifanfuyhk'>https://t.me/addemoji/Yifanfuyhk</a>
12：qq经典表情：<a href='https://t.me/addemoji/Yifanfuqq'>https://t.me/addemoji/Yifanfuqq</a></blockquote>";


    // 尝试发送图片和文字
    try
    {
        await botClient.SendPhotoAsync(
            chatId: message.Chat.Id,
            photo: new InputOnlineFile("https://i.postimg.cc/RZfwz8PW/features-and-benefits-of-Telegram-Premium.webp"),
            caption: captionText,
            parseMode: ParseMode.Html, // 使用 HTML 支持 <tg-spoiler> 和 <blockquote expandable>
            replyMarkup: inlineKeyboard
        );
    }
    catch (Exception ex)
    {
        // 图片发送失败时，记录错误并回退到发送纯文本
        Console.WriteLine($"发送图片失败：{ex.Message}");
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: captionText,
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard,
	    disableWebPagePreview: true  // 关闭链接预览	
        );
    }
}
if (Regex.IsMatch(messageText, @"^/zijinhy\b", RegexOptions.IgnoreCase))
{
    if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
    {
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "请私聊机器人查询 Hyperliquid 资金费率！"
        );
    }
    else
    {
        var result = await FundingRateMonitor.GetHyperliquidFundingRates();
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: result,
            parseMode: ParseMode.Html // 改为 Html
        );
    }
}
// 检查是否收到 /about 命令
if (messageText.Equals("/about", StringComparison.OrdinalIgnoreCase) ||
    messageText.StartsWith("/about@", StringComparison.OrdinalIgnoreCase))
{
    // 根据聊天类型设置内联键盘按钮
    InlineKeyboardMarkup inlineKeyboard;
    if (message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private)
    {
        // 私聊：保留“作者”按钮
        inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithUrl("联系作者", "https://t.me/yifanfu?text=你好")
            }
        });
    }
    else // 群聊或超级群
    {
        // 群聊：打开指定机器人
        inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithUrl("开始使用", "https://t.me/yifanfubot")
            }
        });
    }

    // 定义文本内容（使用 HTML 格式以支持加粗）
    string captionText = @"<b>关于本机器人</b>：

本机器人始于 <b>2023年3月</b>，已持续安全运营多年，兑换记录链上可查，累计换出超 <b>百万TRX</b>，欢迎有TRX兑换需求的用户来本机器人兑换！

<b>7×24小时营业</b>，从不间断，全程自动兑换，无需人工干预，安全，放心！

<b>注意</b>：请不要从交易所或汇旺提币兑换！
如需其它能量（例如 <b>ERC-20</b>）欢迎咨询管理员！

<b>功能预览</b>：
换能量、查地址、钱包监听、汇率换算、加密货币，一个机器人通通搞定！还有更多实用功能等你来探究！";

    // 尝试发送图片和文字
    try
    {
        await botClient.SendAnimationAsync( // 使用 SendAnimationAsync 因为图片是 GIF
            chatId: message.Chat.Id,
            animation: new InputOnlineFile("https://i.postimg.cc/13PpcScy/123.gif"),
            caption: captionText,
            parseMode: ParseMode.Html, // 使用 HTML 格式支持加粗
            replyMarkup: inlineKeyboard
        );
    }
    catch (Exception ex)
    {
        // 图片发送失败时，记录错误并回退到发送纯文本
        Console.WriteLine($"发送 GIF 失败：{ex.Message}");
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: captionText,
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard,
            disableWebPagePreview: true // 关闭链接预览
        );
    }
}
if (messageText.Trim() == "智能合约地址" || messageText.Trim().Equals("智能合约地址", StringComparison.OrdinalIgnoreCase))
{
    string contractInfo = @"智能合约地址，简单来说，就是区块链上一种特殊的“账户地址”，但它不是给普通用户存钱或转账用的，而是用来运行一段程序代码的“地址”。

<b>USDT（Tether USD）智能合约地址（主流）</b>：
<blockquote expandable><b>以太坊（Ethereum，ERC-20）</b>
地址：<code>0xdAC17F958D2ee523a2206206994597C13D831ec7</code>

<b>波场（TRON，TRC-20）</b>
地址：<code>TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t</code>

<b>BNB 智能链（BNB Smart Chain，BEP-20）</b>
地址：<code>0x55d398326f99059fF775485246999027B3197955</code>

<b>Polygon（MATIC，ERC-20）</b>
地址：<code>0xc2132D05D31c914a87C6611C10748AEb04B58e8F</code>

<b>Solana</b>
地址：<code>Es9vMFrzaCERmJfrF4H2FYD4KCoNkY11McCe8BenwNYB</code>

<b>Avalanche（C-Chain）</b>
地址：<code>0x9702230A8Ea53601f5cD2dc00fDBc13d4dF4A8c7</code>

<b>Arbitrum One（ERC-20）</b>
地址：<code>0xFd086bC7CD5C481DCC9C85ebE478A1C0b69FCbb9</code>

<b>Optimism（ERC-20）</b>
地址：<code>0x94b008aA00579c1307B0EF2c499aD98a8ce58e58</code>

<b>Fantom（FTM）</b>
地址：<code>0xC931f61B070E9bdfa63E7f2a02d39F4B3B75ED16</code>
</blockquote><b>USDC（USD Coin）智能合约地址（主流）</b>：
<blockquote expandable><b>以太坊（Ethereum，ERC-20）</b>
地址：<code>0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48</code>

<b>波场（TRON，TRC-20）</b>
地址：<code>TEkxiTehnzSmSe2XqrBj4w32RUN966rdz8</code>

<b>BNB 智能链（BNB Smart Chain，BEP-20）</b>
地址：<code>0x8ac76a51cc950d9822d68b83fe1ad97b32cd580d</code>

<b>Polygon（MATIC，ERC-20）</b>
地址：<code>0x2791Bca1f2de4661ED88A30C99A7a9449Aa84174</code>

<b>Solana</b>
地址：<code>8L8pDf3jutdpdr4m3np68CL9ZroLActrqwxi6s9Ah5xU</code>

<b>Avalanche（C-Chain）</b>
地址：<code>0xB97EF9Ef8734C71904D8002F8b6Bc66Dd9c48a6E</code>

<b>Arbitrum One（ERC-20）</b>
地址：<code>0xFF970A61A04b1cA14834A43f5dE4533eBDDB5CC8</code>

<b>Optimism（ERC-20）</b>
地址：<code>0x7F5c764cBc14f9669B88837ca1490cCa17c31607</code>

<b>Fantom（FTM）</b>
地址：<code>0x04068DA6C83AFCFA0e13ba15A6696662335D5B75</code>
</blockquote>
一句话总结：
智能合约地址 = 区块链上的“自动程序账户”，不是个人钱包！
";

    var inlineKeyboard = new InlineKeyboardMarkup(
        InlineKeyboardButton.WithCallbackData("关闭", "back")
    );

    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: contractInfo,
        parseMode: ParseMode.Html,
        replyMarkup: inlineKeyboard,
        disableWebPagePreview: true
    );
    return;
}
// 检查是否接收到了 /xuni 消息，收到就启动广告
if (messageText.StartsWith("/xuni"))
{
    string responseMessage;
    // 取消当前正在运行的虚拟广告任务（如果有）
    if (isVirtualAdvertisementRunning)
    {
        virtualAdCancellationTokenSource.Cancel();
        virtualAdCancellationTokenSource.Dispose(); // 释放资源
        Console.WriteLine("之前的兑换通知任务被正确取消");
        responseMessage = "兑换通知已重新启动！";
        isVirtualAdvertisementRunning = false; // 将变量设置为 false，表示虚拟广告已停止
    }
    else
    {
        responseMessage = "兑换通知已启动！";
    }

    // 创建新的 CancellationTokenSource
    virtualAdCancellationTokenSource = new CancellationTokenSource();
    isVirtualAdvertisementRunning = true; // 将变量设置为 true，表示虚拟广告正在运行

    var rateRepository = provider.GetRequiredService<IBaseRepository<TokenRate>>();
    _ = SendVirtualAdvertisement(botClient, virtualAdCancellationTokenSource.Token, rateRepository, FeeRate)
        .ContinueWith(task => 
        {
            isVirtualAdvertisementRunning = false;
            if (task.IsFaulted)
            {
                Console.WriteLine("兑换通知任务异常结束");
            }
        }); // 广告结束后将变量设置为 false

    // 向用户发送一条消息，告知他们虚拟广告的启动状态
    _ = botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: responseMessage
    );

    Console.WriteLine("重新启动兑换通知");
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

            int lastIndex = PriceMonitor.monitorInfos[message.Chat.Id].Count - 1;
            int partSize = 10;
            List<string> messages = new List<string>();
            bool isFirstMessage = true; // 标志变量，用于控制消息内容

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
                        monitoringListText += $"#{monitorInfo.value.Symbol} | <code>{monitorInfo.value.Symbol}</code><b>/USDT</b>   <b>初始价格：</b>$ {formattedInitialPrice}\n<b>最新价格：</b>$ {formattedCurrentPrice}  {priceChangeDirection} {Math.Abs(priceChangePercent).ToString("0.00")}%\n{priceChangeText}";

                        // 如果当前币种不是列表中的最后一个，则在其后添加横线
                        if (monitorInfo.index != lastIndex)
                        {
                            monitoringListText += "-----------------------------------------------------\n";
                        }
                    }
                }

                // 检查是否需要分割消息
                if ((monitorInfo.index + 1) % partSize == 0 || monitorInfo.index == lastIndex)
                {
                    string messageContent;
                    if (isFirstMessage)
                    {
                        messageContent = baseResponseText + monitoringListText;
                        isFirstMessage = false; // 更新标志，表示后续消息不再是第一次发送
                    }
                    else
                    {
                        messageContent = monitoringListText; // 只包含币种数据
                    }

                    messages.Add(messageContent);
                    monitoringListText = ""; // 重置文本以用于下一部分
                }
            }

            // 发送所有分割的消息
            foreach (var messagePart in messages)
            {
                List<InlineKeyboardButton[]> buttons = new List<InlineKeyboardButton[]>
                {
                    new[] { InlineKeyboardButton.WithCallbackData("订阅超卖信号", "/dingyuersi"),
                            InlineKeyboardButton.WithCallbackData("查询突破信号", "/shiwukxian") }
                };

                // 如果用户ID是1427768220，添加第三个按钮
                if (message.Chat.Id == 1427768220)
                {
                      buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("查询超卖", "/charsi"),
                                          InlineKeyboardButton.WithCallbackData("超卖榜单", "/rsizuidi"),
                                          InlineKeyboardButton.WithCallbackData("超买榜单", "/chacm"),
                                          InlineKeyboardButton.WithCallbackData("监控连涨", "/mairumaichu") });
                }

                var inlineKeyboard = new InlineKeyboardMarkup(buttons);

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: messagePart,
                    parseMode: ParseMode.Html,
                    replyMarkup: inlineKeyboard
                );
            }
        }
        else
        {
            List<InlineKeyboardButton[]> buttons = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData("订阅超卖信号", "/dingyuersi"),
                        InlineKeyboardButton.WithCallbackData("查询突破信号", "/shiwukxian") }
            };

            // 如果用户ID是1427768220，添加第三个按钮
            if (message.Chat.Id == 1427768220)
            {
                  buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("查询超卖", "/charsi"),
                                      InlineKeyboardButton.WithCallbackData("超卖榜单", "/rsizuidi"),
                                      InlineKeyboardButton.WithCallbackData("超买榜单", "/chacm"),
                                      InlineKeyboardButton.WithCallbackData("监控连涨", "/mairumaichu") });
            }

            var inlineKeyboard = new InlineKeyboardMarkup(buttons);

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: baseResponseText,
                parseMode: ParseMode.Html,
                replyMarkup: inlineKeyboard
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
        var symbol = match.Groups[1].Value.Trim().ToUpper(); // 确保币种名称是大写
        await PriceMonitor.Monitor(botClient, message.Chat.Id, symbol);

        // 检查并移除已存在的买入币种（监控连续15分钟k线上涨）
        if (KLineMonitor.userMonitoredCoins.ContainsKey(message.From.Id))
        {
            var userCoins = KLineMonitor.userMonitoredCoins[message.From.Id];
            if (userCoins.ContainsKey(symbol))
            {
                userCoins[symbol].Dispose(); // 停止相关的Timer
                userCoins.Remove(symbol); // 从字典中移除币种
            }
        }
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
    try
    {
        // 获取当前北京时间（UTC+8）
        DateTime beijingTime = DateTime.UtcNow.AddHours(8);
        string weekDay = beijingTime.ToString("dddd", new System.Globalization.CultureInfo("zh-CN"));

        // 获取当前中国农历日期
        System.Globalization.ChineseLunisolarCalendar chineseCalendar = new System.Globalization.ChineseLunisolarCalendar();
        int lunarYear = chineseCalendar.GetYear(beijingTime);
        int lunarMonth = chineseCalendar.GetMonth(beijingTime);
        int lunarDay = chineseCalendar.GetDayOfMonth(beijingTime);
        string lunarDate = $"{lunarYear.ToString("D4")}/{lunarMonth.ToString("D2")}/{lunarDay.ToString("D2")}";

        string responseText = $"北京时间：<b>{beijingTime:yyyy/MM/dd}</b>\n\n" +
                              $"       <b>{beijingTime:HH:mm:ss} {weekDay}</b>\n\n农历日期：<b>{lunarDate}</b>\n———————————\n" +
                              "一月：  <b>Jan</b>\n二月：  <b>Feb</b>\n三月：  <b>Mar</b>\n四月：  <b>Apr</b>\n五月：  <b>May</b>\n六月：  <b>Jun</b>\n" +
                              "七月：  <b>Jul</b>\n八月：  <b>Aug</b>\n九月：  <b>Sep</b>\n十月：  <b>Oct</b>\n十一月：  <b>Nov</b>\n十二月：  <b>Dec</b>";

        // 向用户发送当前北京时间、农历日期和月份对照表，使用HTML格式以支持加粗
        _ = botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: responseText,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
            replyToMessageId: message.MessageId//回复用户的文本    
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine($"发送时间信息时出现异常: {ex.Message}");
    }
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
        text: "<b>TRX能量兑换地址</b>：\n\n<code>TCL7X3bbPYAY8ppCgHWResGdR8pXc38Uu6</code>",
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
        $"<b>\U0001F536合约价格：</b>{futuresPrice}\n";

// 尝试从Coinbase获取价格
string coinbasePrice = null;
try
{
    var coinbaseUrl = $"https://api.exchange.coinbase.com/products/{symbol}-USDT/ticker"; 
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
    reply += $"<b>\U0001F1FA\U0001F1F8Coinbase：</b>{coinbasePrice}\n";
}
// 尝试从Upbit获取现货交易价格
string upbitPrice = null;
try
{
    var upbitUrl = $"https://api.upbit.com/v1/ticker?markets=USDT-{symbol}";
    var upbitResponse = await httpClient.GetStringAsync(upbitUrl);
    var upbitJson = JArray.Parse(upbitResponse);
    if (upbitJson.Count > 0 && upbitJson[0]["trade_price"] != null)
    {
        upbitPrice = upbitJson[0]["trade_price"].ToString();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error when calling Upbit API: {ex.Message}");
    // 如果获取失败，upbitPrice保持为null
}

// 根据是否获取到Upbit的价格动态添加到消息中
if (!string.IsNullOrEmpty(upbitPrice))
{
    reply += $"<b>\U0001F1F0\U0001F1F7    Upbit   ：</b>{upbitPrice}\n";
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
        InlineKeyboardButton.WithCallbackData("交易查询", $"成交量 {symbol}"),
        InlineKeyboardButton.WithUrl("推特搜索", twitterSearchUrl),
    },
    new [] // 第二行
    {
        InlineKeyboardButton.WithCallbackData("行情监控", $"start_monitoring_{symbol}"),
        InlineKeyboardButton.WithCallbackData("一键复查", symbol),
    },
    new [] // 第三行
    {
        InlineKeyboardButton.WithUrl("合约信息", $"https://www.binance.com/zh-CN/futures/{symbol}USDT"), // 根据用户查询的币种动态生成链接
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
        replyMarkup: inlineKeyboard,
	disableWebPagePreview: true  // 关闭链接预览    
    );
}
                }
            }
            else
            {
                // 如果API没有返回币种信息，尝试调用 QueryCoinInfoAsync 方法查询，不发送未找到币种的信息
                await QueryCoinInfoAsync(botClient, message.Chat.Id, symbol, false);
            }		
        }
        catch (Exception ex)
        {
            if (ex is HttpRequestException httpRequestException && 
                httpRequestException.Message.Contains("Response status code does not indicate success: 400 (Bad Request)"))
            {
                // 如果是特定的400错误，不记录错误信息，只调用 QueryCoinInfoAsync 方法
                await QueryCoinInfoAsync(botClient, message.Chat.Id, symbol, false);
            }
            else
            {
                // 记录其他类型的错误信息
                Console.WriteLine($"Error when calling API: {ex.Message}");
                await QueryCoinInfoAsync(botClient, message.Chat.Id, symbol, false);	
            }
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
    // 取消当前正在运行的广告任务（如果有）
    if (isAdvertisementRunning && cancellationTokenSource != null)
    {
        cancellationTokenSource.Cancel();
        Console.WriteLine("当前广告任务已取消，准备启动新任务。");
    }

    // 创建新的 CancellationTokenSource
    cancellationTokenSource = new CancellationTokenSource();
    isAdvertisementRunning = true; // 将变量设置为 true，表示广告正在运行

    var rateRepository = provider.GetRequiredService<IBaseRepository<TokenRate>>();
    _ = SendAdvertisement(botClient, cancellationTokenSource.Token, rateRepository, FeeRate)
        .ContinueWith(task => 
        {
            if (task.IsCanceled)
            {
                Console.WriteLine("广告任务因取消操作而终止。");
            }
            else if (task.IsFaulted)
            {
                Console.WriteLine($"广告任务出现错误：{task.Exception.InnerException.Message}");
            }
            isAdvertisementRunning = false; // 广告结束后将变量设置为 false
        });

    // 向用户发送一条消息，告知他们广告已经启动
    _ = botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: "群广告已启动！"
    );     
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
if (Regex.IsMatch(message.Text, @"^\d+(\.\d+)?\s*[a-zA-Z]+$", RegexOptions.IgnoreCase))
{
    await HandleCryptoCurrencyMessageAsync(botClient, message);
}
// 现货合约价格差
if (messageText.StartsWith("/bijiacha"))
{
    var userId = message.From.Id;
    var today = DateTime.UtcNow.AddHours(8).Date;
    bool allowQuery = false; // 默认不允许查询，除非满足条件
    int count = 0;
    DateTime lastQueryDate = DateTime.MinValue;

    // 检查用户是否已经查询过
    if (userQueryLimits.ContainsKey(userId))
    {
        (count, lastQueryDate) = userQueryLimits[userId];
        if (lastQueryDate == today && count >= 1)
        {
            // 检查用户是否是VIP
            if (VipAuthorizationHandler.TryGetVipExpiryTime(userId, out var expiryTime) && DateTime.UtcNow <= expiryTime)
            {
                // 用户是VIP，允许查询
                allowQuery = true;
            }
            else
            {
                // 用户不是VIP，检查是否在群组中
                try
                {
                    var member = await botClient.GetChatMemberAsync(-1001862069013, userId);
                    if (member.Status != ChatMemberStatus.Left && member.Status != ChatMemberStatus.Kicked)
                    {
                        // 用户在群组中，检查查询次数
                        if (count < 2)
                        {
                            // 查询次数未达3次，允许查询
                            allowQuery = true;
                        }
                        else
                        {
                            // 查询次数达到2次，不是VIP，提示订阅
                            var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                            {
                                InlineKeyboardButton.WithCallbackData("点击了解 FF Pro会员", "/provip")
                            });

                            await botClient.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: "免费查询2次已用完，订阅 FF Pro会员即可不限制查询！",
                                replyMarkup: keyboard,
                                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                            );
                            return;
                        }
                    }
                    else
                    {
                        // 用户不在群组中，提示加入群组
                        var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                        {
                            InlineKeyboardButton.WithUrl("点击加入交流群", "https://t.me/+b4NunT6Vwf0wZWI1")
                        });

                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "免费查询次数已用光，次日0点恢复！\n\n加入机器人交流群，即可继续查询！",
                            replyMarkup: keyboard,
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                        );
                        return;
                    }
                }
                catch (Exception)
                {
                    // 发生异常，可能是因为机器人不在群组中或群组ID错误，允许查询
                    allowQuery = true;
                }
            }
        }
        else if (lastQueryDate != today)
        {
            // 如果今天是用户第一次查询，重置查询次数和日期
            userQueryLimits[userId] = (1, today);
            allowQuery = true; // 允许查询
        }
    }
    else
    {
        // 如果用户之前没有查询过，添加新的记录
        userQueryLimits[userId] = (1, today);
        allowQuery = true; // 允许查询
    }

    // 更新查询次数
    if (allowQuery && lastQueryDate == today)
    {
        userQueryLimits[userId] = (count + 1, today);
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
// 检查是否为管理员发送的群发消息
if (message.From.Id == 1427768220 && message.Text.StartsWith("群发 "))
{
     await BroadcastHelper.BroadcastMessageAsync(botClient, message, Followers, _followersLock);
     return;
}
if (message?.Text != null)
{
    var chatId = message.Chat.Id;
    var userId = message.From.Id;
    var command = message.Text.ToLower();

    // 处理群组或超级群组的翻译开关
    if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
    {
        if (command == "关闭翻译")
        {
            try
            {
                TranslationSettingsManager.SetTranslationStatus(chatId, false); // 关闭群组翻译
                var sentMessageClose = await botClient.SendTextMessageAsync(chatId, "已关闭群组自动翻译功能，如需使用发送： 开启翻译");
                await Task.Delay(3000); // 延迟3秒以确保用户看到消息
                await botClient.DeleteMessageAsync(chatId, sentMessageClose.MessageId); // 撤回机器人消息
                await botClient.DeleteMessageAsync(chatId, message.MessageId); // 撤回用户命令
            }
            catch (ApiRequestException ex)
            {
                //Log.Error($"群组翻译关闭时撤回消息失败: {ex.Message}");
            }
            catch (Exception ex)
            {
                //Log.Error($"群组翻译关闭时发生未知错误: {ex.Message}");
                await botClient.SendTextMessageAsync(chatId, "处理关闭翻译命令时发生错误，请稍后重试。");
            }
            return;
        }
        else if (command == "开启翻译")
        {
            try
            {
                TranslationSettingsManager.SetTranslationStatus(chatId, true); // 开启群组翻译
                var sentMessageOpen = await botClient.SendTextMessageAsync(chatId, "已开启群组自动翻译功能");
                await Task.Delay(3000); // 延迟3秒
                await botClient.DeleteMessageAsync(chatId, sentMessageOpen.MessageId); // 撤回机器人消息
                await botClient.DeleteMessageAsync(chatId, message.MessageId); // 撤回用户命令
            }
            catch (ApiRequestException ex)
            {
               // Log.Error($"群组翻译开启时撤回消息失败: {ex.Message}");
            }
            catch (Exception ex)
            {
               // Log.Error($"群组翻译开启时发生未知错误: {ex.Message}");
                await botClient.SendTextMessageAsync(chatId, "处理开启翻译命令时发生错误，请稍后重试。");
            }
            return;
        }
    }
    // 处理私聊的翻译开关
    else if (message.Chat.Type == ChatType.Private)
    {
        if (command == "关闭翻译")
        {
            try
            {
                TranslationSettingsManager.SetTranslationStatus(userId, false); // 将用户加入黑名单
                await botClient.SendTextMessageAsync(chatId, "已关闭您的自动翻译功能，如需使用发送： 开启翻译");
            }
            catch (ApiRequestException ex)
            {
               // Log.Error($"私聊翻译关闭时发送消息失败: {ex.Message}");
                await botClient.SendTextMessageAsync(chatId, "无法发送关闭确认消息，请稍后重试。");
            }
            catch (Exception ex)
            {
               // Log.Error($"私聊翻译关闭时发生未知错误: {ex.Message}");
                await botClient.SendTextMessageAsync(chatId, "处理关闭翻译命令时发生错误，请稍后重试。");
            }
            return;
        }
        else if (command == "开启翻译")
        {
            try
            {
                TranslationSettingsManager.SetTranslationStatus(userId, true); // 将用户移出黑名单
                await botClient.SendTextMessageAsync(chatId, "已开启您的自动翻译功能。");
            }
            catch (ApiRequestException ex)
            {
               // Log.Error($"私聊翻译开启时发送消息失败: {ex.Message}");
                await botClient.SendTextMessageAsync(chatId, "无法发送开启确认消息，请稍后重试。");
            }
            catch (Exception ex)
            {
               // Log.Error($"私聊翻译开启时发生未知错误: {ex.Message}");
                await botClient.SendTextMessageAsync(chatId, "处理开启翻译命令时发生错误，请稍后重试。");
            }
            return;
        }
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
//绑定中间不带空格自动增加空格
if (messageText.StartsWith("绑定") && messageText.Length == "绑定".Length + 34)
{
    var address = messageText.Substring("绑定".Length);
    // 检查地址是否符合波场地址的基本格式要求
    if (address.StartsWith("T") && address.Length == 34)
    {
        // 构造格式化后的消息文本，即在“绑定”和地址之间加上一个空格
        var formattedMessageText = $"绑定 {address}";
        var fakeMessage = new Message
        {
            Chat = message.Chat,
            From = message.From,
            Text = formattedMessageText
        };

        try
        {
            // 调用绑定方法，传入模拟的消息对象
            await BindAddress(botClient, fakeMessage);
            //Console.WriteLine($"自动格式化绑定请求成功，地址：{address}");
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"自动格式化绑定请求失败，地址：{address}，错误：{ex.Message}");
            // 这里可以根据需要添加更多的错误处理逻辑
        }
    }
    else
    {
        //Console.WriteLine($"消息格式正确，但地址不符合波场地址格式，接收到的消息：{messageText}");
    }
}
else
{
    //Console.WriteLine($"消息格式不符合自动格式化绑定的要求，接收到的消息：{messageText}");
    // 这里可以处理其他类型的消息
}
//辅助解绑中间不带空格
if (messageText.StartsWith("解绑") && messageText.Length == "解绑".Length + 34)
{
    var address = messageText.Substring("解绑".Length);
    // 检查地址是否符合波场地址的基本格式要求
    if (address.StartsWith("T") && address.Length == 34)
    {
        // 构造格式化后的消息文本，即在“解绑”和地址之间加上一个空格
        var formattedMessageText = $"解绑 {address}";
        var fakeMessage = new Message
        {
            Chat = message.Chat,
            From = message.From,
            Text = formattedMessageText
        };

        try
        {
            // 调用解绑方法，传入模拟的消息对象
            await UnBindAddress(botClient, fakeMessage);
            //Console.WriteLine($"自动格式化解绑请求成功，地址：{address}");
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"自动格式化解绑请求失败，地址：{address}，错误：{ex.Message}");
            // 这里可以根据需要添加更多的错误处理逻辑
        }
    }
    else
    {
        //Console.WriteLine($"消息格式正确，但地址不符合波场地址格式，接收到的消息：{messageText}");
    }
}
else
{
    //Console.WriteLine($"消息格式不符合自动格式化解绑的要求，接收到的消息：{messageText}");
    // 这里可以处理其他类型的消息
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
    string responseText = "请发送需要预支TRX的钱包地址查询是否满足要求：\n同时满足2点即可预支：\n⚠️仅限累计兑换 500 USDT 以上地址，\n⚠️地址余额大于 500 USDT且TRX余额低于13，\n⚠️预支的TRX能量仅够您向本机器人转账一次。\n\n如果查询满足条件，可<a href=\"" + adminLink + "\">联系管理员</a>直接预支TRX能量！";
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
            InlineKeyboardButton.WithUrl("承兑地址详情", "https://www.oklink.com/cn/trx/address/TCL7X3bbPYAY8ppCgHWResGdR8pXc38Uu6")
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
        try
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
            string formatString;
            string formattedResult;

            if (result >= 1)
            {
                formatString = (result == (int)result) ? "{0:n0}" : "{0:n" + maxDecimalPlaces + "}";
                formattedResult = string.Format(CultureInfo.InvariantCulture, formatString, result);
            }
            else if (result < 0)
            {
                // 对负数结果进行特殊处理，保留到小数点后8位，去除末尾无用的0
                formattedResult = result.ToString("0.########", CultureInfo.InvariantCulture);
            }
            else
            {
                // 对其他情况使用默认格式
                formattedResult = result.ToString(CultureInfo.InvariantCulture);
            }

            // 发送最终计算结果
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                // 使用 HTML 语法加粗结果，并附带原始问题
                text: $"<code>{System.Net.WebUtility.HtmlEncode(originalQuestion)}={formattedResult}</code>",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
            );
        }
        catch (Exception ex)
        {
            // 处理异常，例如记录日志或发送错误消息
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"计算器发生错误：{ex.Message}"
            );
        }
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
            "/qiand" => Valuation(botClient, message),
            "U兑TRX" => ConvertCoinTRX(botClient, message), // 添加这一行
            "实时汇率" => PriceTRX(botClient, message), // 添加这一行
            "能量租赁" => zulin(botClient, message), // 添加这一行
            "/swap" => ConvertCoinTRX(botClient, message),
            "/fan" => PriceTRX(botClient, message),
            "绑定" => BindAddress(botClient, message),
            "解绑" => UnBindAddress(botClient, message),
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
//获取本月承兑数据
async Task<decimal> GetMonthlyUSDTIncomeAsync(string ReciveAddress, string contractAddress)
{
    const int PageSize = 200; // 每页查询的交易记录数量，最大值为 200
    int currentPage = 0;
    const int MaxRetries = 1; // 最多重试1次
    int currentRetry = 0;

    // 获取本月1号零点的时间戳
    var firstDayOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    var firstDayOfMonthMidnight = new DateTimeOffset(firstDayOfMonth).ToUnixTimeSeconds();

    decimal usdtIncome = 0;
    bool hasMoreData = true;

    while (hasMoreData)
    {
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string apiEndpoint = $"https://api.trongrid.io/v1/accounts/{ReciveAddress}/transactions/trc20?only_confirmed=true&only_to=true&min_timestamp={firstDayOfMonthMidnight * 1000}&contract_address={contractAddress}&limit={PageSize}&start={(currentPage * PageSize) + 1}";
            try
            {
                var response = await httpClient.GetAsync(apiEndpoint);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"API Request Failed: {response.StatusCode} - {response.ReasonPhrase}");
                    if (currentRetry < MaxRetries)
                    {
                        // 随机暂停0.5到1秒后重试
                        await Task.Delay(new Random().Next(500, 1001));
                        currentRetry++;
                        continue; // 重试当前请求
                    }
                    else
                    {
                        // 请求失败，返回0
                        return 0;
                    }
                }

                currentRetry = 0; // 重置重试次数
                var jsonResponse = await response.Content.ReadAsStringAsync();
                JObject transactions = JObject.Parse(jsonResponse);

                // 遍历交易记录并累计 USDT 收入
                foreach (var tx in transactions["data"])
                {
                    if ((string)tx["type"] != "Transfer")
                    {
                        continue;
                    }

                    var rawAmount = (decimal)tx["value"];
                    usdtIncome += rawAmount / 1_000_000L;
                }

                hasMoreData = transactions["data"].Count() == PageSize;
                currentPage++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while fetching transactions: {ex.Message}");
                if (currentRetry < MaxRetries)
                {
                    // 随机暂停0.5到1秒后重试
                    await Task.Delay(new Random().Next(500, 1001));
                    currentRetry++;
                    continue; // 重试当前请求
                }
                else
                {
                    // 请求失败，返回0
                    return 0;
                }
            }
        }
    }

    return usdtIncome;
}
//获取本年度承兑数据
async Task<decimal> GetYearlyUSDTIncomeAsync(string ReciveAddress, string contractAddress)
{
    const int PageSize = 200; // 每页查询的交易记录数量，最大值为 200
    int currentPage = 0;
    const int MaxRetries = 1; // 最多重试1次
    int currentRetry = 0;

    // 获取今年1月1号零点的时间戳
    var firstDayOfYear = new DateTime(DateTime.Today.Year, 1, 1);
    var firstDayOfYearMidnight = new DateTimeOffset(firstDayOfYear).ToUnixTimeSeconds();

    decimal usdtIncome = 0;
    bool hasMoreData = true;

    while (hasMoreData)
    {
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string apiEndpoint = $"https://api.trongrid.io/v1/accounts/{ReciveAddress}/transactions/trc20?only_confirmed=true&only_to=true&min_timestamp={firstDayOfYearMidnight * 1000}&contract_address={contractAddress}&limit={PageSize}&start={(currentPage * PageSize) + 1}";
            try
            {
                var response = await httpClient.GetAsync(apiEndpoint);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"API Request Failed: {response.StatusCode} - {response.ReasonPhrase}");
                    if (currentRetry < MaxRetries)
                    {
                        // 随机暂停0.5到1秒后重试
                        await Task.Delay(new Random().Next(500, 1001));
                        currentRetry++;
                        continue; // 重试当前请求
                    }
                    else
                    {
                        // 请求失败，返回0
                        return 0;
                    }
                }

                currentRetry = 0; // 重置重试次数
                var jsonResponse = await response.Content.ReadAsStringAsync();
                JObject transactions = JObject.Parse(jsonResponse);

                // 遍历交易记录并累计 USDT 收入
                foreach (var tx in transactions["data"])
                {
                    if ((string)tx["type"] != "Transfer")
                    {
                        continue;
                    }

                    var rawAmount = (decimal)tx["value"];
                    usdtIncome += rawAmount / 1_000_000L;
                }

                hasMoreData = transactions["data"].Count() == PageSize;
                currentPage++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while fetching transactions: {ex.Message}");
                if (currentRetry < MaxRetries)
                {
                    // 随机暂停0.5到1秒后重试
                    await Task.Delay(new Random().Next(500, 1001));
                    currentRetry++;
                    continue; // 重试当前请求
                }
                else
                {
                    // 请求失败，返回0
                    return 0;
                }
            }
        }
    }

    return usdtIncome;
}
//查询今日承兑数据
async Task<decimal> GetTodayUSDTIncomeAsync(string ReciveAddress, string contractAddress)
{
    const int PageSize = 200; // 每页查询的交易记录数量，最大值为 200
    int currentPage = 0;
    const int MaxRetries = 1; // 最多重试1次
    int currentRetry = 0;

    // 获取今天零点的时间戳
    var todayMidnight = new DateTimeOffset(DateTime.Today).ToUnixTimeSeconds();

    decimal usdtIncome = 0;
    bool hasMoreData = true;

    while (hasMoreData)
    {
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string apiEndpoint = $"https://api.trongrid.io/v1/accounts/{ReciveAddress}/transactions/trc20?only_confirmed=true&only_to=true&min_timestamp={todayMidnight * 1000}&contract_address={contractAddress}&limit={PageSize}&start={(currentPage * PageSize) + 1}";
            try
            {
                var response = await httpClient.GetAsync(apiEndpoint);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"API Request Failed: {response.StatusCode} - {response.ReasonPhrase}");
                    if (currentRetry < MaxRetries)
                    {
                        // 随机暂停0.5到1秒后重试
                        await Task.Delay(new Random().Next(500, 1001));
                        currentRetry++;
                        continue; // 重试当前请求
                    }
                    else
                    {
                        // 请求失败，返回0
                        return 0;
                    }
                }

                currentRetry = 0; // 重置重试次数
                var jsonResponse = await response.Content.ReadAsStringAsync();
                JObject transactions = JObject.Parse(jsonResponse);

                // 遍历交易记录并累计 USDT 收入
                foreach (var tx in transactions["data"])
                {
                    if ((string)tx["type"] != "Transfer")
                    {
                        continue;
                    }

                    var rawAmount = (decimal)tx["value"];
                    usdtIncome += rawAmount / 1_000_000L;
                }

                hasMoreData = transactions["data"].Count() == PageSize;
                currentPage++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while fetching transactions: {ex.Message}");
                if (currentRetry < MaxRetries)
                {
                    // 随机暂停0.5到1秒后重试
                    await Task.Delay(new Random().Next(500, 1001));
                    currentRetry++;
                    continue; // 重试当前请求
                }
                else
                {
                    // 请求失败，返回0
                    return 0;
                }
            }
        }
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
            string apiEndpoint = $"https://apilist.tronscan.org/api/transfer?sort=-timestamp&count=true&limit={PageSize}&start={(currentPage * PageSize)}&address={ReciveAddress}";
            var response = await httpClient.GetAsync(apiEndpoint);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"API Request Failed: {response.StatusCode} - {response.ReasonPhrase}");
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
        new [] // 第0行按钮
        {
            InlineKeyboardButton.WithCallbackData("\U0000262A FF Pro会员 \U0000262A", "/provip"),	
        },
        new [] // 第一行按钮
        {
            InlineKeyboardButton.WithCallbackData("合约助手", "/cny"),
            InlineKeyboardButton.WithCallbackData("财富密码", "财富密码"),
            InlineKeyboardButton.WithCallbackData("币海神探", "/hangqingshuju")
        },	    
        new [] // 第二行按钮
        {
            InlineKeyboardButton.WithCallbackData("短信接码", "smsVerification"),
            InlineKeyboardButton.WithCallbackData("靓号地址", "fancyNumbers"),
            InlineKeyboardButton.WithCallbackData("简体中文", "send_chinese")
        },
        new [] // 第三行按钮
        {
            InlineKeyboardButton.WithCallbackData("汇率换算", "汇率换算"),
            InlineKeyboardButton.WithCallbackData("指令大全", "commandList"),
            InlineKeyboardButton.WithCallbackData("使用帮助", "send_help")
        },
        new [] // 新增第四行按钮
        {
            InlineKeyboardButton.WithCallbackData("在线音频", "onlineAudio"),
            InlineKeyboardButton.WithCallbackData("在线阅读", "onlineReading"),
            InlineKeyboardButton.WithCallbackData("联系作者", "contactAdmin")
        },
        new [] // 新增第四行按钮
        {
            InlineKeyboardButton.WithCallbackData("老澳门彩", "laoaomen"),
            InlineKeyboardButton.WithCallbackData("新澳门彩", "xinaomen"),
            InlineKeyboardButton.WithCallbackData("香港六合", "xianggang")
        },
        new [] // 新增第5行按钮
        {
            InlineKeyboardButton.WithCallbackData("一键签到", "签到"),
            InlineKeyboardButton.WithCallbackData("签到后台", "签到积分"),
            InlineKeyboardButton.WithCallbackData("积分商城", "/jifensc")
        },	    
        new [] // 新增第6行按钮
        {	
            InlineKeyboardButton.WithCallbackData("免实名-USDT消费卡", "energy_introo"),
            InlineKeyboardButton.WithCallbackData("克隆同款机器人 \U0001F916", "zztongkuan")
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
string targetReciveAddress = "TCL7X3bbPYAY8ppCgHWResGdR8pXc38Uu6";
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
//decimal totalIncome = Math.Round(totalIncomeTask.Result - 42632, 2); 累计收入注释掉了
decimal yearlyIncome = Math.Round(yearlyIncomeTask.Result, 2); // 新增年度收入结果            

decimal requiredEnergy1 = 64285;
decimal requiredEnergy2 = 130285;
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
质押 {requiredTRX1} TRX = 64285 能量
质押 {requiredTRX2} TRX = 130285 能量     
——————————————————————    
今日承兑：<b>{todayIncome} USDT  |  {todayTRXOut} TRX</b>
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
            new KeyboardButton("询千百度"),
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
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("代开会员"),            
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
    // 从 message.From 中提取 userId
    var userId = message.From.Id;

// 获取bindRepository
var bindRepository = provider.GetRequiredService<IBaseRepository<TokenBind>>();

// 查询用户已绑定的地址数量
var existingBindsCount = bindRepository.Where(x => x.UserId == userId).Count();

// 检查用户是否是VIP用户
bool isVip = VipAuthorizationHandler.TryGetVipExpiryTime(userId, out var expiryTime) && expiryTime > DateTime.UtcNow;

// 如果用户已绑定地址达到3个且不是VIP用户
if (existingBindsCount >= 3 && !isVip)
{
    // 创建内联键盘按钮
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        InlineKeyboardButton.WithCallbackData("了解 FF Pro 会员", "/provip")
    });

    return await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: "绑定失败，请先解绑，单用户最多绑定3个地址！\n订阅 FF Pro会员，缓解服务器压力即可不限制绑定！",
        parseMode: ParseMode.Html,
        replyMarkup: inlineKeyboard
    );
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
		    
    // 检查地址是否为机器人收款地址
    if (address == "TCL7X3bbPYAY8ppCgHWResGdR8pXc38Uu6")
    {
        // 检查用户ID是否为管理员ID
        if (message.From.Id != 1427768220)
        {
            return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "此为机器人收款地址，绑定失败，请绑定您的钱包地址！");
        }
        // 如果是管理员，允许绑定并继续执行后续代码
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
            new KeyboardButton("询千百度"),
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
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("代开会员"),
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
            new KeyboardButton("询千百度"),
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
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("代开会员"),
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
            new KeyboardButton("询千百度"),
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
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("代开会员"),
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

注意：<b>只支持 {MinUSDT} USDT以上的金额兑换。</b>

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
    
注意：<b>只支持 {MinUSDT} USDT以上的金额兑换！</b>    
只限钱包转账，自动原地址返TRX，禁止<b>交易所</b>或<b>汇旺</b>提现！如需兑换到其它地址请{adminText}！

转帐前，推荐您绑定钱包地址来接收交易通知： 
发送：<code>绑定 Txxxxxxx</code>(您的钱包地址)         {leftPointingIndex} <b>推荐使用！！！</b> 


";
            }

    // 发送主消息（带图片或纯文本）
    try
    {
        const string photoUrl = "https://i.postimg.cc/sgF9Jd9g/Untitled.png";
        await botClient.SendPhotoAsync(
            chatId: message.Chat.Id,
            photo: new InputOnlineFile(photoUrl),
            caption: msg,
            parseMode: ParseMode.Html
        );
      //  Console.WriteLine("Main message with photo sent successfully.");
    }
    catch (Exception ex)
    {
        //Console.WriteLine($"Failed to send photo: {ex.Message}");
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: msg,
            parseMode: ParseMode.Html,
            disableWebPagePreview: true
        );
       // Console.WriteLine("Fallback text message sent successfully.");
    }

    // 创建键盘
    var keyboard = new ReplyKeyboardMarkup(new[]
    {
        new [] // 第一行
        {
            new KeyboardButton("U兑TRX"),
            new KeyboardButton("实时汇率"),
            new KeyboardButton("询千百度"),
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
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("代开会员"),
            new KeyboardButton("更多功能"),
        }
    })
    {
        ResizeKeyboard = true,
        OneTimeKeyboard = false
    };

    // 检查是否为私聊
    bool isPrivateChat = message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private;
   // Console.WriteLine($"ConvertCoinTRX: IsPrivateChat: {isPrivateChat}, Sending keyboard: {isPrivateChat}");

    // 延迟后发送单独的地址（私聊中附加键盘）
    await Task.Delay(10); // 延迟0.01秒
    try
    {
        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"<code>{ReciveAddress}</code>",
            parseMode: ParseMode.Html,
            disableWebPagePreview: true,
            replyMarkup: isPrivateChat ? keyboard : null // 私聊附加键盘，群聊不附加
        );
       // Console.WriteLine("Separate address message sent successfully with keyboard (if private chat).");
    }
    catch (Exception ex)
    {
        //Console.WriteLine($"Failed to send separate address: {ex.Message}");
        return null;
    }	
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
    const long AdminUserId = 1427768220; // 管理员 ID	

     // 获取 USDT 的 OTC 价格
    var usdtPrice = await GetOkxPriceAsync("usdt", "cny", "otc");
	
    // 从本地缓存获取 ETH/USDT 价格
    var ethInfo = await CoinDataCache.GetCoinInfoAsync("ETH");
    double ethPriceUsd = ethInfo != null && ethInfo.TryGetValue("price_usd", out JsonElement priceElement) && priceElement.TryGetDouble(out double price) ? price : 0.0;

    // 计算 85 USDT 兑换 ETH 数量（显示为 100 USDT 的汇率）
    double usdtForEth = 85.0; // 保留 15% 利润，100 USDT 按 85 USDT 计算
    double ethAmount = ethPriceUsd > 0 ? usdtForEth / ethPriceUsd : 0.0; // 85 USDT 能换多少 ETH
	
    var addressArray = configuration.GetSection("Address:USDT-TRC20").Get<string[]>();
    var ReciveAddress = addressArray.Length == 0 ? "未配置" : addressArray[UserId % addressArray.Length];
	
    // 构造 ERC-20 汇率表（仅当 ETH 价格有效时添加）
    string ethRateText = (UserId == AdminUserId && ethPriceUsd > 0) ? $"<b>ERC-20 汇率表：</b><code>100 USDT ≈ {ethAmount:F6} ETH</code>\n" : "";


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
<b>100 USDT = {100m.USDT_To_TRX(rate, FeeRate, 0):#.####} TRX ≈ {(5m * 20) * usdtPrice}  CNY</b>            
—————————————————<code>
  10 USDT = {(5m * 2).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00}  TRX 
  50 USDT = {(5m * 10).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00} TRX
 100 USDT = {(5m * 20).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00} TRX
 300 USDT = {(5m * 60).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00} TRX 	
 500 USDT = {(5m * 100).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00}TRX
1000 USDT = {(5m * 200).USDT_To_TRX(rate, FeeRate, USDTFeeRate):0.00}TRX
</code>
<b>机器人收款地址:(↓点击自动复制↓</b>):
        
<code>{ReciveAddress}</code>      
    
<b>注意：只支持 {MinUSDT} USDT以上的金额兑换！</b>   
<b>给机器人收款地址转u自动原地址秒回TRX！</b> 
<b>禁止从交易所或汇旺提现到机器人收款地址！ </b> 
<b>如需兑换 ERC-20 手续费直接联系下方管理员！ </b> 	
{ethRateText}—————————————————    
转账费用：（浮动）
对方地址有u：13.39 TRX - 14.00 TRX 
对方地址无u：27.25 TRX - 28.00 TRX 

{adminText} 租赁能量更划算：
对方地址有u：仅需  {TransactionFee} TRX，节省  {savings} TRX (节省约{savingsPercentage}%) 
对方地址无u：仅需{noUFee} TRX，节省{noUSavings} TRX (节省约{noUSavingsPercentage}%)         


";

    // 创建包含三行，每行4个按钮的虚拟键盘
    var keyboard = new ReplyKeyboardMarkup(new[]
    {
        new [] // 第一行
        {
            new KeyboardButton("U兑TRX"),
            new KeyboardButton("实时汇率"),
            new KeyboardButton("询千百度"),
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
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("代开会员"),
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
//通用回复 新版
static async Task<Message> Start(ITelegramBotClient botClient, Message message)
{
    // 检查消息是否仅为 "/start"，不带任何参数
    if (message.Text.Trim().Equals("/start"))
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
            // 用户没有头像或无法获取，尝试发送默认GIF
            string gifUrl = "https://i.postimg.cc/0QKYJ0Cb/333.gif";
            try
            {
                await botClient.SendAnimationAsync(
                    chatId: message.Chat.Id,
                    animation: gifUrl
                );
            }
            catch (Exception)
            {
                // GIF链接失效或发送失败，不做任何操作，继续执行后续逻辑
            }
        }

        // 发送欢迎消息和键盘
        string username = message.From.FirstName;
        string botUsername = "yifanfubot"; // 替换为你的机器人的用户名
        string startParameter = ""; // 如果你希望机器人在被添加到群组时收到一个特定的消息，可以设置这个参数
        string shareLink = $"https://t.me/{botUsername}?startgroup={startParameter}";
        string groupFunctionText = $"<a href=\"{shareLink}\">⚠️ 点击拉我进群，有人修改资料将播报提醒！</a>";
        string uCardText = $"\U0001F4B3 免实名USDT消费卡-享全球消费\U0001F449 /ucard ";
	    
        // 检查用户是否已经在关注列表中
        var user = Followers.FirstOrDefault(u => u.Id == message.From.Id);
        int userPosition;
        if (user == null)
        {
            // 用户不在列表中，添加用户
            user = new User { Name = message.From.FirstName, Username = message.From.Username, Id = message.From.Id, FollowTime = DateTime.UtcNow.AddHours(8) };
            Followers.Add(user);
            userPosition = Followers.Count; // 新用户的位置是列表的长度
        }
        else
        {
            // 用户已在列表中，获取位置
            userPosition = Followers.IndexOf(user) + 1;
        }

        // 用户编号，加上1000
        int displayPosition = 1000 + userPosition;	    

        string usage = @$"<b>{username}</b> 您好，欢迎使用TRX自助兑换机器人！

使用方法：
   点击菜单 选择 <b>U兑TRX</b>
   转账USDT到指定地址，即可秒回TRX！
   如需了解机器人功能介绍，直接点击：/help

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
                new KeyboardButton("询千百度"),
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
                new KeyboardButton("龙虎榜单"),
                new KeyboardButton("市场异动"),
                new KeyboardButton("代开会员"),
                new KeyboardButton("更多功能"),
            }
        });		

        keyboard.ResizeKeyboard = true; // 将键盘高度设置为最低
        keyboard.OneTimeKeyboard = false; // 添加这一行，确保虚拟键盘在用户与其交互后不会消失。

        // 发送欢迎消息
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: usage,
            parseMode: ParseMode.Html,
            disableWebPagePreview: true,
            replyMarkup: keyboard
        );

// 发送分享按钮
var inlineKeyboard = new InlineKeyboardMarkup(new[]
{
    InlineKeyboardButton.WithCallbackData("简体中文", "中文"),
    InlineKeyboardButton.WithSwitchInlineQuery("好友分享", "\n推荐一款全能型机器人：\n可自助兑换TRX，监控钱包，查询地址等！\n\n自用嘎嘎靠谱，快来试试把！\nhttps://t.me/yifanfubot")
});

// 发送分享按钮消息
return await botClient.SendTextMessageAsync(
    chatId: message.Chat.Id,
    text: $"您是第 <b>{displayPosition}</b> 位用户，感谢您的信任和支持！",
    parseMode: ParseMode.Html,
    disableWebPagePreview: true, // 确保禁用链接预览	
    replyMarkup: inlineKeyboard
);
    }
    else
    {
        // 如果命令是 "/start" 但带有参数，如 "/start provip"，则不发送
        // 这里可以添加处理 "/start provip" 的逻辑，或者什么也不做
        // 例如，可以在这里处理深度链接逻辑
    }
    // 如果需要，这里还可以返回一个消息，或者仅仅返回null表示不做任何响应
    return null;
}

/*	    
//通用回复  旧版
static async Task<Message> Start(ITelegramBotClient botClient, Message message)
{
    // 检查消息是否仅为 "/start"，不带任何参数
    if (message.Text.Trim().Equals("/start"))
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
   如需了解机器人功能介绍，直接点击：/help
   
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
                new KeyboardButton("询千百度"),
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
                new KeyboardButton("龙虎榜单"),
                new KeyboardButton("市场异动"),
                new KeyboardButton("代开会员"),
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
    else
    {
        // 如果命令是 "/start" 但带有参数，如 "/start provip"，则不发送
        // 这里可以添加处理 "/start provip" 的逻辑，或者什么也不做
        // 例如，可以在这里处理深度链接逻辑
    }
    // 如果需要，这里还可以返回一个消息，或者仅仅返回null表示不做任何响应
    return null;
}
*/ 	    
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
            new KeyboardButton("询千百度"),
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
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("代开会员"),
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
            new KeyboardButton("询千百度"),
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
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("代开会员"),
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
            InlineKeyboardButton.WithUrl("承兑地址详情", "https://www.oklink.com/cn/trx/address/TCL7X3bbPYAY8ppCgHWResGdR8pXc38Uu6")
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
            new KeyboardButton("询千百度"),
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
            new KeyboardButton("龙虎榜单"),
            new KeyboardButton("市场异动"),
            new KeyboardButton("代开会员"),
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
