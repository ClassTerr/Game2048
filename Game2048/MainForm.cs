using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace Game2048
{
    public partial class MainForm : Form
    {
        #region Initialization

        public MainForm()
        {
            InitializeComponent();
            _lockObject = painterControl1.RazorLock;
            _tpf = 1.0 / _fps;

            _settingsPack = Fun.LoadSettings();
            if (_settingsPack.Score == 0)
                NewGame();

            undoToolStripMenuItem.Enabled = _settingsPack.PreviousStep != null;
            Resized(null, null);
            _settingsPack.Blocks.Sort();

            _gameLoopThread = new Thread(GameLoop);
            _gameLoopThread.Start();

            _fpsTimer = new System.Timers.Timer(1000);
            _fpsTimer.Elapsed += (sender1, args) =>
            {
                Upd(delegate
                {
                    _displayFps = _displayFpsPrivate;
                    UpdateCaption();
                    _displayFpsPrivate = 0;
                });
            };
            _fpsTimer.Start();
        }

        #endregion

        #region Variables

        private readonly int _fps = 100;
        private readonly int _borderWidth = 20;
        private readonly float _accelerationCoefficient = 15;
        private readonly float _accelerationCoefficientForNotMain = 30;
        private readonly float _disappearingAccelerationCoefficient = 1;
        private readonly float _resizeSpeed = 0.2f;

        private bool _tryingMode;
        private SettingsPack _settingsPack = new SettingsPack();
        private readonly System.Timers.Timer _fpsTimer;
        private readonly Thread _gameLoopThread;
        private bool _gameIsRunning = true;
        private readonly object _lockObject;
        private int _displayFpsPrivate;
        private int _displayFps;
        private readonly double _tpf;
        private float _fieldSizePx;
        private float _fieldLeft;
        private float _fieldTop;
        private float _cellSize;
        private SettingsForm _settingsForm;
        private readonly List<Block> _disappearingBlocks = new List<Block>();

        #endregion

        #region Core

        private void GameLoop()
        {
            double t = 0;
            double elapsed = 0;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            while (_gameIsRunning)
            {
                t = (_tpf - sw.Elapsed.TotalSeconds) * 1000;
                if (t >= 1)
                    Thread.Sleep((int) t);
                while (_tpf - sw.Elapsed.TotalSeconds > 0) ;


                lock (_lockObject)
                {
                    elapsed = sw.Elapsed.TotalSeconds;

                    sw.Reset();
                    sw.Start();
                    Update(elapsed);
                    Render();
                }
            }

            Thread.CurrentThread.Abort();
        }

        private void Render()
        {
            if (!_gameIsRunning)
                return;


            lock (_lockObject)
            {
                Draw(painterControl1.Graphics);

                painterControl1.Graphics.Flush();
                painterControl1.RazorPaint();
                _displayFpsPrivate++;
            }
        }

        #endregion

        #region Additional Functions

        private void Upd(MethodInvoker callback)
        {
            if (IsDisposed || Disposing)
                return;

            try
            {
                if (InvokeRequired)
                    Invoke(callback);
                else
                    callback();
            }
            catch
            {
            }
        }

        private void UpdateCaption()
        {
            Text = "2048     Очки: " + _settingsPack.Score + "     [FPS: " + _displayFps + "]";
        }

        private PointF CoordOfCell(Point pt)
        {
            return CoordOfCell(pt.X, pt.Y);
        }

        private PointF CoordOfCell(int i, int j)
        {
            return new PointF(_fieldLeft + _cellSize * i, _fieldTop + _cellSize * j);
        }

        /// <summary>
        ///     Обновляет насторойки игры.
        /// </summary>
        public void SettingsChanged()
        {
            lock (_lockObject)
            {
                _settingsPack = Fun.LoadSettings();
                Resized(null, null);
            }
        }

        private void Resized(object sender, EventArgs e)
        {
            lock (_lockObject)
            {
                _fieldSizePx = Math.Min(painterControl1.Width, painterControl1.Height) - _borderWidth * 2;
                _fieldLeft = (painterControl1.Width - _fieldSizePx) / 2;
                _fieldTop = (painterControl1.Height - _fieldSizePx) / 2;
                _cellSize = 1f * _fieldSizePx / _settingsPack.FieldSize;
            }
        }

        #endregion

        #region Game Mecanism

        /// <summary>
        ///     Находит ближайший блок, находящийся в направлении searchDirection от блока current.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="searchDirection"></param>
        /// <returns></returns>
        private Block SearchBlock(Block current, Direction searchDirection)
        {
            Block bl = null;
            Point a = current.TargetCell, b;

            foreach (Block item in _settingsPack.Blocks)
            {
                b = item.TargetCell;
                if (item.IsMain && item != current)
                    switch (searchDirection)
                    {
                        case Direction.Right:
                        case Direction.Left:
                            if (b.Y == a.Y)
                            {
                                if (searchDirection == Direction.Right)
                                {
                                    if (b.X - a.X > 0 && (bl == null || b.X - a.X < bl.TargetCell.X - a.X))
                                        bl = item;
                                }
                                else if (b.X - a.X < 0 && (bl == null || b.X - a.X > bl.TargetCell.X - a.X))
                                {
                                    bl = item;
                                }
                            }

                            break;
                        case Direction.Up:
                        case Direction.Down:
                            if (b.X == a.X)
                            {
                                if (searchDirection == Direction.Down)
                                {
                                    if (b.Y - a.Y > 0 && (bl == null || b.Y - a.Y < bl.TargetCell.Y - a.Y))
                                        bl = item;
                                }
                                else if (b.Y - a.Y < 0 && (bl == null || b.Y - a.Y > bl.TargetCell.Y - a.Y))
                                {
                                    bl = item;
                                }
                            }

                            break;
                        case Direction.NotSet:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(searchDirection), searchDirection, null);
                    }
            }

            return bl;
        }

        /// <summary>
        ///     Передвинуть указанный блок в нужном направлении
        /// </summary>
        /// <param name="item"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private bool MoveBlock(Block item, Direction dir)
        {
            Point startPt = item.TargetCell;
            Block bl = SearchBlock(item, dir);
            bool ret = false;

            if (bl == null)
            {
                switch (dir)
                {
                    case Direction.Right:
                        item.TargetCell.X = _settingsPack.FieldSize - 1;
                        break;
                    case Direction.Up:
                        item.TargetCell.Y = 0;
                        break;
                    case Direction.Left:
                        item.TargetCell.X = 0;
                        break;
                    case Direction.Down:
                        item.TargetCell.Y = _settingsPack.FieldSize - 1;
                        break;
                }
            }
            else
            {
                if (!bl.Processed) ret |= MoveBlock(bl, dir);

                if (bl.Index == item.Index && !bl.NowMerged)
                {
                    //их можно соеденить
                    item.IsMain = false;
                    item.TargetBlock = bl;
                    item.Index++;
                    bl.Index++;
                    item.TargetCell = bl.TargetCell;
                    item.NowMerged = bl.NowMerged = true;
                    if (!_tryingMode) _settingsPack.Score += (int) Math.Pow(2, bl.Index + 1);
                }
                else
                {
                    switch (dir)
                    {
                        case Direction.Right:
                            item.TargetCell.X = bl.TargetCell.X - 1;
                            break;
                        case Direction.Up:
                            item.TargetCell.Y = bl.TargetCell.Y + 1;
                            break;
                        case Direction.Left:
                            item.TargetCell.X = bl.TargetCell.X + 1;
                            break;
                        case Direction.Down:
                            item.TargetCell.Y = bl.TargetCell.Y - 1;
                            break;
                        case Direction.NotSet:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
                    }
                }
            }

            item.Processed = true;
            return ret || startPt != item.TargetCell;
        }

        /// <summary>
        ///     Обработчик нажатия клавиш
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            bool moved;
            List<Block> prStep = Fun.CloneList(_settingsPack.Blocks, false);
            long prScore = _settingsPack.Score;
            lock (_lockObject)
            {
                //Update(0.01);;
                moved = false;
                foreach (Block b in _settingsPack.Blocks)
                    if (!b.Processed && b.IsMain)
                        moved |= MoveBlock(b, Fun.ConvertKeyToDirection(e.KeyCode));

                foreach (Block b in _settingsPack.Blocks) b.Processed = b.NowMerged = false;

                _settingsPack.Blocks.Sort();
            }

            if (moved)
            {
                GenerateNew();
                _settingsPack.PreviousScore = prScore;
                _settingsPack.PreviousStep = prStep;
                undoToolStripMenuItem.Enabled = true;
            }

            UpdateCaption();
        }

        /// <summary>
        ///     Часть проверки на проигрыш
        /// </summary>
        /// <param name="blocks"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private bool TryMove(List<Block> blocks, Direction dir)
        {
            lock (_lockObject)
            {
                _tryingMode = true;
                bool ret = false;

                _settingsPack.Blocks.Clear();
                blocks.ForEach(b => { _settingsPack.Blocks.Add(b.Clone()); });
                _settingsPack.Blocks.ForEach(b => { b.Processed = false; });

                foreach (Block b in _settingsPack.Blocks)
                    if (!b.Processed && b.IsMain)
                        ret |= MoveBlock(b, dir);

                _tryingMode = false;
                return ret;
            }
        }

        /// <summary>
        ///     Проверка на проигрыш
        /// </summary>
        /// <returns></returns>
        private bool StepsAreAvailable()
        {
            lock (_lockObject)
            {
                bool ret = false;
                List<Block> original = _settingsPack.Blocks;
                _settingsPack.Blocks = new List<Block>();
                original.ForEach(b => { _settingsPack.Blocks.Add(b.Clone()); });

                ret |= TryMove(original, Direction.Down);
                ret |= TryMove(original, Direction.Left);
                ret |= TryMove(original, Direction.Right);
                ret |= TryMove(original, Direction.Up);

                _settingsPack.Blocks = original;

                return ret || _settingsPack.Blocks.Count == 0;
            }
        }

        /// <summary>
        ///     Создаёт новый блок.
        /// </summary>
        private void GenerateNew()
        {
            lock (_lockObject)
            {
                List<bool> lst = new List<bool>();
                List<Point> l = new List<Point>();
                for (int i = _settingsPack.FieldSize * _settingsPack.FieldSize - 1; i >= 0; i--) lst.Add(true);

                foreach (Block b in _settingsPack.Blocks)
                    if (b.IsMain)
                        lst[b.TargetCell.X + b.TargetCell.Y * _settingsPack.FieldSize] = false;

                for (int i = 0; i < lst.Count; i++)
                    if (lst[i])
                        l.Add(new Point(i % _settingsPack.FieldSize, i / _settingsPack.FieldSize));

                Point spawnPt = l[Fun.Rand.Next(l.Count)];

                Block bl = new Block(Fun.Rand.Next(2), spawnPt.X, spawnPt.Y);
                bl.Location = CoordOfCell(spawnPt);

                _settingsPack.Blocks.Add(bl);
                if (!_tryingMode) _settingsPack.Score += (int) Math.Pow(2, bl.Index + 1);
            }

            if (!StepsAreAvailable()) Loose();
        }

        private void Undo()
        {
            lock (_lockObject)
            {
                undoToolStripMenuItem.Enabled = false;
                foreach (Block b in _settingsPack.Blocks)
                {
                    float rad = 1.5f * Math.Max(painterControl1.Width, painterControl1.Height);
                    float renderSize = _cellSize * b.ScaleCoefficient;
                    PointF center = new PointF(1f * painterControl1.Width / 2, 1f * painterControl1.Height / 2);
                    float x = b.X - center.X + renderSize / 2;
                    float y = b.Y - center.Y + renderSize / 2;

                    PointF to = new PointF();

                    if (Math.Abs(x) > Math.Abs(y))
                    {
                        to.Y = y / Math.Abs(x) * rad + center.X - renderSize / 2;
                        to.X = rad * Math.Sign(x) + center.X - renderSize / 2;
                    }
                    else
                    {
                        to.X = x / Math.Abs(y) * rad + center.X - renderSize / 2;
                        to.Y = rad * Math.Sign(y) + center.X - renderSize / 2;
                    }


                    b.TargetPoint = to;
                }

                _disappearingBlocks.AddRange(_settingsPack.Blocks);
                _settingsPack.Blocks = Fun.CloneList(_settingsPack.PreviousStep, false);
                foreach (Block item in _settingsPack.Blocks)
                {
                    item.Location = CoordOfCell(item.TargetCell);
                    item.ScaleCoefficient = 0;
                    item.RenderIndex = item.Index;
                }

                _settingsPack.Score = _settingsPack.PreviousScore;
                _settingsPack.PreviousStep = null;
            }
        }

        public void NewGame()
        {
            lock (_lockObject)
            {
                _settingsPack.PreviousScore = 0;
                _settingsPack.PreviousStep = null;
                undoToolStripMenuItem.Enabled = false;
                _settingsPack.Score = 0;
                _settingsPack.Blocks.Clear();
                GenerateNew();
                GenerateNew();
            }
        }

        private void Loose()
        {
            MessageBox.Show(null, "Вы проиграли! Попробуйте снова ;)\n• Ваш счёт: " +
                                  _settingsPack.Score, "2048", MessageBoxButtons.OK, MessageBoxIcon.Information);
            NewGame();
        }

        /// <summary>
        ///     Производит обновление состояния игры. Здесь должны происходить все действия, направленые на перемещение блоков
        /// </summary>
        /// <param name="dTime"></param>
        private void Update(double dTime)
        {
            double dt;

            for (int i = 0; i < _disappearingBlocks.Count; i++)
            {
                PointF to = _disappearingBlocks[i].TargetPoint;

                _disappearingBlocks[i].X += (to.X - _disappearingBlocks[i].X) * (float) dTime *
                                            _disappearingAccelerationCoefficient;
                _disappearingBlocks[i].Y += (to.Y - _disappearingBlocks[i].Y) * (float) dTime *
                                            _disappearingAccelerationCoefficient;

                if (Math.Abs(_disappearingBlocks[i].X - _disappearingBlocks[i].TargetPoint.X) < 100 &&
                    Math.Abs(_disappearingBlocks[i].Y - _disappearingBlocks[i].TargetPoint.Y) < 100)
                    _disappearingBlocks.RemoveAt(i--);
            }

            for (int i = 0; i < _settingsPack.Blocks.Count; i++)
            {
                dt = dTime;
                if (_settingsPack.Blocks[i].IsNeedToRemove)
                {
                    _settingsPack.Blocks.RemoveAt(i--);
                    continue;
                }

                PointF to;
                if (_settingsPack.Blocks[i].IsMain)
                    to = CoordOfCell(_settingsPack.Blocks[i].TargetCell);
                else
                    to = _settingsPack.Blocks[i].TargetBlock.Location;

                _settingsPack.Blocks[i].ScaleCoefficient = Math.Min(1,
                    _settingsPack.Blocks[i].ScaleCoefficient +
                    (1f - _settingsPack.Blocks[i].ScaleCoefficient) * _resizeSpeed);

                if (dt < 1)
                    while (dt > 0)
                    {
                        float k = (float) Math.Min(dt, 0.01) * (_settingsPack.Blocks[i].IsMain
                                      ? _accelerationCoefficient
                                      : _accelerationCoefficientForNotMain);
                        _settingsPack.Blocks[i].X += (to.X - _settingsPack.Blocks[i].X) * k;
                        _settingsPack.Blocks[i].Y += (to.Y - _settingsPack.Blocks[i].Y) * k;
                        dt -= 0.01;
                    }

                if (!_settingsPack.Blocks[i].IsMain && Math.Abs(_settingsPack.Blocks[i].X - to.X) < _cellSize / 10 &&
                    Math.Abs(_settingsPack.Blocks[i].Y - to.Y) < _cellSize / 10)
                {
                    _settingsPack.Blocks[i].TargetBlock.RenderIndex++;
                    _settingsPack.Blocks.RemoveAt(i--);
                }
            }
        }

        /// <summary>
        ///     Здесь происходит отрисовка всего в игре.
        /// </summary>
        /// <param name="g"></param>
        private void Draw(Graphics g)
        {
            lock (_lockObject)
            {
                g.Clear(_settingsPack.BackCol);

                g.FillRectangle(new SolidBrush(_settingsPack.FieldCol), _fieldLeft, _fieldTop,
                    _settingsPack.FieldSize * _cellSize, _settingsPack.FieldSize * _cellSize);
                for (int i = 0; i <= _settingsPack.FieldSize; i++)
                    g.DrawLine(new Pen(_settingsPack.GridCol), _fieldLeft + i * _cellSize, _fieldTop,
                        _fieldLeft + i * _cellSize, _fieldTop + _settingsPack.FieldSize * _cellSize);

                for (int i = 0; i <= _settingsPack.FieldSize; i++)
                    g.DrawLine(new Pen(_settingsPack.GridCol), _fieldLeft, _fieldTop + i * _cellSize,
                        _fieldLeft + _cellSize * _settingsPack.FieldSize, _fieldTop + i * _cellSize);

                float l, t, renderSize, centerX, centerY;
                foreach (Block b in _settingsPack.Blocks)
                {
                    int opacity = 255;
                    renderSize = _cellSize * b.ScaleCoefficient;
                    centerX = b.X + _cellSize / 2;
                    centerY = b.Y + _cellSize / 2;
                    l = centerX - renderSize / 2;
                    t = centerY - renderSize / 2;

                    Color col;
                    if (!b.IsMain)
                    {
                        double dist =
                            Math.Sqrt(Math.Pow(b.X - b.TargetBlock.X, 2) + Math.Pow(b.Y - b.TargetBlock.Y, 2));
                        double targ = dist / _cellSize + 0.5f;
                        b.ScaleCoefficient = (float) Math.Min(1, targ);
                        opacity = (int) Math.Min(255, targ * 255);
                        col = Color.FromArgb(opacity, b.Color);
                    }
                    else
                    {
                        col = b.Color;
                    }

                    g.FillPath(new SolidBrush(col), b.GetShape(l, t, renderSize));
                    FontInfo fi = Fun.GetFontInfo(b.Text, renderSize, _settingsPack.FontName, g);
                    Font f = new Font(_settingsPack.FontName, fi.Size);

                    if (fi.Width > fi.Height)
                        t += (renderSize - fi.Height) / 2;
                    else
                        l += (renderSize - fi.Width) / 2;


                    g.DrawString(b.Text, f, new SolidBrush(Color.FromArgb(opacity, Color.Black)), l, t);
                }

                foreach (Block b in _disappearingBlocks)
                {
                    renderSize = _cellSize * b.ScaleCoefficient;
                    centerX = b.X + _cellSize / 2;
                    centerY = b.Y + _cellSize / 2;
                    l = centerX - renderSize / 2;
                    t = centerY - renderSize / 2;
                    g.FillPath(new SolidBrush(b.Color), b.GetShape(l, t, renderSize));
                    FontInfo fi = Fun.GetFontInfo(b.Text, renderSize, _settingsPack.FontName, g);
                    Font f = new Font(_settingsPack.FontName, fi.Size);

                    if (fi.Width > fi.Height)
                        t += (renderSize - fi.Height) / 2;
                    else
                        l += (renderSize - fi.Width) / 2;


                    g.DrawString(b.Text, f, Brushes.Black, l, t);
                }
            }
        }

        #endregion

        #region Controls

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _settingsPack.Apply();
            _gameIsRunning = false;
            _gameLoopThread?.Abort();
            Application.ExitThread();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_settingsForm == null || _settingsForm.IsDisposed) _settingsForm = new SettingsForm();

            if (_settingsForm.Visible)
                _settingsForm.Focus();
            else
                _settingsForm.Show(this);
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Undo();
        }

        private void newGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Вы действительно хотите начать новую игру?", "Внимание!",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                NewGame();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Игра 2048.\n• Версия 1.0 от 10.08.2016.\n• Автор: Артём Баляница.", "О программе",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion
    }
}