using System;
using System.Diagnostics;
using System.Formats.Asn1;
using MySqlConnector;//！这句开始无法识别要在命令行：dotnet add package MySqlConnector

namespace MySqlTestSpace
{
	class Program
	{
			static MySqlConnection sqlCon = new MySqlConnection("server=localhost;user=root;database=world;port=3306;password=60017089");
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

		internal string pcsName = "";

		internal ProcessLog curPcs = new ProcessLog();
		internal static void mainLoop()
		{

			//~~ List<ProcessLog> processLogList = new List<ProcessLog>();
			//!不应该用List而是直接导入DB，不然运行久了卡死你。
			ProcessControl[] pcsMntList = { new ProcessControl("Code"), new ProcessControl("flomo") };//!所以这个依然是应用名不过不要exe而已

			for(; ; Thread.Sleep(100))//
			foreach(ProcessControl pcsCon in pcsMntList)
			{
				pcsCon.getPcsStatus();
			}
			//!先阻塞吧感觉好像也行
			//td加一个check，如果两次关闭开启小于3min就不多记一次了
			//td考虑电脑睡眠后的处理
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
			if(tmpPcsList.Length == 0)
			{
				if(this.curPcs.isRunning == true)
				{
					// pcs.endTime = new DateTime().ToLocalTime();//！错误写法，会表示1900年…………
					this.curPcs.endTime = DateTime.Now.ToLocalTime();
					if(this.curPcs.startTime == null || this.curPcs.endTime == null){ throw new Exception("Time Log Fail or Lost!"); }
					//td写入数据库
					Console.WriteLine($"{pcsName} used time: {this.curPcs.startTime} ~ {this.curPcs.endTime}");//!ToString("yyyy-MM-dd HH:mm:ss")}但是似乎不太行
					this.curPcs = new ProcessLog();
				}
			}
			else
			{
				if(this.curPcs.isRunning == false)
				{
					this.curPcs.isRunning = true;
					this.curPcs.startTime = DateTime.Now.ToLocalTime();
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