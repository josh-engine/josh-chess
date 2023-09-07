/*using Chess_Challenge.src.MyBot;
using ChessChallenge.API;
using System.Collections.Generic;

public class MyBot : IChessBot
{
    public PieceType[] pieceTypes = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen };
    public int[] pieceValues = { 100, 290, 310, 515, 1100 };
    public int[][] pieceTables = new int[5][];
    public Dictionary<ulong, int> transposTable = new Dictionary<ulong, int>();
    public bool selfIsWhite;

    public MyBot()
    {
        long[] ptables = Compressor.generatePieceArrays();
        int[] tablesStaging = new int[64];
        int tableIndex = 0;
        int workingOnTable = 0;
        int a = 0;
        foreach (long comp in ptables)
        {
            short[] pre = new short[4];
            pre[3] = (short)(comp & 0xFFFF);       //Second repeat amount
            pre[2] = (short)(comp >> 16 & 0XFFFF); //Second value
            pre[1] = (short)(comp >> 32 & 0XFFFF); //First repeat amount
            pre[0] = (short)(comp >> 48 & 0XFFFF); //First value
            for (int i = 0; i < pre[1]; i++)
            {
                tablesStaging[tableIndex] = pre[0];
                tableIndex++;
                if (tableIndex == 64)
                {
                    pieceTables[workingOnTable] = tablesStaging;
                    workingOnTable++;
                    tablesStaging = new int[64];
                    tableIndex = 0;
                }
            }
            for (int i = 0; i < pre[3]; i++)
            {
                tablesStaging[tableIndex] = pre[2];
                tableIndex++;
                if (tableIndex == 64)
                {
                    pieceTables[workingOnTable] = tablesStaging;
                    workingOnTable++;
                    tablesStaging = new int[64];
                    tableIndex = 0;
                }
            }
            if (tableIndex == 63)
            {
                pieceTables[workingOnTable] = tablesStaging;
                workingOnTable++;
                tablesStaging = new int[64];
                tableIndex = 0;
            }
            a++;
        }

        System.Console.WriteLine("Complete!");
    }

    int boardEval(Board board)
    {
        if (transposTable.ContainsKey(board.ZobristKey))
        {
            return transposTable.GetValueOrDefault(board.ZobristKey);
        }
        else
        {
            int eval = 0;


            for (int i = 0; i < 5; i++)
            {
                eval += BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(pieceTypes[i], board.IsWhiteToMove)) * pieceValues[i];
                eval -= BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(pieceTypes[i], !board.IsWhiteToMove)) * pieceValues[i];
            }

            eval += board.GetLegalMoves().Length;

            int flip = selfIsWhite == board.IsWhiteToMove ? -1 : 1;

            foreach (ulong ul in board.GameRepetitionHistory)
            {
                if (ul == board.ZobristKey)
                {
                    eval += 500 * flip;
                }

            }

            if (board.IsDraw()) eval += 1500 * flip;

            if (board.IsInCheckmate())
            {
                eval -= 2000;
            }
            if (board.IsInCheck()) eval -= 250;

            return eval;
        }
    }


    int quiesce(int alpha, int beta, Board board)
    {
        int stand_pat = boardEval(board);

        if (stand_pat >= beta)
            return beta;

        if (alpha < stand_pat)
            alpha = stand_pat;

        foreach (Move m in board.GetLegalMoves(true))
        {
            board.MakeMove(m);
            int score = -quiesce(-beta, -alpha, board);
            board.UndoMove(m);


            if (score >= beta)
                return beta;
            if (score > alpha)
                alpha = score;
        }
        return alpha;
    }


    public int timeoutCurrent(ChessChallenge.API.Timer timer)
    {
        int tc = timer.MillisecondsRemaining / 20;
        if (1100 < tc)
        {
            tc = 1100;
        }
        return tc;
    }

    public Move Think(Board board, Timer timer)
    {
        Move currentBestMove = new Move();
        Move finalBestMove = new Move();
        int fBMeval = -int.MaxValue;
        selfIsWhite = board.IsWhiteToMove;
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
                int score = -alphaBeta(-int.MaxValue, -alpha, i + extension, board, timer);
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

    int alphaBeta(int alpha, int beta, int depth, Board board, Timer timer)
    {
        int bestscore = -int.MaxValue;
        if (depth == 0 || board.GetLegalMoves().Length == 0) return quiesce(alpha, beta, board);
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
            int score = -alphaBeta(-beta, -alpha, extension + depth - 1, board, timer);
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
}
*/