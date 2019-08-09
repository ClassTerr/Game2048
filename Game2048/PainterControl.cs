// Test control fronend for WindowsForms for RazorGDIPainter library
//   (c) Mokrov Ivan
// special for habrahabr.ru
// under MIT license

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Game2048
{
    public class PainterControl : Panel
    {
        private readonly Graphics _hDcGraphics;

        private readonly HandleRef _hDcRef;
        private readonly RazorPainter _rp;

        /// <summary>
        ///     Lock it to avoid resize/repaint race
        /// </summary>
        public readonly object RazorLock = new object();

        public PainterControl()
        {
            InitializeComponent();


            MinimumSize = new Size(1, 1);

            SetStyle(ControlStyles.DoubleBuffer, false);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
//			SetStyle(ControlStyles.Opaque, true);

            _hDcGraphics = CreateGraphics();
            _hDcRef = new HandleRef(_hDcGraphics, _hDcGraphics.GetHdc());

            _rp = new RazorPainter();
            Bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            Graphics = Graphics.FromImage(Bitmap);
            Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
            Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Resize += ResizeProc;
            ResizeProc(null, null);
        }

        /// <summary>
        ///     root Bitmap
        /// </summary>
        public Bitmap Bitmap { get; private set; }

        /// <summary>
        ///     Graphics object to paint on RazorBMP
        /// </summary>
        public Graphics Graphics { get; private set; }

        private void ResizeProc(object sender, EventArgs e)
        {
            lock (RazorLock)
            {
                if (Graphics != null) Graphics.Dispose();
                if (Bitmap != null) Bitmap.Dispose();
                Bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
                Graphics = Graphics.FromImage(Bitmap);
                Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            }
        }

        /// <summary>
        ///     After all in-memory paint on RazorGFX, call it to display it on control
        /// </summary>
        public void RazorPaint()
        {
            _rp.Paint(_hDcRef, Bitmap);
        }

        #region Component Designer generated code

        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            lock (this)
            {
                Graphics?.Dispose();
                Bitmap?.Dispose();
                _hDcGraphics?.Dispose();
                _rp?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // RazorPainterWFCtl
            // 
            this.Name = "RazorPainterWFCtl";
            this.ResumeLayout(false);
        }

        #endregion
    }
}