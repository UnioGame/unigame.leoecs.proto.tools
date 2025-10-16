using System;
using Leopotam.EcsLite;
using UniGame.LeoEcs.Shared.Extensions;
using UniGame.LeoEcs.ViewSystem.Components;
using UniModules.UniGame.UiSystem.Runtime;
using UnityEngine;

namespace UniGame.LeoEcs.ViewSystem.Extensions
{
    using System.Runtime.CompilerServices;
    using Leopotam.EcsProto;
    using Leopotam.EcsProto.QoL;
    using Proto.Components;
    using UniGame.ViewSystem.Runtime;
    using Runtime.Utils;
    using Shared.Components;

#if ENABLE_IL2CPP
    using Unity.IL2CPP.CompilerServices;

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
#endif
    public static class ViewIt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProtoItChain ViewChain()
        {
            return It.Chain<ViewModelComponent>()
                .Inc<ViewComponent>()
                .Inc<TypeIdComponent>()
                .Inc<ViewInitializedComponent>();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProtoItChain ViewChain<TModel>()
            where TModel : IViewModel
        {
            return ViewChain()
                .Inc<ViewComponent<TModel>>();
        }
    }
    
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
#endif
    public static class EcsViewExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProtoItChain ViewFilter<TModel>(this ProtoWorld world)
            where TModel : IViewModel
        {
            return It.Chain<ViewModelComponent>()
                .Inc<ViewComponent>()
                .Inc<ViewComponent<TModel>>()
                .Inc<ViewInitializedComponent>();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProtoItChain ViewChain<TModel>(this ProtoItChain chain)
            where TModel : IViewModel
        {
            return chain
                .Inc<ViewModelComponent>()
                .Inc<ViewComponent<TModel>>()
                .Inc<ViewComponent>()
                .Inc<ViewInitializedComponent>();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProtoItChain.ProtoItChainExc ViewChain<TModel>(this ProtoItChain.ProtoItChainExc chain)
            where TModel : IViewModel
        {
            return chain.Inc<ViewModelComponent>()
                .Inc<ViewComponent>() 
                .Inc<ViewComponent<TModel>>()
                .Inc<ViewInitializedComponent>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetViewModel<TModel>(this ProtoWorld world, int entity, out TModel model)
        {
            model = default;
            if (!world.HasComponent<ViewModelComponent>(entity))
                return false;

            ref var viewModelComponent = ref world.GetComponent<ViewModelComponent>(entity);
            if (viewModelComponent.Model is not TModel viewModel) return false;

            model = viewModel;
            return true;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetViewModel<TModel>(this ProtoWorld world, ProtoEntity entity, out TModel model)
        {
            model = default;
            if (!world.HasComponent<ViewModelComponent>(entity))
                return false;

            ref var viewModelComponent = ref world.GetComponent<ViewModelComponent>(entity);
            if (viewModelComponent.Model is not TModel viewModel) return false;

            model = viewModel;
            return true;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TModel GetViewModel<TModel>(this ProtoWorld world, ProtoEntity entity) 
            where TModel : class
        {
            ref var viewModelComponent = ref world.GetComponent<ViewModelComponent>(entity);
            return viewModelComponent.Model as TModel;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TModel GetViewModel<TModel>(this ProtoWorld world, int entity) 
            where TModel : class
        {
            return GetViewModel<TModel>(world, (ProtoEntity)entity);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProtoIt CreateViewFilter<TModel>(this ProtoWorld world)
            where TModel : IViewModel
        {
            return world.ViewFilter<TModel>().End();
        }
        
        public static ProtoEntity MakeViewRequest(
            this ProtoWorld world,
            Type viewType,
            ViewType layoutType = ViewType.None,
            Transform parent = null,
            string tag = null,
            string viewName = null,
            bool stayWorld = false)
        {
            var target = new ProtoPackedEntity();
            return MakeViewRequest(world, viewType.Name,ref target,ref target, layoutType, 
                parent, tag, viewName, stayWorld);
        }

        public static ProtoEntity MakeViewRequest<TView>(
            this ProtoWorld world,
            ProtoPackedEntity target,
            ViewType layoutType = ViewType.None,
            Transform parent = null,
            string tag = null,
            string viewName = null,
            bool stayWorld = false)
        {
            return MakeViewRequest(world, typeof(TView),ref target,ref target, layoutType,
                parent, tag, viewName, stayWorld);
        }
        
        public static ProtoEntity MakeViewRequest(
            this ProtoWorld world,
            Type viewType,
            ref  ProtoPackedEntity targetEntity,
            ref ProtoPackedEntity ownerEntity,
            ViewType layoutType = ViewType.None,
            Transform parent = null,
            string tag = null,
            string viewName = null,
            bool stayWorld = false)
        {
            return MakeViewRequest(world, viewType.Name,ref targetEntity,ref ownerEntity, layoutType, 
                parent, tag, viewName, stayWorld);
        }
        
        public static ProtoEntity MakeViewInContainerRequest<TView>(
            this ProtoWorld world, 
            bool useBusyContainer = false,
            ProtoPackedEntity owner = default,
            string tag = null,
            string viewName = null,
            bool stayWorld = false)
        {
            return world.MakeViewInContainerRequest(typeof(TView).Name, useBusyContainer, owner, tag, viewName, stayWorld);
        }

        public static ProtoEntity MakeViewInContainerRequest<TView>(
            this ProtoWorld world,
            bool useBusyContainer = false,
            ProtoPackedEntity target = default,
            ProtoPackedEntity owner = default,
            string tag = null,
            string viewName = null,
            bool stayWorld = false)
        {
            return world.MakeViewInContainerRequest(typeof(TView).Name, useBusyContainer, target, owner, tag, viewName, stayWorld);
        }

        public static ProtoEntity MakeViewInContainerRequest(
            this ProtoWorld world,
            string view,
            bool useBusyContainer = false,
            ProtoPackedEntity target = default,
            ProtoPackedEntity owner = default,
            string tag = null,
            string viewName = null,
            bool stayWorld = false)
        {
            target.Unpack(world, out var entity);

            ref var component = ref world
                .GetOrAddComponent<CreateViewInContainerRequest>(entity);

            component.Tag = tag;
            component.View = view;
            component.ViewName = viewName;
            component.StayWorld = stayWorld;
            component.UseBusyContainer = useBusyContainer;
            component.Owner = owner;

            return entity;
        }
        
        public static ProtoEntity MakeViewInContainerRequest(
            this ProtoWorld world, 
            string view,
            bool useBusyContainer = false,
            ProtoPackedEntity owner = default,
            string tag = null,
            string viewName = null,
            bool stayWorld = false)
        {
            var entity = world.NewEntity();
            
            ref var component = ref world
                .GetOrAddComponent<CreateViewInContainerRequest>(entity);
            
            component.Tag = tag;
            component.View = view;
            component.ViewName = viewName;
            component.StayWorld = stayWorld;
            component.UseBusyContainer = useBusyContainer;
            component.Owner = owner;

            return entity;
        }
        
        public static ProtoEntity MakeViewRequest<TView>(
            this ProtoWorld world, 
            ViewType layoutType)
        {
            return MakeViewRequest(world,typeof(TView),layoutType);
        }
        
        public static ProtoEntity MakeViewRequest(
            this ProtoWorld world, 
            string viewId,
            ViewType layoutType)
        {
            return MakeViewRequest(world, viewId, layoutType.ToStringFromCache());
        }
        
        public static ProtoEntity MakeViewRequest(
            this ProtoWorld world, 
            string viewId,
            string layoutType)
        {
            var entity = world.NewEntity();
            
            ref var component = ref world
                .GetOrAddComponent<CreateLayoutViewRequest>(entity);
            
            component.View = viewId;
            component.LayoutType = layoutType;

            return entity;
        }
        
        public static ProtoEntity MakeViewRequest(
            this ProtoWorld world, 
            Type viewType,
            ViewType layoutType)
        {
            return MakeViewRequest(world,viewType.Name, layoutType);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProtoEntity MakeViewRequest(
            this ProtoWorld world, 
            string view,
            ref ProtoPackedEntity target,
            ref ProtoPackedEntity owner,
            ViewType layoutType = ViewType.None,
            Transform parent = null,
            string tag = null,
            string viewName = null,
            bool stayWorld = false)
        {
            var layout = layoutType == ViewType.None ? string.Empty : layoutType.ToStringFromCache();
            return MakeViewRequest(world,view,ref target,ref owner, layout, parent, tag, viewName, stayWorld);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProtoEntity MakeViewRequest(
            this ProtoWorld world, 
            string view,
            ref ProtoPackedEntity target,
            ref ProtoPackedEntity owner,
            string layoutType,
            Transform parent = null,
            string tag = null,
            string viewName = null,
            bool stayWorld = false)
        {
            var entity = world.NewEntity();
            ref var component = ref world
                .GetOrAddComponent<CreateViewRequest>(entity);
            
            component.Parent = parent;
            component.Tag = tag;
            component.ViewId = view;
            component.LayoutType = layoutType;
            component.ViewName = viewName;
            component.StayWorld = stayWorld;
            component.Target = target;
            component.Owner = owner;

            return entity;
        }
        
        public static ProtoEntity MakeViewRequest(this ProtoWorld world,ref CreateViewRequest request)
        {
            var entity = world.NewEntity();
            ref var component = ref world.GetOrAddComponent<CreateViewRequest>(entity);
            component.Apply(ref request);

            return entity;
        }
        
        public static CreateViewRequest CreateViewRequest(
            string view,
            ViewType layoutType = ViewType.None,
            Transform parent = null,
            string tag = null,
            string viewName = null,
            bool stayWorld = false)
        {
            var layout = layoutType == ViewType.None ? string.Empty : layoutType.ToStringFromCache();
            return CreateViewRequest(view, layout, parent, tag, viewName, stayWorld);
        }
        
        public static CreateViewRequest CreateViewRequest(
            string view,
            string layoutType,
            Transform parent = null,
            string tag = null,
            string viewName = null,
            bool stayWorld = false)
        {
            var component = new CreateViewRequest
            {
                Parent = parent,
                Tag = tag,
                ViewId = view,
                LayoutType = layoutType,
                ViewName = viewName,
                StayWorld = stayWorld
            };

            return component;
        }
        
    }
}
