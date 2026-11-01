namespace UniGame.LeoEcs.ViewSystem.UiToolkit.Extensions
{
    using System.Runtime.CompilerServices;
    using Components;
    using Leopotam.EcsProto;
    using Shared.Extensions;

    public static class UiToolkitElementExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TElement GetUiToolkitElement<TElement>(
            this ProtoWorld world,
            ProtoEntity entity)
            where TElement : EcsUiToolkitElement
        {
            ref var component =
                ref world.GetComponent<UiToolkitElementComponent>(entity);

            return (TElement)component.Element;
        }
    }
}