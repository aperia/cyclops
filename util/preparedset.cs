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

namespace Cyclops {
    /// <summary>
    /// A basic wrapper used for checking which player
    /// had their Prepare() method called. Used mainly for keeping
    /// track of adding protocol messages before sending them.
    /// </summary>
    public class PreparedSet {
        //Stores players
        private HashSet<Player> players = new HashSet<Player>();

        public HashSet<Player> GetThings() {
            return players;
        }

        public void AddPlayer(Player playerToAdd) {
            if (!players.Contains(playerToAdd)) {
                playerToAdd.ResetNetMessages();
                players.Add(playerToAdd);
            }
        }

        public void Clear() {
            players.Clear();
        }
    }
}
