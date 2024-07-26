namespace Lomont.Games.TicTacToe
{
    internal static class Misc
    {

        // compute a min set of nodes covering all play
        public static void ComputeMin(Node b, Dictionary<int,Node> nodes)
        {
            var seed = 0;
            var minScore = Int32.MaxValue;
            var minSeed = 0;

            // 164 best, seed 241683
            // Min path nodes 162, seed 3950275
            // Min path nodes 160, seed 21973427
            // Min path nodes 159, seed 48994875
            // if only robot goes first, selects center, got down to
            // 41, seed 78193 (beats a 45 I saw online.... what *is* optimal?)

            while (true)
            {
                // clear path
                foreach (var b1 in nodes.Values)
                    b1.onMinPath = false;
                ++seed;
                var rand = new Random(seed);

                // two paths: when robot starts, pick center & pick best each time, human opponent anything
                // when human moves, any first item, then robot best each time

                WalkPath(b, true, true, rand); // robot first
                WalkPath(b, false, false, rand);

                var minPath = nodes.Values.Count(p => p.onMinPath);
                if (minPath < minScore)
                {
                    minScore = minPath;
                    minSeed = seed;
                    Console.WriteLine($"Min path nodes {minPath}, seed {seed}");
                }

            }
            void Score(Node board)
            {
            }

            void WalkPath(Node node, bool pickBest, bool startCenter, Random rand)
            {
                node.onMinPath = true; // need this board

                //Console.WriteLine($"best: {b.bestMoves.Count} {b.wins1}:{b.draws}:{b.wins2}");

                if (startCenter)
                {
                    Next(node.children[4]); // center
                    Score(node);
                }
                else if (pickBest)
                {
                    // pick best, prefer seen ones
                    // other TODO heuristic ideas
                    // - pick child with least leaves that is among best
                    var pickFrom = node.bestMoves.Where(v => node.children[v].onMinPath).ToList();
                    if (!pickFrom.Any())
                        pickFrom = node.bestMoves;
                    if (pickFrom.Any())
                    {
                        var pick = pickFrom[rand.Next(pickFrom.Count)];
                        Next(node.children[pick]);
                        Score(node);
                    }
                }
                else
                {
                    // add all moves
                    for (var i = 0; i < node.children.Length; i++)
                    {
                        var nextBoard = node.children[i];
                        if (nextBoard != null)
                            WalkPath(nextBoard, !pickBest, false, rand);
                    }
                }

                void Next(Node nextBoard)
                {
                    WalkPath(nextBoard, !pickBest, false,  rand);
                }
            }
        }

        public static void ReduceToMin(Dictionary<int, Node> nodes)
        {
            var minPos = nodes.Values.Where(p => p.onMinPath).ToList();
            nodes.Clear();
            foreach (var p in minPos)
            {
                var (_, minHash, perm) = Util.MinHash(p.board);
                nodes.Add(minHash, p);
            }
            // remove any "best moves" leading out of min set
            foreach (var p in minPos)
            {
                var ok = p.bestMoves.Where(m => p.children[m].onMinPath).ToList();
                p.bestMoves = ok;
            }
        }

        // make simple linear lookup for items
        public static void MakeTable(List<Node> nodes)
        {
            using var f = File.CreateText("tinyTbl.txt");
            nodes.Sort((a, b) => Util.MinHash(a.board).minHash.CompareTo(Util.MinHash(b.board).minHash));

            f.WriteLine("void Lookup(int hash)");
            f.WriteLine("{");
            var pref = "   ";
            foreach (var p in nodes)
            {
                f.Write($"{pref}if (hash == {Util.MinHash(p.board).minHash}) {{ ");
                var wins1 = (p.LeafCount == 1 && p.wins1 == 1) ? 1 : 0;
                var wins2 = (p.LeafCount == 1 && p.wins2 == 1) ? 1 : 0;
                var draws = (p.LeafCount == 1 && p.draws == 1) ? 1 : 0;
                var move = p.bestMoves.Any() ? p.bestMoves[0] : -1;

                f.Write($"win1 = {wins1}; win2 = {wins2}; draw = {draws}; move = {move};");
                f.WriteLine($" return;}}");
            }
            // last is error
            f.WriteLine($"{pref}error = 1; return;");
            f.WriteLine("}");
            f.Close();
        }

        public record Edge(int StartHash, int Move, int EndHash, int Result);

        public static void DumpMathematicaGraph(string filename, IEnumerable<Edge> entries, Func<Edge,bool>? passPredicate = null)
        {
            // make list of directed edges
            // DirectedEdge[u,v,t] draws u to v with optional tag t

            if (passPredicate == null)
                passPredicate = f => true;

            using var f = File.CreateText(filename);
            f.WriteLine("Graph[{");
            foreach (var e in entries)
            {
                if (passPredicate(e))
                    f.Write($"DirectedEdge[{e.StartHash},{e.EndHash},{e.Move}],");
            }
            f.WriteLine("}];");
        }


        // Make mathematica graph
        public static void DumpGraph(Node b, Dictionary<int, Node> nodes)
        {
#if false

            var verts = new List<string>();
            var edges = new List<(string, string)>();
            foreach (var v in nodes.Values)
            {
                var vh = v.Hash();
                verts.Add(vh);
                foreach (var c in v.children)
                {
                    if (c != null)
                        edges.Add((vh, c.Hash()));
                }
            }

            using var f = File.CreateText("ttt.txt");
            f.WriteLine("Graph[{");
            for (var i = 0; i < verts.Count; i++)
            {
                var p = nodes[verts[i]];
                //if (p.score == 3)
                {
                    f.Write($"\"{verts[i]}\"");
                }
                //else
                //{
                //    var ch = "XDO"[p.score+1];
                //
                //    f.Write($"Labeled[\"{verts[i]}\",\"{ch}\"]");
                //}

                if (i != verts.Count - 1)
                    f.Write(",");
            }

            f.WriteLine();
            f.WriteLine("},{");
            for (int i = 0; i < edges.Count; i++)
                f.Write($"DirectedEdge[\"{edges[i].Item1}\",\"{edges[i].Item2}\"]" + (i != edges.Count - 1 ? "," : ""));
            f.WriteLine();
            f.WriteLine("},");
            f.Write("VertexStyle->{");
            WriteVS(1, "Green");
            WriteVS(-1, "Red");
            WriteVS(0, "Yellow");
            f.Write($"\"_________\"->Blue");
            f.WriteLine("}");

            f.WriteLine("];");
            f.Close();

            void WriteVS(int score, string color)
            {
                foreach (var k in nodes.Keys.Where(kk => nodes[kk].score == score))
                {
                    if (k == "_________") continue;
                    f.Write($"\"{k}\"->{color},");
                }
            }
#endif

        }
    }
}
