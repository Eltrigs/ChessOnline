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
        if (currentY != tileCountY - 1 && currentY != 0)
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


        //kill move to the left (cant do it if piece is on either end of the board)
        if (currentX != 0 && currentY != tileCountY - 1 && currentY != 0)
        {
            if (board[currentX - 1 , currentY + direction] != null)
            {
                if(board[currentX - 1, currentY + direction].team != team)
                    result.Add(new Vector2Int(currentX - 1, currentY + direction));
            }
        }
        //kill move to the right (cant do it if piece is on either end of the board)
        if (currentX != tileCountX - 1 && currentY != tileCountY - 1 && currentY != 0)
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

    public override SpecialMove getSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> movelist, ref List<Vector2Int> availableMoves)
    {
        int direction = (team == 0) ? 1 : -1;

        if ((team== 0 && currentY == 6) || (team==1 && currentY == 1))
        {
            return SpecialMove.Promotion;
        }

        //En Passant
        if (movelist.Count> 0)
        {
            Vector2Int[] lastMove = movelist[movelist.Count - 1];
            
            //[1] is the tile where the piece ended up on its last move. So we check
            //that tile on the board[,] and whether the last move was made by a pawn.
            if (board[lastMove[1].x, lastMove[1].y].type == ChessPieceType.Pawn)
            {
                //If the last move was 2 tiles with a pawn
                if (Mathf.Abs(lastMove[0].y - lastMove[1].y)  == 2)
                {
                    if (board[lastMove[1].x, lastMove[1].y].team != team)
                    {
                        //If the last move from a pawn landed next to my pawn
                        if(lastMove[1].y == currentY)
                        {
                            if (lastMove[1].x == currentX - 1)
                            {
                                availableMoves.Add(new Vector2Int(currentX - 1, currentY + direction));
                                return SpecialMove.EnPassant;
                            }
                            else if(lastMove[1].x == currentX + 1)
                            {
                                availableMoves.Add(new Vector2Int(currentX + 1, currentY + direction));
                                return SpecialMove.EnPassant;
                            }
                        }
                    }
                }
            }
        }
        return SpecialMove.None;
    }
}
