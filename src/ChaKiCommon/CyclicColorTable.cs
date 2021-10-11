using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ChaKi.Common
{
    public class CyclicColorTable
    {
        private static List<Color> m_Colors;
        private static List<SolidBrush> m_Brushes;

        static CyclicColorTable()
        {
            m_Colors = new List<Color>();
            m_Colors.Add(Color.FromArgb(100, 0x9A, 0xCD, 0x32));	// LimeGreen
            m_Colors.Add(Color.FromArgb(100, 0xFF, 0xA5, 0x00));  // Orange
            m_Colors.Add(Color.FromArgb(100, 0xAD, 0xD8, 0xE6));	// LightBlue
            m_Colors.Add(Color.FromArgb(100, 0xBD, 0xB7, 0x6B));	// DarkKhaki
            m_Colors.Add(Color.FromArgb(100, 0xFF, 0xFF, 0x00));	// Yellow
            m_Colors.Add(Color.FromArgb(100, 0xFF, 0x14, 0x93));	// DeepPink
            m_Colors.Add(Color.FromArgb(100, 0xEE, 0x82, 0xEE));	// Violet
            m_Colors.Add(Color.FromArgb(100, 0xFF, 0xDE, 0xAD));	// NavajoWhite

            m_Brushes = new List<SolidBrush>();
            foreach (Color c in m_Colors)
            {
                m_Brushes.Add(new SolidBrush(Color.FromArgb(170, c.R, c.G, c.B)));
            }
        }

        static public Color GetColor(int index)
        {
            return m_Colors[index % m_Colors.Count];
        }

        static public Brush GetBrush(int index)
        {
            return m_Brushes[index % m_Brushes.Count];
        }
    }
}
