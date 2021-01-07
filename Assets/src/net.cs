using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Threading;


public class Server
{
    public AddressFamily AF_INET = AddressFamily.InterNetwork;
    public SocketType SOCK_STREAM = SocketType.Stream;

    public Socket sock;
    public Socket[] clients;

    public bool[] client_status;
    public IDictionary<int, IDictionary<string, string>> client_data;

    int turn = 0;
    public byte[] EMessage(string message)
    {
        return System.Text.Encoding.UTF8.GetBytes(message);
    }

    public string get_date() {
        string date_string = System.DateTime.Now.ToString("HH:mm tt");
        if (date_string[0].ToString() == "0") date_string = date_string.Substring(1);
        return System.Text.RegularExpressions.Regex.Replace(date_string, @"\s+", "").ToLower();
    }


    // Start is called before the first frame update
    public bool run(string HOST_IP, int PORT) {
        sock = new Socket(AF_INET, SOCK_STREAM, ProtocolType.Tcp);
        IPAddress adress;
        bool isCorrect = System.Net.IPAddress.TryParse(HOST_IP, out adress);
        if (!isCorrect)
            return false;
        System.Net.IPEndPoint endPoint = new System.Net.IPEndPoint(adress, PORT);
        sock.Bind(endPoint);

        /* A chess game requires 2 clients */
        clients = new Socket[2]; 
        client_data = new Dictionary<int, IDictionary<string, string>>(); 
        client_status = new bool[2] {false, false};

        return true; 
    }

    public byte[] pack_message(IDictionary<string, string> message) {
        string buffer = "";
        foreach(var item in message)
        {
            buffer += $"{item.Key}:{item.Value}\n";
        }
        buffer += "END:END\n\n";
        return this.EMessage(buffer);        
    }

    public IDictionary<string, string> unpack_message(byte[] message) {        
        string UTF8_STR = System.Text.Encoding.UTF8.GetString(message);
        return this.unpack_message(UTF8_STR);
    }

    public IDictionary<string, string> unpack_message(string UTF8_STR) {
        int END_INDEX = UTF8_STR.IndexOf("END:END");
        UTF8_STR = UTF8_STR.Substring(0, END_INDEX);

        string[] items = UTF8_STR.Split('\n');
        IDictionary<string, string> RESP_DICT = new Dictionary<string, string>();
        foreach (var item in items)
        {
            if (item.Trim() == "") continue;
            string[] spl = item.Split(new char[] {':'}, count:2); 
            RESP_DICT.Add(spl[0].Trim().ToLower(), spl[1].Trim());
        }
        return RESP_DICT;
    }

    void sendClients(byte[] message)
    {
        foreach(var cli in this.clients)
        {
            cli.Send(message);
        }
    }


    bool SocketConnected(Socket s)
    {
        bool part1 = s.Poll(1000, SelectMode.SelectRead);
        bool part2 = (s.Available == 0);
        if (part1 && part2)
            return false;
        else
            return true;
    }

    void ping(Socket client) {
        client.Send(new byte[] {0xFF, 0xFF, 0xFF, 0xFF});
    }

    public bool isPing(byte[] message) {
        bool is_ping = true;
        for (int i=0; i < 4; i++) {
            if (message[i] != 0xFF)
            {
                is_ping = false;
                break;
            }
        }
        return is_ping;
    }

    long time() {
        System.DateTime foo = System.DateTime.Now;
        return ((System.DateTimeOffset)foo).ToUnixTimeSeconds();
    }


    void handle_game_client(Socket client, int index) 
    {
        Debug.Log("handling game client");
        string sender = client_data[index]["username"];
        string sender_color = client_data[index]["color"];
        SocketAsyncEventArgs e = new SocketAsyncEventArgs();
        e.SetBuffer(new byte[512], 0, 512);

        bool waiting = false;
        while (true) 
        {

            if (!waiting) {
                e = new SocketAsyncEventArgs();
                e.SetBuffer(new byte[512], 0, 512);
                client.ReceiveAsync(e);
                waiting = true;
            }

            if (e.BytesTransferred == 0) 
                continue;

            string RECEVIED_STRING = System.Text.Encoding.UTF8.GetString(e.Buffer);
            int end_index = RECEVIED_STRING.IndexOf("END:END");
            if (end_index == -1) continue;
            IDictionary<string, string> msg = this.unpack_message(RECEVIED_STRING);

            switch(msg["type"])
            {
                case "chat":
                    this.sendClients(
                        this.pack_message(new Dictionary<string, string>() {
                            {"type",     "chat"},
                            {"username", sender},
                            {"date",     this.get_date()},
                            {"color",    sender_color},
                            {"msg",      msg["msg"]}
                        })
                    );
                    break;
                case "move":
                    if (turn != index) 
                        break;
                    msg["turn"] = index == 0 ? "white" : "black"; // TURN FOR THIS MOVE
                    this.sendClients(this.pack_message(msg));
                    turn = turn == 0 ? 1 : 0;
                    break;                    
            }
            waiting = false;
        }
    }

    void handle_client(Socket client, int index)
    {
        var CLIENT_INFO = new Dictionary<string, string>(); client_data.Add(index, CLIENT_INFO);

        byte[] buffer = new byte[1024];
        client.Receive(buffer);
        IDictionary<string, string> data = this.unpack_message(buffer);
        
        CLIENT_INFO.Add("username", data["username"]); 
        CLIENT_INFO.Add("color", data["color"]);

        client_status[index] = true;
        int c_index = index == 1 ? 0 : 1;

        if (!client_status[c_index]) 
        {
            client.Send(this.pack_message(new Dictionary<string, string>() {
                {"type", "admin_msg"},
                {"status", "no"},
                {"date", this.get_date()},
                {"msg", $"{data["username"]} has joined. Waiting for other player to join"}
            }));
            long q = this.time();
            while (!client_status[c_index]) {
                Debug.Log(this.time() - q);
            };
        }
        
        string u1 = client_data[0]["username"];
        string u2 = client_data[1]["username"];
        
        client.Send(this.pack_message(
            new Dictionary<string, string>() {
                {"type", "admin_msg"},
                {"status", "yes"},
                {"piece_color", index == 0 ? "white" : "black"},
                {"date", this.get_date()},
                {"msg", $"All players have joined ({u1}, {u2}), Game is starting..."}
            }
        ));
        
        this.handle_game_client(client, index);
        return;
    }

    public void serve(ref short a) {
        sock.Listen(1);
        int index = 0;
        a++;
        while (true) {
            Socket player_socket = sock.Accept();
            int current_index = index;
            Thread thread = new Thread(() => handle_client(player_socket, current_index));
            clients[index] = player_socket;
            index++;
            thread.Start();
        }
    }
}
