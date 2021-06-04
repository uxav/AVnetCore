namespace UXAV.AVnet.Core.UI.Components
{
    public interface ITextItem
    {
        uint SerialJoinNumber { get; }
        void SetText(string text);
        string Text { get; set; }
    }
}