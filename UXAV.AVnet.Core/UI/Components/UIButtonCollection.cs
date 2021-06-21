using System;
using System.Collections.Generic;
using System.Linq;
using UXAV.AVnet.Core.DeviceSupport;
using UXAV.AVnet.Core.Models.Collections;
using UXAV.Logging;

namespace UXAV.AVnet.Core.UI.Components
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

        public new void Add(IButton button)
        {
            base.Add(button);
            if(_subscribeCount == 0) return;
            button.ButtonEvent += OnButtonEvent;
        }

        private void OnButtonEvent(IButton button, ButtonEventArgs args)
        {
            try
            {
                _buttonEvent?.Invoke(button, new ButtonEventArgs(args.EventType, args.HoldTime, this,
                    InternalDictionary.First(kvp => kvp.Value.DigitalJoinNumber == button.DigitalJoinNumber).Key));
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
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

        public static UIButtonCollection CreateSequencedCollection(ISigProvider provider, uint startJoinNumber, uint count)
        {
            return CreateSequencedCollection(provider, startJoinNumber, 1, count);
        }

        public static UIButtonCollection CreateSequencedCollection(ISigProvider provider, uint startJoinNumber, uint firstId, uint count)
        {
            var list = new List<IButton>();
            for (var b = 0U; b < count; b++)
            {
                var button = new UIButton(provider, startJoinNumber + b);
                button.SetId(b + firstId);
                list.Add(button);
            }
            return new UIButtonCollection(list);
        }
    }
}