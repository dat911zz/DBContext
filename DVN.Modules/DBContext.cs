///Author: Vu Dat
///Discription: This module using for connect and execute query into database 
///but it's safer by using parameters for prevent SQL Injection
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DVN.Modules
{
    public class DBContext
    {
        #region Create Instance (Singleton pattern)
        private DBContext() { }
        private static DBContext instance;
        public static DBContext Instance
        {
            get => instance ?? new DBContext();
            private set => instance = value;
        }
        #endregion
        #region Get Connection String
        public string GetConnectionString()
        {
            return @"Data Source=DESKTOP-GUE0JS7;Initial Catalog=QLSINHVIEN;Integrated Security=True";
        }
        public string GetConnectionString(string serverName, string dbName)
        {
            return string.Format(@"Data Source={0};Initial Catalog={1};Integrated Security=True", serverName, dbName);
        }
        public string GetConnectionString(string serverName, string dbName, string userName, string password)
        {
            return string.Format(@"Data Source={0};Initial Catalog={1};Persist Security Info = True; User ID = {2}; Password = {3}", serverName, dbName, userName, password);
        }
        #endregion
        #region Utilities
        /// <summary>
        /// Add params with prefix: @p_{0} ({0} is integer start from 0)
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="obj"></param>
        public void AddParameters(ref SqlCommand cmd, object[] obj)
        {
            int paramsLenth = obj.Length;
            for (int i = 0; i < paramsLenth; i++)
            {
                cmd.Parameters.AddWithValue("@p_" + i.ToString(), obj[i]);
            }
        }
        private void BeginTransact(Action<SqlCommand> action)
        {
            SqlConnection conn = new SqlConnection(GetConnectionString());
            conn.Open();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;
            action(cmd);
            conn.Close();
        }
        public int ExceuteNonQuery(string query, params object[] obj)
        {
            int result = 0;
            BeginTransact(cmd =>
            {
                cmd.CommandText = query;
                if (obj != null)
                {
                    AddParameters(ref cmd, obj);
                }
                result = cmd.ExecuteNonQuery();
            });
            return result;
        }
        public int ExceuteScalar(string query, params object[] obj)
        {
            int result = 0;
            BeginTransact(cmd =>
            {
                cmd.CommandText = query;
                if (obj != null)
                {
                    AddParameters(ref cmd, obj);
                }
                result = (int)cmd.ExecuteScalar();
            });
            return result;
        }
        public List<T> ExecuteReader<T>(string query, params object[] obj) where T : class, new()//Attribute for avoid normal data type
        {
            List<T> list = new List<T>();
            BeginTransact(cmd =>
            {
                cmd.CommandText = query;
                if (obj != null)
                {
                    AddParameters(ref cmd, obj);
                }
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    T item = new T();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        PropertyInfo propertyInfo = typeof(T).GetProperty(reader.GetName(i));
                        if (propertyInfo != null)
                        {
                            propertyInfo.SetValue(item, reader.GetValue(i));
                        }
                    }
                    list.Add(item);
                }
            });
            return list;
        }
        public SqlDataAdapter GetAdapter(string selectCommandText)
        {
            SqlDataAdapter sqlDataAdapter = new SqlDataAdapter();
            BeginTransact(cmd => {
                cmd.CommandText = selectCommandText;
                sqlDataAdapter.SelectCommand = cmd;
            });
            return sqlDataAdapter;
        }
        #endregion
    }
}
