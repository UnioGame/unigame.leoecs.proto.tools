namespace UniGame.LeoEcs.ViewSystem
{
    using System;
    using Behavriour;
    using Bootstrap.Runtime;
    using Cysharp.Threading.Tasks;
    using UniGame.Core.Runtime;
    using Components;
    using Context.Runtime;
    using Game.Modules.UnioModules.UniGame.LeoEcsLite.LeoEcs.ViewSystem.Components.Events;
    using Layouts.Components;
    using Layouts.Systems;
    using LeoEcsLite.LeoEcs.ViewSystem.Systems;
    using Leopotam.EcsProto;
    using Leopotam.EcsProto.QoL;
    using Proto;
    using Shared.Extensions;
    using ViewSystem;
    using UiSystem.Runtime.Settings;
    using UiToolkit.Systems;
    using UniGame.ViewSystem.Runtime;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    
    [CreateAssetMenu(menuName = "ECS Proto/Features/Views Feature", fileName = "ECS Views Feature")]
    public class ViewSystemFeature : BaseLeoEcsFeature,IAutoInitFeature
    {
        private IEcsViewTools _ecsViewTools;
        
        public override async UniTask InitializeAsync(IProtoSystems ecsSystems)
        {
            var protoWorld = ecsSystems.GetWorld();
            var context = ecsSystems.GetShared<IContext>();
            var viewSystem = await context.ReceiveFirstAsync<IGameViewSystem>();
            
            _ecsViewTools = new EcsViewTools(context, viewSystem);

            protoWorld.SetGlobal(viewSystem);
            protoWorld.SetGlobal(_ecsViewTools);
            protoWorld.SetGlobal(context);
            
            ecsSystems.DelHere<ViewStatusSelfEvent>();
            
            //layouts
            ecsSystems.Add(new RegisterNewViewLayoutSystem());
            ecsSystems.DelHere<RegisterViewLayoutSelfRequest>();
            
            //if view entity is dead and 
            ecsSystems.Add(new CloseViewByDeadEntitySystem());
            ecsSystems.Add(new CloseViewSystem());
            ecsSystems.Add(new ViewServiceInitSystem());
            ecsSystems.Add(new CloseAllViewsSystem());

            //show view queued one by one
            ecsSystems.Add(new ShowViewsQueuedSystem());
            
            //container systems
            ecsSystems.Add(new CreateViewInContainerSystem());
            //check is container free
            ecsSystems.Add(new UpdateViewContainerBusyStatusSystem());

            //view creation systems
            ecsSystems.Add(new CreateLayoutViewSystem());
            ecsSystems.DelHere<CreateLayoutViewRequest>();

            //update view status systems
            ecsSystems.Add(new ViewUpdateStatusSystem());
            ecsSystems.Add(new UpdateViewOrderSystem());
            ecsSystems.Add(new CreateViewSystem());
            ecsSystems.Add(new MarkViewAsInitializedSystem());
            ecsSystems.Add(new SyncUiToolkitElementsSystem());
            
            ecsSystems.Add(new InitializeViewsSystem());
            ecsSystems.Add(new InitializeModelOfViewsSystem());
            //initialize view id component when view initialized
            ecsSystems.Add(new InitializeViewIdComponentSystem());
            ecsSystems.Add(new RemoveUpdateRequestSystem());
            
            ecsSystems.DelHere<CreateViewRequest>();
            ecsSystems.DelHere<CloseAllViewsRequest>();
            ecsSystems.DelHere<CloseViewByTypeRequest>();
            ecsSystems.DelHere<CloseTargetViewByTypeRequest>();
            ecsSystems.DelHere<CloseViewSelfRequest>();
            ecsSystems.DelHere<UpdateViewRequest>();
        }
    }

    [Serializable]
    public class ViewSettingsReference : AssetReferenceT<ViewsSettings>
    {
        public ViewSettingsReference(string guid) : base(guid)
        {
        }
    }
}
