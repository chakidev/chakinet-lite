using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Views.KwicView
{
    internal enum HitType
    {
        None,
        AtSentence,     // 文(Text, Left, Center, Right)の範囲でヒットした
        AtCheckBox,     // Checkboxにヒットした
        AtLine,         // 行の上記以外の部分にヒットした
    }
}
