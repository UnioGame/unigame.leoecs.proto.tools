namespace UniGame.LeoEcs.Converter.Runtime
{
    using System;
    using Abstract;
    using Leopotam.EcsProto;
    using UnityEngine.UIElements;

    [Serializable]
    [UxmlObject]
    public abstract partial class EcsComponentConverter : IEcsComponentConverter
    {
        [UxmlAttribute]
        public bool isEnabled = true;

        public virtual bool IsEnabled => isEnabled;

        public virtual string Name => GetType().Name;

        public abstract void Apply(ProtoWorld world, ProtoEntity entity);

        protected bool IsSubstring(string value, string search)
        {
            if (string.IsNullOrEmpty(value)) return false;
            return value.Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        public virtual bool IsMatch(string searchString)
        {
            if (string.IsNullOrEmpty(searchString)) return true;
            if (IsSubstring(GetType().Name, searchString))
                return true;
            return false;
        }
    }
}