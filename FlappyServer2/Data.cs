using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;
using System.Net;
using System.Threading;
using System.Net.Sockets;

namespace FlappyServer
{
    class Data
    {
        // Holds our connection with the database
        SQLiteConnection m_dbConnection;

        enum OperationCode
        {
            Insert = 1,
            UpdatePwd = 2,
            UpdateScore = 3,
            PrintScore = 5
        }

        static public void TestSQL(object opcode)
        {
            string str = (string)opcode;
            Data p = new Data(str[0] - '0', str.Substring(1));
        }

        public Data(int opcode, object str)
        {
            //createNewDatabase();
            ConnectToDatabase();
            //createTable();
            //FillTable();
            InsertScores("1", "22", "223");
            var Args = ((string)str).Split(' ');
            if(opcode == (int)OperationCode.PrintScore)
                PrintHighscores();
            
            switch(opcode)
            {
                case (int)OperationCode.Insert:
                    InsertData(Args[0], Args[1], Args[2], Args[3]);
                    break;
                case (int)OperationCode.UpdatePwd:
                    UpdatePassword(Args[0], Args[1]);
                    break;
                case (int)OperationCode.UpdateScore:
                    UpdateHighScore(Args[0], Args[1], Args[2]);
                    break;
                case (int)OperationCode.PrintScore:
                    PrintHighscores();
                    break;
            }
        }

        // Creates an empty database file
        void CreateNewDatabase()
        {
            SQLiteConnection.CreateFile("UserData.sqlite");
        }

        // Creates a connection with our database file.
        void ConnectToDatabase()
        {
            m_dbConnection = new SQLiteConnection("Data Source=UserData.sqlite;Version=3;");
            m_dbConnection.Open();
        }

        // Creates a table named 'highscores' with two columns: name (a string of max 20 characters) and score (an int)
        void CreateTable()
        {
            string sql = "create table highscores (name varchar(20), score int)";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }

        // Inserts some values in the highscores table.
        // As you can see, there is quite some duplicate code here, we'll solve this in part two.
        public static bool InsertData(string nickname, string password, string highscore_tube, string time)
        {
            bool flag = false;
            var m_dbConnection = new SQLiteConnection("Data Source=UserData.sqlite;Version=3;");
            m_dbConnection.Open();
            string sql = "insert into userdata (nickname, password, highscore_tube, highscore_time) values" +
                " ('"+nickname+"', '"+password+"', "+highscore_tube+", "+time+")";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            try
            {
                command.ExecuteNonQuery();
                flag = true;
            }
            catch(SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
                flag = false;
            }
            m_dbConnection.Close();
            return flag;
        }

        public static bool InsertScores(string id, string highscore_tube, string highscore_time)
        {
            using (var m_dbConnection = new SQLiteConnection("Data Source=UserData.sqlite;Version=3;"))
            {
                m_dbConnection.Open();
                bool flag = false;
                string sql = "select count(*) from scores where id = " + id + " order by score desc;";
                using (SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection))
                {
                    SQLiteDataReader reader = command.ExecuteReader();
                    int count;
                    reader.Read();
                    count = Convert.ToInt16(reader[0]);
                    reader.Close();
                    //Console.WriteLine("id: " + count);
                    if (count >= 10)
                    {
                        sql = "select * from scores where id = " + id + " order by score;";
                        SQLiteCommand command2 = new SQLiteCommand(sql, m_dbConnection);
                        reader = command2.ExecuteReader();
                        reader.Read();
                        string dateInt = reader[3].ToString();
                        reader.Close();
                        sql = "DELETE FROM scores WHERE id = " + id + " and date = " + dateInt + "";
                        command2 = new SQLiteCommand(sql, m_dbConnection);
                        try
                        {
                            command2.ExecuteNonQuery();
                            flag = true;
                        }
                        catch (SQLiteException ex)
                        {
                            Console.WriteLine(ex.Message);
                            flag = false;
                        }
                    }


                }

                InsertScore(m_dbConnection, id, highscore_tube, highscore_time);
                m_dbConnection.Close();
                return flag;
            }
        }

        public static bool InsertScore(SQLiteConnection m_dbConnection2, string id, string highscore_tube, string highscore_time)
        {
            bool flag = false;
            //m_dbConnection2.Open();
            string sql = "insert into scores(id, score, time, date) values" +
                "(" + id + ", " + highscore_tube + ", " + highscore_time + ", (strftime('%s', 'now')));";
            using (SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection2))
                try
                {
                    command.ExecuteNonQuery();
                    flag = true;
                }
                catch (SQLiteException ex)
                {
                    if (ex.Message == "database is locked\r\ndatabase is locked")
                        InsertScores(id, highscore_tube, highscore_time);

                    Console.WriteLine(ex.Message);
                    flag = false;
                }
            //m_dbConnection2.Close();
            return flag;

        }


        public static bool VarifyLogin(string nickname, string password)
        {
            bool flag = false;
            var m_dbConnection = new SQLiteConnection("Data Source=UserData.sqlite;Version=3;");
            m_dbConnection.Open();
            string sql = "select * from userdata where nickname = '" + nickname + "';";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            try
            {
                reader.Read();
                string pwd = reader[2].ToString();
                reader.Close();
                //Console.WriteLine("id: " + count);
                m_dbConnection.Close();
                if (password == pwd)
                    flag = true;
                else
                    flag = false;
            }
            catch(Exception ex)
            {
                m_dbConnection.Close();
                Console.WriteLine(ex.Message);
                flag = false;
            }
            return flag;
        }

        public static List<string> QueryLeadBorad()
        {
            var list = new List<string>();
            using (var m_dbConnection = new SQLiteConnection("Data Source=UserData.sqlite;Version=3;"))
            {
                m_dbConnection.Open();
                string sql = "select * from scores order by score desc;";
                using (SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection))
                {
                    SQLiteDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        list.Add(Program.GetUser(reader[0].ToString()));
                        list.Add(reader[1].ToString());
                        list.Add(reader[2].ToString());
                        list.Add(reader[3].ToString());
                    }
                    reader.Close();
                }
                m_dbConnection.Close();
            }
            return list;
        }

        void QueryScores()
        {
            string sql = "select * from userdata order by highscore_tube desc";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
                Console.WriteLine("Name: " + reader["nickname"] + "\tScore: " + reader["highscore_tube"]);
            reader.Close();
            Console.ReadLine();
        }
        void UpdatePassword(string nickname, string password)
        {
            string sql = "update userdata SET password = '"+password+"' WHERE nickname = '"+nickname+"'";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            try
            {
                command.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        void UpdateHighScore(string nickname, string highscore_tube, string time)
        {
            string sql = "update userdata SET highscore_tube = " + highscore_tube + ", highscore_time = "+time+" WHERE nickname = '" + nickname + "'";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            try
            {
                command.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        // Writes the highscores to the console sorted on score in descending order.
        void PrintHighscores()
        {
            string sql = "select * from userdata order by highscore_tube desc";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
                Console.WriteLine("Name: " + reader["nickname"] + "\tScore: " + reader["highscore_tube"]);
            reader.Close();
            Console.ReadLine();
        }
        

    }
}
