namespace UniGame.LeoEcs.ViewSystem.UiToolkit.Extensions
{
    using System.Runtime.CompilerServices;
    using Components;
    using Leopotam.EcsProto.QoL;

    public static class UiToolkitElementIt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProtoItChain ElementChain<TElement>()
            where TElement : EcsUiToolkitElement
        {
            return It
                .Chain<UiToolkitElementComponent>()
                .Inc<UiToolkitElementComponent<TElement>>();
        }
    }
}