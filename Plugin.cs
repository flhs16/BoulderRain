using Microsoft.Xna.Framework;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace RockRain;

[ApiVersion(2, 1)]
public class Plugin : TerrariaPlugin
{
    public override string Author => "天子";

    public override string Description => "red最喜欢的一集";

    public override string Name => System.Reflection.Assembly.GetExecutingAssembly().GetName().Name!;
    public override Version Version => new Version(1, 0, 0, 0);

    public Plugin(Main game) : base(game)
    {
        this.Order = 10;
    }

    public override void Initialize()
    {
        // 加载配置
        Config.Load();
        
        // 初始化管理器
        RockRainManager.Initialize();
        
        // 注册游戏更新钩子
        ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        
        // 注册玩家加入钩子
        ServerApi.Hooks.ServerJoin.Register(this, OnServerJoin);
        
        // 注册配置重载事件
        GeneralHooks.ReloadEvent += OnReload;
        
        // 注册指令
        Commands.ChatCommands.Add(new TShockAPI.Command("rockrain.use", Command.RockRainMain, "rm"));
        Commands.ChatCommands.Add(new TShockAPI.Command("rockrain.use", Command.RockRainMain, "rockrain"));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // 释放资源
            ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
            ServerApi.Hooks.ServerJoin.Deregister(this, OnServerJoin);
            GeneralHooks.ReloadEvent -= OnReload;
            Config.Unload();
            RockRainManager.Dispose();
        }
        base.Dispose(disposing);
    }

    private void OnGameUpdate(EventArgs args)
    {
        // 更新巨石雨效果
        RockRainManager.Update();
    }

    /// <summary>
    /// 配置重载事件处理
    /// </summary>
    /// <param name="args">事件参数</param>
    private void OnReload(ReloadEventArgs args)
    {
        Config.Reload();
        RockRainManager.UpdateActiveEffectsConfig();
        args.Player?.SendSuccessMessage("[巨石雨] 配置文件已重载");
        TShock.Log.ConsoleInfo("[巨石雨] 配置文件已重载");
    }

    /// <summary>
    /// 玩家加入服务器事件处理
    /// </summary>
    /// <param name="args">事件参数</param>
    private void OnServerJoin(JoinEventArgs args)
    {
        if (Config.Instance.AutoEnableOnJoin)
        {
            // 使用延迟执行，确保玩家完全初始化
            Task.Run(async () =>
            {
                // 等待2秒以确保玩家完全加载
                await Task.Delay(2000);
                
                // 再次检查玩家状态
                TSPlayer player = TShock.Players[args.Who];
                if (player != null && player.TPlayer != null && player.TPlayer.active && player.IsLoggedIn)
                {
                    // 检查玩家是否已经有巨石雨效果
                    var effects = RockRainManager.GetEffects(player);
                    if (effects.Count == 0)
                    {
                        RockRainManager.AddEffect(player);
                        player.SendSuccessMessage("[巨石雨] 已自动为您激活巨石雨效果");
                    }
                }
            });
        }
    }
}