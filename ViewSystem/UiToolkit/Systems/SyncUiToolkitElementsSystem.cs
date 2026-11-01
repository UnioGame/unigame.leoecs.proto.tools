namespace UniGame.LeoEcs.ViewSystem.UiToolkit.Systems
{
    using System;
    using Aspects;
    using Bootstrap.Runtime.Attributes;
    using Leopotam.EcsProto;
    using Leopotam.EcsProto.QoL;
    using Shared.Extensions;
    using UiSystem.Runtime.UiToolkit;
    using ViewSystem.Extensions;

    /// <summary>
    /// 
    /// </summary>
#if ENABLE_IL2CPP
    using Unity.IL2CPP.CompilerServices;

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
#endif
    [Serializable]
    [ECSDI]
    public class SyncUiToolkitElementsSystem : IProtoRunSystem
    {
        private ProtoWorld _world;
        private ViewAspect _viewAspect;

        private ProtoIt _viewFilter = ViewIt
            .ViewChain()
            .End();

        public void Run()
        {
            foreach (var viewEntity  in _viewFilter)
            {
                ref var viewComponent = ref _viewAspect.View.Get(viewEntity);
                
                if (viewComponent.View is not IUiToolkitView toolkitView)
                    continue;

                var root = toolkitView.Root;
                if (root == null) continue;

                ref var registryComponent = ref _viewAspect.UiToolkitElementsRegistry.GetOrAddComponent(viewEntity);
                registryComponent.Value ??= new UiToolkitElementsRegistry();
                
                if (ReferenceEquals(registryComponent.Value.Root, root)) continue;

                registryComponent.Value.Rebuild(_world, viewEntity, root);
            }
        }
    }
}