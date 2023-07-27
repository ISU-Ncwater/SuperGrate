﻿using SuperGrate.Classes;
using SuperGrate.Properties;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SuperGrate.Controls
{
    /// <summary>
    /// Constructs a dialog to search computers in Active Directory, and outputs the user's selection in the "SelectedComputer" public string.
    /// </summary>
    public partial class DirectorySearch : Form
    {
        string InitialQuery = "";
        DirectorySearcher DS = new DirectorySearcher();
        /// <summary>
        /// The selected computer, filled once the dialog is closed.
        /// </summary>
        public string SelectedComputer = "";
        /// <summary>
        /// Constructs a dialog to search computers in Active Directory, and outputs the user's selection in the "SelectedComputer" public string.
        /// </summary>
        public DirectorySearch(string Query = "")
        {
            InitializeComponent();
            Icon = Resources.check_ico;
            tbSearch.Focus();
            InitialQuery = Query;
            chDisplayName.Text = Language.Get("Control/DirectorySearch/Name");
            chDescription.Text = Language.Get("Control/DirectorySearch/Description");
            chOU.Text = Language.Get("Control/DirectorySearch/OrganizationalUnit");
            btnSearch.Text = Language.Get("Control/DirectorySearch/Search");
            Text = Language.Get("Control/DirectorySearch/SelectAComputer");
            btnSelect.Text = Language.Get("OK");
            btnCancel.Text = Language.Get("Cancel");
        }
        /// <summary>
        /// Perform a directory search (asynchronously), and show the results in the List View.
        /// </summary>
        private async void btnSearch_Click(object sender, EventArgs e)
        {
            tbSearch.Text = Regex.Replace(tbSearch.Text, "[()*]", "");
            if (tbSearch.Text.Length == 0) return;
            Main.Form.Running = RunningTask.Unknown;
            Logger.Information(Language.Get("Control/DirectorySearch/Log/SearchingActiveDirectoryFor", tbSearch.Text));
            btnSearch.Enabled = false;
            btnSearch.Text = Language.Get("Control/DirectorySearch/Searching");
            lvResults.Items.Clear();
            DS.Filter = "(&(objectClass=computer)(|(name=*" + tbSearch.Text + "*)(description=*" + tbSearch.Text + "*)))";
            List<ListViewItem> lvis = new List<ListViewItem>();
            try
            {
                await Task.Run(() =>
                {
                    SearchResultCollection src = DS.FindAll();
                    foreach (SearchResult sr in src)
                    {
                        ListViewItem lvi = new ListViewItem(sr.Properties["name"][0].ToString());
                        if (sr.Properties["description"].Count == 0)
                        {
                            lvi.SubItems.Add("");
                        }
                        else
                        {
                            lvi.SubItems.Add(sr.Properties["description"][0].ToString());
                        }
                        lvi.SubItems.Add(sr.Properties["distinguishedName"][0].ToString());
                        lvis.Add(lvi);
                    }
                });
                foreach (ListViewItem lvi in lvis) lvResults.Items.Add(lvi);
                if (lvResults.Items.Count != 0)
                {
                    lvResults.Items[0].Selected = true;
                    lvResults.Focus();
                }
                lvResults.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                btnSearch.Text = Language.Get("Control/DirectorySearch/Search");
                btnSearch.Enabled = true;
                Main.Form.Running = RunningTask.None;
                Logger.Verbose(Language.Get("Control/DirectorySearch/Log/FoundComputers", lvis.Count.ToString()));
                Logger.Success(Language.Get("Done"));
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, Language.Get("Control/DirectorySearch/Log/Failed/SearchActiveDirectory"));
                Main.Form.Running = RunningTask.None;
                btnSearch.Text = Language.Get("Control/DirectorySearch/Search");
                btnSearch.Enabled = true;
            }
        }
        /// <summary>
        /// Close the dialog without making a selection.
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
        /// <summary>
        /// Return the selected item to the "SelectedComputer" public string and close the dialog.
        /// </summary>
        private void btnSelect_Click(object sender, EventArgs e)
        {
            if (lvResults.SelectedItems.Count != 0) SelectedComputer = lvResults.SelectedItems[0].Text;
            Close();
        }
        /// <summary>
        /// Perform a search when the enter key is pressed.
        /// </summary>
        private void tbSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) btnSearch_Click(null, null);
        }
        /// <summary>
        /// If a list item is double clicked, then select the item and close the dialog.
        /// </summary>
        private void lvResults_ItemActivate(object sender, EventArgs e)
        {
            btnSelect_Click(null, null);
        }
        /// <summary>
        /// If text has been provided to the constructor, then autofill and search the text.
        /// </summary>
        private void DirectorySearch_Load(object sender, EventArgs e)
        {
            if (InitialQuery.Length == 0) return;
            tbSearch.Text = InitialQuery;
            btnSearch_Click(null, null);
        }
    }
}