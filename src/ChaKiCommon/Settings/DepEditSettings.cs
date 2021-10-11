using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ChaKi.Common.Settings
{
    public class DepEditSettings
    {
        public float FontSize;

        public static readonly float FontSizeDefault = 10.0f;

        public DispModes DispMode;

        public int TopMargin;

        public int LeftMargin;

        public int BunsetsuBoxMargin;

        public int WordBoxMargin;

        public int CharBoxMargin;

        public int LineMargin;

        public double CurveParamX;

        public double CurveParamY;

        public string Background;

        public bool ReverseDepArrowDirection;

        [Obsolete("係り受けと通常のLinkの表示を切り替えるのに使用していたが、前者を下、後者を上にして同時表示するようにしたため使用をやめた.")]
        public bool ShowLinks;

        public LexemeSelectionSettings LexemeSelectionSettings;

        public LexemeSelectionSettings LexemeListCheckDialogSettings;

        public bool ShowHeadInfo;

        // SegmentBoxの重なりを避けるためのオフセットマージン
        public double SegmentBoxLevelMarginX;

        public double SegmentBoxLevelMarginY;

        public static DepEditSettings Current = new DepEditSettings();

        private DepEditSettings()
        {
            this.DispMode = DispModes.Diagonal;
            this.FontSize = 10.0f;
            this.TopMargin = 30;
            this.LeftMargin = 10;
            this.BunsetsuBoxMargin = 8;
            this.WordBoxMargin = 5;
            this.CharBoxMargin = 5;
            this.CurveParamX = 0.05;
            this.CurveParamY = 0.14;
            this.Background = "Linen";
            this.ReverseDepArrowDirection = false;
            this.ShowLinks = false;
            LexemeSelectionSettings = new LexemeSelectionSettings();
            LexemeListCheckDialogSettings = new LexemeSelectionSettings();
            this.ShowHeadInfo = true;
            this.SegmentBoxLevelMarginX = 1.0;
            this.SegmentBoxLevelMarginY = 3.0;
        }

        public DepEditSettings(DepEditSettings src)
            :this()
        {
            this.CopyFrom(src);
        }

        public DepEditSettings Copy()
        {
            DepEditSettings obj = new DepEditSettings();
            obj.CopyFrom(this);
            return obj;
        }

        public void CopyFrom(DepEditSettings src)
        {
            this.DispMode = src.DispMode;
            this.FontSize = src.FontSize;
            this.TopMargin = src.TopMargin;
            this.LeftMargin = src.LeftMargin;
            this.BunsetsuBoxMargin = src.BunsetsuBoxMargin;
            this.WordBoxMargin = src.WordBoxMargin;
            this.CharBoxMargin = src.CharBoxMargin;
            this.CurveParamX = src.CurveParamX;
            this.CurveParamY = src.CurveParamY;
            this.Background = src.Background;
            this.ReverseDepArrowDirection = src.ReverseDepArrowDirection;
            this.ShowLinks = src.ShowLinks;
            this.LexemeSelectionSettings = new LexemeSelectionSettings(src.LexemeSelectionSettings);
            this.LexemeListCheckDialogSettings = new LexemeSelectionSettings(src.LexemeListCheckDialogSettings);
            this.ShowHeadInfo = src.ShowHeadInfo;
            this.SegmentBoxLevelMarginX = src.SegmentBoxLevelMarginX;
            this.SegmentBoxLevelMarginY = src.SegmentBoxLevelMarginY;

        }
    }

    public class LexemeSelectionSettings
    {
        public Rectangle InitialLocation;
        public int[] ColumnWidths;
        public Size POSPropTreeSize;
        public Size CTypePropTreeSize;
        public Size CFormPropTreeSize;

        public LexemeSelectionSettings()
        {
            InitialLocation = new Rectangle(0, 0, 740, 220);
            ColumnWidths = new int[] { 100, 100 };
            POSPropTreeSize = new Size(180, 250);
            CTypePropTreeSize = new Size(180, 250);
            CFormPropTreeSize = new Size(180, 250);
        }

        public LexemeSelectionSettings(LexemeSelectionSettings src)
            :this()
        {
            this.CopyFrom(src);
        }

        public void CopyFrom(LexemeSelectionSettings src)
        {
            this.InitialLocation = src.InitialLocation;
            this.ColumnWidths = src.ColumnWidths;
            this.POSPropTreeSize = src.POSPropTreeSize;
            this.CTypePropTreeSize = src.CTypePropTreeSize;
            this.CFormPropTreeSize = src.CFormPropTreeSize;
        }
    }
    
    public enum DispModes
    {
        None,
        Diagonal,
        Horizontal,
        Morphemes
    }
}
