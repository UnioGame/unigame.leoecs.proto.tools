namespace UniGame.LeoEcs.Bootstrap.Runtime
{
    using System;
    using UniGame.Core.Runtime;
    
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using Bootstrap;
    using Aspects;
    using Converter.Runtime;
    using Ecs.Bootstrap.Runtime.Config;
    using Core.Runtime.Extension;
    using Cysharp.Threading.Tasks;
    using Leopotam.EcsLite;
    using GameFlow.Runtime;
    using Leopotam.EcsProto;
    using Proto.Shared;
    using R3;
    using Shared.Extensions;
    using UniCore.Runtime.ProfilerTools;
    using UniGame.Runtime.DataFlow;
    using UniGame.Runtime.Rx;
    using UniGame.Runtime.Utils;
    using UnityEngine;
    using Debug = UnityEngine.Debug;

    public class EcsService : GameService, IEcsService
    {
        private IEcsSystemsConfig _defaultConfig;
        private IEcsExecutorFactory _ecsExecutorFactory;
        private IEnumerable<ISystemsPlugin> _plugins;
        
        private IContext _context;
        
        private ReactiveValue<EcsWorldData> _lastWorldData = new();
        private ReactiveValue<EcsWorldData> _defaultWorldData = new();
        private Dictionary<string,EcsWorldData> _worlds = new();
        private Dictionary<string,EcsServiceWorld> _serviceWorlds = new();

        private float _featureTimeout;

        public EcsService(IContext context, 
            IEcsSystemsConfig config,
            IEcsExecutorFactory ecsExecutorFactory,
            float featureTimeout)
        {
            _context = context;
            _defaultConfig = config;
            _ecsExecutorFactory = ecsExecutorFactory;
            _featureTimeout = featureTimeout;
            
            LifeTime.AddCleanUpAction(CleanUp);
        }
        
        public ProtoWorld World => _defaultWorldData.Value.World;

        public ReadOnlyReactiveProperty<EcsWorldData> LastWorld => _lastWorldData;
        
        public ReadOnlyReactiveProperty<EcsWorldData> DefaultWorld => _defaultWorldData;

        public IReadOnlyDictionary<string,EcsWorldData> Worlds => _worlds;

        public async UniTask<EcsWorldData> CreateWorldAsync(string worldId)
        {
            return await CreateWorldAsync(worldId, _defaultConfig);
        }

        public async UniTask<ProtoWorld> GetWorldAsync(string worldId, 
            bool createIfNotExists = false, 
            CancellationToken cancellationToken = default)
        {
            var isDefault = string.IsNullOrEmpty(worldId);

            if (isDefault)
            {
                await UniTask.WaitUntil(this, x =>
                {
                    var value = x._defaultWorldData.Value;
                    return value != null &&
                        value.IsInitialized &&
                        value.World.IsAlive();
                }, cancellationToken: cancellationToken);
                
                return World;
            }
            
            if (createIfNotExists)
            {
                var world = await CreateWorldAsync(worldId, _defaultConfig);
                return world.World;
            }

            await UniTask.WaitWhile(this, x => x._worlds.ContainsKey(worldId) == false,
                cancellationToken: cancellationToken);
            
            var worldData = _worlds[worldId];

            await UniTask.WaitWhile(worldData,static x => x.IsInitialized == false,
                cancellationToken: cancellationToken);

            return worldData.World;
        }
        
        public async UniTask<EcsWorldData> CreateWorldAsync(string worldId,IEcsSystemsConfig config)
        {
            //check if world already exists
            if (_worlds.TryGetValue(worldId, out var worldData))
            {
                if (worldData.IsInitialized) return worldData;
                // wait for world to be initialized
                await UniTask.WaitWhile(worldData, static x => !x.IsInitialized);
                return worldData;
            }
            
            var configToUse = config ?? _defaultConfig;
            var protoWorld = CreateWorld(configToUse);
            var worldLifeTime = protoWorld.GetWorldLifeTime();
            
            worldData = new EcsWorldData()
            {
                Id = worldId,
                LifeTime = worldLifeTime,
                World = protoWorld,
            };

            _serviceWorlds[worldId] = new EcsServiceWorld()
            {
                Id = worldId,
                Config = configToUse,
                WorldData = worldData,
            };
            
            _worlds[worldId] = worldData;
            _lastWorldData.Value = worldData;
    
            if(string.IsNullOrEmpty(worldId))
                _defaultWorldData.Value ??= worldData;
            
            worldLifeTime.AddCleanUpAction(() =>
            {
                _worlds.Remove(worldId);
                _serviceWorlds.Remove(worldId);
                worldData.Dispose();
            });
            
            await InitializeWorldAsync(worldData,configToUse);
            
            GameLog.Log($"Ecs World [{worldId}] Initialized");
            
            worldData.IsInitialized = true;

            Execute(worldId);
            
            return worldData;
        }

        public bool Execute(string worldId = null)
        {
            if(string.IsNullOrEmpty(worldId))
                worldId = _defaultWorldData.Value.Id;
            
            if (!_serviceWorlds.TryGetValue(worldId, out var serviceWorld))
                return false;
            
            var worldData = serviceWorld.WorldData;
            var world = worldData.World;
            var executors = worldData.Executors;
            var systemsMap = worldData.SystemsMap;

            if (executors.Count > 0)
            {
                foreach (var executor in executors)
                    executor.Value.Execute(world);
                return true;
            }
            
            foreach (var (updateType, systems) in systemsMap)
            {
                if (!executors.TryGetValue(updateType, out var executor))
                {
                    executor = _ecsExecutorFactory.Create(updateType);
                    executors[updateType] = executor;
                }

                executor.Execute(world);
                executor.Add(systems);
            }

            ApplyPlugins(serviceWorld);
            return true;
        }

        public void Pause(string worldId = null)
        {
            worldId ??= _defaultWorldData.Value.Id;
            
            if (!_worlds.TryGetValue(worldId, out var worldData))
                return;
            
            var executors = worldData.Executors;
            foreach (var systemsExecutor in executors.Values)
                systemsExecutor.Stop();
        }

        public void DestroyWorld(ProtoWorld world)
        {
            if (world == World) return;
            var worldId = _worlds
                .FirstOrDefault(x => x.Value.World == world)
                .Key;
            if(string.IsNullOrEmpty(worldId)) return;
            DestroyWorld(worldId);
        }
        
        public void DestroyWorld(string worldId)
        {
            if (string.IsNullOrEmpty(worldId))
                return;
            
            if (!_worlds.TryGetValue(worldId, out var worldData))
                return;

            worldData.Dispose();
            _serviceWorlds.Remove(worldId);
            _worlds.Remove(worldId);

            GameLog.Log($"Ecs World [{worldId}] Destroyed");
        }
        
        public void CleanUp()
        {
            foreach (var worldData in _worlds)
            {
                worldData.Value.Dispose();
            }
            
            _serviceWorlds.Clear();
            _worlds.Clear();
        }

        public bool SetDefaultWorld(string worldId)
        {
            if (!_worlds.TryGetValue(worldId, out var worldData))
                return false;
            
            var world = worldData;
            _defaultWorldData.Value = world;
  
            return true;
        }

                
        private ProtoWorld CreateWorld(IEcsSystemsConfig config)
        {
            var worldConfig = config.WorldConfiguration.Create();
            var aspectsData = config.AspectsData;
            var worldAspect = new WorldAspect();
            
            foreach (var factory in aspectsData.factories)
            {
                var aspect = factory.Create();
                worldAspect.AddAspect(aspect);
            }
            
            foreach (var aspect in aspectsData.aspects)
            {
                if (!aspect.enabled) continue;

                var stype = aspect.aspectType;
                var aspectType = (Type)stype;
                if (aspectType == null)
                {
#if GAME_DEBUG
                    Debug.LogError($"EcsService: missing Aspect of type {stype.fullTypeName}");
#endif
                    continue;
                }
                
                if (aspectType.IsGenericType && !aspectType.IsConstructedGenericType)
                    continue;
                
                var aspectInstance = aspectType.CreateWithDefaultConstructor() as IProtoAspect;
                worldAspect.AddAspect(aspectInstance);
            }
            
            var protoWorld = new ProtoWorld(worldAspect, worldConfig);
            return protoWorld;
        }
        
        private async UniTask InitializeWorldAsync(EcsWorldData worldData,IEcsSystemsConfig config)
        {
#if DEBUG
            var stopwatch = Stopwatch.StartNew();
            stopwatch.Start();
#endif
            var world = worldData.World;
            var systemsMap = worldData.SystemsMap;
            
            await InitializeEcsService(worldData,config);
            
            var plugins = config.Plugins
                .Where(x => x.enabled && x.plugin != null)
                .Select(x => x.plugin)
                .ToList();

            foreach (var plugin in plugins)
            {
                plugin.PreInit(_context);
            }

            foreach (var systems in systemsMap.Values)
            {
                if (config.EnableUnityModules)
                {
                    systems.AddModule(new Leopotam.EcsProto.Unity.UnityModule(new Leopotam.EcsProto.Unity.UnityModule.Config()
                    {
                        NotBakeComponentsInName = true
                    }));
                }

                foreach (var plugin in plugins)
                {
                    plugin.Init(systems);
                }
            }

            foreach (var systems in systemsMap.Values)
            {
                systems.Init();
            }

            foreach (var plugin in plugins)
            {
                plugin.PostInit();
            }
#if DEBUG
            LogServiceTime("InitializeAsync", stopwatch);
#endif
            
        }

        [Conditional("DEBUG")]
        private void LogServiceTime(string message, Stopwatch timer, bool stop = true)
        {
            if(_defaultConfig.UseFeaturesLoadingLog == false)
                return;
            
            var elapsed = timer.ElapsedMilliseconds;
            timer.Restart();
            if (stop) timer.Stop();
            GameLog.Log($"ECS FEATURE: LOAD {message} TIME = {elapsed} ms",Color.cyan);
        }

        private async UniTask InitializeEcsService(EcsWorldData world,IEcsSystemsConfig config)
        {
            var groups = config
                .FeatureGroups
                .Select(x => CreateEcsGroupAsync(x, world));

            await UniTask.WhenAll(groups);
        }

        private List<IEcsFeature> CollectFeatures(EcsConfigGroup ecsGroup)
        {
            var features = new List<IEcsFeature>();
            foreach (var feature in ecsGroup.features)
                features.Add(feature.Feature);
            return features;
        }

        private async UniTask CreateEcsGroupAsync(EcsConfigGroup ecsGroup, EcsWorldData world)
        {
            var systemsGroup = CollectFeatures(ecsGroup);
            await CreateEcsGroup(ecsGroup.updateType, world, systemsGroup);
        }

        private void ApplyPlugins(EcsServiceWorld serviceWorld)
        {
            var worldData = serviceWorld.WorldData;
            var config = serviceWorld.Config;
            var systemPlugins = worldData.SystemPlugins;

            //plugins already applied
            if (systemPlugins.Count != 0)
                return;
            
            foreach (var plugin in config.SystemsPlugins)
                systemPlugins.Add(plugin.Create());   
            
            var systemsMap = worldData.SystemsMap;
            var world = worldData.World;
            var lifeTime = worldData.LifeTime;
            
            foreach (var systemsPlugin in systemPlugins)
            {
                systemsPlugin.AddTo(lifeTime);

                foreach (var map in systemsMap)
                    systemsPlugin.Add(map.Value);
                systemsPlugin.Execute(world);
            }
        }

        private EcsFeatureSystems CreateEcsSystems(string groupId, EcsWorldData worldData)
        {
            var world = worldData.World;
            var systemsMap = worldData.SystemsMap;
            
            var systems = new EcsFeatureSystems(world);
            systems.AddService(_context,typeof(IContext));
            
            systemsMap[groupId] = systems;
            return systems;
        }

        private async UniTask CreateEcsGroup(
            string updateType,
            EcsWorldData worldData,
            IReadOnlyList<IEcsFeature> runnerFeatures)
        {
            var systemsMap = worldData.SystemsMap;
            var world = worldData.World;
            
            if (!systemsMap.TryGetValue(updateType, out var ecsSystems))
            {
                ecsSystems = CreateEcsSystems(updateType, worldData);
                ecsSystems.AddModule(new Leopotam.EcsProto.QoL.AutoInjectModule());
            }

            var asyncFeatures = runnerFeatures
                .Select(x => InitializeFeatureAsync(ecsSystems, x));

            await UniTask.WhenAll(asyncFeatures);
        }

        public async UniTask InitializeFeatureAsync(IProtoSystems ecsSystems, IEcsFeature feature)
        {
            if (!feature.IsFeatureEnabled) return;

#if DEBUG
            var timer = Stopwatch.StartNew();
            timer.Restart();
#endif

            if (feature is IEcsInitializableFeature initializeFeature)
            {
                var featureLifeTime = new LifeTime();

                await initializeFeature
                    .InitializeAsync(ecsSystems)
                    .AttachTimeoutLogAsync(GetErrorMessage(initializeFeature), _featureTimeout, featureLifeTime.Token);

                featureLifeTime.Terminate();
            }

#if DEBUG
            LogServiceTime($"{feature.FeatureName} | {feature.GetType().Name}", timer, false);
#endif

            if (feature is not IEcsSystemsGroup groupFeature)
                return;

            foreach (var system in groupFeature.EcsSystems)
            {
                var leoEcsSystem = system;

                //create instance of SO systems
                if (leoEcsSystem is UnityEngine.Object systemAsset)
                {
                    systemAsset = UnityEngine.Object.Instantiate(systemAsset);
                    leoEcsSystem = systemAsset as IEcsSystem;
                }

                var featureLifeTime = new LifeTime();
                if (leoEcsSystem is IEcsInitializableFeature initFeature)
                {
#if DEBUG
                    timer.Restart();
#endif
                    await initFeature
                        .InitializeAsync(ecsSystems)
                        .AttachTimeoutLogAsync(GetErrorMessage(initFeature), _featureTimeout, featureLifeTime.Token);

#if DEBUG
                    LogServiceTime($"\tSUB FEATURE {feature.GetType().Name}", timer);
#endif

                    featureLifeTime.Terminate();
                }

                ecsSystems.Add(leoEcsSystem);
            }
        }

        private string GetErrorMessage(IEcsInitializableFeature feature)
        {
            var featureName = feature is IEcsFeature ecsFeature
                ? ecsFeature.FeatureName
                : feature.GetType().Name;

            return $"ECS Feature Timeout Error for {featureName}";
        }
    }
}