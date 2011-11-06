using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Cyclops {
    public abstract class ProtocolReceive {
        protected NetworkMessage netmsg;
        protected static object lockStatic = new object();

        /// <summary>
        /// Print a message's header along with its message body, in hex.
        /// </summary>
        /// <param name="netmsg">A reference to the netmsg.</param>
        /// <param name="header">The header sent.</param>
        protected void PrintHeader(NetworkMessage netmsg, ushort header) {
            lock (lockStatic) {
                string hexString = String.Format("{0:x2}", header);
                Tracer.Println("Unknown byte header: 0x" + hexString);
                Tracer.Print("Bytes:");
                for (int i = 0; i < netmsg.GetMessageLength() - 1; i++) {
                    Tracer.Print(" 0x" + String.Format("{0:x2}", netmsg.GetByte()));
                }
                Tracer.Println("");
            }
        }

        /*/// <summary>
        /// Returns an account if the login was valid.
        /// Returns null if the login was not valid.
        /// </summary>
        /// <param name="networkmsg"></param>
        /// <returns></returns>
        public abstract Account HandleAccountLogin(NetworkMessage networkmsg);*/
        public abstract LoginInfo HandlePlayerLogin(Socket s);
        //public abstract bool ProcessNextMessage(Player player, GameWorld world);
        /// <summary>
        /// Stars listening asynchronously and handles the messages as needed. 
        /// </summary>
        public virtual void StartReceiving(GameWorld world, Player player) { }
    }
}
