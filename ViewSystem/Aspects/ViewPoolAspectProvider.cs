namespace UniGame.LeoEcs.ViewSystem.Aspects
{
    using System;
    using System.Collections.Generic;
    using Bootstrap;
    using Bootstrap.Runtime.Aspects;
    using Components;
    using Core.Runtime.SerializableType;
    using Leopotam.EcsProto;

#if UNITY_EDITOR
    using UniGame.ViewSystem.Runtime;
    using UnityEditor;
#endif
    
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif

    [Serializable]
    public class ViewPoolAspectProvider : IProtoAspectFactory
    {
        public static readonly Type ViewComponentType = typeof(ViewComponent<>);

        public static readonly Type UiToolkitElementComponentType =
            typeof(UiToolkit.Components.UiToolkitElementComponent<>);
        
#if ODIN_INSPECTOR
        [Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
#endif
        public List<SType> poolTypes = new();
        
        public IProtoAspect Create()
        {
            var aspect = new ViewPoolAspect
            {
                componentTypes = poolTypes
            };
            return aspect;
        }

#if UNITY_EDITOR
        public void EditorInitialize()
        {
            poolTypes.Clear();
            
            var modelTypes = typeof(IViewModel);
            var poolType = typeof(ProtoPool<>);
            
            var modelTypesList = TypeCache.GetTypesDerivedFrom(modelTypes);
            
            foreach (var modelType in modelTypesList)
            {
                if(modelType.IsInterface || modelType.IsAbstract) continue;
                var genericType = ViewComponentType.MakeGenericType(modelType);
                var poolGenericType = poolType.MakeGenericType(genericType);
                poolTypes.Add(poolGenericType);
            }
            
            var elementBaseType = typeof(UiToolkit.EcsUiToolkitElement);
            var elementTypes = TypeCache.GetTypesDerivedFrom(elementBaseType);

            foreach (var elementType in elementTypes)
            {
                if (elementType.IsInterface || elementType.IsAbstract)
                    continue;

                var componentType =
                    UiToolkitElementComponentType.MakeGenericType(elementType);

                var poolGenericType =
                    poolType.MakeGenericType(componentType);

                poolTypes.Add(poolGenericType);
            }
        }
#endif
        
    }
    
    [Serializable]
    public class ViewPoolAspect : TypePoolAspect
    {
        
    }
}