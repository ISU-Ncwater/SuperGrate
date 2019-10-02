﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

namespace SuperGrate
{
    public partial class Main : Form
    {
        public static Form Form;
        public static RichTextBox LoggerBox;
        public static ProgressBar Progress;
        public static string SourceComputer;
        public static string DestinationComputer;
        public static ListSources CurrentListSource = ListSources.Unknown;
        private static bool isRunning = false;
        private string[] MainParameters = null;
        private bool CloseRequested = false;
        public Main(string[] parameters)
        {
            MainParameters = parameters;
            InitializeComponent();
            Form = this;
            LoggerBox = LogBox;
            Progress = pbMain;
            lbxUsers.Tag = new string[0];
            Icon = Properties.Resources.supergrate;
        }
        private async void Main_Load(object sender, EventArgs e)
        {
            Config.LoadConfig(MainParameters);
            Logger.Success("Welcome to Super Grate! v" + Application.ProductVersion);
            Logger.Information("Enter some information to get started!");
            UpdateFormRestrictions();



            /*
            Logger.Information("Downloading...");
            await new Download("https://github.com/belowaverage-org/SuperGrate/raw/master/USMT/x64.zip", @".\asdf.zip").Start();
            Logger.Success("Done!");*/

        }
        private bool Running {
            get {
                return isRunning;
            }
            set {
                Progress.Value = 0;
                if (value)
                {
                    isRunning = true;
                    imgLoadLogo.Enabled = true;
                    tbSourceComputer.Enabled =
                    tbDestinationComputer.Enabled =
                    btnAFillSrc.Enabled =
                    btnAFillDest.Enabled =
                    btnListSource.Enabled =
                    btnListStore.Enabled =
                    btnDelete.Enabled =
                    lbxUsers.Enabled =
                    false;
                }
                else
                {
                    isRunning = false;
                    imgLoadLogo.Enabled = false;
                    tbSourceComputer.Enabled =
                    tbDestinationComputer.Enabled =
                    btnAFillSrc.Enabled =
                    btnAFillDest.Enabled =
                    btnListSource.Enabled =
                    btnListStore.Enabled =
                    btnDelete.Enabled =
                    lbxUsers.Enabled =
                    true;
                    UpdateFormRestrictions();
                    if (CloseRequested) Close();
                }
            }
        }
        private async void BtStartStop_Click(object sender, EventArgs e)
        {
            if (Running)
            {
                USMT.Cancel();
            }
            else
            {
                Running = true;
                btStartStop.Text = "Stop";
                int count = 0;
                string[] SIDs = new string[lbxUsers.SelectedIndices.Count];
                foreach (int index in lbxUsers.SelectedIndices)
                {
                    SIDs[count++] = ((string[])lbxUsers.Tag)[index];
                }
                if (CurrentListSource == ListSources.SourceComputer)
                {
                    await USMT.Do(USMTMode.ScanState, SIDs);
                }
                if (tbDestinationComputer.Text != "" && Running)
                {
                    await USMT.Do(USMTMode.LoadState, SIDs);
                    bool setting;
                    if (bool.TryParse(Config.Settings["AutoDeleteFromStore"], out setting) && setting)
                    {
                        foreach (string sid in SIDs)
                        {
                            await Misc.DeleteFromStore(sid);
                        }
                    }
                }
                btStartStop.Text = "Start";
                Running = false;
                Logger.Information("Done.");
            }
        }
        private async void BtnListSource_Click(object sender, EventArgs e)
        {
            Running = true;
            lbxUsers.Items.Clear();
            lblUserList.Text = "Users on Source Computer:";
            Dictionary<string, string> users = await Misc.GetUsersFromHost(tbSourceComputer.Text);
            List<string> tags = new List<string>();
            if (users != null)
            {
                bool setting;
                foreach (KeyValuePair<string, string> user in users)
                {
                    if (bool.TryParse(Config.Settings["HideBuiltInAccounts"], out setting) && setting && (user.Value.Contains("NT AUTHORITY") || user.Value.Contains("NT SERVICE")))
                    {
                        Logger.Verbose("Skipped: " + user.Key + ": " + user.Value + ".");
                        continue;
                    }
                    if (bool.TryParse(Config.Settings["HideUnknownSIDs"], out setting) && setting && user.Key == user.Value)
                    {
                        Logger.Verbose("Skipped unknown SID: " + user.Key + ".");
                        continue;
                    }
                    lbxUsers.Items.Add(user.Value);
                    tags.Add(user.Key);
                }
                lbxUsers.Tag = tags.ToArray();
                CurrentListSource = ListSources.SourceComputer;
                Logger.Success("Done!");
            }
            Running = false;
        }
        private async void BtnListStore_Click(object sender, EventArgs e)
        {
            Running = true;
            lbxUsers.Items.Clear();
            lblUserList.Text = "Users in Migration Store:";
            Dictionary<string, string> results = await Misc.GetUsersFromStore(Config.Settings["MigrationStorePath"]);
            if(results != null)
            {
                lbxUsers.Tag = results.Keys.ToArray();
                lbxUsers.Items.AddRange(results.Values.ToArray());
                CurrentListSource = ListSources.MigrationStore;
            }
            Running = false;
        }
        private void LogBox_DoubleClick(object sender, EventArgs e)
        {
            if(Logger.VerboseEnabled)
            {
                Logger.VerboseEnabled = false;
                Logger.Information("Verbose mode disabled.");
            }
            else
            {
                Logger.VerboseEnabled = true;
                Logger.Information("Verbose mode enabled.");
            }
        }
        private void UpdateFormRestrictions(object sender = null, EventArgs e = null)
        {
            if (lbxUsers.SelectedIndices.Count == 0 || (tbDestinationComputer.Text == "" && CurrentListSource == ListSources.MigrationStore))
            {
                btStartStop.Enabled = false;
            }
            else
            {
                btStartStop.Enabled = true;
            }
            if (lbxUsers.SelectedIndices.Count != 0 && CurrentListSource == ListSources.MigrationStore)
            {
                tbSourceComputer.Enabled = btnAFillSrc.Enabled = false;
                btnDelete.Enabled = true;
            }
            else
            {
                tbSourceComputer.Enabled = btnAFillSrc.Enabled = true;
                btnDelete.Enabled = false;
            }
            if(tbSourceComputer.Text == "")
            {
                btnListSource.Enabled = false;
            }
            else
            {
                btnListSource.Enabled = true;
            }
        }
        private void TbSourceComputer_TextChanged(object sender, EventArgs e)
        {
            SourceComputer = tbSourceComputer.Text;
            if(CurrentListSource == ListSources.SourceComputer)
            {
                lbxUsers.Items.Clear();
            }
            UpdateFormRestrictions();
        }
        private void TbDestinationComputer_TextChanged(object sender, EventArgs e)
        {
            DestinationComputer = tbDestinationComputer.Text;
            UpdateFormRestrictions();
        }
        private async void BtnDelete_Click(object sender, EventArgs e)
        {
            Running = true;
            foreach (int index in lbxUsers.SelectedIndices)
            {
                await Misc.DeleteFromStore(((string[])lbxUsers.Tag)[index]);
            }
            Running = false;
            btnListStore.PerformClick();
        }
        private void TbSourceComputer_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                btnListSource.PerformClick();
            }
        }
        private void BtnAFillSrc_Click(object sender, EventArgs e)
        {
            tbSourceComputer.Text = Environment.MachineName;
        }
        private void BtnAFillDest_Click(object sender, EventArgs e)
        {
            tbDestinationComputer.Text = Environment.MachineName;
        }
        private void MiExitButton_Click(object sender, EventArgs e)
        {
            Close();
        }
        private async void MiSaveLog_Click(object sender, EventArgs e)
        {
            dialogSaveLog.FileName = "SuperGrate_" + DateTime.Now.ToShortDateString().Replace('/', '-') + "_" + DateTime.Now.ToLongTimeString().Replace(':', '-');
            if (dialogSaveLog.ShowDialog() == DialogResult.OK)
            {
                await Logger.WriteLogToFile(dialogSaveLog.OpenFile());
            }
        }
        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Running)
            {
                e.Cancel = true;
                CloseRequested = true;
                USMT.Cancel();
            }
        }
        private void MiAboutSG_Click(object sender, EventArgs e)
        {
            new Controls.About().ShowDialog();
        }
        private void MiDocumentation_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/belowaverage-org/SuperGrate/wiki");
        }
        private void MiIssues_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/belowaverage-org/SuperGrate/issues");
        }
        private void MiSettings_Click(object sender, EventArgs e)
        {
            new Controls.Settings().ShowDialog();
        }
    }
    public enum ListSources
    {
        Unknown = -1,
        SourceComputer = 1,
        MigrationStore = 2
    }
}
