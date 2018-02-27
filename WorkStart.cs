using System;
using System.Net;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PortMaping
{
    internal class program
    {
        #region Fields (4)

        private static int _pointPos = -1;
        private static Point _point = new Point(77, 3);

        private static string local = string.Format("{0}", "*");
        private static string remot = string.Format("{0}", "192.168.1.9");

        private static readonly Dictionary<int, WorkStatus> _stat_info = new Dictionary<int, WorkStatus>();
        #endregion Fields

        #region Methods (8)

        private static List<string> _mapping = new List<string>()
        {
            string.Format("link|{0}:29000|{1}:29000", local, remot),
            string.Format("deliveryd|{0}:29100|{1}:29100", local, remot),
            string.Format("auth|{0}:29200|{1}:29200", local, remot),
            string.Format("deliveryd|{0}:29300|{1}:29300", local, remot),
            string.Format("link|{0}:29301|{1}:29301", local, remot),
            string.Format("link|{0}:29302|{1}:29302", local, remot),
            string.Format("link|{0}:29303|{1}:29303", local, remot),
            string.Format("link|{0}:29304|{1}:29304", local, remot),
            string.Format("gamedbd|{0}:29400|{1}:29400", local, remot),
            string.Format("uniquenamed|{0}:29401|{1}:29401", local, remot),
            string.Format("factiond|{0}:29500|{1}:29500", local, remot),
            string.Format("deliveryd|{0}:29600|{1}:29600", local, remot),
            string.Format("mysql|{0}:3306|{1}:3306", local, remot),            
        };

        private static int _id_plus = 0;
        private static List<WorkGroup> load_maps_cfg()
        {
            List<WorkGroup> rtn = new List<WorkGroup>();
            WorkGroup rtn_item;
            foreach (string item in _mapping)
            {
                string[] temp = item.Split(new[] { '|' });

                rtn_item = new WorkGroup { _id = ++_id_plus };
                rtn_item._server = temp[0];

                ushort port;
                IPAddress ip;
                for (int i = 0; i < 2; i++)
                {
                    
                    string[] iport = temp[i + 1].Split(':');

                    ip = IPAddress.Any;
                    port = ushort.Parse(iport[1]);

                    if (i == 0)
                    {
                        rtn_item._point_in = new IPEndPoint(ip, port);
                    }
                    else
                    {
                        rtn_item._point_out_host = iport[0];
                        rtn_item._point_out_port = ushort.Parse(iport[1]);
                    }
                }

                rtn.Add(rtn_item);
            }

            return rtn;
        }

        private static void Main(string[] args)
        {
            List<WorkGroup> maps_list;
            try
            {
                maps_list = load_maps_cfg();
            }
            catch (Exception exp)
            {
                Console.WriteLine(program_ver);
                Console.WriteLine(exp.Message);
                system("pause");
                return;
            }
            foreach (var map_item in maps_list)
            {
                map_start(map_item);
            }

            Console.Title = program_ver;
            Console.CursorVisible = false;

            StringBuilder curr_buf = new StringBuilder();
            curr_buf.AppendLine(program_ver);
            curr_buf.AppendLine(program_link);
            curr_buf.AppendLine(WorkStatus._print_head);

            foreach (KeyValuePair<int, WorkStatus> item in _stat_info)
                curr_buf.AppendLine(item.Value.ToString());

            curr_buf.AppendLine(program_link);
            curr_buf.AppendLine("本机地址： " + GetInternalIP());
            Console.WriteLine(curr_buf);

            while (true)
                show_stat();
        }

        private static void map_start(WorkGroup work)
        {
            Socket sock_svr = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            bool start_error = false;
            try
            {
                sock_svr.Bind(work._point_in);
                sock_svr.Listen(10);
                sock_svr.BeginAccept(on_local_connected, new object[] { sock_svr, work });
            }
            catch (Exception exp)
            {
                start_error = true;
            }
            finally
            {
                _stat_info.Add(work._id, new WorkStatus(work._server, work._point_in.ToString(), work._point_out_host + ":" + work._point_out_port, !start_error, 0, 0, 0));
            }
        }

        private static void on_local_connected(IAsyncResult ar)
        {
            object[] ar_arr = ar.AsyncState as object[];
            Socket sock_svr = ar_arr[0] as Socket;
            WorkGroup work = (WorkGroup)ar_arr[1];

            ++_stat_info[work._id]._connect;
            Socket sock_cli = sock_svr.EndAccept(ar);
            sock_svr.BeginAccept(on_local_connected, ar.AsyncState);
            Socket sock_cli_remote = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                sock_cli_remote.Connect(work._point_out_host, work._point_out_port);
            }
            catch (Exception exp)
            {
                try
                {
                    sock_cli.Shutdown(SocketShutdown.Both);
                    sock_cli_remote.Shutdown(SocketShutdown.Both);
                    sock_cli.Close();
                    sock_cli_remote.Close();
                }
                catch (Exception) { }
                --_stat_info[work._id]._connect;
                return;
            }
            Thread t_send = new Thread(new ParameterizedThreadStart(recv_and_send_caller)) { IsBackground = true };
            Thread t_recv = new Thread(new ParameterizedThreadStart(recv_and_send_caller)) { IsBackground = true };
            t_send.Start(new object[] { sock_cli, sock_cli_remote, work._id, true });
            t_recv.Start(new object[] { sock_cli_remote, sock_cli, work._id, false });
            t_send.Join();
            t_recv.Join();
            --_stat_info[work._id]._connect;
        }

        private static void recv_and_send(Socket from_sock, Socket to_sock, Action<int> send_complete)
        {
            byte[] recv_buf = new byte[4096];
            int recv_len;
            while ((recv_len = from_sock.Receive(recv_buf)) > 0)
            {
                to_sock.Send(recv_buf, 0, recv_len, SocketFlags.None);
                send_complete(recv_len);
            }
        }

        private static void recv_and_send_caller(object thread_param)
        {
            object[] param_arr = thread_param as object[];
            Socket sock1 = param_arr[0] as Socket;
            Socket sock2 = param_arr[1] as Socket;
            try
            {
                recv_and_send(sock1, sock2, bytes =>
                {
                    WorkStatus stat = _stat_info[(int)param_arr[2]];
                    if ((bool)param_arr[3])
                        stat._bytes_send += bytes;
                    else
                        stat._bytes_recv += bytes;
                });
            }
            catch (Exception exp)
            {
                try
                {
                    sock1.Shutdown(SocketShutdown.Both);
                    sock2.Shutdown(SocketShutdown.Both);
                    sock1.Close();
                    sock2.Close();
                }
                catch (Exception) { }
            }
        }

        private static void show_stat()
        {
            _pointPos = -1;
            foreach (KeyValuePair<int, WorkStatus> item in _stat_info)
            {
                ++_pointPos;

                Console.SetCursorPosition(_point.X, _point.Y + _pointPos);
                Console.Write(item.Value._connect.ToString());

                Console.SetCursorPosition(_point.X + 8, _point.Y + _pointPos);
                Console.Write(item.Value.ToString(item.Value._bytes_recv));

                Console.SetCursorPosition(_point.X + 19, _point.Y + _pointPos);
                Console.Write(item.Value.ToString(item.Value._bytes_send));
            }           
        }

        [DllImport("msvcrt.dll")]
        private static extern bool system(string str);

        #endregion Methods

        private const string program_ver = @"Link Port Mapping(1.0) By Wanderingies";
        private const string program_link = "----------------------------------------------------------------------------------------------------------";

        private static string GetInternalIP()
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily.ToString() == "InterNetwork")
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }
    }
}
