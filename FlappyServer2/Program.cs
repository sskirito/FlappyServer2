using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace FlappyServer
{
    class Program
    {
        private static byte[] result = new byte[128];
        private static int myProt = 8885;   //端口  
        static Socket serverSocket;
        static void Main(string[] args)
        {
            //RSA.en();
            //Login("2","2");
            //Data.VarifyLogin("1111", "qqq");
            //GetId("1111");
            //Data.InsertScores("3", "3", "11");
            //var list = Data.QueryLeadBorad();
            //foreach (var i in list)
            //    Console.WriteLine(i);
            CreateKey();
            RunServer();

            EncryptSend(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), "Login 1111 qqq");
            Console.ReadLine();
        }

        static public void RunServer()
        {
            //服务器IP地址  
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(ip, myProt));  //绑定IP地址：端口  
            serverSocket.Listen(10);    //设定最多10个排队连接请求  
            Console.WriteLine("启动监听{0}成功", serverSocket.LocalEndPoint.ToString());
            //通过Clientsoket发送数据  
            Thread myThread = new Thread(ListenClientConnect);
            myThread.Start();
            Console.ReadLine();
        }

        /// <summary>  
        /// 监听客户端连接  
        /// </summary>  
        private static void ListenClientConnect()
        {
            while (true)
            {
                Socket clientSocket = serverSocket.Accept();
                //clientSocket.Send(Encoding.ASCII.GetBytes("Server Say Hello"));
                Thread receiveThread = new Thread(ReceiveMessage);
                receiveThread.Start(clientSocket);
            }
        }

        /// <summary>  
        /// 接收消息  
        /// </summary>  
        /// <param name="clientSocket"></param>  
        private static void ReceiveMessage(object clientSocket)
        {
            Socket myClientSocket = (Socket)clientSocket;
            string currentUser = "";
            int cnt = 0;
            while (true)
            {
                try
                {
                    //通过clientSocket接收数据  
                    int receiveNumber = myClientSocket.Receive(result);
                    if (receiveNumber == 0)
                        throw new Exception("Client disconnected");
                    string msg = Encoding.ASCII.GetString(result, 0, receiveNumber);
                    try { msg = Decrypt(result); }
                    catch
                    {
                        SendPublic(myClientSocket);
                    }
                    Console.WriteLine("接收客户端" + currentUser + "{0}消息{1}", myClientSocket.RemoteEndPoint.ToString(), msg);
                    cnt++;
                    Thread sqql = new Thread(Data.TestSQL);
                    string str = "8";
                    
                    string[] messages = msg.Split(' ');

                    if (str == "6")
                    {
                        int id = 1;
                        sqql = new Thread(delegate () {

                            var list = GetUserInfo(currentUser);

                            myClientSocket.Send(Encoding.ASCII.GetBytes(list[0] + "|" + list[1] + "|" + list[2]));
                            for (int i = 3; i < list.Count; i += 3)
                            {
                                myClientSocket.Send(Encoding.ASCII.GetBytes(list[i] + "|" + list[i + 1] + "|" + list[i + 2]));
                            }
                        });
                        sqql.Start();
                    }
                    else if (messages[0] == "Login")
                    {
                        if (!Data.VarifyLogin(messages[1], messages[2]))
                        {
                            myClientSocket.Send(Encoding.ASCII.GetBytes("Login Failure"));
                        }
                        else
                        {
                            //myClientSocket.Send(Encoding.ASCII.GetBytes("Login Success"));
                            currentUser = messages[1];
                            var list = GetUserInfo(currentUser);
                            string scores = "Login Success|";
                            for (int i = 3; i < list.Count; i += 3)
                            {
                                scores += list[i] + "|" + list[i + 1] + "|" + list[i + 2] + "|";
                            }
                            myClientSocket.Send(Encoding.ASCII.GetBytes(scores));
                            Console.WriteLine(scores);
                        }
                    }
                    else if (messages[0] == "Reg")
                    {
                        if (!Reg(messages[1], messages[2]))
                        {
                            myClientSocket.Send(Encoding.ASCII.GetBytes("Sign In Failure"));
                        }
                        else
                        {
                            currentUser = messages[1];
                            myClientSocket.Send(Encoding.ASCII.GetBytes("Sign In Success"));
                        }
                    }
                    else if (messages[0] == "Scores")
                    {
                        string id = GetId(currentUser);
                        Data.InsertScores(id, messages[1], messages[2]);
                    }
                    else if (messages[0] == "LB")
                    {
                        string scores = "LB Success|";
                        var list = Data.QueryLeadBorad();
                        foreach (var i in list)
                        {
                            scores += i + "|";
                        }
                        myClientSocket.Send(Encoding.ASCII.GetBytes(scores));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    myClientSocket.Shutdown(SocketShutdown.Both);
                    myClientSocket.Close();
                    break;
                }
            }
            Console.WriteLine(cnt);
        }

        static string GetId(string nickname)
        {
            using (var m_dbConnection = new SQLiteConnection("Data Source=UserData.sqlite;Version=3;"))
            {
                m_dbConnection.Open();
                string id = "";
                string sql = "select * from userdata where nickname = '" + nickname + "'";
                using (SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection))
                {
                    SQLiteDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        id = reader["id"].ToString();
                    }
                    reader.Close();
                    m_dbConnection.Close();
                    return id;
                }
            }
        }

        public static string GetUser(string id)
        {
            using (var m_dbConnection = new SQLiteConnection("Data Source=UserData.sqlite;Version=3;"))
            {
                m_dbConnection.Open();
                string username = "";
                string sql = "select * from userdata where id = " + id + "";
                using (SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection))
                {
                    SQLiteDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        username = reader["nickname"].ToString();
                    }
                    reader.Close();
                    m_dbConnection.Close();
                    return username;
                }
            }
        }

        static List<string> GetUserInfo(string nickname)
        {
            var m_dbConnection = new SQLiteConnection("Data Source=UserData.sqlite;Version=3;");
            m_dbConnection.Open();
            List<string> list = new List<string>();
            string sql = "select * from userdata where nickname = '" + nickname + "'";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            int id = 0;
            while (reader.Read())
            {
                id = Convert.ToInt16(reader["id"]);
                list.Add(reader["nickname"].ToString());
                list.Add(reader["highscore_tube"].ToString());
                list.Add(reader["highscore_time"].ToString());
                Console.WriteLine("Name: " + reader["nickname"] + "\tScore: " + reader["highscore_tube"]);
            }
            reader.Close();
            sql = "select score, time, datetime(date,'unixepoch', 'localtime') from scores where id = " + id + " order by score;";
            command = new SQLiteCommand(sql, m_dbConnection);
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(reader[0].ToString());
                list.Add(reader[1].ToString());
                list.Add(reader[2].ToString());
                //Console.WriteLine("Name: " + reader["nickname"] + "\tScore: " + reader["highscore_tube"]);
            }
            reader.Close();
            m_dbConnection.Close();
            return list;
        }

        static bool Reg(string nickname, string pwd)
        {
            bool flag;
            flag = Data.InsertData(nickname, pwd, "0", "0");
            return flag;
        }

        static string publicKey, privateKey;

        static void CreateKey()
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            using (StreamWriter writer = new StreamWriter("PrivateKey.xml"))  //这个文件要保密...
            {
                privateKey = rsa.ToXmlString(true);
                //writer.WriteLine(rsa.ToXmlString(true));
            }
            using (StreamWriter writer = new StreamWriter("PublicKey.xml"))
            {
                publicKey = rsa.ToXmlString(false);
                //writer.WriteLine(rsa.ToXmlString(false));

            }
        }

        static void SendPublic(Socket client)
        {
            client.Send(Encoding.ASCII.GetBytes("RSA " + publicKey));
        }

        static void EncryptSend(Socket client, string msg)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(publicKey);
            byte[] ciphertext =
                rsa.Encrypt(System.Text.Encoding.UTF8.GetBytes(msg), false);
            rsa.FromXmlString(privateKey);
            byte[] decryption =
                rsa.Decrypt(ciphertext, false); //解密后 

            string hhh = Convert.ToBase64String(ciphertext);
            string hah = System.Text.Encoding.UTF8.GetString(decryption);
            Console.WriteLine(hhh);
            Console.WriteLine(hah);
        }

        static string Decrypt(byte[] msg)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(privateKey);
            byte[] decryption =
                rsa.Decrypt(msg, false); //解密后 
            return System.Text.Encoding.UTF8.GetString(decryption);
        }
    }

}
