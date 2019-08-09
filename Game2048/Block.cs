using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Drawing2D;
using Game2048.Properties;

namespace Game2048
{
    [Serializable]
    [SettingsSerializeAs(SettingsSerializeAs.Binary)]
    public class Block : IComparable<Block>
    {
        private int _realIndex;
        private int _renderIndex;
        public Color Color = Color.Orchid;
        public bool IsMain = true;
        public bool IsNeedToRemove = false;
        public int JagCount = 10;
        public bool NowMerged = false;
        public bool Processed = false;
        public float ScaleCoefficient = 0;
        public Block TargetBlock = null;
        public Point TargetCell;
        public PointF TargetPoint = new PointF();


        public string Text = "2";
        public float X;
        public int XGrid = 0;
        public float Y;
        public int YGrid = 0;
        public float Width { get; } = 100;
        public float Height { get; } = 100;

        public int RenderIndex
        {
            get => _renderIndex;
            set
            {
                _renderIndex = value;
                switch (_renderIndex)
                {
                    case 0:
                        Color = Color.IndianRed;
                        break;
                    case 1:
                        Color = Color.OrangeRed;
                        break;
                    case 2:
                        Color = Color.Orange;
                        break;
                    case 3:
                        Color = Color.LightGoldenrodYellow;
                        break;
                    case 4:
                        Color = Color.Yellow;
                        break;
                    case 5:
                        Color = Color.YellowGreen;
                        break;
                    case 6:
                        Color = Color.Green;
                        break;
                    case 7:
                        Color = Color.SeaGreen;
                        break;
                    case 8:
                        Color = Color.Blue;
                        break;
                    case 9:
                        Color = Color.Purple;
                        break;
                    case 10:
                        Color = Color.Violet;
                        break;
                    case 11:
                        Color = Color.PaleVioletRed;
                        break;
                    case 12:
                        Color = Color.DarkViolet;
                        break;
                    case 13:
                        Color = Color.Gray;
                        break;
                    case 14:
                        Color = Color.Black;
                        break;
                    case 15:
                        Color = Color.HotPink;
                        break;
                }

                Text = ((long) Math.Pow(2, _realIndex + 1)).ToString();
            }
        }

        public int Index
        {
            get => _realIndex;
            set => _realIndex = value;
        }

        public PointF Location
        {
            get => new PointF(X, Y);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        public int CompareTo(Block other)
        {
            if (this > other) return 1;

            if (this < other) return -1;

            return 0;
        }

        public static bool operator >(Block a, Block b)
        {
            if (a == null || b == null) return true;

            if (a.IsMain != b.IsMain) return a.IsMain;

            return a.Index > b.Index;
        }

        public static bool operator <(Block a, Block b)
        {
            if (a == null || b == null) return false;

            if (a.IsMain != b.IsMain) return b.IsMain;

            return a.Index < b.Index;
        }

        public override string ToString()
        {
            return TargetCell + " " + Text;
        }

        public Block Clone()
        {
            return MemberwiseClone() as Block;
        }

        #region Constructors

        public Block()
        {
            Index = RenderIndex = 0;
        }

        public Block(int index, int x, int y)
        {
            TargetCell.X = x;
            TargetCell.Y = y;
            Index = index;
            RenderIndex = index;
        }

        #endregion

        #region Shape

        public GraphicsPath GetSquareShape(float coefficient)
        {
            return GetSquareShape(X, Y, Width, Height, coefficient);
        }

        public GraphicsPath GetSquareShape(float x, float y, float width, float height, float coefficient)
        {
            GraphicsPath path = new GraphicsPath();

            if (width <= 0 || height <= 0) return path;

            if (Math.Abs(coefficient) < 0.0000001)
            {
                path.AddRectangle(new RectangleF(x, y, width, height));
                return path;
            }

            float lDot = coefficient * width;
            float rDot = (1 - coefficient) * width;
            float tDot = coefficient * height;
            float bDot = (1 - coefficient) * height;

            path.FillMode = FillMode.Winding;
            path.AddRectangle(new RectangleF(x, tDot + y, width, bDot - tDot));
            path.AddRectangle(new RectangleF(lDot + x, y, rDot - lDot, height));
            path.AddPie(x + rDot - lDot, y, lDot * 2, tDot * 2, -90, 90);
            path.AddPie(x + rDot - lDot, y + bDot - tDot, lDot * 2, tDot * 2, 0, 90);
            path.AddPie(x, y + bDot - tDot, lDot * 2, tDot * 2, 90, 90);
            path.AddPie(x, y, lDot * 2, tDot * 2, 180, 90);

            return path;
        }

        public GraphicsPath GetStarShape(int jagCount)
        {
            return GetStarShape(X, Y, Width, Height, jagCount);
        }

        public GraphicsPath GetStarShape(float x, float y, float width, float height, int jagCount)
        {
            GraphicsPath path = new GraphicsPath();

            if (width <= 0 || height <= 0) return path;

            double dangle = Math.PI / jagCount;
            double radius1 = width / 2, radius2 = 0.8 * width / 2;
            List<PointF> pts = new List<PointF>();
            for (double angle = 0; angle < 2 * Math.PI; angle += dangle)
            {
                pts.Add(new PointF((float) (x + radius1 + radius1 * Math.Cos(angle)),
                    (float) (y + radius1 + radius1 * Math.Sin(angle))));
                pts.Add(new PointF((float) (x + radius1 + radius2 * Math.Cos(angle + dangle / 2)),
                    (float) (y + radius1 + radius2 * Math.Sin(angle + dangle / 2))));
            }

            path.AddLines(pts.ToArray());

            return path;
        }

        public GraphicsPath GetShape()
        {
            return GetShape(X, Y, Width);
        }

        public GraphicsPath GetShape(float x, float y, float sz)
        {
            return GetShape(x, y, sz, sz,
                Settings.Default.SettingsPack.JagCount,
                Settings.Default.SettingsPack.Coefficient,
                Settings.Default.SettingsPack.RenderStyle);
        }

        public GraphicsPath GetShape(
            float x,
            float y,
            float width,
            float height,
            int jagCount,
            float coefficient,
            RenderStyle rs = RenderStyle.Squares)
        {
            switch (rs)
            {
                case RenderStyle.Squares:
                    return GetSquareShape(x, y, width, height, coefficient);
                case RenderStyle.Stars:
                    return GetStarShape(x, y, width, height, jagCount);
                default:
                    throw new ArgumentOutOfRangeException(nameof(rs), rs, null);
            }
        }

        #endregion
    }
}