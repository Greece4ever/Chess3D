using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class PieceMovements {

    public IDictionary<int, GameObject> WHITE_ID = new Dictionary<int, GameObject>();
    public IDictionary<int, GameObject> BLACK_ID = new Dictionary<int, GameObject>();
    public string turn;
    const int NO_ENEMY = 0;

    int[] BlackToWhite(int row, int col) {
        int[] arr = {7 - row, 7 - col};
        return arr;
    }

    bool isEmpty(int row, int col, int[,] boardArray) {
        return (boardArray[row, col] == 0);
    }

    public bool isEnemy(int row, int col, int[,] boardArray)
    {
        int item = boardArray[row, col];
        if (item == 0)
            return false;

        GameObject boardPiece;
        WHITE_ID.TryGetValue(item, out boardPiece);       
        if (boardPiece == null) {
            if (turn == "white") return true;
            return false;
        }

        pieceCode code = boardPiece.GetComponent<pieceCode>();
        int[] check_row = new int[2] {code.row, code.col};

        if (turn == "black") {
            check_row = this.BlackToWhite(code.row, code.col);
        }

        if ((check_row[0] == row) &&  (check_row[1] == col)) 
        {
            if (turn == "white") return false;
            else return true;
        }
        return turn == "white" ? true : false;
    }

    int EmptyOrEnemy(int row, int col, int[,] boardArray)
    {
        if (this.isEmpty(row, col, boardArray))
            return 0;
        else if (this.isEnemy(row, col, boardArray))
            return 1;
        else // Firendly piece in (row, col)
            return 2; 
    }

    int isEnemyKing(
        /*
            0: Empty, continue;
            1: Friendly, stop;
            2: Enemy King;
            3: Enemy Piece; (Other)
        */
        int row, int col, int[,] boardArray) 
    {
        int piece = boardArray[row, col];
        if (piece == 0) // Empty
            return 0;

        GameObject PieceObject;
        string type;
        
        WHITE_ID.TryGetValue(piece, out PieceObject);
        if (PieceObject == null) { // BLACK PIECE
            if (turn == "black") // Friendly Object, must stop
                return 1;
            type = BLACK_ID[piece].GetComponent<pieceCode>().type;
        }
        else {
            pieceCode code = PieceObject.GetComponent<pieceCode>();
            int[] localCords;
            if (turn == "black") // transform black cords to white because code's cords is in white
                localCords = this.BlackToWhite(row, col);
            else
                localCords = new int[2] {row, col};

            Debug.Log($"Piece Info: ({code.row}, {code.col})");
            
            if (code.row == localCords[0] && code.col == localCords[0]) { // WHITE PIECE
                Debug.Log("White piece found");
                if (turn == "white")
                    return 1; // Stop Because Friendly piece
                type = code.type;
            } 
            else { // BLACK PIECE
                Debug.Log("Black Piece Found");
                if (turn == "black")
                    return 1; // stop
                type = BLACK_ID[piece].GetComponent<pieceCode>().type;            
            }
        }


        if (type == "king") {
            Debug.Log("KING FOUND");
            return 2;
        }
        Debug.Log($"ENEMY FOUND {type}");
        return 3;
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
        Debug.Log(buffer);
    }


    int propagate(int row, int col, int[,] boardArray, ref List<int[]> Cords2d) 
    {
        /*
            0: Stop Loop
            1: Continue
            2: Special
         */
        int result = this.EmptyOrEnemy(row, col, boardArray);
        if (result == 2) // Friendly
            return 0;
        Cords2d.Add(new int[] {row, col, result});
                
        if (result == 1) // Enemy
            return 2;
        else
            return 1;
    }


    GameObject get_enemy_piece(
        int[] cords, int[,] boardArray,
        IDictionary<int, GameObject> ENEMY_DICT) 
    {
        int piece = boardArray[cords[0], cords[1]];
        return ENEMY_DICT[piece];
    }

    System.Func<int, int, bool> greater_or_equal = new System.Func<int, int, bool>(
    (int a, int b) => {
         return a >= b;
    });

    System.Func<int, int, bool> less_or_equal = new System.Func<int, int, bool>(
    (int a, int b) => {
         return a <= b;
    });

    System.Func<int, int, bool> always_true = new System.Func<int, int, bool>(
        (int a, int b) => {
            return true;
        });


    int[] findNextPiece(
        ref int row, int row_increment, 
        ref int col, int col_increment,
        System.Func<int, int, bool> comparisonRow,
        System.Func<int, int, bool> comparisonCol,
        int targetRow, int targetCol,

        int[,] boardArray) 
    {
        while (comparisonRow(row + row_increment, targetRow) && comparisonCol(col + col_increment, targetCol)) 
        {
            int enemy = this.isEnemyKing(row + row_increment, col, boardArray);
            if (enemy == 0) { // Nothing found
                row += row_increment;
                col += col_increment;
                continue;
            }
            else if (enemy == 2) { // King found 
                return new int[] {row + row_increment};
            }
            else // Friendly piece or other enemy 
                return new int[] {69, 69}; 
        }
        return new int[] {69, 69};
    }


    void findMoves(
        ref int row, int row_increment,
        ref int col, int col_increment,
        System.Func<int, int, bool> comparisonRow,
        System.Func<int, int, bool> comparisonCol,
        
        int targetRow, int targetCol,
        int[,] boardArray,
        bool FIND_NEXT_PIECE) 
    {
        List<int[]> Cords = new List<int[]>();

        while (comparisonRow(row + row_increment, targetRow) && comparisonCol(col + col_increment, targetCol)) 
        {
            int forward = 2;
            // int forward = this.propagate(
                // row + row_increment, col + col_increment, 
                // boardArray,), ref Cords
            // );
            
            if (forward == 0) // Friendly Piece
                break;
            
            else if (forward == 2) { // Enemy Piece
                if (FIND_NEXT_PIECE)
                    this.findNextPiece(
                        ref row, row_increment, ref col, col_increment, 
                        comparisonRow, comparisonCol, targetRow, targetCol,
                        boardArray);
                break;
            }

            row += row_increment; 
            col += col_increment;
        }
    }

    void handle_tower(int row, int col, int[,] boardArray, List<int[]> Cords2d) 
    {
        int curr_row = row;
        int[] last_enemy = new int[2];
        bool stop = false;
        // bool kingFound = false;
        // GameObject EnemyPiece = null;

        while (curr_row + 1 <= 7) {
            if (stop) {
                int enemy = this.isEnemyKing(curr_row + 1, col, boardArray);
                if (enemy == 0) { // Nothing found
                    curr_row++;
                    continue;
                }
                else if (enemy == 2) { // King found
                    // kingFound = true;            
                    break;
                }
                else
                    break; // Friendly piece or other enemy 
            }

            int forward = this.propagate(curr_row + 1, col, boardArray, ref Cords2d);
            if (forward == 0) 
                break;
            else if (forward == 2) { // Enemy
                last_enemy[0] = curr_row + 1;
                last_enemy[1] = col;
                // EnemyPiece = this.get_enemy_piece(last_enemy, boardArray) == "white" ? BLACK_ID : WHITE_ID);
                stop = true; 
            }
            curr_row++;
        }

        // if (kingFound) {
            // Find all the possible places that the piece can move
            // without en-dangering it's king, for that we have
            // To find the Union of all the moves of the last_enemy with the current piece
            // Debug.Log($"King found, Coordinates of last piece at ({last_enemy[0]}, {last_enemy[1]}) ({turn} cords)");
            // var enemyCode = EnemyPiece.GetComponent<pieceCode>();
        // }
        
        
        curr_row = row;
        while (curr_row - 1 >= 0) {
            int result = this.EmptyOrEnemy(curr_row - 1, col, boardArray);
            if (result == 2) break;
            Cords2d.Add(new int[] {curr_row - 1, col, result});
            if (result == 1) break;            
            curr_row--;
        }

        int current_col = col;
        while (current_col + 1 <= 7)
        {
            int result = this.EmptyOrEnemy(row, current_col + 1, boardArray);
            if (result == 2) break;
            Cords2d.Add(new int[] {row, current_col + 1, result});
            if (result == 1) break;            
            current_col++;
        }  

        current_col = col;
        while (current_col - 1 >= 0)
        {
            int result = this.EmptyOrEnemy(row, current_col - 1, boardArray);
            if (result == 2) break;
            Cords2d.Add(new int[] {row, current_col - 1, result});
            if (result == 1) break;            
            current_col--;
        }  
    }

    void handle_leutenant(int row, int col, int[,] boardArray, List<int[]> Cords2d) 
    {
        int cur_row;
        int cur_col;

        cur_row = row;
        cur_col = col;
        while ((cur_row + 1 <= 7) && (cur_col + 1 <= 7))
        {
            int result = this.EmptyOrEnemy(cur_row + 1, cur_col + 1, boardArray);
            if (result == 2) break;
            Cords2d.Add(new int[] {cur_row + 1, cur_col + 1, result});
            if (result == 1) break;            
            cur_row++;
            cur_col++;
        }

        cur_row = row;
        cur_col = col;

        while ( (cur_row + 1 <= 7) && (cur_col - 1 >= 0 ) )
        {
            int result = this.EmptyOrEnemy(cur_row + 1, cur_col - 1, boardArray);
            if (result == 2) break;
            Cords2d.Add(new int[] {cur_row + 1, cur_col - 1, result});
            if (result == 1) break;            
            cur_row++;
            cur_col--;
        }

        cur_row = row;
        cur_col = col;

        while ((cur_row - 1 >= 0) && (cur_col - 1 >= 0))
        {
            int result = this.EmptyOrEnemy(cur_row - 1, cur_col - 1, boardArray);
            if (result == 2) break;
            Cords2d.Add(new int[] {cur_row - 1, cur_col - 1, result});
            if (result == 1) break;            
            cur_row--;
            cur_col--;            
        }

        cur_row = row;
        cur_col = col;

        while ((cur_row - 1 >= 0) && (cur_col + 1 <= 7))
        {
            int result = this.EmptyOrEnemy(cur_row - 1, cur_col + 1, boardArray);
            if (result == 2) break;
            Cords2d.Add(new int[] {cur_row - 1, cur_col + 1, result});
            if (result == 1) break;            
            cur_row--;
            cur_col++;
        }
    }

    void handleSolider(int row, int col, int[,] boardArray, ref List<int[]> Cords)
    {
        if (row == 7) return;
        // Forward Neuteral
        if (isEmpty(row + 1, col, boardArray)) {  
            Cords.Add(new int[] {row + 1, col, NO_ENEMY});
            if (row == 1 && isEmpty(row + 2, col, boardArray)) 
                Cords.Add(new int[] {row + 2, col, 0});
        }

        // Sideways enemy
        if (col != 7 && isEnemy(row + 1, col + 1, boardArray)) 
            Cords.Add(new int[] { row + 1, col + 1, 1});

        if (col != 0 && isEnemy(row + 1, col - 1, boardArray))
            Cords.Add(new int[] { row + 1, col - 1, 1});
    }

    void handleQueen(int row, int col, int[,] boardArray, ref List<int[]> Cords)
    {
        // Union of Tower's and Leutenant's positions
        List<int[]> lPositions = new List<int[]>();
        handle_tower(row, col, boardArray, Cords);
        handle_leutenant(row, col, boardArray, lPositions);
        Cords = Cords.Union(lPositions).ToList();
    }

    void handleKing(int row, int col, int[,] boardArray, List<int[]> Cords2d) 
    {
        if (col != 0) {
            {
                int result = this.EmptyOrEnemy(row, col - 1, boardArray);
                if (result != 2) Cords2d.Add(new int[]  {row, col - 1, result});
            }

            if (row != 7) {
                {
                    int result = this.EmptyOrEnemy(row + 1, col - 1, boardArray);
                    if (result != 2) Cords2d.Add(new int[]  {row + 1, col - 1, result});
                }
                {
                    int result = this.EmptyOrEnemy(row + 1, col, boardArray);
                    if (result != 2) Cords2d.Add(new int[]  {row + 1, col, result});
                }
            }

            if (row != 0) {
                {
                    int result = this.EmptyOrEnemy(row - 1, col - 1, boardArray);
                    if (result != 2) Cords2d.Add(new int[]  {row - 1, col - 1, result});
                }
                {
                    int result = this.EmptyOrEnemy(row - 1, col, boardArray);
                    if (result != 2) Cords2d.Add(new int[]  {row - 1, col, result});                        
                }
            }
        } else {
            if (row != 7) {
                int result = this.EmptyOrEnemy(row + 1, col, boardArray);
                if (result != 2) Cords2d.Add(new int[]  {row + 1, col, result});                        
            }
            if (row != 0) {
                int result = this.EmptyOrEnemy(row - 1, col, boardArray);
                if (result != 2) Cords2d.Add(new int[]  {row - 1, col, result});                        
            }
        }

        if (col != 7) {
            {
                int result = this.EmptyOrEnemy(row, col + 1, boardArray);
                if (result != 2) Cords2d.Add(new int[]  {row, col + 1, result});
            }

            if (row != 7) {
                int result = this.EmptyOrEnemy(row + 1, col + 1, boardArray);
                if (result != 2) Cords2d.Add(new int[]  {row + 1, col + 1, result});
            }

            if (row != 0) {
                int result = this.EmptyOrEnemy(row - 1, col + 1, boardArray);
                if (result != 2) Cords2d.Add(new int[]  {row - 1, col + 1, result});
            }

        }


    }

    void handleHorse(int row, int col, int[,] boardArray, List<int[]> Cords2d)
    {
        if (row < 6) { // At least two spaces forward (forward left and right) 
            if (col != 0) {
                int result = this.EmptyOrEnemy(row + 2, col - 1, boardArray);
                if (result != 2) Cords2d.Add(new int[]  {row + 2, col - 1, result});
            }
            if (col != 7) {
                int result = this.EmptyOrEnemy(row + 2, col + 1, boardArray);
                if (result != 2) Cords2d.Add(new int[]  {row + 2, col + 1, result});
            }
        }

        if (row != 7) { // At least one space forward (forward mid left and right)
            if (col >= 2) {
                int result = this.EmptyOrEnemy(row + 1, col - 2, boardArray);
                if (result != 2) Cords2d.Add(new int[]  {row + 1, col - 2, result});                    
            }

            if (col <= 5) {
                int result = this.EmptyOrEnemy(row + 1, col + 2, boardArray);
                if (result != 2) Cords2d.Add(new int[]  {row + 1, col + 2, result});                    

            }       
        }

        if (row >= 2) { // At least two spaces behind (backward left and right)
            if (col != 0) {
                int result = this.EmptyOrEnemy(row - 2, col - 1, boardArray);
                if (result != 2) Cords2d.Add(new int[]  {row - 2, col - 1, result});                    
            }

            if (col != 7) {
                int result = this.EmptyOrEnemy(row - 2, col + 1, boardArray);
                if (result != 2) Cords2d.Add(new int[]  {row - 2, col + 1, result});                    
            }
        }

        if (row != 0) { // (backward mid)
            if (col >= 2) {
                int result = this.EmptyOrEnemy(row - 1, col - 2, boardArray);
                if (result != 2) Cords2d.Add(new int[]  {row - 1, col - 2, result});                    
            }

            if (col <= 5) {
                int result = this.EmptyOrEnemy(row - 1, col + 2, boardArray);
                if (result != 2) Cords2d.Add(new int[]  {row - 1, col + 2, result});                    
            }

        }

    }


    public List<int[]> handle_piece(int row, int col, string type, int[,] boardArray)
    {
        Debug.Log($"Turn is {turn}");
        Debug.Log($"Row, Col is {row}, {col}");

        List<int[]> Cords2d = new List<int[]>();
        switch (type) {
            case "solider":
                this.handleSolider(row, col, boardArray, ref Cords2d);
                break;
            case "horse":                
                this.handleHorse(row, col, boardArray, Cords2d);
                break;
            case "tower":
                this.handle_tower(row, col, boardArray, Cords2d);
                break;
            case "leutenant":
                this.handle_leutenant(row, col, boardArray, Cords2d);
                break;
            case "queen":
                this.handleQueen(row, col, boardArray, ref Cords2d);
                break;
            case "king":
                this.handleKing(row, col, boardArray, Cords2d);
                break;
            default:
                break;
        }
        return Cords2d;
    }

}
