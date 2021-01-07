using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine; 
using System.Linq;


public class objectSelect : MonoBehaviour
{
    public AudioSource MOVE_AUDIO;

    /* Piece's Gameobject References */
    public GameObject solider;
    public GameObject TOWER;
    public GameObject QUEEN;
    public GameObject KING;
    public GameObject HORSE;
    public GameObject LEUTENANT;

    /* The Camera (Ray Casting) */
    public Camera cameraBlack;
    public Camera cameraWhite;
    public Transform cam;
    /* Class for telling the available moves of a piece */
    public PieceMovements movements;

    public float unitZ = 2.964571f;
    public float unitX = 2.981963f;

    public Outline current;
    public GameObject selected_piece;
    public bool selected = false;

    /* Store the ID's of each object */
    public IDictionary<int, GameObject> BLACK_ID;
    public IDictionary<int, GameObject> WHITE_ID;
    IDictionary<int, IDictionary<int, GameObject>> SQUARE_ID;
    
    /* Dead Pieces as Dict with count*/
    IDictionary<string, int> DEAD_WHITE;
    IDictionary<string, int> DEAD_BLACK;

    /* Materials for objects */
    public Material WHITE_MATERIAL;
    public Material BLACK_MATERIAL;

    /* Materials for squares */
    public Material SQUARE_ENEMY;
    public Material SQUARE_TARNSPARENT;
    public Material SQUARE_FIRENDLY;
    public Material SQUARE_NEUTRAL;

    public int SQUARE_UNIT = 3; 
    public float GROUND_LAYER = -0.4129999f; // z coordinate == ground
    public float HORSE_GROUND_LAYER = 0.581f; // special case 
    public Vector3 ZeroZero = new Vector3(7.503f, 0, -13.654f); // of squares
    public Vector3 BlackZeroZero;

    public int[,] boardWhite;
    public int[,] boardBlack;
    List<int[]> availableMoves;
    List<Outline> enemy_outlines;
    public string turn = "white";
    private float time0;
    private bool turn_change = false;
    public float TurnChangeWaitTime = 0.1f;

    public Vector3[] DEAD_WHITE_POSITIONS;
    public Vector3[] DEAD_BLACK_POSITIONS;
    public int deadBlack = 0;
    public int deadWhite = 0;

    public RectTransform winnerBlack;
    public RectTransform winnerWhite;
    public bool gameEnded = false;

    // Stupid non DRY function
    public void move(GameObject piece, int row, int col, bool relativeToWhite=true, bool update=true, bool isHorse=false, int fromRow=13, int fromCol=13) {
        int id = piece.GetComponent<pieceCode>().id;
        float GROUND_LEVEL = isHorse ? HORSE_GROUND_LAYER : GROUND_LAYER;


        if (relativeToWhite) {
            piece.transform.position = ZeroZero + new Vector3(row * SQUARE_UNIT, GROUND_LEVEL, -col * SQUARE_UNIT);
            if (update) {
                int[] cords = this.BlackToWhite(row, col);

                boardWhite[row, col] = id;
                boardBlack[cords[0], cords[1]] = id;
                if (fromRow != 13) {
                    int[] fromCords = this.BlackToWhite(fromRow, fromCol);
                    boardWhite[fromRow, fromCol] = 0;
                    boardBlack[fromCords[0], fromCords[1]] = 0;
                }
            }
        }
        else {
            piece.transform.position = BlackZeroZero + new Vector3(-row * SQUARE_UNIT, GROUND_LEVEL, col * SQUARE_UNIT);
            
            if (update) {
                int[] cords = this.BlackToWhite(row, col);

                boardBlack[row, col] = id; 
                boardWhite[cords[0], cords[1]] = id; 
                if (fromRow != 13) {
                    int[] fromCords = this.BlackToWhite(fromRow, fromCol);
                    boardBlack[fromRow, fromCol] = 0;
                    boardWhite[fromCords[0], fromCords[1]] = 0;
                }

            }
        }
    }

    void setMaterial(ref GameObject item, Material material) {
        Renderer renderer = item.GetComponent<Renderer>();
        renderer.material = material;
    }

    public void init_dead_positions() {
        DEAD_BLACK_POSITIONS = new Vector3[16];
        DEAD_WHITE_POSITIONS = new Vector3[16];
        GameObject[] dead_white = GameObject.FindGameObjectsWithTag("dead_white"); 
        GameObject[] dead_black = GameObject.FindGameObjectsWithTag("dead_black");
                
        for (int i=0; i < 16; i++) {
            DEAD_BLACK_POSITIONS[i] = dead_black[i].transform.position;
            DEAD_WHITE_POSITIONS[i] = dead_white[i].transform.position;
            Destroy(dead_black[i]);
            Destroy(dead_white[i]);
        }

        this.sortVector3Array(DEAD_WHITE_POSITIONS, 0, dead_white.Length, "x", increasing:true);
        this.sortVector3Array(DEAD_BLACK_POSITIONS, 0, dead_black.Length, "x", increasing:false);
    }


    void swap(Vector3[] array, int i0, int i1) {
        Vector3 var = array[i0];
        array[i0] = array[i1];
        array[i1] = var;
    }
    float get_cords_at(Vector3 vec3, string axis) {
        switch(axis) {
            case "x":
                return vec3.x;
            case "y":
                return vec3.y;
            case "z":
                return vec3.z;
            default:
                throw new Exception($"Invalid axis {axis}");
        }
    }

    bool compare(float a, float b, bool increasing) {
        if (increasing) {
            return a > b;
        } return a < b;
    }

    void sortVector3Array(Vector3[] array, int start, int end, string axis, bool increasing=false) {

        for (int i = start + 1; i != end; i++)
        {            
            if ( compare(get_cords_at(array[i], axis), get_cords_at(array[i - 1], axis), increasing) ) continue;
            else swap(array, i, i - 1);
            for (int j = i - 1; j > start; --j)
            {
                if ( compare( get_cords_at(array[j], axis), get_cords_at(array[j - 1], axis), increasing) ) break;
                swap(array, j, j - 1);
            }
        }
    }


    public void setTurn(string color) {
        this.turn = color;
        movements.turn = color;

    }

    public int[] BlackToWhite(int row, int col) {
        int[] arr = {7 - row, 7 - col};
        return arr;
    }

    List<int[]> TransformListToBlack(List<int[]> available) { 
        List<int[]> new_available = new List<int[]>();
        foreach(int[] point in available) {
            int[] new_point = new int[3];
            int[] tranformed = this.BlackToWhite(point[0], point[1]);
            
            new_point[0] = tranformed[0];
            new_point[1] = tranformed[1];
            new_point[2] = point[2];

            new_available.Add(new_point);
        }
        return new_available;
    }

    void appendGlobalItem(ref GameObject item, int id, int row, int col, string team, string type) {
        pieceCode code = item.AddComponent<pieceCode>();
        code.id = id; code.row = row; code.col = col;

        if (team == "white") {
            WHITE_ID.Add(code.id, item);
            item.tag = "white";
            setMaterial(ref item, WHITE_MATERIAL);
        } else {
            BLACK_ID.Add(code.id, item);
            item.tag = "black";
            setMaterial(ref item, BLACK_MATERIAL);
        }

        code.type = type;
    }

    void init_soliders() {

       for (int i=0; i < 8; i++) 
       {
           GameObject solider_new = Instantiate(solider);
           this.appendGlobalItem(ref solider_new, i + 1, 1, i, "white", "solider");           
           this.move(solider_new, 1, i, relativeToWhite:true);
       }

        for (int i=0; i < 8; i++) 
        {
           GameObject solider_new = Instantiate(solider);   
           this.appendGlobalItem(ref solider_new, i + 1, 1, i, "black", "solider");
           this.move(solider_new, 1, i, relativeToWhite:false);
        }

       Destroy(solider);
    }


    int instanciate(GameObject piece, int[] cols, int start_id, string type, int len_of_piece=2) {
        int GLOBAL_ID = start_id;
        const int ROW = 0;
        
        bool isImperial = (type == "queen") || (type == "king");
        bool isHorse = (type == "horse");

        for (int i=0; i < 2; i++) {
            GLOBAL_ID = start_id;
            for (int j=0; j < len_of_piece; j++) {
                GameObject new_piece = Instantiate(piece);
                
                this.appendGlobalItem(
                        ref new_piece, GLOBAL_ID, ROW, isImperial ? cols[i] : cols[j], 
                        i == 0 ? "white" : "black", type
                    );

                setMaterial(ref new_piece, i == 0 ? WHITE_MATERIAL : BLACK_MATERIAL);

                if (i == 1 && isHorse) {
                    new_piece.transform.Rotate(new Vector3(0f, 0f, -180f));
                }

                this.move(new_piece, 0, isImperial ? cols[i] : cols[j], i == 0 ? true : false, true, isHorse);
                GLOBAL_ID++;
            }
        }

        Destroy(piece);
        return GLOBAL_ID;
    }


    void init_squares() {
        Vector3 scale = new Vector3(3.000685f, 1, 3.0f);
        Vector3 pos = new Vector3(7.503f, -0.913f, -13.654f);

        /* Square Indexing is based on black (0, 0) */
        for (uint j=0; j < 8; j++) {
            Vector3 localPos = pos + unitX * -new Vector3(j, 0, 0);
            var ROW_TABLE = new Dictionary<int, GameObject>();
            SQUARE_ID.Add((int)j, ROW_TABLE);
            for (uint i=0; i < 8; i++) {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.tag = "SQUARE";
                cube.transform.position = localPos + 3.0f * new Vector3(0, 0, i);
                cube.transform.localScale = scale; 
                cube.AddComponent<BoxCollider>();

                pieceCode code = cube.AddComponent<pieceCode>();
                code.row = (int)j;
                code.col = (int)i;

                ROW_TABLE.Add((int)i, cube);

                var renderer = cube.GetComponent<MeshRenderer>();
                renderer.material = SQUARE_TARNSPARENT;
            };
        };
    }


    void InitializePieces() {
        this.init_soliders();

        int gid = 9;
        gid = this.instanciate(HORSE,       new int[] {1, 6}, gid, "horse");
        gid = this.instanciate(TOWER,       new int[] {0, 7}, gid, "tower");
        gid = this.instanciate(LEUTENANT,   new int[] {2, 5}, gid, "leutenant");
        
        gid = this.instanciate(KING,        new int[] {4, 3}, gid, "king", 1);
        gid = this.instanciate(QUEEN,       new int[] {3, 4}, gid, "queen", 1);
    }


    public void __init__() {
        this.init_squares();
        this.InitializePieces();
    }


    void applyCurrent(ref Outline __outline__) {
        __outline__.OutlineColor = new Color(0.129f, 0.981f, 0.009f, 1f);
        __outline__.OutlineWidth = 4f;
    }

    void applyEnemy(ref GameObject enemy) {
        Outline enemy_outline = enemy.AddComponent<Outline>();
        enemy_outline.OutlineColor = new Color(0.764151f, 0.32f, 0.2955678f, 1f);
        enemy_outline.OutlineWidth = 4f;
        this.enemy_outlines.Add(enemy_outline);
    }

    void applySoliderSelect(ref GameObject dead_piece) {
        Outline __outline__ = dead_piece.AddComponent<Outline>();
        __outline__.OutlineColor = new Color(0.7f, 0.3f, 0.2f);
        __outline__.OutlineWidth = 4f;
    }


    public void PrintArray2d(int[,] arr, int rows, int cols) {
        string buffer = "";
        for (int i=0; i < 8; i++) {
            string innerBuffer = "";
            for (int j=0; j < 8; j++) {
                innerBuffer += $" {arr[i, j]}";
            }
            buffer += innerBuffer + "\n";
        }        
        print(buffer);
    }

    GameObject getSquare(int row, int col) {
        IDictionary<int, GameObject> current_square;
        SQUARE_ID.TryGetValue(row, out current_square);

        GameObject square;
        current_square.TryGetValue(col, out square);

        return square;
    }

    void toggle_avaiable(List<int[]> available) {
        foreach(int[] point in available) 
        {
            GameObject block = this.getSquare(point[0], point[1]);
            if (point[2] == 0) {
                this.setMaterial(ref block, SQUARE_FIRENDLY);
                continue;
            }
            this.setMaterial(ref block, SQUARE_ENEMY);
            GameObject enemy;
            if (turn == "black")
                this.WHITE_ID.TryGetValue(boardBlack[point[0], point[1]], out enemy);
            else
                this.BLACK_ID.TryGetValue(boardBlack[point[0], point[1]], out enemy);
            this.applyEnemy(ref enemy);
        }
    }

    void destroyAvailableMoves() {
        Destroy(current);
        foreach(int[] point in availableMoves) {
            GameObject square = this.getSquare(point[0], point[1]);
            this.setMaterial(ref square, SQUARE_TARNSPARENT);
        }
        foreach(Outline enemy_outline in enemy_outlines) {
            Destroy(enemy_outline);
        }
    }

    void test()
    {
        /*
            When turns change: (do the following for the one who's turn has just started)
                1. See if there exists an enemy piece which can move in one of the kings positions 
                    1.1.0 if yes, prevent the king from moving to that position
                2. See if there exists a friendly piece which when moved makes the king vulnurable
                    with the board only containing the king (from friendly pieces):
                        2.0.0 Check all the possible positions of every enemy piece 
                        2.0.1 if one piece can attack the king:
                             

        */
        GameObject KING; BLACK_ID.TryGetValue(15, out KING);
        pieceCode  KING_ID = KING.GetComponent<pieceCode>();
        List<int[]> KING_MOVES = movements.handle_piece(KING_ID.row, KING_ID.col, "king", boardBlack);


        foreach (var piece in WHITE_ID)
        {
            pieceCode code = piece.Value.GetComponent<pieceCode>();
            List<int[]> moves = movements.handle_piece(code.row, code.col, code.type, boardWhite);            
            
            List<int[]> common = Enumerable.Union(moves, KING_MOVES).ToList();
            if (common.Count == 0) continue;

        }
    }

    void PaintAvailableSquares(Collider pieceHitByRay) {
        pieceCode SEL_PIECE = pieceHitByRay.gameObject.GetComponent<pieceCode>();

        List<int[]> bMoves;
        int[] CURRENT_POINT;

        print($"TURN is {this.turn}");

        if (turn == "white") {
            Debug.Log("white succeded");
            List<int[]> moves = movements.handle_piece(SEL_PIECE.row, SEL_PIECE.col, SEL_PIECE.type, boardWhite); 
            bMoves = this.TransformListToBlack(moves); /* <IMPORTANT> White points are translated to black </IMPORTANT> */
            CURRENT_POINT = this.BlackToWhite(SEL_PIECE.row, SEL_PIECE.col);
        } 
        else {
            Debug.Log("black succeded");
            bMoves = movements.handle_piece(SEL_PIECE.row, SEL_PIECE.col, SEL_PIECE.type, boardBlack);
            CURRENT_POINT = new int[] {SEL_PIECE.row, SEL_PIECE.col};
        }

        
        this.toggle_avaiable(bMoves);

        // Paint point in which the piece is sitting in point blue
        CURRENT_POINT = new int[3] {CURRENT_POINT[0], CURRENT_POINT[1], 2};

        GameObject current_square = this.getSquare(CURRENT_POINT[0], CURRENT_POINT[1]);
        this.setMaterial(ref current_square, SQUARE_NEUTRAL);
        
        bMoves.Add(CURRENT_POINT);
        availableMoves = bMoves;
    }

    void toggle(Camera localCamera) {
        orbitControl control = localCamera.GetComponent<orbitControl>();
        control.enabled = !control.enabled;
    }

    void switch_turns() {
        cameraBlack.enabled = !cameraBlack.enabled;
        cameraWhite.enabled = !cameraWhite.enabled;
        
        cam = cameraBlack.enabled ? cameraBlack.transform : cameraWhite.transform;
        turn = turn == "white" ? "black" : "white";
        movements.turn = turn;

        toggle(cameraBlack);
        toggle(cameraWhite);
    }

    public void unSelect() {
        this.destroyAvailableMoves();
        selected = false;
        availableMoves = new List<int[]>();
        selected_piece = null;
        current = null;
    }

    public void Kill(int row, int col, string team_of_killed) 
    {   
        int PID = boardBlack[row, col];
        Vector3    DEAD_POSITION;
        GameObject dead_piece;
        pieceCode  code;
        string     tag;

        if (team_of_killed == "white") 
            {
                WHITE_ID.TryGetValue(PID, out dead_piece);
                WHITE_ID.Remove(PID);
                
                code = dead_piece.GetComponent<pieceCode>();
                DEAD_POSITION = DEAD_WHITE_POSITIONS[deadWhite];
                
                deadWhite++;
                tag = "dead_white";
            } 
        else 
            {
                BLACK_ID.TryGetValue(PID, out dead_piece);
                BLACK_ID.Remove(PID);
                
                code = dead_piece.GetComponent<pieceCode>();
                DEAD_POSITION = DEAD_BLACK_POSITIONS[deadBlack];
                
                deadBlack++;
                tag = "dead_black";
            }

        dead_piece.tag = tag;
        dead_piece.transform.position = DEAD_POSITION;
        

        int[] point = this.BlackToWhite(row, col);
        boardBlack[row, col] = 0;
        boardWhite[point[0], point[1]] = 0;
    }

    void init_dictionaries() {
        string[] pieces = new string[] {"solider", "tower", "leutenant", "king", "queen", "horse"};
        DEAD_BLACK = new Dictionary<string, int>();
        DEAD_WHITE = new Dictionary<string, int>();

        foreach (string piece in pieces) {
            DEAD_WHITE.Add(piece, 0);
            DEAD_BLACK.Add(piece, 0);
        };

    }

    public void __init__dicts() {
        boardWhite = new int[8, 8];
        boardBlack = new int[8, 8];
    
        this.winnerBlack.gameObject.SetActive(false);
        this.winnerWhite.gameObject.SetActive(false);

        SQUARE_ID = new Dictionary<int, IDictionary<int, GameObject>>();
        BLACK_ID  = new Dictionary<int, GameObject>();
        WHITE_ID  = new Dictionary<int, GameObject>();

        movements = new PieceMovements();
        movements.WHITE_ID = WHITE_ID;
        movements.BLACK_ID = BLACK_ID;
        movements.turn = "white";


        BlackZeroZero = ZeroZero;
        ZeroZero += new Vector3(7 * -SQUARE_UNIT, 0, 7 * SQUARE_UNIT);


        availableMoves = new List<int[]>();
        enemy_outlines = new List<Outline>();
    }

    /* Select A piece and show it's available moves */
    public void select(Collider collider) {

        this.destroyAvailableMoves();

        Outline __outline__ = collider.gameObject.AddComponent<Outline>();
        applyCurrent(ref __outline__);

        selected_piece = collider.gameObject;
        current = __outline__;
        selected = true;

        this.PaintAvailableSquares(collider);                        
    }

    /* See if the selected point is in the available ones and return it */
    int[] get_point(pieceCode target) {
        if (!selected) return null;
        int[] TARGET_POINT = null;
        foreach (int[] point in availableMoves) {
            bool isEqualToPoint = (point[0] == target.row) && (point[1] == target.col) && (point[2] != 2);
            if (isEqualToPoint) {
                TARGET_POINT = point;
            }
        }
        return TARGET_POINT;
    }

    public Tuple<int[], pieceCode,bool,pieceCode, int[]> GetMoveData(Collider collider)
    {
        // The square clicked
        pieceCode target = collider.gameObject.GetComponent<pieceCode>();  

        int[] TARGET_POINT = this.get_point(target);
        if (TARGET_POINT == null) 
            return null;

        // The piece seleceted                        
        pieceCode piece_code = selected_piece.GetComponent<pieceCode>();
        int[] selected_piece_cords; 

        // Convert the position of the piece to global cooridantes (black coordinates)
        if (turn == "white") 
            selected_piece_cords = this.BlackToWhite(piece_code.row, piece_code.col);
        else
            selected_piece_cords = new int[] {piece_code.row, piece_code.col};

        // Player tries to move where they are
        if (selected_piece_cords[0] == target.row && selected_piece_cords[1] == target.col) return null;
        

        int[] LocalTargetCords;
        if (turn == "white") 
            LocalTargetCords = this.BlackToWhite(target.row, target.col);
        else 
            LocalTargetCords = new int[] {target.row, target.col}; 

        bool isEnemy = movements.isEnemy(
            LocalTargetCords[0], LocalTargetCords[1],
            turn == "white" ? boardWhite : boardBlack);
        
        var mData = Tuple.Create<int[], pieceCode,bool,pieceCode, int[]> (selected_piece_cords, target, isEnemy, piece_code, LocalTargetCords);
        return mData;
    }

    string gameWinner() {
        if (WHITE_ID.Count == 0)
            return "black";
        else if (BLACK_ID.Count == 0)
            return "white";
        else
            return "";
    }

    void setWinner(string winner) {
        gameEnded = true;
        if (winner == "white") {
            this.winnerBlack.gameObject.SetActive(false);
            this.winnerWhite.gameObject.SetActive(true);
            return;
        }
        this.winnerBlack.gameObject.SetActive(true);
        this.winnerWhite.gameObject.SetActive(false);
        time0 = Time.time;
    }

    public void HandleMoveData(Tuple<int[], pieceCode,bool,pieceCode, int[]> moveData) {
        pieceCode TO = moveData.Item2; 
        pieceCode PIECE_CODE = moveData.Item4;
        int[] FROM = moveData.Item1;
        int[] LocalTargetCords = moveData.Item5;

        if (moveData.Item3) // Is enemy
            this.Kill(TO.row, TO.col, turn == "white" ? "black" : "white"); // movedata.TO.row, movedata.To.Col

        this.move(
              piece:  selected_piece,
                row:  TO.row,
                col:  TO.col,
    relativeToWhite:  false,
             update:  true,
            isHorse:  PIECE_CODE.type == "horse" ? true : false,
            fromRow:  FROM[0],
            fromCol:  FROM[1]
        );
        
        MOVE_AUDIO.Play();
    
        PIECE_CODE.row = LocalTargetCords[0];
        PIECE_CODE.col = LocalTargetCords[1];

        this.unSelect();
        turn_change = true;
        time0 = Time.time;

        string winner = this.gameWinner();
        if (winner != "") {
            setWinner(winner);
        }
        return;            
    }

    void useNormalGraphics(GameObject CAMERA_OBJECT)
    {
        Destroy(CAMERA_OBJECT.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>());
        Destroy(CAMERA_OBJECT.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>());

    }

    void Start()
    {
        this.winnerBlack.gameObject.SetActive(false);
        this.winnerWhite.gameObject.SetActive(false);

        if (NetData.fancyGraphics) 
            this.GetComponent<settingsApplier>().main(NetData.settings);
        else {
            this.useNormalGraphics(cameraBlack.gameObject);
            this.useNormalGraphics(cameraWhite.gameObject);
        }

        boardWhite = new int[8, 8];
        boardBlack = new int[8, 8];

        /* ID's */
        SQUARE_ID = new Dictionary<int, IDictionary<int, GameObject>>();
        BLACK_ID  = new Dictionary<int, GameObject>();
        WHITE_ID  = new Dictionary<int, GameObject>();

        movements = new PieceMovements();
        movements.turn = "white";
        movements.WHITE_ID = WHITE_ID;
        movements.BLACK_ID = BLACK_ID;


        BlackZeroZero = ZeroZero;
        ZeroZero += new Vector3(7 * -SQUARE_UNIT, 0, 7 * SQUARE_UNIT);

        this.__init__();
        this.init_dictionaries();

        availableMoves = new List<int[]>();
        enemy_outlines = new List<Outline>();

        cam = cameraWhite.transform;
        cameraBlack.enabled = false;
        toggle(cameraBlack);
        this.init_dead_positions();        
    }

    void Update()
    {
        if (gameEnded) {
            if (Time.time - time0 >= TurnChangeWaitTime) {
                UnityEngine.SceneManagement.SceneManager.LoadScene("menu");
            }
        }

        if (this.turn_change) {
            if ((Time.time - time0) >= TurnChangeWaitTime) {
                this.switch_turns();
                turn_change = false;
            }
            return;
        }

        Vector3 mpos = Input.mousePosition;
        Ray pos_ray = Camera.main.ScreenPointToRay(mpos);
        RaycastHit hit;

        if (Physics.Raycast(pos_ray.origin, pos_ray.direction, out hit, 100.0f)) {
            Collider collider = hit.collider;
            pieceCode code = collider.GetComponent<pieceCode>();
            if (code) {
                if (Input.GetMouseButtonDown(0)) {
                    if (collider.tag == turn) {
                        if (collider.gameObject == selected_piece) {
                            this.unSelect();
                            return;
                        }
                        this.select(collider);                        
                        return;
                    }
                    else if (collider.tag == "SQUARE") {
                        Tuple<int[], pieceCode,bool,pieceCode, int[]> moveData = this.GetMoveData(collider);
                        if (moveData != null) 
                            this.HandleMoveData(moveData);
                    }

                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape) && selected) {
            this.unSelect();
        };
    } 
}
