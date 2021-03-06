﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Windows.Forms;
using DevComponents.DotNetBar.Controls;
using eTerm.AsyncSDK.Net;
using eTerm.AsyncSDK;
using eTerm.AsyncSDK.Core;
using System.IO;
using System.Net;
using eTerm.AsyncSDK.Base;
using DevComponents.DotNetBar;
using ASync.MiddleWare;
using System.Reflection;
using eTerm.AsyncSDK.Util;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ASyncSDK.Office {
    public partial class frmMain : Office2007RibbonForm {
        #region 初始化
        ListViewGroup group1 = new ListViewGroup("活动连接", HorizontalAlignment.Center);
        ListViewGroup group2 = new ListViewGroup("非活动连接", HorizontalAlignment.Center);
        private string __resetHour = @"0600";
        private System.Threading.Timer __SvrUpdateInterval;
        private System.Threading.Timer __SvrResetInterval;
        public frmMain()
        {
            InitializeComponent();
            this.Load += new EventHandler(
                    delegate(object sender, EventArgs e)
                    {
                        this.pbSvrUpdate.Visible = false;
                        this.stripSvrUpdate.Visible = false;
                        stripSvrUpdate.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);

                        __SvrUpdateInterval = new System.Threading.Timer(new System.Threading.TimerCallback(delegate {
                            this.SvrUpdate();
                        }),null,15*1000,1000*60*60);
                        __SvrResetInterval = new System.Threading.Timer(new System.Threading.TimerCallback(delegate {
                            this.SvrReset();
                        }),null,1000,1000*60);
                        notifyIcon1.Visible = false;
                        //statusServer.ForeColor = Color.Red;
                        statusServer.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
                        statusServer.ForeColor = System.Drawing.SystemColors.HotTrack;
                        try
                        {
                            string SvrVersionFile = @"Version.Xml";
                            DirectoryInfo SvrPath = new DirectoryInfo(@".\");
                            if (new FileInfo(string.Format(@"{0}{1}", SvrPath.FullName, SvrVersionFile)).Exists)
                            {
                                XElement root = XElement.Load(new FileInfo(string.Format(@"{0}{1}", SvrPath.FullName, SvrVersionFile)).FullName);
                                XElement VersionStr = root.Element(@"SvrReset");
                                __resetHour = VersionStr.Value;
                            }
                        }
                        catch { }
                        statusInfo.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
                        statusInfo.ForeColor = Color.Red ;
                        notifyIcon1.Text = this.Text;
                        lstAsync.Groups.AddRange(new ListViewGroup[] { group1, group2 });
                            foreach (object itemValue in Enum.GetValues(DevComponents.DotNetBar.eStyle.Windows7Blue.GetType())) {
                                ButtonItem btnItem = new ButtonItem() { ButtonStyle = eButtonStyle.ImageAndText, Text = itemValue.ToString(), Tag=itemValue };
                                btnItem.Image = global::ASyncSDK.Office.Properties.Resources.Flag2_Green;
                                btnItem.Click += new EventHandler(
                                        delegate(object btnSender, EventArgs btnEvent) {
                                            this.styleManager1.ManagerStyle = (eStyle)(btnItem as ButtonItem).Tag;
                                        }
                                    );
                                this.btnSkin.SubItems.AddRange(new DevComponents.DotNetBar.BaseItem[] {
                                btnItem
                            });
                        }
                            this.btnStart_Click(null, EventArgs.Empty);
                    }
                );
        }
        #endregion

        #region 服务器重启
        /// <summary>
        /// SVRs the reset.
        /// </summary>
        private void SvrReset() {
            if (Regex.IsMatch(__resetHour,DateTime.Now.ToString(@"HHmm,"), RegexOptions.IgnoreCase| RegexOptions.Multiline))
            {
                //UpdateStatusText(stripSvrUpdate, string.Format(@"重启时间已到，1分钟后系统将自动重新启动。", @""));
                //new System.Threading.Timer(delegate
                {
                    AsyncStackNet.Instance.BeginRateUpdate(new AsyncCallback(delegate(IAsyncResult iar)
                    {
                        AsyncStackNet.Instance.EndRateUpdate(iar);
                        iar.AsyncWaitHandle.Close();
                    }));
                    AsyncStackNet.Instance.EndAsync();
                    AsyncStackNet.Instance.BeginAsync();
                    UpdateStatusText(stripSvrUpdate, string.Format(@"配置于时间“{0}”复位成功。", DateTime.Now.ToString(@"HH:mm")));
                }//, null,1000*60, System.Threading.Timeout.Infinite);
            }
        }
        #endregion

        #region 自动更新主线程

        /// <summary>
        /// Gets the app version.
        /// </summary>
        /// <returns></returns>
        private Version GetAppVersion() {
            string SvrVersionFile = @"Version.Xml";
            DirectoryInfo SvrPath = new DirectoryInfo(@".\");
            Version AppVersion = new Version(1, 0, 0);
            if (new FileInfo(string.Format(@"{0}{1}", SvrPath.FullName, SvrVersionFile)).Exists)
            {
                XElement root = XElement.Load(new FileInfo(string.Format(@"{0}{1}", SvrPath.FullName, SvrVersionFile)).FullName);
                XElement VersionStr = root.Element(@"SvrVersion");
                AppVersion = new Version(VersionStr.Value);
            }
            return AppVersion;
        }

        /// <summary>
        /// SVRs the update.
        /// </summary>
        private void SvrUpdate() {
            string SvrVersionFile = @"Version.Xml";
            DirectoryInfo SvrPath = new DirectoryInfo(@".\");
            FileInfo VersionFile = new FileInfo(string.Format(@"{0}{1}", SvrPath.FullName, SvrVersionFile));
            Version AppVersion = null;
            try
            {
                AppVersion = GetAppVersion();
                FtpClient SvrFtp = new FtpClient(AsyncStackNet.Instance.ASyncSetup.CoreServer, string.Empty, string.Empty);
                SvrFtp.Login();
                SvrFtp.Download(SvrVersionFile, string.Format(@"{0}{1}", SvrPath.FullName, SvrVersionFile));
                if (AppVersion.Equals(GetAppVersion()))
                {
                    UpdateStatusText(stripSvrUpdate, string.Format(@"无可用更新！",@""));
                    return;
                }
                UpdateStatusText(stripSvrUpdate, string.Format(@"发现更新，新版本号为：{0}", GetAppVersion().ToString()));

                XElement root = XElement.Load(new FileInfo(string.Format(@"{0}{1}", SvrPath.FullName, SvrVersionFile)).FullName);
                DirectoryInfo UpdateFolder= new DirectoryInfo(string.Format(@"{0}{1}\", SvrPath.FullName, @"SvrUpdate"));
                if (!UpdateFolder.Exists) UpdateFolder.Create();
                List<XElement> svrItems = root.Element(@"SvrFiles").Elements(@"Item").ToList<XElement>();
                SvrUpdateMaxQueuen(svrItems.Count);
                int SvrIndex=0;
                foreach (XElement element in svrItems)
                {
                    try {
                        SvrFtp.Download(element.Value, string.Format(@"{0}{1}", UpdateFolder.FullName, element.Value),true);
                        SvrUpdateCurrentQueuen(++SvrIndex);
                        UpdateStatusText(stripSvrUpdate, string.Format(@"更新{0}完成！", element.Value));
                    }
                    catch(Exception ex) {
                        UpdateStatusText(stripSvrUpdate, string.Format(@"更新{0}异常：{1}",element.Value, ex.Message));
                    }
                }
                UpdateStatusText(stripSvrUpdate, string.Format(@"文件更新完成，下次重启服务后将生效！", @""));
                //new System.Threading.Timer(delegate {
                //    Application.Exit();
                //    Application.Restart();
                //},null, 5000, 0);
                SvrFtp.Close();
            }
            catch(Exception ex) {
                UpdateStatusText(stripSvrUpdate, string.Format(@"更新发生异常：{0}", ex.Message));
                __SvrUpdateInterval.Dispose();
            }
        }
        #endregion

        #region 更新进度条线程操作
        private delegate void SvrUpdateCallbackMaxQueuen(int MaxLength);

        /// <summary>
        /// SVRs the update max queuen.
        /// </summary>
        /// <param name="MaxLength">Length of the max.</param>
        private void SvrUpdateMaxQueuen(int MaxLength) {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new SvrUpdateCallbackMaxQueuen(SvrUpdateMaxQueuen), MaxLength);
                return;
            }
            try
            {
                this.pbSvrUpdate.Visible = true;
                this.pbSvrUpdate.Maximum = MaxLength;
            }
            catch { }
        }

        private delegate void SvrUpdateCallbackCurrentQueuen(int CurrentValue);

        /// <summary>
        /// SVRs the update max queuen.
        /// </summary>
        /// <param name="MaxLength">Length of the max.</param>
        private void SvrUpdateCurrentQueuen(int CurrentValue)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new SvrUpdateCallbackCurrentQueuen(SvrUpdateCurrentQueuen), CurrentValue);
                return;
            }
            try
            {
                this.pbSvrUpdate.Value = CurrentValue;
            }
            catch { }
        }


        #endregion

        #region 客户端会话状态会话
        /// <summary>
        /// Disconnects the T session.
        /// </summary>
        /// <param name="TSession">The T session.</param>
        private void DisconnectTSession(eTerm363Session TSession) {
            if (this.InvokeRequired) {
                this.BeginInvoke(new TSessionCallback(DisconnectTSession), TSession);
                return;
            }
            try {
                //SQLiteExecute.Instance.BeginExecute(TSession.userName, (TSession.AsyncSocket.RemoteEndPoint as IPEndPoint).Address.ToString(),new byte[]{}, @"DisconnectTSession");
                this.lstSession.Items.Remove(this.lstSession.Items[TSession.SessionId.ToString()]);
            }
            catch { }
        }


        /// <summary>
        /// Accepts the T session.
        /// </summary>
        /// <param name="TSession">The T session.</param>
        private void AcceptTSession(eTerm363Session TSession) {
            if (this.InvokeRequired) {
                this.BeginInvoke(new TSessionCallback(AcceptTSession), TSession);
                return;
            }
            try {
                ListViewItem item = new ListViewItem(new string[] {
                (TSession.AsyncSocket.RemoteEndPoint as IPEndPoint).ToString(),
                string.Empty,
                @"0 KByes",
                @"0",
                @"0",
                @"00:00:00"
            }, @"Circle_Yellow.png") { Name = TSession.SessionId.ToString() };
                item.Tag = TSession;
                //SQLiteExecute.Instance.BeginExecute(TSession.userName, (TSession.AsyncSocket.RemoteEndPoint as IPEndPoint).Address.ToString(), new byte[]{}, @"AcceptTSession");
                this.lstSession.Items.Add(item);
            }
            catch { }
        }

        /// <summary>
        /// Updates the T session.
        /// </summary>
        /// <param name="TSession">The T session.</param>
        private void updateTSessionRead(eTerm363Session TSession) {
            if (this.InvokeRequired) {
                this.BeginInvoke(new TSessionCallback(updateTSessionRead), TSession);
                return;
            }
            try {
                //SQLiteExecute.Instance.BeginExecute(TSession.userName, (TSession.AsyncSocket.RemoteEndPoint as IPEndPoint).Address.ToString(), TSession.LastPacket.OriginalBytes, @"TSessionReadPacket");
                ListViewItem item = this.lstSession.Items[TSession.SessionId.ToString()];
                item.ImageKey = @"Circle_Yellow.png";// ? @"Circle_Yellow.png" : @"Circle_Green.png";
                item.SubItems[1].Text = TSession.userName;
                item.SubItems[2].Text = string.Format(@"{0} KBytes", TSession.TotalBytes.ToString(@"f2"));
                item.SubItems[3].Text = TSession.TotalCount.ToString();
                item.SubItems[4].Text = string.Format(@"{0} Bytes", TSession.CurrentBytes.ToString(@"f2"));
                item.SubItems[5].Text = TSession.LastActive.ToString(@"HH:mm:ss");
            }
            catch { }
        }

        /// <summary>
        /// Updates the T session sent.
        /// </summary>
        /// <param name="TSession">The T session.</param>
        private void updateTSessionSent(eTerm363Session TSession) {
            if (this.InvokeRequired) {
                this.BeginInvoke(new TSessionCallback(updateTSessionSent), TSession);
                return;
            }
            try {
                //SQLiteExecute.Instance.BeginExecute(TSession.userName, (TSession.AsyncSocket.RemoteEndPoint as IPEndPoint).Address.ToString(), TSession.LastPacket.OriginalBytes, @"TSessionSent");
                ListViewItem item = this.lstSession.Items[TSession.SessionId.ToString()];
                item.ImageKey = @"Circle_Green.png";// ? @"Circle_Yellow.png" : @"Circle_Green.png";
                item.SubItems[1].Text = TSession.userName;
                item.SubItems[2].Text = string.Format(@"{0} KBytes", TSession.TotalBytes.ToString(@"f2"));
                item.SubItems[3].Text = TSession.TotalCount.ToString();
                item.SubItems[4].Text = string.Format(@"{0} Bytes", TSession.CurrentBytes.ToString(@"f2"));
                item.SubItems[5].Text = TSession.LastActive.ToString(@"HH:mm:ss");
            }
            catch { }
        }
        #endregion

        #region 客户端会话日志
        /// <summary>
        /// Ts the session log.
        /// </summary>
        /// <param name="TSession">The T session.</param>
        private void TSessionLog(string SessionId, string TSessionCmd, string LogType, string Flag) {
            if (this.InvokeRequired) {
                this.BeginInvoke(new TSessionLogCallback(TSessionLog), SessionId, TSessionCmd, LogType, Flag);
                return;
            }
            try {
                if (this.lstTSessionLog.Items.Count >= 100) this.lstTSessionLog.Items.Clear();
                ListViewItem item = new ListViewItem(new string[]{
                (this.lstTSessionLog.Items.Count+1).ToString(),
                SessionId,
                LogType,
                TSessionCmd,
                Flag,
                DateTime.Now.ToString(@"HH:mm:ss")
            });
                this.lstTSessionLog.Items.Insert(0, item);
            }
            catch { }
        }

        /// <summary>
        /// TAs the sync log.
        /// </summary>
        /// <param name="SessionId">The session id.</param>
        /// <param name="TSessionCmd">The T session CMD.</param>
        /// <param name="LogType">Type of the log.</param>
        /// <param name="Flag">The flag.</param>
        private void TASyncLog(string SessionId, string TSessionCmd, string LogType, string Flag) {
            if (this.InvokeRequired) {
                this.BeginInvoke(new TSessionLogCallback(TASyncLog), SessionId, TSessionCmd, LogType, Flag);
                return;
            }
            try {
                if (this.lstASyncLog.Items.Count >= 100) this.lstASyncLog.Items.Clear();
                ListViewItem item = new ListViewItem(new string[]{
                (this.lstASyncLog.Items.Count+1).ToString(),
                SessionId,
                LogType,
                TSessionCmd,
                Flag,
                DateTime.Now.ToString(@"HH:mm:ss")
            });
                this.lstASyncLog.Items.Insert(0, item);
            }
            catch { }
        }
        #endregion

        #region UI代理定义
        public delegate void UpdateListViewItem(ListViewEx ViewEx, string ImageKey, string Id, float TotalBytes, string Reconnect);

        public delegate void UpdateStatusTextDelegate(ToolStripStatusLabel targetLable, string Text);

        public delegate void ASynConnectCallback(eTerm443Async ASync);

        public delegate void TSessionCallback(eTerm363Session TSession);

        public delegate void TSessionLogCallback(string SessionId, string TSessionCmd, string LogType,string Flag);

        /// <summary>
        /// Appends the A syn connect.
        /// </summary>
        /// <param name="ASync">The A sync.</param>
        private void appendASynConnect(eTerm443Async ASync) {
            if (this.InvokeRequired) {
                this.BeginInvoke(new ASynConnectCallback(appendASynConnect), ASync);
                return;
            }
            try {
                string SessionId = string.Format(@"{0}{1}{2}", ASync.RemoteEP.ToString(), ASync.userName, ASync.IsSsl);
                //SQLiteExecute.Instance.BeginExecute(ASync.userName, (ASync.AsyncSocket.RemoteEndPoint as IPEndPoint).Address.ToString(), new byte[]{}, @"ASynConnect");
                ListViewItem connectItem;
                if (this.lstAsync.Items.ContainsKey(SessionId)) {
                    connectItem = this.lstAsync.Items[SessionId];
                    connectItem.ImageKey = @"Circle_Green.png";
                    connectItem.SubItems[4].Text = ASync.ReconnectCount.ToString();
                    connectItem.Group = group1;
                    return;
                }

                connectItem = new ListViewItem(
                        new string[] {
                    ASync.RemoteEP.ToString(),
                    ASync.userName,
                    ASync.TotalBytes.ToString("f2"),
                    ASync.TotalCount.ToString(),
                    ASync.ReconnectCount.ToString(),
                    string.Format(@"{0} KBytes",ASync.CurrentBytes.ToString(@"f2")),
                    ASync.LastActive.ToString(@"HH:mm:ss")
                }, group1) { Name = SessionId };
                    connectItem.Tag = ASync;
                    connectItem.ImageKey = @"Circle_Green.png";
                    this.lstAsync.Items.Add(connectItem);
            }
            catch { }
        }

        /// <summary>
        /// Disconnects the A sync.
        /// </summary>
        /// <param name="ASync">The A sync.</param>
        private void disconnectASync(eTerm443Async ASync) {
            if (this.InvokeRequired) {
                this.BeginInvoke(new ASynConnectCallback(disconnectASync), ASync);
                return;
            }
            try {
                //SQLiteExecute.Instance.BeginExecute(ASync.userName, (ASync.AsyncSocket.RemoteEndPoint as IPEndPoint).Address.ToString(), new byte[]{}, @"ASynDisconnect");
                appendASynConnect(ASync);
                string SessionId = string.Format(@"{0}{1}{2}", ASync.RemoteEP.ToString(), ASync.userName, ASync.IsSsl);
                ListViewItem item = this.lstAsync.Items[SessionId];
                item.ImageKey = @"Circle_Red.png";
                item.Group = group2;
            }
            catch { }
        }

        /// <summary>
        /// Updates the A sync.
        /// </summary>
        /// <param name="ASync">The A sync.</param>
        private void updateASync(eTerm443Async ASync) {
            if (this.InvokeRequired) {
                this.BeginInvoke(new ASynConnectCallback(updateASync), ASync);
                return;
            }
            try {
                string SessionId = string.Format(@"{0}{1}{2}", ASync.RemoteEP.ToString(), ASync.userName, ASync.IsSsl);
                ListViewItem item = this.lstAsync.Items[SessionId];
                item.SubItems[2].Text =string.Format(@"{0} KBytes", ASync.TotalBytes.ToString(@"f2"));
                item.SubItems[3].Text = ASync.TotalCount.ToString();
                item.SubItems[5].Text = ASync.CurrentBytes.ToString(@"f2");
                item.SubItems[6].Text = ASync.LastActive.ToString(@"HH:mm:ss");
                item.ImageKey = @"Circle_Yellow.png";

            }
            catch { }
        }

        /// <summary>
        /// Packets the A sync.
        /// </summary>
        /// <param name="ASync">The A sync.</param>
        private void packetASync(eTerm443Async ASync) {
            if (this.InvokeRequired) {
                this.BeginInvoke(new ASynConnectCallback(packetASync), ASync);
                return;
            }
            try {
                string SessionId = string.Format(@"{0}{1}{2}", ASync.RemoteEP.ToString(), ASync.userName, ASync.IsSsl);
                ListViewItem item = this.lstAsync.Items[SessionId];
                item.SubItems[2].Text = string.Format(@"{0} KBytes", ASync.TotalBytes.ToString(@"f2"));
                item.SubItems[3].Text = ASync.TotalCount.ToString();
                item.SubItems[5].Text =string.Format(@"{0} Bytes", ASync.CurrentBytes.ToString(@"f0"));
                item.SubItems[6].Text = ASync.LastActive.ToString(@"HH:mm:ss");
                item.ImageKey = @"Circle_Green.png";
            }
            catch { }
        }
        #endregion

        #region 关闭事件
        /// <summary>
        /// Handles the Click event of the btnExit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnExit_Click(object sender, EventArgs e) {
            btnClose_Click(sender, e);
        }


        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Form.Closing"/> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.ComponentModel.CancelEventArgs"/> that contains the event data.</param>
        protected override void OnClosing(CancelEventArgs e)
        {
            Hide();
            this.ShowInTaskbar = false;
            notifyIcon1.Visible = true;
            e.Cancel = true;
        }
        #endregion

        #region 状态栏文本更新
        /// <summary>
        /// Updates the status text.
        /// </summary>
        /// <param name="targetLable">The target lable.</param>
        /// <param name="Text">The text.</param>
        private void UpdateStatusText(ToolStripStatusLabel targetLable, string Text) {
            if (this.InvokeRequired) {
                this.BeginInvoke(new UpdateStatusTextDelegate(UpdateStatusText), targetLable, Text);
                return;
            }
            try {
                targetLable.Visible = true;
                targetLable.Text = Text;
            }
            catch { }
        }

        #endregion

        #region 服务方法

        /// <summary>
        /// Stops the service.
        /// </summary>
        private void StopService() {
            AsyncStackNet.Instance.EndAsync();

            //按钮控制
            this.btnStart.Enabled = true;
            this.btnStop.Enabled = false;
            this.btnReset.Enabled = false;


            btnStartService.Enabled = true;
            btnStopService.Enabled = false;
            btnResetService.Enabled = false;
        }

        /// <summary>
        /// Starts the service.
        /// </summary>
        private void StartService() {
            //SQLiteExecute.Instance.BeginExecute(@"", @"", new byte[]{}, @"START SERVICE");
            AsyncStackNet.Instance.CrypterKey = TEACrypter.GetDefaultKey; 
            AsyncStackNet.Instance.ASyncSetupFile = new FileInfo(@"Setup.Bin").FullName;

            AsyncStackNet.Instance.AfterIntercept = new InterceptCallback(delegate(AsyncEventArgs<eTerm363Packet, eTerm363Packet, eTerm363Session> e)
            {
                TSessionLog(string.IsNullOrEmpty(e.Session.userName) ? e.Session.SessionId.ToString() : e.Session.userName,
string.Empty,
@"AfterIntercept", @"SUCCESS");
                e.Session.SendPacket(__eTerm443Packet.BuildSessionPacket(e.Session.SID, e.Session.RID, "指令被禁止"));
            });


            AsyncStackNet.Instance.RID = 0x51;
            AsyncStackNet.Instance.SID = 0x27;
            AsyncStackNet.Instance.OnExecuteException += new EventHandler<ErrorEventArgs>(
                    delegate(object sender, ErrorEventArgs e)
                    {

                    }
                );

            AsyncStackNet.Instance.OnAsyncConnect += new EventHandler<AsyncEventArgs<eTerm443Async>>(
                    delegate(object sender, AsyncEventArgs<eTerm443Async> e) {
                        //appendASynConnect(e.Session);
                        TASyncLog(e.Session.userName, string.Empty, @"OnAsyncConnect", @"SUCCESS");
                    }
                );

            AsyncStackNet.Instance.OnBeginConnect += new EventHandler<AsyncEventArgs<eTerm443Async>>(
                    delegate(object sender, AsyncEventArgs<eTerm443Async> e) {
                        appendASynConnect(e.Session);
                        TASyncLog(e.Session.userName, string.Empty, @"OnBeginConnect", @"SUCCESS");
                    }
                );

            AsyncStackNet.Instance.OnRateEvent += new EventHandler(
                    delegate(object sender, EventArgs e) {
                        AsyncStackNet.Instance.BeginRateUpdate(new AsyncCallback(delegate(IAsyncResult iar) {
                            AsyncStackNet.Instance.EndRateUpdate(iar);
                            iar.AsyncWaitHandle.Close();
                        }));
                    }
                );

            AsyncStackNet.Instance.OnCoreConnect += new EventHandler<AsyncEventArgs<eTerm443Async>>(
                    delegate(object sender, AsyncEventArgs<eTerm443Async> e)
                    {
                        UpdateStatusText(lableLocalIp, string.Format(@"本机IP：{0}", (e.Session.AsyncSocket.LocalEndPoint as IPEndPoint).Address.ToString()));
                        UpdateStatusText(statusInfo, string.Format(@"中心服务器连接成功！", AsyncStackNet.Instance.ASyncSetup.CoreServer, AsyncStackNet.Instance.ASyncSetup.CoreServerPort));
                    }
                );

            AsyncStackNet.Instance.OnCoreDisconnect += new EventHandler<AsyncEventArgs<eTerm443Async>>(
                    delegate(object sender, AsyncEventArgs<eTerm443Async> e)
                    {
                        UpdateStatusText(statusInfo, string.Format(@"中心服务器连接已断开！", AsyncStackNet.Instance.ASyncSetup.CoreServer, AsyncStackNet.Instance.ASyncSetup.CoreServerPort));
                    }
                );
            AsyncStackNet.Instance.OnSDKTimeout += new EventHandler<ErrorEventArgs>(
                    delegate(object sender, ErrorEventArgs e) {
                        UpdateStatusText(statusInfo, @"授权已到期,系统将自动关闭！");
                        AsyncStackNet.Instance.BeginRateUpdate(new AsyncCallback(delegate(IAsyncResult iar) {
                            AsyncStackNet.Instance.EndRateUpdate(iar);
                            iar.AsyncWaitHandle.Close();
                        }));
                        new System.Threading.Timer(new System.Threading.TimerCallback(delegate(object timerSender) {
                            Application.Exit();
                        }), null, 5 * 60 * 1000, System.Threading.Timeout.Infinite);
                    }
                );
            AsyncStackNet.Instance.OnSystemException += new EventHandler<ErrorEventArgs>(
                    delegate(object sender, ErrorEventArgs e) {
                        UpdateStatusText(statusInfo, e.GetException().Message);
                    }
                );
            AsyncStackNet.Instance.OnAsyncDisconnect += new EventHandler<AsyncEventArgs<eTerm443Async>>(
                    delegate(object sender, AsyncEventArgs<eTerm443Async> e)
                    {
                        disconnectASync(e.Session);
                        //SQLiteExecute.Instance.BeginExecute(e.Session.userName,(e.Session.AsyncSocket.RemoteEndPoint as IPEndPoint).Address.ToString(), @"", @"AsyncDisconnect");
                        TASyncLog(e.Session.userName, string.Empty, @"OnAsyncDisconnect", @"SUCCESS");
                    }
                );

            AsyncStackNet.Instance.OnTSessionPacketSent += new EventHandler<AsyncEventArgs<eTerm363Packet, eTerm363Packet, eTerm363Session>>(
                    delegate(object sender, AsyncEventArgs<eTerm363Packet, eTerm363Packet, eTerm363Session> e)
                    {
                        updateTSessionSent(e.Session);
                        TSessionLog(string.IsNullOrEmpty(e.Session.userName) ? e.Session.SessionId.ToString() : e.Session.userName,
string.Empty,
@"OnTSessionPacketSent", @"SUCCESS");
                        //if (string.IsNullOrEmpty(e.Session.userName)) return;
                        //SQLiteExecute.Instance.BeginExecute(e.Session.userName, (e.Session.AsyncSocket.RemoteEndPoint as IPEndPoint).Address.ToString(), Encoding.GetEncoding(@"gb2312").GetString(e.Session.UnInPakcet(e.OutPacket)), @"TSessionPacketSent");
                    }
                );

            AsyncStackNet.Instance.OnAsyncPacketSent += new EventHandler<AsyncEventArgs<eTerm443Packet, eTerm443Packet, eTerm443Async>>(
                    delegate(object sender, AsyncEventArgs<eTerm443Packet, eTerm443Packet, eTerm443Async> e)
                    {
                        TASyncLog(e.Session.userName, Encoding.GetEncoding(@"gb2312").GetString(e.Session.UnInPakcet(e.OutPacket)), @"OnAsyncPacketSent", @"SUCCESS");
                        updateASync(e.Session);
                    }
                );

            AsyncStackNet.Instance.OnAsyncReadPacket += new EventHandler<AsyncEventArgs<eTerm443Packet, eTerm443Packet, eTerm443Async>>(
                    delegate(object sender, AsyncEventArgs<eTerm443Packet, eTerm443Packet, eTerm443Async> e)
                    {
                        packetASync(e.Session);
                        TASyncLog(e.Session.userName, string.Empty, @"OnAsyncReadPacket", @"SUCCESS");
                        if (e.Session.TSession == null||!(LicenceManager.Instance.LicenceBody.AllowDbLog??false)) return;
                        if (!(AsyncStackNet.Instance.ASyncSetup.AllowLog ?? false)) return;
                        SQLiteExecute.Instance.BeginExecute(e.Session.userName, e.Session.TSession.userName, (e.Session.TSession.AsyncSocket.RemoteEndPoint as IPEndPoint).Address.ToString(),e.Session.UnInPakcet( e.OutPacket),e.Session.UnOutPakcet( e.InPacket));
                    }
                );
            AsyncStackNet.Instance.OnAsyncValidated += new EventHandler<AsyncEventArgs<eTerm443Packet, eTerm443Async>>(
                    delegate(object sender, AsyncEventArgs<eTerm443Packet, eTerm443Async> e)
                    {
                        TASyncLog(e.Session.userName, string.Empty, @"OnAsyncValidated", @"SUCCESS");
                        /*
                        e.Session.OnReadPacket += new EventHandler<AsyncEventArgs<eTerm443Packet, eTerm443Packet, eTerm443Async>>(
                                delegate(object Session, AsyncEventArgs<eTerm443Packet, eTerm443Packet, eTerm443Async> SessionArg)
                                {

                                }
                            );
                        */
                    }
                );
            AsyncStackNet.Instance.OnTSessionValidated += new EventHandler<AsyncEventArgs<eTerm363Session>>(
                    delegate(object sender, AsyncEventArgs<eTerm363Session> e) {
                        updateTSessionRead(e.Session);
                        TSessionLog(string.IsNullOrEmpty(e.Session.userName) ? e.Session.SessionId.ToString() : e.Session.userName,
                            string.Empty,
                            @"OnTSessionValidated", @"SUCCESS");
                    }
                );

            AsyncStackNet.Instance.OnTSessionAssign += new EventHandler<AsyncEventArgs<eTerm363Session>>(
                    delegate(object sender, AsyncEventArgs<eTerm363Session> e)
                    {
                        TSessionLog(string.IsNullOrEmpty(e.Session.userName) ? e.Session.SessionId.ToString() : e.Session.userName,
                            string.Format(@"【{0}】",e.Session.Async443.userName),
                            @"OnTSessionAssign", @"SUCCESS");
                        //TASyncLog(e.Session.userName, string.Format(@"",e.Session., @"OnAsyncValidated", @"SUCCESS");
                        updateTSessionRead(e.Session);
                    }
                );
            AsyncStackNet.Instance.OnTSessionAccept += new EventHandler<AsyncEventArgs<eTerm363Session>>(
                    delegate(object sender, AsyncEventArgs<eTerm363Session> e)
                    {
                        TSessionLog(string.IsNullOrEmpty(e.Session.userName) ? e.Session.SessionId.ToString() : e.Session.userName,
                        string.Empty,
                        @"OnTSessionAccept", @"SUCCESS");
                        AcceptTSession(e.Session);
                    }
                );
            AsyncStackNet.Instance.OnTSessionClosed += new EventHandler<AsyncEventArgs<eTerm363Session>>(
                    delegate(object sender, AsyncEventArgs<eTerm363Session> e)
                    {
                        TSessionLog(string.IsNullOrEmpty(e.Session.userName) ? e.Session.SessionId.ToString() : e.Session.userName,
string.Empty,
@"OnTSessionClosed", @"SUCCESS");
                        DisconnectTSession(e.Session);
                    }
                );
            AsyncStackNet.Instance.OnAsyncTimeout += new EventHandler<AsyncEventArgs<eTerm443Async>>(
                    delegate(object sender, AsyncEventArgs<eTerm443Async> e)
                    {
                        TASyncLog(e.Session.userName, string.Empty, @"OnAsyncTimeout", @"SUCCESS");
                    }
                );
            AsyncStackNet.Instance.OnTSessionRelease += new EventHandler<AsyncEventArgs<eTerm363Session>>(
                    delegate(object sender, AsyncEventArgs<eTerm363Session> e)
                    {
                        updateTSessionSent(e.Session);
                        TSessionLog(string.IsNullOrEmpty(e.Session.userName) ? e.Session.SessionId.ToString() : e.Session.userName,
                           string.Format(@"【{0}】", e.Session.Async443 != null?e.Session.Async443.userName:string.Empty),
@"OnTSessionRelease", @"SUCCESS");
                        if (e.Session.Async443 != null)
                            updateASync(e.Session.Async443);
                        if (e.Session.Async443 != null && !e.Session.IsCompleted)
                            e.Session.SendPacket(__eTerm443Packet.BuildSessionPacket(e.Session.SID, e.Session.RID, "无数据返回或读取超时"));
                    }
                );

            AsyncStackNet.Instance.OnTSessionReadPacket += new EventHandler<AsyncEventArgs<eTerm363Packet, eTerm363Packet, eTerm363Session>>(
                    delegate(object sender, AsyncEventArgs<eTerm363Packet, eTerm363Packet, eTerm363Session> e)
                    {
                        TSessionLog(string.IsNullOrEmpty(e.Session.userName) ? e.Session.SessionId.ToString() : e.Session.userName,
Encoding.GetEncoding(@"gb2312").GetString(e.Session.UnOutPakcet(e.InPacket)),
@"OnTSessionReadPacket", @"SUCCESS");
                        updateTSessionRead(e.Session);
                    }
                );


            LicenceManager.Instance.BeginValidate(new AsyncCallback(
                delegate(IAsyncResult iar)
                {
                    try {
                        if (!LicenceManager.Instance.EndValidate(iar)) {
                            UpdateStatusText(statusServer, string.Format(@"不合法的“{0}”机器码，机器授权验证失败，请速联系开发商！",LicenceManager.Instance.SerialNumber));
                        }
                        else {
                            //激活配置
                            AsyncStackNet.Instance.BeginAsync();
                            //UpdateStatusText(statusServer, string.Format(@"中心服务器为{{{0}:{1}}}", AsyncStackNet.Instance.ASyncSetup.CoreServer, AsyncStackNet.Instance.ASyncSetup.CoreServerPort));
                            if ((AsyncStackNet.Instance.ASyncSetup.AllowPlugIn ?? false)) {
                                AsyncStackNet.Instance.BeginReflectorPlugIn(new AsyncCallback(delegate(IAsyncResult iar1)
                                {
                                    AsyncStackNet.Instance.EndReflectorPlugIn(iar1);
                                }));
                            }
                        }
                    }
                    catch (Exception ex) {
                        MessageBox.Show(string.Format("{0}\r\n{1}", ex.Message,ex.StackTrace), "错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }), new FileInfo(@"Key.Bin").FullName);

            if(Directory.Exists(AsyncStackNet.Instance.ASyncSetup.PlugInPath))
                loadAddIn(AsyncStackNet.Instance.ASyncSetup.PlugInPath);
            
            //按钮控制
            this.btnStart.Enabled = false;
            this.btnStop.Enabled = true;
            this.btnReset.Enabled = true;

            btnStartService.Enabled = false;
            btnStopService.Enabled = true;
            btnResetService.Enabled = true;
        }
        #endregion

        #region 按钮事件
        /// <summary>
        /// Handles the Click event of the btnStart control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnStart_Click(object sender, EventArgs e) {
            StartService();
        }

        /// <summary>
        /// Handles the Click event of the btnStop control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnStop_Click(object sender, EventArgs e) {
            StopService();
        }

        /// <summary>
        /// Handles the Click event of the btnSendMessage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnSendMessage_Click(object sender, EventArgs e) {
            List<eTerm363Session> sessionLst = new List<eTerm363Session>();
            foreach (ListViewItem item in this.lstSession.SelectedItems) {
                sessionLst.Add(item.Tag as eTerm363Session);
            }
            new frmSender(sessionLst).ShowDialog();
        }


        /// <summary>
        /// Handles the Click event of the btnReset control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnReset_Click(object sender, EventArgs e) {
            StopService();

            StartService();
        }
        #endregion

        #region UI更新
        /// <summary>
        /// Handles the SelectedIndexChanged event of the lstAsync control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void lstAsync_SelectedIndexChanged(object sender, EventArgs e) {
            btnDispose.Enabled = lstAsync.SelectedItems.Count > 0;
            btnRelease.Enabled = lstAsync.SelectedItems.Count > 0;
            btnManualConnect.Enabled=(
                    this.lstAsync.SelectedItems.Count==1
                    &&
                    this.lstAsync.SelectedItems[0].Group==this.group2
                );
        }

        /// <summary>
        /// Handles the Click event of the btnDispose control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnDispose_Click(object sender, EventArgs e) {
            foreach (ListViewItem item in this.lstAsync.SelectedItems) {
                eTerm443Async Async = item.Tag as eTerm443Async;
                Async.Close();
            }
        }

        /// <summary>
        /// Handles the Click event of the btnRelease control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnRelease_Click(object sender, EventArgs e) {
            foreach (ListViewItem item in this.lstAsync.SelectedItems) {
                eTerm443Async Async = item.Tag as eTerm443Async;
                Async.TSession = null;
            }
        }

        /// <summary>
        /// Handles the Click event of the btnDisconnectSession control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnDisconnectSession_Click(object sender, EventArgs e) {
            foreach (ListViewItem item in this.lstSession.SelectedItems) {
                eTerm363Session Session = item.Tag as eTerm363Session;
                Session.Close();
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the lstSession control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void lstSession_SelectedIndexChanged(object sender, EventArgs e) {
            btnSendMessage.Enabled = lstSession.SelectedItems.Count > 0;
            btnDisconnectSession.Enabled = lstSession.SelectedItems.Count > 0;
        }
        #endregion

        #region 加载UI插件

        /// <summary>
        /// 查询插件.
        /// </summary>
        private void loadAddIn(string PlugInPath) {
            foreach (FileInfo file in new DirectoryInfo(PlugInPath).GetFiles(@"*.AddIn", SearchOption.TopDirectoryOnly)) {
                LoadPlugIn(file);
            }
        }

        /// <summary>
        /// Loads the plug in.
        /// </summary>
        /// <param name="File">The file.</param>
        private void LoadPlugIn(FileInfo File) {
            Assembly ass;
            try {
                ass = Assembly.LoadFrom(File.FullName);
                foreach (Type t in ass.GetTypes()) {
                    foreach (Type i in t.GetInterfaces()) {
                        if (i.FullName == typeof(IAddIn).FullName) {
                            IAddIn plugIn = (IAddIn)System.Activator.CreateInstance(t);
                            BuildButton(plugIn);
                        }
                    }
                }
            }
            catch { throw; }
        }


        /// <summary>
        /// Builds the button.
        /// </summary>
        /// <param name="PlugIn">The plug in.</param>
        private void BuildButton(IAddIn PlugIn) {
            ButtonItem PlugInButton = new ButtonItem();

            PlugInButton.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            PlugInButton.ColorTable = DevComponents.DotNetBar.eButtonColor.Office2007WithBackground;
            if (!string.IsNullOrEmpty(PlugIn.ImageIcon))
                PlugInButton.Image = new Bitmap(new FileStream(PlugIn.ImageIcon, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            PlugInButton.Shortcuts.Add(DevComponents.DotNetBar.eShortcut.F5);
            PlugInButton.Size = new System.Drawing.Size(158, 23);
            PlugInButton.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            PlugInButton.Text = PlugIn.ButtonName;
            PlugInButton.Tag = PlugIn;
            PlugInButton.Click += new EventHandler(
                    delegate(object sender, EventArgs e)
                    {
                        
                        dockAddIn.Visible = true;
                        dockAddIn.Selected = true;
                        dockAddIn.Control.Controls.Clear();
                        if (!string.IsNullOrEmpty(((sender as ButtonItem).Tag as BaseAddIn).ImageIcon))
                            dockAddIn.Image = (sender as ButtonItem).Image;
                        dockAddIn.Text = (sender as ButtonItem).Text;
                        ((sender as ButtonItem).Tag as BaseAddIn).ASyncSetup = LicenceManager.Instance.LicenceBody;
                        dockAddIn.Control.Controls.Add((sender as ButtonItem).Tag as BaseAddIn);
                    }
                );
            PlugInButton.MouseHover += new EventHandler(
                    delegate(object sender, EventArgs e) {
                        PlugInBar.Text = ((sender as ButtonItem).Tag as BaseAddIn).ButtonName;
                    }
                );
            PlugInButton.ButtonStyle = eButtonStyle.ImageAndText;
            PlugInButton.Name = string.Format(@"btn_{0}", PlugIn.ButtonName);
            AppendAddInButton(PlugInButton);
        }

        /// <summary>
        /// Appends the add in button.
        /// </summary>
        /// <param name="PlugInButton">The plug in button.</param>
        private void AppendAddInButton(ButtonItem PlugInButton) {
            if (this.InvokeRequired) {
                this.BeginInvoke(new AppendAddInButtonDelegate(AppendAddInButton), PlugInButton);
                return;
            }
            this.PlugInBar.Items.AddRange(new DevComponents.DotNetBar.BaseItem[] {
            PlugInButton});
        }


        /// <summary>
        /// Handles the Click event of the btnManualConnect control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnManualConnect_Click(object sender, EventArgs e) {
            (this.lstAsync.SelectedItems[0].Tag as eTerm443Async).Connect();
        }



        /// <summary>
        /// 线程UI代理
        /// </summary>
        private delegate void AppendAddInButtonDelegate(ButtonItem PlugInButton);
        #endregion

        /// <summary>
        /// Handles the Click event of the btnRestore control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnRestore_Click(object sender, EventArgs e)
        {
            this.Show();
            this.ShowInTaskbar = true;
            notifyIcon1.Visible = false;
        }

        /// <summary>
        /// Handles the Click event of the btnClose control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// Handles the DoubleClick event of the notifyIcon1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            btnRestore_Click(sender, e);
        }

    }
}
