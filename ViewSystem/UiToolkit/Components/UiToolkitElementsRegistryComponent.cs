namespace UniGame.LeoEcs.ViewSystem.UiToolkit.Components
{
    using System;
    using Leopotam.EcsProto;
    using Proto;

    [Serializable]
    public struct UiToolkitElementsRegistryComponent : IProtoAutoReset<UiToolkitElementsRegistryComponent>
    {
        public UiToolkitElementsRegistry Value;

        public void SetHandlers(IProtoPool<UiToolkitElementsRegistryComponent> pool) => pool.SetResetHandler(AutoReset);
        
        public static void AutoReset(ref UiToolkitElementsRegistryComponent component)
        {
            component.Value = null;
        }
    }
}