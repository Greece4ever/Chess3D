using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Net.Sockets;
using System.Linq;
using System.Net;

public class client : MonoBehaviour
{

    public objectSelect CHESS; // The main script
    /* Piece's Gameobject References */
    public GameObject solider;
    public GameObject TOWER;
    public GameObject QUEEN;
    public GameObject KING;
    public GameObject HORSE;
    public GameObject LEUTENANT;

    public GameObject CAMERA;

    public Socket client_socket;
    public bool isServer;
    public Server TCP_SERVER;

    public string IP;
    public short PORT;

    public string USERNAME;

    public RectTransform base_msg;
    public RectTransform PARENT;
    public Color USERNAME_COLOR;
    Vector3 NEXT_POSITION;
    float scrollWidth;
    float scrollHeight;
    float[] pos;

    bool started = false;

    public TMPro.TMP_InputField input;

    public AudioSource MOVE_AUDIO;
    
    // Start is called before the first frame update
    void add_message(string name, string message, string date_str, Color name_color) {
        RectTransform parent = Instantiate(base_msg);

        UnityEngine.UI.Text NAME_TEXT = parent.GetComponent<UnityEngine.UI.Text>();
        NAME_TEXT.text = name;
        NAME_TEXT.color = new Color(name_color.r, name_color.g, name_color.b);

        for (int i=0; i < 2; i++) 
        {
            Transform child = parent.GetChild(i);
            var innerText = child.gameObject.GetComponent<TMPro.TextMeshProUGUI>();
            if (i == 0) innerText.text = message;
            else innerText.text = date_str;
        }

        // Store all the positions to reset them after a new child has been added
        int childCount = PARENT.childCount;
        Vector3[] positions = new Vector3[childCount];
        for (int i=0; i < PARENT.childCount; i++) {
            positions[i] = PARENT.GetChild(i).transform.position; 
        } 

        parent.transform.SetParent(PARENT);

        // Here adding hight changes the positions of all the elements
        scrollHeight += 70;
        PARENT.sizeDelta = new Vector2(scrollWidth, scrollHeight);

        for (int i=0; i < childCount; i++) {
            PARENT.GetChild(i).transform.position = positions[i];
        } 

        PARENT.GetChild(childCount).transform.position = positions[childCount - 1] + new Vector3(0, -55, 0);    
    }

    void SendAsyncMessage(byte[] message)
    {   
        sendArgs = new SocketAsyncEventArgs();
        sendArgs.SetBuffer(message, 0, message.Length);
        client_socket.SendAsync(sendArgs);
    }

    public void onSubmit() {
        string value = input.text;
        if (value.Length == 0) 
            return;

        this.SendAsyncMessage(TCP_SERVER.pack_message(new Dictionary<string, string>() {
                {"type", "chat"},
                {"msg", value}
        }));

        input.text = "";
    }

    void setInputPlaceholder(string text) {
        input.placeholder.GetComponent<TMPro.TextMeshProUGUI>().text = text;
    }

    public SocketAsyncEventArgs e;
    public SocketAsyncEventArgs sendArgs;

    byte[] MSG_BUFFER;

    public Vector3 BLACK_START = new Vector3(15.89778f, 12.07936f, -3.47209f);
    public Vector3 BLACK_ROT = new Vector3(42.426f, -92.554f, 0f);
    public Vector3 WHITE_START = new Vector3(-21.63302f, 14.9405f, -3.893924f);
    public Vector3 WHITE_ROT = new Vector3(45.098f, 86.41801f, 0);

    /* Store the ID's of each object */
    IDictionary<int, GameObject> BLACK_ID;
    IDictionary<int, GameObject> WHITE_ID;

    IDictionary<int, IDictionary<int, GameObject>> SQUARE_ID;

    /* The boards */
    public int[,] boardWhite;
    public int[,] boardBlack;


    /* Materials for objects */
    public Material WHITE_MATERIAL;
    public Material BLACK_MATERIAL;

    /* Coordinate Stuff */
    public int     SQUARE_UNIT = 3; 
    public float   GROUND_LAYER = -0.4129999f; // z coordinate == ground
    public float   HORSE_GROUND_LAYER = 0.581f; // special case 
    public Vector3 ZeroZero = new Vector3(7.503f, 0, -13.654f); // of squares
    public Vector3 BlackZeroZero;

    /* Materials for squares */
    public Material SQUARE_ENEMY;
    public Material SQUARE_TARNSPARENT;
    public Material SQUARE_FIRENDLY;
    public Material SQUARE_NEUTRAL;
    /* Positions for dead pieces */
    public Vector3[] DEAD_BLACK_POSITIONS;
    public Vector3[] DEAD_WHITE_POSITIONS;

    /* Info about selected object */
    private Outline current;
    private GameObject selected_piece;
    private bool selected = false;

    /* Outlines */
    List<int[]> availableMoves;
    List<Outline> enemy_outlines;

    public Color SERVER_COLOR = new Color(0.9811321f, 0, 0);
    int turn;
    string color_piece;


    void chess_start() { // Bad WET code 
        CHESS = new objectSelect();

        CHESS.solider =  this.solider;
        CHESS.TOWER =    this.TOWER;
        CHESS.QUEEN =    this.QUEEN;
        CHESS.KING =     this.KING;
        CHESS.HORSE =    this.HORSE;
        CHESS.LEUTENANT= this.LEUTENANT;

        CHESS.boardWhite = boardWhite;
        CHESS.boardBlack = boardBlack;

        CHESS.SQUARE_UNIT = this.SQUARE_UNIT;
        CHESS.GROUND_LAYER = this.GROUND_LAYER;
        CHESS.HORSE_GROUND_LAYER = this.HORSE_GROUND_LAYER;
        CHESS.ZeroZero = this.ZeroZero;
        CHESS.BlackZeroZero = this.BlackZeroZero;

        CHESS.SQUARE_ENEMY = this.SQUARE_ENEMY;
        CHESS.SQUARE_TARNSPARENT = this.SQUARE_TARNSPARENT;
        CHESS.SQUARE_FIRENDLY = this.SQUARE_FIRENDLY;
        CHESS.SQUARE_NEUTRAL = this.SQUARE_NEUTRAL;

        CHESS.WHITE_MATERIAL = WHITE_MATERIAL;
        CHESS.BLACK_MATERIAL = BLACK_MATERIAL;

        CHESS.DEAD_BLACK_POSITIONS = this.DEAD_BLACK_POSITIONS;
        CHESS.DEAD_WHITE_POSITIONS = this.DEAD_WHITE_POSITIONS;

        CHESS.current = this.current;
        CHESS.selected_piece = this.selected_piece;
        CHESS.selected = this.selected;

        CHESS.BLACK_ID = this.BLACK_ID;
        CHESS.WHITE_ID = this.WHITE_ID;

        CHESS.winnerBlack = this.winnerBlack;
        CHESS.winnerWhite = this.winnerWhite;

        CHESS.__init__dicts();
        CHESS.__init__();
        CHESS.init_dead_positions();
    }

    void Start()
    {   

        this.winnerBlack.gameObject.SetActive(false);
        this.winnerWhite.gameObject.SetActive(false);

        this.chess_start();
        this.TCP_SERVER = NetData.TCP_SERVER;
        this.NEXT_POSITION = base_msg.transform.position;
        this.scrollHeight = PARENT.rect.height;
        this.scrollWidth = PARENT.rect.width;
        this.pos = new float[] {PARENT.rect.x, PARENT.rect.y};  


        input.onSubmit.AddListener(delegate {this.onSubmit();});
        input.enabled = false;
        this.setInputPlaceholder("Waiting game start...");
        base_msg.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = TCP_SERVER.get_date();


        System.Random _random = new System.Random();

        this.client_socket = NetData.client_socket;
        this.USERNAME = NetData.username;
        this.USERNAME_COLOR = new Color((float)_random.NextDouble(), (float)_random.NextDouble(), (float)_random.NextDouble());

        client_socket.Send(
            TCP_SERVER.pack_message(new Dictionary<string, string>() {
                {"username", this.USERNAME},
                {"color", $"{this.USERNAME_COLOR.r},{this.USERNAME_COLOR.g},{this.USERNAME_COLOR.b}"},
            })
        );

        e = new SocketAsyncEventArgs();
        e.SetBuffer(new byte[512], 0, 512);
        this.FROM = new pieceCode();
        this.TO   = new pieceCode();
        this.IS_ENEMY = false;
    }
    
    bool awaiting = false;

    Color parse_color(string color) {
        string[] RGB = color.Split(',');
        float[] RGB_f = new float[3];
        for (int i=0; i < 3; i++) {
            RGB_f[i] = float.Parse(RGB[i].Trim());
        } 
        return new Color(RGB_f[0], RGB_f[1], RGB_f[2]);
    }


    void handle_message(IDictionary<string, string> MSG)
    {
        switch (MSG["type"].ToLower()) {
            case "admin_msg":
                if (!started) {
                    if (MSG["status"] == "yes") 
                    {
                        CHESS.setTurn("white");
                        this.color_piece = MSG["piece_color"];
                        string turn_msg = this.color_piece == "white" ? "It is your turn (WHITE) to play" : "It is NOT your turn (BLACK).";
                        string side_msg = $"You ({this.color_piece.ToUpper()}) are in charge of the WHITE side. {turn_msg}";
                        this.add_message("Server", side_msg, MSG["date"], this.SERVER_COLOR);
                        if (this.color_piece == "white")
                        {
                            this.turn = 1;
                            CAMERA.transform.SetPositionAndRotation(WHITE_START, Quaternion.Euler(WHITE_ROT.x, WHITE_ROT.y, WHITE_ROT.z));
                        } 
                        else {
                            this.turn = 0;
                            CAMERA.transform.SetPositionAndRotation(BLACK_START, Quaternion.Euler(BLACK_ROT.x, BLACK_ROT.y, BLACK_ROT.z));
                        }
                        
                        CAMERA.GetComponent<orbitControl>().update_rotation();
                        started = true;
                        input.enabled = true;  
                        this.setInputPlaceholder("Type a message...");
                    }
                }
                this.add_message("Server", MSG["msg"], MSG["date"], this.SERVER_COLOR);
                break;
            case "chat":
                this.add_message(MSG["username"], MSG["msg"], MSG["date"], this.parse_color(MSG["color"]));
                break;
            case "move":
                if (!started) return;
                this.ParseMoveMessage(MSG);
                this.HandleMoveMessage(MSG);
                break;
        }
    }

    pieceCode FROM;
    pieceCode TO;
    bool IS_ENEMY;

    void ParseMoveMessage(IDictionary<string, string> moveMessage) {
        int PID = int.Parse(moveMessage["piece"].Trim());
        
        // Get the piece
        if (moveMessage["turn"] == "white")
            selected_piece = CHESS.WHITE_ID[PID];
        else 
            selected_piece = CHESS.BLACK_ID[PID];

        string[] fromNums = moveMessage["from"].Split(',');

        // Move FROM
        FROM.row = int.Parse(fromNums[0].Trim());
        FROM.col = int.Parse(fromNums[1].Trim());

        string[] toNums = moveMessage["to"].Split(',');

        // Move TO
        TO.row = int.Parse(toNums[0].Trim());
        TO.col = int.Parse(toNums[1].Trim());

        // IS THERE AN ENEMY AT "TO"
        IS_ENEMY = bool.Parse(moveMessage["isenemy"]);
    }

    public float time0 = 0f;
    public bool gameEnded = false;
    public RectTransform winnerWhite;
    public RectTransform winnerBlack;

    void handleGameEnd() {
        string winner = "";
        if (CHESS.WHITE_ID.Count == 0)
            winner = "black";
        else if (CHESS.BLACK_ID.Count == 0)
            winner = "white";
        if (winner == "") return;
        gameEnded = true;
        time0 = Time.time;
        if (winner == "black")
            this.winnerBlack.gameObject.SetActive(true);
        else
            this.winnerWhite.gameObject.SetActive(true);

        client_socket.Close();
    }

    void HandleMoveMessage(IDictionary<string, string> moveMessage)
    {
        if (IS_ENEMY)
            CHESS.Kill(TO.row, TO.col, team_of_killed:moveMessage["turn"] == "white" ? "black" : "white");

        CHESS.move(
             piece: selected_piece,
               row: TO.row,
               col: TO.col,
   relativeToWhite: false,
            update: true,
           isHorse: selected_piece.GetComponent<pieceCode>().type == "horse" ? true : false,
           fromRow: FROM.row,
           fromCol: FROM.col
        );

        CHESS.unSelect();
        string hahaha = moveMessage["turn"];

        if (moveMessage["turn"] != color_piece) 
            this.turn = 1;            
        else 
            this.turn = 0;
        

        int[] LocalToCords;
        if (CHESS.turn == "white")  {
            LocalToCords = CHESS.BlackToWhite(TO.row, TO.col);
        }
        else {
            LocalToCords = new int[2] {TO.row, TO.col};
        }

        if (moveMessage["turn"] == "white") 
            CHESS.setTurn("black");
        else
            CHESS.setTurn("white");

        
        pieceCode code = selected_piece.gameObject.GetComponent<pieceCode>();
        code.row = LocalToCords[0];
        code.col = LocalToCords[1];
        
        if (started) 
            MOVE_AUDIO.Play();
    
        this.handleGameEnd();
    }

    // public bool pinged = true;

    void Update()
    {
        if (!awaiting)
        {
            bool ReceiveAsync = client_socket.ReceiveAsync(e);
            awaiting = true;
        }

        if (e.BytesTransferred > 0) 
        {
            string RECEVIED_STRING = System.Text.Encoding.UTF8.GetString(e.Buffer);
            int end_index = RECEVIED_STRING.IndexOf("END:END");
            if (end_index != -1) // Full message received 
            { 
                IDictionary<string, string> MSG = TCP_SERVER.unpack_message(RECEVIED_STRING);
                this.handle_message(MSG);

                e = new SocketAsyncEventArgs();
                e.SetBuffer(new byte[512], 0, 512);
                awaiting = false;
            }
        }

        if (this.turn != 1) return;
        Vector3 mpos = Input.mousePosition;
        Ray pos_ray = Camera.main.ScreenPointToRay(mpos);
        RaycastHit hit;

        if (Physics.Raycast(pos_ray.origin, pos_ray.direction, out hit, 100.0f))
        {
            Collider collider = hit.collider;
            pieceCode code = collider.GetComponent<pieceCode>();

            if (Input.GetMouseButtonDown(0))
            {
                if ((turn == 1) && (collider.tag == this.color_piece))
                {
                    if (collider.gameObject == CHESS.selected_piece) 
                    {
                        this.CHESS.unSelect();
                        return;
                    }
                    CHESS.select(collider);                        
                    return;
                }
                else if (collider.tag == "SQUARE")
                {
                    if (!CHESS.selected) return;
                    System.Tuple<int[], pieceCode,bool,pieceCode, int[]> moveData = CHESS.GetMoveData(collider);
                    if (moveData != null) 
                    {
                        IDictionary<string, string> MESSAGE = new Dictionary<string, string>() {
                            {"type",    "move"},
                            {"piece",   CHESS.selected_piece.GetComponent<pieceCode>().id.ToString()}, // Piece's ID
                            {"to",      $"{moveData.Item2.row},{moveData.Item2.col}"},
                            {"from",    $"{moveData.Item1[0]},{moveData.Item1[1]}"},
                            {"isenemy", $"{moveData.Item3}"} 
                        };
                        this.SendAsyncMessage(TCP_SERVER.pack_message(MESSAGE));
                    }
                }
            }
        }
    }
}
