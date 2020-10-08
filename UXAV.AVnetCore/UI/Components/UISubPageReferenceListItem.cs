using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using UXAV.AVnetCore.Models;
using UXAV.Logging;

namespace UXAV.AVnetCore.UI.Components
{
    public abstract class UISubPageReferenceListItem : UIObject, IVisibleItem, IEnableItem, IGenericItem
    {
        private string _name;

        protected UISubPageReferenceListItem(UISubPageReferenceList list, uint id)
            : base(list)
        {
            try
            {
                List = list;
                Id = id;

                VisibleJoinNumber = SigProvider.BooleanInput[$"Item {id} Visible"].Number;
                EnableJoinNumber = SigProvider.BooleanInput[$"Item {id} Enable"].Number;

                if (list.DigitalJoinIncrement > 0)
                {
                    uint count = 0;
                    var inputSigs = new Dictionary<uint, BoolInputSig>();
                    var outputSigs = new Dictionary<uint, BoolOutputSig>();
                    for (var i = id * list.DigitalJoinIncrement - (list.DigitalJoinIncrement - 1);
                        i <= id * list.DigitalJoinIncrement;
                        i++)
                    {
                        count++;
                        inputSigs[count] = SigProvider.BooleanInput[$"fb{i}"];
                        outputSigs[count] = SigProvider.BooleanOutput[$"press{i}"];
                    }

                    BoolInputSigs = new ReadOnlyDictionary<uint, BoolInputSig>(inputSigs);
                    BoolOutputSigs = new ReadOnlyDictionary<uint, BoolOutputSig>(outputSigs);
                }

                if (list.AnalogJoinIncrement > 0)
                {
                    uint count = 0;
                    var inputSigs = new Dictionary<uint, UShortInputSig>();
                    var outputSigs = new Dictionary<uint, UShortOutputSig>();
                    for (var i = id * list.AnalogJoinIncrement - (list.AnalogJoinIncrement - 1);
                        i <= id * list.AnalogJoinIncrement;
                        i++)
                    {
                        count++;
                        inputSigs[count] = SigProvider.UShortInput[$"an_fb{i}"];
                        outputSigs[count] = SigProvider.UShortOutput[$"an_act{i}"];
                    }

                    UShortInputSigs = new ReadOnlyDictionary<uint, UShortInputSig>(inputSigs);
                    UShortOutputSigs = new ReadOnlyDictionary<uint, UShortOutputSig>(outputSigs);
                }

                if (list.SerialJoinIncrement > 0)
                {
                    uint count = 0;
                    var inputSigs = new Dictionary<uint, StringInputSig>();
                    var outputSigs = new Dictionary<uint, StringOutputSig>();
                    for (var i = id * list.SerialJoinIncrement - (list.SerialJoinIncrement - 1);
                        i <= id * list.SerialJoinIncrement;
                        i++)
                    {
                        count++;
                        inputSigs[count] = SigProvider.StringInput[$"text-o{i}"];
                        outputSigs[count] = SigProvider.StringOutput[$"text-i{i}"];
                    }

                    StringInputSigs = new ReadOnlyDictionary<uint, StringInputSig>(inputSigs);
                    StringOutputSigs = new ReadOnlyDictionary<uint, StringOutputSig>(outputSigs);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }


        public event VisibilityChangeEventHandler VisibilityChanged;

        protected virtual void OnVisibilityChanged(IVisibleItem item, VisibilityChangeEventArgs args)
        {
            VisibilityChanged?.Invoke(item, args);
        }

        public UISubPageReferenceList List { get; }

        public ReadOnlyDictionary<uint, BoolInputSig> BoolInputSigs { get; }

        public ReadOnlyDictionary<uint, BoolOutputSig> BoolOutputSigs { get; }

        public ReadOnlyDictionary<uint, StringInputSig> StringInputSigs { get; }

        public ReadOnlyDictionary<uint, StringOutputSig> StringOutputSigs { get; }

        public ReadOnlyDictionary<uint, UShortInputSig> UShortInputSigs { get; }

        public ReadOnlyDictionary<uint, UShortOutputSig> UShortOutputSigs { get; }

        public uint Id { get; }

        public uint EnableJoinNumber { get; }

        public void SetId(uint id)
        {
            throw new NotSupportedException("Cannot set Id value");
        }

        public string Name
        {
            get
            {
                if (StringInputSigs.ContainsKey(1) && !string.IsNullOrEmpty(StringInputSigs[1].StringValue))
                {
                    return StringInputSigs[1].StringValue;
                }

                return !string.IsNullOrEmpty(_name) ? _name : $"Item {Id}";
            }
            set
            {
                if (StringInputSigs.ContainsKey(1))
                {
                    StringInputSigs[1].StringValue = value;
                    return;
                }

                _name = value;
            }
        }

        public bool Enabled
        {
            get => EnableJoinNumber == 0 || SigProvider.BooleanInput[EnableJoinNumber].BoolValue;
            set
            {
                if (EnableJoinNumber == 0) return;
                SigProvider.BooleanInput[EnableJoinNumber].BoolValue = value;
            }
        }

        public uint VisibleJoinNumber { get; }

        public bool Visible
        {
            get => VisibleJoinNumber == 0 || SigProvider.BooleanInput[VisibleJoinNumber].BoolValue;
            set
            {
                if (VisibleJoinNumber == 0 || SigProvider.BooleanInput[VisibleJoinNumber].BoolValue == value) return;

                RequestedVisibleState = value;

                OnVisibilityChanged(this,
                    new VisibilityChangeEventArgs(value, value
                        ? VisibilityChangeEventType.WillShow
                        : VisibilityChangeEventType.WillHide));

                SigProvider.BooleanInput[VisibleJoinNumber].BoolValue = value;

                OnVisibilityChanged(this,
                    new VisibilityChangeEventArgs(value, value
                        ? VisibilityChangeEventType.DidShow
                        : VisibilityChangeEventType.DidHide));
            }
        }

        public bool RequestedVisibleState { get; private set; }

        public virtual object LinkedObject { get; set; }


        public bool IsLastItem => Id == List.ItemsAddedCount;

        public void Show()
        {
            Visible = true;
        }

        public void Hide()
        {
            Visible = false;
        }

        public void Enable()
        {
            Enabled = true;
        }

        public void Disable()
        {
            Enabled = false;
        }

        internal void SetFeedbackInternal(bool value)
        {
            SetFeedback(value);
        }

        public abstract void SetFeedback(bool value);
    }
}