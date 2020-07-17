using System;
using System.Collections;
using System.Collections.Generic;
using Crestron.SimplSharpPro;
using UXAV.AVnetCore.Logging;

namespace UXAV.AVnetCore.UI.Components
{
    public class UITabBar : UIObject, IButtonCollection
    {
        private readonly UIButtonCollection _buttons = new UIButtonCollection();

        public UITabBar(SmartObject smartObject)
            : base(smartObject)
        {
            Logger.Debug("{0}.ctor for SmartObject ID: {1}", GetType(), smartObject.ID);
            try
            {
                var count = 1U;
                while (true)
                {
                    var name = $"Tab Button {count} Press";
                    if (SigProvider.BooleanOutput.Contains(name))
                    {
                        var button = new UIButton(this, name,
                            $"Tab Button {count} Select");
                        button.SetId(count);
                        _buttons.Add(button);
                        count++;
                    }
                    else
                    {
                        break;
                    }
                }

                Logger.Debug("{0} for SmartObject ID: {1} contains {2} items", GetType(), smartObject.ID,
                    _buttons.Count);
            }
            catch (Exception e)
            {
                Logger.Error("Error in {0}.ctor, {1}", GetType().Name, e.Message);
            }
        }

        public IButton this[uint index] => _buttons[index];

        public event ButtonEventHandler ButtonEvent
        {
            add => _buttons.ButtonEvent += value;
            remove => _buttons.ButtonEvent -= value;
        }

        public void SetInterlockedFeedback(uint key)
        {
            _buttons.SetInterlockedFeedback(key);
        }

        public void ClearInterlockedFeedback()
        {
            _buttons.ClearInterlockedFeedback();
        }

        public IEnumerator<IButton> GetEnumerator()
        {
            return _buttons.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}