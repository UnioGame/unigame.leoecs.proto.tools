namespace UniGame.LeoEcs.ViewSystem.UiToolkit
{
    using System.Collections.Generic;
    using Components;
    using Converter.Runtime;
    using Leopotam.EcsProto;
    using Leopotam.EcsProto.QoL;
    using Shared.Extensions;
    using UnityEngine;
    using UnityEngine.UIElements;

    [UxmlElement(visibility = LibraryVisibility.Hidden)]
    public abstract partial class EcsUiToolkitElement : VisualElement
    {
        [UxmlObjectReference("converters")]
        public List<EcsComponentConverter> Converters { get; set; } = new();

        public ProtoPackedEntity Entity { get; internal set; }

        protected EcsUiToolkitElement()
        {
            if (!Application.isPlaying)
                return;

            RegisterCallback<AttachToPanelEvent>(_ => UiToolkitElementsTracker.Attached.Add(this));
            RegisterCallback<DetachFromPanelEvent>(_ => UiToolkitElementsTracker.Detached.Add(this));
        }

        internal abstract void AddTypeComponent(ProtoWorld world, ProtoEntity entity);
    }

    [UxmlElement(visibility = LibraryVisibility.Hidden)]
    public abstract partial class EcsUiToolkitElement<TElement> : EcsUiToolkitElement
        where TElement : EcsUiToolkitElement<TElement>
    {
        internal sealed override void AddTypeComponent(ProtoWorld world, ProtoEntity entity)
        {
            world.GetOrAddComponent<UiToolkitElementComponent<TElement>>(entity);
        }
    }
}
