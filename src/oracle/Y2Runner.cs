/*
 * 🆈2🆁🆄🅽🅽🅴🆁.cs - An ADO.NET SQL helper for Oracle.                         
 *                                                                             
 * Version 1.0                                                                 
 * By Alberto Iong on 2022/08/04                                               
 *                                                                             
 * This library is free software; you can redistribute it and/or modify it     
 * under the terms of the GNU Lesser General Public License as published by    
 * the Free Software Foundation; either version 2 of the License, or (at your  
 * option) any later version.                                                  
 *                                                                             
 * This library is distributed in the hope that it will be useful, but WITHOUT 
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or       
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public        
 * License for more details.                                                   
 */
//#undef DEBUG    // <--- Make sure 'DEBUG' is undefined in production environment. 

using System;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Diagnostics;
using System.Web;
using System.Text;
using System.Security.Cryptography;
using ServiceStack.Redis;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading;

public partial class Y2Runner : IDisposable
{
    // Oracle 
    private OracleConnection conn;
    private OracleTransaction tran;
    private OracleCommand cmd;
    private int pendingTrans;

    // Redis 
    private const int DEFAULT_TTL = 60;
    private const int MAX_CACHE_TAGS = 24; 
    private RedisClient redis = null;

    private const string DEFAULT_ONERROR_URI = "~/Content/busy.html?m={0}";
    private string OnErrorURI = "";
    public string message;

#region Oracle
    public DataTable RunSelectSQL(string CommandText, int ttl = DEFAULT_TTL, string[] CacheTags=null)
    {
        DataTable ret = new DataTable();
        string HashedKey = ComputeSha256Hash(CommandText);

#if DEBUG
        Stopwatch stopwatch = new Stopwatch();
        long elapsed_time;
        stopwatch.Start();

        Debug.WriteLine("Y2Runner.RunSelectSQL: " + CommandText);
#endif
        // With redis
        if ((redis != null) && (redis.GetValue(HashedKey) != null))
        {
            // Hit! Load from cache...
#if DEBUG
            Debug.WriteLine(String.Format("\"{0}\" hit! load from cache...", HashedKey));
#endif
            DataSet dataSet = JsonConvert.DeserializeObject<DataSet>(redis.GetValue(HashedKey));
            ret = dataSet.Tables["Table1"];

#if DEBUG
            stopwatch.Stop();
            elapsed_time = stopwatch.ElapsedMilliseconds;
            Debug.WriteLine(String.Format("{0} milliseconds elapsed.", elapsed_time));
#endif
            return ret; 
        }

        // Execute SQL
        cmd.CommandText = CommandText;
        ret.Load(cmd.ExecuteReader());

#if DEBUG
        Thread.Sleep(1000);     // wait 1000 ms 
#endif
        // With redis and ttl > 0
        if ((redis != null) && (ttl > 0))
        {
            // Missed! Add to cache...
#if DEBUG
            Debug.WriteLine(String.Format("\"{0}\" missed! Add to cache...", HashedKey));
#endif
            string JSONValue = JsonConvert.SerializeObject(ret);
            redis.SetValue(HashedKey, String.Format("{{\"Table1\": {0} }}", JSONValue), new TimeSpan(0, 0, ttl));

            if (CacheTags == null)
                CacheTags = ParseCacheTagsfromSelectSQL(CommandText);
            foreach (string CacheTag in CacheTags)
                if (!String.IsNullOrEmpty(CacheTag))
                {
                    redis.SAdd(CacheTag, Encoding.ASCII.GetBytes(HashedKey));
                    redis.Expire(CacheTag, ttl);
                }
        }

#if DEBUG
        stopwatch.Stop();
        elapsed_time = stopwatch.ElapsedMilliseconds;
        Debug.WriteLine(String.Format("{0} milliseconds elapsed.", elapsed_time));
#endif
        return ret; 
    }

    public object RunValueSQL(string CommandText)
    {
        object ret;

#if DEBUG
        Stopwatch stopwatch = new Stopwatch();
        long elapsed_time;
        stopwatch.Start();

        Debug.WriteLine("Y2Runner.RunValueSQL: " + CommandText);
#endif
        cmd.CommandText = CommandText;
        ret = cmd.ExecuteScalar();

#if DEBUG
        stopwatch.Stop();
        elapsed_time = stopwatch.ElapsedMilliseconds;
        Debug.WriteLine(String.Format("{0} milliseconds elapsed.", elapsed_time));
#endif
        return ret;
    }

    public bool RunSQL(string CommandText, string[] CacheTags = null)
    {
        int rows_affected = 0;
        bool ret = true;

#if DEBUG
        Stopwatch stopwatch = new Stopwatch();
        long elapsed_time;
        stopwatch.Start();

        Debug.WriteLine("Y2Runner.RunSQL: " + CommandText);
#endif
        cmd.CommandText = CommandText;
        try
        {
            rows_affected = cmd.ExecuteNonQuery();

#if DEBUG
            Debug.WriteLine(String.Format("Y2Runner.RunSQL.rows_affected: {0}", rows_affected));
#endif
            // Increase pending transaction by 1 
            pendingTrans++;

            if (redis !=null)
            {
                if (CacheTags == null)
                    CacheTags = ParseCacheTagsfromSQL(CommandText);
                foreach (string CacheTag in CacheTags)
                    if (!String.IsNullOrEmpty(CacheTag)) RemoveFromCache(CacheTag);
            }
        }
        catch (OracleException ex)
        {
            message = message + ">An error has occurred while running SQL statement (Y2Runner.RunSQL).<br />";
            message = message + ">" + ex.Message + "<br />";
            message = message + ">" + ex.Source + "<br />";
            message = message + ">SQL statement: " + CommandText + "<br />";
            ret = false;
        }

#if DEBUG
        stopwatch.Stop();
        elapsed_time = stopwatch.ElapsedMilliseconds;
        Debug.WriteLine(String.Format("{0} milliseconds elapsed.", elapsed_time));

        Debug.WriteLine(String.Format("Y2Runner.RunSQL.message: {0}", message));
        Debug.WriteLine(String.Format("Y2Runner.RunSQL: {0} transaction{1} pending.", pendingTrans, (pendingTrans == 1 ? "" : "s")));
#endif
        return ret;
    }

    public int RunInsertSQLYieldRowID(string CommandText, string rowid_name = "id", string[] CacheTags = null)
    {
        int row_id = 0;
        int rows_affected = 0;            
        string sql_stub = " returning {0} into :temp_id";
        OracleParameter outputParameter = new OracleParameter("temp_id", OracleDbType.Decimal);
        outputParameter.Direction = ParameterDirection.Output;

        cmd.CommandText = CommandText + String.Format(sql_stub, rowid_name);

#if DEBUG
        Stopwatch stopwatch = new Stopwatch();
        long elapsed_time;
        stopwatch.Start();
            
        Debug.WriteLine("Y2Runner.RunInsertSQLYieldRowID: " + cmd.CommandText);
#endif
        cmd.Parameters.Add(outputParameter);
        try
        {
            rows_affected = cmd.ExecuteNonQuery();
            
#if DEBUG
            Debug.WriteLine(String.Format("Y2Runner.RunInsertSQLYieldRowID.rows_affected: {0}", rows_affected));
#endif
            row_id = Int32.Parse(outputParameter.Value.ToString());
            cmd.Parameters.Remove(outputParameter);

            // Increase pending transaction by 1 
            pendingTrans++;

            if (redis != null)
            {
                if (CacheTags == null)
                    CacheTags = ParseCacheTagsfromSQL(CommandText);
                foreach (string CacheTag in CacheTags)
                    if (!String.IsNullOrEmpty(CacheTag)) RemoveFromCache(CacheTag);
            }
        }
        catch (OracleException ex)
        {
            message = message + ">An error has occurred while running SQL statement (Y2Runner.RunInsertSQLYieldRowID).<br />";
            message = message + ">" + ex.Message + "<br />";
            message = message + ">" + ex.Source + "<br />";
            message = message + ">SQL statement: " + cmd.CommandText + "<br />";
            row_id = -1;
        }

#if DEBUG
        stopwatch.Stop();
        elapsed_time = stopwatch.ElapsedMilliseconds;
        Debug.WriteLine(String.Format("{0} milliseconds elapsed.", elapsed_time));

        Debug.WriteLine(String.Format("Y2Runner.RunInsertSQLYieldRowID.message: {0}", message));
        Debug.WriteLine(String.Format("Y2Runner.RunInsertSQLYieldRowID: {0} transaction{1} pending.", pendingTrans, (pendingTrans == 1 ? "" : "s")));
#endif
        return row_id; 
    }

    public void Commit()
    {
#if DEBUG
        Debug.WriteLine(String.Format("Y2Runner.Commit: {0} transaction{1} will be committed.", pendingTrans, (pendingTrans == 1 ? "" : "s")));
#endif
        tran.Commit();

        // Start a local transaction
        tran = conn.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
        // Assign transaction object for a pending local transaction
        cmd.Transaction = tran;

        // Set pending transaction to zero
        pendingTrans = 0;
    }

    public void Rollback()
    {
#if DEBUG
        Debug.WriteLine(String.Format("Y2Runner.Rollback: {0} transaction{1} will be rollbacked.", pendingTrans, (pendingTrans == 1 ? "" : "s")));
#endif
        tran.Rollback();

        // Start a local transaction
        tran = conn.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
        // Assign transaction object for a pending local transaction
        cmd.Transaction = tran;

        // Set pending transaction to zero
        pendingTrans = 0;
    }
#endregion

#region Initialization and cleanup
    public Y2Runner(OracleConnection conn, RedisClient redis = null, string OnErrorURI = "")
    {
        this.conn = conn;
        this.redis = redis;
        this.OnErrorURI = OnErrorURI; 

        try
        {
            // OracleConnection.BeginTransaction Method
            // https://docs.microsoft.com/en-us/dotnet/api/system.data.oracleclient.oracleconnection.begintransaction?view=netframework-4.7.2
            this.conn.Open();
            this.cmd = conn.CreateCommand();

            // Start a local transaction
            this.tran = conn.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
            // Assign transaction object for a pending local transaction
            this.cmd.Transaction = tran;

            // Set pending transaction to zero
            this.pendingTrans = 0;
        }
        catch (Exception e)
        {
            if (this.OnErrorURI != "")
            {
                HttpContext.Current.Response.Redirect(String.Format(OnErrorURI, HttpContext.Current.Server.UrlEncode(e.ToString())));
            }
            else
            {
                // 💀「死亡，也許沒有你想像的那樣可怕‧‧‧」
                throw e; 
            }
        }
    }

    public void Dispose()
    {
#if DEBUG
        Debug.WriteLine(String.Format("Y2Runner.Dispose()"));
        Debug.WriteLine(String.Format("Y2Runner: {0} transaction{1} pending.", pendingTrans, (pendingTrans == 1 ? "" : "s")));
#endif
        cmd.Dispose();
    }
#endregion

#region Redis 
    //public void AddToCache(string CacheKey, string CacheValue, string CacheTag="", int ttl = DEFAULT_TTL)
    //{
    //    redis.SetValue(CacheKey, String.Format("{{\"Table1\": {0} }}", CacheValue), new TimeSpan(0, 0, ttl));
    //    if (CacheTag != "")
    //    {
    //        // Converting string to byte array in C#
    //        // https://stackoverflow.com/questions/16072709/converting-string-to-byte-array-in-c-sharp
    //        redis.SAdd(CacheTag, Encoding.ASCII.GetBytes(CacheKey));
    //        redis.Expire(CacheTag, ttl);
    //    }
    //}

    public void RemoveFromCache(string CacheTag)
    {
        byte[][] smembers = redis.SMembers(CacheTag);
        foreach (byte[] bytes in smembers)
        {
            // How to convert byte array to string [duplicate]
            // https://stackoverflow.com/questions/11654562/how-to-convert-byte-array-to-string
            string member = Encoding.Default.GetString(bytes);
            redis.Del(member);
        }
        redis.Del(CacheTag);
    }

    // Compute SHA256 Hash In C#
    // https://www.c-sharpcorner.com/article/compute-sha256-hash-in-c-sharp/
    protected string ComputeSha256Hash(string rawData)
    {
        // Create a SHA256   
        using (SHA256 sha256Hash = SHA256.Create())
        {
            // ComputeHash - returns byte array  
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

            // Convert byte array to a string   
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
#endregion

#region Helpers
    //select * from (select abc from xyz where a=b) a f1, b f2 where f1.key=f2.key
    public string[] ParseCacheTagsfromSelectSQL(string CommandText)
    {
        //#if DEBUG
        //    Debug.WriteLine(String.Format("CommandText=[{0}]", CommandText));            
        //#endif

        // 1. Add space before and after , ( and ) to ease subsequent parsing.
        CommandText = CommandText.Replace(",", " , ").
                                  Replace("(", " ( ").
                                  Replace(")", " ) ");
        // 2. Only ONE space is allowed between tokens. 
        string[] tokens = Regex.Replace(CommandText, @"\s+", " ").
                                Split(new[] { " " }, StringSplitOptions.None);

        string[] result = new string[MAX_CACHE_TAGS];
        int i = 0;  // Number of CacheTags
        bool FromMet = false;
        bool FromUsed = false;
        bool JoinMet = false;

        // Loop through and parse each tokens
        foreach (string token in tokens)
        {
            //#if DEBUG
            //    Debug.WriteLine(String.Format("token=[{0}], FromMet={1}, FromUsed={2}, JoinMet={3}, i={4}", token, FromMet, FromUsed, JoinMet, i));
            //#endif

            switch (token.ToLower().Trim())
            {
                case "from":
                    FromMet = true;
                    FromUsed = false;
                    break;
                case ",":
                    if (FromMet && FromUsed)
                        FromUsed = false;
                    break;
                case "join":
                    JoinMet = true;
                    break;
                case "(":
                    FromMet = false;
                    FromUsed = true;
                    JoinMet = false;
                    break;
                default:                    
                    if (JoinMet)
                    {
                        result[i++] = token;
                        JoinMet = false;
                    }
                    else if (FromMet && !FromUsed)
                    {
                        result[i++] = token;
                        FromUsed = true;
                    }
                    break;
            }
        }
        // How to remove duplicate values from an array in C#
        // https://www.tutorialsteacher.com/articles/remove-duplicate-values-from-array-in-csharp
        return result.Distinct().ToArray();
    }

    public string[] ParseCacheTagsfromSQL(string CommandText)
    {
        string[] result = new string[MAX_CACHE_TAGS];
        int i = 0;
        Regex regex = new Regex(@"(from|join|into|update)\s+(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        Match m = regex.Match(CommandText);

        while (m.Success)
        {
            Group g = m.Groups[2];
            result[i++] = g.ToString();
            m = m.NextMatch();
        }
        // How to remove duplicate values from an array in C#
        // https://www.tutorialsteacher.com/articles/remove-duplicate-values-from-array-in-csharp
        return result;
    }

    public string EscapeSingleQuote(string s)
    {
        if (s == "")
            return " ";
        else
            return s.Replace("'", "''");
    }

    public string DataTableToString(DataTable dt, int index = 0, string delimiter = ", ")
    {
        string ret = "";

        if ((index < 0) | (index >= dt.Columns.Count))
            index = 0;

        foreach (DataRow r in dt.Rows)
        {
            if ((ret != ""))
                ret = ret + delimiter;
            ret = ret + r[index].ToString().Trim();
        }

        return ret;
    }
#endregion
}