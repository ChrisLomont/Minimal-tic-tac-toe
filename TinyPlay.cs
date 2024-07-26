// Code to make Tic Tac Toe for Tiny device 
/* Design:
Device has limitations on numbers, no string, etc., so design to fit well
- to make perfect play, get a game tree
- to get smaller still, only keep nodes that occur in best play
- to hash board, store as integer less than in 3^9=19683
- to make small, remove symmetries in game tree (use min hashed version per board)
- total reachable boards = 5478 via walking tree
- symmetries lowers to 750ish
- min needed ~ 256
- got down to < 100
*/

using System.Diagnostics;

namespace Lomont.Games.TicTacToe
{
    // simulate playing on tiny device. abstract code for that
    // reduce locals to globals for the most part
    // types are integer or float
    internal class TinyPlay : IPlayer
    {

        #region globals



        // globals
        // positions top row, then next row, etc..
        readonly int[] board = new int[9];

        // used for various things
        int col, row;
        int minHash, minPerm, index, result;


        int depth = 0; // move depth

        // flags, bit pack?
        int computerFirst = 0;
        int exit = 0;
        int gameOver = 0;
        int error = 0; // something failed, ends program

        // items filled by ComputeResult
        // move = -1 when game over
        //int win1, win2, draw, 
            int move;

        // stats - delete?
        int wins1 = 0, wins2 = 0, draws = 0;

        // todo - put checksum over data, reuse if agrees on next run, values saved in hw

        #endregion

        // todo  - notes better
        // - computer first, person first tracked
        // - computer always X, person O
        // - only computer can win, can use that to cimplify code
        // - in debug, crash is person wins for now
        // - track move depth
        // - output moves, result at bottom (365721 Computer wins, stats)
        // - first move, space skips to computer first


        public void Play()
        {
            error = 0;
            exit = 0;

            Console.Clear();

            while (exit == 0)
            {
                DrawInitialBoard();

                gameOver = 0;
                for (col = 0; col < 9; col++)
                    board[col] = 0;
                depth = 0;
                computerFirst = 0;

                while (gameOver == 0 && exit == 0)
                {
                    GetMove();
                    DoMove();
                    if (result != 0 || depth == 9)
                        gameOver = 1;
                    //ComputeResult();
                }

                DrawResult();
                Console.WriteLine($"Tally : 1:{wins1} 2:{wins2} d:{draws}");
                //Thread.Sleep(200);

                if (error != 0)
                    exit = 1;
            }
        }

        void DrawResult()
        {
            Console.SetCursorPosition(0,12);
            Console.Write("Result: ");
            if (result == 9 || depth == 9)
            {
                Console.Write("Draw!          ");
                draws++;
            }
            else if (result > 0)
            {
                Console.Write("Player 1 wins! ");
                wins1++;
                DrawLine();
            }
            else if (result < 0)
            {
                Console.Write("Player 2 wins! ");
                wins2++;
                DrawLine();
            }
            else
                Console.Write("Undecided!     ");
            Console.WriteLine();
        }

        void DrawLine()
        {
            // todo - draw winning line
        }
        
        // do a move if legal, else nothing
        void DoMove()
        {
            if (move == -1) 
               return;
            
            // get row,col for draw
            index = move;
            Deindex();

            // depth into toMove, stored in index
            ToMove();

            if (index == 1)
            {
                Console.SetCursorPosition(0, 10);
                Console.WriteLine($"Player move {move+1}");
            }
            else
            {
                Console.SetCursorPosition(0, 11);
                Console.WriteLine($"Bot move move {move+1}");
            }

            Trace.Assert(board[move] == 0);
            board[move] = index;
            DrawMove();
            depth++;
        }

        void DrawMove()
        {
            col = 2 * col + 2;
            row = 2 * row + 1;
            Console.SetCursorPosition(row,col);
            if (index == 1)
                Console.Write('X');
            else
                Console.Write('O');
            Console.SetCursorPosition(0,15);
        }


        // depth into index as toMove 1 or 2
        void ToMove()
        {
            index = depth & 1;
            index++;
        }

        // index into row,col
        void Deindex()
        {
            col = index / 3;
            row = index % 3;
        }

        // row,col into index
        void Index()
        {
            index = col * 3 + row;
        }
        // apply minPerm to row,col
        void ApplyPerm()//int perm, int row, int col)
        {
            if ((minPerm & 4) != 0)
                (col, row) = (row, col);
            if ((minPerm & 2) != 0)
                col = 2 - col;
            if ((minPerm & 1) != 0)
                row = 2 - row;
        }

        // invert minPerm on row,col
        void InvertPerm()
        {
            if ((minPerm & 1) != 0)
                row = 2 - row;
            if ((minPerm & 2) != 0)
                col = 2 - col;
            if ((minPerm & 4) != 0)
                (col, row) = (row, col);
        }

        // get minHash, minPerm
        void MinHash()
        {
            minHash = 50000; // larger than any possible hash
            move = 0; // used to store minPerm till done
            // represent
            for (minPerm = 0; minPerm < 8; ++minPerm) // perm
            {
                result = 0; // current board hash
                for (col = 2; col >= 0; --col)
                for (row = 2; row >= 0; --row)
                {
                    ApplyPerm(); // row,col to perm row,col
                    Index(); // row,col to index
                    result = result * 3 + board[index];
                    InvertPerm(); // restore row,col
                }

                if (result < minHash)
                {
                    minHash = result;
                    move = minPerm;
                }
            }

            minPerm = move;
        }

        readonly Random rand = new Random(1234);

        void RandomMove()
        {
            move = -1;
            var pass = 0;
            //Thread.Sleep(50);
            while (true)
            {
                var m = rand.Next(9);
                if (board[m] == 0)
                {
                    move = m;
                    return;
                }

                if (++pass > 20)
                {
                    gameOver = 1;
                    return;
                }
            }

        }


        // get move, do not return till found
        void GetMove()
        {
            result = 0;
            if (depth == 0)
            {
                computerFirst = 0;
                GetHumanMove();
                if (computerFirst == 0)
                    return;
            }

            // decide:
            var odd = (depth & 1) == 1;
            if ((computerFirst!=0) ^ odd)
                GetComputerMove();
            else
                GetHumanMove();
        }

        void GetComputerMove()
        {
            MinHash();
            Lookup(minHash);

            // move is relative to minPerm, must undo it
            index = move;
            Deindex();
            ApplyPerm();
            Index();
            Debug.WriteLine($"computer move {move} with perm {minPerm} becomes {index}");
            move = index;
        }

        void GetHumanMove()
        {
//#if false
            if (depth==0 && rand.Next(100) < 50)
            {
                computerFirst = 1;
                return;
            }
            RandomMove();
            return;
//#endif
            

            move = -1; // assume no move
            while (true)
            {
                var ch = Console.ReadKey(true).KeyChar;
                if ('1' <= ch && ch <= '9')
                {
                    var m = ch - '1';
                    if (board[m] == 0)
                    {
                        move = m;
                        return;
                    }
                }

                if (ch == ' ' && depth == 0)
                { // let computer go first
                    computerFirst = 1;
                    return;
                }

                if (ch == 'q')
                {
                    exit = 1;
                    return;
                }

                if (ch == 'n')
                {
                    gameOver = 1;
                    return;
                }
            }
        }

        void DrawInitialBoard()
        {
            Console.SetCursorPosition(0,0);
            var h = "+-+-+-+";
            Console.WriteLine("1-9 move, q = quit, n = new game");
            Console.WriteLine(h);

            for (row = 0; row < 3; ++row)
            {
                for (col = 0; col < 3; ++col)
                {
                    Console.Write($"|{col+row*3+1}");
                }

                Console.WriteLine("|");
                Console.WriteLine(h);
            }

            Console.WriteLine();
        }

      

        #region Table

// minHash, bestMove, result 0 (continue), 1-8 win, 9 draw
static readonly int[] tbl = new[]{
0, 4, 0 , 
1, 4, 0 , 
3, 4, 0 , 
81, 0, 0 , 
83, 8, 0 , 
86, 7, 0 , 
87, 6, 0 , 
92, 6, 0 , 
126, 5, 0 , 
146, 6, 8 , 
150, 6, 8 , 
166, 2, 0 , 
172, 1, 0 , 
192, 0, 0 , 
198, 0, 0 , 
203, 8, -7 , 
205, 7, -5 , 
211, 6, -8 , 
383, 6, -4 , 
389, 6, -4 , 
397, 8, 0 , 
399, 7, 0 , 
432, 0, 0 , 
437, 8, -7 , 
443, 8, -7 , 
622, 8, 7 , 
828, 0, 0 , 
830, 1, 0 , 
833, 7, 0 , 
834, 0, 0 , 
857, 1, -1 , 
882, 8, 0 , 
887, 7, 5 , 
889, 8, 7 , 
900, 1, 0 , 
905, 8, -7 , 
907, 7, -5 , 
913, 3, 0 , 
933, 7, -5 , 
1073, 1, -1 , 
1109, 2, 0 , 
1125, 0, 0 , 
1130, 7, 0 , 
1139, 8, -7 , 
1149, 7, -5 , 
1153, 3, 0 , 
1155, 3, 0 , 
1163, 2, 0 , 
1179, 0, 0 , 
1184, 8, -7 , 
1189, 8, 0 , 
1197, 8, 0 , 
1210, 8, 0 , 
1298, 8, 0 , 
1302, 2, 8 , 
1319, 7, 5 , 
1321, 3, 4 , 
1325, 2, 0 , 
1341, 0, 0 , 
1346, 8, -6 , 
1387, 3, -2 , 
1418, 8, -7 , 
1793, 3, -4 , 
1839, 0, -4 , 
1893, 2, -8 , 
1906, 7, -5 , 
2041, 8, 7 , 
2055, 7, 0 , 
2063, 8, 0 , 
2066, 7, 0 , 
2089, 8, 7 , 
2590, 8, 0 , 
3314, 1, -1 , 
3318, 0, -1 , 
3368, 1, -1 , 
3394, 8, 0 , 
3396, 8, 0 , 
3518, 2, -1 , 
3530, 1, -1 , 
3602, 8, -7 , 
4246, 8, 0 , 
4250, 1, 0 , 
4254, 0, 0 , 
4330, 1, 0 , 
5689, 3, 4 , 
7391, 1, -1 , 
7445, 7, 3 , 
7528, 5, -2 , 
7688, 1, -1 , 
7742, 1, -1 , 
7768, 7, 0 , 
8123, 7, 5 , 
8624, 1, 0 , 
8630, 7, 9 };

        // is win, draw? get move, else move = -1
        public delegate (int win1, int win2, int draw, int move) LookupFunc(int position);

        LookupFunc? func = null;
        public void SetLookupFunc(LookupFunc func)
        {
            this.func = func;
        }
        void Lookup(int position)
        {
            //if (func != null)
            //{
            //    (win1,win2,draw,move) = func(position);
            //    return;
            //}

            //win1 = win2 = draw = 0;
            move = 0;

            if (minHash == 0) { move = 4; result = 0; return; }
            if (minHash == 1) { move = 4; result = 0; return; }
            if (minHash == 3) { move = 4; result = 0; return; }
            if (minHash == 81) { move = 0; result = 0; return; }
            if (minHash == 83) { move = 3; result = 0; return; }
            if (minHash == 86) { move = 7; result = 0; return; }
            if (minHash == 87) { move = 5; result = 0; return; }
            if (minHash == 92) { move = 6; result = 0; return; }
            if (minHash == 104) { move = 7; result = 5; return; }
            if (minHash == 116) { move = 5; result = 2; return; }
            if (minHash == 126) { move = 5; result = 0; return; }
            if (minHash == 128) { move = 5; result = 2; return; }
            if (minHash == 132) { move = 5; result = 2; return; }
            if (minHash == 156) { move = 7; result = 5; return; }
            if (minHash == 166) { move = 2; result = 0; return; }
            if (minHash == 172) { move = 1; result = 0; return; }
            if (minHash == 192) { move = 0; result = 0; return; }
            if (minHash == 198) { move = 0; result = 0; return; }
            if (minHash == 203) { move = 8; result = -7; return; }
            if (minHash == 205) { move = 7; result = -5; return; }
            if (minHash == 211) { move = 6; result = -8; return; }
            if (minHash == 383) { move = 6; result = -4; return; }
            if (minHash == 384) { move = 8; result = 0; return; }
            if (minHash == 389) { move = 6; result = -4; return; }
            if (minHash == 396) { move = 0; result = 0; return; }
            if (minHash == 397) { move = 8; result = 0; return; }
            if (minHash == 399) { move = 7; result = 0; return; }
            if (minHash == 403) { move = 8; result = 7; return; }
            if (minHash == 432) { move = 8; result = 0; return; }
            if (minHash == 437) { move = 8; result = -7; return; }
            if (minHash == 443) { move = 8; result = -7; return; }
            if (minHash == 624) { move = 7; result = 5; return; }
            if (minHash == 828) { move = 8; result = 0; return; }
            if (minHash == 833) { move = 7; result = 0; return; }
            if (minHash == 857) { move = 5; result = 0; return; }
            if (minHash == 900) { move = 5; result = 0; return; }
            if (minHash == 905) { move = 8; result = -7; return; }
            if (minHash == 907) { move = 7; result = -5; return; }
            if (minHash == 913) { move = 3; result = 0; return; }
            if (minHash == 933) { move = 7; result = -5; return; }
            if (minHash == 1073) { move = 1; result = -1; return; }
            if (minHash == 1109) { move = 2; result = 0; return; }
            if (minHash == 1125) { move = 0; result = 0; return; }
            if (minHash == 1130) { move = 7; result = 0; return; }
            if (minHash == 1139) { move = 8; result = -7; return; }
            if (minHash == 1149) { move = 7; result = -5; return; }
            if (minHash == 1153) { move = 3; result = 0; return; }
            if (minHash == 1155) { move = 3; result = 0; return; }
            if (minHash == 1163) { move = 2; result = 0; return; }
            if (minHash == 1179) { move = 0; result = 0; return; }
            if (minHash == 1184) { move = 8; result = -7; return; }
            if (minHash == 1189) { move = 2; result = 0; return; }
            if (minHash == 1197) { move = 8; result = 0; return; }
            if (minHash == 1210) { move = 8; result = 0; return; }
            if (minHash == 1325) { move = 2; result = 0; return; }
            if (minHash == 1331) { move = 2; result = 8; return; }
            if (minHash == 1341) { move = 0; result = 0; return; }
            if (minHash == 1346) { move = 8; result = -6; return; }
            if (minHash == 1347) { move = 0; result = 4; return; }
            if (minHash == 1387) { move = 3; result = -2; return; }
            if (minHash == 1418) { move = 8; result = -7; return; }
            if (minHash == 1560) { move = 7; result = 5; return; }
            if (minHash == 1788) { move = 3; result = 2; return; }
            if (minHash == 1793) { move = 3; result = -4; return; }
            if (minHash == 1839) { move = 0; result = -4; return; }
            if (minHash == 1855) { move = 8; result = 7; return; }
            if (minHash == 1893) { move = 2; result = -8; return; }
            if (minHash == 1906) { move = 7; result = -5; return; }
            if (minHash == 2055) { move = 7; result = 0; return; }
            if (minHash == 2063) { move = 8; result = 0; return; }
            if (minHash == 2066) { move = 7; result = 0; return; }
            if (minHash == 2590) { move = 8; result = 0; return; }
            if (minHash == 3314) { move = 1; result = -1; return; }
            if (minHash == 3318) { move = 0; result = -1; return; }
            if (minHash == 3368) { move = 1; result = -1; return; }
            if (minHash == 3394) { move = 8; result = 0; return; }
            if (minHash == 3396) { move = 8; result = 0; return; }
            if (minHash == 3491) { move = 8; result = 3; return; }
            if (minHash == 3518) { move = 2; result = -1; return; }
            if (minHash == 3530) { move = 1; result = -1; return; }
            if (minHash == 3543) { move = 2; result = 8; return; }
            if (minHash == 3602) { move = 8; result = -7; return; }
            if (minHash == 4174) { move = 8; result = 0; return; }
            if (minHash == 4219) { move = 8; result = 7; return; }
            if (minHash == 4246) { move = 8; result = 0; return; }
            if (minHash == 4250) { move = 1; result = 0; return; }
            if (minHash == 4254) { move = 0; result = 0; return; }
            if (minHash == 4330) { move = 1; result = 0; return; }
            if (minHash == 7391) { move = 1; result = -1; return; }
            if (minHash == 7528) { move = 5; result = -2; return; }
            if (minHash == 7688) { move = 1; result = -1; return; }
            if (minHash == 7742) { move = 1; result = -1; return; }
            if (minHash == 7768) { move = 7; result = 0; return; }
            if (minHash == 8546) { move = 7; result = 0; return; }
            if (minHash == 8624) { move = 1; result = 0; return; }
            if (minHash == 8630) { move = 7; result = 9; return; }
            Trace.Assert(false,$"Error position! {position}");
            error = 1; 
        }
        #endregion

        #region IPlayer interface
        public (int move, bool exit) GetMove(int[] board)
        {
            Array.Copy(board, this.board, 9);
            MinHash();
            Lookup(minHash);
            index = move;
            Deindex();
            ApplyPerm();
            Index();
            move = index;
            return (move, false);
        }

        public string Name => "Tiny Device Player";
        #endregion
    }
}

