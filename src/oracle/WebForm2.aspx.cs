using Oracle.ManagedDataAccess.Client;
using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class WebForm2 : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // Oracle 
        OracleConnection conn = new OracleConnection(WebConfigurationManager.ConnectionStrings["ORACLE"].ConnectionString);
        
        // Redis
        string host = ConfigurationManager.AppSettings["REDIS_HOST"].ToString();
        int port = Convert.ToInt32(ConfigurationManager.AppSettings["REDIS_PORT"]);
        RedisClient redis = new RedisClient(host, port);

        // Y2Runner
        Y2Runner y2r = new Y2Runner(conn, redis);

        DataTable dataTable = y2r.RunSelectSQL("select * from tbphysts order by physts", 
                                                60, 
                                                new string[] { "Customers", "Orders" });
        GridView1.DataSource = dataTable;
        GridView1.DataBind();

        GridView1.UseAccessibleHeader = true;
        GridView1.HeaderRow.TableSection = TableRowSection.TableHeader;

        //string[] ret = y2r.ParseCacheTagsR("select from from rsconmst f1,rsconfam where f1.nrctrnum=f2.nrctrnum and f1.sitcod='A' and aaa=','");
        //string[] ret = y2r.ParseCacheTagsfromSelectSQL(@"select * from from rsconmst f1, 
        //                                            (select count(*) from rsconfam f2 where f1.nrctrnum=f2.nrctrnum) f3, 
        //                                            rsconur f4
        //                                     where f1.nrctrnum=f2.nrctrnum and f1.sitcod='A' and aaa='def'");
        //string[] ret = y2r.ParseCacheTagsfromSelectSQL("select * from rsconmst f1 inner join rsconfam f2 on f1.nrctrnum=f2.nrctrnum where f1.sitcod='A' and");
        //string[] ret = y2r.ParseCacheTagsCUD("update    aaa set b = 1 where x=y; delete FROM    ccc where d=1; insert into eee values()");
        string[] ret = y2r.ParseCacheTagsfromSelectSQL(@"select
                                                                *
                                                            from
                                                                Employee
                                                                join (
                                                                    select
                                                                        *
                                                                    from
                                                                        A, B
                                                                    where
                                                                        A.a = B.b
                                                                ) AB on Employee.eid = AB.eid
                                                            where
                                                                Employee.eid = '1234'
                                                            ");
        //string[] ret = y2r.ParseCacheTagsfromSelectSQL(@"select * from Employee join (select * from A, B where A.a = B.b ) AB on Employee.eid = AB.eid where Employee.eid = '1234'");
        foreach (string s in ret)
            if (! String.IsNullOrEmpty(s)) Response.Write("[" + s + "]<br />");


        //string test = "a,,,b";
        //string[] bbb = test.Split(',');
        //foreach (string ccc in bbb)
        //    Response.Write("[" + ccc + "]");
    }
}