
namespace ChaKi.GUICommon
{
    public interface IChaKiView
    {
        void SetModel(object model);
        void SetVisible(bool f);

        void CutToClipboard();
        void CopyToClipboard();
        void PasteFromClipboard();

        bool CanCut { get; }
        bool CanCopy { get; }
        bool CanPaste { get; }
    }
}
