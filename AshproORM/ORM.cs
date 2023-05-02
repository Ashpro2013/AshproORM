using AshproStringExtension;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;


namespace AshproORM
{
    public class ORM
    {

        #region Public Method

        #region Async Method
        public static async Task<bool> BulkCopyAsync(string sTable, DataTable dt, bool isIdentity = true, string sCon = null)
        {
            var value = await Task<bool>.Factory.StartNew(() =>
            {
                sCon = sCon ?? DBConnection.Connection;
                try
                {
                    SqlBulkCopyOptions sqlBulk = isIdentity ? SqlBulkCopyOptions.KeepIdentity : SqlBulkCopyOptions.Default;
                    using (SqlBulkCopy bulkData = new SqlBulkCopy(sCon, sqlBulk))
                    {
                        bulkData.DestinationTableName = sTable;
                        bulkData.WriteToServer(dt);
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
        public static async Task<dynamic> GetSingleDataAsync(string Query, string sCon = null)
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection con = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(Query, con))
                    {
                        await con.OpenAsync();
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != System.DBNull.Value)
                        {
                            return result;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<DataTable> GetDataTableAsync(string Query, string sCon = null)
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(Query, con))
                    {
                        await con.OpenAsync();
                        using (SqlDataAdapter sdr = new SqlDataAdapter(cmd))
                        {
                            sdr.Fill(dt);
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
        public static async Task<T> GetAsync<T>(string Query, string sCon = null) where T : new()
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection conn = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(Query, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        await conn.OpenAsync();
                        var reader = await cmd.ExecuteReaderAsync();
                        T list = ToSingle<T>(reader);
                        reader.Close();
                        reader.Dispose();
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<List<T>> GetListAsync<T>(string commandText, string sCon = null) where T : new()
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection conn = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(commandText, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        await conn.OpenAsync();
                        var reader = await cmd.ExecuteReaderAsync();
                        List<T> list = ToList<T>(reader);
                        reader.Close();
                        reader.Dispose();
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<List<string>> GetStringListAsync(string Query, string sCon = null)
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection conn = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(Query, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        await conn.OpenAsync();
                        var reader = await cmd.ExecuteReaderAsync();
                        List<string> list = ToList(reader);
                        reader.Close();
                        reader.Dispose();
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<bool> DatabaseMethodAsync(string Query, string sCon = null)
        {
            try
            {
                if (Query == string.Empty) { return false; }
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection con = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(Query, con))
                    {
                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }
        public static async Task<bool> InsertAsync(List<object> datas, string table, string sCon = null)
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                foreach (object data in datas)
                {
                    await InsertAsync(data, table, sCon);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<bool> InsertAsync(object data, string table, string sCon = null)
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
                using (SqlConnection con = new SqlConnection(sCon))
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
                        EntityLoadMethod(data, cmd);
                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        public static async Task<bool> UpdateAsync(object data, string table, string column, int iValue, string sCon = null)
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
                using (SqlConnection con = new SqlConnection(sCon))
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
                        EntityLoadMethod(data, cmd);
                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        public static async Task<bool> UpdateAsync(List<object> datas, string table, string column, string sCon = null)
        {
            try
            {
                int iValue = -1;
                sCon = sCon ?? DBConnection.Connection;
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
                    await UpdateAsync(data, table, column, iValue, sCon);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<bool> DeleteAsync(string table, string column, int iValue, string sCon = null)
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                string Query = "Delete From  " + table + " Where " + column + " = @" + column + "";
                using (SqlConnection con = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(Query, con))
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@" + column, iValue);
                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<bool> DeleteAsync(string Query, string sCon = null) => await DatabaseMethodAsync(Query, sCon);
        public static async Task<bool> UpdateAsync(DataTable datas, DataTable oldDatas, string table, string sCon = null)
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                await DeleteOldAsync(datas, oldDatas, table, sCon);
                bool result = false;
                string sValue = string.Empty;
                List<KeyValuePair<dynamic, dynamic>> values = new List<KeyValuePair<dynamic, dynamic>>();
                SqlConnection con = new SqlConnection(sCon);
                await con.OpenAsync();
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
                            iCommon = await GetCommonAsync(table, sColumn, sCon);
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
                                await cmd.ExecuteNonQueryAsync();
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
        public static async Task<bool> UpdateAsync(List<object> newDatas, List<object> oldDatas, string sTable, string sColumn, string sCon = null)
        {
            sCon = sCon ?? DBConnection.Connection;
            var newList = new List<Common>();
            var oldList = new List<Common>();
            newList = await GetIdListAsync(newDatas, sColumn);
            oldList = await GetIdListAsync(oldDatas, sColumn);
            try
            {
                foreach (Common item in oldList)
                {
                    bool included = newList.Any(x => x.id == item.id);
                    if (!included)
                    {
                        await DeleteAsync(sTable, sColumn, item.id, sCon);
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
                                        await InsertAsync(obj, sTable, sCon);
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
                                        await UpdateAsync(obj, sTable, sColumn, sVal.ToInt32(), sCon);
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
        public static bool BulkCopy(string sTable, DataTable dt, bool isIdentity = true, string sCon = null)
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                SqlBulkCopyOptions sqlBulk = isIdentity ? SqlBulkCopyOptions.KeepIdentity : SqlBulkCopyOptions.Default;
                using (SqlBulkCopy bulkData = new SqlBulkCopy(sCon, sqlBulk))
                {
                    bulkData.DestinationTableName = sTable;
                    bulkData.WriteToServer(dt);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return true;
        }
        public static dynamic GetSingleData(string Query, string sCon = null)
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection con = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(Query, con))
                    {
                        con.Open();
                        return cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static T GetObjectDetails<T>(string Query, string sCon = null) where T : new()
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection conn = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(Query, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        conn.Open();
                        var reader = cmd.ExecuteReader();
                        T list = ToSingle<T>(reader);
                        reader.Close();
                        reader.Dispose();
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static DataTable GetDataTable(string Query, string sCon = null)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(sCon))
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
        public static dynamic ValueFindMethod(string Query, string sCon = null)
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection con = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(Query, con))
                    {
                        con.Open();
                        return cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static List<T> GetList<T>(string commandText, string sCon = null) where T : new()
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection conn = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(commandText, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        conn.Open();
                        var reader = cmd.ExecuteReader();
                        List<T> list = ToList<T>(reader);
                        reader.Close();
                        reader.Dispose();
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static List<string> GetStringListMethod(string Query, string sCon = null)
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection conn = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(Query, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        conn.Open();
                        var reader = cmd.ExecuteReader();
                        List<string> list = ToList(reader);
                        reader.Close();
                        reader.Dispose();
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static bool DatabaseMethod(string Query, string sCon = null)
        {
            sCon = sCon ?? DBConnection.Connection;
            if (Query == string.Empty)
            {
                return false;
            }
            try
            {
                using (SqlConnection con = new SqlConnection(sCon))
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
        public static bool InsertToDatabase(List<object> datas, string table, string sCon = null)
        {
            try
            {
                foreach (object data in datas)
                {
                    InsertToDatabaseObj(data, table, sCon);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static bool InsertToDatabaseObj(object data, string table, string sCon = null)
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
                using (SqlConnection con = new SqlConnection(sCon))
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
                        EntityLoadMethod(data, cmd);
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
        public static bool UpdateToDatabase(List<object> datas, string table, string column, string sCon = null)
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
                    UpdateToDatabaseObj(data, table, column, iValue, sCon);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static bool UpdateToDatabaseObj(object data, string table, string column, int iValue, string sCon = null)
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
                using (SqlConnection con = new SqlConnection(sCon))
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
                        EntityLoadMethod(data, cmd);
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
        public static bool DeleteFromDatabase(string table, string column, int iValue, string sCon = null)
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                string Query = "Delete From  " + table + " Where " + column + " = @" + column + "";
                using (SqlConnection con = new SqlConnection(sCon))
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
        public static bool UpdateDatabase(DataTable datas, DataTable oldDatas, string table, string sCon = null)
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                DeleteOldItem(datas, oldDatas, table, sCon);
                bool result = false;
                string sValue = string.Empty;
                List<KeyValuePair<dynamic, dynamic>> values = new List<KeyValuePair<dynamic, dynamic>>();
                SqlConnection con = new SqlConnection(sCon);
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
                            iCommon = GetCommon(table, sColumn, sCon);
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
        public static bool UpdateDatabase(List<object> newDatas, List<object> oldDatas, string sTable, string sColumn, string sCon = null)
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
                        DeleteFromDatabase(sTable, sColumn, item.id, sCon);
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
                                        InsertToDatabaseObj(obj, sTable, sCon);
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
                                        UpdateToDatabaseObj(obj, sTable, sColumn, sVal.ToInt32(), sCon);
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
        private static async Task DeleteOldAsync(DataTable newDt, DataTable oldDt, string sTable, string sCon = null)
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
                        await DeleteAsync(sTable, sColumn, item.id, sCon);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        private static async Task<List<Common>> GetCommonAsync(string sTable, string sColumn, string sCon = null)
        {
            try
            {
                List<Common> iCommon = new List<Common>();
                var dt = await GetDataTableAsync("Select " + sColumn + " From " + sTable, sCon);
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
        private static async Task<T> GetItemAsync<T>(DataRow dr)
        {
            var value = await Task.Run<T>(() =>
            {
                return GetItem<T>(dr);
            });
            return value;
        }
        private static async Task<string> getInsertCommandAsync(string table, List<KeyValuePair<dynamic, dynamic>> values)
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
        private static string GetDate(DateTime dateTime)
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
        private static List<string> ToList(SqlDataReader dataReader)
        {
            List<string> res = new List<string>();
            while (dataReader.Read())
            {
                string t = null;

                for (int inc = 0; inc < dataReader.FieldCount; inc++)
                {
                    var val = dataReader.GetValue(inc);
                    if (val != DBNull.Value)
                    {
                        t = val.ToString();
                    }
                }
                res.Add(t);
            }
            return res;
        }
        private static List<T> ToList<T>(SqlDataReader dataReader) where T : new()
        {
            string sval = null;
            try
            {
                List<T> res = new List<T>();
                while (dataReader.Read())
                {
                    T t = new T();

                    for (int inc = 0; inc < dataReader.FieldCount; inc++)
                    {
                        Type type = t.GetType();
                        PropertyInfo prop = type.GetProperty(dataReader.GetName(inc));
                        var val = dataReader.GetValue(inc);
                        sval = val.ToString();
                        if (val != DBNull.Value)
                        {
                            try
                            {
                                prop.SetValue(t, val, null);
                            }
                            catch (Exception ex)
                            {
                                try
                                {
                                    string s = ex.Message + sval;
                                    switch (prop.PropertyType.Name)
                                    {
                                        case "String":
                                            prop.SetValue(t, sval, null);
                                            break;
                                        case "DateTime":
                                            prop.SetValue(t, sval.toDateTime(), null);
                                            break;
                                        case "Int64":
                                            prop.SetValue(t, sval.ToInt32(), null);
                                            break;
                                        case "Int32":
                                            prop.SetValue(t, sval.ToInt32(), null);
                                            break;
                                        case "Decimal":
                                            prop.SetValue(t, sval.ToDecimal(), null);
                                            break;
                                        case "Boolean":
                                            prop.SetValue(t, sval.ToBool(), null);
                                            break;
                                        case "Nullable`1":
                                            if (prop.PropertyType.FullName.Contains("System.Int32"))
                                            {
                                                if (sval != string.Empty)
                                                {
                                                    prop.SetValue(t, sval.ToInt32(), null);
                                                }
                                            }
                                            else if (prop.PropertyType.FullName.Contains("System.Boolean"))
                                            {
                                                if (sval != string.Empty)
                                                {
                                                    prop.SetValue(t, sval.ToBool(), null);
                                                }
                                            }
                                            else if (prop.PropertyType.FullName.Contains("System.DateTime"))
                                            {
                                                if (sval != string.Empty)
                                                {
                                                    prop.SetValue(t, sval.toDateTime(), null);
                                                }
                                            }
                                            else if (prop.PropertyType.FullName.Contains("System.Decimal"))
                                            {
                                                if (sval != string.Empty)
                                                {
                                                    prop.SetValue(t, sval.ToDecimal(), null);
                                                }
                                            }
                                            break;
                                        default:
                                            prop.SetValue(t, null, null);
                                            break;
                                    }
                                }
                                catch (Exception) { continue; }
                                continue;
                            }

                        }
                        else
                        {
                            prop.SetValue(t, null, null);
                        }
                    }
                    res.Add(t);
                }
                return res;
            }
            catch (Exception ex)
            {
                string s = ex.Message + sval;
                throw;
            }

        }
        private static T ToSingle<T>(SqlDataReader dataReader) where T : new()
        {
            T t = new T();
            while (dataReader.Read())
            {
                for (int inc = 0; inc < dataReader.FieldCount; inc++)
                {
                    Type type = t.GetType();
                    PropertyInfo prop = type.GetProperty(dataReader.GetName(inc));
                    var val = dataReader.GetValue(inc);
                    if (val != DBNull.Value)
                    {
                        prop.SetValue(t, val, null);
                    }
                }

            }
            return t;

        }
        private static void DeleteOldItem(DataTable newDt, DataTable oldDt, string sTable, string sCon = null)
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
                        DeleteFromDatabase(sTable, sColumn, item.id, sCon);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        private static List<Common> GetCommon(string sTable, string sColumn, string sCon = null)
        {
            try
            {
                List<Common> iCommon = new List<Common>();
                var dt = GetDataTable("Select " + sColumn + " From " + sTable, sCon);
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
        public static T GetItem<T>(DataRow dr)
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
                                switch (pro.PropertyType.Name)
                                {
                                    case "Boolean":
                                        if (dr[column.ColumnName].ToString() != string.Empty)
                                            pro.SetValue(obj, dr[column.ColumnName].ToString().ToBool(), null);
                                        break;
                                    case "Int32":
                                        if (dr[column.ColumnName].ToString() != string.Empty)
                                        {
                                            pro.SetValue(obj, dr[column.ColumnName].ToInt32(), null);
                                        }
                                        break;
                                    case "Decimal":
                                        if (dr[column.ColumnName].ToString() != string.Empty)
                                            pro.SetValue(obj, dr[column.ColumnName].toDecimal(), null);
                                        break;
                                    case "DateTime":
                                        if (dr[column.ColumnName].ToString() != string.Empty)
                                            pro.SetValue(obj, Convert.ToDateTime(dr[column.ColumnName].ToString()), null);
                                        break;
                                    case "Byte[]":
                                        pro.SetValue(obj, (byte[])dr[column.ColumnName], null);
                                        break;
                                    case "Nullable`1":
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
                                                pro.SetValue(obj, dr[column.ColumnName].toDecimal(), null);
                                            }
                                        }
                                        break;
                                    default:
                                        pro.SetValue(obj, dr[column.ColumnName].ToString(), null);
                                        break;
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
        public static void EntityLoadMethod(object entity, SqlCommand cmd)
        {
            foreach (var item in entity.GetType().GetProperties())
            {
                if (item.GetValue(entity, null) != null)
                {
                    if (item.PropertyType.Name == "Nullable`1" && item.GetValue(entity, null).ToString() == "0")
                    {
                        continue;
                    }
                    switch (item.PropertyType.Name)
                    {
                        case "Byte[]":
                            cmd.Parameters.AddWithValue(item.Name, (byte[])(item.GetValue(entity, null)));
                            break;
                        case "DateTime":
                            cmd.Parameters.AddWithValue("@" + item.Name, GetDate((DateTime)(item.GetValue(entity, null))));
                            break;
                        default:
                            cmd.Parameters.AddWithValue(item.Name, item.GetValue(entity, null).ToString());
                            break;
                    }
                }
            }
        }
        #endregion

        #endregion

        #region Public Method BySP

        #region Normal
        public static bool InsertMethod_SP(List<object> entities, string sStoredProceedure, string sCon = null)
        {
            bool result = false;
            try
            {
                foreach (object data in entities)
                {
                    InsertMethod_SP(data, sStoredProceedure, sCon);
                }
                result = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }
        public static bool InsertMethod_SP(object entity, string sStoredProceedure, string sCon = null)
        {
            bool result = false;
            sCon = sCon ?? DBConnection.Connection;
            using (SqlConnection con = new SqlConnection(sCon))
            {
                using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    try
                    {
                        EntityLoadMethod(entity, cmd);
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
        public static DBOutput InsertMethod_SP(object entity, DataSet ds, string sStoredProceedure, object Secondentity = null, string sCon = null)
        {
            DBOutput dBOutput = new DBOutput();
            sCon = sCon ?? DBConnection.Connection;
            using (SqlConnection con = new SqlConnection(sCon))
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
                            EntityLoadMethod(entity, cmd);
                        }
                        if (Secondentity != null)
                        {
                            EntityLoadMethod(Secondentity, cmd);
                        }
                        cmd.Parameters.Add("@return", SqlDbType.Int);
                        cmd.Parameters.Add("@errMessage", SqlDbType.Char, 500);
                        cmd.Parameters["@return"].Direction = ParameterDirection.Output;
                        cmd.Parameters["@errMessage"].Direction = ParameterDirection.Output;
                        con.Open();
                        cmd.CommandTimeout = 0;
                        cmd.ExecuteNonQuery();
                        dBOutput.Message = cmd.Parameters["@errMessage"].Value.ToString2();
                        dBOutput.Value = cmd.Parameters["@return"].Value.ToIntiger();
                    }
                    catch (Exception ex)
                    {
                        dBOutput.Message = ex.Message;
                        dBOutput.Value = null;
                    }
                }
            }
            return dBOutput;
        }
        public static bool UpdateMethod_SP(object entity, string sStoredProceedure, string sCon = null)
        {
            bool result = false;
            int numRes = 0;
            sCon = sCon ?? DBConnection.Connection;
            using (SqlConnection con = new SqlConnection(sCon))
            {
                using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    try
                    {
                        EntityLoadMethod(entity, cmd);
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
        public static bool UpdateMethod_SP(List<object> entities, string sStoredProceedure, string sCon = null)
        {
            try
            {
                foreach (object data in entities)
                {
                    UpdateMethod_SP(data, sStoredProceedure, sCon);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static bool UpdateMethod_SP(List<object> newDatas, List<object> oldDatas, string sTable, string sColumn, string sCon = null)
        {
            var newList = GetIdList(newDatas, sColumn);
            var oldList = GetIdList(oldDatas, sColumn);
            try
            {
                if (oldList.Count > 0)
                {
                    foreach (Common item in oldList)
                    {
                        bool included = newList.Any(x => x.id == item.id);
                        if (!included)
                        {
                            DeleteMethod_SP(item.id, "spDelete" + sTable, sCon);
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
                                        InsertMethod_SP(obj, "spInsert" + sTable, sCon);
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
                                        UpdateMethod_SP(obj, "spUpdate" + sTable, sCon);
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
        public static bool DeleteMethod_SP(object entity, string sStoredProceedure, string sCon = null)
        {
            bool result = false;
            int numRes = 0;
            sCon = sCon ?? DBConnection.Connection;
            using (SqlConnection con = new SqlConnection(sCon))
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
        public static DataTable GetDataTable_SP(string sStoredProceedure, string sCon = null)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(sCon))
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
        public static DataTable GetDataTableWithIdParameter_SP(string sStoredProceedure, string Value, string sCon = null)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(sCon))
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
        public static DataTable GetDataTableWithIdParameter_SP(string sStoredProceedure, object entity, string sCon = null)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(sCon))
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
        public static List<T> GetList_SP<T>(string sStoredProceedure, string sCon = null) where T : new()
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection conn = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(sStoredProceedure, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        conn.Open();
                        var reader = cmd.ExecuteReader();
                        List<T> list = ToList<T>(reader);
                        reader.Close();
                        reader.Dispose();
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static List<string> GetList_SP(string commandText, object entity, string sCon = null)
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection conn = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(commandText, conn))
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
                        conn.Open();
                        var reader = cmd.ExecuteReader();
                        List<string> list = ToList(reader);
                        reader.Close();
                        reader.Dispose();
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static List<T> GetList_SP<T>(string sStoredProceedure, string Value, string sCon = null) where T : new()
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection conn = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(sStoredProceedure, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Id", Value);
                        conn.Open();
                        var reader = cmd.ExecuteReader();
                        List<T> list = ToList<T>(reader);
                        reader.Close();
                        reader.Dispose();
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static List<T> GetList_SP<T>(string commandText, object entity, string sCon = null) where T : new()
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection conn = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(commandText, conn))
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
                        conn.Open();
                        var reader = cmd.ExecuteReader();
                        List<T> list = ToList<T>(reader);
                        reader.Close();
                        reader.Dispose();
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static T GetObject_SP<T>(string sStoredProceedure, string Value, string sCon = null) where T : new()
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection conn = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(sStoredProceedure, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Id", Value);
                        conn.Open();
                        var reader = cmd.ExecuteReader();
                        T list = ToSingle<T>(reader);
                        reader.Close();
                        reader.Dispose();
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static T GetObjectWithparameter_SP<T>(string sStoredProceedure, object entity, string sCon = null) where T : new()
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection conn = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(sStoredProceedure, conn))
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
                        conn.Open();
                        var reader = cmd.ExecuteReader();
                        T list = ToSingle<T>(reader);
                        reader.Close();
                        reader.Dispose();
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static T GetObject_SP<T>(string sStoredProceedure, string sCon = null) where T : new()
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection conn = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(sStoredProceedure, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        conn.Open();
                        var reader = cmd.ExecuteReader();
                        T list = ToSingle<T>(reader);
                        reader.Close();
                        reader.Dispose();
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static dynamic GetData_SP(string sStoredProceedure, string sCon = null)
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection con = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        con.Open();
                        return cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static dynamic GetDataWithParameter_SP(string sStoredProceedure, object entity, string sCon = null)
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection con = new SqlConnection(sCon))
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
                        con.Open();
                        return cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static bool DatabaseExecution_SP(string sStoredProceedure, object entity = null, string sCon = null)
        {
            sCon = sCon ?? DBConnection.Connection;
            bool result = false;
            using (SqlConnection con = new SqlConnection(sCon))
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
        public static DataSet GetDataSet_SP(string sStoredProceedure, object entity, string sCon = null)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
            DataSet ds = new DataSet();
            sCon = sCon ?? DBConnection.Connection;
            try
            {
                using (SqlConnection con = new SqlConnection(sCon))
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
        public static async Task<bool> InsertAsync_SP(List<object> entities, string sStoredProceedure, string sCon = null)
        {
            try
            {
                foreach (object data in entities)
                {
                    await InsertAsync_SP(data, sStoredProceedure, sCon);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<bool> InsertAsync_SP(object entity, string sStoredProceedure, string sCon = null)
        {
            try
            {
                bool result = false;
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection con = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            EntityLoadMethod(entity, cmd);
                            await con.OpenAsync();
                            int numRes = await cmd.ExecuteNonQueryAsync();
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
        }
        public static async Task<DBOutput> InsertAsync_SP(object entity, DataSet ds, string sStoredProceedure, object Secondentity = null, bool isSimple = false, string sCon = null)
        {
            DBOutput dBOutput = new DBOutput();
            sCon = sCon ?? DBConnection.Connection;
            try
            {
                using (SqlConnection con = new SqlConnection(sCon))
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
                                EntityLoadMethod(entity, cmd);
                            }
                            if (Secondentity != null)
                            {
                                EntityLoadMethod(Secondentity, cmd);
                            }
                            if (!isSimple)
                            {
                                cmd.Parameters.Add("@return", SqlDbType.Int);
                                cmd.Parameters.Add("@errMessage", SqlDbType.Char, 500);
                                cmd.Parameters["@return"].Direction = ParameterDirection.Output;
                                cmd.Parameters["@errMessage"].Direction = ParameterDirection.Output;
                            }
                            await con.OpenAsync();
                            cmd.CommandTimeout = 0;
                            await cmd.ExecuteNonQueryAsync();
                            if (!isSimple)
                            {
                                dBOutput.Message = cmd.Parameters["@errMessage"].Value.ToString2();
                                dBOutput.Value = cmd.Parameters["@return"].Value.ToIntiger();
                            }
                            else
                            {
                                dBOutput.Value = 1;
                                dBOutput.Message = string.Empty;
                            }
                        }
                        catch (Exception ex)
                        {
                            dBOutput.Message = ex.Message;
                            dBOutput.Value = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                dBOutput.Message = ex.Message;
                dBOutput.Value = null;
            }
            return dBOutput;
        }
        public static async Task<bool> UpdateAsync_SP(object entity, string sStoredProceedure, string sCon = null)
        {
            try
            {
                bool result = false;
                int numRes = 0;
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection con = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            EntityLoadMethod(entity, cmd);
                            await con.OpenAsync();
                            numRes = await cmd.ExecuteNonQueryAsync();
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
        }
        public static async Task<bool> UpdateAsync_SP(List<object> entities, string sStoredProceedure, string sCon = null)
        {
            try
            {
                foreach (object data in entities)
                {
                    await UpdateAsync_SP(data, sStoredProceedure, sCon);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<bool> UpdateAsync_SP(List<object> newDatas, List<object> oldDatas, string sTable, string sColumn, string sCon = null)
        {
            var newList = await GetIdListAsync(newDatas, sColumn);
            var oldList = await GetIdListAsync(oldDatas, sColumn);
            try
            {
                if (oldList.Count > 0)
                {
                    foreach (Common item in oldList)
                    {
                        bool included = newList.Any(x => x.id == item.id);
                        if (!included)
                        {
                            await DeleteAsync_SP(item.id, "spDelete" + sTable, sCon);
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
                                        await InsertAsync_SP(obj, "spInsert" + sTable, sCon);
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
                                        await UpdateAsync_SP(obj, "spUpdate" + sTable, sCon);
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
        public static async Task<bool> DeleteAsync_SP(object entity, string sStoredProceedure, string sCon = null)
        {
            try
            {
                bool result = false;
                int numRes = 0;
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection con = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            cmd.Parameters.AddWithValue("Id", entity.ToInt32());
                            await con.OpenAsync();
                            numRes = await cmd.ExecuteNonQueryAsync();
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

        }
        public static async Task<DataTable> GetDataTableAsync_SP(string sStoredProceedure, string sCon = null)
        {
            var value = await Task<DataTable>.Factory.StartNew(() =>
            {
                try
                {
                    sCon = sCon ?? DBConnection.Connection;
                    System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
                    DataTable dt = new DataTable();
                    using (SqlConnection con = new SqlConnection(sCon))
                    {
                        using (SqlCommand cmd = new SqlCommand())
                        {
                            cmd.Connection = con;
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.CommandText = sStoredProceedure;
                            using (SqlDataAdapter sdr = new SqlDataAdapter(cmd))
                            {
                                sdr.Fill(dt);
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
        public static async Task<DataTable> GetDataTableWithIdParameterAsync_SP(string sStoredProceedure, string Value, string sCon = null)
        {
            var value = await Task<DataTable>.Factory.StartNew(() =>
            {
                try
                {
                    System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
                    DataTable dt = new DataTable();
                    sCon = sCon ?? DBConnection.Connection;
                    using (SqlConnection con = new SqlConnection(sCon))
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
        public static async Task<DataTable> GetDataTableWithIdParameterAsync_SP(string sStoredProceedure, object entity, string sCon = null)
        {
            var value = await Task<DataTable>.Factory.StartNew(() =>
            {
                try
                {
                    System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
                    DataTable dt = new DataTable();
                    sCon = sCon ?? DBConnection.Connection;
                    using (SqlConnection con = new SqlConnection(sCon))
                    {
                        using (SqlCommand cmd = new SqlCommand())
                        {
                            cmd.Connection = con;
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
                            cmd.CommandText = sStoredProceedure;
                            cmd.CommandTimeout = 0;
                            cmd.CommandType = CommandType.StoredProcedure;
                            using (SqlDataAdapter sdr = new SqlDataAdapter(cmd))
                            {
                                sdr.Fill(dt);
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
        public static async Task<List<T>> GetListAsync_SP<T>(string commandText, string sCon = null) where T : new()
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection conn = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(commandText, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        await conn.OpenAsync();
                        var reader = await cmd.ExecuteReaderAsync();
                        List<T> list = ToList<T>(reader);
                        reader.Close();
                        reader.Dispose();
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<List<string>> GetStringListAsync_SP(string sStoredProceedure, string sCon = null)
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection conn = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(sStoredProceedure, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        await conn.OpenAsync();
                        var reader = await cmd.ExecuteReaderAsync();
                        var list = ToList(reader);
                        reader.Close();
                        reader.Dispose();
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<List<T>> GetListAsync_SP<T>(string sStoredProceedure, string Value, string sCon = null) where T : new()
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection conn = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(sStoredProceedure, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Id", Value);
                        await conn.OpenAsync();
                        var reader = await cmd.ExecuteReaderAsync();
                        List<T> list = ToList<T>(reader);
                        reader.Close();
                        reader.Dispose();
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<List<T>> GetListAsync_SP<T>(string commandText, object entity, string sCon = null) where T : new()
        {
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection conn = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(commandText, conn))
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
                        await conn.OpenAsync();
                        var reader = await cmd.ExecuteReaderAsync();
                        List<T> list = ToList<T>(reader);
                        reader.Close();
                        reader.Dispose();
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<T> GetAsync_SP<T>(string sStoredProceedure, string Value, string sCon = null) where T : new()
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection conn = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(sStoredProceedure, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Id", Value);
                        await conn.OpenAsync();
                        var reader = await cmd.ExecuteReaderAsync();
                        T list = ToSingle<T>(reader);
                        reader.Close();
                        reader.Dispose();
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<T> GetAsyncWithparameter_SP<T>(string sStoredProceedure, object entity, string sCon = null) where T : new()
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection conn = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(sStoredProceedure, conn))
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
                        await conn.OpenAsync();
                        var reader = await cmd.ExecuteReaderAsync();
                        T list = ToSingle<T>(reader);
                        reader.Close();
                        reader.Dispose();
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<T> GetAsync_SP<T>(string sStoredProceedure, string sCon = null) where T : new()
        {
            try
            {
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection conn = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(sStoredProceedure, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        await conn.OpenAsync();
                        var reader = await cmd.ExecuteReaderAsync();
                        T list = ToSingle<T>(reader);
                        reader.Close();
                        reader.Dispose();
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<dynamic> GetDataAsync_SP(string sStoredProceedure, string sCon = null)
        {
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection con = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        await con.OpenAsync();
                        return (await cmd.ExecuteScalarAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<dynamic> GetDataWithParameterAsync_SP(string sStoredProceedure, object entity, string sCon = null)
        {
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection con = new SqlConnection(sCon))
                {
                    using (SqlCommand cmd = new SqlCommand(sStoredProceedure, con))
                    {
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
                        cmd.CommandType = CommandType.StoredProcedure;
                        await con.OpenAsync();
                        return (await cmd.ExecuteScalarAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task<bool> DatabaseExecutionAsync_SP(string sStoredProceedure, Object entity = null, string sCon = null)
        {
            try
            {
                bool result = false;
                sCon = sCon ?? DBConnection.Connection;
                using (SqlConnection con = new SqlConnection(sCon))
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
                            await con.OpenAsync();
                            int numRes = await cmd.ExecuteNonQueryAsync();
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
        }
        public static async Task<DataSet> GetDataSetAsync_SP(string sStoredProceedure, object entity, string sCon = null)
        {
            var value = await Task<DataSet>.Factory.StartNew(() =>
            {
                try
                {
                    System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
                    DataSet ds = new DataSet();
                    sCon = sCon ?? DBConnection.Connection;
                    using (SqlConnection con = new SqlConnection(sCon))
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
