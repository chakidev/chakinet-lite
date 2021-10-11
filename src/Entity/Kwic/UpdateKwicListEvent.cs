using System;

namespace ChaKi.Entity.Kwic
{
    public class UpdateKwicListEventArgs : EventArgs
    {
        public bool RedrawNeeded { get; set; }
        public bool AnnotationChanged { get; set; }
        public bool Sorted { get; set; }

        public UpdateKwicListEventArgs()
        {
            this.RedrawNeeded = false;
            this.AnnotationChanged = false;
            this.Sorted = false;
        }

        public UpdateKwicListEventArgs(bool redraw, bool annotationChanged, bool sorted)
        {
            this.RedrawNeeded = redraw;
            this.AnnotationChanged = annotationChanged;
            this.Sorted = sorted;
        }
    }

    public delegate void UpdateKwicListEventHandler(object sender, UpdateKwicListEventArgs e);
}
