using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LabelHelper
{
    public partial class FormSetLabel : Form
    {
        private TextBox[] boxes;
        public string[] labels = { "", "", "", "", "", "", "", "", "" };

        public FormSetLabel(string[] lbls)
        {
            InitializeComponent();
            boxes = new TextBox[] { TxBoxLabel1, TxBoxLabel2, TxBoxLabel3, TxBoxLabel4, TxBoxLabel5, TxBoxLabel6, TxBoxLabel7, TxBoxLabel8, TxBoxLabel9 };
            for (int i = 0; i < lbls.Length && i < MainForm.MAX_LABELS; i++)
            {
                boxes[i].Text = lbls[i];
            }
        }

        private void FetchLabels()
        {
            for(int i = 0; i < boxes.Length; i++)
            {
                labels[i] = boxes[i].Text;
            }
        }

        private void ValidateTextBox(TextBox target)
        {
            foreach (char fLetter in MainForm.Globals.forbiddenPathLetters)
            {
                if (target.Text.Contains(fLetter))
                {
                    string msg = string.Format("Character {0} is invalid for a folder name!\nAvoid using {1}", fLetter, new string(MainForm.Globals.forbiddenPathLetters));
                    target.Text = string.Empty;
                    MessageBox.Show(msg, "Invalid label name", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BttnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void FormSetLabel_FormClosing(object sender, FormClosingEventArgs e)
        {
            FetchLabels();
        }

        private void TxBoxLabel_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                this.Close();
            }
        }

        private void TxBoxLabel9_Leave(object sender, EventArgs e)
        {
            ValidateTextBox((TextBox)sender);
        }
    }
}
