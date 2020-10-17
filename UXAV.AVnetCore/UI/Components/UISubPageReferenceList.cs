using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro;
using UXAV.AVnetCore.DeviceSupport;
using UXAV.Logging;

namespace UXAV.AVnetCore.UI.Components
{
    public abstract class UISubPageReferenceList : UIObject, IButtons, IEnumerable<UISubPageReferenceListItem>
    {
        private uint _selectedItemIndex;
        private ushort _count;

        private readonly Dictionary<uint, UISubPageReferenceListItem> _items =
            new Dictionary<uint, UISubPageReferenceListItem>();

        protected UISubPageReferenceList(SmartObject smartObject, uint digitalJoinIncrement, uint analogJoinIncrement,
            uint serialJoinIncrement, CreateItemForIndexCallBack callBack)
            : base(smartObject)
        {
            ButtonCollection = new UIButtonCollection();

            DigitalJoinIncrement = digitalJoinIncrement;
            AnalogJoinIncrement = analogJoinIncrement;
            SerialJoinIncrement = serialJoinIncrement;

            SigProvider.SigChange += OnSigChange;

            Logger.Debug("{0}.ctor for SmartObject ID: {1}", GetType(), smartObject.ID);
            try
            {
                uint count = 1;
                while (true)
                {
                    var name = $"Item {count} Visible";
                    if (SigProvider.BooleanInput.Contains(name))
                    {
                        count++;
                    }
                    else
                        break;
                }

                MaxNumberOfItems = count - 1;

                Logger.Debug("{0} for SmartObject ID: {1} contains {2} items", GetType(), smartObject.ID,
                    MaxNumberOfItems);

                for (uint i = 1; i <= MaxNumberOfItems; i++)
                {
                    _items[i] = callBack(this, i);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error in {0}.ctor, {1}", GetType().Name, e.Message);
            }
        }

        public IEnumerable<IButton> Buttons => ButtonCollection;

        protected UIButtonCollection ButtonCollection { get; }

        public event UISubPageReferenceListSelectedItemChangeEventHander SelectedItemChange;

        public event UISubPageReferenceListIsMovingChangedEventHandler IsMovingChange;

        public delegate UISubPageReferenceListItem CreateItemForIndexCallBack(UISubPageReferenceList list, uint id);

        public UISubPageReferenceListItem this[uint id] => _items[id];

        public virtual ushort NumberOfItems => SigProvider.UShortInput["Set Number of Items"].UShortValue;

        public uint MaxNumberOfItems { get; }

        public uint NumberOfEmptyItems => MaxNumberOfItems - _count;

        public bool IsMoving => SigProvider.BooleanOutput["Is Moving"].BoolValue;

        public uint DigitalJoinIncrement { get; }

        public uint AnalogJoinIncrement { get; }

        public uint SerialJoinIncrement { get; }

        public UISubPageReferenceListItem SelectedItem =>
            _items.ContainsKey(_selectedItemIndex) ? _items[_selectedItemIndex] : null;

        internal ushort ItemsAddedCount => _count;

        public virtual void ScrollToItem(ushort item)
        {
            SigProvider.UShortInput["Scroll To Item"].UShortValue = item;
        }

        protected virtual void SetNumberOfItems(ushort items)
        {
            SigProvider.UShortInput["Set Number of Items"].UShortValue = items;
            Logger.Debug($"{GetType().Name} NumberOfItems: {NumberOfItems}");
        }

        public IEnumerator<UISubPageReferenceListItem> GetEnumerator()
        {
            return _items.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void ClearList(bool justByResettingTheCount)
        {
            _selectedItemIndex = 0;

            _count = 0;

            if (!justByResettingTheCount)
            {
                foreach (var item in this)
                {
                    item.LinkedObject = null;
                    item.SetFeedbackInternal(false);
                }

                SetNumberOfItems(_count);
            }

            OnSelectedItemChange(this);
        }

        public virtual void ClearList()
        {
            ClearList(false);
            OnSelectedItemChange(this);
        }

        public virtual uint AddItem(object linkedObject, bool holdOffSettingListSize)
        {
            if (_count == MaxNumberOfItems)
            {
                Logger.Error("Cannot add item to {0}, No more items available!, count = {1}, max = {2}",
                    GetType().Name, _count, MaxNumberOfItems);
                return 0;
            }

            _count++;

            var item = this[_count];

            item.Show();
            item.Enable();
            item.LinkedObject = linkedObject;

            if (!holdOffSettingListSize)
                SetNumberOfItems(_count);

            return _count;
        }

        public virtual uint AddItem(object linkedObject)
        {
            return AddItem(linkedObject, false);
        }

        public bool ContainsLinkedObject(object linkedObject)
        {
            for (uint i = 1; i <= NumberOfItems; i++)
            {
                if (_items[i].LinkedObject == linkedObject) return true;
            }

            return false;
        }

        public void SetListSizeToItemCount()
        {
            SetNumberOfItems(_count);
        }

        public void SetSelectedItem(object linkedObject)
        {
            if (linkedObject == null)
            {
                ClearSelectedItems();
                return;
            }

            try
            {
                SetSelectedItem(Nullable.GetUnderlyingType(linkedObject.GetType()) != null
                    ? this.FirstOrDefault(i => i.LinkedObject == linkedObject)
                    : this.FirstOrDefault(i => i.LinkedObject != null && i.LinkedObject.Equals(linkedObject)));
            }
            catch (Exception e)
            {
                Logger.Error("Cannot set selected item in SRL, linkedObject type is \"{0}\", {1}",
                    linkedObject.GetType(), e.Message);
            }
        }

        public void SetSelectedItem(UISubPageReferenceListItem item)
        {
            foreach (var listItem in this.Where(i => i != item))
            {
                listItem.SetFeedbackInternal(false);
            }

            if (item != null)
            {
                item.SetFeedbackInternal(true);
                _selectedItemIndex = item.Id;
            }
            else
            {
                _selectedItemIndex = 0;
            }

            OnSelectedItemChange(this);
        }

        public void ClearSelectedItems()
        {
            foreach (var listItem in this)
            {
                listItem.SetFeedbackInternal(false);
            }

            _selectedItemIndex = 0;
            OnSelectedItemChange(this);
        }

        protected virtual void OnSelectedItemChange(UISubPageReferenceList list)
        {
            SelectedItemChange?.Invoke(list);
        }

        protected virtual void OnIsMovingChange(UISubPageReferenceList list, bool ismoving)
        {
            IsMovingChange?.Invoke(list, ismoving);
        }

        private void OnSigChange(SigProviderDevice sigProvider, SigEventArgs args)
        {
            if (args.Sig.Name == "Is Moving" && args.Sig.Type == eSigType.Bool)
            {
                OnIsMovingChange(this, args.Sig.BoolValue);
            }
        }
    }

    public delegate void UISubPageReferenceListSelectedItemChangeEventHander(UISubPageReferenceList list);

    public delegate void UISubPageReferenceListIsMovingChangedEventHandler(UISubPageReferenceList list, bool isMoving);
}