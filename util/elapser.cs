
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
    /// This class is used for keeping track of whether certain actions are
    /// performed too quickly. This is used for things such as limiting walking
    /// speed, exhaustion on spells, etc.
    /// </summary>
    public class Elapser {
        private uint ticks;
        private DateTime dTime;

        /// <summary>
        /// Elapser constructor, used to initialize an Elapser object.
        /// </summary>
        /// <param name="timeInCS">The time interval to check.</param>
        public Elapser(uint timeInCS) {
            //60 000 000 = 60 seconds in ticks
            ticks = timeInCS * 100000;

            dTime = new DateTime(2005, 1, 1); //Arbitrary date set long ago
        }

        /// <summary>
        /// Set the time interval in centiseconds between each elapsed call
        /// that will return true.
        /// </summary>
        /// <param name="timeInCs">The time in centiseconds.</param>
        public void SetTimeInCS(uint timeInCS) {
            ticks = timeInCS * 100000;
        }

        /// <summary>
        /// Returns false if not enough time was given from the previous Elapsed call.
        /// Returns true if enough time was given and also resets the counter.
        /// </summary>
        /// <returns>True if enough time has passed and resets counter,
        /// false otherwise.</returns>
        public bool Elapsed() {
            double elapsedTicks = (DateTime.Now.Ticks - dTime.Ticks);
            if (elapsedTicks < ticks)
                return false;

            dTime = DateTime.Now;
            return true;
        }
    }
}
