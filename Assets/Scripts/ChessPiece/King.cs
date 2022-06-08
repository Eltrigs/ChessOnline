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

    public override SpecialMove getSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> movelist, ref List<Vector2Int> availableMoves)
    {
        SpecialMove result = SpecialMove.None;

        //Find a king move from history
        var kingMove = movelist.Find(m => m[0].x == 4 && m[0].y == ((team == 0) ? 0 : 7));
        var leftRook = movelist.Find(m => m[0].x == 0 && m[0].y == ((team == 0) ? 0 : 7));
        var rightRook = movelist.Find(m => m[0].x == 7 && m[0].y == ((team == 0) ? 0 : 7));

        if (kingMove == null && currentX == 4)
        {
            //White team
            if (team == 0)
            {
                if(leftRook == null)
                {
                    //thing at 0,0 is a rook
                    if (board[0,0].type == ChessPieceType.Rook)
                    {
                        //thing at 0,0 is from my team
                        if (board[0,0].team == 0)
                        {
                            //there are no pieces in between king and rook
                            if (board[3,0] == null && board[2,0] == null && board[1,0] == null)
                            {
                                availableMoves.Add(new Vector2Int(2, 0));
                                result = SpecialMove.Castling;
                            }
                        }
                    }
                }
                if (rightRook == null)
                {
                    //thing at 0,0 is a rook
                    if (board[7, 0].type == ChessPieceType.Rook)
                    {
                        //thing at 0,0 is from my team
                        if (board[7, 0].team == 0)
                        {
                            //there are no pieces in between king and rook
                            if (board[5, 0] == null && board[6, 0] == null)
                            {
                                availableMoves.Add(new Vector2Int(6, 0));
                                result = SpecialMove.Castling;
                            }
                        }
                    }
                }
            }
            else
            {
                if (leftRook == null)
                {
                    //thing at 0,0 is a rook
                    if (board[0, 7].type == ChessPieceType.Rook)
                    {
                        //thing at 0,0 is from my team
                        if (board[0, 7].team == 1)
                        {
                            //there are no pieces in between king and rook
                            if (board[3, 7] == null && board[2, 7] == null && board[1, 7] == null)
                            {
                                availableMoves.Add(new Vector2Int(2, 7));
                                result = SpecialMove.Castling;
                            }
                        }
                    }
                }
                if (rightRook == null)
                {
                    //thing at 0,0 is a rook
                    if (board[7, 7].type == ChessPieceType.Rook)
                    {
                        //thing at 0,0 is from my team
                        if (board[7, 7].team == 1)
                        {
                            //there are no pieces in between king and rook
                            if (board[5, 7] == null && board[6, 7] == null)
                            {
                                availableMoves.Add(new Vector2Int(6, 7));
                                result = SpecialMove.Castling;
                            }
                        }
                    }
                }
            }
        }
        
        return result;
    }
}