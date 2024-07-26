using System.Diagnostics;

namespace Lomont.Games.TicTacToe
{
    interface IPlayer
    {
        /*
         generate legal move given a board
         return true to continue games, else stop simulation
         */
        (int move, bool exit) GetMove(int[] board);
        string Name { get; }
    }

    class RandomPlayer : IPlayer
    {
        readonly Random rand = new Random(123);

        public string Name => "Random Player";

        public (int move, bool exit) GetMove(int[] board)
        {
            var legal = Util.LegalMoves(board);
            var w = rand.Next(legal.Count);
            return (legal[w],false);
        }
    }

    class ConsolePlayer : IPlayer
    {
        public string Name => "Console Player";

        public (int move, bool exit) GetMove(int[] board)
        {
            while (true)
            {
                var ch = Console.ReadKey(true).KeyChar;
                if ('1' <= ch && ch <= '9')
                {
                    var m = ch - '1';
                    if (board[m] == 0)
                    {
                        return (m, false);
                    }
                }

                if (ch == 'q')
                {
                    return (-1, true);
                }
            }
        }
    }

    class TreePlayer :IPlayer
    {
        readonly Random rand = new(123);
        readonly Dictionary<int, Node> nodes;
        public string Name {get;}

        public TreePlayer(Dictionary<int,Node> nodes, string name = "Tree Player")
        {
            Name = name;
            this.nodes = nodes;
        }
        public (int move, bool exit) GetMove(int[] board)
        {
            // get board directly or via min perm
            var (hash,minHash,perm) = Util.MinHash(board);
            var direct = nodes.ContainsKey(hash);
            var b = direct?nodes[hash]:nodes[minHash];
            var m = b.bestMoves;
            var nextMove = m[rand.Next(m.Count)];
            if (!direct)
            { // invert move
                var (r,c) = Util.Deindex(nextMove);
                (r,c) = Util.ApplyPerm(perm, r, c);
                nextMove = Util.Index(r, c);
            }
            return (nextMove,false);
        }
    }

    class TablePlayer : IPlayer
    {
        readonly List<TableEntry> tbl;
        public string Name { get; }

        public TablePlayer(List<TableEntry> tbl, string name = "Table Player")
        {
            Name = name; this.tbl = tbl;
        }

        public (int move, bool exit) GetMove(int[] board)
        {
            var (_, minHash, perm) = Util.MinHash(board);
            var b = tbl.First(e => e.MinHash == minHash);


            var (r, c) = Util.Deindex(b.BestMove);
            var (pr, pc) = Util.ApplyPerm(perm, r, c);
            var nextMove = Util.Index(pr, pc);
            return (nextMove, false);
        }
    }

    class AllPosPlayer : IPlayer
    {
        public HashSet<int> seen = new();
        public string Name => "All Pos Player";

        public (int move, bool exit) GetMove(int[] board)
        {
            var legal = Util.LegalMoves(board);
            var tm = Util.ToMove(board);
            // prefer unseen
            foreach (var m in legal)
            {
                // see if child seen
                Trace.Assert(board[m]==0);
                board[m] = tm;
                var hash = Util.Hash(board);
                board[m] = 0;
                if (seen.Contains(hash))
                    continue;
                seen.Add(hash);
                return (m, false);
            }
            // all children seen, do 0
            return (legal[0],false);
        }
    }

    internal static class Play
    {
        class Track
        {
            public IPlayer player;
            public int wins = 0;
        }
        public static (int wins1, int wins2, int draws) PlayGame(IPlayer player1, IPlayer player2, int numGames = -1, bool showboards = true)
        {
            (int left, int top) lastPos = Console.GetCursorPosition();
            var exit = false;
            var draws = 0;

            var p1 = new Track { player = player1 };
            var p2 = new Track { player = player2 };
            var gameCount = 0;

            while (!exit)
            {
                if (numGames>0 && gameCount >= numGames)
                {
                    Console.SetCursorPosition(lastPos.left, lastPos.top);
                    Console.Write("FINAL: ");
                    Stats(false);
                    break;
                }

                gameCount++;


                var board = new Node();
                var gameOver = false;
                while (!gameOver && !exit)
                {
                    if (showboards)
                    {
                        Draw1.DrawB(board);
                    }

                    var res = Node.BoardResult(board);
                    var toMove = board.ToMove();
                    var player = toMove == Node.Player1 ? p1 : p2;
                    if (showboards)
                        Console.WriteLine(player.player.GetType().Name);
                    if (res != 3)
                    {
                        if (res == 0) draws++;
                        if (res == 1) p1.wins++;
                        if (res == 2) p2.wins++;
                        if (showboards)
                        {
                            var result = new[] { "draw", "Player 1 wins", "Player2 wins" };
                            Console.WriteLine("GAME OVER! " + result[res]);
                        }
                        Stats();
                        gameOver = true;
                        continue;
                    }

                    var (move, quit) = player.player.GetMove(board.board);
                    if (quit)
                    {
                        Console.WriteLine("player quitting");
                        return (p1.wins, p2.wins,draws);
                    }
                    if (board.board[move] != 0)
                        throw new Exception("Invalid move!");

                    board.board[move] = toMove;
                }
            }
            return (p1.wins, p2.wins, draws);


            void Stats(bool reset = true)
            {
                if (reset)
                    Console.SetCursorPosition(lastPos.left, lastPos.top);

                var total = p1.wins + p2.wins + draws;
                var msg = $"Score: {p1.player.Name} {p1.wins}({Util.Pct(p1.wins,total)}), {p2.player.Name} {p2.wins}({Util.Pct(p2.wins, total)}), draws {draws}({Util.Pct(draws,total)})          ";
                Console.WriteLine(msg);
            }
        }
    }
}
