namespace UXAV.AVnetCore.UI
{
    public interface ITextItem
    {
        uint SerialJoinNumber { get; }
        void SetText(string text);
        string Text { get; set; }
    }
}