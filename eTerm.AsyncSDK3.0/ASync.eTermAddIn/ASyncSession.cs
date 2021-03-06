﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ASync.MiddleWare;
using eTerm.AsyncSDK;
using eTerm.AsyncSDK.Util;
using System.Text.RegularExpressions;
using System.Collections;

namespace ASync.eTermAddIn {
    public partial class ASyncSession : BaseAddIn {
        /// <summary>
        /// Initializes a new instance of the <see cref="ASyncSession"/> class.
        /// </summary>
        public ASyncSession() {
            InitializeComponent();
            this.Load += new EventHandler(
                    delegate(object sender, EventArgs e)
                    {
                        tabControl1.SelectedTab = tabItem1;
                        this.lstSession.Items.Clear();
                        foreach (TSessionSetup Setup in AsyncStackNet.Instance.ASyncSetup.SessionCollection) {
                            SDKGroup group = null;
                            if (AsyncStackNet.Instance.ASyncSetup.GroupCollection != null && !string.IsNullOrEmpty(Setup.GroupCode) && AsyncStackNet.Instance.ASyncSetup.GroupCollection.Contains(new SDKGroup() { groupCode = Setup.GroupCode }))
                                group = AsyncStackNet.Instance.ASyncSetup.GroupCollection[
                                    AsyncStackNet.Instance.ASyncSetup.GroupCollection.IndexOf(new SDKGroup() { groupCode = Setup.GroupCode })];
                            ListViewItem Item = new ListViewItem(new string[] {
                                Setup.SessionCode,
                                group==null?"未分组":group.groupName,
                                Setup.SessionExpire.ToString(),
                                Setup.FlowRate.ToString(),
                                Setup.ForbidCmdReg
                            });
                            Item.Name = Setup.SessionCode;
                            Item.Tag = Setup;
                            this.lstSession.Items.Add(Item);
                            this.comboTree1.Nodes.Clear();
                            listBox1.Items.Clear();
                        }
                    });
        }

        

        /// <summary>
        /// Handles the Click event of the btnDelete control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnDelete_Click(object sender, EventArgs e) {
            if (MessageBox.Show("操作不可恢复，确实要继续吗？", "系统提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
                foreach (ListViewItem item in this.lstSession.Items) {
                    if (!item.Selected) continue;
                    AsyncStackNet.Instance.ASyncSetup.SessionCollection.Remove(new TSessionSetup() { SessionCode=item.Name });
                }
                btnSave_Click(null, EventArgs.Empty);
                this.OnLoad(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnSave_Click(object sender, EventArgs e) {
            if (MessageBox.Show("操作不可恢复，重启程序后配置将生效！", "系统提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
                AsyncStackNet.Instance.BeginRateUpdate(new AsyncCallback(delegate(IAsyncResult iar)
                {
                    AsyncStackNet.Instance.EndRateUpdate(iar);
                }));
            }
        }

        /// <summary>
        /// Handles the Click event of the btnSessionEdit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnSessionEdit_Click(object sender, EventArgs e) {
            foreach (ListViewItem item in this.lstSession.Items) {
                if (!item.Selected) continue;
                TSessionSetup Setup = item.Tag as TSessionSetup;
                PanelSession.Tag = Setup;
                txtDescription.Text = Setup.Description;
                txtExpire.Value = Setup.SessionExpire;
                txtPassword.Text = Setup.SessionPass;
                txtSessionName.Text= Setup.SessionCode;
                chkIsOpen.Checked = Setup.IsOpen;
                txtFlow.Value =int.Parse( Setup.FlowRate.ToString());
                PanelSession.Tag = Setup;
                listBox1.Items.Clear();
                lstCmd.Items.Clear();
                chkAllowRepeat.Checked = Setup.AllowDuplicate ?? false;
                listBox1.ValueMember = "SessionCode";
                listBox1.DisplayMember = "Description";
                foreach (string Cmd in Setup.TSessionForbidCmd)
                    this.lstCmd.Items.Add(Cmd);


                comboBoxEx2.Items.Clear();
                if (AsyncStackNet.Instance.ASyncSetup.GroupCollection == null) return;
                foreach (SDKGroup group in AsyncStackNet.Instance.ASyncSetup.GroupCollection) {
                    comboBoxEx2.Items.Add(new { Text = group.groupName, Value = group.groupCode });
                }
                if (!string.IsNullOrEmpty(Setup.SpecialIntervalList)) {
                    foreach (Match m in Regex.Matches(Setup.SpecialIntervalList, @"\^([A-Z0-9]+)\|(\d+)\,", RegexOptions.IgnoreCase | RegexOptions.Multiline)) {
                        listBox1.Items.Add(new TSessionSetup() { SessionCode = m.Groups[1].Value, GroupCode = m.Groups[2].Value, Description = string.Format(@"{0}    {1}", m.Groups[1].Value, m.Groups[2].Value) });

                    }
                }

                if (!string.IsNullOrEmpty(Setup.GroupCode))
                    foreach (object group in this.comboBoxEx2.Items) {
                        string GValue = group.GetType().GetProperty("Value").GetValue(group, null).ToString();
                        //string GText = group.GetType().GetProperty("Text").GetValue(group, null).ToString();
                        if (GValue == Setup.GroupCode)
                            comboBoxEx2.SelectedItem = group;
                    }
                PanelSession.Enabled = true;
                if (!AsyncStackNet.Instance.ASyncSetup.SessionCollection.Contains(new TSessionSetup(Setup.SessionCode)))
                    return;
                this.comboTree1.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
                this.comboTree1.ValueMember = @"MonthString";
                this.comboTree1.DisplayMembers = @"MonthString,Traffic,UpdateDate";
                this.comboTree1.DataSource = AsyncStackNet.Instance.ASyncSetup.SessionCollection[
                    AsyncStackNet.Instance.ASyncSetup.SessionCollection.IndexOf(new TSessionSetup(Setup.SessionCode))].Traffics;
                //PanelSession.act
                tabControl1.SelectedTab = tabItem2;
                break;
            }
        }

        /// <summary>
        /// Handles the Click event of the btnInsert control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnInsert_Click(object sender, EventArgs e) {
            txtDescription.Text = string.Empty;
            txtExpire.Value = 10;
            txtFlow.Value = 100;
            txtPassword.Text = string.Empty;
            txtSessionName.Text = string.Empty;
            PanelSession.Tag = null;
            PanelSession.Enabled = true;
            //PanelSession.Show();
            comboBoxEx2.Items.Clear();
            PanelSession.Tag = new TSessionSetup() { Traffics=new List<SocketTraffic>() };
            if (AsyncStackNet.Instance.ASyncSetup.GroupCollection == null) return;
            foreach (SDKGroup group in AsyncStackNet.Instance.ASyncSetup.GroupCollection) {
                comboBoxEx2.Items.Add(new { Text = group.groupName, Value = group.groupCode });
            }
            tabControl1.SelectedTab = tabItem2;
        }

        /// <summary>
        /// Handles the Click event of the btnSingleSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnSingleSave_Click(object sender, EventArgs e) {
            PanelSession.Enabled = false;

            string groupCode = string.Empty;
            if (comboBoxEx2.SelectedItem != null)
                groupCode = comboBoxEx2.SelectedItem.GetType().GetProperty("Value").GetValue(comboBoxEx2.SelectedItem, null).ToString();


            TSessionSetup Setup = new TSessionSetup() { 
                 Description=txtDescription.Text,
                  IsOpen =chkIsOpen.Checked,
                   SessionCode=txtSessionName.Text,
                 GroupCode = groupCode,
                    SessionExpire=txtExpire.Value,
                     SessionPass=txtPassword.Text,
                      FlowRate=float.Parse(this.txtFlow.Value.ToString()),
                       AllowDuplicate=chkAllowRepeat.Checked,
                 SpecialIntervalList =string.Empty
            };

            Setup.Traffics = (PanelSession.Tag as TSessionSetup).Traffics;
            foreach (TSessionSetup item in this.listBox1.Items)
            {
                Setup.SpecialIntervalList += string.Format(@"^{0}|{1},", item.SessionCode,item.GroupCode);
            }
            this.listBox1.Items.Clear();
            foreach (string Cmd in this.lstCmd.Items) {
                Setup.TSessionForbidCmd.Add(Cmd);
            }
            if (!AsyncStackNet.Instance.ASyncSetup.SessionCollection.Contains(Setup))
                AsyncStackNet.Instance.ASyncSetup.SessionCollection.Add(Setup);
            else{
                int indexOf = AsyncStackNet.Instance.ASyncSetup.SessionCollection.IndexOf(this.PanelSession.Tag as TSessionSetup);
                AsyncStackNet.Instance.ASyncSetup.SessionCollection[indexOf] = Setup;
            }
            btnSave_Click(null, EventArgs.Empty);
            this.OnLoad(EventArgs.Empty);
        }

        

       

        
        /// <summary>
        /// Gets the name of the button.
        /// </summary>
        /// <value>The name of the button.</value>
        public override string ButtonName {
            get { return "用户管理"; }
        }

        /// <summary>
        /// Gets the image icon.
        /// </summary>
        /// <value>The image icon.</value>
        public override string ImageIcon {
            get { return "Hourglass.png"; }
        }

        /// <summary>
        /// Handles the Click event of the btnDeleteSpecial control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        //private void btnDeleteSpecial_Click(object sender, EventArgs e)
        //{
        //    this.comboBoxEx3.Items.RemoveAt(this.comboBoxEx3.SelectedIndex);
        //}

        /*
        /// <summary>
        /// Handles the Click event of the btnUpdateSpecial control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnUpdateSpecial_Click(object sender, EventArgs e) {
            if (!Regex.IsMatch(this.comboBoxEx3.Text, @"^([A-Z0-9]+)\|(\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase)) {
                MessageBox.Show(@"输入格式不正确，正确格式为：SS|25", @"格式错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Match m = Regex.Match(this.comboBoxEx3.Text, @"^([A-Z0-9]+)\|(\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            this.comboBoxEx3.Items[this.comboBoxEx3.SelectedIndex] = new { Interval = m.Groups[2].Value, Command = string.Format(@"{1}   {0}", m.Groups[2].Value, m.Groups[1].Value) };
        }
        */


        /// <summary>
        /// Handles the Click event of the btnNewSpecial control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        //private void btnNewSpecial_Click(object sender, EventArgs e) {
        //    TSessionSetup setup = PanelSession.Tag as TSessionSetup;
        //    if (!Regex.IsMatch(this.comboBoxEx3.Text, @"^([A-Z0-9]+)\|(\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase)) {
        //        MessageBox.Show(@"输入格式不正确，正确格式为：SS|25", @"格式错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        return;
        //    }
        //    Match m = Regex.Match(this.comboBoxEx3.Text, @"^([A-Z0-9]+)\|(\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        //    this.comboBoxEx3.Items.Add(new TSessionSetup() { SessionCode = m.Groups[1].Value, Description = string.Format(@"{1} {0}", m.Groups[2].Value, m.Groups[1].Value), SessionPass = m.Groups[2].Value });
            
        //}

        /// <summary>
        /// Handles the SelectedTabChanged event of the tabControl1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DevComponents.DotNetBar.TabStripTabChangedEventArgs"/> instance containing the event data.</param>
        private void tabControl1_SelectedTabChanged(object sender, DevComponents.DotNetBar.TabStripTabChangedEventArgs e) {
            if (PanelSession.Tag == null) return;
            TSessionSetup Setup = PanelSession.Tag as TSessionSetup;
            this.comboTree1.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.comboTree1.ValueMember = @"MonthString";
            this.comboTree1.DisplayMembers = @"MonthString,Traffic,UpdateDate";
            if (string.IsNullOrEmpty(Setup.SessionCode) || AsyncStackNet.Instance.ASyncSetup.SessionCollection == null || AsyncStackNet.Instance.ASyncSetup.SessionCollection.Count==0) return;
            if (AsyncStackNet.Instance.ASyncSetup.SessionCollection[
                AsyncStackNet.Instance.ASyncSetup.SessionCollection.IndexOf(new TSessionSetup(Setup.SessionCode))].Traffics == null)
                return;
            this.comboTree1.DataSource = AsyncStackNet.Instance.ASyncSetup.SessionCollection[
                AsyncStackNet.Instance.ASyncSetup.SessionCollection.IndexOf(new TSessionSetup(Setup.SessionCode))].Traffics;
        }

        /// <summary>
        /// Handles the Click event of the btnClearTraffic control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnClearTraffic_Click(object sender, EventArgs e) {
            TSessionSetup Setup = lstSession.SelectedItems[0].Tag as TSessionSetup;
            if (this.comboTree1.SelectedValue.ToString() == DateTime.Now.ToString(@"yyyyMM")) { MessageBox.Show(@"当月流量统计不允许清除！", @"操作错误", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            List<SocketTraffic> Traffics = AsyncStackNet.Instance.ASyncSetup.SessionCollection[
                AsyncStackNet.Instance.ASyncSetup.SessionCollection.IndexOf(new TSessionSetup(Setup.SessionCode))].Traffics;
            Traffics.RemoveAt(Traffics.IndexOf(new SocketTraffic() { MonthString = this.comboTree1.SelectedValue.ToString() }));
            AsyncStackNet.Instance.BeginRateUpdate(new AsyncCallback(delegate(IAsyncResult iar)
            {
                AsyncStackNet.Instance.EndRateUpdate(iar);
                btnSessionEdit_Click(null, EventArgs.Empty);
            }));
        }

        /// <summary>
        /// Handles the DoubleClick event of the lstSession control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void lstSession_DoubleClick(object sender, EventArgs e) {
            btnSessionEdit_Click(null, EventArgs.Empty);
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the lstSession control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void lstSession_SelectedIndexChanged(object sender, EventArgs e) {
            if (lstSession.SelectedItems.Count != 1) {
                btnDelete.Enabled = false;
                btnInsert.Enabled = false;
                btnSessionEdit.Enabled = false;
                return;
            }
            btnDelete.Enabled = true;
            btnInsert.Enabled = true;
            btnSessionEdit.Enabled = true;

        }

        /// <summary>
        /// Handles the Click event of the btnAddRegCommand control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnAddRegCommand_Click(object sender, EventArgs e)
        {
            this.lstCmd.Items.Add(txtRegCmd.Text.Trim());
        }

        /// <summary>
        /// Handles the Click event of the btnRemoveCommand control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnRemoveCommand_Click(object sender, EventArgs e)
        {
            foreach (object item in this.lstCmd.SelectedItems) {
                this.lstCmd.Items.Remove(item);
            }
        }

        /// <summary>
        /// Handles the Click event of the buttonX1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void buttonX1_Click(object sender, EventArgs e)
        {
            this.listBox1.Items.Add(new TSessionSetup() { Description = string.Format(@"{0}   {1}", textBoxX1.Text.Trim(), integerInput1.Value), GroupCode = integerInput1.Value.ToString(), SessionCode = textBoxX1.Text.Trim() });
        }

        /// <summary>
        /// Handles the Click event of the buttonX2 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void buttonX2_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < this.listBox1.SelectedItems.Count; i++)
                this.listBox1.Items.Remove(this.listBox1.SelectedItems[i]);
        }

    }
}
