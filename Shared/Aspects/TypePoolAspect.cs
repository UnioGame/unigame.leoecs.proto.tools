namespace UniGame.LeoEcs.Bootstrap.Runtime.Aspects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Attributes;
    using Core.Runtime.SerializableType;
    using Leopotam.EcsProto;
    using Leopotam.EcsProto.QoL;
    using UniGame.Runtime.Utils;
    using UnityEngine;

#if ENABLE_IL2CPP
    using Unity.IL2CPP.CompilerServices;

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [Serializable]
    [ECSDI]
    public abstract class TypePoolAspect : ProtoAspectInject
    {
        public List<SType> componentTypes = new();

        public override void Init(ProtoWorld world)
        {
            var thisType = this.GetType();
            if (world.HasAspect(thisType))
                return;

            for (int i = 0; i < componentTypes.Count(); i++)
            {
                var stype = componentTypes[i];
                
                Type poolType = stype;
                if (poolType == null)
                {
                    Debug.LogError($"TypePoolAspect {thisType.Name}: component type with name {stype.fullTypeName}");
                    continue;
                }
                
                if(world.HasPool(poolType))continue;
                
                var pool = poolType.CreateWithDefaultConstructor() as IProtoPool;
                if(pool == null) continue;
                world.AddPool(pool);
            }
            
            foreach (Type poolType in componentTypes)
            {

            }
            
            base.Init(world);
        }
    }
}