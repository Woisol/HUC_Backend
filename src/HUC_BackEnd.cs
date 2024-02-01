using System;
using System.Data;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Formats.Asn1;
using MySqlConnector;//！这句开始无法识别要在命令行：dotnet add package MySqlConnector

namespace MySqlTestSpace
{
	class Program
	{
		static void Main()
		{
			ProcessControl.mainLoop();//静态方法不能用实例访问…………
			// try
			// {
			// 	Console.WriteLine("Connecting to MySQL..");
			// 	sqlCon.Open();
			// 	MySqlCommand sqlCmd = new MySqlCommand();
			// 	//~~这个不太能放到外面因为要构造
			// 	sqlCmd.Connection = sqlCon;
			// 	sqlCmd.CommandText = "SELECT Name, HeadOfState FROM Country WHERE Continent='Oceania'";
			// 	// sqlCmd.CommandText = "SELECT * FROM city WHERE ID = 1";
			// 	MySqlDataReader sqlReader = sqlCmd.ExecuteReader();

			// 	while(sqlReader.Read())
			// 	{
			// 		Console.WriteLine($"{sqlReader[0]} -- {sqlReader[1]}");
			// 	}
			// }
			// catch(Exception ex)
			// {
			// 	Console.WriteLine(ex.ToString());
			// }

			// sqlCon.Close();
			// Console.WriteLine("Done");

			// List<ProcessLog> ProcessList = new List<ProcessLog>();
			// try { var process = Process.GetProcessesByName("code.exe")[0]; }
			// //！如果不使用异常try只能用getProcess检测所有进程是否包含code.exe…………
			// catch { }
			// ProcessList.Add(Process.GetProcessesByName("code.exe")[0]);//！注意这里如果找不到会报错IndexOutOfRange
			// !ProcessList.Add(new Process = Process.GetProcessesByName("code.exe")[0]);


		}
	}
	class ProcessLog
	{
		public DateTime? startTime { get; set; } = null;
		public DateTime? endTime { get; set; } = null;
		public bool isRunning { get; set; } = false;
	}
	class ProcessControl
	{
		internal ProcessControl(string pcsName)
		{
			this.pcsName = pcsName;
		}

		static string DATABASE_NAME = "HUC_AppUsageLog_Test";
		internal string pcsName = "";

		internal ProcessLog curPcs = new ProcessLog();
		internal static MySqlConnection sqlCon = new MySqlConnection("server=localhost;user=root;database=HUC_AppUsageLog_Test;port=3306;password=60017089");
		// internal static MySqlCommand sqlCmd = new MySqlCommand();
		internal static MySqlDataReader? sqlReader = null;
		internal static void mainLoop()
		{
			//~~ List<ProcessLog> processLogList = new List<ProcessLog>();
			sqlCon.Open();//！别忘艹…………而且不能多次调用不然出错
			//~~ sqlCmd.Connection = sqlCon;
			// List<ProcessControl> pcsMntList = new List<ProcessControl> { new ProcessControl("Code"), new ProcessControl("flomo") };
			//!不应该用List而是直接导入DB，不然运行久了卡死你。
			//~~ sqlCmd.CommandText = "SELECT TABLE_NAME FROM information_schema.tables WHERE TABLE_SCHEMA = 'HUC_AppUsageLog_Test';";
			//!md不搞一个静态cmd了…………巨麻烦艹
			sqlReader = new MySqlCommand($"SELECT TABLE_NAME FROM information_schema.tables WHERE TABLE_SCHEMA = '{DATABASE_NAME}';", sqlCon).ExecuteReader();
			// sqlCon.Close();
			List<ProcessControl> pcsMntList = new List<ProcessControl>();//~~~ = { new ProcessControl("Code"), new ProcessControl("flomo") };//!所以这个依然是应用名不过不要exe而已
			while(sqlReader.Read())
			{
				pcsMntList.Add(new ProcessControl(sqlReader.GetString(0)));
			}
			//!由于不能反复设置cmd的原因这里必须要重置一次…………
			// sqlCmd.CommandText = null;并不行
			sqlCon.Close();sqlCon.Open();//！关键句…………不理解艹…………
			// sqlCmd = new MySqlCommand();
			// sqlCmd.Connection = sqlCon;

			for(; ; Thread.Sleep(100))//
			foreach(ProcessControl pcsCon in pcsMntList)
			{
				pcsCon.getPcsStatus();
			}
			//!先阻塞吧感觉好像也行
			//td加一个check，如果两次关闭开启小于3min就不多记一次了
			//td考虑电脑睡眠后的处理
			sqlCon.Close();//!!暂时无法访问
		}
		void getPcsStatus()
		//**理清思路
		//未启动：null, null, false				where Lengh = 0
		//启动：[startTime], null, [true]		where Judge
		//运行中：同
		//结束：startTime, [endTime], [false]	where Judge 写入and创建新的

		{
			// ProcessLog curPcs = new ProcessLog();//!开始忘记new了导致前后影响——但是同一个应用的不能丢失啊艹…………
			var tmpPcsList = Process.GetProcessesByName(pcsName);
			//dtd逻辑可以优化一下——现在优化不了了
			if(tmpPcsList.Length == 0)
			{
				//**----------------------------关闭时-----------------------------------------------------
				if(this.curPcs.isRunning == true)
				{
					// pcs.endTime = new DateTime().ToLocalTime();//！错误写法，会表示1900年…………
					this.curPcs.isRunning = false;
					this.curPcs.endTime = DateTime.Now.ToLocalTime();
					if(this.curPcs.startTime == null || this.curPcs.endTime == null){ throw new Exception("Time Log Fail or Lost!"); }

					//td写入数据库
					// sqlCmd.CommandText = $"UPDATE {pcsName} SET EndTime = '{this.curPcs.endTime}', LastTime = {((DateTime)this.curPcs.endTime - (DateTime)this.curPcs.startTime).TotalMinutes} WHERE StartTime = '{this.curPcs.startTime}'";//！md这里一直说没有这个函数就是在于可空类型…………
					// sqlCmd.ExecuteNonQueryAsync();//！这个是异步方法，实际使用复杂自己查了
					// sqlCon.Open();
					new MySqlCommand("UPDATE {pcsName} SET EndTime = '{this.curPcs.endTime}', LastTime = {((DateTime)this.curPcs.endTime - (DateTime)this.curPcs.startTime).TotalMinutes} WHERE StartTime = '{this.curPcs.startTime}'", sqlCon).ExecuteNonQueryAsync();//！这个是异步方法，实际使用复杂自己查了
					//not错误写法，可能导致注入！！！https://zhuanlan.zhihu.com/p/28401873 是喔用了这样的写法也不会担心哈哈
					//!不对注意例子中是WHERE NAME = ...的…………这样依然会导致注入

					Console.WriteLine($"{pcsName} end: {this.curPcs.startTime} ~ {this.curPcs.endTime}");//!ToString("yyyy-MM-dd HH:mm:ss")}但是似乎不太行
				}
			}
			else
			{
				//**----------------------------启动时-----------------------------------------------------
				if(this.curPcs.isRunning == false)
				{
					this.curPcs.isRunning = true;

					MySqlConnection lastCon = new MySqlConnection($"server=localhost;user=root;database={DATABASE_NAME};port=3306;password=60017089");
					lastCon.Open();//麻了又忘
					MySqlCommand lastCmd = new MySqlCommand($"SELECT * FROM {pcsName} ORDER BY EndTime DESC LIMIT 1;", lastCon);
					MySqlDataReader lastDataReader = lastCmd.ExecuteReader();
					lastDataReader.Read();
					// var lastEndTime = lastDataReader.GetDateTime("EndTime");
					if((DateTime.Now - lastDataReader.GetDateTime("EndTime")).TotalMinutes < 5 )
					{
						//~~ this.curPcs.startTime = lastDataReader.GetDateTime("StartTime");
						//~ sqlCmd.CommandText = $"UPDATE {pcsName} SET EndTime = '{this.curPcs.endTime}', LastTime = {((DateTime)this.curPcs.endTime - (DateTime)this.curPcs.startTime).TotalMinutes} WHERE StartTime = '{this.curPcs.startTime}';";//！md这里一直说没有这个函数就是在于可空类型…………
						//!这里忽略了开始检测时就已经启动的情况……
						if(this.curPcs.startTime == null || this.curPcs.endTime == null)
						{
							this.curPcs.startTime = lastDataReader.GetDateTime("StartTime");
							this.curPcs.endTime = DateTime.Now.ToLocalTime();
						}
						new MySqlCommand($"UPDATE {pcsName} SET EndTime = '{this.curPcs.endTime}', LastTime = {((DateTime)this.curPcs.endTime - (DateTime)this.curPcs.startTime).TotalMinutes} WHERE StartTime = '{this.curPcs.startTime}';", sqlCon).ExecuteNonQueryAsync();
						Console.WriteLine($"{pcsName} last exited in less then 5min: {this.curPcs.startTime} ~ {this.curPcs.endTime}");
					}
					else
					{
						this.curPcs = new ProcessLog();
						this.curPcs.isRunning = true;
						this.curPcs.startTime = DateTime.Now.ToLocalTime();
						//~~ sqlCmd.CommandText = $"INSERT INTO {pcsName} VALUES ('{this.curPcs.startTime}', '{this.curPcs.startTime}', 0);";
						new MySqlCommand($"INSERT INTO {pcsName} VALUES ('{this.curPcs.startTime}', '{this.curPcs.startTime}', 0);",sqlCon).ExecuteNonQueryAsync();
						Console.WriteLine($"{pcsName} start at {this.curPcs.startTime}");//!ToString("yyyy-MM-dd HH:mm:ss")}但是似乎不太行

					}
				}
				//!要考虑到如果长时间运行数据完全不更新…………
				//**----------------------------运行中-----------------------------------------------------
				else
				{
					this.curPcs.endTime = DateTime.Now.ToLocalTime();
					if(this.curPcs.startTime == null || this.curPcs.endTime == null){ throw new Exception("Time Log Fail or Lost!"); }
					//~~ sqlCmd.CommandText = $"UPDATE {pcsName} SET EndTime = '{this.curPcs.endTime}', LastTime = {((DateTime)this.curPcs.endTime - (DateTime)this.curPcs.startTime).TotalMinutes} WHERE StartTime = '{this.curPcs.startTime}';";//！md这里一直说没有这个函数就是在于可空类型…………
					new MySqlCommand($"UPDATE {pcsName} SET EndTime = '{this.curPcs.endTime}', LastTime = {((DateTime)this.curPcs.endTime - (DateTime)this.curPcs.startTime).TotalMinutes} WHERE StartTime = '{this.curPcs.startTime}';",sqlCon).ExecuteNonQueryAsync();
					//漏个;艹
					//!开始一直输出0，但是看来应该是decimal也显示的是四舍五入的整数艹其实没有问题了的
					///UPDATE flomo SET EndTime = '2024-01-31 22:52:19', LastTime = 666 WHERE StartTime = '2024-01-31 22:52:04'
					//~~ sqlCmd.ExecuteNonQueryAsync();
				}
			}
			// try
			// {
			// 	// ProcessLog curPcs = new ProcessLog();不能每次都new掉不然丢失上一个信息
			// 	curPcs.process = Process.GetProcessesByName(pcsName)[0];
			// }
			// catch(Exception ex)
			// {

			// }
		}
	}
}