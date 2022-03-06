namespace UXAV.AVnet.Core.UI.Components
{
    public interface ITextItem
    {
        uint SerialJoinNumber { get; }
        string Text { get; set; }
        void SetText(string text);
    }
}