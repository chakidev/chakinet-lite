using System;

namespace ChaKi.Panels
{
    internal partial class FilterButton {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal FilterButton() {
            this.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.IsOn = false;
            this.Image = FilterOff;
            this.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.Size = new System.Drawing.Size(24, 24);
            this.Text = "Filter";
        }
        
        /// <summary>
        ///   このクラスで使用されているキャッシュされた ResourceManager インスタンスを返します。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("ChaKi.Panels.FilterButton", typeof(FilterButton).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   厳密に型指定されたこのリソース クラスを使用して、すべての検索リソースに対し、
        ///   現在のスレッドの CurrentUICulture プロパティをオーバーライドします。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        internal static System.Drawing.Bitmap FilterOff {
            get {
                object obj = ResourceManager.GetObject("FilterOff", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        internal static System.Drawing.Bitmap FilterOn {
            get {
                object obj = ResourceManager.GetObject("FilterOn", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
    }
}
