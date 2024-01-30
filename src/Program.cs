using System;
using MySqlConnector;//！这句开始无法识别要在命令行：dotnet add package MySqlConnector

namespace MySqlTestSpace
{
	class Program
	{
			static MySqlConnection sqlCon = new MySqlConnection("server=localhost;user=root;database=world;port=3306;password=60017089");
		static void Main()
		{
			try
			{
				Console.WriteLine("Connecting to MySQL..");
				sqlCon.Open();
				MySqlCommand sqlCmd = new MySqlCommand();
				//~~这个不太能放到外面因为要构造
				sqlCmd.Connection = sqlCon;
				sqlCmd.CommandText = "SELECT Name, HeadOfState FROM Country WHERE Continent='Oceania'";
				// sqlCmd.CommandText = "SELECT * FROM city WHERE ID = 1";
				MySqlDataReader sqlReader = sqlCmd.ExecuteReader();

				while(sqlReader.Read())
				{
					Console.WriteLine($"{sqlReader[0]} -- {sqlReader[1]}");
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}

			sqlCon.Close();
			Console.WriteLine("Done");
		}
	}
}