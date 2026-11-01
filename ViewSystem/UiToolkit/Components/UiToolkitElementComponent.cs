namespace UniGame.LeoEcs.ViewSystem.UiToolkit.Components
{
    using System;
    using System.Threading;
    using Leopotam.EcsProto;
    using Proto;

    /// <summary>
    ///
    /// </summary>
#if ENABLE_IL2CPP
    using Unity.IL2CPP.CompilerServices;

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
#endif
    [Serializable]
    public struct UiToolkitElementComponent : IProtoAutoReset<UiToolkitElementComponent>
    {
        public EcsUiToolkitElement Element;

        public void SetHandlers(IProtoPool<UiToolkitElementComponent> pool) => pool.SetResetHandler(AutoReset);

        public static void AutoReset(ref UiToolkitElementComponent c)
        {
            c.Element = null;
        }
    }
    
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
#endif
    [Serializable]
    public struct UiToolkitElementComponent<TElement>
        where TElement : EcsUiToolkitElement
    {
    }
}