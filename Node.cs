
namespace Lomont.Games.TicTacToe
{
    // hold a board position and related info
    // used as node in a game tree
    public class Node
    {
        #region constants

        public const int Player1 = 1;
        public const int Player2 = 2;

        #endregion

        #region stuff for final boards

        int minHash; // minimal hash of 8 symmetries
        int bestMove; // 0-9, relative to min hash position
        int result; // 0,1,2,3

        #endregion

        #region helper variables

        // used to track a minimal set of nodes covering the whole game
        public bool onMinPath = false;

        // board holds pieces 0,1,2
        public int[] board = new int[9];

        // sum of leaf results under this node
        // at leaf, only one of these set to 1, all other nodes have higher totals
        public int wins1 = 0, wins2 = 0, draws = 0;
        public int LeafCount => wins1 + wins2 + draws;

        // normalized children
        public Node?[] children = new Node[9];

        // minimax score for this node, positive for player 1, 0 for draw, negative for player 2, 3 unscored
        public int score = 3;
        public List<int> bestMoves = new(); // best moves that obtain score

        // has this board been scored with wins, losses, draws?
        public bool Scored => LeafCount != 0;

        #endregion


        // accessor
        public int this[int row, int col]
        {
            get => board[Util.Index(row, col)];
            set => board[Util.Index(row, col)] = value;
        }

        // make clone of board with same pieces, nothing else
        public Node ClonePieces()
        {
            var b = new Node();
            for (var i = 0; i < board.Length; ++i)
            {
                b.board[i] = board[i];
            }

            return b;
        }

        public int ToMove() => Util.ToMove(board);

        // apply permutation 0-7 to this board
        public void PermuteBoard(int perm) => board = Util.PermuteBoard(perm, board);


        // 3 = more to play, 1,2 = player wins, 0 = forced draw
        // todo - unify this and the column mode markings
        public static int BoardResult(Node node)
        {
            for (var i = 0; i < 3; i++)
            {
                if (ThreeInRow(i, 0, 0, 1))
                    return node[i, 0];
                if (ThreeInRow(0, i, 1, 0))
                    return node[0, i];
            }

            if (ThreeInRow(0, 0, 1, 1))
                return node[0, 0];
            if (ThreeInRow(2, 0, -1, 1))
                return node[2, 0];

            if (node.board.All(c => c != 0))
                return 0; // no more moves left
            return 3; // more to play

            bool ThreeInRow(int x, int y, int dx, int dy)
            {
                var (v1, v2, v3) = (node[x, y], node[x + dx, y + dy], node[x + 2 * dx, y + 2 * dy]);
                return v1 == v2 && v2 == v3 && v1 != 0;
            }
        }
    }
}
