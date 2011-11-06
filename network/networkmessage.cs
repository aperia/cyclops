/*
 * Copyright (c) 2010 Jopirop
 * 
 * All rights reserved.
 * 
 */

using System;
using System.Net.Sockets;
using System.Net;

namespace Cyclops {

    /// <summary>
    /// This is the class handles network communication between the server
    /// and a client.
    /// </summary>
    /// 
    public class NetworkMessage {
        private const int MAX_SIZE = 0xFFFF; //Max message length

        //First 2 bytes of message length incidate message length size
        private const int MESSAGE_LENGTH_BYTES = 2;

        private ushort position;
        private Socket socket;
        private byte[] buffer;

        /// <summary>
        /// This method returns the right most bits of the specified value.
        /// </summary>
        /// <param name="val">
        /// The value for which to get the last eight bits.
        /// </param>
        /// <returns>
        /// The eight right most bits.
        /// </returns>
        private byte LastEight(int val) {
            return (byte)(val & 0xFF);
        }
        

        /// <summary>
        /// Prepend message length into the message buffer.
        /// </summary>
        private void AddMessageLength() {
            ushort tmp = position;
            position = 0;
            AddU16((ushort)(tmp - LengthOffset));
            position = tmp;
        }

        /// <summary>
        /// Constructor for the NetworkMessage class.
        /// </summary>
        /// <param name="pSocket">
        /// The socket used for communication.
        /// </param>
        public NetworkMessage(Socket pSocket) {
            socket = pSocket;
            buffer = new byte[MAX_SIZE];
            Reset();
            MarkSocketClosed = false;
        }

        public NetworkMessage(Socket pSocket, byte headerLengthOffset)
            : this(pSocket) {
                LengthOffset = headerLengthOffset;
        }

        /// <summary>
        /// Skip the number of bytes specified in the buffer.
        /// </summary>
        /// <param name="amt">
        /// Number of bytes to skip.
        /// </param>
        public void SkipBytes(short amt) {
            position = (ushort)(amt + position);
        }

        /// <summary>
        /// Resets the buffer.
        /// </summary>
        public void Reset() {
            position = 2;
        }

        //New protocols don't include length of header, old ones do
        public byte LengthOffset {
            get;
            set;
        }

        /// <summary>
        /// Gets the next byte in the buffer.
        /// </summary>
        /// <returns>
        /// The next byte in the buffer.
        /// </returns>
        public byte GetByte() {
            return buffer[position++];
        }

        /// <summary>
        /// Gets the message length.
        /// </summary>
        /// <returns>
        /// Message length.
        /// </returns>
        public ushort GetMessageLength() {
            ushort temp = position;
            position = 0;
            ushort length = GetU16();
            position = temp;
            return length;
        }

        /// <summary>
        /// Gets the next ushort from the buffer.
        /// </summary>
        /// <returns>
        /// The next ushort in the buffer.
        /// </returns>
        public ushort GetU16() {
            ushort val = GetByte();
            val |= (ushort)(GetByte() << 8);
            return val;
        }

        /// <summary>
        /// Gets the next uint in the buffer.
        /// </summary>
        /// <returns>
        /// The next uint in the buffer.
        /// </returns>
        public uint GetU32() {
            uint val = GetU16();
            val |= (uint)(GetU16() << 16);
            return val;
        }

        /// <summary>
        /// Gets the next bytes in the buffer and treats them as a position.
        /// </summary>
        /// <returns>
        /// The position as specified by the buffer.
        /// </returns>
        public Position GetPosition() {
            Position pos = new Position();
            pos.x = GetU16();
            pos.y = GetU16();
            pos.z = GetByte();

            return pos;
        }

        /// <summary>
        /// Gets a the next string in the buffer, where the next string
        /// specifies its length.
        /// </summary>
        /// <returns>
        /// The next string in the buffer.
        /// </returns>
        public string GetStringL() {
            ushort length = GetU16();
            string val = "";
            for (int i = 0; i < length; i++) {
                val += (char)GetByte();
            }
            return val;
        }

        /// <summary>
        /// Gets the next string in the buffer, where the next
        /// string's length is determined by null-termination.
        /// </summary>
        /// <returns>
        /// The next string in the buffer.
        /// </returns>
        public string GetStringZ() {
            string val = "";
            byte b;
            while ((b = GetByte()) != '\0')
                val += (char)b;

            return val;
        }

        /// <summary>
        /// Gets the next null-terminated string and ignores
        /// the rest of the data up to the maxStringLength.
        /// </summary>
        /// <param name="maxStringLength">The max length of 
        /// the string in the buffer</param>
        /// <returns>The null-terminated string.</returns>
        public string GetStringZ(ushort maxStringLength) {
            string stringZ = GetStringZ();
            int leftToRead = maxStringLength - stringZ.Length;
            //Starts at 1 since the GetStringZ counts the
            //null-termination character
            for (int i = 1; i < leftToRead; i++) {
                GetByte(); //Ignore rest of bytes
            }

            return stringZ;
        }

        /// <summary>
        /// Adds the specified byte to the buffer
        /// </summary>
        /// <param name="b">
        /// The byte to add.
        /// </param>
        public void AddByte(byte b) {
            buffer[position++] = b;
        }

        /// <summary>
        /// Adds the specified ushort to the buffer.
        /// </summary>
        /// <param name="val">
        /// The ushort to add.
        /// </param>
        public void AddU16(ushort val) {
            AddByte(LastEight(val));
            AddByte(LastEight(val >> 8));
        }


        /// <summary>
        /// Adds the specified uint to the buffer.
        /// </summary>
        /// <param name="val">
        /// The uint to add.
        /// </param>
        public void AddU32(uint val) {
            AddU16((ushort)(val & 0xFFFF));
            AddU16((ushort)((val >> 16) & 0xFFFF));
        }

        /// <summary>
        /// Adds the specified position to the buffer.
        /// </summary>
        /// <param name="pos">
        /// The position to add.
        /// </param>
        public void AddPosition(Position pos) {
            AddU16(pos.x);
            AddU16(pos.y);
            AddByte(pos.z);
        }

        /// <summary>
        /// Adds the raw string to the buffer, without zero-termination nor
        /// its length being specified.
        /// </summary>
        /// <param name="val">
        /// The string to add.
        /// </param>
        public void AddString(string val) {
            for (int i = 0; i < val.Length; i++) {
                AddByte((byte)val[i]);
            }
        }

        /// <summary>
        /// Adds the specified string to the buffer and appends it with
        /// null termination.
        /// </summary>
        /// <param name="val">
        /// The string to add.
        /// </param>
        public void AddStringZ(string val) {
            AddString(val);
            AddByte(0x00);
        }

        ///
        /// <summary>
        /// Adds the specified string to the buffer and appends
        /// null-terminated characters to fill the required length. 
        /// </summary>
        /// <param name="val">
        /// The string to add.
        /// </param>
        public void AddStringZ(string val, ushort requiredLength) {
            AddString(val);
            for (int i = 0; i < (requiredLength - val.Length); i++)
                AddByte(0x00);
        }

        /// <summary>
        /// Adds a string to the buffer and prepends the length of the string.
        /// </summary>
        /// <param name="val">
        /// The string to add.
        /// </param>
        public void AddStringL(string val) {
            int length = val.Length;
            if (length > ushort.MaxValue) {
                throw new Exception("String length is too long.");
            }
            AddU16((ushort)length);
            AddString(val);
        }

       
        /// <summary>
        /// Read a message from the socket. This methos is synchronous,
        /// (blocks until there is a message to read).
        /// </summary>
        public void ReadFromSocket() {
            if (!Connected()) {
                throw new Exception("Socket is closed.");
            }
            int size = socket.Receive(buffer);
            if (size >= MAX_SIZE) {
                throw new Exception("Network data exceeds max buffer size.");
            }
            Reset();
        }

        public bool MarkSocketClosed {
            get;
            set;
        }

        /// <summary>
        /// Write the message to the socket if message length > 0.
        /// </summary>
        public void WriteToSocket() {
            AddMessageLength();

            //Avoid sending empty messages (2 is used because length header = 2)
            if (GetMessageLength() == 2 - LengthOffset) { 
                return; 
            }

            if (!socket.Connected) {
#if DEBUG
                Tracer.Println("Attempting to write to a closed socket.");
#endif
                return;
            }

            byte[] copy = new byte[position];
            for (int i = 0; i < position; i++) {
                copy[i] = buffer[i];
            }
            // Begin sending the data to the remote device.
            socket.BeginSend(copy, 0, position, 0,
                new AsyncCallback(SendCallback), socket);
        }

        private void SendCallback(IAsyncResult ar) {
            // Retrieve the socket from the state object.
            Socket handler = (Socket)ar.AsyncState;
                        if (!socket.Connected) {
                Console.WriteLine("Attempted to write to a close socket");
                return;
            }
            // Complete sending the data to the remote device.
            int bytesSent = handler.EndSend(ar);
#if DEBUG
            Console.WriteLine("Sent {0} bytes to client.", bytesSent);
#endif
            if (MarkSocketClosed) {
                socket.Close();
            }
        }


        /// <summary>
        /// <see cref=">Socket.Connected()"/>
        /// </summary>
        /// <returns></returns>
        public bool Connected() {
            return socket.Connected;
        }

        /// <summary>
        /// Close the networkmessage stream.
        /// </summary>
        public void Close() {
            socket.Close();
        }

        /// <summary>
        /// Begin receiving data in an asynchronous manner. The data received
        /// is handled by the callBack object. 
        /// </summary>
        /// <param name="callBack">The call back object.</param>
        public void BeginReceiving(AsyncCallback callBack) {
            if (!socket.Connected) {
                return;
            }
            socket.BeginReceive(buffer, 0, MAX_SIZE, SocketFlags.None, callBack, this);
        }

        public int EndReceive(IAsyncResult result) {
            int bytesRead = socket.EndReceive(result);
            return bytesRead;
        }
    }
}
