using System.Collections.Generic;
using System.Linq;
using UXAV.AVnetCore.Models.Collections;

namespace UXAV.AVnetCore.UI.Components
{
    public sealed class UIButtonCollection : UXCollection<IButton>, IButtonCollection
    {
        private ButtonEventHandler _buttonEvent;
        private int _subscribeCount;

        public UIButtonCollection()
        {
        }

        public UIButtonCollection(IEnumerable<IButton> fromButtons)
            : base(fromButtons)
        {
        }

        public event ButtonEventHandler ButtonEvent
        {
            add
            {
                if (_subscribeCount == 0)
                {
                    foreach (var button in InternalDictionary.Values)
                    {
                        button.ButtonEvent += OnButtonEvent;
                    }
                }

                _subscribeCount++;
                _buttonEvent += value;
            }
            remove
            {
                if (_subscribeCount == 0) return;
                _subscribeCount--;
                // ReSharper disable once DelegateSubtraction
                _buttonEvent -= value;
                if (_subscribeCount == 0)
                {
                    foreach (var button in InternalDictionary.Values)
                    {
                        button.ButtonEvent -= OnButtonEvent;
                    }
                }
            }
        }

        private void OnButtonEvent(IButton button, ButtonEventArgs args)
        {
            _buttonEvent?.Invoke(button, new ButtonEventArgs(args.EventType, args.HoldTime, this,
                InternalDictionary.First(kvp => kvp.Value.DigitalJoinNumber == button.DigitalJoinNumber).Key));
        }

        public void SetInterlockedFeedback(uint key)
        {
            foreach (var dictItem in InternalDictionary.Where(item => item.Key != key))
            {
                dictItem.Value.Feedback = false;
            }

            if (InternalDictionary.ContainsKey(key))
            {
                InternalDictionary[key].Feedback = true;
            }
        }

        public void ClearInterlockedFeedback()
        {
            foreach (var button in InternalDictionary.Values)
            {
                button.Feedback = false;
            }
        }
    }
}