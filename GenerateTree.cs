namespace Lomont.Games.TicTacToe
{
    public class GenerateTree
    {
        // stats
        public int nodesExamined = 0;

        // nodes reached
        public Dictionary<int, Node> nodes = new();

        // root node after Generate done
        public Node startNode = new Node();

        int method = 0;

        // gen all nodes
        // method 2 = reduce by symmetries & hash them, 1 = hash them only, 0 = full tree
        // call Score after this to score tree
        public Node Generate(int method = 2)
        {
            var style = method switch
            {
                0 => "Full tree",
                1 => "Hashed nodes",
                2 => "Reduced symmetries",
                _ => throw new ArgumentException("Method should be in 0-2")
            };

            this.method = method;
            nodes = new Dictionary<int, Node>();
            nodesExamined = 0;

            // gen all nodes
            startNode = new Node();

            startNode = MakeGraph(startNode, Node.Player1);
            //ScoreGraph();

            int recCount = 0, leafCount=0;
            Util.Recurse(startNode, p =>
            {
                recCount++;
                if (p.children.All(c => c == null))
                    ++leafCount;
                return true;
            });

            
            Console.WriteLine($"{style}: {nodesExamined} nodes examined, {nodes.Count} retained, reachable nodes {Util.Count(startNode)}={recCount}, leaf count {leafCount}");

            return startNode;
        }

        // return normalized board
        Node MakeGraph(Node node, int toMove)
        {
            nodesExamined++;

            int key;
            if (method != 0)
            {
                var (hash, minHash, perm) = Util.MinHash(node.board);
                key = method==2 ? minHash : hash;
                if (nodes.ContainsKey(key))
                {
                    node = nodes[key];
                    return node;
                }

                if (method==2)
                {
                    // update board to match best permutation
                    node.PermuteBoard(perm);
                }
            }
            else 
                key = nodesExamined;
            nodes.Add(key, node);

            var result = Node.BoardResult(node);

            if (result == Node.Player1)
            {
                node.score = 1;
                node.wins1 = 1;
                return node;
            }

            if (result == Node.Player2)
            {
                node.score = -1;
                node.wins2 = 1;
                return node;
            }

            if (result == 0)
            {
                node.score = 0;
                node.draws = 1;
                return node;
            }

            for (var move = 0; move < node.board.Length; move++)
            {
                if (node.board[move] == 0)
                {
                    var b2 = node.ClonePieces();
                    b2.board[move] = toMove;
                    b2 = MakeGraph(b2, Util.NextPlayer(toMove));
                    node.children[move] = b2;
                }
            }

            return node;

          
        }


        public void ScoreGraph(bool verbose = true)
        {
            var pref = "   ";
            if (verbose)
                Console.WriteLine("Scoring graph:");
            // iterate
            var done = false;
            var pass = 0;
            while (!done)
            {
                if (verbose)
                    Console.Write($"{pref}Pass: {nodes.Values.Max(c => c.draws)}");
                int updated = 0, unscored = 0;
                foreach (var board in nodes.Values)
                {
                    if (board.Scored)
                        continue;

                    var gc = board.children.Where(c => c != null).ToList();
                    if (gc.All(c => c!.Scored))
                    {
                        board.wins1 = gc.Sum(c => c!.wins1);
                        board.wins2 = gc.Sum(c => c!.wins2);
                        board.draws = gc.Sum(c => c!.draws);
                        ++updated;
                        //  Draw(board);
                        //Trace.Assert(board.scored);
                        SelectBest(board);
                        //Trace.Assert(board.score != 3);
                    }
                    else
                        unscored++;
                }

                if (verbose)
                    Console.WriteLine($"->{nodes.Values.Max(c => c.draws)} updated {updated} unscored {unscored}");
                done = updated == 0; // needs more work
                                     // if (++pass > 10) break;
            }

            if (verbose)
            {
                var total = startNode.wins1+startNode.wins2+startNode.draws;
                Console.WriteLine($"{pref}Player 1 wins {startNode.wins1}({Util.Pct(startNode.wins1,total)}), Player 2 wins {startNode.wins2}({Util.Pct(startNode.wins2, total)}), draws {startNode.draws}({Util.Pct(startNode.draws, total)})");
            }

            void SelectBest(Node b)
            {
                var blankCount = b.board.Count(c => c == 0); // 0-9 = 1,-1,1,-1,... to move
                var toMove = (blankCount & 1) == 1 ? Node.Player1 : Node.Player2;

                // non-null children
                if (toMove == Node.Player1)
                {
                    // pick max result from child
                    var curScore = -1; // start at worst

                    for (var i = 0; i < 9; ++i)
                    {
                        var ch = b.children[i];
                        if (ch == null)
                            continue;
                        var s = ch.score;
                        if (s >= curScore)
                        {
                            if (s > curScore)
                                b.bestMoves.Clear();
                            curScore = s;
                            b.bestMoves.Add(i);
                        }
                    }

                    b.score = curScore;
                }
                else if (toMove == Node.Player2)
                {
                    // pick min result from child
                    var curScore = +1; // start at worst

                    for (var i = 0; i < 9; ++i)
                    {
                        var ch = b.children[i];
                        if (ch == null)
                            continue;
                        var s = ch.score;
                        if (s <= curScore)
                        {
                            if (s < curScore)
                                b.bestMoves.Clear();
                            curScore = s;
                            b.bestMoves.Add(i);
                        }
                    }
                    b.score = curScore;
                }
            }
        }

    }
}
