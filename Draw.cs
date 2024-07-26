namespace Lomont.Games.TicTacToe
{
    internal static  class Draw1
    {
#if false


static void WriteResult(Board br)
{
    var bestMove = br.bestMoves.FirstOrDefault();
    Console.Write($"score: {br.score} best move: {bestMove} w/w/d: {br.wins1}/{br.wins2}/{br.draws}");
}
     
 
#endif
        static readonly string cellText = " XO";

        public static void PosBrowserOld(Dictionary<int, Node> nodes)
        {
            Console.Clear();
            List<Node> pos = new();
            foreach (var k in nodes.Keys.OrderBy(k => k))
                pos.Add(nodes[k]);
            var start = 0;
            var del = 10;
            int dx = 5, dy = 6;
            while (true)
            {
                var index = 0;
                for (var j = 0; j < 40 - dy; j += dy)
                for (var i = 0; i < 110 - dx; i += dx)
                {
                    DrawA(pos[start + index], i, j);
                    ++index;
                }

                var ch = Console.ReadKey(true).KeyChar;
                if (ch == 'x') start += del;
                if (ch == 'z' && start > 0) start -= del;
            }
        }

        // feed hashsed tree, keep symmetries
        public static void PosBrowser(Dictionary<int,Node> nodes)
        {
            Console.Clear();
            Console.WriteLine("move 1-9, <space> to go back up tree, ESC to quit");
            Stack<Node> stack = new();

            var pos = nodes[0];
            var quit = false;
            while (!quit)
            {
                DrawC(stack.ToList(),pos);
                var result = Node.BoardResult(pos);
                while (true)
                {
                    var c = Console.ReadKey(true).KeyChar;
                    if ('1' <= c && c <= '9' && result == 3)
                    {
                        var m = c - '1';
                        if (pos.board[m] == 0)
                        {
                            stack.Push(pos);

                            var b = new int[9];
                            Array.Copy(pos.board, b, pos.board.Length);
                            b[m] = pos.ToMove();
                            pos = nodes[Util.Hash(b)];
                            break;
                        }
                    }

                    if (c == ' ' && stack.Count>0)
                    {
                        pos = stack.Pop();
                        break;
                    }
                    if (c == (char)27)
                    {
                        quit= true;
                        break;
                    }
                }
            }

        }

        static (ConsoleColor fore, ConsoleColor back) GetC() => (Console.ForegroundColor, Console.BackgroundColor);

        static void SetC((ConsoleColor fore, ConsoleColor back) colors) =>
            (Console.ForegroundColor, Console.BackgroundColor) = colors;

        static readonly ConsoleColor[] resultColors =
            { ConsoleColor.Yellow, ConsoleColor.Green, ConsoleColor.Red, ConsoleColor.White };

        public static void DrawC2(
            Node? board, int i1, int j1, 
            bool colorChildren = false,
            bool showBest = true
            )
        {
            var oldColors = GetC();
            // 8 wide, 8 high
            Console.SetCursorPosition(i1,j1);
            var h = "+-+-+-+";

            var edgeColor = board != null ? ConsoleColor.White : oldColors.back;
            Console.ForegroundColor = edgeColor;
            Console.Write(h);

            var resultColor = BoardColor(board);

            static ConsoleColor BoardColor(Node board)
            {
                var result = board == null ? 3 : Node.BoardResult(board);
                var resultColor = resultColors[result];
                return resultColor;
            }

            for (var row = 0; row < 3; ++row)
            {
                Console.SetCursorPosition(i1, j1+2*row+1);
                for (var col = 0; col < 3; ++col)
                {
                    char ccc;
                    if (board != null)
                        ccc = PieceChar(row, col, board.board);
                    else
                        ccc = ' ';
                    Console.ForegroundColor = edgeColor;
                    Console.Write("|");
                    Console.ForegroundColor = resultColor;
                    if (colorChildren)
                    {
                        var ind = Util.Index(row,col);
                        var c = board.children[ind];
                        if (c != null)
                        {
                            var sc = c.score; //-1,0,1
                            if (sc == -1) sc = 2; // remap
                            Console.ForegroundColor = resultColors[sc];
                        }
                    }
                    Console.Write($"{ccc}");
                }

                Console.ForegroundColor = edgeColor;
                Console.Write("|");
                Console.SetCursorPosition(i1, j1 + 2*row + 2);
                Console.Write(h);
            }

            if (showBest)
            {
                if (board != null)
                {

                    var bm = board.bestMoves.Aggregate("", (a, b) => a + (b+1).ToString());
                    var msg = $"{board.score}:{bm}          "; // make long
                    Console.SetCursorPosition(i1, j1 + 7);
                    Console.Write(msg[0..7]);
                    Console.SetCursorPosition(i1, j1 + 8);
                    Console.Write(msg[7..12]);
                }
                else
                {
                    Console.SetCursorPosition(i1, j1 + 7);
                    Console.Write("       ");
                    Console.SetCursorPosition(i1, j1 + 8);
                    Console.Write("       ");
                }
            }

            SetC(oldColors);
        }

        // draw board, colors, and results of choices
        static void DrawC(List<Node> moves, Node node)
        {
            int dx = 10, dy = 10;
            int cx = 2*dx, cy = 2;
            DrawC2(node,5,cy, true);
            for (var ch = 0; ch < 9; ch++)
            {
                var (r,c) = Util.Deindex(ch);
                DrawC2(node.children[ch], cx + dx * c, cy + dy * r);
            }
        }

        public static void DrawA(Node node, int i1, int j1)
        {
            for (var row = 0; row < 3; ++row)
            for (var col = 0; col < 3; ++col)
            {
                Console.SetCursorPosition(col + i1, row + j1);
                Console.Write(PieceChar(row, col, node.board));
            }
            var bm = node.bestMoves.Any() ? node.bestMoves[0] + 1 : -1;
            var cnt = node.board.Count(c => c != 0); // played
            var player = "XO"[cnt & 1];
            Console.SetCursorPosition(i1, j1 + 3);
            Console.Write($"{player}{bm}  ");

            var (_, h, _) = Util.MinHash(node.board);
            Console.SetCursorPosition(i1, j1 + 4);
            Console.Write(h); 
        }

        static char PieceChar(int row, int col, int[] pieces)
        {
            var ind = Util.Index(row, col);
            var p = pieces[ind];
            var ch = p != 0 ? cellText[p] : (char)(ind + '1');
            return ch;
        }

        public static void DrawB(Node node)
        {
            var h = "+-+-+-+";
            Console.WriteLine("\n\n" + h);

            for (var row = 0; row < 3; ++row)
            {
                for (var col = 0; col < 3; ++col)
                    Console.Write($"|{PieceChar(row,col, node.board)}");

                Console.WriteLine("|");
                Console.WriteLine(h);
            }

            Console.WriteLine();

#if false
            // children
            var (per, minHash, _) = board.MinHash();
            var bb = nodes[minHash];

            for (int i = 0; i < 9; ++i)
            {
                Console.Write($"{i + 1}: ");
                var c = bb.children[i];
                if (c == null)
                    Console.Write(" - placed...");
                else
                    WriteResult(c);
                Console.WriteLine();
            }
#endif
        }

        public static void Draw(int[] board)
        {
            for (var row = 0; row < 3; ++row)
            {
                for (var col = 0; col < 3; ++col)
                {
                    Console.Write(board[Util.Index(row,col)]);
                }
                Console.WriteLine();
            }
        }

    }
}
