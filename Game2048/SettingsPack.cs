using System;
using System.Collections.Generic;
using System.Drawing;
using Game2048.Properties;

namespace Game2048
{
    [Serializable]
    public class SettingsPack
    {
        public SettingsPack()
        {
            BackCol = Color.CornflowerBlue;
            FieldCol = Color.LightBlue;
            GridCol = Color.Green;
            Coefficient = 0.15f;
            JagCount = 10;
            FieldSize = 4;
            FontName = "Jokerman"; //"Kristen ITC";//"Century Gothic";
            RenderStyle = RenderStyle.Stars;
            Blocks = new List<Block>();
            PreviousStep = null;
            Score = 0;
            PreviousScore = 0;
        }

        public Color BackCol { get; set; }
        public Color FieldCol { get; set; }
        public Color GridCol { get; set; }
        public float Coefficient { get; set; }
        public int JagCount { get; set; }
        public int FieldSize { get; set; }
        public string FontName { get; set; }
        public RenderStyle RenderStyle { get; set; }
        public List<Block> Blocks { get; set; }
        public List<Block> PreviousStep { get; set; }
        public long Score { get; set; }
        public long PreviousScore { get; set; }

        public void Apply()
        {
            Settings.Default.SettingsPack = this;
            Settings.Default.Save();
        }

        public SettingsPack Clone()
        {
            return MemberwiseClone() as SettingsPack;
        }
    }
}