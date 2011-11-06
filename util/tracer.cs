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
    /// This class provides a standard way to write a message and 
    /// can be edited to either pipe to it to the screen or to a file etc.
    /// </summary>
    public class Tracer {
        /// <summary>
        /// Print the message out.
        /// </summary>
        /// <param name="message">The message to print.</param>
        public static void Print(string message) {
            Console.Out.Write(message);
        }

        /// <summary>
        /// Print the message out and add a newline character
        /// at the end.
        /// </summary>
        /// <param name="message">The message to print.</param>
        public static void Println(string message) {
            Console.Out.WriteLine(message);
        }
    }
}
