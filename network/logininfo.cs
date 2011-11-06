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
    /*
     * This is the class the holds the login info (username, password)
     * of a player attemping to connect to the server. Note: The 
     * username and password do not have to be valid.
     */

    /// <summary>
    /// This is the class that holds the login information 
    /// of a player attempting to connect to the server. Note: The username
    /// and password are the ones being attemped, so they could be invalid.
    /// </summary>
    public class LoginInfo {
        private string name;
        private string pw;

        /// <summary>
        /// LoginInfo constructor.
        /// </summary>
        /// <param name="username">The username of this instance.</param>
        /// <param name="password">The password of this instance.</param>
        public LoginInfo(string username, string password) {
            name = username;
            pw = password;
        }

        /// <summary>
        /// Get the password assigned to this instance.
        /// </summary>
        /// <returns>The password.</returns>
        public string GetPassword() {
            return pw;
        }

        /// <summary>
        /// Get the username of this instance.
        /// </summary>
        /// <returns>The username.</returns>
        public string GetUsername() {
            return name;
        }
    }
}
