using System;
using System.ComponentModel;
using System.Data;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MySqlConnector;//！这句开始无法识别要在命令行：dotnet add package MySqlConnector
//~~ using Microsoft;
//~~ using Microsoft.Win32;

namespace HUC_BackEnd_Remaster
// ！艹忘记了改变namespace就可以防止二义了呀！
{
	class MainProcess
	{
		public static Config? config;
		static void Main()
		{
			//**----------------------------aka ConsoleControl-----------------------------------------------------
			//~~ //**----------------------------原来是可以修改窗口大小的！并不行-----------------------------------------------------
			//~~ Console.WindowWidth = 200;
			//~~ Console.WindowHeight = 300;
		//**----------------------------读取json配置文件-----------------------------------------------------
			if(!File.Exists("../config.json")){
				File.Open("../config.json", FileMode.Create).Close();
				File.WriteAllText("../config.json","{\r\n\t\"ProcessListFileDir\": \"pcsMntBlackList.plf\",\r\n\t\"RuntimeLogFileDir\": \"runtimeLog.rlf\",\r\n\t\"DATABASE_NAME\": \"HUC_AppUsageLog\"\r\n}");
			}
				var configFile = File.ReadAllText("../config.json");
				config = JsonSerializer.Deserialize<Config>(configFile);
				// ！艹解决了…………为什么必须是同一个类下面的………………
		//**----------------------------Sql cmd 监视进程初始化-----------------------------------------------------
			ProcessLog.sqlCon = new MySqlConnection($"server=localhost;user=root;database={config.DATABASE_NAME};port=3306;password=60017089");
			try{ProcessLog.sqlCon.Open();ProcessLog.sqlCon.Close();}
			catch{
				Console.WriteLine($"Database {config.DATABASE_NAME} not found, creating...");
				MySqlConnection conn = new MySqlConnection($"server=localhost;user=root;port=3306;password=60017089");
				conn.Open();
				new MySqlCommand($"CREATE DATABASE {config.DATABASE_NAME};", conn).ExecuteNonQuery();
				new MySqlCommand($"USE {config.DATABASE_NAME};", conn).ExecuteNonQuery();
				new MySqlCommand($"CREATE TABLE longtimenoaction (StartTime DateTime, EndTime DateTime, LastTime Decimal);",conn).ExecuteNonQuery();
				new MySqlCommand($"INSERT INTO longtimenoaction VALUES ('{DateTime.Now.ToLocalTime()}', '{DateTime.Now.ToLocalTime()}', 0);", conn).ExecuteNonQuery();
				conn.Close();
				ProcessLog.sqlCon = new MySqlConnection($"server=localhost;user=root;database={config.DATABASE_NAME};port=3306;password=60017089");
			}

			Process titleChangePcs = new Process();
			titleChangePcs.StartInfo.FileName = "cmd.exe";
			titleChangePcs.StartInfo.Arguments = "/c title HUC";
			titleChangePcs.Start();

			try{(ProcessLog.threadMonitor = new Thread(ProcessLog.thread_Monitor)).Start(); }
			// ！注意这个并不返回bool而是抛出异常…………
			catch{ Console.WriteLine("Monitor Start Failed"); }

			while (true)
			{
				var input = Console.ReadLine();
				if (input.ToLower() == "exit")
				{
					ProcessLog.setMonitorStauts(false);
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
					ProcessLog.setMonitorStauts(true);
				}
				else if (input.ToLower() == "monitor off")
				{
					ProcessLog.setMonitorStauts(false);
				}
				else if (input.ToLower() == "reboot")
				{
					Console.WriteLine("Monitor Rebooting");
					ProcessLog.setMonitorStauts(false);
					ProcessLog.setMonitorStauts(true);
				}
				else if (input.ToLower() == "log")
				{
					ProcessLog.showRTLog();
				}
				else if(input.ToLower() == "clear")
				{
					Console.Clear();
				}
				else if (new Regex(@"(?<=show )\w+", RegexOptions.IgnoreCase).IsMatch(input))
				{
					if (input.ToLower() == "show apps")
					{
						// Console.WriteLine("Apps Monitored:");
						ProcessLog.showAppMonitored();
					}
					else if (input.ToLower() == "show blist")
					{
						// Console.WriteLine("Blacklist:");
						ProcessLog.showBlackList();
					}
					else if (new Regex(@"(?<=show )\w+", RegexOptions.IgnoreCase).IsMatch(input))
					{
						var pcsName = new Regex(@"(?<=show )\w+", RegexOptions.IgnoreCase).Match(input).Value;
						ProcessLog.showPcsRunTime(pcsName);
					}
				}
				else if (new Regex(@"(?<=add )\w+", RegexOptions.IgnoreCase).IsMatch(input))
				{
					ProcessLog.addAppMonitored(new Regex(@"(?<=add )\w+", RegexOptions.IgnoreCase).Match(input).Value);
				}
				else if (new Regex(@"(?<=drop )\w+", RegexOptions.IgnoreCase).IsMatch(input))
				{
					ProcessLog.dropAppMonitored(new Regex(@"(?<=drop )\w+", RegexOptions.IgnoreCase).Match(input).Value);
				}
				else if (new Regex(@"(?<=delete )\w+", RegexOptions.IgnoreCase).IsMatch(input))
				{
					var pcsName = new Regex(@"(?<=delete )\w+", RegexOptions.IgnoreCase).Match(input).Value;
					var existApp = false;
					ProcessLog.pcsMntList.ForEach(p => { if (p.pcsName == pcsName) existApp = true; });
					// if(!ProcessLog.pcsMntList.Contains(pcsName))Console.WriteLine("App not found");
					if (!existApp) { Console.WriteLine("App not found"); continue; }
					// Console.WriteLine($"Are you sure to permanently delete the app {pcsName} and its data? (y/n)");
					var i = Console.ReadLine();
					if (i.ToLower() == "y")
					{
						ProcessLog.deleteAppMonitored(pcsName);
					}
				}
				else { Console.WriteLine("Unknown command"); }
			}
		}
	}
	public class Config{
			// ！艹类也可以public…………
			// public Config(string ProcessListFileDir, string RuntimeLogFileDir, string DATABASE_NAME){
			// 	this.ProcessListFileDir = ProcessListFileDir;this.RuntimeLogFileDir = RuntimeLogFileDir;this.DATABASE_NAME = DATABASE_NAME;
			// }
			public string ProcessListFileDir{get;set;} = "";
			public string RuntimeLogFileDir{get;set;} = "";
			public string DATABASE_NAME{get;set;} = "";
		}

	class ProcessLog
	{
		//**----------------------------总属性-----------------------------------------------------
		public static bool isMonitor { get; set; } = true;
		//**----------------------------进程属性-----------------------------------------------------
		internal string pcsName = "";
		internal DateTime? startTime { get; set; } = null;
		internal DateTime? endTime { get; set; } = null;
		internal bool isRunning { get; set; } = false;
		internal ProcessLog(string pcsName)
		{
			this.pcsName = pcsName;
		}
		//**----------------------------配置信息-----------------------------------------------------
		// public static Config? config = new Config();
		internal static List<ProcessLog> pcsMntList = new List<ProcessLog>();//~~~ = { new ProcessLog("Code"), new ProcessLog("flomo") };//!所以这个依然是应用名不过不要exe而已
		internal static List<string> pcsMntBlackList = new List<string>();
		//**----------------------------文件读写-----------------------------------------------------
		internal static FileStream rlfStream = new FileStream(MainProcess.config.RuntimeLogFileDir,FileMode.Append);
		internal static StreamWriter runtimeLogStreamWriter = new StreamWriter(rlfStream);


		// dtd检查一下必要性…………
		//**----------------------------数据库读写-----------------------------------------------------
		internal static MySqlConnection? sqlCon = null;
		internal static MySqlDataReader? sqlReader = null;
		//##----------------------------进程相关-----------------------------------------------------
		internal static Thread threadMonitor = new Thread(thread_Monitor);
		internal static void thread_Monitor()
		{
			runtimeLogStreamWriter.AutoFlush = true;
			//**----------------------------读取黑名单进程-----------------------------------------------------
			if (!File.Exists(MainProcess.config.ProcessListFileDir))
            	File.Create(MainProcess.config.ProcessListFileDir).Close(); //!创建并关闭文件流
			pcsMntBlackList = File.ReadAllLines(MainProcess.config.ProcessListFileDir).ToList<string>();//！芜湖
			sqlCon.Open();//！别忘艹…………而且不能多次调用不然出错
			sqlReader = new MySqlCommand($"SELECT TABLE_NAME FROM information_schema.tables WHERE TABLE_SCHEMA = '{MainProcess.config.DATABASE_NAME}';", sqlCon).ExecuteReader();
			// sqlCon.Close();
			while (sqlReader.Read())
			{
				var curPcsName = sqlReader.GetString(0);
				var isAdd = true;
				if (pcsMntBlackList.Contains(curPcsName)) continue;
				foreach (var curPcs in pcsMntList)
				{
					if (curPcs.pcsName.ToLower() == curPcsName.ToLower()) isAdd = false;
				}
				if (isAdd)
					pcsMntList.Add(new ProcessLog(curPcsName));
			}
			sqlCon.Close(); sqlCon.Open();
			Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")}: [STAT] Monitor Started");
			runtimeLogStreamWriter.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: [STAT] Monitor Started");
			//**----------------------------主监视代码-----------------------------------------------------
			for (; isMonitor; Thread.Sleep(1000))
			{
				if(Process.GetProcessesByName("LongTimeNoAction").Length > 0)
				{
                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")}: [STAT] Monitor Suspend");
                    runtimeLogStreamWriter.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: [STAT] Monitor Suspend");
					foreach(ProcessLog pcsCon in pcsMntList)
					{
						if (pcsCon.isRunning == false) continue;
						pcsCon.isRunning = false;
						pcsCon.endTime = DateTime.Now.ToLocalTime();
						if (pcsCon.startTime == null || pcsCon.endTime == null) { throw new Exception("Time Log Fail or Lost!"); }
						new MySqlCommand("UPDATE {pcsName} SET EndTime = '{pcsCon.endTime}', LastTime = {((DateTime)pcsCon.endTime - (DateTime)pcsCon.startTime).TotalMinutes} WHERE StartTime = '{pcsCon.startTime}'", sqlCon).ExecuteNonQueryAsync();
						Console.WriteLine($"{((DateTime)pcsCon.endTime).ToString("HH:mm:ss")}: [INFO] AppEnd {pcsCon.pcsName}");
						runtimeLogStreamWriter.WriteLine($"{((DateTime)pcsCon.endTime).ToString("HH:mm:ss")}: [INFO] AppEnd {pcsCon.pcsName}");
					}
					new Thread(thread_WaitForReboot).Start();
                    break;
				}

				foreach (ProcessLog pcsCon in pcsMntList)
				{
					pcsCon.getPcsStatus();
				}
			}
			//dtd考虑电脑睡眠后的处理
			foreach(ProcessLog pcsCon in pcsMntList){
				pcsCon.shutdownPcs();
			}
			sqlCon.Close();
				Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")}: [STAT] Monitor Stop");
				runtimeLogStreamWriter.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: [STAT] Monitor Stop");
		}
		//**----------------------------重启代码-----------------------------------------------------
		internal static void thread_WaitForReboot()
		{
			while(true)
			{
				Thread.Sleep(10000);
				if(Process.GetProcessesByName("LongTimeNoAction").Length == 0)
				{
					isMonitor = true;
					(threadMonitor = new Thread(thread_Monitor)).Start();
					Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")}: [STAT] Reboot Successfully");
					runtimeLogStreamWriter.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: [STAT] Reboot Successfully");
					break;
				}
			}
		}
		internal static void setMonitorStauts(bool isMonitor)
		{
			if (isMonitor)
			{
				if(ProcessLog.isMonitor){ Console.WriteLine("Monitor is already Opened!"); return; }
				ProcessLog.isMonitor = true;
				(threadMonitor = new Thread(thread_Monitor)).Start();
				// Console.WriteLine("Monitor Opened!");
			}
			else
			{
				ProcessLog.isMonitor = false;
				threadMonitor.Join();
				// Console.WriteLine("Monitor Closed!");
			}
		}
		//**----------------------------方法-----------------------------------------------------
		internal static void showPcsRunTime(string pcsName)
		{
			try
			{
				MySqlDataReader sqlReader = new MySqlCommand($"SELECT * FROM {pcsName} ORDER BY StartTime DESC LIMIT 1;", sqlCon).ExecuteReader();
				sqlReader.Read();
				Console.WriteLine($"{sqlReader.GetDateTime("StartTime")}-{sqlReader.GetDateTime("EndTime")} for {sqlReader.GetDecimal("LastTime")}min\n");
			}
			catch (Exception e)
			{
				//td输出异常
				Console.WriteLine(e.Message);
				return;
			}
			sqlCon.Close();
			sqlCon.Open();
		}
		public static string[] getAllPcsRunTime(string timeStr)
		{
			List<string> pcsList = new List<string>();
			MySqlConnection sqlConnection = new MySqlConnection($"Server=localhost;user=root;Database={MainProcess.config.DATABASE_NAME};port=3306;password=60017089;");
			List<string> res = new List<string>();
			sqlConnection.Open();
			MySqlDataReader sqlReader = new MySqlCommand($"SELECT TABLE_NAME FROM information_schema.tables WHERE TABLE_SCHEMA = '{MainProcess.config.DATABASE_NAME}';", sqlConnection).ExecuteReader();
			while (sqlReader.Read())
				pcsList.Add(sqlReader.GetString(0));
			try
			{
				foreach(var pcsName in pcsList)
				{
					res.Add(pcsName);
			sqlConnection.Close();sqlConnection.Open();
					sqlReader = new MySqlCommand($"SELECT * FROM {pcsName} WHERE StartTime >= '{timeStr} 00:40:00' AND StartTime < '{DateTime.Parse(timeStr).AddDays(1)} 00:04:00';", sqlConnection).ExecuteReader();
					while(sqlReader.Read())
					{
						res.Add(sqlReader.GetDateTime(0).ToString().Split(' ')[1]);
						res.Add(sqlReader.GetDateTime(1).ToString().Split(' ')[1]);
					}
				}
				sqlConnection.Close();
				return res.ToArray();
			}
			catch (Exception e)
			{
				//td输出异常
				Console.WriteLine(e.Message);
				sqlConnection.Close();
				return new string[]{};
			}
		}
		internal static void showAppMonitored()
		{
			if (pcsMntList.Count == 0)
			{
				//td输出
				Console.WriteLine("No app monitored!");
			}
			foreach (ProcessLog pcsCon in pcsMntList)
			{
				//td输出
				Console.WriteLine(pcsCon.pcsName);
			}
			Console.WriteLine();
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
			Console.WriteLine();
		}
		internal static void showRTLog()
		{
			Process notepadPcs = new Process();
			notepadPcs.StartInfo.FileName = "notepad";
			notepadPcs.StartInfo.Arguments = @$"""{MainProcess.config.RuntimeLogFileDir}"" /c";
			notepadPcs.Start();
		}
		internal static void addAppMonitored(string pcsName)
		{
			if (pcsMntBlackList.Contains(pcsName))
				pcsMntBlackList.Remove(pcsName);
			// Console.WriteLine("Monitor Running");
			File.WriteAllLinesAsync(MainProcess.config.ProcessListFileDir, pcsMntBlackList.ToArray<string>());//！好耶！[]和数组的相互转化！
			foreach (ProcessLog pcsCon in pcsMntList)
			{
				if (pcsCon.pcsName == pcsName)
				// throw new Exception("App already monitored!");
				{
					Console.WriteLine("App already monitored!"); return;
				}
			}
			new MySqlCommand($"CREATE TABLE IF NOT EXISTS {pcsName} LIKE longtimenoaction;", sqlCon).ExecuteNonQuery();//!艹这里不能用异步了…………
			new MySqlCommand($"INSERT INTO {pcsName} VALUES ('{DateTime.Now.ToLocalTime()}', '{DateTime.Now.ToLocalTime()}', 0);", sqlCon).ExecuteNonQueryAsync();
			//!可以多次cmd…………
			setMonitorStauts(false);
			Console.WriteLine("Rebooting...");
			runtimeLogStreamWriter.WriteLine($"{DateTime.Now}: [STAT] Monitor Reboot");
			pcsMntList.Add(new ProcessLog(pcsName));
			setMonitorStauts(true);

		}
		internal static void dropAppMonitored(string pcsName)
		{
			if (pcsMntBlackList.Contains(pcsName)) { Console.WriteLine("App already dropped!"); }//td输出
			else pcsMntBlackList.Add(pcsName);
			setMonitorStauts(false);
			var existApp = false;
			foreach (ProcessLog pcsCon in pcsMntList)
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
			if (existApp) File.WriteAllLinesAsync(MainProcess.config.ProcessListFileDir, pcsMntBlackList.ToArray<string>());
			else Console.WriteLine("App not found!");
			setMonitorStauts(true);
		}
		internal static void deleteAppMonitored(string pcsName)
		{
			var originStr = File.ReadAllLines(MainProcess.config.ProcessListFileDir);
			List<string> resStrList = new List<string>();
			foreach (var curStr in originStr)
				if (curStr != pcsName) resStrList.Add(curStr);
			File.WriteAllLines(MainProcess.config.ProcessListFileDir, resStrList.ToArray<string>());

			new MySqlCommand($"DROP TABLE IF EXISTS {pcsName};", sqlCon).ExecuteNonQueryAsync();
			Console.WriteLine($"App {pcsName} and its data has been deleted!");
		}
		void getPcsStatus()
		//**----------------------------理清思路-----------------------------------------------------
		//未启动：null, null, false				where Lengh = 0
		//启动：[startTime], null, [true]		where Judge
		//运行中：同
		//结束：startTime, [endTime], [false]	where Judge 写入and创建新的
		{
			var tmpPcsList = Process.GetProcessesByName(pcsName);
			if (tmpPcsList.Length == 0)
			{
				//**----------------------------关闭时-----------------------------------------------------
				shutdownPcs();
			}
			else
			{
				//**----------------------------启动时-----------------------------------------------------
				if (this.isRunning == false)
				{
					this.isRunning = true;

					MySqlConnection lastCon = new MySqlConnection($"server=localhost;user=root;database={MainProcess.config.DATABASE_NAME};port=3306;password=60017089");
					lastCon.Open();//麻了又忘
					MySqlDataReader lastDataReader = new MySqlCommand($"SELECT * FROM {pcsName} ORDER BY EndTime DESC LIMIT 1;", lastCon).ExecuteReader();
					lastDataReader.Read();
					if ((DateTime.Now - lastDataReader.GetDateTime("EndTime")).TotalMinutes < 5)
					{
						if (this.startTime == null || this.endTime == null)
						{
							this.startTime = lastDataReader.GetDateTime("StartTime");
							this.endTime = DateTime.Now.ToLocalTime();
						}
						new MySqlCommand($"UPDATE {pcsName} SET EndTime = '{this.endTime}', LastTime = {((DateTime)this.endTime - (DateTime)this.startTime).TotalMinutes} WHERE StartTime = '{this.startTime}';", sqlCon).ExecuteNonQueryAsync();
						Console.WriteLine($"{((DateTime)this.endTime).ToString("HH:mm:ss")}: [INFO] AppReboot {pcsName}");
						runtimeLogStreamWriter.WriteLine($"{((DateTime)this.endTime).ToString("yyyy-MM-dd HH:mm:ss")}: [INFO] AppReboot {pcsName}");
					}
					else
					{
						this.isRunning = true;
						this.startTime = DateTime.Now.ToLocalTime();
						this.endTime = null;
						new MySqlCommand($"INSERT INTO {pcsName} VALUES ('{this.startTime}', '{this.startTime}', 0);", sqlCon).ExecuteNonQueryAsync();
						Console.WriteLine($"{((DateTime)this.startTime).ToString("HH:mm:ss")}: [INFO] AppStart {this.pcsName}");
						runtimeLogStreamWriter.WriteLine($"{((DateTime)this.startTime).ToString("yyyy-MM-dd HH:mm:ss")}: [INFO] AppStart {this.pcsName}");
					}
				}
				//**----------------------------运行中-----------------------------------------------------
				else
				{
					this.endTime = DateTime.Now.ToLocalTime();
					if (this.startTime == null || this.endTime == null) { throw new Exception("Time Log Fail or Lost!"); }
					// td看看这句是否存在必要…………
					new MySqlCommand($"UPDATE {pcsName} SET EndTime = '{this.endTime}', LastTime = {((DateTime)this.endTime - (DateTime)this.startTime).TotalMinutes} WHERE StartTime = '{this.startTime}';", sqlCon).ExecuteNonQueryAsync();
				}
			}

		}
		void shutdownPcs(){
			if (this.isRunning == true){
				this.isRunning = false;
				this.endTime = DateTime.Now.ToLocalTime();
				if (this.startTime == null || this.endTime == null) { throw new Exception("Time Log Fail or Lost!"); }
				new MySqlCommand("UPDATE {pcsName} SET EndTime = '{this.curPcs.endTime}', LastTime = {((DateTime)this.curPcs.endTime - (DateTime)this.curPcs.startTime).TotalMinutes} WHERE StartTime = '{this.curPcs.startTime}'", sqlCon).ExecuteNonQueryAsync();//！这个是异步方法，实际使用复杂自己查了
				Console.WriteLine($"{((DateTime)this.endTime).ToString("HH:mm:ss")}: [INFO] AppEnd {pcsName}");
				runtimeLogStreamWriter.WriteLine($"{((DateTime)this.endTime).ToString("yyyy-MM-dd HH:mm:ss")}: [INFO] AppEnd {pcsName}");
				// Console.Out.Flush();
			}
		}
	}
}