# Caching SQL results with Redis, a strategic approach.

![Go ahead. Make my day.](img/go-ahead-make-my-day-quote-1.jpg)

## I. Introduction
Performance is costly, costs more than you can think, ie. knowledge and imagination. Previously, we've slightly tackled client/server performance issues, ie. Ajax as front-end enhancement to minimize unnecessary postbacks; Nginx reverse proxy caching Web Server files to speed up page loading. In thie article, we explore into the realm of backend database. 

No matter what brand of SQL servers you used, execution of SQL statements is time consuming when comparing to different application components. Instead of investing on SQL servers, one way to speed it up is to invest on a Redis server and *stategic caching policy*, ie. data are read and cached, so that subsequent access to the same data doesn't need another trip to database; cache needed to be invalidated by some events. Database is slow but massive; Cache is fast but tiny. By mixing both merits, this article serves as preliminary study on *feasibility* of caching SQL results with Redis. 

>Being a luxury things, performance, not only means money. Luxury relates more to lifestyle and taste, whereas money only relates to tycoon(大款).

## I. [SQL JOIN](https://www.w3schools.com/sql/sql_join.asp)
Following are typical SQL statement on single file:

```console
SELECT OrderID, CustomerID, OrderDate
FROM Orders;
```
On two files:

```console
SELECT Orders.OrderID, Customers.CustomerName, Orders.OrderDate
FROM Orders
INNER JOIN Customers 
ON Orders.CustomerID=Customers.CustomerID;
```
Relational database system are complicated softwares, they twist disk files into 
*logical* tables, so that further filter, join, group and sort operations are possible. 
What happens behind [SQL Processing](https://docs.oracle.com/database/121/TGSQL/tgsql_sqlproc.htm#TGSQL175) involves SQL Parsing, Syntax Check, Semantic Check, SQL Optimization, and SQL Execution. SQL Server have caching policy on their own but not 
specific on some data of some application domain. 


## II. Y2Runner
```console
Y2Runner yr = new Y2Runner(...);

DataTable dt = yr.RunSelectSQL("...", {"Orders", "Customers"});

String val = yr.RunValueSQL("...", {"Orders", "Customers"});
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


## III. Expire on write
```console
Y2Runner yr = new Y2Runner(...);

bool result = yr.RunSQL("...", {"Customers"});
// Note: 'RunSQL' implicitly call clearCache() function. 
```


## IV. Expire on demand
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
3. [Compute SHA256 Hash In C#](https://www.c-sharpcorner.com/article/compute-sha256-hash-in-c-sharp/)
4. [Convert Datatable to JSON in Asp.net C# [3 ways]](https://codepedia.info/convert-datatable-to-json-in-asp-net-c-sharp)
5. [4 WAYS TO CONVERT JSON TO DATATABLE IN C# – ASP.NET](https://www.technothirsty.com/4-ways-to-convert-json-to-datatable-csharp-asp-net/)
6. [Five Best Ways To Use Redis With ASP.NET MVC](https://www.c-sharpcorner.com/article/five-best-ways-to-use-redis-with-asp-net-mvc/)
7. [Redis 101: Foundation and Core Concepts](https://medium.com/@Mohammad_Hasham123/redis-101-foundation-and-core-concepts-41f32c2bf021)
8. [Redis | Transactions](https://redis.io/docs/manual/transactions/)

## EOF (2022/06/05)
