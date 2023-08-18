﻿using SuperGrate.Classes;
using SuperGrate.UserList;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SuperGrate.Controls
{
    public partial class UserProperties : Form
    {
        /// <summary>
        /// The entry point for the UserProperties form.
        /// </summary>
        /// <param name="Template">Template user rows to generate the "Properties" column.</param>
        /// <param name="Row">The rows with data to fill in the "Values" column.</param>
        public UserProperties(UserRow Template, UserRow Row)
        {
            InitializeComponent();
            Icon = Properties.Resources.user_ico;
            Text = Language.Get("Control/UserProperties/UserProperties");
            btnOK.SetSystemIcon(Properties.Resources.check_ico);
            btnOK.Text = Language.Get("Control/UserProperties/OK");
            foreach (KeyValuePair<ULColumnType, string> property in Row)
            {
                string value = ULControl.ConvertColumnValue(property);
                if (property.Key == ULColumnType.SourceNTAccount)
                {
                    Text = value;
                }
                string name;
                if (Template.ContainsKey(property.Key))
                {
                    name = Template[property.Key];
                }
                else
                {
                    name = property.Key.ToString();
                }
                ListViewItem lvProperty = lvProperties.Items.Add(name);
                lvProperty.SubItems.Add(value);
            }
            lvProperties.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            lvProperties.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            int headWidth = 0;
            foreach(ColumnHeader colHeader in lvProperties.Columns)
            {
                headWidth += colHeader.Width;
            }
            Width = headWidth + 40;
            Height = (17 * Row.Count) + 150;
            CenterToParent();
        }
    }
}
