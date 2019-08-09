using System.Drawing;

namespace Game2048
{
    public struct FontInfo
    {
        public float Size { get; }
        public float Width { get; }
        public float Height { get; }
        public Font Font { get; }

        public FontInfo(Font f, float sz, float w, float h)
        {
            Size = sz;
            Width = w;
            Height = h;
            Font = f;
        }
    }
}