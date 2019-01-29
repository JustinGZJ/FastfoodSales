using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using Dapper;
using System.Data;
using System.Threading.Tasks;
using System.Threading;
namespace DAQ.Service
{
    public class DataAccess : IDisposable
    {
        public static readonly object loker=new object();
        public DataAccess()
        {
            Monitor.Enter(loker);
            GetDbConnection();
            CreateTable();
        }
        public string DbFile
        {
            get { return AppDomain.CurrentDomain.BaseDirectory + "\\DAQ.db"; }
        }
        IDbConnection conn;
        void GetDbConnection()
        {
            conn = new SQLiteConnection("Data Source=" + DbFile);
            conn.Open();
        }
        void CreateTable()
        {
            string tb = @"CREATE TABLE IF NOT EXISTS TEST_SPECS (
                    ID       INTEGER  PRIMARY KEY AUTOINCREMENT,
                    NAME     TEXT,
                    LOWER    REAL,
                    UPPER    REAL,
                    VALUE    REAL,
                    RESULT   REAL,
                    SOURCE   TEXT,
                    T_INSERT DATETIME DEFAULT ( (datetime('now', 'localtime') ) ) 
                                      NOT NULL);";
            conn.Execute(tb);
        }
        public async void SaveTestSpecs(IEnumerable<TestSpecViewModel> testSpecs)
        {
            IDbTransaction transaction = conn.BeginTransaction();
            string sql = "INSERT INTO TEST_SPECS (NAME,LOWER,UPPER,VALUE,RESULT,SOURCE) VALUES(@NAME,@LOWER,@UPPER,@VALUE,@RESULT,@SOURCE)";
            try
            {
                foreach (var s in testSpecs)
                {
                    await conn.ExecuteAsync(sql, testSpecs, transaction);
                }
                //提交事务
                transaction.Commit();
            }
            catch (Exception ex)
            {
                //出现异常，事务Rollback
                transaction.Rollback();
                throw new Exception(ex.Message);
            }
        }
        public IEnumerable<TestSpecViewModel> GetTestSpecs(DateTime from, DateTime to,string Source="")
        {
            IEnumerable<TestSpecViewModel> testSpecs;
            if (Source == "")
            {
                string sql = "SELECT * FROM TEST_SPECS WHERE T_INSERT BETWEEN @FROM AND @TO";

                 testSpecs = conn.Query<TestSpecViewModel>(sql,
                    new { FROM = from, TO = to });
            }
            else
            {
                string sql = "SELECT * FROM TEST_SPECS WHERE T_INSERT BETWEEN @FROM AND @TO WHERE SOURCE=@SOURCE";

                 testSpecs =  conn.Query<TestSpecViewModel>(sql,
                    new { FROM = from, TO = to ,SOURCE=Source});
            }
            return testSpecs;
        }
        public void Dispose()
        {
            Monitor.Exit(loker);
            if (conn != null)
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
                conn.Dispose();
            }
        }
    }

}