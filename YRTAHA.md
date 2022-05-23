YRunner ─ The Accidental HA (YRTAHA)

0. Prologue
It was a black Friday on August. Sun burns and rain pours day after day. I was 
chanced to upgrade my computer from win7 to Win10. Tormenting both eyes and brain, 
suffering endless anguish and agitation. I loathe it's non-human countenance and 
sank miserably on my armchair, withering on this barren and desolated land. 

A hideous grin was flashed on my mind, perceiving that i am nothing but a fool, 
an fatigued old-dog... Previously, a WebApp, namely TA, repeatedly causing troubles 
due to lack of Db resource to fulfill it's task. I endeavour to re-act to this 
sinister situation... 

At length, I manage to install the IIS components on my Win10 and determined to host 
the TA WebApp on it as a mirror site, ie: 

Main URL:
http://web2022/ta/login.aspx

Mirror URL: 
http://myhost/ta/login.aspx


1. YRunner  
Any application can not do without database. YRunner essentially provides three 
functions: RunSelectSQL, RunValueSQL and RunSQL. Simply speaking, RunSelectSQL 
executes SQL and returns a DataTable; RunValueSQL executes SQL and returns a value; 
RunSQL executes SQL of insert/update/delete.   

Lastly, a special RunInsertSQLYieldRowID is provided to run an insert SQL statement 
and return the auto rowid. YRunner runs on  Oracle (ADO.NET).


2. An graceful end 
The constructor function of YRunner is so designed that in case of Oracle Server is 
languid and reluctant to response to server request, instead of error page, a falsified 
server busy page is shown. 
	Login.aspx.cs
	-------------
	. . . 
        protected YRunner yr = new YRunner("conn");
	. . . 
	protected void Page_Load(object sender, EventArgs e)
	{
		String s = Request.QueryString["tuid"];

		if (! Page.IsPostBack )
		{
			DropDownList1.DataSource = TAUtils.getUsers(yr);
			DropDownList1.DataBind();
			TextBox1.Enabled = DropDownList1.Items[0].Text.Contains(TAUtils.PWD_INDICATOR);
			TextBox1.Focus();

			if (!string.IsNullOrEmpty(s))
			{
				DropDownList1.SelectedValue = s; 
			}
			clearFilterOptions();
		}
	}
	. . .

	YRunner.cs
	----------
	. . . 
	catch (Exception e)
	{
		Debug.WriteLine(ConfigurationManager.AppSettings["YR_MASK_MSG"]);
		Debug.WriteLine(e.ToString());

		if (Convert.ToBoolean(ConfigurationManager.AppSettings["YR_MASK_FLAG"]))
		{
			Http​Context.Current.Response.Redirect(String.Format(ConfigurationManager.AppSettings["YR_REDIRECT_URL"],                                                                         
									    ConfigurationManager.AppSettings["YR_MASK_MSG"].ToString()) +                                                                                                                                                  
									    HttpContext.Current.Server.UrlEncode(Environment.NewLine + 
													 	 Environment.NewLine + 
													 	 e.ToString()));
		}
		else
		{
			// 💀「死亡，也許沒有你想像的那樣可怕‧‧‧」
			throw e; 
		}
	}
	. . . 
	Web.config in web2022
	---------------------
	. . . 
	<appSettings>
	<!-- YRunner Exception handling -->
	<add key="YR_MASK_FLAG" value="true"/>
	<add key="YR_MASK_MSG" value="Database server is busy, please try again later... (YRnner.cs)"/>

	<!--- <add key="YR_REDIRECT_URL" value="~/Content/busy.html?m={0}"/> -->
	<add key="YR_REDIRECT_URL" value="http://myhost/ta/login.aspx?m={0}"/>  

	<!-- YRunner Exception handling --> 
	</appSettings>
	. . . 
	<connectionStrings>
		<!-- Oracle database connection (production) -->
		<add name="conn" connectionString="DATA SOURCE=oracle12-scan/pdbwrk;USER ID=dbconn;PASSWORD=poonsio$da;PERSIST SECURITY INFO=True;Connection Timeout=120;" providerName="Oracle.ManagedDataAccess.Client" />
		<!-- Oracle database connection (production) -->
	</connectionStrings>
	. . . 

Instead of busy.html of the same site; re-directing to another our mirror site has 
the effect of diverging request whenever main server can not connect to Db. In this 
way, a manual HA facility is setup... 


3. Summary 
High availability (HA) is the ability of a system to operate continuously without 
failing for a designated period of time. HA works to ensure a system meets an 
agreed-upon operational performance level. 

Load balancing is defined as the methodical and efficient distribution of network or 
application traffic across multiple servers in a server farm. Each load balancer sits 
between client devices and backend servers, receiving and then distributing incoming 
requests to any available server capable of fulfilling them.

Both HA and LB needs additional infrastructure hardware/software to operate. Diverting 
failed calls in YRunner only alleviate loading and of so little a value on day-to-day 
programming practice.  



4. Reference:
a. 
b. 
c. 


<EOF>
Written in 2021/08/13

/*

                 uuuuuuu
             uu$$$$$$$$$$$uu
          uu$$$$$$$$$$$$$$$$$uu
         u$$$$$$$$$$$$$$$$$$$$$u
        u$$$$$$$$$$$$$$$$$$$$$$$u
       u$$$$$$$$$$$$$$$$$$$$$$$$$u
       u$$$$$$$$$$$$$$$$$$$$$$$$$u
       u$$$$$$"   "$$$"   "$$$$$$u
       "$$$$"      u$u       $$$$"
        $$$u       u$u       u$$$
        $$$u      u$$$u      u$$$
         "$$$$uu$$$   $$$uu$$$$"
          "$$$$$$$"   "$$$$$$$"
            u$$$$$$$u$$$$$$$u
             u$"$"$"$"$"$"$u
  uuu        $$u$ $ $ $ $u$$       uuu
 u$$$$        $$$$$u$u$u$$$       u$$$$
  $$$$$uu      "$$$$$$$$$"     uu$$$$$$
u$$$$$$$$$$$uu    """""    uuuu$$$$$$$$$$
$$$$"""$$$$$$$$$$uuu   uu$$$$$$$$$"""$$$"
 """      ""$$$$$$$$$$$uu ""$"""
           uuuu ""$$$$$$$$$$uuu
  u$$$uuu$$$$$$$$$uu ""$$$$$$$$$$$uuu$$$
  $$$$$$$$$$""""           ""$$$$$$$$$$$"
   "$$$$$"                      ""$$$$""
     $$$"                         $$$$"


ASCII art for tag skull
https://textart.io/art/tag/skull/3


░▒▒ 💀 ▒▒░
Cool text art to use on your socials (>‿◠)✌
https://cooltext.top/skull
*/