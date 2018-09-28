using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Data.Common;
using System.Linq;

namespace Com.Alipay
{
    public delegate void HandleDataReader(DbDataReader reader);
    /// <summary>
    /// Summary description for DataAccess
    /// </summary>
    public static class DataAccess
    {
        public static void ExecuteReader(string sql, HandleDataReader dataReaderHandler, List<SqlParameter> parameters = null)
        {
            using (var conn = Get_connection())
            {
                var cmd = new SqlCommand(sql, conn);

                if (parameters != null && parameters.Count > 0)
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                }

                conn.Open();
                dataReaderHandler(cmd.ExecuteReader());
            }
        }

        public static DataTable ExcuteDataTableReader(string sql)
        {
            var dt = new DataTable();
            DataAccess.ExecuteReader(sql, reader =>
            {
                dt.Load(reader);
            });

            return dt;
        }

        public static DataSet ExecuteStoredProcedure(string procedureName, List<SqlParameter> parameters = null)
        {
            using (var conn = Get_connection())
            {
                var cmd = new SqlCommand
                {
                    CommandText = procedureName,
                    CommandType = CommandType.StoredProcedure,
                    Connection = conn
                };

                if (parameters != null)
                    foreach (var param in parameters)
                        cmd.Parameters.Add(param);

                var adapter = new SqlDataAdapter(cmd);
                var ds = new DataSet();
                conn.Open();
                adapter.Fill(ds);
                return ds;
            }
        }

        public static int ExecuteStoredProcedureNonQuery(string procedureName, List<SqlParameter> parameters = null)
        {
            using (var conn = Get_connection())
            {
                var cmd = new SqlCommand
                {
                    CommandText = procedureName,
                    CommandType = CommandType.StoredProcedure,
                    Connection = conn
                };

                if (parameters != null)
                    foreach (var param in parameters)
                        cmd.Parameters.Add(param);
                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();
                int val = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
                return val;
            }
        }

        public static T ExecuteScalar<T>(string sql, List<SqlParameter> parameters = null)
        {
            using (var conn = Get_connection())
            {
                var cmd = new SqlCommand(sql, conn);

                if (parameters != null && parameters.Count > 0)
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                }

                conn.Open();
                var result = cmd.ExecuteScalar();

                if (result is DBNull)
                {
                    return default(T);
                }
                return (T)result;
            }
        }

        public static int ExecuteNonQuery(string sql, List<SqlParameter> parameters = null)
        {
            using (var conn = Get_connection())
            {
                var cmd = new SqlCommand(sql, conn);

                if (parameters != null && parameters.Count > 0)
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                }

                conn.Open();

                return cmd.ExecuteNonQuery();
            }
        }

        public static bool ExecuteTransaction(List<string> sqls)
        {
            bool res = false;

            using (var sqlconn = Get_connection())
            {
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = sqlconn;
                SqlTransaction sqltran;
                sqlconn.Open();
                sqltran = sqlconn.BeginTransaction();
                cmd.Transaction = sqltran;

                try
                {
                    var results = new List<int>();
                    foreach (var sql in sqls)
                    {
                        cmd.CommandText = sql;
                        var result = cmd.ExecuteNonQuery();
                        results.Add(result);

                        if (result <= 0)
                            Logger.Log(sql);
                    }
                    if (results.All(result => result > 0))
                    {
                        res = true;
                    }

                    if (!res)
                        sqltran.Rollback();
                    else
                        sqltran.Commit();
                }
                catch (Exception)
                {
                    sqltran.Rollback();
                }
                finally
                {
                    cmd.Dispose();
                    sqlconn.Close();
                }
            }

            return res;
        }

        private static SqlConnection Get_connection()
        {
            return new SqlConnection(ConfigurationManager.AppSettings["conn"]);
        }
    }
}
