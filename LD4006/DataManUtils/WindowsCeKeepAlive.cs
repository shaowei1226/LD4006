using System;
using System.Collections.Generic;
using System.Text;
using Cognex.DataMan.SDK;

namespace Cognex.DataMan.SDK.Utils
{
    /// <summary>
    /// Utility class that implements keep alive functionality on Windows CE where TCP level keep alive is not available.
    /// </summary>
    public class WindowsCeKeepAlive
    {
        private DataManSystem _hostDmSystem;
        private DataManSystem _keepAliveDmSystem;
        private int _timeout;
        private int _interval;

        /// <summary>
        /// Constructor that creates a new WindowsCeKeepAlive object with the specified parameters.
        /// </summary>
        /// <param name="system">The remote system for which keep alive functionality is enabled.</param>
        /// <param name="timeout">The timeout in milliseconds that must ellapse from the last activity on the socket before keep alive frames are sent to the remote host.</param>
        /// <param name="interval">The frequency in milliseconds of the keep alive frames.</param>
        public WindowsCeKeepAlive(DataManSystem system, int timeout, int interval)
        {
            _hostDmSystem = system;
            _timeout = timeout;
            _interval = interval;

            _hostDmSystem.SystemConnected += new SystemConnectedHandler(OnHostSystemConnected);
            _hostDmSystem.SystemDisconnected += new SystemDisconnectedHandler(OnSystemDisconnected);
        }

        private void OnHostSystemConnected(object sender, EventArgs args)
        {
            if (_keepAliveDmSystem == null)
            {
                EthSystemConnector connector = null;

                if (_hostDmSystem.Connector is EthSystemConnector)
                {
                    var c = _hostDmSystem.Connector as EthSystemConnector;

                    connector = new EthSystemConnector(c.Address, c.Port);
                    connector.UserName = c.UserName;
                    connector.Password = c.Password;

                    _keepAliveDmSystem = new DataManSystem(connector);
                    _keepAliveDmSystem.SetResultTypes(ResultTypes.None);
                    _keepAliveDmSystem.SetKeepAliveOptions(true, _timeout, _interval);
                }
            }

            try
            {
                _keepAliveDmSystem.SystemDisconnected += new SystemDisconnectedHandler(OnSystemDisconnected);
                _keepAliveDmSystem.Connect();
            }
            catch
            {
                _keepAliveDmSystem = null;
            }                                     
        }

        private void OnSystemDisconnected(object sender, EventArgs args)
        {
            _keepAliveDmSystem.SystemDisconnected -= OnSystemDisconnected;
            _keepAliveDmSystem.Disconnect();

            _hostDmSystem.Disconnect();
        }
    }
}
