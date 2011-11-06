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
    /// A basic wrapper class used to avoid redundent code.
    /// </summary>
    public class ThingSet {
        //Stores things
        private HashSet<Thing> things;
 
        /// <summary>
        /// Default constructor, used to initalize a ThingSet.
        /// </summary>
        public ThingSet() {
            things = new HashSet<Thing>();
        }

        /// <summary>
        /// Gets all the things in this thing set.
        /// </summary>
        /// <returns>Things in this set</returns>
        public HashSet<Thing> GetThings() {
            return things;
        }

        /// <summary>
        /// Add a thing to this set.
        /// </summary>
        /// <param name="thing"></param>
        public void AddThing(Thing thing) {
            things.Add(thing);
        }

        /// <summary>
        /// Gets whether this set contains the specified thing.
        /// Note: Uses reference equality.
        /// </summary>
        /// <param name="thing">The thing to check.</param>
        /// <returns>True if this set contains the specified thing,
        /// false otherwise</returns>
        public bool ContainsThing(Thing thing) {
            return things.Contains(thing);
        }
    }
}
