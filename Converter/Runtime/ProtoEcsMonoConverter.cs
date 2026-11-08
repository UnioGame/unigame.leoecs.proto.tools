namespace UniGame.LeoEcs.Converter.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Abstract;
    using Core.Runtime;
    using Cysharp.Threading.Tasks;
    using Leopotam.EcsProto;
    using Leopotam.EcsProto.QoL;
    using Proto;
    using Proto.Shared;
    using Shared.Extensions;
    using UniGame.Runtime.DataFlow;
    using UnityEngine;
    using UnityEngine.Serialization;

#if UNITY_EDITOR
    using Editor;
#endif
    
#if TRI_INSPECTOR
    using TriInspector;
#endif
    
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
    public class ProtoEcsMonoConverter : 
        MonoBehaviour, 
        ILeoEcsMonoConverter,
        IEcsEntity
    {
        #region inspector data
        
#if ODIN_INSPECTOR
        [BoxGroup("converter settings")]
#endif
        [SerializeField] 
        public bool createEntityOnEnabled = true;
        
#if ODIN_INSPECTOR
        [BoxGroup("converter settings")]
#endif
        [SerializeField] 
        public bool createEntityOnStart = false;

        [Space]
#if ODIN_INSPECTOR
        [BoxGroup("converter settings")]
#endif
        [SerializeField] 
        public bool destroyEntityOnDisable = true;
        
#if ODIN_INSPECTOR
        [BoxGroup("converter settings")]
#endif
        [SerializeField] 
        public bool destroyOnDestroy = false;
        
#if ODIN_INSPECTOR
        [BoxGroup("converter settings")]
#endif
        public WorldType worldType = WorldType.DefaultWorld;
        
#if ODIN_INSPECTOR
        [BoxGroup("converter settings")]
        [ShowIf(nameof(IsCustomWorld))]
#endif
        [Tooltip("If true, create world if not exists")]
        public bool createWorldIfNonExists = true;
        
#if ODIN_INSPECTOR
        [BoxGroup("converter settings")]
        [ShowIf(nameof(IsCustomWorld))]
        [ValueDropdown(valuesGetter:nameof(GetWorlds),IsUniqueList = true,AppendNextDrawer = true)]
#endif
        public string worldId = string.Empty;
        
        
        [Header("Converters")]
        [FormerlySerializedAs("_serializableConverters")]
#if ODIN_INSPECTOR
        [Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
#endif
#if  ODIN_INSPECTOR || TRI_INSPECTOR
        //[InlineProperty]
        [ListDrawerSettings(ListElementLabelName = "@Name")]
#endif
        [Space(8)]
        [SerializeReference]
        public List<IEcsComponentConverter> serializableConverters = new();

        [Space(8)] 
#if ODIN_INSPECTOR
        [Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface)] 
        [ListDrawerSettings(ListElementLabelName = "@Name")]
#endif
#if  ODIN_INSPECTOR || TRI_INSPECTOR
        [InlineEditor()]
#endif
#if TRI_INSPECTOR
        [ListDrawerSettings()]
#endif
        public List<EcsConverterAsset> assetConverters = new();

#if ODIN_INSPECTOR
        [BoxGroup("runtime info")] 
#endif
        [FormerlySerializedAs("ecsEntityId")]
        [Space]
        [SerializeField]
#if  ODIN_INSPECTOR || TRI_INSPECTOR
        [ReadOnly]
        [ShowIf(nameof(IsRuntime))] 
#endif
        public ProtoEntity entity = (ProtoEntity)(-1);
        
#if ODIN_INSPECTOR
        [BoxGroup("runtime info")] 
        [InlineProperty]
        [HideLabel]
#endif
        public EntityInfoView components = new();
        
#endregion

#region private data

        private EntityState _state = EntityState.Destroyed;
        private ProtoPackedEntity _packedEntity;
        private ProtoWorld _world;
        private List<IEcsComponentConverter> _converters = new();
        private LifeTime _entityLifeTime = new();
        private int _generation;
        
#endregion

        public bool IsCustomWorld => worldType == WorldType.CustomWorld;

        public bool IsRuntime => Application.isPlaying;

        public bool IsAutoGenerating => createEntityOnEnabled || createEntityOnStart;

        public bool IsPlayingAndReady => IsRuntime && (int)entity >= 0;

        public bool IsCreated => _state == EntityState.Created;

        public ProtoWorld World => _world;

        public ProtoPackedEntity PackedEntity => _packedEntity;
        
        public ILifeTime LifeTime => _entityLifeTime;

        public ProtoEntity Entity => entity;

        public bool AutoCreate => createEntityOnEnabled || createEntityOnStart;
        
        public IReadOnlyList<IEcsComponentConverter> MonoConverters => UpdateMonoConverter();

        public IReadOnlyList<IEcsComponentConverter> ComponentConverters => assetConverters;
        
        #region public methods
        
        public async UniTask Convert()
        {
            _generation++; 
            
            var generation = _generation;

            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            
            if(_state != EntityState.Creating) return;

            var world = await gameObject
                .WaitWorldReady(worldId,worldType,createWorldIfNonExists,_entityLifeTime.Token);

            if (world.IsAlive() == false)   
            {
                DestroyEntity();
                return;
            }
            
            if(this == null ||
               _state != EntityState.Creating || 
               generation != _generation) return;
            
            var targetEntity = world.NewEntity();
            
            Convert(world, targetEntity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ConnectEntity(ProtoWorld world,ProtoEntity ecsEntity)
        {
            entity = ecsEntity;
            
            _world = world;
            _packedEntity = ecsEntity.PackEntity(world);
            _state = EntityState.Created;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Convert(ProtoWorld world, ProtoEntity ecsEntity)
        {
            components.World = world;
            components.Entity = world.PackEntity(ecsEntity);
            
            ConnectEntity(world,ecsEntity);
            gameObject.ConvertGameObjectToEntity(world,ecsEntity);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Convert(ProtoWorld world, int ecsEntity)
        {
            Convert(world, (ProtoEntity)ecsEntity);
        }
        
#endregion
        
#region private methods

        private List<IEcsComponentConverter> UpdateMonoConverter()
        {
            if(_converters.Count > 0) return _converters;
            
            _converters ??= new List<IEcsComponentConverter>();
            _converters.Clear();
            
            GetComponents(_converters);
            
            _converters.AddRange(serializableConverters);
            return _converters;
        }

        private void CreateEntity()
        {
            if (_state != EntityState.Destroyed) return;

            _state = EntityState.Creating;
            _entityLifeTime.Restart();

            Convert()
                .AttachExternalCancellation(_entityLifeTime.Token)
                .Forget();
        }

#if  ODIN_INSPECTOR || TRI_INSPECTOR
        [ShowIf(nameof(IsPlayingAndReady))]
        [Button("Destroy")]
#endif
        private void DestroyObject()
        {
            DestroyEntity();
            Destroy(gameObject);
        }

        public void DestroyEntity()
        {
            DestroyEcsEntity();
            SetDestroyedState();
        }

        private void DestroyEcsEntity()
        {
            if (_world == null || _world.IsAlive() == false) return;
            if ((int)entity < 0) return;
            if (!_packedEntity.Unpack(_world, out var targetEntity)) return;
            
            //notify converters about destroy
            foreach (var converter in UpdateMonoConverter())
            {
                if (converter is IConverterEntityDestroyHandler destroyHandler)
                    destroyHandler.OnEntityDestroy(_world, targetEntity);
            }
            
            //notify converters about destroy
            foreach (var converter in assetConverters)
            {
                if (converter is not IConverterEntityDestroyHandler destroyHandler) continue;
                destroyHandler.OnEntityDestroy(_world, targetEntity);
            }
            
            _world.DelEntity(targetEntity);
        }

        private void SetDestroyedState()
        {
            entity = (ProtoEntity)(-1);
            _state = EntityState.Destroyed;
            _packedEntity = default;
            _entityLifeTime.Terminate();
        }

#endregion
        
#region unity methods

        // Start is called before the first frame update
        private void Start()
        {
            if (!createEntityOnStart) return;
            CreateEntity();
        }

        private void OnEnable()
        {
            if (!createEntityOnEnabled) return;
            var frame = Time.frameCount;
            CreateEntity();
        }

        private void OnDisable()
        {
            if (!destroyEntityOnDisable) return;
            var frame = Time.frameCount;
            DestroyEntity();
        }

        private void OnDestroy()
        {
            if (destroyOnDestroy) DestroyEntity();
            _entityLifeTime.Terminate();
        }

        private void Awake()
        {
            _entityLifeTime ??= new LifeTime();
            //get all converters
            _converters ??= new List<IEcsComponentConverter>();
        }

#endregion
        
        public IEnumerable<string> GetWorlds() => EcsWorldsAsset.GetCustomWorlds();

#if UNITY_EDITOR
        
        private void OnDrawGizmos()
        {
            foreach (var converter in serializableConverters)
            {
                if (converter is ILeoEcsGizmosDrawer gizmosDrawer)
                    gizmosDrawer.DrawGizmos(gameObject);
            }

            foreach (var converter in assetConverters)
            {
                if(converter == null) continue;
                
                var converters = converter.converters;
                foreach (var converterValue in converters)
                {
                    switch (converterValue.Value)
                    {
                        case null:
                            continue;
                        case ILeoEcsGizmosDrawer gizmosDrawer:
                            gizmosDrawer.DrawGizmos(gameObject);
                            break;
                    }
                }
            }
        }

#if ODIN_INSPECTOR
        [BoxGroup("runtime info")] 
        [Button(ButtonSizes.Large,Icon = SdfIconType.Book)]
#endif
#if  ODIN_INSPECTOR || TRI_INSPECTOR
        [EnableIf(nameof(IsPlayingAndReady))]
        [GUIColor(1f,0.5f,0.1f)]
#endif
#if TRI_INSPECTOR
        [Button(ButtonSizes.Large)]
#endif
        public void ShowComponents()
        {
            var debugEntityData = new EntityEditorData()
            {
                world = _world,
                worldId = worldId,
                entity = entity,
            };
            EntityEditorCommands.OpenEntityInfo(debugEntityData);
        }

#endif
        
    }

    [Serializable]
    public enum EntityState : byte
    {
        Creating,
        Created,
        Destroyed,
    }
}
