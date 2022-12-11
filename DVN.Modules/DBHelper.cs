///Author: Vu Dat and CVT
///Discription: This module using for connect and execute query into database 
///but it's safer by using parameters for prevent SQL Injection
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DVN.Modules
{
    public class DBHelper
    {
        private string conStr;
        public DBHelper() { }
        #region Initialize Instance (Singleton pattern)
        private static DBHelper instance;
        public static DBHelper Instance
        {
            get => instance ?? new DBHelper();
            private set => instance = value;
        }
        public string ConStr { get => conStr; set => conStr = value; }
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
        public void AddParameters(ref SqlCommand cmd, Dictionary<string, object> mapParams)
        {
            foreach (var param in mapParams)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value);
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
        public int ExceuteNonQuery(string query, Dictionary<string, object> mapParams)
        {
            int result = 0;
            BeginTransact(cmd =>
            {
                cmd.CommandText = query;
                if (mapParams != null)
                {
                    AddParameters(ref cmd, mapParams);
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
        public int ExceuteScalar(string query, Dictionary<string, object> mapParams)
        {
            int result = 0;
            BeginTransact(cmd =>
            {
                cmd.CommandText = query;
                if (mapParams != null)
                {
                    AddParameters(ref cmd, mapParams);
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
        public List<T> ExecuteReader<T>(string query, Dictionary<string, object> mapParams) where T : class, new()
        {
            List<T> list = new List<T>();
            BeginTransact(cmd =>
            {
                cmd.CommandText = query;
                if (mapParams != null)
                {
                    AddParameters(ref cmd, mapParams);
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
        public DataSet GetDataSet(SqlDataAdapter sqlDataAdapter)
        {
            DataSet dt = new DataSet();
            return sqlDataAdapter.Fill(dt) != 0 ? dt : null;
        }
        public DataSet GetDataSet(string selectCommandText)
        {
            DataSet dt = new DataSet();
            return GetAdapter(selectCommandText).Fill(dt) != 0 ? dt : null;
        }
        public void FillData(DataSet dataSet, string selectCmd, string tableName)
        {
            SqlDataAdapter adapter = new SqlDataAdapter(selectCmd, ConStr);
            adapter.Fill(dataSet, tableName);
        }
        public void LoadDataIntoCbo(ComboBox cbo, DataSet dataSet, string selectCmd, string tableName, string display, string value)
        {
            FillData(dataSet, selectCmd, tableName);
            cbo.DataSource = dataSet.Tables[tableName];
            cbo.DisplayMember = display;
            cbo.ValueMember = value;
        }
        public void LoadDataIntoDgv(DataGridView dgv, DataSet dataSet, string selectCmd, string tableName)
        {
            FillData(dataSet, selectCmd, tableName);
            dgv.DataSource = dataSet.Tables[tableName];
        }
        public int Update(DataSet ds, string selectCmd, string tableName)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(selectCmd, ConStr);
                SqlCommandBuilder sqlCommandBuilder = new SqlCommandBuilder(adapter);
                return adapter.Update(ds, tableName);
            }
            catch (Exception)
            {
                return 0;
            }

        }
        public void SetPrimaryKey(ref DataTable dt, params object[] mapKeys)
        {
            DataColumn[] keys = new DataColumn[mapKeys.Length];
            for (int i = 0; i < mapKeys.Length; i++)
            {
                keys[i] = dt.Columns[mapKeys[i].ToString()];
            }
            dt.PrimaryKey = keys;
        }
        #endregion
    }
}
