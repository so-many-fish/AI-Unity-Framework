using Unity.Entities;
using Unity.Mathematics;

namespace GameFramework.ECS.Components
{
    // 移动组件
    public struct MoveComponent : IComponentData
    {
        public float Speed;
        public float3 Direction;
    }

    // 生命值组件
    public struct HealthComponent : IComponentData
    {
        public float Current;
        public float Max;

        public float HealthPercentage => Max > 0 ? Current / Max : 0;
        public bool IsDead => Current <= 0;
    }

    // 伤害组件(临时组件)
    public struct DamageComponent : IComponentData
    {
        public float Amount;
        public Entity Source;
    }

    // 玩家标签
    public struct PlayerTag : IComponentData { }

    // 敌人标签
    public struct EnemyTag : IComponentData { }

    // 可销毁标签
    public struct DestroyTag : IComponentData { }

    // 位置组件(如果不使用Transform)
    public struct PositionComponent : IComponentData
    {
        public float3 Value;
    }

    // 旋转组件
    public struct RotationComponent : IComponentData
    {
        public quaternion Value;
    }

    // 输入组件
    public struct InputComponent : IComponentData
    {
        public float2 Move;
        public bool Fire;
        public bool Jump;
    }

    // 攻击组件
    public struct AttackComponent : IComponentData
    {
        public float Damage;
        public float Range;
        public float Cooldown;
        public float LastAttackTime;
    }

    // 经验值组件
    public struct ExperienceComponent : IComponentData
    {
        public int Level;
        public int CurrentXP;
        public int RequiredXP;
    }

    // 这是一个特殊的组件，整个世界只存一份，用于充当 InputManager 和 ECS 之间的桥梁
    public struct GlobalInputComponent : IComponentData
    {
        public float2 Move;
        public bool Fire;
        public bool Jump;
    }
}
