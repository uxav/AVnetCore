using System;
using System.Threading.Tasks;
using Crestron.SimplSharpPro;
using UXAV.AVnet.Core.DeviceSupport;

namespace UXAV.AVnet.Core.UI.Components.Views
{
    public class UIDialog : UISubPageViewController
    {
        private readonly UISubPageReferenceList _list;
        private readonly UIButtonCollection _listButtons;
        private readonly UILabel _subTitleLabel;
        private readonly UILabel _titleLabel;

        public UIDialog(ISigProvider sigProvider, uint visibleJoinNumber, uint titleJoin, uint subTitleJoin,
            UISubPageReferenceList list)
            : base(sigProvider, visibleJoinNumber)
        {
            _titleLabel = new UILabel(this, titleJoin);
            _subTitleLabel = new UILabel(this, subTitleJoin);
            _list = list;
            _listButtons = new UIButtonCollection(list.Buttons);
        }

        public UIDialog(ISigProvider sigProvider, uint visibleJoinNumber, uint titleJoin, uint subTitleJoin,
            SmartObject smartObject)
            : this(sigProvider, visibleJoinNumber, titleJoin, subTitleJoin,
                new UIGenericSubPageReferenceList(smartObject))
        {
        }

        protected UIDialogCallbackHandler Callback { get; private set; }

        public string DialogId { get; private set; }

        public override void Show()
        {
            throw new NotSupportedException("Please use alternative Show method");
        }

        public void Show(UIDialogCallbackHandler callback, string dialogId, string title, string subTitle,
            params string[] optionTitles)
        {
            Show(callback, dialogId, title, subTitle, TimeSpan.Zero, optionTitles);
        }

        public void Show(UIDialogCallbackHandler callback, string dialogId, string title, string subTitle,
            TimeSpan timeoutTime,
            params string[] optionTitles)
        {
            Callback = callback;
            DialogId = dialogId;
            _list.ClearList();
            Task.Run(() =>
            {
                _titleLabel.SetText(title);
                _subTitleLabel.SetText(subTitle);
                if (optionTitles.Length > _list.MaxNumberOfItems)
                    throw new IndexOutOfRangeException("optionTitles arg count is more than list size");

                foreach (var optionTitle in optionTitles)
                {
                    var index = _list.AddItem(optionTitle);
                    _list[index].Name = optionTitle;
                }
            });
            base.Show(timeoutTime);
        }

        protected override void WillShow()
        {
            _listButtons.ButtonEvent += ListButtonsOnButtonEvent;
        }

        protected override void DidShow()
        {
        }

        protected override void WillHide()
        {
            _listButtons.ButtonEvent -= ListButtonsOnButtonEvent;
            Callback = null;
            DialogId = null;
        }

        protected override void DidHide()
        {
        }

        protected virtual void ListButtonsOnButtonEvent(IButton button, ButtonEventArgs args)
        {
            if (args.EventType != ButtonEventType.Pressed) return;
            var callback = Callback;
            var dialogId = DialogId;
            Hide();
            callback.Invoke(this, dialogId, args.CollectionKey);
        }
    }

    public delegate void UIDialogCallbackHandler(UIDialog dialog, string dialogId, uint itemSelected);
}