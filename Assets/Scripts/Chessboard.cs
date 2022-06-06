using System;
using System.Collections.Generic;
using UnityEngine;

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


    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    //LOGIC
    private ChessPiece[,] chessPieces;
    private ChessPiece currentlyDragging;
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;

    // Start is called before the first frame update
    void Start()
    {
        tileSize = 0.721f;
        yOffset = 0.01f;
        generateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        spawnAllPieces();
        positionAllPieces();
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
        //100 is arbitrary length of the ray that is cast, long rays take more computation
        //LayerMask filters out all objects that are hit that are not called "Tile"
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover")))
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
                //Change the previous tile's layer back to type "Tile"
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                
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
                    if (true)
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];
                    }
                }
            }
            //If we release the mousebutton
            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {   
                //Where the piece was dragged from
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);
                bool validmove = moveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                //If the attempted move was not valid
                if (!validmove)
                {
                    currentlyDragging.setPosition( getTileCenter(previousPosition.x, previousPosition.y));
                    currentlyDragging = null;
                }
                else
                {
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
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }

            if (currentlyDragging && Input.GetMouseButtonUp(0))
            {
                currentlyDragging = null;
                currentlyDragging.setPosition(getTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));

            }
        }

        // If we're currently dragging a piece
        if (currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if (horizontalPlane.Raycast(ray, out distance))
            {
                currentlyDragging.setPosition( ray.GetPoint(distance) + Vector3.up * dragOffset);
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
        vertices[1] = new Vector3(x * tileSize, yOffset, (y+1) * tileSize) - bounds;
        vertices[2] = new Vector3((x+1) * tileSize, yOffset, y * tileSize) - bounds;
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
        ChessPiece cp = Instantiate(prefabs[((int)type - 1) + team*6], transform).GetComponent<ChessPiece>();
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
        chessPieces[x, y].setPosition(getTileCenter(x,y),snapToTile);
    }
    private Vector3 getTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize/2, 0, tileSize/2);
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
    private bool moveTo(ChessPiece cp, int x, int y)
    {
        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        //Is there another piece on the targeted tile
        if (chessPieces[x, y] != null)
        {
            ChessPiece otherPiece = chessPieces[x, y];
            if (cp.team == otherPiece.team)
            {
                return false;
            }
            //If its the enemy team's tile
            if (otherPiece.team == 0)
            {
                deadWhites.Add(otherPiece);
                otherPiece.setScale(Vector3.one * deathSize);
                otherPiece.setPosition(
                    new Vector3(8 * tileSize, yOffset, -1 * tileSize)
                    - bounds + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.forward * deathSpacing) * deadWhites.Count);
            }
            else
            {
                deadBlacks.Add(otherPiece);
                otherPiece.setScale(Vector3.one * deathSize);
                otherPiece.setPosition(
                    new Vector3(-1 * tileSize, yOffset, 8 * tileSize)
                    - bounds + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.back * deathSpacing) * deadBlacks.Count);
            }
        }

        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        positionSinglePiece(x, y);

        return true;
    }
}
