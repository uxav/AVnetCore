using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro;
using UXAV.AVnet.Core.DeviceSupport;

namespace UXAV.AVnet.Core.UI.Components
{
    public class UIKeypad : UIObject, IButtons
    {
        private readonly UIButtonCollection _buttons = new UIButtonCollection();

        public UIKeypad(ISigProvider sigProvider, uint startJoin)
            : base(sigProvider)
        {
            var enumValues = Enum.GetValues(typeof(UIKeypadButtonValue)).Cast<UIKeypadButtonValue>();
            foreach (var value in enumValues)
            {
                var btn = new UIButton(this, startJoin + (uint)value);
                btn.SetId((uint)value);
                _buttons.Add(btn);
            }
        }

        public UIKeypad(SmartObject smartObject)
            : base(smartObject)
        {
            var enumValues = Enum.GetValues(typeof(UIKeypadButtonValue)).Cast<UIKeypadButtonValue>();
            foreach (var value in enumValues)
            {
                UIButton btn;
                if (value == UIKeypadButtonValue.Key0)
                {
                    btn = new UIButton(this, 10);
                    btn.SetId(0);
                    _buttons.Add(btn);
                    continue;
                }

                if (value >= UIKeypadButtonValue.Misc1)
                {
                    btn = new UIButton(this, 1 + (uint)value);
                    btn.SetId((uint)value);
                    _buttons.Add(btn);
                    continue;
                }

                btn = new UIButton(this, (uint)value);
                btn.SetId((uint)value);
                _buttons.Add(btn);
            }
        }

        public IEnumerable<IButton> Buttons => _buttons;
    }

    public enum UIKeypadButtonValue
    {
        Key0,
        Key1,
        Key2,
        Key3,
        Key4,
        Key5,
        Key6,
        Key7,
        Key8,
        Key9,
        Misc1,
        Misc2
    }
}