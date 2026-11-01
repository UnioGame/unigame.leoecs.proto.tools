namespace UniGame.LeoEcs.ViewSystem.UiToolkit.Elements
{
    using System;
    using UnityEngine.UIElements;

    [UxmlElement]
    public partial class EcsButtonElement : EcsUiToolkitElement<EcsButtonElement>
    {
        public const string ButtonClassName = "ecs-button__control";
        
        private readonly Button _button;
        private bool _clicked;
        
        [UxmlAttribute]
        public string Text
        {
            get => _button.text;
            set => _button.text = value;
        }
        
        public override VisualElement contentContainer => _button;

        public EcsButtonElement()
        {
            _button = new Button(OnClick);
            _button.AddToClassList(ButtonClassName);

            hierarchy.Add(_button);
        }

        public bool TakeClick()
        {
            if (!_clicked)
                return false;

            _clicked = false;
            return true;
        }

        public void SetInteractable(bool value)
        {
            _button.SetEnabled(value);

            if (!value)
                _clicked = false;
        }

        private void OnClick()
        {
            _clicked = true;
        }
    }
}