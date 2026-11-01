namespace UniGame.LeoEcs.ViewSystem.Aspects
{
    using System;
    using Components;
    using Converter.Runtime.Components;
    using Bootstrap;
    using Game.Modules.UnioModules.UniGame.LeoEcsLite.LeoEcs.ViewSystem.Components;
    using Game.Modules.UnioModules.UniGame.LeoEcsLite.LeoEcs.ViewSystem.Components.Events;
    using Leopotam.EcsProto;
    using Shared.Components;
    using UiToolkit.Components;

    /// <summary>
    /// View Aspect
    /// </summary>
#if ENABLE_IL2CPP
    using Unity.IL2CPP.CompilerServices;

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
#endif
    [Serializable]
    public class ViewAspect : EcsAspect
    {
        public ProtoPool<ViewComponent> View;
        public ProtoPool<ViewIdComponent> Id;
        public ProtoPool<ViewModelComponent> Model;
        public ProtoPool<ViewEntityLifeTimeComponent> LifeTime;
        public ProtoPool<ViewInitializedComponent> Initialized;
        
        public ProtoPool<ViewStatusComponent> Status;
        public ProtoPool<ViewOrderComponent> Order;
        
        public ProtoPool<TransformComponent> Transform;
        public ProtoPool<TransformPositionComponent> Position;
        
        public ProtoPool<ViewContainerComponent> ContainerView;
        
        // UiToolkit
        public ProtoPool<UiToolkitElementComponent> UiToolkitElement;
        public ProtoPool<UiToolkitElementsRegistryComponent> UiToolkitElementsRegistry;
        
        //events
        public ProtoPool<ViewStatusSelfEvent> StatusChanged;
        
        //requests
        public ProtoPool<CreateViewRequest> CreateView;
        public ProtoPool<UpdateViewRequest> UpdateView;
        public ProtoPool<CreateLayoutViewRequest> CreateLayoutView;
        public ProtoPool<CloseViewSelfRequest> CloseView;
        public ProtoPool<ShowQueuedRequest> ShowQueued;
        public ProtoPool<CreateViewInContainerRequest> CreateInContainer;
    }
}