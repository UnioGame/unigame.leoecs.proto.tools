namespace Game.Ecs.Core.Death.Aspects
{
    using System;
    using Components;
    using Leopotam.EcsProto;
    using Leopotam.EcsProto.QoL;
    using UniGame.LeoEcs.Bootstrap;
    using UniGame.LeoEcs.Bootstrap.Runtime.Attributes;
    using UnityEngine.Scripting;
    /// <summary>
    /// destroy entity aspect
    /// </summary>
#if ENABLE_IL2CPP
    using Unity.IL2CPP.CompilerServices;

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
#endif
    [Serializable]
    [ECSDI]
    [Preserve]
    public class DestroyAspect : EcsAspect
    {
        public ProtoPool<DestroyComponent> Destroy;
        public ProtoPool<DisabledComponent> Disabled;
        public ProtoPool<DontKillComponent> DontKill;
        public ProtoPool<CanDisableComponent> CanDisable;
        public ProtoPool<PoolingComponent> Pooling;
        
        //requests
        public ProtoPool<ValidateDeadChildEntitiesRequest> ValidateDeadChild;
        public ProtoPool<DestroySelfRequest> DestroySelf;
        public ProtoPool<KillSelfRequest> Kill;
        
        //events
        public ProtoPool<DeadEvent> DeadEvent;
        public ProtoPool<DisabledEvent> DisabledEvent;
        public ProtoPool<KillEvent> KillEvent;

        public void BlockDeath(ProtoPackedEntity entity)
        {
            entity.TryUnpack(world, out var unpackedEntity);

            if (!DontKill.Has(unpackedEntity))
                DontKill.Add(unpackedEntity);
            
            ref var dontKillComponent = ref DontKill.Get(unpackedEntity);
            
            dontKillComponent.blockers++;
        }

        public void UnblockDeath(ProtoPackedEntity entity)
        {
            entity.TryUnpack(world, out var unpackedEntity);
            
            ref var dontKillComponent = ref DontKill.Get(unpackedEntity);
            
            dontKillComponent.blockers--;
        }
    }
}