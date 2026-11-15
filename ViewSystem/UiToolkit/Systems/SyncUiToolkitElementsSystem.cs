namespace UniGame.LeoEcs.ViewSystem.UiToolkit.Systems
{
    using System;
    using Aspects;
    using Components;
    using Bootstrap.Runtime.Attributes;
    using Leopotam.EcsProto;
    using Leopotam.EcsProto.QoL;
    using Shared.Extensions;
    using UiSystem.Runtime.UiToolkit;
    using ViewSystem.Extensions;

    /// <summary>
    /// Registers UI Toolkit elements of every view as entities: the whole tree when the root appears,
    /// then single elements added to or removed from the tree while the view is open.
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

        private ProtoIt _registryFilter = It
            .Chain<UiToolkitElementsRegistryComponent>()
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

            ReleaseDetachedElements();
            RegisterAttachedElements();
        }

        private void ReleaseDetachedElements()
        {
            var detached = UiToolkitElementsTracker.Detached;

            foreach (var element in detached)
            {
                // re-attached in the same frame: keeps its entity
                if (element.panel != null)
                    continue;

                foreach (var viewEntity in _registryFilter)
                {
                    var registry = _viewAspect.UiToolkitElementsRegistry.Get(viewEntity).Value;
                    if (registry != null && registry.Release(_world, viewEntity, element))
                        break;
                }
            }

            detached.Clear();
        }

        // Elements of a view whose registry is not built yet are skipped: Rebuild picks them up with the whole tree.
        private void RegisterAttachedElements()
        {
            var attached = UiToolkitElementsTracker.Attached;

            foreach (var element in attached)
            {
                if (element.panel == null || element.Entity.Unpack(_world, out _))
                    continue;

                foreach (var viewEntity in _registryFilter)
                {
                    var registry = _viewAspect.UiToolkitElementsRegistry.Get(viewEntity).Value;
                    if (registry?.Root == null || !registry.Root.Contains(element))
                        continue;

                    registry.Register(_world, viewEntity, element);
                    break;
                }
            }

            attached.Clear();
        }
    }
}