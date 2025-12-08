using GameFramework.ECS.Components;
using GameFramework.Managers;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace GameFramework.ECS.Systems
{
    // 移动系统
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial struct MoveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (transform, move) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveComponent>>())
            {
                if (math.lengthsq(move.ValueRO.Direction) > 0.001f)
                {
                    float3 movement = math.normalize(move.ValueRO.Direction)
                        * move.ValueRO.Speed * deltaTime;
                    transform.ValueRW.Position += movement;
                }
            }
        }
    }

    // ======================================================================================
    // 1. 输入同步系统 (主线程运行)
    // 职责：仅负责从 InputManager 搬运数据到 ECS World，不做任何游戏逻辑。
    // ======================================================================================
    [UpdateInGroup(typeof(InitializationSystemGroup))] // 在游戏逻辑开始前尽早更新
    public partial class InputSyncSystem : SystemBase
    {
        protected override void OnCreate()
        {
            // 确保 World 中有一个 GlobalInputComponent 单例
            // 这样我们在 Burst 系统中就可以随时读取它
            EntityManager.CreateSingleton<GlobalInputComponent>();
        }

        protected override void OnUpdate()
        {
            // 这里的 InputManager.Instance 是外部单例，必须在主线程访问
            var inputData = InputManager.Instance.GetInputData();

            // 将数据写入 ECS 单例
            SystemAPI.SetSingleton(new GlobalInputComponent
            {
                Move = inputData.Move,
                Fire = inputData.Fire,
                Jump = inputData.Jump
            });
        }
    }

    // ======================================================================================
    // 2. 玩家输入处理系统 (Burst 编译，高性能)
    // 职责：读取全局输入，并行处理所有玩家实体的状态。
    // ======================================================================================
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateBefore(typeof(MoveSystem))] // 确保在移动计算之前应用输入
    public partial struct PlayerInputSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 获取全局输入 (这是值拷贝，非常快)
            var globalInput = SystemAPI.GetSingleton<GlobalInputComponent>();

            // 使用 Job 方式并行处理所有带 PlayerTag 的实体
            // 这在移动端上有极高的性能优势
            new ApplyInputJob
            {
                Input = globalInput
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct ApplyInputJob : IJobEntity
        {
            public GlobalInputComponent Input;

            // 只需要读写 MoveComponent 和 InputComponent，并且只针对 PlayerTag
            void Execute(ref MoveComponent move, ref InputComponent inputComponent, in PlayerTag tag)
            {
                // 1. 更新组件状态
                inputComponent.Move = Input.Move;
                inputComponent.Fire = Input.Fire;
                inputComponent.Jump = Input.Jump;

                // 2. 将输入转换为移动方向 (逻辑部分)
                move.Direction = new float3(Input.Move.x, 0, Input.Move.y);
            }
        }
    }

    // ======================================================================================
    // 3. 伤害处理系统 (优化版 - 使用 ECB 单例)
    // ======================================================================================
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial struct DamageSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 关键修改：获取系统提供的 EndSimulation ECB 单例
            // 这允许我们在所有计算完成后，统一在帧末尾应用变更
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // 使用 ScheduleParallel 并行处理所有伤害逻辑
            new ApplyDamageJob
            {
                Ecb = ecb.AsParallelWriter() // 并行写入器
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct ApplyDamageJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;

            // 这里我们需要 Entity ID 来记录命令 (entityInQueryIndex 用于多线程排序)
            void Execute(Entity entity, [EntityIndexInQuery] int sortKey, ref HealthComponent health, in DamageComponent damage)
            {
                health.Current -= damage.Amount;

                // 移除伤害组件 (不需要立即生效，帧末尾统一移除)
                Ecb.RemoveComponent<DamageComponent>(sortKey, entity);

                // 死亡判定
                if (health.IsDead)
                {
                    Ecb.AddComponent<DestroyTag>(sortKey, entity);
                }
            }
        }
    }

    // ======================================================================================
    // 4. 销毁系统 (优化版 - 使用 ECB 单例)
    // ======================================================================================
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(DamageSystem))]
    public partial struct DestroySystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            new DestroyEntityJob
            {
                Ecb = ecb.AsParallelWriter()
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct DestroyEntityJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;

            void Execute(Entity entity, [EntityIndexInQuery] int sortKey, in DestroyTag tag)
            {
                Ecb.DestroyEntity(sortKey, entity);
            }
        }
    }

    // AI系统示例
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial class EnemyAISystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var playerQuery = GetEntityQuery(typeof(PlayerTag), typeof(LocalTransform));

            if (playerQuery.CalculateEntityCount() == 0)
                return;

            var playerTransform = playerQuery.GetSingleton<LocalTransform>();
            var playerPos = playerTransform.Position;
            var currentTime = (float)SystemAPI.Time.ElapsedTime;

            Entities
                .WithAll<EnemyTag>()
                .ForEach((ref MoveComponent move,
                         ref AttackComponent attack,
                         in LocalTransform transform) =>
                {
                    float3 direction = playerPos - transform.Position;
                    float distance = math.length(direction);

                    if (distance > attack.Range)
                    {
                        // 移动向玩家
                        move.Direction = direction / distance;
                    }
                    else
                    {
                        // 在攻击范围内,停止移动
                        move.Direction = float3.zero;

                        // 尝试攻击
                        if (currentTime - attack.LastAttackTime >= attack.Cooldown)
                        {
                            attack.LastAttackTime = currentTime;
                            // 触发攻击事件或添加伤害组件到玩家
                        }
                    }
                }).Run();
        }
    }
}
