namespace UniGame.LeoEcs.ViewSystem.Behavriour
{
    using System;
    using Core.Runtime;
    using Cysharp.Threading.Tasks;
    using Leopotam.EcsProto;
    using UiSystem.Runtime;

    [Serializable]
    public class EcsViewModel : ViewModel, IEcsViewModel
    {

        public virtual async UniTask InitializeAsync(ProtoWorld world, IContext context)
        {
            
        }
        
    }
}