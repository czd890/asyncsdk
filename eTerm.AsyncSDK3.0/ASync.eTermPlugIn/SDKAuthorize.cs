﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using eTerm.AsyncSDK.Base;
using eTerm.AsyncSDK;
using eTerm.AsyncSDK.Net;
using System.Text.RegularExpressions;

namespace ASync.eTermPlugIn {
    [AfterASynCommand("!help", IsSystem = true)]
    public sealed class SDKAuthorize : BaseASyncPlugIn {
        /// <summary>
        /// 开始线程.
        /// </summary>
        /// <param name="SESSION">会话端.</param>
        /// <param name="InPacket">入口数据包.</param>
        /// <param name="OutPacket">出口数据包.</param>
        /// <param name="Key">The key.</param>
        protected override void ExecutePlugIn(eTerm.AsyncSDK.Core.eTerm363Session SESSION, eTerm.AsyncSDK.Core.eTerm363Packet InPacket, eTerm.AsyncSDK.Core.eTerm363Packet OutPacket, eTerm.AsyncSDK.AsyncLicenceKey Key) {
            string Cmd = Encoding.GetEncoding("gb2312").GetString(SESSION.UnOutPakcet(InPacket)).Trim();
            string Message = string.Empty;
            if (Regex.IsMatch(Cmd, @"(\d{1,3}\.(\d{1,3}\.\d{1,3}\.\d{1,3})\:(\d{1,})\s+([A-Z0-9]{1,})\s+([A-Z0-9]{1,})", RegexOptions.IgnoreCase | RegexOptions.Multiline)) {
                Message = @"成功修改";
                Match m = Regex.Match(Cmd, @"(\d{1,3}\.(\d{1,3}\.\d{1,3}\.\d{1,3})\:(\d{1,})\s+([A-Z0-9]{1,})\s+([A-Z0-9]{1,})", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                AsyncStackNet.Instance.ASyncSetup.CoreServer = m.Groups[1].Value;
                AsyncStackNet.Instance.ASyncSetup.CoreServerPort =int.Parse( m.Groups[2].Value);
                AsyncStackNet.Instance.ASyncSetup.CoreAccount = m.Groups[3].Value;
                AsyncStackNet.Instance.ASyncSetup.CorePass = m.Groups[4].Value;
                AsyncStackNet.Instance.BeginRateUpdate(new AsyncCallback(delegate(IAsyncResult iar)
                {
                    AsyncStackNet.Instance.EndRateUpdate(iar);
                }));
            }
            else {
                Message = @"修改失败";
            }
            SESSION.SendPacket(__eTerm443Packet.BuildSessionPacket(SESSION.SID, SESSION.RID, Message));
        }

        /// <summary>
        /// 验证可用性.
        /// </summary>
        /// <param name="SESSION">The SESSION.</param>
        /// <param name="InPacket">The in packet.</param>
        /// <param name="OutPacket">The out packet.</param>
        /// <param name="Key">The key.</param>
        /// <returns></returns>
        protected override bool ValidatePlugIn(eTerm.AsyncSDK.Core.eTerm363Session SESSION, eTerm.AsyncSDK.Core.eTerm363Packet InPacket, eTerm.AsyncSDK.Core.eTerm363Packet OutPacket, eTerm.AsyncSDK.AsyncLicenceKey Key) {
            return SESSION != null;
        }

        /// <summary>
        /// 指令说明.
        /// </summary>
        /// <value>The description.</value>
        public override string Description {
            get {
                return "授权服务器";
            }
        }
    }
}
