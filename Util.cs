using System.Diagnostics;

namespace Lomont.Games.TicTacToe
{
    public static class Util
    {
        // row,column in 0-2 to index in 0-8
        public static int Index(int row, int col) => col + row * 3;
        // index 0-8 to row, column
        public static (int row, int col) Deindex(int index) => (index / 3, index % 3);

        // hash board with 0,1,2 entries to hash in 0 to 3^9-1
        public static int Hash(int[] board)
        {
            var hash = 0;
            for (var i = board.Length - 1; i >= 0; i--)
                hash = 3 * hash + board[i];
            return hash;
        }
        
        // reverse hash into a board
        public static int[] Dehash(int hash)
        {
            var board = new int[9];
            for (var i = 0; i < board.Length; i++)
            {
                board[i] = hash%3;
                hash /= 3;
            }

            return board;
        }

        // compute hash of board under all 8 permutations, 
        // return and hash of original position, min hash found, perm to get min hash
        public static (int hash, int minHash, int minPerm) MinHash(int[] board)
        {
            var hashes = Enumerable.Range(0, 8).Select(p => (perm: p, hash: Hash(PermuteBoard(p, board)))).ToList();
            var hash = hashes[0].hash;
            var minHash = hashes.Min(p => p.hash);
            var minPerm = hashes.First(p => p.hash == minHash).perm;
            return (hash, minHash, minPerm);
        }

        // count nodes
        public static int Count(Node node)
        {
            return Rec(node);

            static int Rec(Node n)
            {
                var count = 1;
                foreach (var child in n.children)
                    count += child != null?Rec(child):0;
                return count;
            }
        }

        // from a board, compute who is to move, 1 or 2
        public static int ToMove(int[]board)
        {
            var p1 = board.Count(p => p == Node.Player1);
            var p2 = board.Count(p => p == Node.Player2);
            Trace.Assert(p1 == p2 || p1 - 1 == p2);
            var isOdd = ((p1 + p2) & 1) == 1;
            return isOdd ? Node.Player2 : Node.Player1;
        }

        // count depth of board, which is number of moves made 0-9
        public static int Depth(int[] board) => board.Count(p => p != 0);

        // get list of legal moves, each in 0-8
        public static List<int> LegalMoves(int[]board) =>
            board
                .Select((item, index) => (item, index))
                .Where(p => p.item == 0)
                .Select(p => p.index)
                .ToList();

        // apply permutation 0-7 to row,col, get permuted row, col
        public static (int row, int col) ApplyPerm(int perm, int row, int col)
        {
            var pr = row;
            var pc = col;

            if ((perm & 1) != 0)
                pr = 2 - pr;
            if ((perm & 2) != 0)
                pc = 2 - pc;
            if ((perm & 4) != 0)
                (pr, pc) = (pc, pr);
            return (pr, pc);
        }

        // invert permutation 0-7 from permuted row,col to get original row, col
        public static (int pr, int pc) InvertPerm(int perm, int row, int col)
        {
            var pr = row;
            var pc = col;

            if ((perm & 4) != 0)
                (pr, pc) = (pc, pr);
            if ((perm & 2) != 0)
                pc = 2 - pc;
            if ((perm & 1) != 0)
                pr = 2 - pr;

            return (pr,pc);
        }

        // loop over (row,col), col first
        public static IEnumerable<(int row, int col)> Loop()
        {
            for (var row = 0; row < 3; ++row)
            for (var col = 0; col < 3; ++col)
                yield return (row, col);
        }

        // apply permutation 0-7 to this board, return new one
        public static int[] PermuteBoard(int perm, int[] board)
        {
            var b = new int[9];
            foreach (var (row,col) in Util.Loop())
            {
                var (row1, col1) = ApplyPerm(perm, row, col);
                b[Index(row, col)] = board[Index(row1, col1)];
            }
            return b;
        }
        // recurse from board over all parent,move,children triples
        // apply functor, return true if recurse further
        public static void Recurse(Node parent, Func<Node, Node, int, bool> checker)
        {
            for (var move = 0; move < 9; ++move)
            {
                var child = parent.children[move];
                if (child!= null && checker(parent, child, move))
                    Recurse(child, checker);
            }
        }

        // recurse from board over all children
        // apply functor, return true if recurse further
        public static void Recurse(Node p, Func<Node, bool> checker)
        {
            var rec = checker(p);
            if (rec)
            {
                foreach (var ch in p.children.Where(c=>c!=null))
                    Recurse(ch!,checker);
            }
        }

        // percent formatter
        public static string Pct(int v, int total, int len=2)
        {
            var d = 100.0 * v;
            d /= total;
            return $"{d:F2}%";
        }

        // given player, get next player
        public static int NextPlayer(int player) => player == Node.Player1 ? Node.Player2 : Node.Player1;

        // outcome is 0 = nothing yet, 1,2,3 = row win 1,2,3; 4,5,6 = col win 1,2,3; 7,8 = diag UL to LR, UR, to LL, 9 = draw
        // negative for player 2
        public static int Outcome(int[] board)
        {
            if (Depth(board) == 9) return 9; // draw
            for (var t = 0; t < runs.Length; t += 3)
            {
                var (a, b, c) = (board[runs[t]], board[runs[t + 1]], board[runs[t + 2]]);
                if (a == b && b == c && c != 0)
                {
                    var it = 1 + (t / 3);
                    if (a == 2) it = -it;
                    return it;
                }
            }

            return 0; // nothing
        }

        static readonly int[] runs = {
            // rows
            0,1,2,3,4,5,6,7,8,
            // cols
            0,3,6,1,4,7,2,5,8,
            // diags
            0,4,8,
            2,4,6
        };

    }
}
