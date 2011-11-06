/*
 * Copyright (c) 2010 Jopirop
 * 
 * All rights reserved.
 * 
 */
/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Cyclops {
    public class PlayerThread {
        private Player player;
        private GameWorld world; //A reference to the game world
        private ProtocolReceive protoReceive;

        private void Run() {
            for (; ; ) {
               // try {
                    lock (this) { //TODO: Fix
                        bool proceed = false; // protoReceive.ProcessNextMessage(player, world);
                       if (!proceed) {
                           return;
                       }
                    }
               // } catch (Exception e) {
                //    Tracer.Print(e.ToString());
               //     return;
               // }
            }
        }

        public PlayerThread(Player nPlayer, GameWorld gameWorld,
            ProtocolReceive protocolReceive) {
            player = nPlayer;
            world = gameWorld;
            protoReceive = protocolReceive;
        }

        public void StartThread() {
            new Thread(Run).Start();
        }
    }
}
*/