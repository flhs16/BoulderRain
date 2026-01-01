using Newtonsoft.Json;

namespace RockRain;

/// <summary>
/// 弹幕数据类
/// </summary>
public class ProjectileData
{
    /// <summary>
    /// 弹幕ID
    /// </summary>
    [JsonProperty("弹幕ID")]
    public int ID { get; set; } = 99; // 使用默认弹幕ID=99

    /// <summary>
    /// 下落速度
    /// </summary>
    [JsonProperty("下落速度")]
    public float Speed { get; set; } = 10f;

    /// <summary>
    /// 伤害
    /// </summary>
    [JsonProperty("伤害")]
    public int Damage { get; set; } = 50;

    /// <summary>
    /// 击退
    /// </summary>
    [JsonProperty("击退")]
    public float Knockback { get; set; } = 5f;

    /// <summary>
    /// 持续时间（帧）
    /// </summary>
    [JsonProperty("持续时间")]
    public int Duration { get; set; } = 1800;

    /// <summary>
    /// 生成间隔（帧）
    /// </summary>
    [JsonProperty("生成间隔")]
    public int SpawnInterval { get; set; } = 10;

    /// <summary>
    /// 生成高度
    /// </summary>
    [JsonProperty("生成高度")]
    public float Height { get; set; } = 600f;

    /// <summary>
    /// 生成范围
    /// </summary>
    [JsonProperty("生成范围")]
    public float Width { get; set; } = 1000f;



    /// <summary>
    /// 是否开启追踪模式
    /// </summary>
    [JsonProperty("追踪模式")]
    public bool Homing { get; set; } = false;
    
    /// <summary>
    /// 追踪目标类型
    /// </summary>
    [JsonProperty("追踪目标")]
    public TrackingTargetType TrackingTarget { get; set; } = TrackingTargetType.Monsters;
    
    /// <summary>
    /// 追踪速度
    /// </summary>
    [JsonProperty("追踪速度")]
    public float TrackingSpeed { get; set; } = 5f;
    
    /// <summary>
    /// 追踪范围
    /// </summary>
    [JsonProperty("追踪范围")]
    public float TrackingRange { get; set; } = 500f;
    
    /// <summary>
    /// 弹丸生命周期（帧）
    /// </summary>
    [JsonProperty("生命周期")]
    public int TimeLeft { get; set; } = 300;
    
    /// <summary>
    /// 每帧更新次数
    /// </summary>
    [JsonProperty("每帧更新次数")]
    public int ExtraUpdates { get; set; } = 0;
}

/// <summary>
/// 追踪目标类型枚举
/// </summary>
public enum TrackingTargetType
{
    /// <summary>
    /// 不追踪
    /// </summary>
    None,
    /// <summary>
    /// 追踪怪物
    /// </summary>
    Monsters,
    /// <summary>
    /// 追踪玩家
    /// </summary>
    Players,
    /// <summary>
    /// 追踪所有目标
    /// </summary>
    All
}