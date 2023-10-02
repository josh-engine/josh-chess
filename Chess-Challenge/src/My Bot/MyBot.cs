using System;
using System.Numerics;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    // Variable initialization
    int nodes = 0; // #DEBUG
    int qnodes = 0; // #DEBUG
    PieceType[] pieceTypes = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen, PieceType.King };
    static int[] pieceValues = { 100, 290, 310, 515, 1100, 0 };
    Move currentBestMove;
    int currentBestEvaluation;
    int maxLeafDepth;

    ulong[] signs =
    {
        27145260584828927u,
        35777300645543680u,
        9144084140555280u,
        26922969726976u,
        72057587570209346u,
        4569840889168896u,
        4332515962709278719u,
        5046283450099691519u,
        629626815131740412u,
        278172146794238u,
        15475212744581841350u,
        17025374898617952u,
    };

    decimal[] tables =
    {
        142306632458125986651441152m,
        32323710779676160184895406080m,
        309106242897139272463151872m,
        18613835566972928272596205568m,
        58122494493188309235590503940m,
        44352119060785341075222970528m,
        30126458437578178447383003136m,
        26079821723006539014869090371m,
        17771367769247381480928542236m,
        76284883022919444171608556162m,
        29026653604276504495078244352m,
        51793276813453694285199180032m,
        74275984791651544547704247812m,
        43717437767157163102490951939m,
        68386549413127376610214805504m,
        4539278139239711486743937024m,
        19775660543103684062172481472m,
        53573427074160751723012980739m,
        47736443481928549762101419039m,
        66900472844434768844596192213m,
        53771250857462548014150385689m,
        5300167825635658316063250516m,
        75554952879985111517666477572m,
        62792942960757910956410962121m,
    };

    public static int[] decompress(decimal[] decimals)
    {

        int[] toReturn = new int[decimals.Length * 32];
        int[] current;

        for (int i = 0; i < decimals.Length; i++)
        {

            current = decompressDecimal(decimals[i]);
            for (int j = 0; j < 32; j++) { toReturn[(32 * i) + j] = current[j]; }

        }

        return toReturn;

    }

    public static int[] decompressDecimal(decimal dec)
    {

        int[] toReturn = new int[32];
        int[] deconstructed = decimal.GetBits(dec);
        uint[] Deconstructed = { (uint)deconstructed[0], (uint)deconstructed[1], (uint)deconstructed[2], (uint)deconstructed[3] };

        for (int i = 0; i < 32; i++) { toReturn[i] = pieceValues[i/64] + (int)((4 * ((Deconstructed[0] << i) >> 31)) + (2 * (Deconstructed[1] << i >> 31)) + (Deconstructed[2] << i >> 31)); }

        return toReturn;

    }

    int[,] psqts = new int[12,64];

    public MyBot()
    {
        int[] ints = decompress(tables);
        for (int i = 0; i < ints.Length; i++) {
            psqts[i/64, i%64] = pieceValues[i/128] + (((++ints[i]) << (i<256 ? 4:3)) * (int)((signs[i / 64] >> (i % 64)) % 2) > 0 ? 1 : -1);
        }
    }

    int psqtEvaluate (Board board)
    {
        int eval = 0;
        ulong pieces = board.AllPiecesBitboard;
        float gamePhase = BitOperations.PopCount(pieces) / 32;
        for (int i = 0; i<64; i++)
        {
            if ((pieces%2) != 0)
            {
                foreach (PieceType type in pieceTypes)
                {
                    if (((board.GetPieceBitboard(type, true) >> i) % 2) != 0)
                    {
                        eval += (int)(psqts[2*((int)type - 1), (63-i)] * gamePhase);
                        eval += (int)(psqts[2*((int)type - 1)+1, (63-i)] * (1f-gamePhase));
                        break;
                    }
                    if (((board.GetPieceBitboard(type, false) >> i) % 2) != 0)
                    {
                        eval -= (int)(psqts[2*((int)type - 1), (63-i) ^ 56] * gamePhase);
                        eval -= (int)(psqts[2*((int)type - 1)+1, (63-i) ^ 56] * (1f-gamePhase));
                        break;
                    }
                }
            }
            pieces >>= 1;
        }
        return board.IsWhiteToMove ? eval:-eval;
    }

    // Determine which side is winning a given position
    /*int boardEval(Board board)
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
    }*/

    int search(Board board, int alpha, int beta, int ply, int depth, int extensionsLeft, bool qsearch)
    {
        // end of game scenarios
        if (board.IsInCheckmate()) return -9999995 + depth;
        if (board.IsDraw()) return 0;

        // variables
        int score = -9999999;
        bool isCheck = board.IsInCheck();

        // go through the legal moves and sort them using MVV LVA move ordering
        Move[] legalMoves = board.GetLegalMoves();
        int[] scores = new int[legalMoves.Length];
        for (int i = 0; i < legalMoves.Length; i++) scores[i] = mvvlva(legalMoves[i]);
        Array.Sort(scores, legalMoves);
        Array.Reverse(legalMoves);
        int eval = psqtEvaluate(board);

        if (!isCheck && depth <= 6 && eval + 20 * depth >= beta) return eval;

        if (qsearch)
        {
            if (eval > beta) return beta;
            if (eval > alpha) alpha = eval;
        }

        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);
            if (!qsearch || move.IsCapture || move.IsPromotion || isCheck || (board.IsInCheck() && extensionsLeft > 0))
            {
                nodes++; // #DEBUG
                if (qsearch) qnodes++; // #DEBUG
                score = -search(board, -beta, -alpha, ply, depth + 1, extensionsLeft - (board.IsInCheck() && qsearch ? 1 : 0), depth+1 >= ply);
            }
            board.UndoMove(move);
            if (score >= beta) return beta;
            if (score > alpha)
            {
                alpha = score;
                if (depth == 0) currentBestMove = move;
            }
        }

        return score;

        // expand the search tree
        /*if (qsearch)
        {
            int eval = boardEval(board, depth);
            if (eval > beta) return beta;
            if (eval > alpha) alpha = eval;

            foreach (Move qmove in legalMoves)
            {
                board.MakeMove(qmove);
                if (qmove.IsCapture || qmove.IsPromotion || isCheck || (board.IsInCheck() && extensionsLeft > 0))
                {
                    nodes++; // #DEBUG
                    qnodes++; // #DEBUG
                    score = -search(board, -beta, -alpha, ply, depth + 1, extensionsLeft - (board.IsInCheck() ? 1 : 0), true);
                }
                board.UndoMove(qmove);
                if (score >= beta) return beta;
                if (score > alpha) alpha = score;
            }

            return alpha;
        }
        else
        {
            foreach(Move move in legalMoves)
            {
                board.MakeMove(move);
                nodes++;
                score = -search(board, -beta, -alpha, ply, depth + 1, 2, depth+1 >= ply);
                board.UndoMove(move);
                if (score >= beta) return beta;
                if (score > alpha)
                {
                    alpha = score;
                    if (depthZero)
                    {
                        currentBestMove = move;
                        currentBestEvaluation = score;
                    }
                }
            }
        }
        return score;*/
    }

    int mvvlva(Move move) { return (int)move.CapturePieceType * 100 - (int)move.MovePieceType; }

    // Returns the move decided to be the best (this is the function that will be called by the rest of the program)
    public Move Think(Board board, Timer timer)
    {
        Console.WriteLine(Convert.ToString((long)board.AllPiecesBitboard, 2));
        Console.WriteLine(psqtEvaluate(board));
        currentBestEvaluation = -9999999;
        nodes = 0; // #DEBUG
        qnodes = 0; // #DEBUG
        for (int i = 1; i < 10; i++)
        {
            currentBestEvaluation = search(board, -9999999, 9999999, i, 0, 2, false);
            Console.WriteLine("Finished search at ply = " + i + ", " + currentBestMove + ", with an eval of " + currentBestEvaluation + ", with " + nodes + " nodes searched, of which " + (100 * qnodes) / nodes + "% being qnodes,"); // #DEBUG
            //if (timer.MillisecondsElapsedThisTurn > 2500) break;
        }

        Console.WriteLine(nodes / (timer.MillisecondsElapsedThisTurn > 0 ? timer.MillisecondsElapsedThisTurn : 1) + " knps (1000s of nodes per second)");

        // Actually return the best move
        return currentBestMove;
    }
}