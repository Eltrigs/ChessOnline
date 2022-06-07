using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bishop : ChessPiece
{
    public override List<Vector2Int> getAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        //Downwards to left
        for (int i = (currentX < currentY ? currentX -1 : currentY - 1); i >= 0; i--)
        {
            if (board[currentX - i, currentY - i] == null)
            {
                result.Add(new Vector2Int(currentX - i, currentY - i));
            }
            if (board[currentX - i, currentY - i] != null)
            {
                if (board[currentX - i, currentY - i].team != team)
                {
                    result.Add(new Vector2Int(currentX - i, currentY - i));
                }
                break;
            }
        }

        //Upwards right
        for (int x = currentX + 1, y= currentY + 1; x<tileCountX && y<tileCountY; x++, y++)
        {
            if (board[x, y] == null)
            {
                result.Add(new Vector2Int(x, y));
            }
            else
            {
                if (board[x, y].team != team)
                {
                    result.Add(new Vector2Int(x, y));
                }
                break;
            }
        }

        //Upwards left
        for (int x = currentX - 1, y = currentY + 1; x>=0 && y < tileCountY; x--, y++)
        {
            if (board[x, y] == null)
            {
                result.Add(new Vector2Int(x, y));
            }
            else
            {
                if (board[x, y].team != team)
                {
                    result.Add(new Vector2Int(x, y));
                }
                break;
            }
        }

        //Downwards right
        for (int x = currentX + 1, y = currentY - 1; x < tileCountX && y >= 0; x++, y--)
        {
            if (board[x, y] == null)
            {
                result.Add(new Vector2Int(x, y));
            }
            else
            {
                if (board[x, y].team != team)
                {
                    result.Add(new Vector2Int(x, y));
                }
                break;
            }
        }
        return result;
    }
}
