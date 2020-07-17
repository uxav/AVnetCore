using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro;
using UXAV.AVnetCore.DeviceSupport;
using UXAV.Logging;

namespace UXAV.AVnetCore.UI.Components
{
    public sealed class UIDynamicButtonList : UIObject, IButtons, IEnumerable<UIDynamicButtonListItem>
    {
        private readonly uint _maxNumberOfItems;
        private uint _selectedItemIndex;
        private ushort _count;

        private readonly Dictionary<uint, UIDynamicButtonListItem> _items =
            new Dictionary<uint, UIDynamicButtonListItem>();

        public UIDynamicButtonList(SmartObject smartObject) :
            base(smartObject)
        {
            SigProvider.SigChange += OnSigChange;

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

                _maxNumberOfItems = count - 1;

                Logger.Debug("{0} for SmartObject ID: {1} contains {2} items", GetType(), smartObject.ID,
                    _maxNumberOfItems);

                for (uint i = 1; i <= _maxNumberOfItems; i++)
                {
                    _items[i] = new UIDynamicButtonListItem(this, i);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }


        public event UIDynamicButtonListSelectedItemChangeEventHander SelectedItemChange;

        public event UIDynamicButtonListIsMovingChangedEventHandler IsMovingChange;

        public UIDynamicButtonListItem this[uint index]
        {
            get { return _items[index]; }
        }

        public IEnumerable<IButton> Buttons => this;

        public ushort NumberOfItems => SigProvider.UShortInput["Set Number of Items"].UShortValue;

        public uint MaxNumberOfItems => _maxNumberOfItems;

        public uint NumberOfEmptyItems => MaxNumberOfItems - _count;

        public bool IsMoving => SigProvider.BooleanOutput["Is Moving"].BoolValue;

        public UIDynamicButtonListItem SelectedItem =>
            _items.ContainsKey(_selectedItemIndex) ? _items[_selectedItemIndex] : null;

        public void ScrollToItem(ushort item)
        {
            SigProvider.UShortInput["Scroll To Item"].UShortValue = item;
        }

        private void SetNumberOfItems(ushort items)
        {
            SigProvider.UShortInput["Set Number of Items"].UShortValue = items;
        }

        public IEnumerator<UIDynamicButtonListItem> GetEnumerator()
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
                }

                SetNumberOfItems(_count);
            }

            OnSelectedItemChange(this);
        }

        public void ClearList()
        {
            ClearList(false);
        }

        public uint AddItem(object linkedObject, bool holdOffSettingListSize)
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
            item.Feedback = false;
            item.LinkedObject = linkedObject;

            if (!holdOffSettingListSize)
                SetNumberOfItems(_count);

            return _count;
        }

        public uint AddItem(object linkedObject)
        {
            return AddItem(linkedObject, false);
        }

        public uint AddItem(string title, object linkedObject, bool holdOffSettingListSize)
        {
            var index = AddItem(linkedObject, holdOffSettingListSize);

            var item = this[index];
            item.Text = title;

            return index;
        }


        public uint AddItem(string title, object linkedObject)
        {
            var index = AddItem(linkedObject);

            var item = this[index];
            item.Text = title;

            return index;
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

        public void SetSelectedItem(UIDynamicButtonListItem item)
        {
            foreach (var listItem in this.Where(i => i != item))
            {
                listItem.Feedback = false;
            }

            if (item != null)
            {
                item.Feedback = true;
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
                listItem.Feedback = false;
            }

            _selectedItemIndex = 0;
            OnSelectedItemChange(this);
        }

        private void OnSelectedItemChange(UIDynamicButtonList list)
        {
            SelectedItemChange?.Invoke(list);
        }

        private void OnIsMovingChange(UIDynamicButtonList list, bool ismoving)
        {
            IsMovingChange?.Invoke(list, ismoving);
        }

        private void OnSigChange(SigProviderDevice sigProviderDevice, SigEventArgs args)
        {
            if (args.Sig.Type != eSigType.Bool || args.Sig.Name != "Is Moving") return;
            OnIsMovingChange(this, args.Sig.BoolValue);
        }
    }

    public delegate void UIDynamicButtonListSelectedItemChangeEventHander(UIDynamicButtonList list);

    public delegate void UIDynamicButtonListIsMovingChangedEventHandler(UIDynamicButtonList list, bool isMoving);
}