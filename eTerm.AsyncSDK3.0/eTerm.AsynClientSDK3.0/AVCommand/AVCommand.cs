﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using eTerm.ASynClientSDK.Base;
using eTerm.ASynClientSDK;
using System.Text.RegularExpressions;
using eTerm.ASynClientSDK.AVException;

namespace eTerm.ASynClientSDK {
    /// <summary>
    /// AV指令对象
    /// <remarks>
    /// 提供简单有效的航班信息实时查询通道 查询指定日期及航线上的可用航班信息支持多种查询参数 结果数据以类AvResult的形式表示
    /// </remarks>
    /// <code>
    /// AVCommand Av = new AVCommand();
    /// SyncResult r = Av.getAvailability("SHA", "CTU", DateTime.Now.AddMonths(1),string.Empty,true);
    /// </code>
    /// <example>
    /// AVCommand Av = new AVCommand();
    /// SyncResult r = Av.getAvailability("SHA", "CTU", DateTime.Now.AddMonths(1),string.Empty,true);
    /// </example>
    /// </summary>
    public sealed class AVCommand : ASyncPNCommand {

        #region 变量定义
        private string queryDate = string.Empty;
        private string __AvCommand = string.Empty;
        private DateTime? orgDate;
        AVResult avResult = new AVResult();
        private string orgCity = string.Empty;
        private string destCity = string.Empty;
        #endregion


        #region 重写
        /// <summary>
        /// 是否还有下页数据（将自动执行“PnCommand”）.
        /// </summary>
        /// <param name="msgBody">当前指令结果.</param>
        /// <returns></returns>
        /// <value><c>true</c> if [exist next page]; otherwise, <c>false</c>.</value>
        protected override bool ExistNextPage(string msgBody) {
            return Regex.IsMatch(msgBody, @"7\+");
        }

        /// <summary>
        /// 异常抛出.
        /// </summary>
        /// <param name="Msg"></param>
        protected override void ThrowExeption(string Msg) {
            if (Regex.IsMatch(Msg, @"\*NO\sROUTING\*", RegexOptions.IgnoreCase | RegexOptions.Multiline))
                throw new AVNoRoutingException();
            if (Regex.IsMatch(Msg, @"CITY\sPAIR", RegexOptions.IgnoreCase | RegexOptions.Multiline))
                throw new AVCityPairException();
            if (Regex.IsMatch(Msg, @"\*CITY\*", RegexOptions.IgnoreCase | RegexOptions.Multiline))
                throw new AVCityPairException();
        }

        /// <summary>
        /// 生成指令并发送分析(子类必须重写).
        /// </summary>
        /// <param name="SynCmd">eTerm实质指令.</param>
        /// <returns></returns>
        protected override ASyncResult GetSyncResult(string SynCmd) {
            __AvCommand = SynCmd;
            return base.GetSyncResult(SynCmd);
        }

        /// <summary>
        /// 指令结果解析适配器.
        /// </summary>
        /// <param name="Msg">指令结果集合.</param>
        /// <returns></returns>
        protected override ASyncResult ResultAdapter(string Msg) {
            AVResult result = new AVResult() { getDate=this.orgDate, getDst=this.destCity, getOrg=orgCity,  AvSegment=new List<AvItem>()};
            foreach (Flight seg in new AnalysisAVH().ParseAVH(this.__AvCommand, Msg).Flights) {
                AvItem AvSegment = new AvItem() { 
                 getAirline=seg.FlightNO,
                  getArritime=seg.ArrivalTime.Insert(2,":"),
                   getCarrier=seg.Airline,
                 getDeptime = seg.DepartureTime.Insert(2, ":"),
                     getDstcity=seg.ArrivalAirport,
                      getMeal=seg.Meal.Trim().Length>0,
                       getPlanestyle=seg.AircraftType,
                        getOrgcity=seg.DepartureAirport,
                         getStopnumber=int.Parse( seg.Stop),
                          isCodeShare=seg.CodeShare,
                           getLink=seg.Connect,
                            getDepdate=queryDate,
                            getArridate=queryDate,
                            getCabins=new List<AvItemCabinChar>(),
                };
                foreach (FlightCarbin carbin in seg.Carbins.Where<FlightCarbin>(CARBIN=>@"123456789A".IndexOf(CARBIN.Number)>=0)) {
                    AvSegment.getCabins.Add(new AvItemCabinChar() {
                         getCode=carbin.Carbin,
                          getAvalibly=carbin.Number,
                    });
                }
                result.AvSegment.Add(AvSegment);
            }
            return result;
        }
        #endregion

        #region 查询方法
        /// <summary>
        ///  查询指定日期和城市间的实时航班信息,包含转飞航班.
        /// </summary>
        /// <param name="org">起飞城市三字代码.</param>
        /// <param name="dst">到达城市三字代码.</param>
        /// <param name="date">查询日期.</param>
        /// <param name="airline">航空公司.</param>
        /// <returns></returns>
        public ASyncResult getAvailability(string org, string dst, DateTime date, string airline) {
            avResult.getDate = date;
            avResult.getOrg = org;
            avResult.getDst = dst;
            return this.getAvailability(org, dst, string.Format(@"{0}{1}", date.Day.ToString("D2"), getDateString(date)), airline, true, true);
        }

        /// <summary>
        /// 查询指定日期和城市间的实时航班信息,包含转飞航班.
        /// </summary>
        /// <param name="org">起飞城市三字代码.</param>
        /// <param name="dst">到达城市三字代码.</param>
        /// <param name="date">查询日期.</param>
        /// <returns></returns>
        public ASyncResult getAvailability(string org, string dst, DateTime date) {
            avResult.getDate = date;
            avResult.getOrg = org;
            avResult.getDst = dst;
            orgDate = date;
            return this.getAvailability(org, dst, string.Format(@"{0}{1}", date.Day.ToString("D2"), getDateString(date)), string.Empty, true, true);
        }

        /// <summary>
        /// 查询指定日期和城市间的实时航班信息.
        /// </summary>
        /// <param name="org">起飞城市三字代码.</param>
        /// <param name="dst">到达城市三字代码.</param>
        /// <param name="date">查询日期.</param>
        /// <param name="airline">航空公司.</param>
        /// <param name="direct">是否只查询直达航班.</param>
        /// <returns></returns>
        public ASyncResult getAvailability(string org, string dst, DateTime date, string airline, bool direct) {
            avResult.getDate = date;
            avResult.getOrg = org;
            avResult.getDst = dst;
            return this.getAvailability(org, dst, string.Format(@"{0}{1}", date.Day.ToString("D2"), getDateString(date)), airline, direct, true);
        }

        /// <summary>
        ///   查询指定日期和城市间的实时航班信息.
        /// </summary>
        /// <param name="org">起飞城市三字代码.</param>
        /// <param name="dst">到达城市三字代码.</param>
        /// <param name="date">查询日期.</param>
        /// <param name="airline">航空公司.</param>
        /// <param name="direct">是否只查询直达航班.</param>
        /// <param name="e_ticket">是否只查询支持电子客户票航班.</param>
        /// <returns></returns>
        public ASyncResult getAvailability(string org, string dst, DateTime date, string airline, bool direct, bool e_ticket) {
            avResult.getDate = date;
            avResult.getOrg = org;
            avResult.getDst = dst;
            return this.getAvailability(org, dst, string.Format(@"{0}{1}", date.Day.ToString("D2"), getDateString(date)), airline, direct, e_ticket);
        }


        /// <summary>
        /// 查询指定航班号和日期的航班信息.
        /// </summary>
        /// <param name="airline">航空公司.</param>
        /// <param name="date">查询日期.</param>
        /// <returns></returns>
        //public SyncResult getAvailability(string airline, string date) {
        //    return this.getAvailability(string.Empty,string.Empty, date, airline, false, true);
        //}

        /// <summary>
        ///  查询指定日期和城市间的实时航班信息,包含转飞航班.
        /// </summary>
        /// <param name="org">起飞城市三字代码.</param>
        /// <param name="dst">到达城市三字代码.</param>
        /// <param name="date">查询日期(10JUL).</param>
        /// <param name="airline">航空公司.</param>
        /// <returns></returns>
        public ASyncResult getAvailability(string org, string dst, string date, string airline) {
            return this.getAvailability(org, dst, date, airline, true, true);
        }

        /// <summary>
        /// 查询指定日期和城市间的实时航班信息.
        /// </summary>
        /// <param name="org">起飞城市三字代码.</param>
        /// <param name="dst">到达城市三字代码.</param>
        /// <param name="date">查询日期(10JUL).</param>
        /// <param name="airline">航空公司.</param>
        /// <param name="direct">是否只查询直达航班.</param>
        /// <returns></returns>
        public ASyncResult getAvailability(string org, string dst, string date, string airline, bool direct) {
            return this.getAvailability(org, dst, date, airline, direct, true);
        }

        /// <summary>
        ///   查询指定日期和城市间的实时航班信息.
        /// </summary>
        /// <param name="org">起飞城市三字代码.</param>
        /// <param name="dst">到达城市三字代码.</param>
        /// <param name="date">查询日期(10JUL).</param>
        /// <param name="airline">航空公司.</param>
        /// <param name="direct">是否只查询直达航班.</param>
        /// <param name="e_ticket">是否只查询支持电子客户票航班.</param>
        /// <returns></returns>
        public ASyncResult getAvailability(string org, string dst, string date, string airline, bool direct, bool e_ticket) {
            string avCommand = string.Format(@"AV:H/{0}{1}/{2}{3}{4}{5}"
                , org
                , dst
                , date
                , string.IsNullOrEmpty(airline) ? string.Empty : "/" + airline
                , direct ? "/D" : string.Empty
                , e_ticket ? "" : ""
                );
            this.orgCity = org;
            this.destCity = dst;
            this.queryDate = Regex.Match(avCommand, @"\d{2,2}[A-Z]{3,3}", RegexOptions.Singleline).Value;
            return base.GetSyncResult(avCommand);
        }
        #endregion
    }
}
