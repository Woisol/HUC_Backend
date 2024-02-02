using System;
using System.Data;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MySqlConnector;//！这句开始无法识别要在命令行：dotnet add package MySqlConnector
//~~ using Microsoft;
//~~ using Microsoft.Win32;

namespace MySqlTestSpace
{
	class Program
	{
		static void Main()
		//**----------------------------aka ConsoleControl-----------------------------------------------------
		{
			try
			{
				ProcessControl.sqlCon = new MySqlConnection($"server=localhost;user=root;database={ProcessControl.DATABASE_NAME};port=3306;password=60017089");
				ProcessControl.sqlCon.Open();ProcessControl.sqlCon.Close();
				// try{new MySqlCommand($"USE {ProcessControl.DATABASE_NAME};", conn); }
				//!这个无法作为判断……因为出错了完全不反应…………
			}
			catch
			{
				Console.WriteLine($"Database {ProcessControl.DATABASE_NAME} not found, creating...");
				MySqlConnection conn = new MySqlConnection($"server=localhost;user=root;port=3306;password=60017089");
				conn.Open();
				new MySqlCommand($"CREATE DATABASE {ProcessControl.DATABASE_NAME};", conn).ExecuteNonQuery();
				new MySqlCommand($"USE {ProcessControl.DATABASE_NAME};", conn).ExecuteNonQuery();
				new MySqlCommand($"CREATE TABLE longtimenoaction (StartTime DateTime, EndTime DateTime, LastTime Decimal);",conn).ExecuteNonQuery();
				new MySqlCommand($"INSERT INTO longtimenoaction VALUES ('{DateTime.Now.ToLocalTime()}', '{DateTime.Now.ToLocalTime()}', 0);", conn).ExecuteNonQuery();
				conn.Close();
			}

			Console.WriteLine("Monitor running...");
			(ProcessControl.threadMonitor = new Thread(ProcessControl.thread_Monitor)).Start();//！用新线程的方法！注意没有()
			Console.WriteLine("ConsoleControl Started, type your command below:");
																							   //~~ threadMonitor.Start();

			// SystemEvents.PowerModeChanged += ProcessControl.onPowerModeChanged;
			//！这个方法似乎是旧方法了，在本电脑上无效…………
			//！！！好耶！上网查就是要添加的包！dotnet add package Microsoft.Win32.SystemEvents --version 8.0.0
			while (true)
			{
				// Thread.Sleep(20);//!要不然初始的存在信息就覆盖在>后面了
				// Console.Write(">");
				var input = Console.ReadLine();
				if (input.ToLower() == "exit")
				{
					//td输出，以及下面
					Console.WriteLine("Exiting...");
					ProcessControl.setMonitorStauts(false);
					break;
				}
				else if (input.ToLower() == "help")
				{
					Console.WriteLine(@"1.monitor on/off: set monitor on/off
2.show apps: show apps monitored
3.show blist: show blacklist
4.show {app name}: show app runtime
5.add {app name}: add app to monitor
6.drop {app name}: stop monitoring app but keep its data
7.delete {app name}: delete app and its data
8.reboot: reboot monitor");
				}
				else if (input.ToLower() == "monitor on")
				{
					Console.WriteLine("Opening Monitor");
					ProcessControl.setMonitorStauts(true);
				}
				else if (input.ToLower() == "monitor off")
				{
					Console.WriteLine("Closing Monitor");
					ProcessControl.setMonitorStauts(false);
				}
				else if (input.ToLower() == "reboot")
				{
					Console.WriteLine("Rebooting...");
					ProcessControl.setMonitorStauts(false);
					ProcessControl.setMonitorStauts(true);
				}
				else if (new Regex(@"(?<=show )\w+", RegexOptions.IgnoreCase).IsMatch(input))
				{
					if (input.ToLower() == "show apps")
					{
						Console.WriteLine("Apps Monitored:");
						ProcessControl.showAppMonitored();
					}
					else if (input.ToLower() == "show blist")
					{
						Console.WriteLine("Blacklist:");
						ProcessControl.showBlackList();
					}
					else if (new Regex(@"(?<=show )\w+", RegexOptions.IgnoreCase).IsMatch(input))
					{
						var pcsName = new Regex(@"(?<=show )\w+", RegexOptions.IgnoreCase).Match(input).Value;
						//！忽略大小写的方法！
						ProcessControl.showPcsRunTime(pcsName);
					}
				}
				else if (new Regex(@"(?<=add )\w+", RegexOptions.IgnoreCase).IsMatch(input))
				{
					ProcessControl.addAppMonitored(new Regex(@"(?<=add )\w+", RegexOptions.IgnoreCase).Match(input).Value);
				}
				else if (new Regex(@"(?<=drop )\w+", RegexOptions.IgnoreCase).IsMatch(input))
				{
					ProcessControl.dropAppMonitored(new Regex(@"(?<=drop )\w+", RegexOptions.IgnoreCase).Match(input).Value);
				}
				else if (new Regex(@"(?<=delete )\w+", RegexOptions.IgnoreCase).IsMatch(input))
				{
					var pcsName = new Regex(@"(?<=delete )\w+", RegexOptions.IgnoreCase).Match(input).Value;
					var existApp = false;
					ProcessControl.pcsMntList.ForEach(p => { if (p.pcsName == pcsName) existApp = true; });
					// if(!ProcessControl.pcsMntList.Contains(pcsName))Console.WriteLine("App not found");
					if (!existApp) { Console.WriteLine("App not found"); continue; }
					Console.WriteLine($"Are you sure to permanently delete the app {pcsName} and its data? (y/n)");
					var i = Console.ReadLine();
					if (i.ToLower() == "y")
					{
						ProcessControl.deleteAppMonitored(pcsName);
					}
				}

				else { Console.WriteLine("Unknown command"); }
				// switch(input)还是不用switch了……效率估计不行
			}

			// ProcessControl.mainLoop_Monitor();//静态方法不能用实例访问…………
			// //！如果不使用异常try只能用getProcess检测所有进程是否包含code.exe…………
			// catch { }
			// ProcessList.Add(Process.GetProcessesByName("code.exe")[0]);//！注意这里如果找不到会报错IndexOutOfRange
			// !ProcessList.Add(new Process = Process.GetProcessesByName("code.exe")[0]);
		}
	}
	class ProcessLog
	{
		internal DateTime? startTime { get; set; } = null;
		internal DateTime? endTime { get; set; } = null;
		internal bool isRunning { get; set; } = false;
	}
	class ProcessControl
	{
		//td鉴于要搞成dll，改一下修饰符…………
		internal ProcessControl(string pcsName)
		{
			this.pcsName = pcsName;
		}

		internal static string ProcessListFileDir = @"pcsMntBlackList.plf";
		internal static string DATABASE_NAME = "HUC_AppUsageLog";
		public static bool isMonitor { get; set; } = true;
		internal string pcsName = "";


		internal ProcessLog pcs = new ProcessLog();
		internal static List<ProcessControl> pcsMntList = new List<ProcessControl>();//~~~ = { new ProcessControl("Code"), new ProcessControl("flomo") };//!所以这个依然是应用名不过不要exe而已
		internal static List<string> pcsMntBlackList = new List<string>();
		internal static MySqlConnection sqlCon = null;
		// internal static MySqlCommand sqlCmd = new MySqlCommand();
		internal static MySqlDataReader sqlReader = null;
		internal static Thread threadMonitor = new Thread(thread_Monitor);
		internal static void thread_Monitor()//!async X！注意这不是个标志，是在不能阻塞的方法里用，在里面用await来实现异步？
		{
			//~~ List<ProcessLog> processLogList = new List<ProcessLog>();
			pcsMntBlackList = File.ReadAllLines(ProcessListFileDir).ToList<string>();//！芜湖
																					 // if (sqlCon.State == ConnectionState.Open)
																					 // 	sqlCon.Close();
			sqlCon.Open();//！别忘艹…………而且不能多次调用不然出错
						  //~~ sqlCmd.Connection = sqlCon;
						  // List<ProcessControl> pcsMntList = new List<ProcessControl> { new ProcessControl("Code"), new ProcessControl("flomo") };
						  //!不应该用List而是直接导入DB，不然运行久了卡死你。
						  //~~ sqlCmd.CommandText = "SELECT TABLE_NAME FROM information_schema.tables WHERE TABLE_SCHEMA = 'HUC_AppUsageLog_Test';";
						  //!md不搞一个静态cmd了…………巨麻烦艹
			sqlReader = new MySqlCommand($"SELECT TABLE_NAME FROM information_schema.tables WHERE TABLE_SCHEMA = '{DATABASE_NAME}';", sqlCon).ExecuteReader();
			// sqlCon.Close();
			while (sqlReader.Read())
			{
				var curPcsName = sqlReader.GetString(0);
				var isAdd = true;
				if (pcsMntBlackList.Contains(curPcsName)) continue;
				foreach (var curPcs in pcsMntList)
				{
					if (curPcs.pcsName.ToLower() == curPcsName.ToLower()) isAdd = false;
					//！md所以为什么两次读取表名是不一样的？？？？啊啊啊！！！！
					//!考虑还是用ini文件等记录黑名单
				}
				if (isAdd)
					pcsMntList.Add(new ProcessControl(curPcsName));
			}
			//!由于不能反复设置cmd的原因这里必须要重置一次…………
			// sqlCmd.CommandText = null;并不行
			sqlCon.Close(); sqlCon.Open();//！关键句…………不理解艹…………
			for (; isMonitor; Thread.Sleep(100))//
			{
				if(Process.GetProcessesByName("LongTimeNoAction").Length > 0)
				{
                    Console.WriteLine($"Suspend: {DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}!");
					foreach(ProcessControl pcsCon in pcsMntList)
					{
						if (pcsCon.pcs.isRunning == false) continue;
						pcsCon.pcs.isRunning = false;
						pcsCon.pcs.endTime = DateTime.Now.ToLocalTime();
						if (pcsCon.pcs.startTime == null || pcsCon.pcs.endTime == null) { throw new Exception("Time Log Fail or Lost!"); }
						new MySqlCommand("UPDATE {pcsName} SET EndTime = '{pcsCon.curPcs.endTime}', LastTime = {((DateTime)pcsCon.curPcs.endTime - (DateTime)pcsCon.curPcs.startTime).TotalMinutes} WHERE StartTime = '{pcsCon.curPcs.startTime}'", sqlCon).ExecuteNonQueryAsync();
						Console.WriteLine($"{pcsCon.pcsName} end: {pcsCon.pcs.startTime} ~ {pcsCon.pcs.endTime}");//!ToString("yyyy-MM-dd HH:mm:ss")}但是似乎不太行
					}
					new Thread(thread_WaitForReboot).Start();
                    break;
				}

				foreach (ProcessControl pcsCon in pcsMntList)
				{
					pcsCon.getPcsStatus();
				}
			}
			//!先阻塞吧感觉好像也行
			//td考虑电脑睡眠后的处理
			sqlCon.Close();
		}
		internal static void thread_WaitForReboot()
		{
			while(true)
			{
				Thread.Sleep(1000);
				if(Process.GetProcessesByName("LongTimeNoAction").Length == 0)
				{
					isMonitor = true;
					(threadMonitor = new Thread(thread_Monitor)).Start();//!艹不能重启啊…………那我搞变量有什么用…………
					Console.WriteLine($"Reboot Successfully: {DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}!");
					break;
				}
			}
		}
		internal static void setMonitorStauts(bool isMonitor)
		{
			if (isMonitor)
			{
				ProcessControl.isMonitor = true;
				(threadMonitor = new Thread(thread_Monitor)).Start();//!艹不能重启啊…………那我搞变量有什么用…………
			}
			else
			{
				ProcessControl.isMonitor = false;
				//这种方式其实不太正规吧…………
				// if (threadMonitor != null && threadMonitor.IsAlive)
				threadMonitor.Join(); //！重要方法！！！ 等待结束否则connection没有close就打开无法加载新的command！
									  //！后面出现了重复检测的问题，开始一直以为是线程没关掉但是已经关了，在于List的Add加多了！！！！！！！！！！！！！
			}
		}
		internal static void showPcsRunTime(string pcsName)
		{
			// MySqlConnection sqlCon = new MySqlConnection($"Server=localhost;user=root;Database={ProcessControl.DATABASE_NAME};port=3306;password=60017089;");
			// sqlCon.Open();
			try
			{
				MySqlDataReader sqlReader = new MySqlCommand($"SELECT * FROM {pcsName} ORDER BY StartTime DESC LIMIT 1;", sqlCon).ExecuteReader();
				sqlReader.Read();//！当前上下文中不存在名称“sqlReader”看来try里面是一个密闭空间…………
								 //td输出
				Console.WriteLine($"{pcsName} last runtime: {sqlReader.GetDateTime("StartTime")} - {sqlReader.GetDateTime("EndTime")} for {sqlReader.GetDecimal("LastTime")}min");
			}
			catch (Exception e)
			{
				//td输出异常
				Console.WriteLine(e.Message);
				return;
			}
			//！sqlReader["StartTime"]和sqlReader.GetString("EndTime")的区别在于前者不知道返回类型返回的是object
			sqlCon.Close();
			sqlCon.Open();
		}
		internal static void showAppMonitored()
		{
			if (pcsMntList.Count == 0)
			{
				//td输出
				Console.WriteLine("No app monitored!");
			}
			foreach (ProcessControl pcsCon in pcsMntList)
			{
				//td输出
				Console.WriteLine(pcsCon.pcsName);
			}
		}
		internal static void showBlackList()
		{
			if (pcsMntList.Count == 0)
			{
				//td输出
				Console.WriteLine("No app in black list!");
			}
			foreach (string blackName in pcsMntBlackList)
			{
				//td输出
				Console.WriteLine(blackName);
			}
		}
		internal static void addAppMonitored(string pcsName)
		{
			// pcsMntList.ForEach(pcsCon => if(pcsCon.pcsName == pcsName)throw new Exception("App already monitored!");)
			//！wokforeach的行内写法！！！可f惜if用不了
			if (pcsMntBlackList.Contains(pcsName))
				pcsMntBlackList.Remove(pcsName);
			File.WriteAllLinesAsync(ProcessListFileDir, pcsMntBlackList.ToArray<string>());//！好耶！[]和数组的相互转化！
			foreach (ProcessControl pcsCon in pcsMntList)
			{
				if (pcsCon.pcsName == pcsName)
				// throw new Exception("App already monitored!");
				{
					Console.WriteLine("App already monitored!"); return;
				}
			}
			new MySqlCommand($"CREATE TABLE IF NOT EXISTS {pcsName} LIKE longtimenoaction;", sqlCon).ExecuteNonQuery();//!艹这里不能用异步了…………
																											//td新建一个样板表
			new MySqlCommand($"INSERT INTO {pcsName} VALUES ('{DateTime.Now.ToLocalTime()}', '{DateTime.Now.ToLocalTime()}', 0);", sqlCon).ExecuteNonQueryAsync();
			//!可以多次cmd…………
			//！不能在foreach的时候改变List，所以只能重启进程了………………
			// threadReboot();
			setMonitorStauts(false);
			Console.WriteLine("Rebooting...");
			pcsMntList.Add(new ProcessControl(pcsName));
			setMonitorStauts(true);

		}
		internal static void dropAppMonitored(string pcsName)
		{
			if (pcsMntBlackList.Contains(pcsName)) { Console.WriteLine("App already dropped!"); }//td输出
			else pcsMntBlackList.Add(pcsName);
			setMonitorStauts(false);
			var existApp = false;
			foreach (ProcessControl pcsCon in pcsMntList)
			{
				if (pcsCon.pcsName == pcsName)
				{
					pcsMntList.Remove(pcsCon);
					//td输出
					Console.WriteLine("App dropped!");
					existApp = true;
					break;
				}
			}
			if (existApp) File.WriteAllLinesAsync(ProcessListFileDir, pcsMntBlackList.ToArray<string>());
			else Console.WriteLine("App not found!");
			setMonitorStauts(true);
		}
		public static void deleteAppMonitored(string pcsName)
		{
			var originStr = File.ReadAllLines(ProcessListFileDir);
			List<string> resStrList = new List<string>();
			foreach (var curStr in originStr)
				if (curStr != pcsName) resStrList.Add(curStr);
			File.WriteAllLines(ProcessListFileDir, resStrList.ToArray<string>());

			new MySqlCommand($"DROP TABLE IF EXISTS {pcsName};", sqlCon).ExecuteNonQueryAsync();
			Console.WriteLine($"App {pcsName} and its data has been deleted!");
		}
		// internal static void threadReboot()
		// {
		// }
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
			if (tmpPcsList.Length == 0)
			{
				//**----------------------------关闭时-----------------------------------------------------
				if (this.pcs.isRunning == true)
				{
					// pcs.endTime = new DateTime().ToLocalTime();//！错误写法，会表示1900年…………
					this.pcs.isRunning = false;
					this.pcs.endTime = DateTime.Now.ToLocalTime();
					if (this.pcs.startTime == null || this.pcs.endTime == null) { throw new Exception("Time Log Fail or Lost!"); }

					//td写入数据库
					// sqlCmd.CommandText = $"UPDATE {pcsName} SET EndTime = '{this.curPcs.endTime}', LastTime = {((DateTime)this.curPcs.endTime - (DateTime)this.curPcs.startTime).TotalMinutes} WHERE StartTime = '{this.curPcs.startTime}'";//！md这里一直说没有这个函数就是在于可空类型…………
					// sqlCmd.ExecuteNonQueryAsync();//！这个是异步方法，实际使用复杂自己查了
					// sqlCon.Open();
					new MySqlCommand("UPDATE {pcsName} SET EndTime = '{this.curPcs.endTime}', LastTime = {((DateTime)this.curPcs.endTime - (DateTime)this.curPcs.startTime).TotalMinutes} WHERE StartTime = '{this.curPcs.startTime}'", sqlCon).ExecuteNonQueryAsync();//！这个是异步方法，实际使用复杂自己查了
																																																																	   //not错误写法，可能导致注入！！！https://zhuanlan.zhihu.com/p/28401873 是喔用了这样的写法也不会担心哈哈
																																																																	   //!不对注意例子中是WHERE NAME = ...的…………这样依然会导致注入

					Console.WriteLine($"{pcsName} end: {this.pcs.startTime} ~ {this.pcs.endTime}");//!ToString("yyyy-MM-dd HH:mm:ss")}但是似乎不太行
				}
			}
			else
			{
				//**----------------------------启动时-----------------------------------------------------
				if (this.pcs.isRunning == false)
				{
					this.pcs.isRunning = true;

					MySqlConnection lastCon = new MySqlConnection($"server=localhost;user=root;database={DATABASE_NAME};port=3306;password=60017089");
					lastCon.Open();//麻了又忘
					MySqlDataReader lastDataReader = new MySqlCommand($"SELECT * FROM {pcsName} ORDER BY EndTime DESC LIMIT 1;", lastCon).ExecuteReader();
					lastDataReader.Read();
					// var lastEndTime = lastDataReader.GetDateTime("EndTime");
					//！新建了一个app table但是没有初始数据导致异常！
					if ((DateTime.Now - lastDataReader.GetDateTime("EndTime")).TotalMinutes < 5)
					{
						//~~ this.curPcs.startTime = lastDataReader.GetDateTime("StartTime");
						//~ sqlCmd.CommandText = $"UPDATE {pcsName} SET EndTime = '{this.curPcs.endTime}', LastTime = {((DateTime)this.curPcs.endTime - (DateTime)this.curPcs.startTime).TotalMinutes} WHERE StartTime = '{this.curPcs.startTime}';";//！md这里一直说没有这个函数就是在于可空类型…………
						//!这里忽略了开始检测时就已经启动的情况……
						if (this.pcs.startTime == null || this.pcs.endTime == null)
						{
							this.pcs.startTime = lastDataReader.GetDateTime("StartTime");
							this.pcs.endTime = DateTime.Now.ToLocalTime();
						}
						new MySqlCommand($"UPDATE {pcsName} SET EndTime = '{this.pcs.endTime}', LastTime = {((DateTime)this.pcs.endTime - (DateTime)this.pcs.startTime).TotalMinutes} WHERE StartTime = '{this.pcs.startTime}';", sqlCon).ExecuteNonQueryAsync();
						Console.WriteLine($"{pcsName} reboot in less then 5min: {this.pcs.startTime} ~ {this.pcs.endTime}");
					}
					else
					{
						this.pcs = new ProcessLog();
						this.pcs.isRunning = true;
						this.pcs.startTime = DateTime.Now.ToLocalTime();
						//~~ sqlCmd.CommandText = $"INSERT INTO {pcsName} VALUES ('{this.curPcs.startTime}', '{this.curPcs.startTime}', 0);";
						new MySqlCommand($"INSERT INTO {pcsName} VALUES ('{this.pcs.startTime}', '{this.pcs.startTime}', 0);", sqlCon).ExecuteNonQueryAsync();
						//INSERT INTO AltDrag VALUES ('2024-01-01 00:00:00', '2024-01-01 00:00:00', 0);
						Console.WriteLine($"{pcsName} start at {this.pcs.startTime}");//!ToString("yyyy-MM-dd HH:mm:ss")}但是似乎不太行
					}
				}
				//!要考虑到如果长时间运行数据完全不更新…………
				//**----------------------------运行中-----------------------------------------------------
				else
				{
					this.pcs.endTime = DateTime.Now.ToLocalTime();
					if (this.pcs.startTime == null || this.pcs.endTime == null) { throw new Exception("Time Log Fail or Lost!"); }
					//~~ sqlCmd.CommandText = $"UPDATE {pcsName} SET EndTime = '{this.curPcs.endTime}', LastTime = {((DateTime)this.curPcs.endTime - (DateTime)this.curPcs.startTime).TotalMinutes} WHERE StartTime = '{this.curPcs.startTime}';";//！md这里一直说没有这个函数就是在于可空类型…………
					new MySqlCommand($"UPDATE {pcsName} SET EndTime = '{this.pcs.endTime}', LastTime = {((DateTime)this.pcs.endTime - (DateTime)this.pcs.startTime).TotalMinutes} WHERE StartTime = '{this.pcs.startTime}';", sqlCon).ExecuteNonQueryAsync();
					//漏个;艹
					//!开始一直输出0，但是看来应该是decimal也显示的是四舍五入的整数艹其实没有问题了的
					///UPDATE flomo SET EndTime = '2024-01-31 22:52:19', LastTime = 666 WHERE StartTime = '2024-01-31 22:52:04'
					//~~ sqlCmd.ExecuteNonQueryAsync();
				}
			}

		}
		//~~ internal void onPowerModeChanged()//!static对本项无效
		//~~ {

		//~~ 	Console.WriteLine("PowerModeChanged!");
		//~~ 	foreach(ProcessControl pcsCon in pcsMntList)
		//~~ 	{

		//~~ 	}
		//~~ }
		//~~ void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        //~~ {
        //~~     switch (e.Mode)
        //~~     {

        //~~         case PowerModes.Resume://系统挂起到重新唤醒
        //~~             Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "  Resume\n");
		//~~ 			threadMonitor = new Thread(thread_Monitor);
        //~~             break;
        //~~         case PowerModes.Suspend://系统挂起到重新唤醒
        //~~             Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "  Suspend\n");
		//~~ 			foreach(ProcessControl pcsCon in pcsMntList)
		//~~ 			{
		//~~ 				if (pcsCon.curPcs.isRunning == false) continue;
		//~~ 				pcsCon.curPcs.isRunning = false;
		//~~ 				pcsCon.curPcs.endTime = DateTime.Now.ToLocalTime();
		//~~ 				if (pcsCon.curPcs.startTime == null || pcsCon.curPcs.endTime == null) { throw new Exception("Time Log Fail or Lost!"); }
		//~~ 				new MySqlCommand("UPDATE {pcsName} SET EndTime = '{pcsCon.curPcs.endTime}', LastTime = {((DateTime)pcsCon.curPcs.endTime - (DateTime)pcsCon.curPcs.startTime).TotalMinutes} WHERE StartTime = '{pcsCon.curPcs.startTime}'", sqlCon).ExecuteNonQueryAsync();
		//~~ 				Console.WriteLine($"{pcsName} end: {pcsCon.curPcs.startTime} ~ {pcsCon.curPcs.endTime}");//!ToString("yyyy-MM-dd HH:mm:ss")}但是似乎不太行
		//~~ 			}
        //~~             break;
        //~~     }
        //~~ }
	}
}