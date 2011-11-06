using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cyclops {
    public class Tile {
        private List<Thing> tileThings;

        public Tile(Thing ground) {
            tileThings = new List<Thing>();
            tileThings.Add(ground);
        }

        public void GetThings(ThingSet tSet) {
            foreach (Thing thing in tileThings) {
                tSet.AddThing(thing);
            }
        }

        public void AddThing(Thing thing) {
            for (int i = 1; i < tileThings.Count; i++) {
                if (thing.CompareTo(tileThings[i]) >= 0) {
                    tileThings.Insert(i, thing);
                    return;
                }
            }

            tileThings.Add(thing);
        }

        public void RemoveThing(Thing thing) {
            tileThings.Remove(thing);
        }

        public List<Thing> GetThings() {
            return tileThings;
        }

        /// <summary>
        /// Gets the stack position of a specified thing.
        /// Prevariant: That thing for which this method is called
        /// MUST be on the tile.
        /// </summary>
        /// <param name="thing">Thing to get the stack position for.</param>
        /// <returns>The thing's stack position.</returns>
        public byte GetStackPosition(Thing thing) {
            return (byte) tileThings.IndexOf(thing);
        }

        /// <summary>
        /// Gets the top thing on the tile.
        /// </summary>
        /// <returns>The "top" thing.</returns>
        public Thing GetTopThing() {
            if (tileThings.Count == 1)
                return tileThings[0];
            StackPosType posType = tileThings[1].GetStackPosType();
            if (posType == StackPosType.TOP_ITEM) {
                if (tileThings.Count == 2) {
                    return tileThings[1];
                } else {
                    return tileThings[2];
                }
            } else {
                return tileThings[1];
            }
        }

        /// <summary>
        /// Gets the thing based on the stackpos or
        /// returns null if the stackpos is invalid.
        /// </summary>
        /// <param name="stackpos">The stackpos</param>
        /// <returns>The thing if valid or null otherwise.</returns>
        public Thing GetThing(byte stackpos) {
            if (stackpos >= tileThings.Count) {
                return null;
            }

            Console.WriteLine("getthign stackpos: " + stackpos);
            Console.WriteLine("items.");
            foreach (Thing thing in tileThings) {
                Console.WriteLine(thing.Name);
            }
            return tileThings[stackpos];
        }

        /// <summary>
        /// Gets whether any things on this tile contain
        /// the specified type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if a thing on this tile is of the type, false otherwise.</returns>
        public bool ContainsType(uint type) {
            foreach (Thing thing in tileThings) {
                if (thing.IsOfType(type)) {
                    return true;
                }
            }
            return false;
        }
    }
}
