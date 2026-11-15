namespace UniGame.LeoEcs.ViewSystem.UiToolkit
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Elements attached to or detached from a panel during play. SyncUiToolkitElementsSystem uses them to give
    /// elements created after their view was registered an entity, and to free the entity of a removed element.
    /// </summary>
    internal static class UiToolkitElementsTracker
    {
        public static readonly List<EcsUiToolkitElement> Attached = new();
        public static readonly List<EcsUiToolkitElement> Detached = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Attached.Clear();
            Detached.Clear();
        }
    }
}