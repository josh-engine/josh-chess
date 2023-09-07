﻿using System;
using System.Numerics;
using System.Collections.Generic;
using ChessChallenge.API;
using static System.Formats.Asn1.AsnWriter;

public class MyBot : IChessBot
{

    // Variable initialization
    int nodes = 0; // #DEBUG
    int qnodes = 0; // #DEBUG
    public PieceType[] pieceTypes = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen };
    public int[] pieceValues = { 100, 290, 310, 515, 1100 };
    public Move currentBestMove;
    public int currentBestEvaluation;

    // Transposition table
    private const sbyte EXACT = 0, LOWERBOUND = -1, UPPERBOUND = 1, INVALID = -2;
    public struct Transposition
    {
        public Transposition(ulong zhash, int eval, byte d, Move m)
        {
            zobristHash = zhash;
            evaluation = eval;
            depth = d;
            flag = INVALID;
            move = m;
        }
        public Move move;
        public ulong zobristHash = 0;
        public int evaluation = 0;
        public byte depth = 0;
        public sbyte flag = INVALID;
    }

    static ulong Transposition_Mask = 0x7FFFFF;

    private Transposition[] TranspositionTable = new Transposition[Transposition_Mask + 1];

    // Determine which side is winning a given position
    int boardEval(Board board, int depth)
    {
        if (board.IsInCheckmate())
        {
            return (-9999995 + depth);
        }
        if (board.IsDraw())
        {
            return 0;
        }

        int eval = 0;

        for (int i = 0; i < 5; i++)
        {
            eval += BitOperations.PopCount(board.GetPieceBitboard(pieceTypes[i], board.IsWhiteToMove)) * pieceValues[i] - BitOperations.PopCount(board.GetPieceBitboard(pieceTypes[i], !board.IsWhiteToMove)) * pieceValues[i];
        }

        eval += board.GetLegalMoves().Length;

        if (board.IsInCheck()) eval -= 10;

        return eval;
    }

    // Search the move tree for the optimal path
    int alphaBeta(Board board, int alpha, int beta, int ply, int depth)
    {
        // End of game scenarios
        if (board.IsInCheckmate()) return -9999995 + depth;
        if (board.IsDraw()) return 0;

        // Transposition table
        ref Transposition transposition = ref TranspositionTable[board.ZobristKey & 0x7FFFFF];
        if (transposition.zobristHash == board.ZobristKey && transposition.depth >= ply-depth && transposition.flag != 0)
        {
            // In between bounds (a > score > b)
            if (transposition.flag == EXACT) return transposition.evaluation;
            // Lower bound but still better than current beta
            if (transposition.flag == LOWERBOUND && transposition.evaluation >= beta) return transposition.evaluation;
            // Upper bound but not above current alpha
            if (transposition.flag == UPPERBOUND && transposition.evaluation <= alpha) return transposition.evaluation;
        }

        // Variable initialization
        bool DepthIsZero = depth == 0;
        int startingAlpha = alpha;
        int score = 0;

        // Do a quiescence check if we are at a leaf node
        if (depth == ply) return quiesce(board, alpha, beta, depth, ply);

        // Loop through each move to determine its quality
        foreach (Move move in board.GetLegalMoves())
        {
            score = 0;
            board.MakeMove(move);
            nodes++; // #DEBUG
            score = -alphaBeta(board, -beta, -alpha, ply, depth + 1);
            board.UndoMove(move);
            if (score >= beta)
            {
                return score;
            }
            if (score > alpha)
            {
                alpha = score;
                if (DepthIsZero)
                {
                    currentBestMove = move;
                    currentBestEvaluation = score;
                    //Console.WriteLine("New best move!"); // #DEBUG
                }
            }
        }

        transposition.evaluation = alpha;
        transposition.zobristHash = board.ZobristKey;
        if (alpha < startingAlpha)
            transposition.flag = UPPERBOUND;
        else if (alpha >= beta)
            transposition.flag = LOWERBOUND;
        else transposition.flag = EXACT;
        transposition.depth = (byte)(ply-depth);

        return score;

    }

    // Deal with tactically complex positions
    int quiesce(Board board, int alpha, int beta, int depth, int extensionsLeft)
    {
        if (board.IsInCheckmate()) return -9999995 + depth;
        if (board.IsDraw()) return 0;
        int stand_pat = boardEval(board, depth);
        int score = -9999999;
        if (stand_pat > beta)
            return beta;
        if (alpha < stand_pat)
            alpha = stand_pat;

        foreach (Move qmove in board.GetLegalMoves(true))
        {
            if (qmove.IsCapture || qmove.IsPromotion)
            {
                board.MakeMove(qmove);
                score = actualQcheck(board, alpha, beta, ++depth, extensionsLeft);
                board.UndoMove(qmove);
            }
            else
            {
                board.MakeMove(qmove);
                if (board.IsInCheck() && extensionsLeft > 0) score = actualQcheck(board, alpha, beta, ++depth, extensionsLeft - 1);
                board.UndoMove(qmove);
            }
            //if (qmove.IsCapture || qmove.IsPromotion || board.IsInCheck())
            //{
            //    nodes++; // #DEBUG
            //    qnodes++; // #DEBUG
            //    score = -quiesce(board, -beta, -alpha, depth + 1, extensionsLeft );


                if (score >= beta)
                {
            //        board.UndoMove(qmove);
                    return beta;
                }
                if (score > alpha)
                    alpha = score;
            //}
            //board.UndoMove(qmove);
        }
        return alpha;
    }

    // for weird reasons this is a different function (trust me on this one)
    public int actualQcheck(Board board, int alpha, int beta, int depth, int extensionsLeft)
    {
        nodes++; // #DEBUG
        qnodes++; // #DEBUG
        int score = -quiesce(board, -beta, -alpha, depth, extensionsLeft);
        if (score > beta) return beta;
        if (score > alpha) alpha = score;
        return alpha;
    }

    // Returns the move decided to be the best (this is the function that will be called by the rest of the program)
    public Move Think(Board board, Timer timer)
    {
        currentBestEvaluation = -9999999;
        nodes = 0; // #DEBUG
        qnodes = 0; // #DEBUG
        for (int i = 1; i < 4; i++)
        {
            Console.WriteLine("starting search at ply = " + i);
            alphaBeta(board, -9999999, 9999999, i, 0);
            //if (timer.MillisecondsElapsedThisTurn > 2500) break;
        }

        // Debug stuff to help us see how the bot is doing
        Console.WriteLine(nodes + " total nodes"); // #DEBUG
        Console.WriteLine(qnodes + " qnodes"); // #DEBUG
        Console.WriteLine("Best move is " + currentBestMove + " with an evaluation of " + currentBestEvaluation); // #DEBUG
        if (timer.MillisecondsElapsedThisTurn != 0 && nodes != 0) // #DEBUG
        {
            Console.WriteLine(nodes / timer.MillisecondsElapsedThisTurn + " knps (1000s of nodes per second)"); // #DEBUG
        }
        else
        {
            Console.WriteLine("Divide by zero error prevention, knps not displayed."); // #DEBUG
        }

        // Actually return the best move
        return currentBestMove;
    }
}