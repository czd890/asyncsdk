﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.Common;
using eTerm.AsyncSDK;

namespace ASyncSDK.Office
{
    /// <summary>
    /// 日志执行器
    /// </summary>
    public sealed class SQLiteExecute
    {
        #region 变量定义
        private string __dbString = string.Empty;
        private static readonly SQLiteExecute __instance = new SQLiteExecute();
        private SQLiteDatabase __sqliteDb;
        private string __CurrentTable = string.Empty;
        private InvokeSQLiteDbCommand __Execute;
        #endregion

        /// <summary>
        /// 执行代理
        /// </summary>
        public delegate void InvokeSQLiteDbCommand(string TSession, string TASync, string TSessionIp, byte[] TInPacket, byte[] TOutPacket);


        #region 构造函数
        /// <summary>
        /// Initializes a new instance of the <see cref="SQLiteExecute"/> class.
        /// </summary>
        private SQLiteExecute() {
            __dbString = new FileInfo(@"SQLiteDb.s3db").FullName;
            __sqliteDb = new SQLiteDatabase(__dbString);
            BuildLogTable(DateTime.Now);
            __Execute = new InvokeSQLiteDbCommand(ExecuteLog);
        }
        #endregion

        #region 写日志
        /// <summary>
        /// 开始线程.
        /// </summary>
        /// <param name="TSession">The T session.</param>
        /// <param name="TSessionIp">The T session ip.</param>
        /// <param name="TData">The T data.</param>
        /// <param name="TLogType">Type of the T log.</param>
        /// <returns></returns>
        public IAsyncResult BeginExecute(string TSession, string TASync, string TSessionIp, byte[] TInPacket, byte[] TOutPacket)
        {
            try
            {
                return __Execute.BeginInvoke(TSession,TASync, TSessionIp, TInPacket,TOutPacket, new AsyncCallback(delegate(IAsyncResult iar)
                                    {
                                        EndExecute(iar);
                                    }), null);
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Ends the execute.
        /// </summary>
        /// <param name="iar">The iar.</param>
        private void EndExecute(IAsyncResult iar) {
            if (iar == null) return;
            try
            {
                __Execute.EndInvoke(iar);
                iar.AsyncWaitHandle.Close();
            }
            catch
            {
                // Hide inside method invoking stack 
                //throw e;
            }
        }



        /// <summary>
        /// Executes the log.
        /// </summary>
        /// <param name="TSession">The T session.</param>
        /// <param name="TSessionIp">The T session ip.</param>
        /// <param name="TData">The T data.</param>
        /// <param name="TLogType">Type of the T log.</param>
        private void ExecuteLog(string TSession, string TASync, string TSessionIp, byte[] TInPacket, byte[] TOutPacket)
        {
            if (!(AsyncStackNet.Instance.ASyncSetup.AllowLog??false)) return;
            DbCommand sqliteCommand = __sqliteDb.GetSqlStringCommand(string.Format(@"
    INSERT INTO {0}([TSession],[TASync],[TargetIp],[TInPacket],[TOutPacket],[TLogDate],[IsProcess]) 
                    VALUES(?,?,?,?,?,?,?)
", this.__CurrentTable));
            __sqliteDb.AddInParameter(sqliteCommand, System.Data.DbType.String, TSession);
            __sqliteDb.AddInParameter(sqliteCommand, System.Data.DbType.String, TASync);
            __sqliteDb.AddInParameter(sqliteCommand, System.Data.DbType.String, TSessionIp);
            __sqliteDb.AddInParameter(sqliteCommand, System.Data.DbType.String,Encoding.GetEncoding("gb2312").GetString( TInPacket));
            __sqliteDb.AddInParameter(sqliteCommand, System.Data.DbType.String, Encoding.GetEncoding("gb2312").GetString( TOutPacket));
            __sqliteDb.AddInParameter(sqliteCommand, System.Data.DbType.DateTime, DateTime.Now);
            __sqliteDb.AddInParameter(sqliteCommand, System.Data.DbType.Boolean, false);
            sqliteCommand.ExecuteNonQuery();
        }
        #endregion

        #region 以月份为单位储存日志
        /// <summary>
        /// Exists the log table.
        /// </summary>
        /// <param name="Current">The current.</param>
        /// <returns></returns>
        private bool ExistLogTable(DateTime Current) {
            return ((long)__sqliteDb.GetSqlStringCommand(string.Format(@"SELECT COUNT(*) FROM sqlite_master where type='table' and name='SQLiteLog{0}';", Current.ToString(@"yyyyMM"))).ExecuteScalar())>0;
        }

        /// <summary>
        /// Builds the log table.
        /// </summary>
        /// <param name="Current">The current.</param>
        /// <returns></returns>
        private bool BuildLogTable(DateTime Current) {
            __CurrentTable = string.Format(@"SQLiteLog{0}", Current.ToString(@"yyyyMM"));
            if (ExistLogTable(Current)) return true;
            __sqliteDb.GetSqlStringCommand( string.Format(@"
CREATE TABLE [SQLiteLog{0}] (
    [TLogId] INTEGER  NOT NULL PRIMARY KEY AUTOINCREMENT,
    [TSession] NVARCHAR(50)  NOT NULL,
    [TASync]    NVARCHAR(50)  NOT NULL,
    [TargetIp] NVARCHAR(50)  NOT NULL,
    [TInPacket]  NVARCHAR(255)   NULL,
    [TOutPacket]  NVARCHAR(2048)   NULL,
    [TLogDate] DATETIME  NOT NULL,
    [IsProcess] BOOLEAN NULL
);

CREATE INDEX [IDX_SQLiteLog{0}_] ON [SQLiteLog{0}](
    [TSession]  ASC,
    [TASync]  ASC,
    [TLogDate]  ASC
);
", Current.ToString(@"yyyyMM"))).ExecuteNonQuery() ;
            return false;
        }
        #endregion

        #region 单例对象
        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static SQLiteExecute Instance { get { return __instance; } }
        #endregion
    }
}
