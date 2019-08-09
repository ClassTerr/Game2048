// RazorGDIPainter library - ultrafast 2D painting. See test applications
// on http://razorgdipainter.codeplex.com/
//   (c) Mokrov Ivan
// special for habrahabr.ru
// under MIT license

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Game2048
{
    public class RazorPainter : IDisposable
    {
        private Bitmapinfo _bi;
        private GCHandle _gcHandle;
        private int[] _pArray;

        public int Width { get; private set; }

        public int Height { get; private set; }

        public void Dispose()
        {
            if (_gcHandle.IsAllocated)
                _gcHandle.Free();
            GC.SuppressFinalize(this);
        }

        [DllImport("gdi32")]
        private static extern int SetDIBitsToDevice(HandleRef hDc, int xDest, int yDest, int dwWidth, int dwHeight,
            int xSrc, int ySrc, int uStartScan, int cScanLines, ref int lpvBits, ref Bitmapinfo lpbmi, uint fuColorUse);

        ~RazorPainter()
        {
            Dispose();
        }

        private void Realloc(int width, int height)
        {
            if (_gcHandle.IsAllocated)
                _gcHandle.Free();

            Width = width;
            Height = height;

            _pArray = new int[Width * Height];
            _gcHandle = GCHandle.Alloc(_pArray, GCHandleType.Pinned);
            _bi = new Bitmapinfo
            {
                biHeader =
                {
                    bihBitCount = 32,
                    bihPlanes = 1,
                    bihSize = 40,
                    bihWidth = Width,
                    bihHeight = -Height,
                    bihSizeImage = (Width * Height) << 2
                }
            };
        }

        public void Paint(HandleRef hRef, Bitmap bitmap)
        {
            if (bitmap == null || bitmap.Width == 0 || bitmap.Height == 0)
            {
                Console.WriteLine("impossiburu Bitmap at Paint() in RazorPainter");
                return;
            }

            if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
            {
                Console.WriteLine("PixelFormat must be Format32bppArgb at Paint() in RazorPainter");
                return;
            }

            if (bitmap.Width != Width || bitmap.Height != Height)
                Realloc(bitmap.Width, bitmap.Height);

            //_gcHandle = GCHandle.Alloc(_pArray, GCHandleType.Pinned);

            BitmapData bd = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            Marshal.Copy(bd.Scan0, _pArray, 0, Width * Height);
            SetDIBitsToDevice(hRef, 0, 0, Width, Height, 0, 0, 0, Height, ref _pArray[0], ref _bi, 0);
            bitmap.UnlockBits(bd);

            //if (_gcHandle.IsAllocated)
            //	_gcHandle.Free();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Bitmapinfoheader
        {
            public int bihSize;
            public int bihWidth;
            public int bihHeight;
            public short bihPlanes;
            public short bihBitCount;
            public readonly int bihCompression;
            public int bihSizeImage;
            public readonly double bihXPelsPerMeter;
            public readonly double bihClrUsed;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Bitmapinfo
        {
            public Bitmapinfoheader biHeader;
            public readonly int biColors;
        }
    }
}