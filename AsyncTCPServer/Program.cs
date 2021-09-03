using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

// State object for reading client data asynchronously  
public class StateObject
{
    // Size of receive buffer.  
    public const int BufferSize = 1024;

    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];

    // Received data string.
    public StringBuilder sb = new StringBuilder();

    // Client socket.
    public Socket workSocket = null;
}

public class AsynchronousSocketListener
{
    // Thread signal.  
    public static ManualResetEvent allDone = new ManualResetEvent(false);

    public AsynchronousSocketListener()
    {
    }

    public static void StartListening()
    {
        // Establish the local endpoint for the socket.  
        // The DNS name of the computer  
        // running the listener is "host.contoso.com".  
        //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
        //IPAddress ipAddress = ipHostInfo.AddressList[0];
        System.Net.IPAddress ipAddress = System.Net.IPAddress.Parse("127.0.0.1");  //127.0.0.1 as an example
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 3079);

        // Create a TCP/IP socket.  
        Socket listener = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        // Bind the socket to the local endpoint and listen for incoming connections.  
        try
        {
            listener.Bind(localEndPoint);
            listener.Listen(100);

            while (true)
            {
                // Set the event to nonsignaled state.  
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.  
                Console.WriteLine("Waiting for a connection...");
                listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    listener);

                // Wait until a connection is made before continuing.  
                allDone.WaitOne();
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        Console.WriteLine("\nPress ENTER to continue...");
        Console.Read();

    }

    public static void AcceptCallback(IAsyncResult ar)
    {
        // Signal the main thread to continue.  
        allDone.Set();

        // Get the socket that handles the client request.  
        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);

        // Create the state object.  
        StateObject state = new StateObject();
        state.workSocket = handler;
        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            new AsyncCallback(ReadCallback), state);
    }

    public static void ReadCallback(IAsyncResult ar)
    {
        String content = String.Empty;

        // Retrieve the state object and the handler socket  
        // from the asynchronous state object.  
        StateObject state = (StateObject)ar.AsyncState;
        Socket handler = state.workSocket;

        // Read data from the client socket.
        int bytesRead = handler.EndReceive(ar);

        if (bytesRead > 0)
        {
            #region MyRegion
            //// There  might be more data, so store the data received so far.  
            //state.sb.Append(Encoding.ASCII.GetString(
            //    state.buffer, 0, bytesRead));

            //// Check for end-of-file tag. If it is not there, read
            //// more data.  
            //content = state.sb.ToString();
            //if (content.IndexOf("<EOF>") > -1)
            //{
            //    // All the data has been read from the
            //    // client. Display it on the console.  
            //    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
            //        content.Length, content);
            //    // Echo the data back to the client.  
            //    Send(handler, content);
            //}
            //else
            //{
            //    // Not all data received. Get more.  
            //    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            //    new AsyncCallback(ReadCallback), state);
            //}
            #endregion

            if (state.buffer[3] == 1)
            {
                string input = BitConverter.ToString(state.buffer, 0, bytesRead).Replace("-", "");

                Console.WriteLine("Recived {0} bytes to client.", input);

                byte[] bytes = Unpack(input);

                //byte[] serialNumber = bytes.Skip(bytes.Length - 2).ToArray();

                //byte[] response = { 0x78, 0x78, 0x05, 0x01, 0x00, 0x00, 0x00, 0x0 };

                //serialNumber.CopyTo(response, 4);

                //UInt16 sendCRC = crc_bytes(response.Take(response.Length - 2).ToArray());

                //response[response.Length - 2] = (byte)((sendCRC >> 8) & 0xFF);
                //response[response.Length - 1] = (byte)((sendCRC) & 0xFF);
                content = "message recieved";
                Send(handler, content);
                // handler.Send(response);
            }
            else
            {
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                //// Check for end-of-file tag. If it is not there, read
                //// more data.  
                content = state.sb.ToString();

                Console.WriteLine("Recived {0} bytes to client.", Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                //  SaveData(content);
                // Not all data received. Get more.
           

                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the
                    // client. Display it on the console.  
                    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                        content.Length, content);
                    // Echo the data back to the client.  
                    Send(handler, content);
                }
                else
                {
                    // Not all data received. Get more.  
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
                }
                // }
            }
        }
    }



    private static void Send(Socket handler, String data)
    {
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        // Begin sending the data to the remote device.  
        handler.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), handler);
    }

    static byte[] Unpack(string data)
    {
        //return null indicates an error
        List<byte> bytes = new List<byte>();

        // check start and end bytes

        if ((data.Substring(0, 4) != "7878") && (data.Substring(data.Length - 4) != "0D0A"))
        {
            return null;
        }

        for (int index = 4; index < data.Length - 4; index += 2)
        {
            bytes.Add(byte.Parse(data.Substring(index, 2), System.Globalization.NumberStyles.HexNumber));
        }
        //crc test
        byte[] packet = bytes.Take(bytes.Count - 2).ToArray();
        byte[] crc = bytes.Skip(bytes.Count - 2).ToArray();

        uint CalculatedCRC = crc_bytes(packet);


        return packet;
    }

    static public UInt16 crc_bytes(byte[] data)
    {
        ushort crc = 0xFFFF;

        for (int i = 0; i < data.Length; i++)
        {
            crc ^= (ushort)(Reflect(data[i], 8) << 8);
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 0x8000) > 0)
                    crc = (ushort)((crc << 1) ^ 0x1021);
                else
                    crc <<= 1;
            }
        }
        crc = Reflect(crc, 16);
        crc = (ushort)~crc;
        return crc;
    }

    static public ushort Reflect(ushort data, int size)
    {
        ushort output = 0;
        for (int i = 0; i < size; i++)
        {
            int lsb = data & 0x01;
            output = (ushort)((output << 1) | lsb);
            data >>= 1;
        }
        return output;
    }


    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket handler = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = handler.EndSend(ar);
            Console.WriteLine("Sent {0} bytes to client.", bytesSent);

            handler.Shutdown(SocketShutdown.Both);
            handler.Close();

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public static int Main(String[] args)
    {
        StartListening();
        return 0;
    }
}