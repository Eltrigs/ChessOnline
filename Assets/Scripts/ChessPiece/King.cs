using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class King : ChessPiece
{
    public override List<Vector2Int> getAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        int[,] movementMatrix = new int[,] { { 1, 1 }, { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, -1 }, { -1, 0 }, { -1, 1 }, { 0, 1 } };

        for (int i = 0; i < movementMatrix.Length / 2; i++)
        {
            if (currentX + movementMatrix[i, 0] < tileCountX
                && currentX + movementMatrix[i, 0] >= 0
                && currentY + movementMatrix[i, 1] < tileCountY
                && currentY + movementMatrix[i, 1] >= 0)
            {
                if (board[currentX + movementMatrix[i, 0], currentY + movementMatrix[i, 1]] == null
                    || board[currentX + movementMatrix[i, 0], currentY + movementMatrix[i, 1]].team != team)
                {
                    result.Add(new Vector2Int(currentX + movementMatrix[i, 0], currentY + movementMatrix[i, 1]));
                }
            }
        }
        return result;
    }
}