using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;
using System.Net.Sockets;
using System.Threading;

public static class NetData {
    public static Server TCP_SERVER {get; set;}
    public static Socket client_socket {get; set;}
    public static string username {get; set;}
    public static bool fancyGraphics {get; set;}  
    public static IDictionary<string, float> settings {get; set;}  
}


public class menu : MonoBehaviour
{

    public UnityEngine.UI.Button START_BUTTON;
    public UnityEngine.UI.Button ONLINE_BUTTON;
    public UnityEngine.UI.Button CONNECT_BUTTON;

    public UnityEngine.UI.Button APPLY_BUTTON;

    public UnityEngine.UI.Button OPTIONS_BUTTON;
    public UnityEngine.UI.Button BACK_BUTTON;
    public GameObject IP_INPUT;
    public GameObject PORT_INPUT;
    public GameObject USERNAME_INPUT;

    public GameObject SERVER_SELECT;

    public UnityEngine.RectTransform PanelOptions;

    public GameObject ErrorText;

    bool awaiting_connect = false;
    public float time0;
    System.Threading.Tasks.Task connect_task;
    Socket client_socket;

    string getLabel() {
        return SERVER_SELECT.GetComponent<RectTransform>().GetChild(1).gameObject.GetComponent<UnityEngine.UI.Text>().text;
    }

    void limit_input(GameObject input, int length) {
        string text =this.get_text_box(input);
        if (text.Length > length) 
            this.setText(input, text.Substring(0, length));
    }

    void AddListener(GameObject INPUT, int length) {
        INPUT.GetComponent<TMPro.TMP_InputField>().onValueChanged.AddListener(delegate {this.limit_input(INPUT, length);} );        
    }

    void setText(GameObject INPUT, string text) {
        INPUT.GetComponent<TMPro.TMP_InputField>().text = text;
    }

    public GameObject[] OPTION_DATA;

    // Fuck you unity for this shitty API making Instance() objects not work
    public UnityEngine.UI.Slider OPTION0;
    public UnityEngine.UI.Slider OPTION1;
    public UnityEngine.UI.Slider OPTION2;
    public UnityEngine.UI.Slider OPTION3;

    public UnityEngine.UI.Toggle BUTTON_TOGGLE;

    public Dictionary<int, string> SettingsPointer;

    public IDictionary<string, float> TestSettings;

    IDictionary<string, float> clone(IDictionary<string, float> input)
    {
        var new_dict = new Dictionary<string, float>();
        foreach(var item in input)
        {
            new_dict.Add(item.Key, item.Value);
        }
        return new_dict;
    }

    void init_settings()
    {


        NetData.settings = new Dictionary<string, float>() {
            {"Ambient Occulusion", 0.05f},
            {"Bloom Effect", 4f},
            {"Depth of Field", 20f},
            {"Color Grading", 89f},
        };

        TestSettings = this.clone(NetData.settings);

        SettingsPointer = new Dictionary<int, string>();
        OPTION_DATA = new GameObject[4];

        UnityEngine.UI.Slider[] options = new UnityEngine.UI.Slider[] {
            OPTION0, OPTION1, OPTION2, OPTION3
        };

        int i = 0;
        NetData.fancyGraphics = true;
        foreach (var option in NetData.settings)
        {
            SettingsPointer.Add(i, option.Key);
            UnityEngine.UI.Slider slider = options[i];   
            RectTransform s_t = slider.GetComponent<RectTransform>();
            TMPro.TextMeshProUGUI text = s_t.GetChild(s_t.childCount - 1).GetComponent<TMPro.TextMeshProUGUI>();

            slider.value = option.Value;
            pieceCode code = slider.gameObject.AddComponent<pieceCode>();
            int iCopy = i; 
            slider.onValueChanged.AddListener(delegate {
                TestSettings[SettingsPointer[iCopy]] = slider.value;
            });
            OPTION_DATA[i] = slider.gameObject;
            i++;
        }
        APPLY_BUTTON.onClick.AddListener(delegate {
            NetData.settings = clone(TestSettings);
        });

        BUTTON_TOGGLE.onValueChanged.AddListener(delegate {
            NetData.fancyGraphics = !NetData.fancyGraphics;
            foreach(var item in options)
            {
                item.enabled = NetData.fancyGraphics;
            }
        });

    }

    // Start is called before the first frame update
    void Start()
    {
        this.init_settings();
        ONLINE_BUTTON.onClick.AddListener( delegate {this.EnterOnlineMode(0, 1, 0);});
        BACK_BUTTON.onClick.AddListener(   delegate {this.EnterOnlineMode(1, 0, 0);});
        OPTIONS_BUTTON.onClick.AddListener(delegate {this.EnterOnlineMode(0, 0, 1);});

        CONNECT_BUTTON.onClick.AddListener(delegate    { this.handleConnect(); });
        START_BUTTON.onClick.AddListener(  delegate {
            SceneManager.LoadScene("2 player");
        });
        this.EnterOnlineMode(1, 0, 0);
        NetData.TCP_SERVER = new Server();

        AddListener(IP_INPUT,       16);
        AddListener(PORT_INPUT,      5);
        AddListener(USERNAME_INPUT, 16);
    }

    string get_text_box(GameObject input, string set_string=null) {
        RectTransform rect_transform = input.GetComponent<RectTransform>();
        Transform text_area = rect_transform.GetChild(0);

        Transform tranform = text_area.GetChild(1);        
        TMPro.TextMeshProUGUI text = tranform.gameObject.GetComponent<TMPro.TextMeshProUGUI>();
        
        if (set_string != null) {
            text.text = set_string;
        } 

        return text.text;
    }

    void setMessage(string msg, bool isError)
    {
        TMPro.TextMeshProUGUI text_ = ErrorText.GetComponent<TMPro.TextMeshProUGUI>();
        text_.text = msg;
        if (isError)
            text_.color = new Color(0.96f, 0.26f, 0.34f);
        else
            text_.color = new Color(0.38f, 0.87f, 0.05f);
    }

    bool validate_ip(string a)
    {
        if (a.Length < 1) return false;
        a = a.Remove(a.Length - 1);
        string[] bytes = a.Split('.');
        if (bytes.Length != 4) return false;
                
        foreach (string BYTE in bytes)
        {
            if (!Regex.IsMatch(BYTE, @"^(\d+)$")) return false;
            
            int parsed = int.Parse(BYTE);
            if (parsed > 0xFF || parsed < 0x00) return false;
        }
        return true;
    }

    void EstablishClientSocket(string IP, int PORT)
    {
        Server net = NetData.TCP_SERVER;
        this.setMessage($"Attempting to connect to \"{IP}:{PORT}\"", false);
        client_socket = new Socket(net.AF_INET, net.SOCK_STREAM, ProtocolType.Tcp);

        try {
            connect_task = client_socket.ConnectAsync(IP, PORT);
            this.setMessage($"Attempting to connect to server...", false);
            this.setButtonText(CONNECT_BUTTON.gameObject, "CANCEL");

            time0 = Time.time;
            awaiting_connect = true;
        }
        catch (System.Exception err)  {
            this.setMessage($"{err.GetType()}: {err.Message}".Replace("\n", " "), true);
            try {client_socket.Close();}
            catch(System.Exception) {} 
        }        
    }

    void setButtonText(GameObject Button, string text) {
        Button.GetComponent<RectTransform>().GetChild(0).gameObject.GetComponent<UnityEngine.UI.Text>().text = text;
    }

    void handleConnect() 
    {
        if (awaiting_connect) {
            this.setMessage("Connection to server canceled", true);
            client_socket.Dispose();
            return;
        }

        string port = this.get_text_box(PORT_INPUT).Trim('\r', '\n');
        string err_msg = $"Invalid Port: Input \"{port}\" must be at most an unsigned 16 bit number.";

        // Places a 0x200b character at the END
        port = port.Remove(port.Length - 1); 


        if (!Regex.IsMatch(port, @"^(\d+)$")) {
            this.setMessage(err_msg, true);
            return;
        }
        
        int PORT = int.Parse(port);

        if (PORT > 0xFFFF || PORT < 0x0000) {
            this.setMessage(err_msg, true);
            return;
        }


        string IP = this.get_text_box(IP_INPUT);
        if (!this.validate_ip(IP)) {
            this.setMessage($"Invalid IP: Input \"{IP}\" must be four (8 bit numbers) seperated by a dot (.).", true);
            return;
        }
        IP = IP.Remove(IP.Length - 1);

        string username = this.get_text_box(USERNAME_INPUT);
        username = username.Remove(username.Length - 1);
        if (username.Length < 2) {
            this.setMessage("Username must be between 2 and 16 Characters.", true);
            return;
        }

        NetData.username = username;

        if (getLabel().ToLower() == "client")
        {
            this.EstablishClientSocket(IP, PORT);
        }

        else { // Server
            this.setMessage($"Attempting to host a server at \"{IP}:{PORT}\"", false);
            try {
                NetData.TCP_SERVER.run(IP, PORT);
                short c = 3;
                Thread t = new Thread(() => NetData.TCP_SERVER.serve(ref c));
                t.Start();
                while  (c != 4) {}; 
                this.EstablishClientSocket(IP, PORT);
            } catch (System.Exception err) {
               this.setMessage($"{err.GetType()}: {err.Message.Replace("\n", "")}", true);
             }
        }
    }

    void toggle(UnityEngine.UI.Button button, int q)
    {
        this.toggle(button.gameObject, q);
    }

    void toggle(GameObject button, int q) {
        if (q == 0)
            button.SetActive(false);
        else
            button.SetActive(true);
    }


    // ARDUINO LED kind of code 
    void EnterOnlineMode(int B1, int B2, int B3) 
    {
        // Main Menu
        toggle( START_BUTTON,  B1);
        toggle(ONLINE_BUTTON,  B1);
        toggle(OPTIONS_BUTTON, B1);

        // Online
        toggle(CONNECT_BUTTON, B2);
        toggle(   BACK_BUTTON, B2 | B3);

        toggle(IP_INPUT,       B2);
        toggle(PORT_INPUT,     B2);
        toggle(SERVER_SELECT,  B2);
        toggle(USERNAME_INPUT, B2);

        // Settings
        toggle(APPLY_BUTTON,  B3);
        toggle(BUTTON_TOGGLE.gameObject, B3);
        foreach (var option in OPTION_DATA)
        {
            toggle(option, B3);
        }

    }

    void handle_connect()
    {
        NetData.client_socket = this.client_socket;
        SceneManager.LoadScene("multiplaer");
    }


    // Update is called once per frame
    void Update()
    {
        if (this.awaiting_connect)
        {
            if (connect_task.IsCompleted) {
                if (!client_socket.Connected) {
                    this.awaiting_connect = false;
                    this.setMessage("The Operation was either cancelled or something went wrong (Adress not available or the Socket was closed)", true);
                    this.setButtonText(CONNECT_BUTTON.gameObject, "CONNECT");
                    return;
                }
                awaiting_connect = false;
                this.setMessage($"Successfuly connected to server", false);
                this.handle_connect();
            } 
            else {
                this.setMessage($"Waiting for server to respond: " + string.Format("{0:0.00}", Time.time - time0) + "s", false);
            }
        }
    }
}
