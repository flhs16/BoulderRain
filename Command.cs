using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using TShockAPI;

namespace RockRain;

/// <summary>
/// 指令处理类
/// </summary>
public class Command
{
    /// <summary>
    /// 玩家临时配置字典，用于存储玩家当前会话的临时配置
    /// </summary>
    private static readonly ConcurrentDictionary<string, ProjectileData> PlayerTempConfigs = new ConcurrentDictionary<string, ProjectileData>();
    /// <summary>
    /// 更新全局配置并保存到文件
    /// </summary>
    /// <param name="property">属性名</param>
    /// <param name="value">属性值</param>
    private static void UpdateGlobalConfig(string property, object value)
    {
        switch (property)
        {
            case "id":
                Config.Instance.DefaultProjectile.ID = (int)value;
                break;
            case "speed":
                Config.Instance.DefaultProjectile.Speed = (float)value;
                break;
            case "damage":
                Config.Instance.DefaultProjectile.Damage = (int)value;
                break;
            case "knockback":
                Config.Instance.DefaultProjectile.Knockback = (float)value;
                break;
            case "duration":
                Config.Instance.DefaultProjectile.Duration = (int)value;
                break;
            case "interval":
                Config.Instance.DefaultProjectile.SpawnInterval = (int)value;
                break;
            case "height":
                Config.Instance.DefaultProjectile.Height = (float)value;
                break;
            case "width":
                Config.Instance.DefaultProjectile.Width = (float)value;
                break;
            case "timeleft":
                Config.Instance.DefaultProjectile.TimeLeft = (int)value;
                break;
            case "extraupdates":
                Config.Instance.DefaultProjectile.ExtraUpdates = (int)value;
                break;

            case "homing":
                Config.Instance.DefaultProjectile.Homing = (bool)value;
                break;
            case "trackingtarget":
                Config.Instance.DefaultProjectile.TrackingTarget = (TrackingTargetType)value;
                break;
            case "trackingspeed":
                Config.Instance.DefaultProjectile.TrackingSpeed = (float)value;
                break;
            case "trackingrange":
                Config.Instance.DefaultProjectile.TrackingRange = (float)value;
                break;
        }
        Config.Save();
    }

    /// <summary>
    /// 获取玩家的临时配置，如果不存在则创建基于全局默认配置的临时配置
    /// </summary>
    /// <param name="player">玩家</param>
    /// <returns>弹幕配置</returns>
    public static ProjectileData GetPlayerConfig(TSPlayer player)
    {
        string playerKey = player.Name.ToLower();
        
        // 尝试获取玩家的临时配置
        if (PlayerTempConfigs.TryGetValue(playerKey, out ProjectileData? tempConfig))
        {
            return tempConfig;
        }
        
        // 如果临时配置不存在，创建一个基于全局默认配置的临时配置
        var defaultConfig = Config.Instance.DefaultProjectile;
        var config = new ProjectileData()
        {
            ID = defaultConfig.ID,
            Speed = defaultConfig.Speed,
            Damage = defaultConfig.Damage,
            Knockback = defaultConfig.Knockback,
            Duration = defaultConfig.Duration,
            SpawnInterval = defaultConfig.SpawnInterval,
            Height = defaultConfig.Height,
            Width = defaultConfig.Width,
            TimeLeft = defaultConfig.TimeLeft,
            ExtraUpdates = defaultConfig.ExtraUpdates,

            Homing = defaultConfig.Homing,
            TrackingTarget = defaultConfig.TrackingTarget,
            TrackingSpeed = defaultConfig.TrackingSpeed,
            TrackingRange = defaultConfig.TrackingRange
        };
        
        // 保存临时配置
        PlayerTempConfigs.TryAdd(playerKey, config);
        return config;
    }

    /// <summary>
    /// 主指令处理
    /// </summary>
    /// <param name="args">指令参数</param>
    public static void RockRainMain(CommandArgs args)
    {
        if (args.Parameters.Count == 0)
        {
            ShowHelp(args.Player);
            return;
        }

        string subCommand = args.Parameters[0].ToLower();

        switch (subCommand)
        {
            case "help":
                ShowHelp(args.Player);
                break;
            case "start":
                StartEffect(args);
                break;
            case "stop":
                StopEffect(args);
                break;
            case "set":
                HandleSetCommand(args);
                break;
            case "reload":
                ReloadConfig(args);
                break;
            default:
                args.Player.SendErrorMessage("[巨石雨] 未知指令，请输入 /rm help 查看帮助");
                break;
        }
    }

    /// <summary>
    /// 显示帮助信息
    /// </summary>
    /// <param name="player">玩家</param>
    private static void ShowHelp(TSPlayer player)
    {
        player.SendInfoMessage("===== 巨石雨插件指令 ====");
        player.SendInfoMessage("/rm help - 显示此帮助信息");
        player.SendInfoMessage("/rm start - 开始巨石雨效果");
        player.SendInfoMessage("/rm stop - 停止巨石雨效果");
        player.SendInfoMessage("/rm set - 查看当前弹幕属性设置");
        player.SendInfoMessage("/rm set <属性> <值> - 设置弹幕属性");
        player.SendInfoMessage("/rm reload - 重载配置文件");
        player.SendInfoMessage("可设置的属性：id, speed, damage, knockback, duration, interval, height, width, timeleft, extraupdates, homing, trackingtarget, trackingspeed, trackingrange");
        player.SendInfoMessage("=======================");
    }

    /// <summary>
    /// 开始巨石雨效果
    /// </summary>
    /// <param name="args">指令参数</param>
    private static void StartEffect(CommandArgs args)
    {
        // 获取玩家的临时配置
        ProjectileData playerConfig = GetPlayerConfig(args.Player);
        
        // 使用玩家的临时配置启动效果
        RockRainManager.AddEffect(args.Player, playerConfig);
        
        // 清除玩家的临时配置，确保下一次启动使用新的配置
        string playerKey = args.Player.Name.ToLower();
        PlayerTempConfigs.TryRemove(playerKey, out _);
    }

    /// <summary>
    /// 停止巨石雨效果
    /// </summary>
    /// <param name="args">指令参数</param>
    private static void StopEffect(CommandArgs args)
    {
        RockRainManager.RemoveEffects(args.Player);
        args.Player.SendSuccessMessage("[巨石雨] 效果已停止");
    }

    /// <summary>
    /// 重载配置文件
    /// </summary>
    /// <param name="args">指令参数</param>
    private static void ReloadConfig(CommandArgs args)
    {
        Config.Reload();
        args.Player.SendSuccessMessage("[巨石雨] 配置文件已重载");
    }

    /// <summary>
    /// 处理set指令
    /// </summary>
    /// <param name="args">指令参数</param>
    private static void HandleSetCommand(CommandArgs args)
    {
        ProjectileData config = GetPlayerConfig(args.Player);

        if (args.Parameters.Count == 1)
        {
            // 查看当前设置
            ShowCurrentSettings(args.Player, config);
        }
        else if (args.Parameters.Count >= 3)
        {
            // 设置属性
            string property = args.Parameters[1].ToLower();
            string valueStr = args.Parameters[2];
            SetProperty(args.Player, config, property, valueStr);
        }
        else
        {
            args.Player.SendErrorMessage("[巨石雨] 语法错误：/rm set <属性> <值>");
        }
    }

    /// <summary>
    /// 显示当前设置
    /// </summary>
    /// <param name="player">玩家</param>
    /// <param name="config">配置</param>
    private static void ShowCurrentSettings(TSPlayer player, ProjectileData config)
    {
        player.SendInfoMessage("===== 当前弹幕设置 ====");
        player.SendInfoMessage($"弹幕ID: {config.ID}");
        player.SendInfoMessage($"下落速度: {config.Speed}");
        player.SendInfoMessage($"伤害: {config.Damage}");
        player.SendInfoMessage($"击退: {config.Knockback}");
        player.SendInfoMessage($"持续时间: {config.Duration} 帧");
        player.SendInfoMessage($"生成间隔: {config.SpawnInterval} 帧");
        player.SendInfoMessage($"生成高度: {config.Height} 像素");
        player.SendInfoMessage($"生成范围: {config.Width} 像素");
        player.SendInfoMessage($"生命周期: {config.TimeLeft} 帧");
        player.SendInfoMessage($"额外更新次数: {config.ExtraUpdates}");

        player.SendInfoMessage($"追踪模式: {config.Homing}");
        player.SendInfoMessage($"追踪目标: {config.TrackingTarget}");
        player.SendInfoMessage($"追踪速度: {config.TrackingSpeed}");
        player.SendInfoMessage($"追踪范围: {config.TrackingRange}");
        player.SendInfoMessage("====================");
    }

    /// <summary>
    /// 设置属性（更新玩家临时配置）
    /// </summary>
    /// <param name="player">玩家</param>
    /// <param name="config">配置</param>
    /// <param name="property">属性名</param>
    /// <param name="valueStr">属性值</param>
    private static void SetProperty(TSPlayer player, ProjectileData config, string property, string valueStr)
    {
        try
        {
            string playerKey = player.Name.ToLower();
            
            switch (property)
            {
                case "id":
                    if (int.TryParse(valueStr, out int id))
                    {
                        config.ID = id;
                        player.SendSuccessMessage($"[巨石雨] 临时弹幕ID已设置为: {id}");
                    }
                    else
                    {
                        player.SendErrorMessage("[巨石雨] 无效的弹幕ID");
                    }
                    break;
                case "speed":
                    if (float.TryParse(valueStr, out float speed))
                    {
                        config.Speed = speed;
                        player.SendSuccessMessage($"[巨石雨] 临时下落速度已设置为: {speed}");
                    }
                    else
                    {
                        player.SendErrorMessage("[巨石雨] 无效的速度值");
                    }
                    break;
                case "damage":
                    if (int.TryParse(valueStr, out int damage))
                    {
                        config.Damage = damage;
                        player.SendSuccessMessage($"[巨石雨] 临时伤害已设置为: {damage}");
                    }
                    else
                    {
                        player.SendErrorMessage("[巨石雨] 无效的伤害值");
                    }
                    break;
                case "knockback":
                    if (float.TryParse(valueStr, out float knockback))
                    {
                        config.Knockback = knockback;
                        player.SendSuccessMessage($"[巨石雨] 临时击退已设置为: {knockback}");
                    }
                    else
                    {
                        player.SendErrorMessage("[巨石雨] 无效的击退值");
                    }
                    break;
                case "duration":
                    if (int.TryParse(valueStr, out int duration))
                    {
                        config.Duration = duration;
                        player.SendSuccessMessage($"[巨石雨] 临时持续时间已设置为: {duration} 帧");
                    }
                    else
                    {
                        player.SendErrorMessage("[巨石雨] 无效的持续时间");
                    }
                    break;
                case "interval":
                    if (int.TryParse(valueStr, out int interval))
                    {
                        config.SpawnInterval = interval;
                        player.SendSuccessMessage($"[巨石雨] 临时生成间隔已设置为: {interval} 帧");
                    }
                    else
                    {
                        player.SendErrorMessage("[巨石雨] 无效的生成间隔");
                    }
                    break;
                case "height":
                    if (float.TryParse(valueStr, out float height))
                    {
                        config.Height = height;
                        player.SendSuccessMessage($"[巨石雨] 临时生成高度已设置为: {height} 像素");
                    }
                    else
                    {
                        player.SendErrorMessage("[巨石雨] 无效的生成高度");
                    }
                    break;
                case "width":
                    if (float.TryParse(valueStr, out float width))
                    {
                        config.Width = width;
                        player.SendSuccessMessage($"[巨石雨] 临时生成范围已设置为: {width} 像素");
                    }
                    else
                    {
                        player.SendErrorMessage("[巨石雨] 无效的生成范围");
                    }
                    break;

                case "homing":
                    if (bool.TryParse(valueStr, out bool homing))
                    {
                        config.Homing = homing;
                        player.SendSuccessMessage($"[巨石雨] 临时追踪模式已设置为: {homing}");
                    }
                    else
                    {
                        player.SendErrorMessage("[巨石雨] 无效的布尔值 (true/false)");
                    }
                    break;
                case "trackingtarget":
                    if (Enum.TryParse<TrackingTargetType>(valueStr, true, out TrackingTargetType trackingTarget))
                    {
                        config.TrackingTarget = trackingTarget;
                        player.SendSuccessMessage($"[巨石雨] 临时追踪目标已设置为: {trackingTarget}");
                    }
                    else
                    {
                        player.SendErrorMessage("[巨石雨] 无效的追踪目标类型");
                        player.SendInfoMessage("可用类型: None, Monsters, Players, All");
                    }
                    break;
                case "trackingspeed":
                    if (float.TryParse(valueStr, out float trackingSpeed))
                    {
                        config.TrackingSpeed = trackingSpeed;
                        player.SendSuccessMessage($"[巨石雨] 临时追踪速度已设置为: {trackingSpeed}");
                    }
                    else
                    {
                        player.SendErrorMessage("[巨石雨] 无效的追踪速度值");
                    }
                    break;
                case "trackingrange":
                    if (float.TryParse(valueStr, out float trackingRange))
                    {
                        config.TrackingRange = trackingRange;
                        player.SendSuccessMessage($"[巨石雨] 临时追踪范围已设置为: {trackingRange}");
                    }
                    else
                    {
                        player.SendErrorMessage("[巨石雨] 无效的追踪范围值");
                    }
                    break;
                case "timeleft":
                    if (int.TryParse(valueStr, out int timeLeft))
                    {
                        config.TimeLeft = timeLeft;
                        player.SendSuccessMessage($"[巨石雨] 临时生命周期已设置为: {timeLeft} 帧");
                    }
                    else
                    {
                        player.SendErrorMessage("[巨石雨] 无效的生命周期值");
                    }
                    break;

                case "extraupdates":
                    if (int.TryParse(valueStr, out int extraUpdates))
                    {
                        config.ExtraUpdates = extraUpdates;
                        player.SendSuccessMessage($"[巨石雨] 临时额外更新次数已设置为: {extraUpdates}");
                    }
                    else
                    {
                        player.SendErrorMessage("[巨石雨] 无效的更新次数值");
                    }
                    break;
                default:
                    player.SendErrorMessage($"[巨石雨] 未知属性: {property}");
                    player.SendInfoMessage("可设置的属性：id, speed, damage, knockback, duration, interval, height, width, timeleft, tilecollide, penetrate, useslocalnpcimmunity, localnpchitcooldown, extraupdates, homing, trackingtarget, trackingspeed, trackingrange");
                    break;
            }
            
            // 更新玩家的临时配置
            PlayerTempConfigs[playerKey] = config;
        }
        catch (Exception ex)
        {
            player.SendErrorMessage($"[巨石雨] 设置属性失败: {ex.Message}");
        }
    }
}