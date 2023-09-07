/*using System;
using System.Collections.Generic;
using System.ComponentModel;
using ChessChallenge.API;
using static System.Formats.Asn1.AsnWriter;

public class MyBot : IChessBot
{
    public PieceType[] pieceTypes = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen };
    public int[] pieceValues = { 100, 290, 310, 515, 1100 };
    public Dictionary<ulong, int> transposTable = new Dictionary<ulong, int>();

    int boardEval(Board board, bool isWhite)
    {
        if (transposTable.ContainsKey(board.ZobristKey))
        {
            return transposTable.GetValueOrDefault(board.ZobristKey);
        }
        else
        {
            int eval = 0;
            int turn = isWhite == board.IsWhiteToMove ? 1 : -1;


            for (int i = 0; i < 5; i++)
            {
                eval += BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(pieceTypes[i], isWhite)) * pieceValues[i];
                eval -= BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(pieceTypes[i], !isWhite)) * pieceValues[i];
            }

            eval += turn * board.GetLegalMoves().Length;

            foreach (ulong ul in board.GameRepetitionHistory)
            {
                if (ul == board.ZobristKey)
                {
                    eval -= 5000;
                }

            }

            if (board.IsDraw()) eval -= 1000;

            if (board.IsInCheckmate()) eval += turn * 1500;

            if (board.IsInCheck()) eval += turn * 250;

            return eval;
        }
    }


    int quiesce(int alpha, int beta, Board board, bool isWhite)
    {
        int stand_pat = boardEval(board, isWhite);
        if (alpha < stand_pat)
            alpha = stand_pat;
        if (alpha >= beta)
            return beta;

        foreach (Move m in board.GetLegalMoves(true))
        {
            board.MakeMove(m);
            int score = -quiesce(-beta, -alpha, board, !isWhite);
            board.UndoMove(m);

            if (score > alpha)
                alpha = score;
            if (alpha >= beta)
                return beta;
        }
        return alpha;
    }


    public int timeoutCurrent(Timer timer)
    {
        int tc = timer.MillisecondsRemaining / 20;
        if (1350 < tc)
        {
            tc = 1350;
        }
        return tc;
    }

    public Move Think(Board board, Timer timer)
    {
        Move currentBestMove = new Move();
        Move finalBestMove = new Move();
        int fBMeval = -int.MaxValue;
        //Root NegaMax
        for (int i = 1; i <= 7; i++)
        {
            int alpha = -int.MaxValue;
            foreach (Move m in board.GetLegalMoves())
            {
                if (timer.MillisecondsElapsedThisTurn > timeoutCurrent(timer))
                {
                    break;
                }
                int extension = 0;
                board.MakeMove(m);
                if (board.IsInCheck())
                {
                    extension += 1;
                }
                int score = -alphaBeta(-int.MaxValue, -alpha, i + extension, board, timer, board.IsWhiteToMove);
                board.UndoMove(m);
                if (score > alpha || currentBestMove == new Move())
                {
                    alpha = score;
                    currentBestMove = m;
                    if (finalBestMove == new Move()) finalBestMove = currentBestMove;
                }
            }

            if (timer.MillisecondsElapsedThisTurn > timeoutCurrent(timer))
            {
                break;
            }

            if (fBMeval < alpha)
            {
                finalBestMove = currentBestMove;
                fBMeval = alpha;
            }
        }

        return finalBestMove;
    }

    /*
    int negaMax(int alpha, int beta, int depth, Board board, Timer timer, bool isWhite)
    {
        if (depth == 0 || board.GetLegalMoves().Length == 0) return boardEval(board, isWhite);
        foreach (Move m in board.GetLegalMoves())
        {
            if (timer.MillisecondsElapsedThisTurn > timeoutCurrent(timer))
            {
                break;
            }
            int extension = 0;
            board.MakeMove(m);
            if (board.IsInCheck())
            {
                extension += 1;
            }
            int score = -negaMax(-beta, -alpha, extension + depth - 1, board, timer, !isWhite);
            board.UndoMove(m);
            if (score > alpha)
                alpha = score; // alpha is max
            if (alpha >= beta)
                return beta;   // beta cutoff (cull nodes)
        }
        return alpha;
    }*/

    /*int alphaBeta(int alpha, int beta, int depth, Board board, Timer timer, bool isWhite)
    {
        int bestscore = -int.MaxValue;
        if (depth == 0) return quiesce(alpha, beta, board, isWhite);
        foreach (Move m in board.GetLegalMoves())
        {
            if (timer.MillisecondsElapsedThisTurn > timeoutCurrent(timer))
            {
                break;
            }
            int extension = 0;
            board.MakeMove(m);
            if (board.IsInCheck())
            {
                extension += 1;
            }
            int score = -alphaBeta(-beta, -alpha, depth - 1, board, timer, isWhite);
            board.UndoMove(m);
            if (score >= beta)
                return score;  // fail-soft beta-cutoff
            if (score > bestscore)
            {
                bestscore = score;
                if (score > alpha)
                    alpha = score;
            }
        }
        return bestscore;
    }
}*/