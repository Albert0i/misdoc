# Caching SQL results with Redis, a strategic approach.

![Go ahead. Make my day.](img/go-ahead-make-my-day-quote-1.jpg)

## I. Introduction
Performance is costly, costs more than you can think, ie. knowledge and imagination. Previously, we've slightly tackled client/server performance issues, ie. **Ajax** as front-end enhancement to minimize unnecessary postbacks; **Nginx** as reverse proxy caching Web Server static files to speed up page loading. In thie article, we explore into the last realm of backend database. 

No matter what brand of SQL servers you used, execution of SQL statements is always most time consuming in request/response cycle. Instead of investing on SQL servers, one way to speed up is to invest on a **Redis** server and employing a *stategic caching policy*, so that the same data doesn't need another trip to database; At some point, cache needs to be invalidated by some events, since cache is fast but tiny, whereas SQL Server is slow but huge. By mixing both merits, this article serves as preliminary study on *feasibility* of caching SQL results with Redis. 

>Being a luxury things, performance, not only means money. Luxury relates more to lifestyle and taste, whereas money only relates to tycoon(大款).


## II. [SQL Joins](https://www.w3schools.com/sql/sql_join.asp)
A JOIN clause is used to combine rows from two or more tables, based on a related column between them. Let's look at a selection from the "Orders" table:

```sql
SELECT OrderID, CustomerID, OrderDate
FROM Orders;
```
And selects records that have matching values in both tables:

```sql
SELECT Orders.OrderID, Customers.CustomerName, Orders.OrderDate
FROM Orders
INNER JOIN Customers 
ON Orders.CustomerID=Customers.CustomerID;
```
Relational database system are complicated softwares, they twist disk files into 
*logical* tables, so that further match, group and order operations are possible. 
What happens behind [SQL Processing](https://docs.oracle.com/database/121/TGSQL/tgsql_sqlproc.htm#TGSQL175) involves SQL Parsing, Syntax Check, Semantic Check, Optimization, and Execution. SQL Servers have intrinsic caching policy on their own. 


## III. [“To cache, or not to cache: that is the policy!”](https://www.goodreads.com/quotes/36560-to-be-or-not-to-be-that-is-the-question)
```c#
// Oracle 
OracleConnection conn = new OracleConnection(ConnectionString);
// Redis
RedisClient redis = new RedisClient(Host, Port);
...
Y2Runner yr = new Y2Runner(conn, redis);
/* 
   Create a Y2Runner instance
   public Y2Runner(OracleConnection conn, RedisClient redis = null, string OnErrorURI = "")
*/
// (a) no cache
Y2Runner yr = new Y2Runner(conn);

// (b) with Cache
Y2Runner yr = new Y2Runner(conn, redis);

// (b) with Cache and redirect to URI when error happen
Y2Runner yr = new Y2Runner(conn, redis, "~/Content/busy.html?m={0}");

/* 
   Run SQL statement and returns a datatable result
   public DataTable RunSelectSQL(string CommandText, int ttl = DEFAULT_TTL, string[] CacheTags=null)
*/
// (a) ttl=60, all filenames in SQLText will be used as CacheTags.
DataTable SomeTable = yr.RunSelectSQL(SQLText);

// (b) ttl=120, all filenames in SQLText will be used as CacheTags.
DataTable SomeTable = yr.RunSelectSQL(SQLText, 120);

// (c) ttl=120, Only "Orders" will be used as CacheTag.
DataTable SomeTable = yr.RunSelectSQL(SQLText, 120, new string[] {"Orders"});

// (d) ttl=120, Only "Orders" and "Customers" will be used as CacheTags.
DataTable SomeTable = yr.RunSelectSQL(SQLText, 120, new string[] {"Orders", "Customers"});

// (e) Do not cache the result, all cacheTags will be discarded if specified. 
DataTable SomeTable = yr.RunSelectSQL(SQLText, 0);
```

## IV. “To invalidate is a `MUST`!”
```c#
...
Y2Runner yr = new Y2Runner(conn, redis);
/*
   Run SQL statement to perform INSERT, UPDATE or DELETE operations.
   public bool RunSQL(string CommandText, string[] CacheTags = null)
*/
// (a) Use default CacheTags 
bool Result = yr.RunSQL(SQLText);

// (b) All specified CacheTags get called automatically.
bool Result = yr.RunSQL(SQLText, new string[] {"Orders", "Customers"});
```

## V. Cache in Action 
On invoking **RunSelectSQL**: 
```c#
   string HashedKey = ComputeSha256Hash(CommandText);
...
   if ((redis != null) && (redis.GetValue(HashedKey) != null))
   {
      // Hit! Load from cache...
      Debug.WriteLine(String.Format("\"{0}\" hit! load from cache...", HashedKey));
      DataSet dataSet = JsonConvert.DeserializeObject<DataSet>(redis.GetValue(HashedKey));
      ret = dataSet.Tables["Table1"];

      return ret; 
   }
...
// Read data from server. 
...
if ((redis != null) && (ttl > 0))
{
   // Missed! Add to cache...
   Debug.WriteLine(String.Format("\"{0}\" missed! Add to cache...", HashedKey));
   string JSONValue = JsonConvert.SerializeObject(ret);
   redis.SetValue(HashedKey, String.Format("{{\"Table1\": {0} }}", JSONValue), new TimeSpan(0, 0, ttl));

   if (CacheTags == null)
         CacheTags = ParseCacheTagsfromSelectSQL(CommandText);
   foreach (string CacheTag in CacheTags)
   {
         redis.SAdd(CacheTag, Encoding.ASCII.GetBytes(HashedKey));
         redis.Expire(CacheTag, ttl);
   }
}
```
To expire on time:

![All those moments will be lost in time like tears in rain](img/All-those-moments-will-be-lost-in-time-like-tears-in-rain.jpg)

To expire on demand by invoking **RunSQL**, *RemoveFromCache* get called automatically:
```c#
public void RemoveFromCache(string CacheTag)
{
   byte[][] smembers = redis.SMembers(CacheTag);
   foreach (byte[] bytes in smembers)
   {
      string member = Encoding.Default.GetString(bytes);
      redis.Del(member);
   }
   redis.Del(CacheTag);
}
```

## VI. Conclusion
![Knowledge is power but cash is king](img/knowledge-is-power-but-cash-is-king.jpg)

There's no **Rule of Thumb** to cache policy, what should be cached and what shouldn't is completely domain specific. Cache is not panacea, improper use of caching would even downgrade system performance significantly! 

Let's discuss what shouldn't be cached:
1. Rarely visited pages;
2. Pages used by a few;
3. CRUD pages and their direct parents;
4. User specific information pages;
5. All administrative pages (*Bosses don't like stale stuffs*);

And, what benefits from cached: 
1. Pages use code-table(s);
2. Common information or satistics pages;
3. Other less regularly updated pages. 


## VII. Reference 
1. [Query Caching with Redis](https://redis.com/blog/query-caching-redis/)
2. [Using Redis with Nodejs and MongoDB](https://subhrapaladhi.medium.com/using-redis-with-nodejs-and-mongodb-28e5a39a2696)
3. [Five Best Ways To Use Redis With ASP.NET MVC](https://www.c-sharpcorner.com/article/five-best-ways-to-use-redis-with-asp-net-mvc/)
4. [4 WAYS TO CONVERT JSON TO DATATABLE IN C# – ASP.NET](https://www.technothirsty.com/4-ways-to-convert-json-to-datatable-csharp-asp-net/)
5. [Convert Datatable to JSON in Asp.net C# [3 ways]](https://codepedia.info/convert-datatable-to-json-in-asp-net-c-sharp)
6. [Deserialize a DataSet](https://www.newtonsoft.com/json/help/html/DeserializeDataSet.htm)
7. [Redis 101: Foundation and Core Concepts](https://medium.com/@Mohammad_Hasham123/redis-101-foundation-and-core-concepts-41f32c2bf021)
8. [Compute SHA256 Hash In C#](https://www.c-sharpcorner.com/article/compute-sha256-hash-in-c-sharp/)
9. [Converting string to byte array in C#](https://stackoverflow.com/questions/16072709/converting-string-to-byte-array-in-c-sharp)
10. [How to convert byte array to string [duplicate]](https://stackoverflow.com/questions/11654562/how-to-convert-byte-array-to-str)
11. [Time elapse computation in milliseconds C#](https://stackoverflow.com/questions/13589853/time-elapse-computation-in-milliseconds-c-sharp)
12. [How to remove duplicate values from an array in C#](https://www.tutorialsteacher.com/articles/remove-duplicate-values-from-array-in-csharp)

## EOF (2022/06/17)
