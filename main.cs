
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cyclops {
    class Program {
        private static void Pause() {
            Tracer.Println("Press enter and any key to continue");
            Console.ReadLine(); //Lazy pause
        }

        static void Main(string[] args) {

            /*
Map map = new Map(4096, 4096);
map.SetTile(new Position(320, 320, 7), new Tile(null));
if (map.GetTile(320, 320, 7) == null) {
    Console.WriteLine("dangz it");
} else {
    Console.WriteLine("woohoo");
}

Player[] players = new Player[20000];
for (int i = 0; i < players.Length; i++) {
    players[i] = new Player(new ProtocolSend(new NetworkMessage(null)));
}
*/
            //TODO: REMOVE
            //Dictionary<ushort, Item> items = new FileHandler().LoadItems(Config.GetPath()
            //    + "data\\items\\" + "items.bin");
            //return;
            //END TODO
           
            // Console.WriteLine(uint.MaxValue);
           // DatabaseHandler dbHandler = new DatabaseHandler();
           // dbHandler.Connect();
            //dbHandler.SavePlayer(new Player(null));
            //dbHandler.Disconnect();
            //if (true) { return; }
            /* TEST CODE */
#if DEBUG
            Tracer.Println("Debug Mode is currently on.");
#endif

            Tracer.Print("Loading configuration...");
            try {
                DynamicCompile dCompile = new DynamicCompile();
                Dictionary<string, string> values =
                    (Dictionary<string, string>)dCompile.Compile("config.cs", null);
                Config.Load(values); //Load config
            } catch (Exception e) {
                Tracer.Println("Unable to load config.");
                Tracer.Println(e.ToString());
                Pause();
                return; //Exit
            }
            Tracer.Println(" Done");
           // try {
                new Server().Start();
           // } catch (Exception e) {
            //    Tracer.Println("Exception thrown!");
          //      Tracer.Println(e.ToString());
          //  }
            /* END TEST CODE */
           
            Pause();
        }
    }
}