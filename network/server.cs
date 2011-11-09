/*
 * Copyright (c) 2010 Jopirop
 * 
 * All rights reserved.
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Cyclops {
    /// <summary>
    /// This class handles all incoming connection requests as
    /// well as starting the server.
    /// </summary>
    class Server2 {
        private GameWorld world;
        private TcpListener listener;
        private const ushort PROTO_SERVER_OLD = 0x0101;
        private const ushort PROTO_SERVER_NEW = 0x0201;
        private const ushort PROTO_PLAYER_OLD = 0x0000;
        private const ushort PROTO_PLAYER_NEW = 0x020A;
        private const string INVALID_NAME_OR_PW = "Invalid username or password.";

        /// <summary>
        /// Accept incoming connection requests.
        /// </summary>
        private void AcceptConnections() {
            Player player = new Player(null);
            player.Name = "Remyx";
            player.Password = "remyx";
            player.CurrentPosition = new Position(32369, 32241, 7);
            player.MaxCapacity = 420; //TODO: Verify if correct
            player.MagicLevel = 1;
            player.MaxHP = 150;
            player.CurrentHP = 150;
            player.MaxMana = 0;
            player.CurrentMana = 0;
            player.CurrentVocation = Vocation.NONE;
            player.BaseSpeed = 220;
            player.SavePlayer();
            while (true) {
                Console.WriteLine("accept connections");
                //try {
                    HandleConnection();
                //} catch (Exception e) {
                //    Tracer.Println(e.ToString());
                //}
            }
        }
        /*private void HandleAccountConnection(NetworkMessage netmsg,
            ProtocolReceive protoReceive, ProtocolSend protoSend) {
            Account act = protoReceive.HandleAccountLogin(netmsg);
            if (act == null) { //invalid login
                protoSend.Reset();
                protoSend.AddSorryBox(INVALID_NAME_OR_PW);
                protoSend.WriteToSocket();
                return;
            }
            protoSend.Reset();
            protoSend.AddCharacterList(act.GetCharList(), Config.GetWorldName(),
                0x0100007F *//*TODO: FINISH *//*, 7171 *//*TODO: FINISH *//*, 0);
            protoSend.WriteToSocket();
        }*/

        private void HandlePlayerConnection(Socket socket, 
            ProtocolReceive protoReceive, ProtocolSend protocolSend) {
            LoginInfo loginInfo = protoReceive.HandlePlayerLogin(socket);
            GameWorld localWorld = world; //The gameworld to be used when logging in
            string name = loginInfo.GetUsername();
            string pw = loginInfo.GetPassword();
            Console.WriteLine("name: " + name);
            Console.WriteLine("pw: " + pw);
            /*bool isActManager = (name == Constants.ACT_MANAGER_NAME);
            if (isActManager) {
                //If the login is to the account manager, create an account manager world
                //and use that as the login world instead.
                localWorld = new ManagerWorld();
            }*/

            //TODO: Kick current player and let this one login
            if (localWorld.IsPlayerOnline(loginInfo.GetUsername())) {
                protocolSend.Reset();
                protocolSend.AddSorryBox("A player with this name is already online.");
                protocolSend.MarkSocketAsClosed();
                protocolSend.WriteToSocket();
                return;
            }

            Player player = new Player(protocolSend);
            bool successful = /*isActManager ||*/ player.LoadPlayer(loginInfo);
            if (!successful) {
                protocolSend.Reset();
                protocolSend.AddSorryBox(INVALID_NAME_OR_PW);
                protocolSend.MarkSocketAsClosed();
                protocolSend.WriteToSocket();
                return;
            }
            Console.WriteLine("almost ");
            localWorld.SendAddPlayer(player, player.CurrentPosition);
            protoReceive.StartReceiving(world, player);
            //Create a thread to handle player messages
            //new PlayerThread(player, localWorld, protoReceive).StartThread();
        }

        /// <summary>
        /// Handle incoming connection requests.
        /// </summary>
        private void HandleConnection() {
            Socket socket = listener.AcceptSocket();
            HandlePlayerConnection(socket, new ProtocolReceive65(socket), new ProtocolSend65(socket));
            /*NetworkMessage netmsg = new NetworkMessage(socket, 2);
            netmsg.ReadFromSocket();
            ushort connectionType = netmsg.GetU16();
            switch (connectionType) {
                /*case PROTO_SERVER_NEW:
                    HandleAccountConnection(netmsg, new ProtocolReceive71(netmsg),
                        new ProtocolSend71(netmsg)); 
                    break;
                case PROTO_SERVER_OLD:
                    HandleAccountConnection(netmsg, new ProtocolReceive65(socket),
                        new ProtocolSend65(socket));
                    break;
                case PROTO_PLAYER_NEW:
                    HandlePlayerConnection(socket, new ProtocolReceive71(netmsg),
                        new ProtocolSend71(netmsg));
                    break;*/
               /* case PROTO_PLAYER_OLD:
                    HandlePlayerConnection(socket, new ProtocolReceive65(socket),
                        new ProtocolSend65(socket));
                    break;
            }*/
        }
    
        

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Server2() {
        }

        /// <summary>
        /// Start the server.
        /// </summary>
        public void Start() {
            DateTime start = System.DateTime.Now;
            Tracer.Print("Loading items...");
            Item.LoadItems();
            Tracer.Println(" Done");

            Tracer.Print("Loading spells...");
            Spell.Load();
            Tracer.Println(" Done");

            Tracer.Print("Loading monsters...");
            Monster.Load();
            Tracer.Println(" Done");

            Tracer.Print("Loading commands...");
            Command.Load();
            Tracer.Println(" Done");

            Tracer.Print("Loading game map...");
            Map map = Map.Load();
            world = new GameWorld(map);
            Tracer.Println(" Done");

            Tracer.Print("Loading NPCs...");
            NPC.Load();
            List<NPC> allNPCs = NPC.GetAllNPCs();
            foreach (NPC npc in allNPCs) {
                world.SendAddNPC(npc, npc.CurrentPosition);
            }
            Tracer.Println(" Done");

            Tracer.Print("Loading spawns...");
            Respawn.Load(world);
            Tracer.Println(" Done");
			
			
			
            /*//TODO: Remove
            Tracer.Println("Setting account manager's number to 1 and pw to null");
            AccountManager.ManagerName = "1";
            AccountManager.ManagerPassword = null;*/

            Tracer.Print("Starting connection listener...");
            
            //Start the server listener
            listener = new TcpListener(IPAddress.Any, Config.GetPort());
            listener.Start();
            Tracer.Println(" Done");
            string timeToStart = (DateTime.Now - start) + "";
            Tracer.Println("Server is now fully running. Time to start: " 
                + timeToStart + " seconds");
            AcceptConnections();
        }
    }
}
