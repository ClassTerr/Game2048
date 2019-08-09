using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Game2048.Properties;

namespace Game2048
{
    public partial class SettingsForm : Form
    {
        private readonly Block _testBlock = new Block();
        private bool _controlsUpdating;
        private SettingsPack _settingsPack;

        public SettingsForm()
        {
            InitializeComponent();
            _testBlock.X = _testBlock.Y = 50;
            _settingsPack = Fun.LoadSettings().Clone();

            UpdateControls();

            foreach (Control item in Controls) item.KeyDown += SettingsForm_KeyDown;
        }

        private void UpdateVariables()
        {
            if (_controlsUpdating) return;

            _settingsPack.BackCol = panel1.BackColor;
            _settingsPack.FieldCol = panel2.BackColor;
            _settingsPack.GridCol = panel3.BackColor;
            _settingsPack.FontName = label11.Font.Name;
            _settingsPack.Coefficient = (float) trackBar1.Value / 100;
            _settingsPack.JagCount = trackBar2.Value;
            _settingsPack.RenderStyle = (RenderStyle) comboBox1.SelectedIndex;
            _settingsPack.FieldSize = (int) numericUpDown1.Value;
            DrawExample();
        }

        private void UpdateControls()
        {
            _controlsUpdating = true;
            panel1.BackColor = _settingsPack.BackCol;
            panel2.BackColor = _settingsPack.FieldCol;
            panel3.BackColor = _settingsPack.GridCol;
            label11.Font = new Font(_settingsPack.FontName, label11.Font.Size);
            trackBar1.Value = (int) (_settingsPack.Coefficient * 100);
            trackBar2.Value = _settingsPack.JagCount;
            numericUpDown1.Value = _settingsPack.FieldSize;
            comboBox1.SelectedIndex = (int) _settingsPack.RenderStyle;
            DrawExample();
            trackBar1_Scroll(null, null);
            trackBar2_Scroll(null, null);
            _controlsUpdating = false;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                fontDialog1.Font = label11.Font;
                if (fontDialog1.ShowDialog() == DialogResult.OK)
                    label11.Font = new Font(fontDialog1.Font.Name, label11.Font.Size);
                UpdateVariables();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void panel1_Click(object sender, EventArgs e)
        {
            if (sender is Control c)
            {
                colorDialog1.Color = c.BackColor;
                if (colorDialog1.ShowDialog() == DialogResult.OK)
                    (sender as Panel).BackColor = colorDialog1.Color;
                UpdateVariables();
            }
        }

        private void DrawExample()
        {
            Graphics g = painterControl1.Graphics;
            GraphicsPath p = new GraphicsPath();
            float l, t;
            RenderStyle rs = (RenderStyle) comboBox1.SelectedIndex;

            switch (rs)
            {
                case RenderStyle.Squares:
                    p = _testBlock.GetSquareShape(_settingsPack.Coefficient);
                    break;
                case RenderStyle.Stars:
                    p = _testBlock.GetStarShape(_settingsPack.JagCount);
                    break;
            }


            g.Clear(_settingsPack.BackCol);

            g.FillRectangle(new SolidBrush(_settingsPack.FieldCol), new Rectangle(50, 50, 120, 120));
            g.DrawLine(new Pen(_settingsPack.GridCol), 50, 50, 50, 170);
            g.DrawLine(new Pen(_settingsPack.GridCol), 150, 50, 150, 170);
            g.DrawLine(new Pen(_settingsPack.GridCol), 50, 50, 170, 50);
            g.DrawLine(new Pen(_settingsPack.GridCol), 50, 150, 170, 150);

            g.FillPath(new SolidBrush(_testBlock.Color), p);

            FontInfo fi = Fun.GetFontInfo(_testBlock.Text, _testBlock.Width, _settingsPack.FontName, g);
            Font f = new Font(_settingsPack.FontName, fi.Size);

            l = t = 50;
            if (fi.Width > fi.Height)
                t += (_testBlock.Width - fi.Height) / 2;
            else
                l += (_testBlock.Width - fi.Width) / 2;

            g.DrawString(_testBlock.Text, f, Brushes.Black, l, t);

            painterControl1.RazorPaint();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            trackBar1.Enabled = comboBox1.SelectedIndex == 0;
            trackBar2.Enabled = comboBox1.SelectedIndex == 1;

            UpdateVariables();
        }

        private void painterControl1_Paint(object sender, PaintEventArgs e)
        {
            UpdateVariables();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            label8.Text = trackBar1.Value + "%";
            UpdateVariables();
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            label10.Text = trackBar2.Value.ToString();
            UpdateVariables();
        }

        private void ok_button_Click(object sender, EventArgs e)
        {
            bool isFieldSizeChanged = Settings.Default.SettingsPack.FieldSize != _settingsPack.FieldSize;
            if (isFieldSizeChanged &&
                MessageBox.Show("Это сбросит ваш прогресс, вы уверены что хотите изменить размер поля?",
                    "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
                return;

            _settingsPack.Apply();
            if (Owner is MainForm o)
            {
                o.SettingsChanged();

                if (isFieldSizeChanged) o.NewGame();
            }

            Close();
        }

        private void reset_Click(object sender, EventArgs e)
        {
            SettingsPack t = new SettingsPack
            {
                Blocks = _settingsPack.Blocks,
                Score = _settingsPack.Score,
                PreviousScore = _settingsPack.PreviousScore,
                PreviousStep = _settingsPack.PreviousStep
            };

            _settingsPack = t;

            UpdateControls();
        }

        private void SettingsForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}