using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LabelHelper
{
    public partial class MainForm : Form
    {
        public const int MAX_LABELS = 9;
        public const string VERSION = "0.9";
        public const string DEFAULT_SOURCE = "D:\\GPX\\Datasets\\FEC_dataset\\test";
        public const string DEFAULT_DESTINATION = "D:\\GPX\\Datasets\\FEC_dataset\\smaller_set";


        private bool isRunning = false;
        private int validPaths = 0;
        private int amountToMove = 0;
        private string method;
        private string[] labelPaths;
        private string[] filesInSource;
        private List<string>[] filesToMove = new List<string>[9];
        private List<string> errorsList = new List<string>();       // Errors recorded during file movimentation

        private Label[] labels;
        private Button[] buttons;

        public static class GlobalColors
        {
            public static readonly Color colorGood = Color.Lime, colorWarning = Color.Gold;
            public static readonly Color colorAttention = Color.Orange, colorBad = Color.Red;
            public static readonly Color defForm = Color.FromKnownColor(KnownColor.Control), defStrip = Color.FromKnownColor(KnownColor.ControlDark);
        }

        public static class Globals
        {
            public static readonly char[] forbiddenPathLetters = { '\\', '/', ':', '*', '?', '\"', '<', '>', '|' };
        }

        public MainForm()
        {
            InitializeComponent();
            this.Text += string.Format(" V.{0}", VERSION);
            for(int i = 0; i < MAX_LABELS; i++)
            {
                filesToMove[i] = new List<string>();
            }
            labels = new Label[] { LblLabel1, LblLabel2, LblLabel3, LblLabel4, LblLabel5, LblLabel6, LblLabel7, LblLabel8, LblLabel9 };
            buttons = new Button[] { BttnLabel1, BttnLabel2, BttnLabel3, BttnLabel4, BttnLabel5, BttnLabel6, BttnLabel7, BttnLabel8, BttnLabel9 };

            TxBoxSource.Text = string.IsNullOrWhiteSpace(DEFAULT_SOURCE) ? "" : DEFAULT_SOURCE;
            TxBoxDest.Text = string.IsNullOrWhiteSpace(DEFAULT_DESTINATION) ? "" : DEFAULT_DESTINATION;

            string[] temp = { "Happy", "Sad", "Neutral", "", "", "", "", "", "Undefined" };
            labelPaths = temp;
            for (int i = 0; i < MAX_LABELS; i++)
            {
                if (labelPaths[i] == string.Empty)
                {
                    labels[i].Enabled = buttons[i].Enabled = false;
                    labels[i].Text = buttons[i].Text = "-";
                }
                else
                {
                    labels[i].Enabled = buttons[i].Enabled = true;
                    buttons[i].Text = "(" + (i + 1) + ") " + labelPaths[i];
                    labels[i].Text = labelPaths[i] + ": 0";
                    validPaths++;
                }
            }
        }

        /// <summary>
        /// Verifies if both, the source foldeer and the destination exists
        /// </summary>
        /// <returns>Boolean true if both exists and false if one or none doesn't</returns>
        private bool VerifySrcDestValid()
        {
            bool srcValid = false, dstValid = false;
            if (System.IO.Directory.Exists(TxBoxSource.Text))
            {
                TxBoxSource.BackColor = GlobalColors.colorGood;
                srcValid = true;
            }
            else if (TxBoxSource.Text == string.Empty)
            {
                TxBoxSource.BackColor = GlobalColors.colorBad;
            }
            else
            {
                TxBoxSource.BackColor = GlobalColors.colorWarning;
            }

            if (System.IO.Directory.Exists(TxBoxDest.Text))
            {
                TxBoxDest.BackColor = GlobalColors.colorGood;
                dstValid = true;
            }
            else if (TxBoxDest.Text == string.Empty)
            {
                TxBoxDest.BackColor = GlobalColors.colorBad;
            }
            else
            {
                TxBoxDest.BackColor = GlobalColors.colorWarning;
            }
            return BttnStart.Enabled = srcValid && dstValid;
        }

        private void ClearLists()
        {
            foreach (List<string> list in filesToMove)
            {
                list.Clear();
            }
        }

        private void LockControls()
        {
            TableControls.Enabled = BttnLabelSet.Enabled = BttnStart.Enabled = false;
            BttnEnd.Enabled = isRunning = true;
        }

        private void UnlockControls()
        {
            TableControls.Enabled = BttnLabelSet.Enabled = BttnStart.Enabled= true;
            BttnEnd.Enabled = isRunning = false;
        }

        // Add to one of the 9 available labels (list indexes) and ask 'LoadNextPreview' to advance to the next image
        private void AddToList(int label)
        {
            if(isRunning)
            {
                if(labelPaths[label] != string.Empty && (label >= 0 && label < MAX_LABELS))
                {
                    filesToMove[label].Add(string.Format("{0}", filesInSource[BarRemain.Value - 1]));
                    labels[label].Text = labelPaths[label] + ": " + filesToMove[label].Count.ToString();
                    amountToMove++;
                    LoadNextPreview();
                }
            }
            else
            {
                MessageBox.Show("Classification didn't started yet. Click 'Start' to begin", "Not ready", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void EndAndMoveFiles()
        {
            BarRemain.Value = 0;
            BarRemain.Maximum = amountToMove;
            method = (RadioMethodCopy.Checked ? "Copying" : "Moving");
            bgWorkerMover.RunWorkerAsync();
        }

        // Loads the next image in the preview window
        private void LoadNextPreview()
        {
            if (BarRemain.Value < BarRemain.Maximum)
            {
                LblRemain.Text = string.Format("{0} / {1}", BarRemain.Value + 1, BarRemain.Maximum);
            }
            else
            {
                PictBoxPreview.Image.Dispose();
                MessageBox.Show("There are no images left to label", "Finished", MessageBoxButtons.OK, MessageBoxIcon.Information);
                EndAndMoveFiles();
                return;
            }
            try
            {
                string fPath = string.Format("{0}\\{1}", TxBoxSource.Text, filesInSource[BarRemain.Value]);
                System.IO.FileInfo info = new System.IO.FileInfo(fPath);
                using (Bitmap img = new Bitmap(fPath))
                {
                    PictBoxPreview.Image = new Bitmap(img);
                }
                LblFileName.Text = filesInSource[BarRemain.Value];
                LblFileSize.Text = info.Length.ToString("#,#0") + " KB";
                LblFileExtension.Text = info.Extension;
                LblFileResolution.Text = string.Format("{0}x{1}", PictBoxPreview.Image.Width, PictBoxPreview.Image.Height);
                BarRemain.Value++;
            }
            catch (Exception ecp)
            {
                string msg = string.Format("'{0}' couldn't be loaded and was ignored.\nException: {1}", filesInSource[BarRemain.Value], ecp.Message);
                MessageBox.Show(msg, "Read error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                BarRemain.Value++;
                LoadNextPreview();
            }
        }


        // Control events -------------------------------------------------------------------------
        private void BttnLabelSet_Click(object sender, EventArgs e)
        {
            validPaths = 0;
            FormSetLabel form = new FormSetLabel(labelPaths);
            form.ShowDialog();
            labelPaths = form.labels;
            for (int i = 0; i < MAX_LABELS; i++)
            {
                if (labelPaths[i] == string.Empty)
                {
                    labels[i].Enabled = buttons[i].Enabled = false;
                    labels[i].Text = buttons[i].Text = "-";
                }
                else
                {
                    labels[i].Enabled = buttons[i].Enabled = true;
                    buttons[i].Text = "(" + (i + 1) + ") " + labelPaths[i];
                    labels[i].Text = labelPaths[i] + ": 0";
                    validPaths++;
                }
            }
            ClearLists();
        }

        private void BttnStart_Click(object sender, EventArgs e)
        {
            if(validPaths == 0)
            {
                MessageBox.Show("There are no labels set!", "No labels set", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if(!VerifySrcDestValid())
            {
                MessageBox.Show("One of the folders was removed or became inaccessible!", "Path error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // Fetch all jpg and png images on the folder
            List<string> sortingList = new List<string>();
            sortingList = System.IO.Directory.GetFiles(TxBoxSource.Text).Select(filesInSource => System.IO.Path.GetFileName(filesInSource)).ToList();
            Random rng = new Random();
            if(RadioSelectMax.Checked)
            {
                int maxVal = Convert.ToInt32(NUpDownSelectMax.Value);
                if(maxVal >= sortingList.Count )
                {
                    MessageBox.Show("Maximum defined it too big for the amount found in the folder!", "Maximum was too big", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (ChkBoxSelectRandom.Checked)
                {
                    // Delete some from the array, randomly
                    while (sortingList.Count > maxVal)
                    {
                        sortingList.RemoveAt(rng.Next(sortingList.Count));
                    }
                }
                else
                {
                    // Delete some from the array, from the start
                    while (sortingList.Count > maxVal)
                    {
                        sortingList.RemoveAt(0);
                    }
                }
            }
            else if(ChkBoxSelectRandom.Checked)
            {
                // Just shuffle everything
                for(int i = 0; i < sortingList.Count; i++)
                {
                    int rIndex = rng.Next(sortingList.Count);
                    string temp = sortingList[rIndex];
                    sortingList[rIndex] = sortingList[i];
                    sortingList[i] = temp;
                }
            }
            filesInSource = sortingList.ToArray();
            sortingList.Clear();
            BarRemain.Maximum = filesInSource.Length;
            if (BarRemain.Maximum == 0)
            {
                MessageBox.Show("Source Folder does not contains any supported files!", "Empty Source Folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // Everything fine, continue
            LockControls();
            LoadNextPreview();
        }

        private void BttnEnd_Click(object sender, EventArgs e)
        {
            EndAndMoveFiles();
        }

        private void BttnLabel1_Click(object sender, EventArgs e)
        {
            AddToList(0);
        }

        private void BttnLabel2_Click(object sender, EventArgs e)
        {
            AddToList(1);
        }

        private void BttnLabel3_Click(object sender, EventArgs e)
        {
            AddToList(2);
        }

        private void BttnLabel4_Click(object sender, EventArgs e)
        {
            AddToList(3);
        }

        private void BttnLabel5_Click(object sender, EventArgs e)
        {
            AddToList(4);
        }

        private void BttnLabel6_Click(object sender, EventArgs e)
        {
            AddToList(5);
        }

        private void BttnLabel7_Click(object sender, EventArgs e)
        {
            AddToList(6);
        }

        private void BttnLabel8_Click(object sender, EventArgs e)
        {
            AddToList(7);
        }

        private void BttnLabel9_Click(object sender, EventArgs e)
        {
            AddToList(8);
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (isRunning)
            {
                switch (e.KeyCode)
                {
                    case Keys.NumPad0:
                    case Keys.D0:
                        EndAndMoveFiles();
                        break;
                    case Keys.NumPad1:
                    case Keys.D1:
                        AddToList(0);
                        break;
                    case Keys.NumPad2:
                    case Keys.D2:
                        AddToList(1);
                        break;
                    case Keys.NumPad3:
                    case Keys.D3:
                        AddToList(2);
                        break;
                    case Keys.NumPad4:
                    case Keys.D4:
                        AddToList(3);
                        break;
                    case Keys.NumPad5:
                    case Keys.D5:
                        AddToList(4);
                        break;
                    case Keys.NumPad6:
                    case Keys.D6:
                        AddToList(5);
                        break;
                    case Keys.NumPad7:
                    case Keys.D7:
                        AddToList(6);
                        break;
                    case Keys.NumPad8:
                    case Keys.D8:
                        AddToList(7);
                        break;
                    case Keys.NumPad9:
                    case Keys.D9:
                        AddToList(8);
                        break;
                }
            }
        }

        private void TxBoxSource_TextChanged(object sender, EventArgs e)
        {
            VerifySrcDestValid();
        }

        private void TxBoxDest_TextChanged(object sender, EventArgs e)
        {
            VerifySrcDestValid();
        }

        private void RegisterDragDrop(TextBox TxBox, DragEventArgs e)
        {
            TxBox.Text = System.IO.Path.GetDirectoryName(((string[])e.Data.GetData(DataFormats.FileDrop, false))[0]);
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void TxBoxSource_DragEnter(object sender, DragEventArgs e)
        {
            RegisterDragDrop((TextBox)sender, e);
        }

        private void TxBoxDest_DragEnter(object sender, DragEventArgs e)
        {
            RegisterDragDrop((TextBox)sender, e);
        }

        private void RadioSelectMax_CheckedChanged(object sender, EventArgs e)
        {
            NUpDownSelectMax.Enabled = RadioSelectMax.Checked;
        }

        private void TxBoxDest_Leave(object sender, EventArgs e)
        {
            if (!System.IO.Directory.Exists(TxBoxDest.Text))
            {
                if (DialogResult.Yes == MessageBox.Show("Destionation Folder does not exists. Want to create it?", "Destination does not exists", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(TxBoxDest.Text);
                        if (!System.IO.Directory.Exists(TxBoxDest.Text))
                        {
                            string msg = string.Format("It wasn't possible to create a new directory on '{0}'", TxBoxDest.Text);
                            MessageBox.Show(msg, "Directory creation failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception ecp)
                    {
                        string msg = string.Format("There was an error creating a directory on '{0}'\n{1}", TxBoxDest.Text, ecp.Message);
                        MessageBox.Show(msg, "Directory creation exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            VerifySrcDestValid();
        }

        private void bgWorkerMover_DoWork(object sender, DoWorkEventArgs e)
        {
            for (int i = 0, moved = 0; i < MAX_LABELS; i++)
            {
                if (labelPaths[i] == string.Empty)
                {
                    continue;    //Ignore
                }
                string dst = string.Format("{0}\\{1}", TxBoxDest.Text, labelPaths[i]);
                if (!System.IO.Directory.Exists(dst))
                {
                    System.IO.Directory.CreateDirectory(dst);
                }
                if (RadioMethodCopy.Checked)
                {
                    foreach (string file in filesToMove[i])
                    {
                        try
                        {
                            string src = string.Format("{0}\\{1}", TxBoxSource.Text, file);
                            System.IO.File.Copy(@src, @dst + "\\" + file);
                        }
                        catch (Exception ecp)
                        {
                            errorsList.Add(string.Format("'{0}' => {1}", file, ecp.Message));
                        }
                        bgWorkerMover.ReportProgress(++moved);
                    }
                }
                else if (RadioMethodMove.Checked)
                {
                    foreach (string file in filesToMove[i])
                    {
                        try
                        {
                            string src = string.Format("{0}\\{1}", TxBoxSource.Text, file);
                            System.IO.File.Move(@src, @dst + "\\" + file);
                        }
                        catch (Exception ecp)
                        {
                            errorsList.Add(string.Format("'{0}' => {1}", file, ecp.Message));
                        }
                        bgWorkerMover.ReportProgress(++moved);
                    }

                }
                filesToMove[i].Clear();
            }
        }

        private void bgWorkerMover_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            BarRemain.Value = e.ProgressPercentage;
            string temp = string.Format("{0} {1} / {2}", method, e.ProgressPercentage.ToString("#,##0"), amountToMove.ToString("#,##0"));
            LblRemain.Text = temp;
        }

        private void bgWorkerMover_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string errors = "No errors";
            if (errorsList.Count < 20 && errorsList.Count > 0)
            {
                errors = "The following files could not be moved / copied:\n";
                foreach (string err in errorsList)
                {
                    errors += err + "\n";
                }
            }
            else if (errorsList.Count >= 20)
            {
                errors = errorsList.Count + " / " + amountToMove + " files failed";
            }
            MessageBox.Show("Operation finished.\n" + errors, "Finished", MessageBoxButtons.OK, MessageBoxIcon.Information);
            errorsList.Clear();
            UnlockControls();
            amountToMove = BarRemain.Value = 0;
            LblRemain.Text = "- / -";
        }

        private void LblSource_Click(object sender, EventArgs e)
        {
            if (System.IO.Directory.Exists(TxBoxSource.Text))
            {
                Process.Start(@TxBoxSource.Text);
            }
        }

        private void LblDest_Click(object sender, EventArgs e)
        {
            if (System.IO.Directory.Exists(TxBoxDest.Text))
            {
                Process.Start(TxBoxDest.Text);
            }
        }
    }
}
