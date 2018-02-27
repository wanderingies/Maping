using System.Net;

namespace PortMaping
{
    internal struct WorkGroup
    {
        #region Data Members (4)

        public int _id;
        public string _server;
        public EndPoint _point_in;
        public string _point_out_host;
        public ushort _point_out_port;

        #endregion Data Members
    }
}
