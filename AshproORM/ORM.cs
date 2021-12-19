using AshproStringExtension;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace AshproORM
{
    public class ORM
    {

        #region Public Method

        #region Async Method
        public static async Task<DataTable> GetDataTableAsync(string Query, string sConnection)
        {
            var value = await Task.Run<DataTable>(() =>
            {
                try
                {
                    DataTable dt = new DataTable();
                    using (SqlConnection con = new SqlConnection(sConnection))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(Query, con))
                        {
                            da.Fill(dt);
                        }
                    }
                    return dt;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
            return value;
        }
        public static async Task<T> GetAsync<T>(string Query, string Connection)
        {
            try
            {
                Type temp = typeof(T);
                T obj = Activator.CreateInstance<T>();
                string table = obj.GetType().Name;
                DataTable dt = new DataTable();
                dt = await GetDataTableAsync(Query, Connection);
                if (dt.Rows.Count > 0)
                {
                    obj = GetItem<T>(dt.Rows[0]);
                }
                return obj;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<dynamic> GetValueAsync(string Query, string Connection)
        {
            try
            {
                DataTable dt = new DataTable();
                dt = await GetDataTableAsync(Query, Connection);
                if (dt.Rows.Count > 0)
                {
                    return dt.Rows[0][0] == DBNull.Value ? null : dt.Rows[0][0];
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<List<T>> GetListAsync<T>(string Query, string Connection)
        {
            try
            {
                if (!Query.ToLower().Contains("select"))
                {
                    Type temp = typeof(T);
                    T obj = Activator.CreateInstance<T>();
                    string table = Query ?? obj.GetType().Name;
                    Query = "Select * From " + table;
                }
                List<T> dsList = new List<T>();
                DataTable dt = new DataTable();
                dt = await GetDataTableAsync(Query, Connection);
                dsList = ConvertDataTable<T>(dt);
                return dsList;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<List<string>> GetStringListAsync(string Query, string Connection)
        {
            try
            {
                List<string> dsList = new List<string>();
                DataTable dt = new DataTable();
                dt = await GetDataTableAsync(Query, Connection);
                foreach (DataRow item in dt.Rows)
                {
                    dsList.Add(item[0].ToString());
                }
                return dsList;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<bool> DatabaseMethodAsync(string Query, string Connection)
        {
            var value = await Task.Run<bool>(() =>
            {
                try
                {
                    if (Query == string.Empty)
                    {
                        return false;
                    }
                    using (SqlConnection con = new SqlConnection(Connection))
                    {
                        using (SqlCommand cmd = new SqlCommand(Query, con))
                        {
                            con.Open();
                            cmd.ExecuteNonQuery();
                        }
                    }
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            });
            return value;
        }
        public static async Task<bool> InsertAsync(List<object> datas, string table, string Connection)
        {
            try
            {
                foreach (object data in datas)
                {
                    await InsertAsync(data, table, Connection);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<bool> InsertAsync(object data, string table, string Connection)
        {
            try
            {
                List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
                using (SqlConnection con = new SqlConnection(Connection))
                {
                    foreach (var item in data.GetType().GetProperties())
                    {
                        if (item.GetValue(data, null) != null)
                        {
                            if (item.PropertyType.Name == "Nullable`1" && item.GetValue(data, null).ToString() == "0")
                            {
                                continue;
                            }
                            values.Add(new KeyValuePair<string, string>(item.Name, "@" + item.Name));
                        }
                    }
                    string Query = await getInsertCommandAsync(table, values);
                    using (SqlCommand cmd = new SqlCommand(Query, con))
                    {
                        cmd.Parameters.Clear();
                        foreach (var item in data.GetType().GetProperties())
                        {
                            if (item.GetValue(data, null) != null)
                            {
                                if (item.PropertyType.Name == "Nullable`1" && item.GetValue(data, null).ToString() == "0")
                                {
                                    continue;
                                }
                                if (item.PropertyType.Name == "Byte[]")
                                {
                                    cmd.Parameters.AddWithValue("@" + item.Name, (byte[])(item.GetValue(data, null)));
                                }
                                else if (item.PropertyType.Name == "DateTime")
                                {
                                    cmd.Parameters.AddWithValue("@" + item.Name, GetDate((DateTime)(item.GetValue(data, null))));
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue("@" + item.Name, item.GetValue(data, null).ToString());
                                }
                            }
                        }
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        public static async Task<bool> UpdateAsync(object data, string table, string column, int iValue, string Connection)
        {
            try
            {
                List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
                using (SqlConnection con = new SqlConnection(Connection))
                {
                    foreach (var item in data.GetType().GetProperties())
                    {
                        if (item.GetValue(data, null) != null && item.Name != column)
                        {
                            if (item.PropertyType.Name == "Nullable`1" && item.GetValue(data, null).ToString() == "0")
                            {
                                continue;
                            }
                            values.Add(new KeyValuePair<string, string>(item.Name, "@" + item.Name));
                        }
                    }
                    string Query = await getUpdateCommandAsync(table, values, column, "@" + column);
                    using (SqlCommand cmd = new SqlCommand(Query, con))
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@" + column, iValue);
                        foreach (var item in data.GetType().GetProperties())
                        {
                            if (item.GetValue(data, null) != null && item.Name != column)
                            {
                                if (item.PropertyType.Name == "Nullable`1" && item.GetValue(data, null).ToString() == "0")
                                {
                                    continue;
                                }
                                if (item.PropertyType.Name == "Byte[]")
                                {
                                    cmd.Parameters.AddWithValue("@" + item.Name, (byte[])(item.GetValue(data, null)));
                                }
                                else if (item.PropertyType.Name == "DateTime")
                                {
                                    cmd.Parameters.AddWithValue("@" + item.Name, GetDate((DateTime)(item.GetValue(data, null))));
                                }
                                else
                                    cmd.Parameters.AddWithValue("@" + item.Name, item.GetValue(data, null).ToString());
                            }
                        }
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        public static async Task<bool> UpdateAsync(List<object> datas, string table, string column, string Connection)
        {
            try
            {
                int iValue = -1;
                foreach (object data in datas)
                {
                    foreach (var item in data.GetType().GetProperties())
                    {
                        if (item.Name == column)
                        {
                            iValue = item.GetValue(data, null).ToInt32();
                            break;
                        }
                    }
                    await UpdateAsync(data, table, column, iValue, Connection);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<bool> DeleteAsync(string table, string column, int iValue, string Connection)
        {
            var value = await Task.Run<bool>(() =>
            {
                try
                {
                    string Query = "Delete From  " + table + " Where " + column + " = @" + column + "";
                    using (SqlConnection con = new SqlConnection(Connection))
                    {
                        using (SqlCommand cmd = new SqlCommand(Query, con))
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@" + column, iValue);
                            con.Open();
                            cmd.ExecuteNonQuery();
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
            return value;
        }
        public static async Task<bool> DeleteAsync(string Query, string Connection)
        {
            return await DatabaseMethodAsync(Query, Connection);

        }
        public static async Task<bool> UpdateAsync(DataTable datas, DataTable oldDatas, string table, string sConnection)
        {
            try
            {
                await DeleteOldAsync(datas, oldDatas, table, sConnection);
                bool result = false;
                string sValue = string.Empty;
                List<KeyValuePair<dynamic, dynamic>> values = new List<KeyValuePair<dynamic, dynamic>>();
                SqlConnection con = new SqlConnection(sConnection);
                con.Open();
                try
                {
                    foreach (DataRow data in datas.Rows)
                    {
                        bool iIncluded = false;
                        string sColumn = string.Empty;
                        string Query = string.Empty;
                        List<Common> iCommon = new List<Common>();
                        values.Clear();
                        foreach (DataColumn item in data.Table.Columns)
                        {
                            if (item.ColumnName == data.Table.Columns[0].ColumnName)
                            {
                                sColumn = item.ColumnName;
                                sValue = data[item.ColumnName].ToString();
                            }
                            else
                            {
                                values.Add(new KeyValuePair<dynamic, dynamic>(item.ColumnName, data[item.ColumnName].ToString()));
                            }
                            iCommon = await GetCommonAsync(table, sColumn, sConnection);
                        }
                        if (sValue != null && sValue != string.Empty)
                        {
                            iIncluded = iCommon.Any(x => x.id == Convert.ToInt32(sValue));
                            if (iIncluded)
                            {
                                Query = await getUpdateCommandAsync(table, values, sColumn, sValue);
                            }
                            else
                            {
                                Query = await getInsertCommandAsync(table, values);
                            }
                            using (SqlCommand cmd = new SqlCommand(Query, con))
                            {
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    result = true;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    con.Close();
                }
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        public static async Task<bool> UpdateAsync(List<object> newDatas, List<object> oldDatas, string sTable, string sColumn, string sConnection)
        {
            List<Common> newList = new List<Common>();
            List<Common> oldList = new List<Common>();
            newList = await GetIdListAsync(newDatas, sColumn);
            oldList = await GetIdListAsync(oldDatas, sColumn);
            try
            {
                foreach (Common item in oldList)
                {
                    bool included = newList.Any(x => x.id == item.id);
                    if (!included)
                    {
                        await DeleteAsync(sTable, sColumn, item.id, sConnection);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            foreach (Common item in newList)
            {
                bool included = oldList.Any(x => x.id == item.id);
                if (!included)
                {
                    int? sVal = item.id;
                    try
                    {
                        foreach (var obj in newDatas)
                        {
                            foreach (var x in obj.GetType().GetProperties())
                            {
                                if (x.Name == sColumn)
                                {
                                    int? iVal = x.GetValue(obj, null).ToInt32();
                                    if (iVal == sVal)
                                    {
                                        await InsertAsync(obj, sTable, sConnection);
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
                else
                {
                    int? sVal = item.id;
                    try
                    {
                        foreach (var obj in newDatas)
                        {
                            foreach (var x in obj.GetType().GetProperties())
                            {
                                if (x.Name == sColumn)
                                {
                                    int? iVal = x.GetValue(obj, null).ToInt32();
                                    if (iVal == sVal)
                                    {
                                        await UpdateAsync(obj, sTable, sColumn, sVal.ToInt32(), sConnection);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            return true;
        }
        #endregion

        #region Normal Method
        public static T GetObject<T>(string Query, string Connection)
        {
            try
            {
                Type temp = typeof(T);
                T obj = Activator.CreateInstance<T>();
                string table = obj.GetType().Name;
                DataTable dt = new DataTable();
                dt = GetDataTable(Query, Connection);
                if (dt.Rows.Count > 0)
                {
                    obj = GetItem<T>(dt.Rows[0]);
                }
                return obj;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static DataTable GetDataTable(string Query, string sConnection)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(sConnection))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter(Query, con))
                    {
                        da.Fill(dt);
                    }
                }
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static dynamic GetValue(string Query, string Connection)
        {
            try
            {
                DataTable dt = new DataTable();
                dt = GetDataTable(Query, Connection);
                if (dt.Rows.Count > 0)
                {
                    return dt.Rows[0][0] == DBNull.Value ? null : dt.Rows[0][0];
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static List<T> GetList<T>(string Query, string Connection)
        {
            try
            {
                List<T> dsList = new List<T>();
                DataTable dt = new DataTable();
                dt = GetDataTable(Query, Connection);
                dsList = ConvertDataTable<T>(dt);
                return dsList;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static List<string> GetStringList(string Query, string Connection)
        {
            try
            {
                List<string> dsList = new List<string>();
                DataTable dt = new DataTable();
                dt = GetDataTable(Query, Connection);
                foreach (DataRow item in dt.Rows)
                {
                    dsList.Add(item[0].ToString());
                }
                return dsList;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static bool DatabaseExecution(string Query, string Connection)
        {
            if (Query == string.Empty)
            {
                return false;
            }
            try
            {
                using (SqlConnection con = new SqlConnection(Connection))
                {
                    using (SqlCommand cmd = new SqlCommand(Query, con))
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static bool InsertToDatabase(List<object> datas, string table, string Connection)
        {
            try
            {
                foreach (object data in datas)
                {
                    InsertToDatabase(data, table, Connection);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static bool InsertToDatabase(object data, string table, string Connection)
        {
            try
            {
                List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
                using (SqlConnection con = new SqlConnection(Connection))
                {
                    foreach (var item in data.GetType().GetProperties())
                    {
                        if (item.GetValue(data, null) != null)
                        {
                            if (item.PropertyType.Name == "Nullable`1" && item.GetValue(data, null).ToString() == "0")
                            {
                                continue;
                            }
                            values.Add(new KeyValuePair<string, string>(item.Name, "@" + item.Name));
                        }
                    }
                    string Query = getInsertCommand(table, values);
                    using (SqlCommand cmd = new SqlCommand(Query, con))
                    {
                        cmd.Parameters.Clear();
                        foreach (var item in data.GetType().GetProperties())
                        {
                            if (item.GetValue(data, null) != null)
                            {
                                if (item.PropertyType.Name == "Nullable`1" && item.GetValue(data, null).ToString() == "0")
                                {
                                    continue;
                                }
                                if (item.PropertyType.Name == "Byte[]")
                                {
                                    cmd.Parameters.AddWithValue("@" + item.Name, (byte[])(item.GetValue(data, null)));
                                }
                                else if (item.PropertyType.Name == "DateTime")
                                {
                                    cmd.Parameters.AddWithValue("@" + item.Name, GetDate((DateTime)(item.GetValue(data, null))));
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue("@" + item.Name, item.GetValue(data, null).ToString());
                                }
                            }
                        }
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static bool UpdateDatabase(List<object> datas, string table, string column, string Connection)
        {
            try
            {
                int iValue = -1;
                foreach (object data in datas)
                {
                    foreach (var item in data.GetType().GetProperties())
                    {
                        if (item.Name == column)
                        {
                            iValue = item.GetValue(data, null).ToInt32();
                            break;
                        }
                    }
                    UpdateDatabase(data, table, column, iValue, Connection);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static bool UpdateDatabase(object data, string table, string column, int iValue, string Connection)
        {
            try
            {
                List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
                using (SqlConnection con = new SqlConnection(Connection))
                {
                    foreach (var item in data.GetType().GetProperties())
                    {
                        if (item.GetValue(data, null) != null && item.Name != column)
                        {
                            if (item.PropertyType.Name == "Nullable`1" && item.GetValue(data, null).ToString() == "0")
                            {
                                continue;
                            }
                            values.Add(new KeyValuePair<string, string>(item.Name, "@" + item.Name));
                        }
                    }
                    string Query = getUpdateCommand(table, values, column, "@" + column);
                    using (SqlCommand cmd = new SqlCommand(Query, con))
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@" + column, iValue);
                        foreach (var item in data.GetType().GetProperties())
                        {
                            if (item.GetValue(data, null) != null && item.Name != column)
                            {
                                if (item.PropertyType.Name == "Nullable`1" && item.GetValue(data, null).ToString() == "0")
                                {
                                    continue;
                                }
                                if (item.PropertyType.Name == "Byte[]")
                                {
                                    cmd.Parameters.AddWithValue("@" + item.Name, (byte[])(item.GetValue(data, null)));
                                }
                                else if (item.PropertyType.Name == "DateTime")
                                {
                                    cmd.Parameters.AddWithValue("@" + item.Name, GetDate((DateTime)(item.GetValue(data, null))));
                                }
                                else
                                    cmd.Parameters.AddWithValue("@" + item.Name, item.GetValue(data, null).ToString());
                            }
                        }
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static bool DeleteFromDatabase(string table, string column, int iValue, string Connection)
        {
            try
            {
                string Query = "Delete From  " + table + " Where " + column + " = @" + column + "";
                using (SqlConnection con = new SqlConnection(Connection))
                {
                    using (SqlCommand cmd = new SqlCommand(Query, con))
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@" + column, iValue);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static bool DeleteFromDatabase(string Query, string Connection)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(Connection))
                {
                    using (SqlCommand cmd = new SqlCommand(Query, con))
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static bool UpdateDatabase(DataTable datas, DataTable oldDatas, string table, string sConnection)
        {
            try
            {
                DeleteOldItem(datas, oldDatas, table, sConnection);
                bool result = false;
                string sValue = string.Empty;
                List<KeyValuePair<dynamic, dynamic>> values = new List<KeyValuePair<dynamic, dynamic>>();
                SqlConnection con = new SqlConnection(sConnection);
                con.Open();
                try
                {
                    foreach (DataRow data in datas.Rows)
                    {
                        bool iIncluded = false;
                        string sColumn = string.Empty;
                        string Query = string.Empty;
                        List<Common> iCommon = new List<Common>();
                        values.Clear();
                        foreach (DataColumn item in data.Table.Columns)
                        {
                            if (item.ColumnName == data.Table.Columns[0].ColumnName)
                            {
                                sColumn = item.ColumnName;
                                sValue = data[item.ColumnName].ToString();
                            }
                            else
                            {
                                values.Add(new KeyValuePair<dynamic, dynamic>(item.ColumnName, data[item.ColumnName].ToString()));
                            }
                            iCommon = GetCommon(table, sColumn, sConnection);
                        }
                        if (sValue != null && sValue != string.Empty)
                        {
                            iIncluded = iCommon.Any(x => x.id == Convert.ToInt32(sValue));
                            if (iIncluded)
                            {
                                Query = getUpdateCommand(table, values, sColumn, sValue);
                            }
                            else
                            {
                                Query = getInsertCommand(table, values);
                            }
                            using (SqlCommand cmd = new SqlCommand(Query, con))
                            {
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    result = true;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    con.Close();
                }
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static bool UpdateDatabase(List<object> newDatas, List<object> oldDatas, string sTable, string sColumn, string sConnection)
        {
            List<Common> newList = new List<Common>();
            List<Common> oldList = new List<Common>();
            newList = GetIdList(newDatas, sColumn);
            oldList = GetIdList(oldDatas, sColumn);
            try
            {
                foreach (Common item in oldList)
                {
                    bool included = newList.Any(x => x.id == item.id);
                    if (!included)
                    {
                        DeleteFromDatabase(sTable, sColumn, item.id, sConnection);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            foreach (Common item in newList)
            {
                bool included = oldList.Any(x => x.id == item.id);
                if (!included)
                {
                    int? sVal = item.id;
                    try
                    {
                        foreach (var obj in newDatas)
                        {
                            foreach (var x in obj.GetType().GetProperties())
                            {
                                if (x.Name == sColumn)
                                {
                                    int? iVal = x.GetValue(obj, null).ToInt32();
                                    if (iVal == sVal)
                                    {
                                        InsertToDatabase(obj, sTable, sConnection);
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
                else
                {
                    int? sVal = item.id;
                    try
                    {
                        foreach (var obj in newDatas)
                        {
                            foreach (var x in obj.GetType().GetProperties())
                            {
                                if (x.Name == sColumn)
                                {
                                    int? iVal = x.GetValue(obj, null).ToInt32();
                                    if (iVal == sVal)
                                    {
                                        UpdateDatabase(obj, sTable, sColumn, sVal.ToInt32(), sConnection);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            return true;
        }
        #endregion

        #endregion

        #region Private Method

        #region Async Method
        private static async Task DeleteOldAsync(DataTable newDt, DataTable oldDt, string sTable, string sConnection)
        {
            try
            {
                string sColumn = string.Empty;
                foreach (DataRow item in newDt.Rows)
                {
                    sColumn = item.Table.Columns[0].ColumnName;
                }
                List<Common> newList = new List<Common>();
                List<Common> oldList = new List<Common>();
                newList = await GetIdListAsync(newDt);
                oldList = await GetIdListAsync(oldDt);
                foreach (Common item in oldList)
                {
                    bool included = newList.Any(x => x.id == item.id);
                    if (!included)
                    {
                        await DeleteAsync(sTable, sColumn, item.id, sConnection);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        private static async Task<List<Common>> GetCommonAsync(string sTable, string sColumn, string sConnection)
        {
            try
            {
                List<Common> iCommon = new List<Common>();
                DataTable dt = new DataTable();
                dt = await GetDataTableAsync("Select " + sColumn + " From " + sTable, sConnection);
                foreach (DataRow drw in dt.Rows)
                {
                    Common cmn = new Common();
                    cmn.id = Convert.ToInt32(drw[0].ToString());
                    iCommon.Add(cmn);
                }
                return iCommon;
            }
            catch (Exception)
            {
                throw;
            }
        }
        private static async Task<List<Common>> GetIdListAsync(List<object> data, string sColumn)
        {
            var value = await Task.Run<List<Common>>(() =>
            {
                try
                {
                    List<Common> iCommon = new List<Common>();
                    foreach (var obj in data)
                    {
                        foreach (var item in obj.GetType().GetProperties())
                        {
                            Common cmn = new Common();
                            if (item.Name == sColumn)
                            {
                                cmn.id = item.GetValue(obj, null).ToInt32();
                                iCommon.Add(cmn);
                                break;
                            }
                        }
                    }
                    return iCommon;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
            return value;
        }
        private static async Task<List<Common>> GetIdListAsync(DataTable sTable)
        {
            var value = await Task.Run<List<Common>>(() =>
            {
                try
                {
                    List<Common> iCommon = new List<Common>();
                    foreach (DataRow drw in sTable.Rows)
                    {
                        Common cmn = new Common();
                        cmn.id = Convert.ToInt32(drw[0].ToString());
                        iCommon.Add(cmn);
                        continue;
                    }
                    return iCommon;
                }
                catch (Exception)
                {
                    throw;
                }
            });
            return value;
        }
        private static async Task<List<T>> ConvertDataTableAsync<T>(DataTable dt)
        {
            try
            {
                List<T> data = new List<T>();
                foreach (DataRow row in dt.Rows)
                {
                    T item = await GetItemAsync<T>(row);

                    data.Add(item);
                }
                return data;
            }
            catch (Exception)
            {
                throw;
            }
        }
        private static async Task<T> GetItemAsync<T>(DataRow dr)
        {
            var value = await Task.Run<T>(() =>
            {
                try
                {
                    Type temp = typeof(T);
                    T obj = Activator.CreateInstance<T>();
                    foreach (DataColumn column in dr.Table.Columns)
                    {
                        foreach (PropertyInfo pro in temp.GetProperties())
                        {
                            if (pro.Name == column.ColumnName)

                                if (dr[column.ColumnName] != DBNull.Value)
                                {
                                    if (pro.PropertyType.Name == "Boolean")
                                    {
                                        if (dr[column.ColumnName].ToString() != string.Empty)
                                            pro.SetValue(obj, dr[column.ColumnName].ToString().ToBool(), null);
                                    }
                                    else if (pro.PropertyType.Name == "Int32")
                                    {
                                        if (dr[column.ColumnName].ToString() != string.Empty)
                                        {
                                            pro.SetValue(obj, dr[column.ColumnName].ToInt32(), null);
                                        }
                                    }
                                    else if (pro.PropertyType.Name == "Decimal")
                                    {
                                        if (dr[column.ColumnName].ToString() != string.Empty)
                                            pro.SetValue(obj, dr[column.ColumnName].ToString().ToDecimal(), null);
                                    }
                                    else if (pro.PropertyType.Name == "Nullable`1")
                                    {
                                        if (pro.PropertyType.FullName.Contains("System.Int32"))
                                        {
                                            if (dr[column.ColumnName].ToString() != string.Empty)
                                            {
                                                pro.SetValue(obj, dr[column.ColumnName].ToInt32(), null);
                                            }
                                        }
                                        else if (pro.PropertyType.FullName.Contains("System.Boolean"))
                                        {
                                            if (dr[column.ColumnName].ToString() != string.Empty)
                                            {
                                                pro.SetValue(obj, dr[column.ColumnName].ToString().ToBool(), null);
                                            }
                                        }
                                        else if (pro.PropertyType.FullName.Contains("System.DateTime"))
                                        {
                                            if (dr[column.ColumnName].ToString() != string.Empty)
                                            {
                                                pro.SetValue(obj, Convert.ToDateTime(dr[column.ColumnName].ToString()), null);
                                            }
                                        }
                                        else if (pro.PropertyType.FullName.Contains("System.Decimal"))
                                        {
                                            if (dr[column.ColumnName].ToString() != string.Empty)
                                            {
                                                pro.SetValue(obj, dr[column.ColumnName].ToString().ToDecimal(), null);
                                            }
                                        }
                                    }
                                    else if (pro.PropertyType.Name == "DateTime")
                                    {
                                        if (dr[column.ColumnName].ToString() != string.Empty)
                                            pro.SetValue(obj, Convert.ToDateTime(dr[column.ColumnName].ToString()), null);
                                    }
                                    else if (pro.PropertyType.Name == "Byte[]")
                                    {
                                        pro.SetValue(obj, (byte[])dr[column.ColumnName], null);
                                    }
                                    else
                                    {
                                        pro.SetValue(obj, dr[column.ColumnName].ToString(), null);
                                    }
                                }
                                else
                                {
                                    pro.SetValue(obj, null, null);
                                }
                            else
                                continue;
                        }
                    }
                    return obj;

                }
                catch (Exception ex)
                {
                    string s = ex.Message;
                    throw;
                }
            });
            return value;
        }
        private static async Task<string> getInsertCommandAsync(string table, List<KeyValuePair<dynamic, dynamic>> values)
        {
            var value = await Task.Run<string>(() =>
            {
                try
                {
                    string? query = "";
                    query += "INSERT INTO " + table + " ( ";
                    foreach (var item in values)
                    {
                        query += item.Key;
                        query += ", ";
                    }
                    query = query.Remove(query.Length - 2, 2);
                    query += ") VALUES ( ";
                    foreach (var item in values)
                    {
                        if (item.Key.GetType().Name == "System.Int") // or any other numerics
                        {
                            query += item.Value;
                        }
                        else
                        {
                            query += "'";
                            query += item.Value;
                            query += "'";
                        }
                        query += ", ";
                    }
                    query = query.Remove(query.Length - 2, 2);
                    query += ")";
                    return query;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
            return value;
        }
        private static async Task<string> getUpdateCommandAsync(string table, List<KeyValuePair<dynamic, dynamic>> values, string column, string sValue)
        {

            var value = await Task.Run<string>(() =>
            {
                try
                {
                    string query = null;
                    query += "Update  " + table + " Set ";
                    foreach (var item in values)
                    {
                        query += item.Key;
                        query += "=";
                        if (item.Key.GetType().Name == "System.Int") // or any other numerics
                        {
                            query += item.Value;
                        }
                        else
                        {
                            query += "'";
                            query += item.Value;
                            query += "'";
                        }
                        query += ", ";
                    }
                    query = query.Remove(query.Length - 2, 2);
                    query += " Where " + column + " = '" + sValue + "'";
                    return query;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
            return value;
        }
        private static async Task<bool> isValidDataTypeAsync(string dataType)
        {
            var value = await Task.Run<bool>(() =>
            {
                try
                {
                    bool isValid = false;
                    switch (dataType)
                    {
                        case "System.Nullable`1[System.Double]":
                            isValid = true;
                            break;
                        case "System.Nullable`1[System.Decimal]":
                            isValid = true;
                            break;
                        case "System.Nullable`1[System.Int16]":
                            isValid = true;
                            break;
                        case "System.Nullable`1[System.Int32]":
                            isValid = true;
                            break;
                        case "System.Nullable`1[System.Int64]":
                            isValid = true;
                            break;
                        case "System.Nullable`1[System.Boolean]":
                            isValid = true;
                            break;
                        case "System.Nullable`1[System.DateTime]":
                            isValid = true;
                            break;
                        case "System.Boolean":
                            isValid = true;
                            break;
                        case "System.Int16":
                            isValid = true;
                            break;
                        case "System.Int32":
                            isValid = true;
                            break;
                        case "System.Int64":
                            isValid = true;
                            break;
                        case "System.String":
                            isValid = true;
                            break;
                        case "System.Decimal":
                            isValid = true;
                            break;
                        case "System.Double":
                            isValid = true;
                            break;
                    }
                    return isValid;

                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
            return value;
        }
        private static async Task<string> getUpdateCommandAsync(string table, List<KeyValuePair<string, string>> values, string column, dynamic sValue)
        {
            var value = await Task.Run<string>(() =>
            {
                try
                {
                    string query = null;
                    query += "Update  " + table + " Set ";
                    foreach (var item in values)
                    {
                        query += item.Key;
                        query += "=";
                        query += item.Value;
                        query += ", ";
                    }
                    query = query.Remove(query.Length - 2, 2);
                    query += " Where " + column + " = " + sValue;
                    return query;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
            return value;
        }
        private static async Task<string> getInsertCommandAsync(string table, List<KeyValuePair<string, string>> values)
        {
            var value = await Task.Run<string>(() =>
            {
                try
                {
                    string query = null;
                    query += "INSERT INTO " + table + " ( ";
                    foreach (var item in values)
                    {
                        query += item.Key;
                        query += ", ";
                    }
                    query = query.Remove(query.Length - 2, 2);
                    query += ") VALUES ( ";
                    foreach (var item in values)
                    {

                        query += item.Value;
                        query += ", ";
                    }
                    query = query.Remove(query.Length - 2, 2);
                    query += ")";
                    return query;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
            return value;
        }
        #endregion

        #region Normal Method
        public static string GetDate(DateTime dateTime)
        {
            try
            {
                System.Globalization.CultureInfo enCul = new System.Globalization.CultureInfo("en-US");
                string sVal = dateTime.ToString("yyyy-MM-ddTHH:mm:ss", enCul);
                return sVal;
            }
            catch (Exception)
            {
                throw;
            }
        }
        private static void DeleteOldItem(DataTable newDt, DataTable oldDt, string sTable, string sConnection)
        {
            try
            {
                string sColumn = string.Empty;
                foreach (DataRow item in newDt.Rows)
                {
                    sColumn = item.Table.Columns[0].ColumnName;
                }
                List<Common> newList = new List<Common>();
                List<Common> oldList = new List<Common>();
                newList = GetIdList(newDt);
                oldList = GetIdList(oldDt);
                foreach (Common item in oldList)
                {
                    bool included = newList.Any(x => x.id == item.id);
                    if (!included)
                    {
                        DeleteFromDatabase(sTable, sColumn, item.id, sConnection);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        private static List<Common> GetCommon(string sTable, string sColumn, string sConnection)
        {
            try
            {
                List<Common> iCommon = new List<Common>();
                DataTable dt = new DataTable();
                dt = GetDataTable("Select " + sColumn + " From " + sTable, sConnection);
                foreach (DataRow drw in dt.Rows)
                {
                    Common cmn = new Common();
                    cmn.id = Convert.ToInt32(drw[0].ToString());
                    iCommon.Add(cmn);
                }
                return iCommon;
            }
            catch (Exception)
            {
                throw;
            }
        }
        private static List<Common> GetIdList(List<object> data, string sColumn)
        {
            try
            {
                List<Common> iCommon = new List<Common>();
                foreach (var obj in data)
                {
                    foreach (var item in obj.GetType().GetProperties())
                    {
                        Common cmn = new Common();
                        if (item.Name == sColumn)
                        {
                            cmn.id = item.GetValue(obj, null).ToInt32();
                            iCommon.Add(cmn);
                            break;
                        }
                    }
                }
                return iCommon;
            }
            catch (Exception)
            {
                throw;
            }
        }
        private static List<Common> GetIdList(DataTable sTable)
        {
            try
            {
                List<Common> iCommon = new List<Common>();
                foreach (DataRow drw in sTable.Rows)
                {
                    Common cmn = new Common();
                    cmn.id = Convert.ToInt32(drw[0].ToString());
                    iCommon.Add(cmn);
                    continue;
                }
                return iCommon;
            }
            catch (Exception)
            {
                throw;
            }
        }
        private static List<T> ConvertDataTable<T>(DataTable dt)
        {
            try
            {
                List<T> data = new List<T>();
                foreach (DataRow row in dt.Rows)
                {
                    T item = GetItem<T>(row);

                    data.Add(item);
                }
                return data;
            }
            catch (Exception)
            {
                throw;
            }
        }
        private static T GetItem<T>(DataRow dr)
        {
            try
            {
                Type temp = typeof(T);
                T obj = Activator.CreateInstance<T>();
                foreach (DataColumn column in dr.Table.Columns)
                {
                    foreach (PropertyInfo pro in temp.GetProperties())
                    {
                        if (pro.Name == column.ColumnName)

                            if (dr[column.ColumnName] != DBNull.Value)
                            {
                                if (pro.PropertyType.Name == "Boolean")
                                {
                                    if (dr[column.ColumnName].ToString() != string.Empty)
                                        pro.SetValue(obj, dr[column.ColumnName].ToString().ToBool(), null);
                                }
                                else if (pro.PropertyType.Name == "Int32")
                                {
                                    if (dr[column.ColumnName].ToString() != string.Empty)
                                    {
                                        pro.SetValue(obj, dr[column.ColumnName].ToInt32(), null);
                                    }
                                }
                                else if (pro.PropertyType.Name == "Decimal")
                                {
                                    if (dr[column.ColumnName].ToString() != string.Empty)
                                        pro.SetValue(obj, dr[column.ColumnName].ToString().ToDecimal(), null);
                                }
                                else if (pro.PropertyType.Name == "Nullable`1")
                                {
                                    if (pro.PropertyType.FullName.Contains("System.Int32"))
                                    {
                                        if (dr[column.ColumnName].ToString() != string.Empty)
                                        {
                                            pro.SetValue(obj, dr[column.ColumnName].ToInt32(), null);
                                        }
                                    }
                                    else if (pro.PropertyType.FullName.Contains("System.Boolean"))
                                    {
                                        if (dr[column.ColumnName].ToString() != string.Empty)
                                        {
                                            pro.SetValue(obj, dr[column.ColumnName].ToString().ToBool(), null);
                                        }
                                    }
                                    else if (pro.PropertyType.FullName.Contains("System.DateTime"))
                                    {
                                        if (dr[column.ColumnName].ToString() != string.Empty)
                                        {
                                            pro.SetValue(obj, Convert.ToDateTime(dr[column.ColumnName].ToString()), null);
                                        }
                                    }
                                    else if (pro.PropertyType.FullName.Contains("System.Decimal"))
                                    {
                                        if (dr[column.ColumnName].ToString() != string.Empty)
                                        {
                                            pro.SetValue(obj, dr[column.ColumnName].ToString().ToDecimal(), null);
                                        }
                                    }
                                }
                                else if (pro.PropertyType.Name == "DateTime")
                                {
                                    if (dr[column.ColumnName].ToString() != string.Empty)
                                        pro.SetValue(obj, Convert.ToDateTime(dr[column.ColumnName].ToString()), null);
                                }
                                else if (pro.PropertyType.Name == "Byte[]")
                                {
                                    pro.SetValue(obj, (byte[])dr[column.ColumnName], null);
                                }
                                else
                                {
                                    pro.SetValue(obj, dr[column.ColumnName].ToString(), null);
                                }
                            }
                            else
                            {
                                pro.SetValue(obj, null, null);
                            }
                        else
                            continue;
                    }
                }
                return obj;

            }
            catch (Exception ex)
            {
                string s = ex.Message;
                throw;
            }
        }
        private static string getInsertCommand(string table, List<KeyValuePair<dynamic, dynamic>> values)
        {
            string query = null;
            query += "INSERT INTO " + table + " ( ";
            foreach (var item in values)
            {
                query += item.Key;
                query += ", ";
            }
            query = query.Remove(query.Length - 2, 2);
            query += ") VALUES ( ";
            foreach (var item in values)
            {
                if (item.Key.GetType().Name == "System.Int") // or any other numerics
                {
                    query += item.Value;
                }
                else
                {
                    query += "'";
                    query += item.Value;
                    query += "'";
                }
                query += ", ";
            }
            query = query.Remove(query.Length - 2, 2);
            query += ")";
            return query;
        }
        private static string getUpdateCommand(string table, List<KeyValuePair<dynamic, dynamic>> values, string column, string sValue)
        {
            string query = null;
            query += "Update  " + table + " Set ";
            foreach (var item in values)
            {
                query += item.Key;
                query += "=";
                if (item.Key.GetType().Name == "System.Int") // or any other numerics
                {
                    query += item.Value;
                }
                else
                {
                    query += "'";
                    query += item.Value;
                    query += "'";
                }
                query += ", ";
            }
            query = query.Remove(query.Length - 2, 2);
            query += " Where " + column + " = '" + sValue + "'";
            return query;
        }
        private static bool isValidDataType(string dataType)
        {
            bool isValid = false;
            switch (dataType)
            {
                case "System.Nullable`1[System.Double]":
                    isValid = true;
                    break;
                case "System.Nullable`1[System.Decimal]":
                    isValid = true;
                    break;
                case "System.Nullable`1[System.Int16]":
                    isValid = true;
                    break;
                case "System.Nullable`1[System.Int32]":
                    isValid = true;
                    break;
                case "System.Nullable`1[System.Int64]":
                    isValid = true;
                    break;
                case "System.Nullable`1[System.Boolean]":
                    isValid = true;
                    break;
                case "System.Nullable`1[System.DateTime]":
                    isValid = true;
                    break;
                case "System.Boolean":
                    isValid = true;
                    break;
                case "System.Int16":
                    isValid = true;
                    break;
                case "System.Int32":
                    isValid = true;
                    break;
                case "System.Int64":
                    isValid = true;
                    break;
                case "System.String":
                    isValid = true;
                    break;
                case "System.Decimal":
                    isValid = true;
                    break;
                case "System.Double":
                    isValid = true;
                    break;
            }
            return isValid;
        }
        private static string getUpdateCommand(string table, List<KeyValuePair<string, string>> values, string column, dynamic sValue)
        {
            string query = null;
            query += "Update  " + table + " Set ";
            foreach (var item in values)
            {
                query += item.Key;
                query += "=";
                query += item.Value;
                query += ", ";
            }
            query = query.Remove(query.Length - 2, 2);
            query += " Where " + column + " = " + sValue;
            return query;
        }
        private static string getInsertCommand(string table, List<KeyValuePair<string, string>> values)
        {
            string query = null;
            query += "INSERT INTO " + table + " ( ";
            foreach (var item in values)
            {
                query += item.Key;
                query += ", ";
            }
            query = query.Remove(query.Length - 2, 2);
            query += ") VALUES ( ";
            foreach (var item in values)
            {

                query += item.Value;
                query += ", ";
            }
            query = query.Remove(query.Length - 2, 2);
            query += ")";
            return query;
        }
        #endregion

        #endregion

        #region Public Method BySP

        #region Normal
        public static bool InsertToDatabase_SP(List<object> entities, string sStoredProceedure, string Connection)
        {
            bool result = false;
            try
            {
                foreach (object data in entities)
                {
                    InsertToDatabase_SP(data, sStoredProceedure, Connection);
                }
                result = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }
        public static bool InsertToDatabase_SP(object entity, string sStoredProceedure, string Connection)
        {
            bool result = false;
            using (SqlConnection con = new SqlConnection(Connection))
            {
                using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    try
                    {
                        foreach (var item in entity.GetType().GetProperties())
                        {
                            if (item.GetValue(entity, null) != null)
                            {
                                if (item.PropertyType.Name == "Nullable`1" && item.GetValue(entity, null).ToString() == "0")
                                {
                                    continue;
                                }
                                if (item.PropertyType.Name == "Byte[]")
                                {
                                    cmd.Parameters.AddWithValue(item.Name, (byte[])(item.GetValue(entity, null)));
                                }
                                else if (item.PropertyType.Name == "DateTime")
                                {
                                    cmd.Parameters.AddWithValue("@" + item.Name, GetDate((DateTime)(item.GetValue(entity, null))));
                                }
                                else
                                    cmd.Parameters.AddWithValue(item.Name, item.GetValue(entity, null).ToString());
                            }
                        }
                        con.Open();
                        int numRes = cmd.ExecuteNonQuery();
                        if (numRes > 0)
                        {
                            result = true;
                        }
                        else
                        {
                            result = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            return result;
        }
        public static int InsertToDatabase_SP(object entity, DataSet ds, string sStoredProceedure, string Connection, object Secondentity = null)
        {
            int? result = null;
            using (SqlConnection con = new SqlConnection(Connection))
            {
                using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    try
                    {
                        if (ds != null)
                        {
                            foreach (DataTable dt in ds.Tables)
                            {
                                cmd.Parameters.AddWithValue("@" + dt.TableName, dt);
                            }
                        }
                        if (entity != null)
                        {
                            foreach (var item in entity.GetType().GetProperties())
                            {
                                if (item.GetValue(entity, null) != null)
                                {
                                    if (item.PropertyType.Name == "Nullable`1" && item.GetValue(entity, null).ToString() == "0")
                                    {
                                        continue;
                                    }
                                    if (item.PropertyType.Name == "Byte[]")
                                    {
                                        cmd.Parameters.AddWithValue(item.Name, (byte[])(item.GetValue(entity, null)));
                                    }
                                    else if (item.PropertyType.Name == "DateTime")
                                    {
                                        cmd.Parameters.AddWithValue("@" + item.Name, GetDate((DateTime)(item.GetValue(entity, null))));
                                    }
                                    else
                                        cmd.Parameters.AddWithValue(item.Name, item.GetValue(entity, null).ToString());
                                }
                            }
                        }
                        if (Secondentity != null)
                        {
                            foreach (var item in Secondentity.GetType().GetProperties())
                            {
                                if (item.GetValue(Secondentity, null) != null)
                                {
                                    if (item.PropertyType.Name == "Nullable`1" && item.GetValue(Secondentity, null).ToString() == "0")
                                    {
                                        continue;
                                    }
                                    if (item.PropertyType.Name == "Byte[]")
                                    {
                                        cmd.Parameters.AddWithValue(item.Name, (byte[])(item.GetValue(Secondentity, null)));
                                    }
                                    else if (item.PropertyType.Name == "DateTime")
                                    {
                                        cmd.Parameters.AddWithValue("@" + item.Name, GetDate((DateTime)(item.GetValue(Secondentity, null))));
                                    }
                                    else
                                        cmd.Parameters.AddWithValue(item.Name, item.GetValue(Secondentity, null).ToString());
                                }
                            }
                        }
                        cmd.Parameters.Add("@return", SqlDbType.Int);
                        cmd.Parameters.Add("@errMessage", SqlDbType.Char, 500);
                        cmd.Parameters["@return"].Direction = ParameterDirection.Output;
                        cmd.Parameters["@errMessage"].Direction = ParameterDirection.Output;
                        con.Open();
                        cmd.CommandTimeout = 0;
                        cmd.ExecuteNonQuery();
                        string message = cmd.Parameters["@errMessage"].Value.ToString2();
                        result = cmd.Parameters["@return"].Value.ToString2().ToInt32();
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            return result.toInt32();
        }
        public static bool UpdateDatabase_SP(object entity, string sStoredProceedure, string Connection)
        {
            bool result = false;
            int numRes = 0;
            using (SqlConnection con = new SqlConnection(Connection))
            {
                using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    try
                    {
                        foreach (var item in entity.GetType().GetProperties())
                        {
                            if (item.GetValue(entity, null) != null)
                            {
                                if (item.PropertyType.Name == "Nullable`1" && item.GetValue(entity, null).ToString() == "0")
                                {
                                    continue;
                                }
                                if (item.PropertyType.Name == "Byte[]")
                                {
                                    cmd.Parameters.AddWithValue(item.Name, (byte[])(item.GetValue(entity, null)));
                                }
                                else if (item.PropertyType.Name == "DateTime")
                                {
                                    cmd.Parameters.AddWithValue("@" + item.Name, GetDate((DateTime)(item.GetValue(entity, null))));
                                }
                                else
                                    cmd.Parameters.AddWithValue(item.Name, item.GetValue(entity, null).ToString());
                            }
                        }
                        con.Open();
                        numRes = cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            if (numRes > 0)
            {
                result = true;
            }
            else
            {
                result = false;
            }
            return result;
        }
        public static bool UpdateDatabase_SP(List<object> entities, string sStoredProceedure, string Connection)
        {
            try
            {
                foreach (object data in entities)
                {
                    UpdateDatabase_SP(data, sStoredProceedure, Connection);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static bool UpdateDatabase_SP(List<object> newDatas, List<object> oldDatas, string sTable, string sColumn, string sConnection)
        {
            List<Common> newList = new List<Common>();
            List<Common> oldList = new List<Common>();
            newList = GetIdList(newDatas, sColumn);
            oldList = GetIdList(oldDatas, sColumn);
            try
            {
                if (oldList.Count > 0)
                {
                    foreach (Common item in oldList)
                    {
                        bool included = newList.Any(x => x.id == item.id);
                        if (!included)
                        {
                            DeleteDatabase_SP(item.id, "spDelete" + sTable, sConnection);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            foreach (Common item in newList)
            {
                bool included = oldList.Any(x => x.id == item.id);
                if (!included)
                {
                    int? sVal = item.id;
                    try
                    {
                        foreach (var obj in newDatas)
                        {
                            foreach (var x in obj.GetType().GetProperties())
                            {
                                if (x.Name == sColumn)
                                {
                                    int? iVal = x.GetValue(obj, null).ToInt32();
                                    if (iVal == sVal)
                                    {
                                        InsertToDatabase_SP(obj, "spInsert" + sTable, sConnection);
                                        break;
                                    }
                                }
                            }

                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
                else
                {
                    int? sVal = item.id;
                    try
                    {
                        foreach (var obj in newDatas)
                        {
                            foreach (var x in obj.GetType().GetProperties())
                            {
                                if (x.Name == sColumn)
                                {
                                    int? iVal = x.GetValue(obj, null).ToInt32();
                                    if (iVal == sVal)
                                    {
                                        UpdateDatabase_SP(obj, "spUpdate" + sTable, sConnection);
                                    }
                                }
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            return true;
        }
        public static bool DeleteDatabase_SP(object entity, string sStoredProceedure, string Connection)
        {
            bool result = false;
            int numRes = 0;
            using (SqlConnection con = new SqlConnection(Connection))
            {
                using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    try
                    {
                        cmd.Parameters.AddWithValue("Id", entity.ToInt32());
                        con.Open();
                        numRes = cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            if (numRes > 0)
            {
                result = true;
            }
            else
            {
                result = false;
            }
            return result;
        }
        public static DataTable GetDataTable_SP(string sStoredProceedure, string Connection)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(Connection))
                {
                    using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static DataTable GetDataTableWithParameter_SP(string sStoredProceedure, string Value, string Connection)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(Connection))
                {
                    using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Id", Value);
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static DataTable GetDataTableWithParameter_SP(string sStoredProceedure, object entity, string Connection)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(Connection))
                {
                    using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        if (entity != null)
                        {
                            foreach (var item in entity.GetType().GetProperties())
                            {
                                if (item.GetValue(entity, null) != null)
                                {
                                    cmd.Parameters.AddWithValue(item.Name, item.GetValue(entity, null).ToString());
                                }
                            }
                        }
                        cmd.CommandTimeout = 0;
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static List<T> GetList_SP<T>(string sStoredProceedure, string Connection)
        {
            try
            {
                List<T> dsList = new List<T>();
                DataTable dt = new DataTable();
                dt = GetDataTable_SP(sStoredProceedure, Connection);
                dsList = ConvertDataTable<T>(dt);
                return dsList;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static List<T> GetListWithParameter_SP<T>(string sStoredProceedure, string Value, string Connection)
        {
            try
            {
                List<T> dsList = new List<T>();
                DataTable dt = new DataTable();
                dt = GetDataTableWithParameter_SP(sStoredProceedure, Value, Connection);
                dsList = ConvertDataTable<T>(dt);
                return dsList;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static List<T> GetListWithParameter_SP<T>(string sStoredProceedure, object entity, string Connection)
        {
            try
            {
                List<T> dsList = new List<T>();
                DataTable dt = new DataTable();
                dt = GetDataTableWithParameter_SP(sStoredProceedure, entity, Connection);
                dsList = ConvertDataTable<T>(dt);
                return dsList;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static T GetObjectWithparameter_SP<T>(string sStoredProceedure, string Value, string Connection)
        {
            try
            {
                Type temp = typeof(T);
                T obj = Activator.CreateInstance<T>();
                DataTable dt = new DataTable();
                dt = GetDataTableWithParameter_SP(sStoredProceedure, Value, Connection);
                if (dt.Rows.Count > 0)
                {
                    obj = GetItem<T>(dt.Rows[0]);
                }
                return obj;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static T GetObjectWithparameter_SP<T>(string sStoredProceedure, object entity, string Connection)
        {
            try
            {
                Type temp = typeof(T);
                T obj = Activator.CreateInstance<T>();
                DataTable dt = new DataTable();
                dt = GetDataTableWithParameter_SP(sStoredProceedure, entity, Connection);
                if (dt.Rows.Count > 0)
                {
                    obj = GetItem<T>(dt.Rows[0]);
                }
                return obj;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static T GetObject_SP<T>(string sStoredProceedure, string Connection)
        {
            try
            {
                T obj = Activator.CreateInstance<T>();
                DataTable dt = new DataTable();
                dt = GetDataTable_SP(sStoredProceedure, Connection);
                if (dt.Rows.Count > 0)
                {
                    obj = GetItem<T>(dt.Rows[0]);
                }
                return obj;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static string GetData_SP(string sStoredProceedure, string Connection)
        {
            DataTable dt = new DataTable();
            dt = GetDataTable_SP(sStoredProceedure, Connection);
            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0][0].ToString();
            }
            else
            {
                return null;
            }
        }
        public static string GetDataWithParameter_SP(string sStoredProceedure, object entity, string Connection)
        {
            DataTable dt = new DataTable();
            dt = GetDataTableWithParameter_SP(sStoredProceedure, entity, Connection);
            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0][0].ToString();
            }
            else
            {
                return null;
            }
        }
        public static bool DatabaseExecution_SP(string sStoredProceedure, string Connection, Object entity = null)
        {
            bool result = false;
            using (SqlConnection con = new SqlConnection(Connection))
            {
                using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    if (entity != null)
                    {
                        foreach (var item in entity.GetType().GetProperties())
                        {
                            if (item.GetValue(entity, null) != null)
                            {
                                cmd.Parameters.AddWithValue(item.Name, item.GetValue(entity, null).ToString());
                            }
                        }
                    }
                    try
                    {
                        con.Open();
                        int numRes = cmd.ExecuteNonQuery();
                        if (numRes > 0)
                        {
                            result = true;
                        }
                        else
                        {
                            result = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            return result;
        }
        public static DataSet GetDataSet_SP(string sStoredProceedure, object entity, string Connection)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection con = new SqlConnection(Connection))
                {
                    using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        if (entity != null)
                        {
                            foreach (var item in entity.GetType().GetProperties())
                            {
                                if (item.GetValue(entity, null) != null)
                                {
                                    cmd.Parameters.AddWithValue(item.Name, item.GetValue(entity, null).ToString());
                                }
                            }
                        }
                        cmd.CommandTimeout = 0;
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(ds);
                        }
                    }
                }
                return ds;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region Async
        public static async Task<bool> InsertAsync_SP(List<object> entities, string sStoredProceedure, string Connection)
        {

            try
            {
                foreach (object data in entities)
                {
                    await InsertAsync_SP(data, sStoredProceedure, Connection);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<bool> InsertAsync_SP(object entity, string sStoredProceedure, string Connection)
        {
            var value = await Task.Run<bool>(() =>
            {
                try
                {
                    bool result = false;
                    using (SqlConnection con = new SqlConnection(Connection))
                    {
                        using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            try
                            {
                                foreach (var item in entity.GetType().GetProperties())
                                {
                                    if (item.GetValue(entity, null) != null)
                                    {
                                        if (item.PropertyType.Name == "Nullable`1" && item.GetValue(entity, null).ToString() == "0")
                                        {
                                            continue;
                                        }
                                        if (item.PropertyType.Name == "Byte[]")
                                        {
                                            cmd.Parameters.AddWithValue(item.Name, (byte[])(item.GetValue(entity, null)));
                                        }
                                        else if (item.PropertyType.Name == "DateTime")
                                        {
                                            cmd.Parameters.AddWithValue("@" + item.Name, GetDate((DateTime)(item.GetValue(entity, null))));
                                        }
                                        else
                                            cmd.Parameters.AddWithValue(item.Name, item.GetValue(entity, null).ToString());
                                    }
                                }
                                con.Open();
                                int numRes = cmd.ExecuteNonQuery();
                                if (numRes > 0)
                                {
                                    result = true;
                                }
                                else
                                {
                                    result = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
            return value;
        }
        public static async Task<int> InsertAsync_SP(object entity, DataSet ds, string sStoredProceedure, string Connection, object Secondentity = null)
        {
            var value = await Task.Run<int>(() =>
            {
                try
                {
                    int? result = null;
                    using (SqlConnection con = new SqlConnection(Connection))
                    {
                        using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            try
                            {
                                if (ds != null)
                                {
                                    foreach (DataTable dt in ds.Tables)
                                    {
                                        cmd.Parameters.AddWithValue("@" + dt.TableName, dt);
                                    }
                                }
                                if (entity != null)
                                {
                                    foreach (var item in entity.GetType().GetProperties())
                                    {
                                        if (item.GetValue(entity, null) != null)
                                        {
                                            if (item.PropertyType.Name == "Nullable`1" && item.GetValue(entity, null).ToString() == "0")
                                            {
                                                continue;
                                            }
                                            if (item.PropertyType.Name == "Byte[]")
                                            {
                                                cmd.Parameters.AddWithValue(item.Name, (byte[])(item.GetValue(entity, null)));
                                            }
                                            else if (item.PropertyType.Name == "DateTime")
                                            {
                                                cmd.Parameters.AddWithValue("@" + item.Name, GetDate((DateTime)(item.GetValue(entity, null))));
                                            }
                                            else
                                                cmd.Parameters.AddWithValue(item.Name, item.GetValue(entity, null).ToString());
                                        }
                                    }
                                }
                                if (Secondentity != null)
                                {
                                    foreach (var item in Secondentity.GetType().GetProperties())
                                    {
                                        if (item.GetValue(Secondentity, null) != null)
                                        {
                                            if (item.PropertyType.Name == "Nullable`1" && item.GetValue(Secondentity, null).ToString() == "0")
                                            {
                                                continue;
                                            }
                                            if (item.PropertyType.Name == "Byte[]")
                                            {
                                                cmd.Parameters.AddWithValue(item.Name, (byte[])(item.GetValue(Secondentity, null)));
                                            }
                                            else if (item.PropertyType.Name == "DateTime")
                                            {
                                                cmd.Parameters.AddWithValue("@" + item.Name, GetDate((DateTime)(item.GetValue(Secondentity, null))));
                                            }
                                            else
                                                cmd.Parameters.AddWithValue(item.Name, item.GetValue(Secondentity, null).ToString());
                                        }
                                    }
                                }
                                cmd.Parameters.Add("@return", SqlDbType.Int);
                                cmd.Parameters.Add("@errMessage", SqlDbType.Char, 500);
                                cmd.Parameters["@return"].Direction = ParameterDirection.Output;
                                cmd.Parameters["@errMessage"].Direction = ParameterDirection.Output;
                                con.Open();
                                cmd.CommandTimeout = 0;
                                cmd.ExecuteNonQuery();
                                string message = cmd.Parameters["@errMessage"].Value.ToString2();
                                result = cmd.Parameters["@return"].Value.ToString2().ToInt32();
                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    return result.toInt32();

                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
            return value;
        }
        public static async Task<bool> UpdateAsync_SP(object entity, string sStoredProceedure, string Connection)
        {
            var value = await Task.Run<bool>(() =>
            {
                try
                {
                    bool result = false;
                    int numRes = 0;
                    using (SqlConnection con = new SqlConnection(Connection))
                    {
                        using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            try
                            {
                                foreach (var item in entity.GetType().GetProperties())
                                {
                                    if (item.GetValue(entity, null) != null)
                                    {
                                        if (item.PropertyType.Name == "Nullable`1" && item.GetValue(entity, null).ToString() == "0")
                                        {
                                            continue;
                                        }
                                        if (item.PropertyType.Name == "Byte[]")
                                        {
                                            cmd.Parameters.AddWithValue(item.Name, (byte[])(item.GetValue(entity, null)));
                                        }
                                        else if (item.PropertyType.Name == "DateTime")
                                        {
                                            cmd.Parameters.AddWithValue("@" + item.Name, GetDate((DateTime)(item.GetValue(entity, null))));
                                        }
                                        else
                                            cmd.Parameters.AddWithValue(item.Name, item.GetValue(entity, null).ToString());
                                    }
                                }
                                con.Open();
                                numRes = cmd.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    if (numRes > 0)
                    {
                        result = true;
                    }
                    else
                    {
                        result = false;
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
            return value;
        }
        public static async Task<bool> UpdateAsync_SP(List<object> entities, string sStoredProceedure, string Connection)
        {
            try
            {
                foreach (object data in entities)
                {
                    await UpdateAsync_SP(data, sStoredProceedure, Connection);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<bool> UpdateAsync_SP(List<object> newDatas, List<object> oldDatas, string sTable, string sColumn, string sConnection)
        {
            List<Common> newList = new List<Common>();
            List<Common> oldList = new List<Common>();
            newList = await GetIdListAsync(newDatas, sColumn);
            oldList = await GetIdListAsync(oldDatas, sColumn);
            try
            {
                if (oldList.Count > 0)
                {
                    foreach (Common item in oldList)
                    {
                        bool included = newList.Any(x => x.id == item.id);
                        if (!included)
                        {
                            await DeleteAsync_SP(item.id, "spDelete" + sTable, sConnection);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            foreach (Common item in newList)
            {
                bool included = oldList.Any(x => x.id == item.id);
                if (!included)
                {
                    int? sVal = item.id;
                    try
                    {
                        foreach (var obj in newDatas)
                        {
                            foreach (var x in obj.GetType().GetProperties())
                            {
                                if (x.Name == sColumn)
                                {
                                    int? iVal = x.GetValue(obj, null).ToInt32();
                                    if (iVal == sVal)
                                    {
                                        await InsertAsync_SP(obj, "spInsert" + sTable, sConnection);
                                        break;
                                    }
                                }
                            }

                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
                else
                {
                    int? sVal = item.id;
                    try
                    {
                        foreach (var obj in newDatas)
                        {
                            foreach (var x in obj.GetType().GetProperties())
                            {
                                if (x.Name == sColumn)
                                {
                                    int? iVal = x.GetValue(obj, null).ToInt32();
                                    if (iVal == sVal)
                                    {
                                        await UpdateAsync_SP(obj, "spUpdate" + sTable, sConnection);
                                    }
                                }
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            return true;
        }
        public static async Task<bool> DeleteAsync_SP(object entity, string sStoredProceedure, string Connection)
        {
            var value = await Task.Run<bool>(() =>
            {
                try
                {
                    bool result = false;
                    int numRes = 0;
                    using (SqlConnection con = new SqlConnection(Connection))
                    {
                        using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            try
                            {
                                cmd.Parameters.AddWithValue("Id", entity.ToInt32());
                                con.Open();
                                numRes = cmd.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    if (numRes > 0)
                    {
                        result = true;
                    }
                    else
                    {
                        result = false;
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
            return value;
        }
        public static async Task<DataTable> GetDataTableAsync_SP(string sStoredProceedure, string Connection)
        {
            var value = await Task.Run<DataTable>(() =>
            {
                try
                {
                    System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
                    DataTable dt = new DataTable();
                    using (SqlConnection con = new SqlConnection(Connection))
                    {
                        using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                da.Fill(dt);
                            }
                        }
                    }
                    return dt;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
            return value;
        }
        public static async Task<DataTable> GetDataTableWithParameterAsync_SP(string sStoredProceedure, string Value, string Connection)
        {
            var value = await Task.Run<DataTable>(() =>
            {
                try
                {
                    System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
                    DataTable dt = new DataTable();
                    using (SqlConnection con = new SqlConnection(Connection))
                    {
                        using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@Id", Value);
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                da.Fill(dt);
                            }
                        }
                    }
                    return dt;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
            return value;
        }
        public static async Task<DataTable> GetDataTableWithParameterAsync_SP(string sStoredProceedure, object entity, string Connection)
        {
            var value = await Task.Run<DataTable>(() =>
            {
                try
                {
                    System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
                    DataTable dt = new DataTable();
                    using (SqlConnection con = new SqlConnection(Connection))
                    {
                        using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            if (entity != null)
                            {
                                foreach (var item in entity.GetType().GetProperties())
                                {
                                    if (item.GetValue(entity, null) != null)
                                    {
                                        cmd.Parameters.AddWithValue(item.Name, item.GetValue(entity, null).ToString());
                                    }
                                }
                            }
                            cmd.CommandTimeout = 0;
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                da.Fill(dt);
                            }
                        }
                    }
                    return dt;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
            return value;
        }
        public static async Task<List<T>> GetListAsync_SP<T>(string sStoredProceedure, string Connection)
        {
            try
            {
                List<T> dsList = new List<T>();
                DataTable dt = new DataTable();
                dt = await GetDataTableAsync_SP(sStoredProceedure, Connection);
                dsList = ConvertDataTable<T>(dt);
                return dsList;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<List<T>> GetListAsync_SP<T>(string sStoredProceedure, string Value, string Connection)
        {
            try
            {
                List<T> dsList = new List<T>();
                DataTable dt = new DataTable();
                dt = await GetDataTableWithParameterAsync_SP(sStoredProceedure, Value, Connection);
                dsList = await ConvertDataTableAsync<T>(dt);
                return dsList;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<List<T>> GetListAsync_SP<T>(string sStoredProceedure, object entity, string Connection)
        {
            try
            {
                List<T> dsList = new List<T>();
                DataTable dt = new DataTable();
                dt = await GetDataTableWithParameterAsync_SP(sStoredProceedure, entity, Connection);
                dsList = await ConvertDataTableAsync<T>(dt);
                return dsList;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<T> GetAsyncWithparameter_SP<T>(string sStoredProceedure, string Value, string Connection)
        {
            try
            {
                Type temp = typeof(T);
                T obj = Activator.CreateInstance<T>();
                DataTable dt = new DataTable();
                dt = await GetDataTableWithParameterAsync_SP(sStoredProceedure, Value, Connection);
                if (dt.Rows.Count > 0)
                {
                    obj = await GetItemAsync<T>(dt.Rows[0]);
                }
                return obj;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<T> GetAsyncWithparameter_SP<T>(string sStoredProceedure, object entity, string Connection)
        {
            try
            {
                Type temp = typeof(T);
                T obj = Activator.CreateInstance<T>();
                DataTable dt = new DataTable();
                dt = await GetDataTableWithParameterAsync_SP(sStoredProceedure, entity, Connection);
                if (dt.Rows.Count > 0)
                {
                    obj = await GetItemAsync<T>(dt.Rows[0]);
                }
                return obj;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<T> GetAsync_SP<T>(string sStoredProceedure, string Connection)
        {
            try
            {
                T obj = Activator.CreateInstance<T>();
                DataTable dt = new DataTable();
                dt = await GetDataTableAsync_SP(sStoredProceedure, Connection);
                if (dt.Rows.Count > 0)
                {
                    obj = await GetItemAsync<T>(dt.Rows[0]);
                }
                return obj;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<string> GetDataAsync_SP(string sStoredProceedure, string Connection)
        {
            DataTable dt = new DataTable();
            dt = await GetDataTableAsync_SP(sStoredProceedure, Connection);
            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0][0].ToString();
            }
            else
            {
                return null;
            }
        }
        public static async Task<string> GetDataWithParameterAsync_SP(string sStoredProceedure, object entity, string Connection)
        {
            DataTable dt = new DataTable();
            dt = await GetDataTableWithParameterAsync_SP(sStoredProceedure, entity, Connection);
            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0][0].ToString();
            }
            else
            {
                return null;
            }
        }
        public static async Task<bool> DatabaseExecutionAsync_SP(string sStoredProceedure, string Connection, Object entity = null)
        {
            var value = await Task.Run<bool>(() =>
            {
                try
                {
                    bool result = false;
                    using (SqlConnection con = new SqlConnection(Connection))
                    {
                        using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            if (entity != null)
                            {
                                foreach (var item in entity.GetType().GetProperties())
                                {
                                    if (item.GetValue(entity, null) != null)
                                    {
                                        cmd.Parameters.AddWithValue(item.Name, item.GetValue(entity, null).ToString());
                                    }
                                }
                            }
                            try
                            {
                                con.Open();
                                int numRes = cmd.ExecuteNonQuery();
                                if (numRes > 0)
                                {
                                    result = true;
                                }
                                else
                                {
                                    result = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
            return value;
        }
        public static async Task<DataSet> GetDataSetAsync_SP(string sStoredProceedure, object entity, string Connection)
        {
            var value = await Task.Run<DataSet>(() =>
            {
                try
                {
                    System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
                    DataSet ds = new DataSet();
                    using (SqlConnection con = new SqlConnection(Connection))
                    {
                        using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            if (entity != null)
                            {
                                foreach (var item in entity.GetType().GetProperties())
                                {
                                    if (item.GetValue(entity, null) != null)
                                    {
                                        cmd.Parameters.AddWithValue(item.Name, item.GetValue(entity, null).ToString());
                                    }
                                }
                            }
                            cmd.CommandTimeout = 0;
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                da.Fill(ds);
                            }
                        }
                    }
                    return ds;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
            return value;
        }
        #endregion

        #endregion
    }
}
