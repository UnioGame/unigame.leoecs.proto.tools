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

        internal ProtoEntity Register(ProtoWorld world, ProtoEntity rootEntity, EcsUiToolkitElement element)
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
        
        // Frees the entity of an element removed from the tree while its view stays open.
        internal bool Release(ProtoWorld world, ProtoEntity rootEntity, EcsUiToolkitElement element)
        {
            for (var i = 0; i < _entities.Count; i++)
            {
                if (_entities[i] != element.Entity)
                    continue;

                ReleaseEntity(world, rootEntity, _entities[i]);
                _entities.RemoveAt(i);
                return true;
            }

            return false;
        }

        private void ReleaseElements(ProtoWorld world, ProtoEntity rootEntity)
        {
            for (var i = _entities.Count - 1; i >= 0; i--)
                ReleaseEntity(world, rootEntity, _entities[i]);

            _entities.Clear();
            Root = null;
        }

        private static void ReleaseEntity(ProtoWorld world, ProtoEntity rootEntity, ProtoPackedEntity packedEntity)
        {
            if (!packedEntity.Unpack(world, out var entity))
                return;

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