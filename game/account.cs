using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Cyclops {
    public class Account {
        //Used for global locking between acts
        private static object lockStatic = new object();
        private List<string> charList;

        public Account() {
            charList = charList = new List<string>();
        }

        public uint Number {
            get;
            set;
        }

        public string Password {
            get;
            set;
        }

        public List<string> GetCharList() {
            return charList;
        }

        public void Save() {
            lock (lockStatic) {
                Console.WriteLine("datapath: " + Config.GetDataPath());
                string path = Config.GetDataPath() + "accounts/"
                    + Number + ".bin";
                FileStream writeStream;
                writeStream = new FileStream(path, FileMode.Create);
                BinaryWriter wbin = new BinaryWriter(writeStream);
                wbin.Write((uint)Number);
                wbin.Write((string)Password);
                wbin.Write((byte)charList.Count);
                for (int i = 0; i < charList.Count; i++) {
                    wbin.Write((string)charList[i]);
                }
                wbin.Close();
            }
        }

        /// <summary>
        /// Attemps to load the account. Returns the account
        /// or null if the parameters passed are invalid.
        /// </summary>
        /// <param name="actNumber"></param>
        /// <returns></returns>
        public static Account Load(uint actNumber, string password) {
            lock (lockStatic) {
                string path = Config.GetDataPath() + "accounts/"
                    + actNumber + ".bin";
                if (!File.Exists(path)) {
#if DEBUG
                    Tracer.Println("Account Number: " + actNumber + " does not exist!");
#endif
                    return null;
                }
                Account account = new Account();
                BinaryReader bReader = new BinaryReader(File.Open(path, FileMode.Open));
                account.Number = bReader.ReadUInt32();
                account.Password = bReader.ReadString();
                if (account.Password != password) {
                    return null;
                }
                byte count = bReader.ReadByte();
                for (int i = 0; i < count; i++) {
                    account.charList.Add(bReader.ReadString());
                }
                bReader.Close();
                return account;
            }
        }
    }
}
