/*using System;
using ChessChallenge.API;
using System.Collections.Generic;
using System.Numerics;

namespace Chess_Challenge
{
	public class CursedBot
	{
        // Variable initialization
        int nodes = 0; // #DEBUG
        int qnodes = 0; // #DEBUG
        PieceType[] pieceTypes = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen };
        int[] pieceValues = { 100, 290, 310, 515, 1100 };
        Move currentBestMove;
        int currentBestEvaluation;
        int maxLeafDepth;
        List<MoveEval> moveRankings = new();

        struct MoveEval
        {
            public MoveEval(Move move_, int eval_)
            {
                move = move_;
                eval = eval_;
            }
            public Move move;
            public int eval;
        }

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
            List<MoveEval> lMoveRankings = new();
            List<MoveEval> moveEvals = new();
            moveEvals.EnsureCapacity(128);
            moveEvals.Add(new MoveEval(Move.NullMove, -9999999));
            lMoveRankings.EnsureCapacity(128);
            lMoveRankings.Add(new MoveEval(Move.NullMove, -9999999));
            // End of game scenarios
            if (board.IsInCheckmate()) return -9999995 + depth;
            if (board.IsDraw()) return 0;

            // Transposition table
            ref Transposition transposition = ref TranspositionTable[board.ZobristKey & 0x7FFFFF];
            if (transposition.zobristHash == board.ZobristKey && transposition.depth >= ply - depth && transposition.flag != INVALID)
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
            if (depth == ply) return quiesce(board, alpha, beta, depth, 2);

            // Loop through each move to determine its quality
            /*foreach (Move move in board.GetLegalMoves())
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
            }*/
            /*int legalMovesTested = 0;
            foreach (Move move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                score = boardEval(board, depth);
                board.UndoMove(move);
                legalMovesTested++;
                for (int i = 0; i < legalMovesTested; i++)
                {
                    if (score > lMoveRankings[i].eval)
                    {
                        lMoveRankings.Insert(i, new MoveEval(move, score));
                        break;
                    }
                }
            }
            score = -9999999;
            int legalMovesRanked = 0;
            for (int i = 0; i < legalMovesTested; i++)
            {
                board.MakeMove(lMoveRankings[i].move);
                nodes++; // #DEBUG
                int moveScore = -alphaBeta(board, -beta, -alpha, ply, depth + 1);
                score = Math.Max(score, moveScore);
                board.UndoMove(lMoveRankings[i].move);
                legalMovesRanked++;
                for (int j = 0; j < legalMovesRanked; j++)
                {
                    if (moveScore > moveEvals[j].eval)
                    {
                        moveEvals.Insert(j, new MoveEval(lMoveRankings[j].move, moveScore));
                        break;
                    }
                }
            }
            if (DepthIsZero)
            {
                for (int i = 0; i < board.GetLegalMoves().Length; i++)
                {
                    if (moveEvals[i].eval == score)
                    {
                        Console.WriteLine(i + " number in list with actual best move");
                        currentBestMove = moveEvals[i].move;
                        break;
                    }
                }
            }

            transposition.evaluation = score;
            transposition.zobristHash = board.ZobristKey;
            if (score < startingAlpha)
                transposition.flag = UPPERBOUND;
            else if (score >= beta)
                transposition.flag = LOWERBOUND;
            else transposition.flag = EXACT;
            transposition.depth = (byte)(ply - depth);

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

            foreach (Move qmove in board.GetLegalMoves())
            {
                board.MakeMove(qmove);
                if (qmove.IsCapture || qmove.IsPromotion)
                {
                    score = actualQcheck(board, alpha, beta, ++depth, extensionsLeft);
                }
                else
                {
                    if (board.IsInCheck() && extensionsLeft > 0) score = actualQcheck(board, alpha, beta, ++depth, extensionsLeft - 1);
                }
                board.UndoMove(qmove);
                //if (qmove.IsCapture || qmove.IsPromotion || board.IsInCheck())
                //{
                //    nodes++; // #DEBUG
                //    qnodes++; // #DEBUG
                //    score = -quiesce(board, -beta, -alpha, depth + 1, extensionsLeft );

                maxLeafDepth = Math.Max(depth, maxLeafDepth);

                if (score >= beta)
                {
                    //        board.UndoMove(qmove);
                    return beta;
                }
                alpha = Math.Max(alpha, score);
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
            alpha = Math.Max(alpha, score);
            return score;
        }

        // Returns the move decided to be the best (this is the function that will be called by the rest of the program)
        public Move Think(Board board, Timer timer)
        {
            moveRankings.EnsureCapacity(128);
            maxLeafDepth = 0;
            moveRankings.Clear();
            currentBestEvaluation = -9999999;
            nodes = 0; // #DEBUG
            qnodes = 0; // #DEBUG
            int MoveScore;
            bool notAdded;
            /*foreach (Move move in board.GetLegalMoves())
            {
                moveRankings.Add(new MoveEval(move, -9999699));
            }
            for (int iteration = 3; iteration < 5; iteration++)
            {
                List<MoveEval> currentMoveRankings = new List<MoveEval>();
                currentMoveRankings.EnsureCapacity(board.GetLegalMoves().Length);
                int movesAddedToMoveRankings = 0;
                foreach (MoveEval moveEval in moveRankings)
                {
                    notAdded = true;
                    board.MakeMove(moveEval.move);
                    MoveScore = -alphaBeta(board, -9999999, -currentBestEvaluation, movesAddedToMoveRankings < 4 ? iteration:Math.Max(3, 2*iteration/3), 1);
                    board.UndoMove(moveEval.move);
                    //if (currentMoveRankings.Count == 0)
                    for (int i = 0; i < movesAddedToMoveRankings; i++)
                    {
                        //if (moveRankings[i].move == moveEval.move) moveRankings.RemoveAt(i);
                        if (currentMoveRankings[i].eval < MoveScore && notAdded)
                        {
                            notAdded = false;
                            currentMoveRankings.Insert(i, new MoveEval(moveEval.move, MoveScore));
                            movesAddedToMoveRankings++;
                            if (i == 0)
                            {
                                Console.WriteLine("New best move: " + moveEval.move); // #DEBUG
                                currentBestMove = moveEval.move;
                                currentBestEvaluation = MoveScore;
                            }
                            break;
                        }
                    }
                    if (notAdded == true)
                    {
                        currentMoveRankings.Add(new MoveEval(moveEval.move, MoveScore));
                        if (movesAddedToMoveRankings == 0)
                        {
                            currentBestMove = moveEval.move;
                            currentBestEvaluation = MoveScore;
                        }
                        movesAddedToMoveRankings++;

                    }
                }
                moveRankings = currentMoveRankings;
            }*/
            /*for (int i = 1; i < 4; i++)
            {
                //Console.WriteLine("starting search at ply = " + i);
                currentBestEvaluation = alphaBeta(board, -9999999, 9999999, i, 0);
                //if (timer.MillisecondsElapsedThisTurn > 2500) break;
            }

            // Debug stuff to help us see how the bot is doing
            //Console.WriteLine(nodes + " total nodes"); // #DEBUG
            //Console.WriteLine(qnodes + " qnodes"); // #DEBUG
            //Console.WriteLine("Best move is " + currentBestMove + " with an evaluation of " + currentBestEvaluation); // #DEBUG
            /*if (timer.MillisecondsElapsedThisTurn != 0 && nodes != 0) // #DEBUG
            {
                Console.WriteLine(nodes / timer.MillisecondsElapsedThisTurn + " knps (1000s of nodes per second)"); // #DEBUG
            }
            else
            {
                Console.WriteLine("Divide by zero error prevention, knps not displayed."); // #DEBUG
            }*/
            /*int knps = nodes / (timer.MillisecondsElapsedThisTurn == 0 ? 1 : timer.MillisecondsElapsedThisTurn); // #DEBUG
            Console.WriteLine(nodes + " nodes");
            Console.WriteLine(knps + " knps"); // #DEBUG
            Console.WriteLine(qnodes + " qnodes");
            Console.WriteLine((float)qnodes / (float)nodes + " proportion of qnodes"); // #DEBUG
            Console.WriteLine(maxLeafDepth + " max leaf depth (why is this so high???)");
            Console.WriteLine((float)currentBestEvaluation / 100 + " current eval");
            // Actually return the best move
            return currentBestMove;
        }
        public CursedBot()
		{
		}
	}
}

*/