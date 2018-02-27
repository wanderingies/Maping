
using System;
namespace PortMaping
{
    internal class WorkStatus
    {
        #region Fields (5)

        public int _connect;
        public string _server;
        public string _point_in;
        public string _point_out;        
        public static string _print_head = string.Format("{0}{1}{2}{3}{4}{5}{6}", "服务".PadRight(18, ' '), "输入".PadRight(18, ' '), "输出".PadRight(23, ' '), "状态".PadRight(10, ' '), "连接".PadRight(6, ' '), "接收".PadRight(9, ' '), "发送");
        public bool _running;
        public long _bytes_send;
        public long _bytes_recv;

        #endregion Fields

        #region Constructors (1)

        public WorkStatus(string server, string point_in, string point_out, bool running, int connect_cnt, int bytes_send, int bytes_recv)
        {
            _server = server;
            _point_in = point_in;
            _point_out = point_out;
            _running = running;
            _connect = connect_cnt;
            _bytes_recv = bytes_recv;
            _bytes_send = bytes_send;
        }

        #endregion Constructors

        #region Methods (1)

        // Public Methods (1) 

        public override string ToString()
        {
            return string.Format("{0}{1}{2}{3}{4}{5}{6}", _server.PadRight(20, ' '), _point_in.PadRight(20, ' '), _point_out.PadRight(25, ' '), (_running ? "运行中  " : "启动失败").PadRight(9,' '), _connect.ToString().PadRight(8, ' '), Math.Round((double)_bytes_recv / 1024) + "k".PadRight(10,' ') , Math.Round((double)_bytes_send / 1024) + "k");
        }

        public string ToString(long _long)
        {
            return string.Format("{0}", Math.Round((double)_long / 1024) + "k");
        }

        #endregion Methods
    }
}
