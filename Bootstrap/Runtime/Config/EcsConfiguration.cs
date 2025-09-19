namespace UniGame.Ecs.Bootstrap.Runtime.Config
{
    using System.Collections.Generic;
    using System.Linq;
    using Game.Ecs.Core;
    using LeoEcs.Bootstrap;
    using LeoEcs.Bootstrap.Runtime.PostInitialize;
    using Leopotam.EcsProto;
    using UniGame.Runtime.Utils;
    using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
    using UniModules.Editor;
#endif
    
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif

#if TRI_INSPECTOR
    using TriInspector;
#endif
    
    [CreateAssetMenu(menuName = "ECS Proto/ECS Features Configuration", fileName = nameof(EcsConfiguration))]
    public class EcsConfiguration : ScriptableObject, IEcsSystemsConfig
    {
        [Header("Worlds Configuration")]
#if ODIN_INSPECTOR
       [FoldoutGroup("world config")]
#endif
#if ODIN_INSPECTOR || TRI_INSPECTOR
        [InlineProperty]
        [HideLabel]
#endif
        public WorldConfiguration worldConfiguration = new();
        
        [Tooltip("If true, enable ECS Proto Unity module")]
        public bool enableUnityModule = true;
        
        [Tooltip("if true, use features loading log in the console")]
        public bool useFeaturesLoadingLog = true;
        
        [Space(8)]
        [SerializeField]
#if ODIN_INSPECTOR
        [ListDrawerSettings(ListElementLabelName = "updateType")]
#endif
        //[Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
        public List<EcsConfigGroup> ecsUpdateGroups = new()
        {
            new()
            {
                updateType = EcsUpdateMap.UpdateQueueId,
                features = new List<EcsFeatureData>()
                {
                    new()
                    {
                        featureGroup = new CoreFeature(),
                    }
                }
            }
        };
        
#if ODIN_INSPECTOR || TRI_INSPECTOR
        [InlineProperty]
        [HideLabel]
#endif  
        [Space(10)]
        public AspectsData aspectsData = new();
        
        [Space(10)]
        public EcsUpdateMap ecsUpdateMap = new();

        [Header("ECS Plugins")]
#if ODIN_INSPECTOR 
        [ListDrawerSettings(ListElementLabelName = "@name")]
#endif
        [Tooltip("Ecs Plugins that provide additional functionality for the ECS framework, such as dependency injection,etc.")]
        public List<EcsPlugin> plugins = new()
        {
            new EcsPlugin()
            {
                enabled = true,
                name = nameof(EcsDiPlugin),
                plugin = new EcsDiPlugin()
            },
        };
                
#if ODIN_INSPECTOR || TRI_INSPECTOR
        [InlineProperty]
#endif
        [Space(8)]
        [Tooltip("Plugins that provide additional functionality for Systems, such as logging, profiling, etc.")]
        [SerializeReference]
        public List<IEcsSystemsPluginProvider> systemsPlugins = new();



        public bool EnableUnityModules => enableUnityModule;
        public WorldConfiguration WorldConfiguration => worldConfiguration;

        public AspectsData AspectsData => aspectsData;

        public bool UseFeaturesLoadingLog => useFeaturesLoadingLog;

        public IReadOnlyList<EcsPlugin> Plugins => plugins;

        public IReadOnlyList<EcsConfigGroup> FeatureGroups => ecsUpdateGroups;

        public IReadOnlyList<IEcsSystemsPluginProvider> SystemsPlugins => systemsPlugins;

#if ODIN_INSPECTOR || TRI_INSPECTOR
        [Button]
#endif
        public void CollectAspects()
        {
#if UNITY_EDITOR
            var aspectsCollection = aspectsData.aspects;
            aspectsCollection.Clear();
            
            var aspectItems = TypeCache.GetTypesDerivedFrom(typeof(IProtoAspect));
            foreach (var aspectType in aspectItems)
            {
                if(aspectType.IsAbstract || aspectType.IsInterface) continue;
                if(!aspectType.HasDefaultConstructor()) continue;
                
                aspectsCollection.Add(new AspectData()
                {
                    enabled = true,
                    name = aspectType.Name,
                    aspectType = aspectType
                });
            }

            aspectsData.factories.Clear();
            var aspectsFactories = TypeCache.GetTypesDerivedFrom(typeof(IProtoAspectFactory));
            foreach (var aspectsFactory in aspectsFactories)
            {
                if(aspectsFactory.IsAbstract || aspectsFactory.IsInterface) continue;
                if(!aspectsFactory.HasDefaultConstructor()) continue;
                
                var factory = aspectsFactory.CreateWithDefaultConstructor() as IProtoAspectFactory;
                if(factory == null) continue;
                
                factory.EditorInitialize();
                aspectsData.factories.Add(factory);
            }
            
            this.MarkDirty();

#endif
        }

#if ODIN_INSPECTOR || TRI_INSPECTOR
        [Button]
#endif
        public void AddAspectsIntoLinkFile()
        {
#if UNITY_EDITOR
            var aspects = aspectsData.aspects;
            var types = aspects
                .Select(x => x.aspectType.Type)
                .Distinct();
            EcsLinkXmlGenerator.GenerateLinkXml(types);
#endif
        }
        
        
#if ODIN_INSPECTOR || TRI_INSPECTOR
        [Button]
#endif
        public void ResetPlugins()
        {
            plugins = new List<EcsPlugin>()
            {
                new()
                {
                    enabled = true,
                    name = nameof(EcsDiPlugin),
                    plugin = new EcsDiPlugin()
                },
            };
        }

#if UNITY_EDITOR

        [InitializeOnLoadMethod]
        public static void ReloadEcsConfiguration()
        {
            var assets = AssetEditorTools.GetAssets<EcsConfiguration>();
            foreach (var asset in assets)
            {
                if(!asset.aspectsData.autoRegisterAspects)
                    continue;
                
                asset.CollectAspects();
            }
        }
        
#endif
        
    }
}