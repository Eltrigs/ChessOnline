using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPiece
{
    public override List<Vector2Int> getAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        int direction = (team == 0) ? 1 : -1;

        //One step forward
        if (currentY != tileCountY - 1)
        {
            if (board[currentX, currentY + direction] == null)
            {
                result.Add(new Vector2Int(currentX, currentY + direction));
            }
        }

        //Two steps forward
        //are the tiles even empty?
        if ((team == 0 && currentY == 1) || (team == 1 && currentY == 6))
        {
            //is the pawn on it's first tile?
            if (board[currentX, currentY + 2 * direction] == null && board[currentX, currentY + direction] == null)
            {
                result.Add(new Vector2Int(currentX, currentY + direction*2));
            }
            
        }


        //kill move
        if (currentX != tileCountX - 1 && currentY != tileCountY - 1)
        {
            if (board[currentX - 1 , currentY + direction] != null)
            {
                if(board[currentX - 1, currentY + direction].team != team)
                    result.Add(new Vector2Int(currentX - 1, currentY + direction));
            }
        }
        if (currentX != tileCountX + 1 && currentY != tileCountY - 1)
        {
            if (board[currentX + 1, currentY + direction] != null)
            {
                if (board[currentX + 1, currentY + direction].team != team)
                {
                    result.Add(new Vector2Int(currentX + 1, currentY + direction));
                }
            }
        }


        return result;
    }
}
