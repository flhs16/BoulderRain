using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Terraria;
using TShockAPI;

namespace RockRain;

/// <summary>
    /// 巨石雨效果
    /// </summary>
    public class RockRainEffect
    {
        /// <summary>
        /// 效果ID
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// 拥有者
        /// </summary>
        public TSPlayer Owner { get; set; } = null!;

        /// <summary>
        /// 开始时间（帧）
        /// </summary>
        public int StartTime { get; set; }

        /// <summary>
        /// 上次生成时间（帧）
        /// </summary>
        public int LastSpawnTime { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// 弹幕配置
        /// </summary>
        public ProjectileData ProjectileData { get; set; } = null!;

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsActive => Active && Owner.TPlayer != null && Owner.TPlayer.active;

        /// <summary>
        /// 生成弹幕
        /// </summary>
        public void SpawnProjectile()
        {
            if (!IsActive || Owner.TPlayer == null || !Owner.TPlayer.active)
                return;

            int currentTime = (int)Terraria.Main.GameUpdateCount;
            ProjectileData config = ProjectileData;
            
            if (currentTime - LastSpawnTime < config.SpawnInterval)
                return;

            LastSpawnTime = currentTime;

            // 计算生成位置
            Vector2 playerPos = Owner.TPlayer.Center;
            float halfWidth = config.Width / 2;
            float xOffset = (float)(Main.rand.NextDouble() * config.Width - halfWidth);
            Vector2 spawnPos = new Vector2(playerPos.X + xOffset, playerPos.Y - config.Height);

            // 创建弹幕
            int type = config.ID;
            float speedX = (float)(Main.rand.NextDouble() - 0.5) * 2;
            float speedY = config.Speed;
            Vector2 velocity = new Vector2(speedX, speedY);
            int damage = config.Damage;
            float knockback = config.Knockback;

            // 根据弹幕类型设置不同的属性
            var entitySource = Projectile.GetNoneSource();
            
            // 友好弹幕：对怪物造成伤害，不伤害玩家
            // 敌对弹幕：对玩家造成伤害，不伤害怪物
            int projIndex;
            
            // 创建弹幕，使用系统索引0（敌对弹幕，对怪物造成伤害）
            projIndex = Projectile.NewProjectile(
                entitySource,
                spawnPos.X,
                spawnPos.Y,
                velocity.X,
                velocity.Y,
                type,
                damage,
                knockback,
                0
            );

            if (projIndex >= 0 && projIndex < Main.maxProjectiles)
            {
                Projectile proj = Main.projectile[projIndex];
                // 设置其他属性
                proj.timeLeft = config.TimeLeft; // 生命周期
                proj.tileCollide = true; // 默认启用地形碰撞
                proj.penetrate = 1; // 默认穿透次数
                proj.damage = config.Damage;
                proj.knockBack = config.Knockback;
                proj.velocity = velocity;
                proj.extraUpdates = config.ExtraUpdates;
                proj.damage = config.Damage; // 确保伤害值被正确设置
                
                // 设置追踪属性
                if (config.Homing)
                {
                    // 设置追踪标识，包含效果ID以便识别属于哪个效果实例
                    var trackingId = $"rockrain_tracking_{ID}_{projIndex}_{Owner.TPlayer.whoAmI}";
                    proj.miscText = trackingId;
                    RockRainManager.AddTrackingProjectile(trackingId, proj);
                }
                
                // 确保伤害值被正确设置
                proj.damage = config.Damage;
                
                // 网络同步（多人游戏需要）
                if (Main.netMode != 0)
                {
                    NetMessage.SendData(27, -1, -1, null, projIndex);
                }
                
                // 向所有玩家发送弹幕创建数据包
                TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", projIndex);
            }
        }
    }

/// <summary>
/// 巨石雨管理器
/// </summary>
public static class RockRainManager
{
    private static int _nextEffectId = 1;
    private static List<RockRainEffect> _activeEffects = new List<RockRainEffect>();
    
    // 追踪弹幕管理
    private static readonly ConcurrentDictionary<string, Projectile> _trackingProjectiles = new ConcurrentDictionary<string, Projectile>();
    
    /// <summary>
    /// 添加追踪弹幕
    /// </summary>
    /// <param name="trackingId">追踪标识</param>
    /// <param name="projectile">弹幕实例</param>
    public static void AddTrackingProjectile(string trackingId, Projectile projectile)
    {
        _trackingProjectiles.TryAdd(trackingId, projectile);
    }

    /// <summary>
    /// 初始化管理器
    /// </summary>
    public static void Initialize()
    {
        _activeEffects.Clear();
        _nextEffectId = 1;
        _trackingProjectiles.Clear();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public static void Dispose()
    {
        _activeEffects.Clear();
        _trackingProjectiles.Clear();
    }

    /// <summary>
    /// 更新所有活动效果的配置为新的默认配置
    /// </summary>
    public static void UpdateActiveEffectsConfig()
    {
        // 获取最新的默认配置
        var defaultConfig = Config.Instance.DefaultProjectile;
        
        // 更新所有活动效果的配置
        foreach (var effect in _activeEffects)
        {
            if (effect.Active)
            {
                // 创建新的配置副本，避免所有效果共享同一个引用
                effect.ProjectileData = new ProjectileData()
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
            }
        }
    }

    /// <summary>
    /// 更新所有效果
    /// </summary>
    public static void Update()
    {
        // 移除无效效果
        _activeEffects.RemoveAll(effect => !effect.IsActive);

        // 更新剩余效果
        foreach (RockRainEffect effect in _activeEffects)
        {
            effect.SpawnProjectile();
        }
        
        // 清理不再活跃的追踪弹幕
        var inactiveProjectiles = new List<string>();
        foreach (var kvp in _trackingProjectiles)
        {
            if (!kvp.Value.active)
            {
                inactiveProjectiles.Add(kvp.Key);
            }
        }
        
        foreach (var key in inactiveProjectiles)
        {
            _trackingProjectiles.TryRemove(key, out _);
        }
        
        // 追踪模式逻辑：始终执行，只要弹幕有追踪标识
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            var projectile = Main.projectile[i];
            
            if (projectile.active && !string.IsNullOrEmpty(projectile.miscText) && _trackingProjectiles.ContainsKey(projectile.miscText))
            {
                // 从追踪标识中解析效果ID
                string[] parts = projectile.miscText.Split('_');
                if (parts.Length >= 5 && int.TryParse(parts[2], out int effectId))
                {
                    // 查找对应的效果实例
                    var effect = _activeEffects.FirstOrDefault(e => e.ID == effectId);
                    if (effect != null)
                    {
                        // 使用效果实例的追踪属性
                        var config = effect.ProjectileData;
                        
                        // 查找最近的目标
                        var targetPosition = FindNearestTarget(projectile.position, config.TrackingRange, config.TrackingTarget);
                        
                        if (targetPosition.HasValue)
                        {
                            // 计算朝向目标的向量
                            var direction = targetPosition.Value - projectile.Center;
                            if (direction.Length() > 0)
                            {
                                direction.Normalize();
                                
                                // 使用平滑转向，而不是直接设置速度
                                // 计算目标速度
                                var targetVelocity = direction * config.TrackingSpeed;
                                
                                // 平滑插值，使弹幕逐渐转向目标
                                var smoothingFactor = 0.1f; // 转向平滑度，值越小转向越慢
                                projectile.velocity.X = projectile.velocity.X * (1 - smoothingFactor) + targetVelocity.X * smoothingFactor;
                                projectile.velocity.Y = projectile.velocity.Y * (1 - smoothingFactor) + targetVelocity.Y * smoothingFactor;
                                
                                // 限制最大速度
                                var maxSpeed = config.TrackingSpeed * 1.5f; // 最大速度为追踪速度的1.5倍
                                var currentSpeed = projectile.velocity.Length();
                                if (currentSpeed > maxSpeed)
                                {
                                    projectile.velocity = projectile.velocity / currentSpeed * maxSpeed;
                                }
                                
                                // 更新弹幕位置信息
                                NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, i);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 添加巨石雨效果
    /// </summary>
    /// <param name="player">玩家</param>
    /// <param name="projectileData">弹幕配置（可选，默认使用全局配置）</param>
    /// <returns>效果ID</returns>
    public static int AddEffect(TSPlayer player, ProjectileData? projectileData = null)
    {
        if (player == null || !player.TPlayer.active)
            return -1;

        // 检查效果数量限制
        if (_activeEffects.Count >= Config.Instance.MaxEffects)
        {
            player.SendErrorMessage("[巨石雨] 达到最大生成数量限制");
            return -1;
        }

        // 使用提供的配置或全局默认配置
        var config = projectileData ?? Config.Instance.DefaultProjectile;
        
        // 创建新效果
        RockRainEffect effect = new RockRainEffect
        {
            ID = _nextEffectId++,
            Owner = player,
            StartTime = (int)Terraria.Main.GameUpdateCount,
            LastSpawnTime = (int)Terraria.Main.GameUpdateCount,
            ProjectileData = new ProjectileData() // 创建配置的副本，确保每个效果有独立的配置
            {
                ID = config.ID,
                Speed = config.Speed,
                Damage = config.Damage,
                Knockback = config.Knockback,
                Duration = config.Duration,
                SpawnInterval = config.SpawnInterval,
                Height = config.Height,
                Width = config.Width,
                Homing = config.Homing,
                TrackingTarget = config.TrackingTarget,
                TrackingSpeed = config.TrackingSpeed,
                TrackingRange = config.TrackingRange
            }
        };

        _activeEffects.Add(effect);
        player.SendSuccessMessage("[巨石雨] 效果已激活");
        return effect.ID;
    }

    /// <summary>
    /// 移除指定玩家的所有效果
    /// </summary>
    /// <param name="player">玩家</param>
    public static void RemoveEffects(TSPlayer player)
    {
        _activeEffects.RemoveAll(effect => effect.Owner == player);
    }

    /// <summary>
    /// 获取指定玩家的效果
    /// </summary>
    /// <param name="player">玩家</param>
    /// <returns>效果列表</returns>
    public static List<RockRainEffect> GetEffects(TSPlayer player)
    {
        return _activeEffects.FindAll(effect => effect.Owner == player);
    }
    
    /// <summary>
    /// 查找最近的敌人NPC
    /// </summary>
    /// <param name="position">当前位置</param>
    /// <param name="range">搜索范围</param>
    /// <returns>找到的敌人NPC，如果没有找到则返回null</returns>
    private static NPC? FindNearestEnemy(Vector2 position, float range)
    {
        NPC? nearestNPC = null;
        float nearestDistance = range;
        
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            var npc = Main.npc[i];
            
            // 检查NPC是否有效且敌对
            if (npc.active && !npc.friendly && npc.lifeMax > 0 && !npc.dontTakeDamage)
            {
                var distance = Vector2.Distance(position, npc.Center);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestNPC = npc;
                }
            }
        }
        
        return nearestNPC;
    }
    
    /// <summary>
    /// 查找最近的玩家
    /// </summary>
    /// <param name="position">当前位置</param>
    /// <param name="range">搜索范围</param>
    /// <returns>找到的玩家，如果没有找到则返回null</returns>
    private static Player? FindNearestPlayer(Vector2 position, float range)
    {
        Player? nearestPlayer = null;
        float nearestDistance = range;
        
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            var player = Main.player[i];
            
            // 检查玩家是否有效
            if (player.active && player.statLifeMax > 0)
            {
                var distance = Vector2.Distance(position, player.Center);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPlayer = player;
                }
            }
        }
        
        return nearestPlayer;
    }
    
    /// <summary>
    /// 查找最近的目标（根据目标类型）
    /// </summary>
    /// <param name="position">当前位置</param>
    /// <param name="range">搜索范围</param>
    /// <param name="targetType">目标类型</param>
    /// <returns>找到的目标中心位置，如果没有找到则返回null</returns>
    private static Vector2? FindNearestTarget(Vector2 position, float range, TrackingTargetType targetType)
    {
        Vector2? nearestTarget = null;
        float nearestDistance = range;
        
        // 查找怪物
        if (targetType == TrackingTargetType.Monsters || targetType == TrackingTargetType.All)
        {
            var enemy = FindNearestEnemy(position, range);
            if (enemy != null)
            {
                nearestTarget = enemy.Center;
                nearestDistance = Vector2.Distance(position, enemy.Center);
            }
        }
        
        // 查找玩家
        if (targetType == TrackingTargetType.Players || targetType == TrackingTargetType.All)
        {
            var player = FindNearestPlayer(position, range);
            if (player != null)
            {
                float distance = Vector2.Distance(position, player.Center);
                if (distance < nearestDistance)
                {
                    nearestTarget = player.Center;
                    nearestDistance = distance;
                }
            }
        }
        
        return nearestTarget;
    }
}