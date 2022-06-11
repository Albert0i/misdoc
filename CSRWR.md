# Caching SQL results with Redis, a strategic approach.

![Go ahead. Make my day.](img/go-ahead-make-my-day-quote-1.jpg)

## I. Introduction
Performance is costly, costs more than you can think, ie. knowledge and imagination. Previously, we've slightly tackled client/server performance issues, ie. **Ajax** as front-end enhancement to minimize unnecessary postbacks; **Nginx** as reverse proxy caching Web Server static files to speed up page loading. In thie article, we explore into the last realm of backend database. 

No matter what brand of SQL servers you used, execution of SQL statements is always most time consuming in request/response cycle. Instead of investing on SQL servers, one way to speed up is to invest on a **Redis** server and employing a *stategic caching policy*, so that the same data doesn't need another trip to database; At some point, cache needs to be invalidated by some events, since cache is fast but tiny, whereas SQL Server is slow but huge. By mixing both merits, this article serves as preliminary study on *feasibility* of caching SQL results with Redis. 

>Being a luxury things, performance, not only means money. Luxury relates more to lifestyle and taste, whereas money only relates to tycoon(大款).


## I. [SQL Joins](https://www.w3schools.com/sql/sql_join.asp)
A JOIN clause is used to combine rows from two or more tables, based on a related column between them. Let's look at a selection from the "Orders" table:

```console
SELECT OrderID, CustomerID, OrderDate
FROM Orders;
```
And selects records that have matching values in both tables:

```console
SELECT Orders.OrderID, Customers.CustomerName, Orders.OrderDate
FROM Orders
INNER JOIN Customers 
ON Orders.CustomerID=Customers.CustomerID;
```
Relational database system are complicated softwares, they twist disk files into 
*logical* tables, so that further match, group and order operations are possible. 
What happens behind [SQL Processing](https://docs.oracle.com/database/121/TGSQL/tgsql_sqlproc.htm#TGSQL175) involves SQL Parsing, Syntax Check, Semantic Check, Optimization, and Execution. SQL Servers have intrinsic caching policy on their own. 


## II. [“To cache, or not to cache: that is the question!”](https://www.goodreads.com/quotes/36560-to-be-or-not-to-be-that-is-the-question)
```console
SqlConnection conn = new SqlConnection(...);
RedisClient redis = new RedisClient(...); 

Y2Runner yr = new Y2Runner(conn, redis);

// 1. Run SQL statement and returns a datatable result 
// RunSelectSQL(string sqlText, int ttl=60, string[] cacheTags);
DataTable SomeTable = yr.RunSelectSQL("...", {"Orders", "Customers"});

// 2. Run SQL statement and return a value result
// RunValueSQL(string sqlText, int ttl=60, string[] cacheTags);
String SomeValue = yr.RunValueSQL("...", new string[] {"Orders", "Customers"}).ToString();

```
1. calculate hash value of SQL statement;
2. \> GET hash-value;
3. **if the result is not nil, refresh *ttl*, de-serialize and return datatable/value else go to step 4**;
4. run the query and serialize the datatable/value to json-value;
5. \> MULTI
6. \> SET hash-value json-value EX *ttl*
7. \> SADD Orders hash-value EX *ttl*
8. \> SADD Customers hash-value EX *ttl*
9. \> EXEC


## III. “To invalidae is a MUST!”
```console
Y2Runner yr = new Y2Runner(...);

// 3. Run SQL statement to perform INSERT, UPDATE or DELETE operations.
// RunSQL(string sqlText);
Bool Result = yr.RunSQL("...");
// Note: 'RunSQL' implicitly call clearCache() function. 
```


## IV. Expire on time and Expire on demand
```console
Y2Runner yr = new Y2Runner(...);

yr.clearCache({"Orders", "Customers"});
```
1. \>SMEMBERS Customers
2. \>DEL hash-value(s)
3. \>DEL Customers


## V. Conclusion
```console
Cache is expensive but fast; 
Cache is fast but small;
Cache is small deliberately used;
```
There's no **Rule of Thumb** to cache policy, what should be cached and what shouldn't is completely domain specific. Cache is not panacea, improper use of caching would even downgrade system performance! 

Let discuss what shouldn't be cached:
1. Rarely visited pages;
2. Pages used by a few;
3. CRUD pages and their direct parent;
4. User specific information page;
5. All administrative pages (*Bosses don't like stale info*);

And, what benefits from cached:  
1. Pages use code-table(s);
2. Common information or satistics pages;
3. 

### VI. Reference 
1. [Query Caching with Redis](https://redis.com/blog/query-caching-redis/)
2. [Using Redis with Nodejs and MongoDB](https://subhrapaladhi.medium.com/using-redis-with-nodejs-and-mongodb-28e5a39a2696)
3. [Five Best Ways To Use Redis With ASP.NET MVC](https://www.c-sharpcorner.com/article/five-best-ways-to-use-redis-with-asp-net-mvc/)
4. [4 WAYS TO CONVERT JSON TO DATATABLE IN C# – ASP.NET](https://www.technothirsty.com/4-ways-to-convert-json-to-datatable-csharp-asp-net/)
5. [Convert Datatable to JSON in Asp.net C# [3 ways]](https://codepedia.info/convert-datatable-to-json-in-asp-net-c-sharp)
6. [Redis 101: Foundation and Core Concepts](https://medium.com/@Mohammad_Hasham123/redis-101-foundation-and-core-concepts-41f32c2bf021)
7. [Redis | Transactions](https://redis.io/docs/manual/transactions/)
8. [Compute SHA256 Hash In C#](https://www.c-sharpcorner.com/article/compute-sha256-hash-in-c-sharp/)

## EOF (2022/06/12)
