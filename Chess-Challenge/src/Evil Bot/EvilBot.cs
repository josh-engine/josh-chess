using ChessChallenge.API;
using System;
using System.Numerics;

namespace ChessChallenge.Example
{
    // Old version of the bot (throws stack overflow errors for some reason?!?!?)
    public class EvilBot : IChessBot
    {

        int nodes = 0; // #DEBUG
        int qnodes = 0; // #DEBUG
        public PieceType[] pieceTypes = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen };
        public int[] pieceValues = { 100, 290, 310, 515, 1100 };
        public Move currentBestMove;
        public int currentBestEvaluation;

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

        int alphaBeta(Board board, int alpha, int beta, int ply, int depth)
        {
            if (board.IsInCheckmate()) return -9999995 + depth;
            if (board.IsDraw()) return 0;
            bool DepthIsZero = depth == 0;
            ref Transposition transposition = ref TranspositionTable[board.ZobristKey & 0x7FFFFF];
            if (transposition.zobristHash == board.ZobristKey && transposition.depth >= ply - depth && transposition.flag != 0)
            {
                // In between bounds (a > score > b)
                if (transposition.flag == EXACT) return transposition.evaluation;
                // Lower bound but still better than current beta
                if (transposition.flag == LOWERBOUND && transposition.evaluation >= beta) return transposition.evaluation;
                // Upper bound but not above current alpha
                if (transposition.flag == UPPERBOUND && transposition.evaluation <= alpha) return transposition.evaluation;
            }
            int startingAlpha = alpha;
            int score = 0;
            if (depth == ply) return quiesce(board, alpha, beta, depth);
            foreach (Move move in board.GetLegalMoves())
            {
                score = 0;
                board.MakeMove(move);
                nodes++; // #DEBUG
                score = -alphaBeta(board, -beta, -alpha, ply, depth + 1);
                board.UndoMove(move);
                //if (DepthIsZero) Console.WriteLine("Score for " + move + " is " + score); // #DEBUG
                if (score > beta)
                {
                    //if (DepthIsZero) Console.WriteLine("this move is too good"); // #DEBUG
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
            transposition.depth = (byte)(ply - depth);

            return score;

        }

        int quiesce(Board board, int alpha, int beta, int depth)
        {
            int stand_pat = boardEval(board, depth);
            int score;
            if (stand_pat > beta)
                return beta;
            if (alpha < stand_pat)
                alpha = stand_pat;

            foreach (Move move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                nodes++; // #DEBUG
                qnodes++; // #DEBUG
                score = -quiesce(board, -beta, -alpha, depth + 1);
                board.UndoMove(move);

                if (score >= beta)
                    return beta;
                if (score > alpha)
                    alpha = score;
            }
            return alpha;
        }

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
            Console.WriteLine(nodes + " total nodes"); // #DEBUG
            Console.WriteLine(qnodes + " qnodes"); // #DEBUG
            Console.WriteLine("Best move is " + currentBestMove + " with an evaluation of " + currentBestEvaluation);
            if (timer.MillisecondsElapsedThisTurn != 0 && nodes != 0) // #DEBUG
            {
                Console.WriteLine(nodes / timer.MillisecondsElapsedThisTurn + " knps (1000s of nodes per second)"); // #DEBUG
            }
            else
            {
                Console.WriteLine("Divide by zero error prevention, knps not displayed."); // #DEBUG
            }
            return currentBestMove;
        }
    }
}