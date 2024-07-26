using System.Diagnostics;

namespace Lomont.Games.TicTacToe
{
    internal class Tests
    {

        // check layout of things correct
        public static void Testing()
        {
            /* row, col and index, hash val
             * 0,0,0,  1   0,1,1,   3   0,2,2,   9
             * 1,0,3, 27   1,1,4,  81   1,2,5, 243
             * 2,0,6,729   2,1,7,2187   2,2,8,6561
             *
             *
             * - All items row,col
             * - int[] array is called a board
             * - item with board and lots of tracking stuff is a Node
             * - move in 0-8
             * - player is 1,2
             * - cell holds 0 = empty, else 1,2
             * - scoring leaf node: 0 = draw, 1 = player 1 win, -1 = player2 win
             */

            Trace.Assert(Util.Index(0, 1) == 1);
            Trace.Assert(Util.Index(1, 2) == 5);
            Trace.Assert(Util.Index(2, 0) == 6);

            for (var row = 0; row < 2; ++row)
                for (var col = 0; col < 2; ++col)
                {
                    var index = Util.Index(row, col);
                    Trace.Assert(Util.Deindex(index) == (row, col));

                    for (var perm = 0; perm < 8; ++perm)
                    {
                        var (pr, pc) = Util.ApplyPerm(perm, row, col);
                        Trace.Assert(Util.InvertPerm(perm, pr, pc) == (row, col));
                    }

                }

            // hash, dehash
            var board = new int[9];
            Trace.Assert(Util.Hash(board) == 0);
            Trace.Assert(Same(board, Util.Dehash(0)));
            board[0] = 1;
            Trace.Assert(Util.Hash(board) == 1);
            Trace.Assert(Same(board, Util.Dehash(1)));
            board[0] = 2;
            Trace.Assert(Util.Hash(board) == 2);
            Trace.Assert(Same(board, Util.Dehash(2)));
            board[Util.Index(2, 1)] = 2;
            Trace.Assert(Util.Hash(board) == 2 + 2 * 2187);
            Trace.Assert(Same(board, Util.Dehash(2 + 2 * 2187)));

            // perm: bits 4,2,1 does transpose, then  row = 2-row, then col = 2-col
            Clear();
            board = new[]
            {
        0,0,1,
        2,2,1,
        0,1,0
    };

            var flipRow = Util.PermuteBoard(1, board);
            Trace.Assert(Same(flipRow, new[] { 0, 1, 0, 2, 2, 1, 0, 0, 1 }));
            var flipCol = Util.PermuteBoard(2, board);
            Trace.Assert(Same(flipCol, new[] { 1, 0, 0, 1, 2, 2, 0, 1, 0 }));
            // 8 distinct
            var hashes = Enumerable.Range(0, 8).Select(perm => Util.Hash(Util.PermuteBoard(perm, board))).ToList();
            Trace.Assert(hashes.Distinct().Count() == 8);

            // min hash
            var (hash, minHash, minPerm) = Util.MinHash(board);
            Trace.Assert(minHash == hashes.Min());
            var minB = Util.PermuteBoard(minPerm, board);
            Trace.Assert(minHash == Util.Hash(minB));





            Console.WriteLine("Testing succeeded");

        //    Draw1.Draw(board);
        //    Console.WriteLine();
        //    Draw1.Draw(flipRow);


            void Clear() => board = new int[9];
            bool Same(int[] a, int[] b) => a.Select((v, i) => v - b[i]).All(v => v == 0);
            ;
        }
    }
}
