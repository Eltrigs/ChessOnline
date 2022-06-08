using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ChessPieceType
{
    None = 0,
    Pawn = 1,
    Rook = 2,
    Knight = 3,
    Bishop = 4,
    Queen = 5,
    King = 6,
}


public class ChessPiece : MonoBehaviour
{
    private void Start()
    {
        transform.rotation = Quaternion.Euler((team == 0) ? new Vector3(-90,0,0) : new Vector3(-90, 180, 0));
    }


    public int team;
    public int currentX;
    public int currentY;
    public ChessPieceType type;

    //For smooth movements
    private Vector3 desiredPosition;
    private Vector3 desiredScale = Vector3.one;

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);

    }

    public virtual SpecialMove getSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> movelist, ref List<Vector2Int> availableMoves)
    {
        return SpecialMove.None;
    }

    public virtual void setPosition(Vector3 position, bool snapToTile = false)
    {
        desiredPosition = position;
        if (snapToTile)
        {
            transform.position = desiredPosition;
        }
    }
    public virtual void setScale(Vector3 scale, bool snapToTile = false)
    {
        desiredScale = scale;
        if (snapToTile)
        {
            transform.localScale = desiredScale;
        }
    }


    //ref makes it so that we don't duplicate the entire board for this function
    public virtual List<Vector2Int> getAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        result.Add(new Vector2Int(3, 3));
        result.Add(new Vector2Int(3, 4));
        result.Add(new Vector2Int(4, 3));
        result.Add(new Vector2Int(4, 4));

        return result;
    }
}
