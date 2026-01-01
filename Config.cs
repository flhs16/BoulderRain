using Newtonsoft.Json;
using System.IO;
using TShockAPI;

namespace RockRain;

/// <summary>
/// 配置文件管理
/// </summary>
public class Config
{
    private static Config? _instance;
    private const string ConfigFileName = "RockRain.json";
    private static string ConfigPath => Path.Combine(TShock.SavePath, ConfigFileName);

    /// <summary>
    /// 配置实例
    /// </summary>
    public static Config Instance => _instance ??= Load();

    /// <summary>
    /// 默认弹幕数据
    /// </summary>
    [JsonProperty("默认弹幕配置")]
    public ProjectileData DefaultProjectile { get; set; } = new ProjectileData();

    /// <summary>
    /// 是否启用调试模式
    /// </summary>
    [JsonProperty("调试模式")]
    public bool DebugMode { get; set; } = false;

    /// <summary>
    /// 最大同时存在的巨石雨生成数量
    /// </summary>
    [JsonProperty("最大生成数量")]
    public int MaxEffects { get; set; } = 10;

    /// <summary>
    /// 玩家加入服务器时自动开启巨石雨效果
    /// </summary>
    [JsonProperty("玩家加入自动开启")]
    public bool AutoEnableOnJoin { get; set; } = false;

    /// <summary>
    /// 加载配置文件
    /// </summary>
    public static Config Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                _instance = JsonConvert.DeserializeObject<Config>(json) ?? new Config();
                TShock.Log.ConsoleInfo("[巨石雨] 配置文件加载成功");
            }
            else
            {
                _instance = new Config();
                Save();
                TShock.Log.ConsoleInfo("[巨石雨] 配置文件不存在，已创建默认配置");
            }
        }
        catch (Exception ex)
        {
            _instance = new Config();
            TShock.Log.ConsoleError("[巨石雨] 配置文件加载失败: " + ex.Message);
        }
        return _instance!;
    }

    /// <summary>
    /// 保存配置文件
    /// </summary>
    public static void Save()
    {
        try
        {
            string json = JsonConvert.SerializeObject(_instance, Formatting.Indented);
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError("[巨石雨] 配置文件保存失败: " + ex.Message);
        }
    }

    /// <summary>
    /// 重载配置
    /// </summary>
    public static void Reload()
    {
        TShock.Log.ConsoleInfo("[巨石雨] 开始重载配置文件...");
        Load();
        TShock.Log.ConsoleInfo("[巨石雨] 配置文件重载完成");
        if (_instance != null)
        {
            TShock.Log.ConsoleInfo($"[巨石雨] 当前追踪模式: {_instance.DefaultProjectile.Homing}");
            TShock.Log.ConsoleInfo($"[巨石雨] 当前自动开启: {_instance.AutoEnableOnJoin}");
            
            // 更新所有活动效果的配置
            RockRainManager.UpdateActiveEffectsConfig();
        }
    }

    /// <summary>
    /// 卸载配置
    /// </summary>
    public static void Unload()
    {
        _instance = null;
    }
}