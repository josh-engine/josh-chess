using System;
using System.Collections.Generic;
using ChessChallenge.API;

namespace Chess_Challenge.src.MyBot
{
    public class Compressor
    {
        public PieceType[] pieceTypes = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen };
        public static int[] pieceValues = { 100, 290, 310, 515, 1100 };

        static int[] pawns =
        {
            0,  0,  0,  0,  0,  0,  0,  0,
            50, 50, 50, 50, 50, 50, 50, 50,
            10, 10, 20, 30, 30, 20, 10, 10,
             5,  5, 10, 25, 25, 10,  5,  5,
             0,  0,  0, 20, 20,  0,  0,  0,
             5, -5,-10,  0,  0,-10, -5,  5,
             5, 10, 10,-20,-20, 10, 10,  5,
             0,  0,  0,  0,  0,  0,  0,  0
        };

        static int[] knights =
        {
            -50,-40,-30,-30,-30,-30,-40,-50,
            -40,-20,  0,  0,  0,  0,-20,-40,
            -30,  0, 10, 15, 15, 10,  0,-30,
            -30,  5, 15, 20, 20, 15,  5,-30,
            -30,  0, 15, 20, 20, 15,  0,-30,
            -30,  5, 10, 15, 15, 10,  5,-30,
            -40,-20,  0,  5,  5,  0,-20,-40,
            -50,-40,-30,-30,-30,-30,-40,-50
        };

        static int[] bishops =
        {
            -20,-10,-10,-10,-10,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5, 10, 10,  5,  0,-10,
            -10,  5,  5, 10, 10,  5,  5,-10,
            -10,  0, 10, 10, 10, 10,  0,-10,
            -10, 10, 10, 10, 10, 10, 10,-10,
            -10,  5,  0,  0,  0,  0,  5,-10,
            -20,-10,-10,-10,-10,-10,-10,-20
        };

        static int[] rooks =
        {
              0,  0,  0,  0,  0,  0,  0,  0,
              5, 10, 10, 10, 10, 10, 10,  5,
             -5,  0,  0,  0,  0,  0,  0, -5,
             -5,  0,  0,  0,  0,  0,  0, -5,
             -5,  0,  0,  0,  0,  0,  0, -5,
             -5,  0,  0,  0,  0,  0,  0, -5,
             -5,  0,  0,  0,  0,  0,  0, -5,
              0,  0,  0,  5,  5,  0,  0,  0
        };

        static int[] queen =
        {
            -20,-10,-10, -5, -5,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5,  5,  5,  5,  0,-10,
             -5,  0,  5,  5,  5,  5,  0, -5,
              0,  0,  5,  5,  5,  5,  0, -5,
            -10,  5,  5,  5,  5,  5,  0,-10,
            -10,  0,  5,  0,  0,  0,  0,-10,
            -20,-10,-10, -5, -5,-10,-10,-20
        };

        public static long shortsCombine4(int x, int y, int z, int w)
        {

            var _x = (long)x;
            var _y = (long)y;
            var _z = (long)z;
            var _w = (long)w;

            return (_x << 48) | (_y << 32) | (_z << 16) | _w;
        }

        public static long[] generatePieceArrays()
        {
            List<long> compressed = new List<long>();
            int[][] tables = { pawns, knights, bishops, rooks, queen };
            List<short> shortStaging = new List<short>();

            int i1 = 0;
            foreach (int[] table in tables)
            {
                int i2 = 0;
                foreach (int value in table)
                {
                    tables[i1][i2] += pieceValues[i1];
                    i2++;
                }
                i1++;
            }

            int type = 0;
            short i = 1;
            foreach (int[] table in tables)
            {
                int last = int.MaxValue;
                foreach (int value in table)
                {
                    //Prepare values
                    if (value == last)
                    {
                        //increment if same value
                        i++;
                    }
                    else //could be doing something better, however i will only run this code once so idrc
                    {
                        //cast to short
                        if (last != int.MaxValue)
                        {
                            short a = (short)last;
                            shortStaging.Add(a);
                            shortStaging.Add(i);
                            i = 1;
                        }
                        last = value;
                    }

                    //Compression time
                    if (shortStaging.Count == 4)
                    {
                        compressed.Add(shortsCombine4(shortStaging[0], shortStaging[1], shortStaging[2], shortStaging[3]));
                        shortStaging = new List<short>();
                    }
                }
                type++;
            }
            if (shortStaging.Count > 1)
            {
                Console.WriteLine("istg if this doesnt work out perfectly ima do absolutely nothing and fix it");
            }
            return compressed.ToArray();
        }
    }
}
