using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Game2048.Properties;

namespace Game2048
{
    public static class Fun
    {
        public static readonly object LockObject = new object();
        public static readonly Random Rand = new Random();

        public static Direction ConvertKeyToDirection(Keys key)
        {
            switch (key)
            {
                case Keys.Down:
                    return Direction.Down;
                case Keys.Left:
                    return Direction.Left;
                case Keys.Right:
                    return Direction.Right;
                case Keys.Up:
                    return Direction.Up;
            }

            return Direction.NotSet;
        }

        public static List<Block> CloneList(List<Block> lst, bool enableNotMainBlocks = true)
        {
            List<Block> ret = new List<Block>();
            lock (LockObject)
            {
                lst.ForEach(b =>
                {
                    if (enableNotMainBlocks || b.IsMain) ret.Add(b.Clone());
                });
            }

            return ret;
        }

        public static FontInfo GetFontInfo(string s, float cellSize, string fontName, Graphics g)
        {
            float min = 0.1f, max = 200, med = 50;
            SizeF size = new SizeF(1, 1);
            Font f = null;

            while (max - min > 0.5)
            {
                med = (max + min) / 2;
                f = new Font(fontName, med);
                size = g.MeasureString(s, f);
                if (size.Width > cellSize || size.Height > cellSize)
                    max = med;
                else
                    min = med;
            }

            return new FontInfo(f, med, size.Width, size.Height);
        }

        public static SettingsPack LoadSettings()
        {
            SettingsPack sp = Settings.Default.SettingsPack;

            if (sp != null) return sp;

            sp = new SettingsPack();
            sp.Apply();
            return sp;
        }
    }
}