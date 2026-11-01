namespace UniGame.LeoEcs.ViewSystem.UiToolkit
{
    using System.Collections.Generic;
    using Components;
    using Converter.Runtime;
    using Leopotam.EcsProto;
    using Leopotam.EcsProto.QoL;
    using Shared.Extensions;
    using UniGame.Proto.Ownership;
    using UnityEngine.UIElements;

    public sealed class UiToolkitElementsRegistry
    {
        private readonly List<ProtoPackedEntity> _entities = new();

        public VisualElement Root { get; private set; }

        public void Rebuild(ProtoWorld world, ProtoEntity rootEntity, VisualElement root)
        {
            ReleaseElements(world, rootEntity);
            
            Root = root;

            root.Query<EcsUiToolkitElement>()
                .ForEach(element => Register(world, rootEntity, element));
        }

        private ProtoEntity Register(ProtoWorld world, ProtoEntity rootEntity, EcsUiToolkitElement element)
        {
            if (element.Entity.Unpack(world, out var existingEntity))
                return existingEntity;

            var entity = world.NewEntity();

            ref var uiToolkitElementComponent = ref world.GetOrAddComponent<UiToolkitElementComponent>(entity);
            uiToolkitElementComponent.Element = element;
            
            element.AddTypeComponent(world, entity);

            foreach (var converter in element.Converters)
            {
                if (converter == null || !converter.IsEnabled)
                    continue;

                converter.Apply(world, entity);
            }
            
            rootEntity.AddChild(entity, world);

            element.Entity = entity.PackEntity(world);
            _entities.Add(element.Entity);

            return entity;
        }
        
        private void ReleaseElements(ProtoWorld world, ProtoEntity rootEntity)
        {
            for (var i = _entities.Count - 1; i >= 0; i--)
            {
                var packedEntity = _entities[i];

                if (!packedEntity.Unpack(world, out var entity))
                    continue;

                if (world.HasComponent<UiToolkitElementComponent>(entity))
                {
                    ref var component =
                        ref world.GetComponent<UiToolkitElementComponent>(entity);

                    var element = component.Element;

                    if (element != null)
                    {
                        foreach (var converter in element.Converters)
                        {
                            if (converter is { IsEnabled: true } &&
                                converter is IConverterEntityDestroyHandler handler)
                            {
                                handler.OnEntityDestroy(world, entity);
                            }
                        }

                        element.Entity = default;
                    }
                }

                RemoveOwnerLink(world, rootEntity, packedEntity);
                world.DelEntity(entity);
            }

            _entities.Clear();
            Root = null;
        }
        
        private static void RemoveOwnerLink(ProtoWorld world, ProtoEntity rootEntity, ProtoPackedEntity child)
        {
            if (!world.HasComponent<OwnerComponent>(rootEntity))
                return;

            ref var owner = ref world.GetComponent<OwnerComponent>(rootEntity);
            owner.RemoveChild(child);
        }
        
        public static void Release(ProtoWorld world, ProtoEntity rootEntity)
        {
            if (!world.HasComponent<UiToolkitElementsRegistryComponent>(rootEntity))
                return;

            ref var component =
                ref world.GetComponent<UiToolkitElementsRegistryComponent>(rootEntity);

            component.Value?.ReleaseElements(world, rootEntity);
            component.Value = null;
        }
    }
}