using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace Cognex.DataMan.SDK.Utils
{
    /// <summary>
    /// Utility class that contains methods for configuring TCP-level keep alive.
    /// </summary>
    public static class SocketKeepAlive
    {
        /// <summary>
        /// Configures the specified socket for keep alive.
        /// </summary>
        /// <param name="socket">The socket to be configured for keep alive.</param>
        /// <param name="enabled">True if keep alive is to be enabled, false otherwise.</param>
        /// <param name="timeout">The timeout in milliseconds that must ellapse from the last activity on the socket before keep alive frames are sent to the remote host.</param>
        /// <param name="interval">The frequency in milliseconds of the keep alive frames.</param>
        public static void SetKeepAliveOptions(Socket socket, bool enabled, int timeout, int interval)
        {
            byte[] v1 = BitConverter.GetBytes((int)(enabled ? 1 : 0));
            byte[] v2 = BitConverter.GetBytes(timeout);
            byte[] v3 = BitConverter.GetBytes(interval);
            byte[] values = new byte[3 * 4];

            Array.Copy(v1, 0, values, 0, v1.Length);
            Array.Copy(v2, 0, values, 4, v2.Length);
            Array.Copy(v3, 0, values, 8, v3.Length);

#if WindowsCE
            unchecked
            {
                socket.IOControl((int)2550136836, values, null);
            }
#else
            socket.IOControl(IOControlCode.KeepAliveValues, values, null);
#endif
        }
    }
}
