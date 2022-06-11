using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;

public enum SpecialMove
{
    None = 0,
    EnPassant,
    Castling,
    Promotion,
}
public class Chessboard : MonoBehaviour
{
    [Header("Art stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 0.2f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = new Vector3();
    [SerializeField] private float deathSize = 0.3f;
    [SerializeField] private float deathSpacing = 0.3f;
    [SerializeField] private float dragOffset = 0.3f;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private Transform rematchIndicator;
    [SerializeField] private Button rematchButton;

    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    //LOGIC
    private ChessPiece[,] chessPieces;
    private ChessPiece currentlyDragging;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private bool isWhiteTurn;

    private List<Vector2Int[]> moveList = new List<Vector2Int[]>();
    private SpecialMove specialMove;

    //Multiplayer logic
    private int playerCount = -1;
    private int currentTeam = -1;
    private bool localGame = true;
    private bool[] playerRematch = new bool[2];

    // Start is called before the first frame update
    void Start()
    {
        isWhiteTurn = true;
        tileSize = 0.721f;
        yOffset = 0.01f;
        generateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        spawnAllPieces();
        positionAllPieces();

        registerEvents();
    }
    void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);

        //Raycast sends out a ray from origin (which is currently generated by
        //mouse on a screen point, which itself is hooked to a camera.
        //info stores data about where the closest collider was hit
        //100 is an arbitrary length of the ray that is cast, long rays take more computation
        //LayerMask filters out all objects that are hit that are not called "Tile"
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
        {
            //transform is the position of the rigidbody collider that was hit.
            //Get the indexes (x,y) of the tile that was hit with mouse generated ray
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);


            //If hovering after not hovering any tile:
            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition; //Assign the hovered tile to class variable

                //Since hitPosition is a Vector2Int of a tile's location then we can
                //take the tile that was hit and change it's layer
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            //If we were already hovering a tile and now it's another tile:
            else if (currentHover != hitPosition)
            {
                //Change the previous tile's layer back to type "Tile" or "Highlight"
                tiles[currentHover.x, currentHover.y].layer = (containsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");

                currentHover = hitPosition; //Assign the hovered tile to class variable
                //Since hitPosition is a Vector2Int of a tile's location then we can
                //take the tile that was hit and change it's layer
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            //If we press down on the mouse
            if (Input.GetMouseButtonDown(0))
            {
                //And of there is something there aside from an empty tile
                if (chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    // Is it our turn?
                    if ((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn && currentTeam == 0)
                        || chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn && currentTeam == 1)
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];

                        //Ask for all the available moves for currentlyDragging
                        availableMoves = currentlyDragging.getAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                        //Get a list of special moves
                        specialMove = currentlyDragging.getSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);
                        //Pins
                        preventCheck();
                        //Highlight the available tiles
                        highlightTiles();
                    }
                }
            }
            //If we release the mousebutton
            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);
                
                if (containsValidMove(ref availableMoves, new Vector2(hitPosition.x, hitPosition.y)))
                {
                    moveTo(previousPosition.x, previousPosition.y, hitPosition.x, hitPosition.y);

                    // Net implementation
                    NetMakeMove mm = new NetMakeMove();
                    mm.originalX = previousPosition.x;
                    mm.originalY = previousPosition.y;
                    mm.destinationX = hitPosition.x;
                    mm.destinationY = hitPosition.y;
                    mm.teamID = currentTeam;
                    Client.Instance.sendToServer(mm);
                }
                //If the attempted move was not valid
                else
                {
                    currentlyDragging.setPosition(getTileCenter(previousPosition.x, previousPosition.y));
                    removeHighlightTiles();
                    currentlyDragging = null;
                }

            }
        }

        //If the ray gets nothing and therefore we are outside the board:
        else
        {
            //If the previous frame was on a tile, but now we are casting rays into empty space:
            if (currentHover != -Vector2Int.one)
            {
                //Change the previous tile's layer back to type "Tile"
                tiles[currentHover.x, currentHover.y].layer = (containsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }

            if (currentlyDragging && Input.GetMouseButtonUp(0))
            {
                currentlyDragging.setPosition(getTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                removeHighlightTiles();
            }
        }

        // If we're currently dragging a piece
        if (currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if (horizontalPlane.Raycast(ray, out distance))
            {
                currentlyDragging.setPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
            }
        }
    }

    //Generate the board
    private void generateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        yOffset += transform.position.y;
        bounds = new Vector3(tileCountX / 2 * tileSize, 0, (tileCountX / 2 * tileSize)) + boardCenter;

        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                tiles[x, y] = generateSingleTile(tileSize, x, y);
            }
        }
    }
    private GameObject generateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        //Connects tile to Chessboard class
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        //Tile vertice locations, spawn them slightly above chessboard 
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;

        //How triangles are made for mesh. In pairs of 3 attempts to make a triangle
        //according to the vertice indexes
        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        //mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");

        tileObject.AddComponent<BoxCollider>();
        return tileObject;
    }


    //Spawn pieces
    private void spawnAllPieces()
    {
        //new is full of nullptr's if declared like this:
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
        int whiteTeam = 0, blackTeam = 1;

        //White team
        chessPieces[0, 0] = spawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[1, 0] = spawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[2, 0] = spawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[3, 0] = spawnSinglePiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[4, 0] = spawnSinglePiece(ChessPieceType.King, whiteTeam);
        chessPieces[5, 0] = spawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[6, 0] = spawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[7, 0] = spawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i, 1] = spawnSinglePiece(ChessPieceType.Pawn, whiteTeam);
        }

        //White team
        chessPieces[0, 7] = spawnSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[1, 7] = spawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 7] = spawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[3, 7] = spawnSinglePiece(ChessPieceType.Queen, blackTeam);
        chessPieces[4, 7] = spawnSinglePiece(ChessPieceType.King, blackTeam);
        chessPieces[5, 7] = spawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[6, 7] = spawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[7, 7] = spawnSinglePiece(ChessPieceType.Rook, blackTeam);
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i, 6] = spawnSinglePiece(ChessPieceType.Pawn, blackTeam);
        }
    }
    private ChessPiece spawnSinglePiece(ChessPieceType type, int team)
    {
        ChessPiece cp = Instantiate(prefabs[((int)type - 1) + team * 6], transform).GetComponent<ChessPiece>();
        cp.type = type;
        cp.team = team;
        //cp.GetComponent<MeshRenderer>().material = teamMaterials[team];
        return cp;
    }


    //Positioning
    private void positionAllPieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    positionSinglePiece(x, y, true);
                }
            }
        }
    }

    // force value makes the movement instant if true
    private void positionSinglePiece(int x, int y, bool snapToTile = false)
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].setPosition(getTileCenter(x, y), snapToTile);
    }
    private Vector3 getTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    //Highlight tiles
    private void highlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
        }
    }
    private void removeHighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
        }
        availableMoves.Clear();
    }

    //Checkmate
    private void checkMate(int team)
    {
        displayVictory(team);
    }
    private void displayVictory(int team)
    {
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(team).gameObject.SetActive(true);

    }
    public void onRematchButton()
    {
        if (localGame)
        {
            NetRematch wrm = new NetRematch();
            wrm.teamID = 0;
            wrm.wantRematch = 1;
            Client.Instance.sendToServer(wrm);
            
            NetRematch brm = new NetRematch();
            brm.teamID = 1;
            brm.wantRematch = 1;
            Client.Instance.sendToServer(brm);
        }
        else
        {
            NetRematch rm = new NetRematch();
            rm.teamID = currentTeam;
            rm.wantRematch = 1;
            Client.Instance.sendToServer(rm);
        }

    }
    public void gameReset()
    {
        //UI
        rematchButton.interactable = true;
        rematchIndicator.transform.GetChild(0).gameObject.SetActive(false);
        rematchIndicator.transform.GetChild(1).gameObject.SetActive(false);
        //Take care of the UI
        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false);


        //Fields reset
        currentlyDragging = null;
        availableMoves.Clear();
        moveList.Clear();
        playerRematch[0] = playerRematch[1] = false;

        //Clean up chesspiece array
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    Destroy(chessPieces[x, y].gameObject);
                }
                chessPieces[x, y] = null;
            }
        }

        //Get rid of pieces on the sied
        for (int i = 0; i < deadWhites.Count; i++)
        {
            Destroy(deadWhites[i].gameObject);

        }
        for (int i = 0; i < deadBlacks.Count; i++)
        {
            Destroy(deadBlacks[i].gameObject);

        }

        deadWhites.Clear();
        deadBlacks.Clear();

        spawnAllPieces();
        positionAllPieces();
        isWhiteTurn = true;
    }
    public void onMenuButton()
    {
        NetRematch rm = new NetRematch();
        rm.teamID = currentTeam;
        rm.wantRematch = 0;
        Client.Instance.sendToServer(rm);
        Invoke("shutDownInX", 1.0f);
        gameReset();
        GameUI.Instance.onLeaveFromGameMenu();
        // Reset some values
        currentTeam = -1;
        playerCount = -1;
    }


    //Special Moves
    private void processSpecialMove()
    {
        if (specialMove == SpecialMove.EnPassant)
        {
            var newMove = moveList[moveList.Count - 1];
            ChessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y];

            var targetPawnPosition = moveList[moveList.Count - 2];
            ChessPiece enemyPawn = chessPieces[targetPawnPosition[1].x, targetPawnPosition[1].y];

            if (myPawn.currentX == enemyPawn.currentX)
            {
                if (myPawn.currentY == enemyPawn.currentY + 1 || myPawn.currentY == enemyPawn.currentY - 1)
                {
                    if (enemyPawn.team == 0)
                    {
                        deadWhites.Add(enemyPawn);
                        enemyPawn.setScale(Vector3.one * deathSize);
                        enemyPawn.setPosition(
                            new Vector3(8 * tileSize, yOffset, -1 * tileSize)
                            - bounds + new Vector3(tileSize / 2, 0, tileSize / 2)
                            + (Vector3.forward * deathSpacing) * deadWhites.Count);
                    }
                    else
                    {
                        deadBlacks.Add(enemyPawn);
                        enemyPawn.setScale(Vector3.one * deathSize);
                        enemyPawn.setPosition(
                            new Vector3(-1 * tileSize, yOffset, 8 * tileSize)
                            - bounds + new Vector3(tileSize / 2, 0, tileSize / 2)
                            + (Vector3.back * deathSpacing) * deadBlacks.Count);
                    }
                    chessPieces[enemyPawn.currentX, enemyPawn.currentY] = null;
                }
            }
        }

        if (specialMove == SpecialMove.Promotion)
        {
            Vector2Int[] lastmove = moveList[moveList.Count - 1];
            ChessPiece targetPawn = chessPieces[lastmove[1].x, lastmove[1].y];

            if (targetPawn.type == ChessPieceType.Pawn)
            {
                if (targetPawn.team == 0 && lastmove[1].y == 7)
                {
                    ChessPiece newQueen = spawnSinglePiece(ChessPieceType.Queen, 0);
                    newQueen.transform.position = chessPieces[lastmove[1].x, lastmove[1].y].transform.position;
                    Destroy(chessPieces[lastmove[1].x, lastmove[1].y].gameObject);
                    chessPieces[lastmove[1].x, lastmove[1].y] = newQueen;
                    positionSinglePiece(lastmove[1].x, lastmove[1].y, true);
                }
                else if (targetPawn.team == 1 && lastmove[1].y == 0)
                {
                    ChessPiece newQueen = spawnSinglePiece(ChessPieceType.Queen, 1);
                    newQueen.transform.position = chessPieces[lastmove[1].x, lastmove[1].y].transform.position;
                    Destroy(chessPieces[lastmove[1].x, lastmove[1].y].gameObject);
                    chessPieces[lastmove[1].x, lastmove[1].y] = newQueen;
                    positionSinglePiece(lastmove[1].x, lastmove[1].y, true);
                }
            }
        }

        if (specialMove == SpecialMove.Castling)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];

            //Going to left rook
            if (lastMove[1].x == 2)
            {
                if (lastMove[1].y == 0) //White side 0,0 rook
                {
                    ChessPiece rook = chessPieces[0, 0];
                    chessPieces[3, 0] = rook;
                    positionSinglePiece(3, 0);
                    chessPieces[0, 0] = null;
                }
                else if (lastMove[1].y == 7) //Black side 0,7 rook
                {
                    ChessPiece rook = chessPieces[0, 7];
                    chessPieces[3, 7] = rook;
                    positionSinglePiece(3, 7);
                    chessPieces[0, 7] = null;
                }
            }
            else if (lastMove[1].x == 6)
            {
                if (lastMove[1].y == 0) //White side 7,0 rook
                {
                    ChessPiece rook = chessPieces[7, 0];
                    chessPieces[5, 0] = rook;
                    positionSinglePiece(5, 0);
                    chessPieces[0, 0] = null;
                }
                else if (lastMove[1].y == 7) //Black side 7,7 rook
                {
                    ChessPiece rook = chessPieces[7, 7];
                    chessPieces[5, 7] = rook;
                    positionSinglePiece(5, 7);
                    chessPieces[7, 7] = null;
                }
            }
        }
    }
    private void preventCheck()
    {
        ChessPiece targetKing = null;
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    if (chessPieces[x, y].type == ChessPieceType.King)
                    {
                        if (chessPieces[x, y].team == currentlyDragging.team)
                        {
                            targetKing = chessPieces[x, y];
                        }
                    }
                }
            }
        }
        //Since we're seinding ref availableMoves, we will delete moves that are putting us in check
        simulateMoveForSinglePiece(currentlyDragging, ref availableMoves, targetKing);
    }
    private void simulateMoveForSinglePiece(ChessPiece cp, ref List<Vector2Int> moves, ChessPiece targetKing)
    {
        //Save the current values to reset after function call
        int actualX = cp.currentX;
        int actualY = cp.currentY;
        List<Vector2Int> movesToRemove = new List<Vector2Int>();

        //Go through all the moves, simulate them and check if we are in check
        for (int i = 0; i < moves.Count; i++)
        {
            int simX = moves[i].x;
            int simY = moves[i].y;

            Vector2Int kingPosThisSim = new Vector2Int(targetKing.currentX, targetKing.currentY);
            //Did we simulate the king's move
            if (cp.type == ChessPieceType.King)
            {
                kingPosThisSim = new Vector2Int(simX, simY);
            }

            //Copy the [,] not the reference (hard copy)
            ChessPiece[,] simulation = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
            List<ChessPiece> simAttackingPieces = new List<ChessPiece>();
            for (int x = 0; x < TILE_COUNT_X; x++)
            {
                for (int y = 0; y < TILE_COUNT_Y; y++)
                {
                    if (chessPieces[x, y] != null)
                    {
                        simulation[x, y] = chessPieces[x, y];
                        if (simulation[x, y].team != cp.team)
                        {
                            simAttackingPieces.Add(simulation[x, y]);
                        }
                    }
                }
            }

            //Simulate the move
            simulation[actualX, actualY] = null;
            cp.currentX = simX;
            cp.currentY = simY;
            simulation[simX, simY] = cp;

            //Did one of the pieces get taken down during simulation
            var deadPiece = simAttackingPieces.Find(c => c.currentX == simX && c.currentY == simY);
            if (deadPiece != null)
            {
                simAttackingPieces.Remove(deadPiece);
            }
            //Get all the simulated attacking pieces moves
            List<Vector2Int> simMoves = new List<Vector2Int>();
            for (int a = 0; a < simAttackingPieces.Count; a++)
            {
                var pieceMoves = simAttackingPieces[a].getAvailableMoves(ref simulation, TILE_COUNT_X, TILE_COUNT_Y);
                for (int b = 0; b < pieceMoves.Count; b++)
                {
                    simMoves.Add(pieceMoves[b]);
                }
            }

            //Does the enemy-covered tiles include your team's king location?
            if (containsValidMove(ref simMoves, kingPosThisSim))
            {
                movesToRemove.Add(moves[i]);
            }

            //Restore the piece to where it was
            cp.currentX = actualX;
            cp.currentY = actualY;
        }


        //Remove from the current availableMove list
        for (int i = 0; i < movesToRemove.Count; i++)
        {
            moves.Remove(movesToRemove[i]);
        }
    }
    private bool checkForCheckmate()
    {
        var lastMove = moveList[moveList.Count - 1];

        //If white is the one who attacked last move, then the targetTeam is black and we look for it's checkmate
        int targetTeam = (chessPieces[lastMove[1].x, lastMove[1].y].team == 0) ? 1 : 0;
        List<ChessPiece> attackingPieces = new List<ChessPiece>();
        List<ChessPiece> defendingPieces = new List<ChessPiece>();
        ChessPiece targetKing = null;
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    if (chessPieces[x, y].team == targetTeam)
                    {
                        defendingPieces.Add(chessPieces[x, y]);
                        if (chessPieces[x, y].type == ChessPieceType.King)
                        {
                            targetKing = chessPieces[x, y];
                        }
                    }
                    else
                    {
                        attackingPieces.Add(chessPieces[x, y]);
                    }
                }
            }
        }

        //Is the king attacked right now?
        List<Vector2Int> currentAvailableMoves = new List<Vector2Int>();
        for (int i = 0; i < attackingPieces.Count; i++)
        {
            var pieceMoves = attackingPieces[i].getAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            for (int b = 0; b < pieceMoves.Count; b++)
            {
                currentAvailableMoves.Add(pieceMoves[b]);
            }
        }

        if (containsValidMove(ref currentAvailableMoves, new Vector2Int(targetKing.currentX, targetKing.currentY)))
        {
            //King is under attack, can we move something to help
            for (int i = 0; i < defendingPieces.Count; i++)
            {
                List<Vector2Int> defendingMoves = defendingPieces[i].getAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                simulateMoveForSinglePiece(defendingPieces[i], ref defendingMoves, targetKing);
                if (defendingMoves.Count != 0)
                {
                    return false;
                }
            }
            return true; //Checkmate exit
        }
        return false;
    }


    //Operations
    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (tiles[x, y] == hitInfo)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return -Vector2Int.one; //This should not happen ever.
    }
    private void moveTo(int originalX, int originalY, int destinationX, int destinationY)
    {
        ChessPiece cp = chessPieces[originalX, originalY];
        Vector2Int previousPosition = new Vector2Int(originalX, originalY);

        //Is there another piece on the targeted tile
        if (chessPieces[destinationX, destinationY] != null)
        {
            ChessPiece otherPiece = chessPieces[destinationX, destinationY];
            if (cp.team == otherPiece.team)
            {
                return;
            }
            //If its the enemy team's tile
            if (otherPiece.team == 0)
            {
                if (otherPiece.type == ChessPieceType.King)
                {
                    checkMate(1);
                }
                deadWhites.Add(otherPiece);
                otherPiece.setScale(Vector3.one * deathSize);
                otherPiece.setPosition(
                    new Vector3(8 * tileSize, yOffset, -1 * tileSize)
                    - bounds + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.forward * deathSpacing) * deadWhites.Count);
            }
            else
            {
                if (otherPiece.type == ChessPieceType.King)
                {
                    checkMate(0);
                }
                deadBlacks.Add(otherPiece);
                otherPiece.setScale(Vector3.one * deathSize);
                otherPiece.setPosition(
                    new Vector3(-1 * tileSize, yOffset, 8 * tileSize)
                    - bounds + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.back * deathSpacing) * deadBlacks.Count);
            }
        }

        chessPieces[destinationX, destinationY] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        positionSinglePiece(destinationX, destinationY);
        isWhiteTurn = !isWhiteTurn;
        if (localGame)
        {
            currentTeam = (currentTeam == 0) ? 1 : 0;
        }
        moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(destinationX, destinationY) });

        processSpecialMove();

        removeHighlightTiles();
        if (currentlyDragging)
        {
            currentlyDragging = null;
        } 

        if (checkForCheckmate())
        {
            checkMate(cp.team);
        }
        return;
    }

    private bool containsValidMove(ref List<Vector2Int> moves, Vector2 pos)
    {
        for (int i = 0; i < moves.Count; i++)
        {
            if (moves[i].x == pos.x && moves[i].y == pos.y)
            {
                return true;
            }
        }
        return false;
    }


    #region
    private void registerEvents()
    {
        //Start listening to an action: whether a welcome message is sent
        NetUtility.S_WELCOME += onWelcomeServer;
        NetUtility.S_MAKE_MOVE += onMakeMoveServer;
        NetUtility.S_REMATCH += onRematchServer;
        NetUtility.C_WELCOME += onWelcomeClient;
        NetUtility.C_START_GAME += onStartGameClient;
        NetUtility.C_MAKE_MOVE += onMakeMoveClient;
        NetUtility.C_REMATCH += onRematchClient;

        GameUI.Instance.SetLocalGame += onSetLocalGame;
    }

    //Server messages
    //Client has connected. As a server, assign a team and return the message to client
    private void onRematchServer(NetMessage arg1, NetworkConnection arg2)
    {
        Server.Instance.broadcast(arg1);
    }
    private void onWelcomeServer(NetMessage msg, NetworkConnection connection)
    {
        NetWelcome nw = msg as NetWelcome;

        //Assign a team
        nw.AssignedTeam = ++playerCount;

        //Return back to client
        Server.Instance.sendToClient(connection, nw);

        //If full start the game
        if(playerCount == 1)
        {
            Server.Instance.broadcast(new NetStartGame());
        }
    }

    private void onMakeMoveServer(NetMessage msg, NetworkConnection connection)
    {
        NetMakeMove mm = msg as NetMakeMove;
        
        //Recieve and then broadcast it back.
        Server.Instance.broadcast(mm);
    }


    //Client messages
    private void onMakeMoveClient(NetMessage msg)
    {
        NetMakeMove mm = msg as NetMakeMove;
        Debug.Log($"MM: {mm.teamID} : {mm.originalX} {mm.originalY} -> {mm.destinationX} {mm.destinationY}");

        if (mm.teamID != currentTeam)
        {
            ChessPiece target = chessPieces[mm.originalX, mm.originalY];
            availableMoves = target.getAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            specialMove = target.getSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);
            moveTo(mm.originalX, mm.originalY, mm.destinationX, mm.destinationY);
        }
    }
    private void onRematchClient(NetMessage msg)
    {
        NetRematch rm = msg as NetRematch;
        playerRematch[rm.teamID] = rm.wantRematch == 1;

        //Activate UI
        if (rm.teamID != currentTeam)
        {
            rematchIndicator.transform.GetChild((rm.wantRematch == 1) ? 0 : 1).gameObject.SetActive(true);
            if (rm.wantRematch != 1)
            {
                rematchButton.interactable = false;
            }
        }

        //If both want to rematch
        if (playerRematch[0] && playerRematch[1])
        {
            gameReset();
        }
    }
    private void onWelcomeClient(NetMessage msg)
    {
        //Recieve the connection message
        NetWelcome nw = msg as NetWelcome;

        //Assign the team
        currentTeam = nw.AssignedTeam;

        Debug.Log($"My assigned team is: {nw.AssignedTeam}");

        if (localGame && currentTeam == 0)
        {
            Server.Instance.broadcast(new NetStartGame());
        }
    }
    private void onStartGameClient(NetMessage obj)
    {
        //We just need to change the camera
        GameUI.Instance.changeCamera((currentTeam == 0) ? CameraAngle.whiteTeam : CameraAngle.blackTeam);
    }

    private void unRegisterEvents()
    {
        //Start listening to an action: whether a welcome message is sent
        NetUtility.S_WELCOME -= onWelcomeServer;
        NetUtility.S_MAKE_MOVE -= onMakeMoveServer;
        NetUtility.S_REMATCH -= onRematchServer;
        NetUtility.C_WELCOME -= onWelcomeClient;
        NetUtility.C_START_GAME -= onStartGameClient;
        NetUtility.C_MAKE_MOVE -= onMakeMoveClient;
        NetUtility.C_REMATCH -= onRematchClient;

        GameUI.Instance.SetLocalGame -= onSetLocalGame;
    }
    #endregion

    private void shutDownInX()
    {
        Client.Instance.shutDown();
        Server.Instance.shutDown();
    }
    private void onSetLocalGame(bool obj)
    {
        playerCount = -1;
        currentTeam = -1;
        localGame = obj;
    }
}
