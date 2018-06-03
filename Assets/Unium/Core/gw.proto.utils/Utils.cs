﻿// Copyright (c) 2017 Gwaredd Mountain, https://opensource.org/licenses/MIT

//#define GW_LOGGING

using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace gw.proto.utils
{
    public static class Util
    {
        public static Random Rnd = new Random();
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";

        public static string RandomString( int length = 8 )
        {
            return new string( Enumerable.Repeat( chars, length ).Select( s => s[ Rnd.Next( s.Length ) ] ).ToArray() );
        }

        [Conditional("GW_LOGGING")]
        public static void Print( string msg, params object[] args )
        {
            UnityEngine.Debug.Log( string.Format( "[{0:HH:mm:ss.ffff}] {1}", DateTime.Now, string.Format( msg, args ) ) );
        }

        [Conditional( "GW_LOGGING" )]
        public static void Warn( string msg, params object[] args )
        {
            UnityEngine.Debug.LogWarning( string.Format( "[{0:HH:mm:ss.ffff}] {1}", DateTime.Now, string.Format( msg, args ) ) );
        }

        [Conditional( "GW_LOGGING" )]
        public static void Error( string msg, params object[] args )
        {
            UnityEngine.Debug.LogError( string.Format( "[{0:HH:mm:ss.ffff}] {1}", DateTime.Now, string.Format( msg, args ) ) );
        }

        public static IPAddress GetIPAddress()
        {
            if( System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() == false )
            {
                return IPAddress.Any; // 0.0.0.0
            }

            var host = Dns.GetHostEntry( Dns.GetHostName() );
            return host.AddressList.LastOrDefault( ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip) );
        }
    }
}
