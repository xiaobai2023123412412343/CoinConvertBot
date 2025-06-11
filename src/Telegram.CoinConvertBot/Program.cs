
using FreeSql;
using FreeSql.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Data.Common;
using System.Net;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.CoinConvertBot.BgServices;
using Telegram.CoinConvertBot.BgServices.BotHandler;
using Telegram.CoinConvertBot.Domains;
using Telegram.CoinConvertBot.Models;
using TronNet;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var host = Host.CreateDefaultBuilder(args);

host.UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.Console());
host.ConfigureServices(ConfigureServices);
using var app = host.Build();

try
{
    await app.RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "机器人启动失败！");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

static void ConfigureServices(HostBuilderContext Context, IServiceCollection Services)
{
    var Configuration = Context.Configuration;
    var HostingEnvironment = Context.HostingEnvironment;

    #region 数据库
    var connectionString = Configuration.GetConnectionString("DB");
    IFreeSql fsql = new FreeSqlBuilder()
        .UseConnectionString(DataType.Sqlite, connectionString)
        .Build();
    fsql.CodeFirst.SyncStructure(GetTypesByNameSpace());
    Services.AddSingleton(fsql);
    Services.AddFreeRepository();
    Services.AddScoped<UnitOfWorkManager>();
    #endregion

    #region TRON Net
    Services.AddTronNet(x =>
    {
        x.Network = TronNetwork.MainNet;
        x.Channel = new GrpcChannelOption { Host = Configuration.GetValue<string>("TronNet:Host"), Port = Configuration.GetValue<int>("TronNet:ChannelPort") };
        x.SolidityChannel = new GrpcChannelOption { Host = Configuration.GetValue<string>("TronNet:Host"), Port = Configuration.GetValue<int>("TronNet:SolidityChannelPort") };
        x.ApiKey = Configuration.GetValue<string>("TronNet:ApiKey");
    });
    Services.Configure<MyTronConfig>(Configuration.GetSection("TronConfig"));
    #endregion

    #region 机器人
    var token = Configuration.GetValue<string>("BotConfig:Token");
    var baseUrl = Configuration.GetValue<string>("BotConfig:Proxy");
    var useProxy = Configuration.GetValue<bool>("BotConfig:UseProxy");
    TelegramBotClient botClient = new TelegramBotClient(new TelegramBotClientOptions(token, baseUrl));

    var WebProxy = Configuration.GetValue<string>("WebProxy");
    if (useProxy && !string.IsNullOrEmpty(WebProxy))
    {
        var uri = new Uri(WebProxy);
        var userinfo = uri.UserInfo.Split(":");
        var webProxy = new WebProxy($"{uri.Scheme}://{uri.Authority}")
        {
            Credentials = string.IsNullOrEmpty(uri.UserInfo) ? null : new NetworkCredential(userinfo[0], userinfo[1])
        };
        var httpClient = new HttpClient(
            new HttpClientHandler { Proxy = webProxy, UseProxy = true, }
        );
        botClient = new TelegramBotClient(token, httpClient);

    }
    Log.Logger.Information("开始{UseProxy}连接Telegram服务器...", (useProxy ? "使用代理" : "不使用代理"));
    var me = botClient.GetMeAsync().GetAwaiter().GetResult();
    UpdateHandlers.BotUserName = me.Username;
    var SetDefaultMenu = Configuration.GetValue<bool>("SetDefaultMenu");
    if (SetDefaultMenu)
    {
        botClient.SetMyCommandsAsync(new BotCommand[]
        {
        new BotCommand(){Command="start",Description="开始使用"},
        new BotCommand(){Command="yi",Description="U兑TRX"},
        new BotCommand(){Command="fan",Description="实时汇率"},
        new BotCommand(){Command="fu",Description="汇率换算"},
        new BotCommand(){Command="qiand",Description="一键签到"},            
        new BotCommand(){Command="ucard",Description="消费u卡"},               
        new BotCommand(){Command="trc",Description="千百度查"},   
        new BotCommand(){Command="tron",Description="能量租赁"},            
        new BotCommand(){Command="btc",Description="加密货币"},
        new BotCommand(){Command="usd",Description="外汇助手"},
        new BotCommand(){Command="more",Description="更多功能"},
        new BotCommand(){Command="home",Description="地址监听"}, 
        new BotCommand(){Command="cny",Description="合约助手"},
        new BotCommand(){Command="usdt",Description="实时u价"},                   
        new BotCommand(){Command="jkbtc",Description="行情监控"},                     
        new BotCommand(){Command="zijin",Description="资金费率"},   
        new BotCommand(){Command="caifu",Description="财富密码"},              
        new BotCommand(){Command="faxian",Description="龙虎榜单"},   
        new BotCommand(){Command="bijiacha",Description="信号广场"},             
        new BotCommand(){Command="help",Description="使用帮助"},             
        }).GetAwaiter().GetResult();
    }
    Log.Logger.Information("Telegram机器人上线！机器人ID：{Id}({username})，机器人名字：{FirstName}.", me.Id, $"@{me.Username}", me.FirstName);
    var AdminUserId = Configuration.GetValue<long>("BotConfig:AdminUserId");
    if (AdminUserId > 0)
    {
        botClient.SendTextMessageAsync(AdminUserId, $"您的机器人<a href=\"tg://user?id={me.Id}\">{me.FirstName}</a>已上线!", Telegram.Bot.Types.Enums.ParseMode.Html);
    }
    Services.AddSingleton<ITelegramBotClient>(botClient);
    Services.AddHostedService<BotService>();
    #endregion

    Services.AddHostedService<UpdateRateService>();
    Services.AddHostedService<USDT_TRC20Service>();
    Services.AddHostedService<TransferTrxService>();
}

//获取所有表
static Type[] GetTypesByNameSpace()
{
    List<Type> tableAssembies = new List<Type>();
    List<string> entitiesFullName = new List<string>()
    {
        "Telegram.CoinConvertBot.Domains.Tables"
    };
    foreach (Type type in Assembly.GetAssembly(typeof(IEntity))!.GetExportedTypes())
        foreach (var fullname in entitiesFullName)
            if (type.FullName!.StartsWith(fullname) && type.IsClass && type.GetCustomAttributes().Any(
                x => x is not TableAttribute || x is TableAttribute && !((TableAttribute)x).DisableSyncStructure))
                tableAssembies.Add(type);

    return tableAssembies.ToArray();
}
