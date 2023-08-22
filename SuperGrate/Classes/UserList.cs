﻿using SuperGrate.Classes;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SuperGrate.UserList
{
    public static class ULControl
    {
        public static UserRow CurrentHeaderRow = null;
        public static UserRow HeaderRowComputerSource = new UserRow()
        {
            { ULColumnType.Tag, Language.Get("Class/UserList/Column/SecurityIdentifier") },
            { ULColumnType.SourceNTAccount, Language.Get("Class/UserList/Column/UserName") },
            { ULColumnType.LastModified, Language.Get("Class/UserList/Column/LastModified") },
            { ULColumnType.FirstCreated, Language.Get("Class/UserList/Column/FirstCreated") },
            { ULColumnType.ProfilePath, Language.Get("Class/UserList/Column/ProfilePath") },
            { ULColumnType.Size, Language.Get("Class/UserList/Column/Size") }
        };
        public static UserRow HeaderRowStoreSource = new UserRow()
        {
            { ULColumnType.Tag, Language.Get("Class/UserList/Column/StoreIdentifier") },
            { ULColumnType.SecurityIdentifier, Language.Get("Class/UserList/Column/SecurityIdentifier") },
            { ULColumnType.SourceNTAccount, Language.Get("Class/UserList/Column/SourceUserName") },
            { ULColumnType.DestinationNTAccount, Language.Get("Class/UserList/Column/DestinationUserName") },
            { ULColumnType.SourceComputer, Language.Get("Class/UserList/Column/SourceComputer") },
            { ULColumnType.DestinationComputer, Language.Get("Class/UserList/Column/DestinationComputer") },
            { ULColumnType.ImportedBy, Language.Get("Class/UserList/Column/ImportedBy") },
            { ULColumnType.ImportedOn, Language.Get("Class/UserList/Column/ImportedOn") },
            { ULColumnType.ExportedBy, Language.Get("Class/UserList/Column/ExportedBy") },
            { ULColumnType.ExportedOn, Language.Get("Class/UserList/Column/ExportedOn") },
            { ULColumnType.Size, Language.Get("Class/UserList/Column/Size") }
        };
        public static void SetColumns(this ListView Owner, UserRow Row)
        {
            Owner.SuspendLayout();
            Owner.BeginUpdate();
            Owner.Items.Clear();
            Owner.Columns.Clear();
            foreach (KeyValuePair<ULColumnType, string> Column in Row)
            {
                if (Column.Value != null)
                {
                    Owner.Columns.Add(Column.Value).Tag = Column.Key;
                }
            }
            CurrentHeaderRow = Row;
            Owner.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            Owner.EndUpdate();
            Owner.ResumeLayout();
        }
        public static void SetColumns(this ListView Owner, UserRow TemplateRow, ULColumnType[] Format)
        {
            UserRow Row = new UserRow
            {
                { ULColumnType.Tag, null }
            };
            foreach (ULColumnType ColumnType in Format)
            {
                if(TemplateRow.ContainsKey(ColumnType) && !Row.ContainsKey(ColumnType))
                {
                    Row.Add(ColumnType, TemplateRow[ColumnType]);
                }
            }
            Owner.SetColumns(Row);
        }
        public static void SetColumns(this ListView Owner, UserRow TemplateRow, string Format)
        {
            List<ULColumnType> ColIDs = new List<ULColumnType>();
            foreach(string sColID in Format.Split(','))
            {
                if (int.TryParse(sColID, out int ColID))
                {
                    ColIDs.Add((ULColumnType)ColID);
                }
            }
            Owner.SetColumns(TemplateRow, ColIDs.ToArray());
        }
        public static void SetRows(this ListView Owner, UserRows Rows, ULColumnType SortColumn = ULColumnType.Tag, ULSortDirection SortDirection = ULSortDirection.Ascending)
        {
            Owner.BeginUpdate();
            Owner.SuspendLayout();
            Owner.Items.Clear();
            foreach (ColumnHeader colHead in Owner.Columns)
            {
                ULColumnType colType = (ULColumnType)colHead.Tag;
                string arrow = "";
                if (colType == SortColumn)
                {
                    if (SortDirection == ULSortDirection.Ascending) arrow = " ↑";
                    if (SortDirection == ULSortDirection.Descending) arrow = " ↓";
                }
                colHead.Text = CurrentHeaderRow[colType] + arrow;
            }
            if (Rows != null)
            {
                Rows.Sort(delegate (UserRow x, UserRow y) {
                    if (!x.ContainsKey(SortColumn) && !y.ContainsKey(SortColumn)) return 0;
                    if (x[SortColumn] == null) return 1;
                    if (y[SortColumn] == null) return -1;
                    if (x[SortColumn] == y[SortColumn]) return 0;
                    if (long.TryParse(x[SortColumn], out long intX) && long.TryParse(y[SortColumn], out long intY))
                    {
                        if (intX > intY) return -1;
                        if (intX < intY) return 1;
                    }
                    return x[SortColumn].CompareTo(y[SortColumn]);
                });
                if (SortDirection == ULSortDirection.Ascending) Rows.Reverse();
                foreach (UserRow row in Rows)
                {
                    ListViewItem lvRow = null;
                    bool first = true;
                    foreach (KeyValuePair<ULColumnType, string> column in row)
                    {
                        if (column.Key == ULColumnType.Tag) continue;
                        string value = ConvertColumnValue(column);
                        if (first)
                        {
                            lvRow = Owner.Items.Add(value);
                            lvRow.ImageKey = "user";
                            lvRow.Tag = row[ULColumnType.Tag];
                            first = false;
                        }
                        else
                        {
                            lvRow.SubItems.Add(value);
                        }
                    }
                }
            }
            Owner.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            Owner.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            Owner.ResumeLayout();
            Owner.EndUpdate();
        }
        public static string ConvertColumnValue(KeyValuePair<ULColumnType, string> ColumnItem)
        {
            if (ColumnItem.Key == ULColumnType.Size)
            {
                if (double.TryParse(ColumnItem.Value, out double dblValue))
                {
                    return dblValue.ByteHumanize();
                }
            }
            if (
                ColumnItem.Key == ULColumnType.ExportedOn ||
                ColumnItem.Key == ULColumnType.FirstCreated ||
                ColumnItem.Key == ULColumnType.ImportedOn ||
                ColumnItem.Key == ULColumnType.LastModified
            ) {
                if (long.TryParse(ColumnItem.Value, out long longValue))
                {
                    return DateTime.FromFileTime(longValue).ToString();
                }
            }
            return ColumnItem.Value;
        }
        public static void SetViewMode(this ListView View, string ViewMode)
        {
            View.View = ParseViewMode(ViewMode);
        }
        public static View ParseViewMode(string ViewMode)
        {
            int.TryParse(ViewMode, out int intMode);
            if (intMode < 0 || intMode > 4) intMode = 1;
            return (View)intMode;
        }
    }
    public class UserRow : Dictionary<ULColumnType, string>
    {
        public UserRow() { }
        public UserRow(UserRow TemplateRow)
        {
            foreach (KeyValuePair<ULColumnType, string> column in TemplateRow)
            {
                Add(column.Key, null);
            }
        }
    }
    public class UserRows : List<UserRow> { }
    public enum ULColumnType
    {
        Tag = -1,
        SourceNTAccount = 0,
        SourceComputer = 1,
        DestinationComputer = 2,
        LastModified = 3,
        Size = 4,
        ImportedBy = 5,
        ImportedOn = 6,
        ExportedBy = 7,
        ExportedOn = 8,
        FirstCreated = 9,
        ProfilePath = 10,
        SecurityIdentifier = 11,
        DestinationNTAccount = 12
    }
    public enum ULSortDirection
    {
        Ascending = 0,
        Descending = 1
    }
}