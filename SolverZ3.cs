using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.Z3;

namespace Lomont.Games.TicTacToe
{
    internal class SolverZ3
    {
        // hold an expression to seek values that satisfy it
        class Expr
        {
            public readonly Context ctx;

            public Expr(BoolExpr ex, Context ctx)
            {
                expr = ex;
                this.ctx = ctx;
            }

            public BoolExpr expr;
            public readonly Dictionary<string, BoolExpr> verts = new();
            public readonly Dictionary<string, BoolExpr> edges = new();

            public BoolExpr AddEdge(Node parent, int move)
            {
                var hParent = Util.MinHash(parent.board).minHash;
                var hash = $"e_{hParent}_{move}";
                if (!edges.ContainsKey(hash))
                    edges.Add(hash, ctx.MkBoolConst(hash));
                return edges[hash];
            }

            public BoolExpr AddVert(Node? node)
            {
                Trace.Assert(node != null);
                var h = Util.MinHash(node.board).minHash;
                var hash = $"v_{h}";
                if (!verts.ContainsKey(hash))
                    verts.Add(hash, ctx.MkBoolConst(hash));
                return verts[hash];
            }

            // AND item into expression
            public void And(BoolExpr be)
            {
                expr = ctx.MkAnd(expr, be);
            }

        }

        // get vertices and edges needed in graph
        // NEW method: jump levels, only count on those needed for computer moves
        // pass in the first set of nodes the computer will see
        void MakeGraphExpressionSkips(List<Node> startNodes, int startPos, Expr expr)
        {
            var ctx = expr.ctx;
            // require start nodes to be in graph
            expr.And(ctx.MkAnd(startNodes.Select(expr.AddVert)));

            // idea will be to take an unadded vert, and add edges to second level in groups:
            // for each possible best move, make AND of all children from there, add verts to frontier
            // OR these AND groups, add to expression

            Queue<Node> frontier = new();
            HashSet<int> processed = new();
            foreach (var board in startNodes)
                frontier.Enqueue(board);
            while (frontier.Count > 0)
            {
                // Console.WriteLine($"Frontier count {frontier.Count}");
                var parent = frontier.Dequeue();
                var pMinHash = Util.MinHash(parent.board).minHash;
                if (processed.Contains(pMinHash))
                    continue;
                processed.Add(pMinHash); // don't process node again

                // lots of clauses will be OR'd
                List<BoolExpr> orClauses = new();
                foreach (var bestMoveTemp in parent.bestMoves)
                {
                    var bestMove = bestMoveTemp;
                    if (Util.Depth(parent.board) == 0)
                    {
                        // computer first, move to startpos is only choice
                        bestMove = startPos;
                    }

                    var child = parent.children[bestMove];
                    Trace.Assert(child != null);
                    //if (child == null) continue;

                    // start with a clause describing how to get from parent to this child...
                    var andClauses = new List<BoolExpr> { expr.AddEdge(parent, bestMove) };
                    var grandchildren = child.children.Where(gc => gc != null).Select(gc => gc!).ToList();

                    // add in all the grandchildren that go with this move
                    andClauses.AddRange(grandchildren.Select(expr.AddVert));

                    // create AND of all these, to be ORed later
                    orClauses.Add(ctx.MkAnd(andClauses));

                    // extend frontier with any unprocessed grandchildren
                    foreach (var gg in grandchildren)
                        if (!processed.Contains(Util.MinHash(gg.board).minHash))
                            frontier.Enqueue(gg);

                    if (Util.Depth(parent.board) == 0)
                    {
                        // computer first, move to center
                        break; // only center move at depth 0
                    }
                }

                if (orClauses.Count > 0)
                {
                    var impl = ctx.MkImplies(expr.AddVert(parent), ctx.MkOr(orClauses));
                    expr.expr = ctx.MkAnd(expr.expr, impl);
                }
            }
        }

        // get vertices and edges needed in graph
        void MakeGraphExpressionAll(
            List<Node> startNodes, 
            int startPos,
            bool computerFirst,
            Expr expr
                )
        {
            var ctx = expr.ctx;
            // require start nodes to be in graph
            expr.And(ctx.MkAnd(startNodes.Select(expr.AddVert)));

            // get expression for graph by adding edge constraints
            // todo - all start positions?
            Recurse(startNodes[0]/*todo*/, computerFirst);

            void Recurse(Node b, bool pickBest)
            {
                var possibleNextMoves = new List<Node?>();
                var depth = Util.Depth(b.board);

                if (pickBest)
                {
                    // need one of best moves
                    if (depth == 0)
                    {
                        // play start moves: 
                        if (startPos >= 0)
                        {
                            possibleNextMoves.Add(b.children[startPos]!);
                        }
                        else// add all children
                        {
                            possibleNextMoves.AddRange(b.children);
                        }
                    }
                    else
                    {
                        // best moves
                        possibleNextMoves.AddRange(b.bestMoves.Select(x => b.children[x])!);

                        // various kinds of children possible: win, don't lose, accept any...
                        //possibleNextMoves.AddRange(b.children.Where(x => x!=null));// && x.score>=0)!);
                    }
                }
                else
                {
                    // need all of moves
                    possibleNextMoves.AddRange(b.children!);
                }

                possibleNextMoves.RemoveAll(n2 => n2 == null);

                //var useAllChildren = !pickBest;

                AddEdges( /*useAllChildren, */depth, b, possibleNextMoves);

                foreach (var dest in possibleNextMoves)
                {
                    Trace.Assert(dest != null);
                    Recurse(dest, !pickBest);
                }
            }



            void AddEdges(int fromDepth, Node fromNode, List<Node?> toNodes)
            {
                var isOdd = (fromDepth & 1) == 1;
                var requireAll = computerFirst ^ !isOdd;

                if (toNodes.Count == 0)
                    return;
                var children = toNodes.Select(expr.AddVert).ToList();
                var parent = expr.AddVert(fromNode);
                //expr.edges += children.Count;
                var consequence = requireAll ? ctx.MkAnd(children) : ctx.MkOr(children);
                expr.expr = ctx.MkAnd(expr.expr, ctx.MkImplies(parent, consequence));
            }
        }

        (bool success, List<string> verts, List<string> edges) CheckBoundedExpr(Context ctx, Expr expr, uint bound)
        {

            // limit count of vertices used
            var boundExpr = ctx.MkAnd(expr.expr, ctx.MkAtMost(expr.verts.Values, (uint)bound));
            List<string> verts = new();
            List<string> edges = new();

            var (model, success) = Check(ctx, boundExpr, Status.SATISFIABLE);
            if (!success || model == null)
                return (false, verts, edges); // no solution found


            foreach (var v in expr.verts.Values)
            {
                var ev = model.Evaluate(v);
                var msg = $"{v}->{ev}";
                if (msg.Contains("true"))
                {
                    verts.Add(msg);
                    //Console.WriteLine(msg);
                }
            }

            foreach (var v in expr.edges.Values)
            {
                var ev = model.Evaluate(v);
                var msg = $"{v}->{ev}";
                if (msg.Contains("true"))
                {
                    edges.Add(msg);
                    //Console.WriteLine(msg);
                }
            }

            verts.Sort();
            edges.Sort();
            //foreach (var u in used)
            //    Console.WriteLine(u);


            return (true, verts, edges);
        }

        // add edges for human first and/or robotFirst 
        // see if bound sufficient
        (List<string> verts, List<string> edges) FindOptimalGraph(Node startNode,
            bool humanFirst, bool computerFirst,
            bool skipLevels = true, // skip levels, don't ever use for lookup
            int startPos = 4, // start move position, or -1 for all
            bool showSearch = true
        )
        {
            Console.Write($"Z3: human 1st {humanFirst}, computer 1st {computerFirst}, start pos {startPos}, skip levels {skipLevels}");

            // at each depth from root:
            // - if choice is table driven depth, select ONE of the best scoring moves
            // - if choice is player choice depth, select ALL of the moves
            // - if a node chosen, must continue down till end of game
            //
            // want minimal number of nodes needed to satisfy


            Microsoft.Z3.Global.ToggleWarningMessages(true);

            using (var ctx = new Context(new Dictionary<string, string>() { { "model", "true" } }))
            {
                // build all requirements into these expression
                var expr1 = new Expr(ctx.MkBool(true), ctx);
                var expr2 = new Expr(ctx.MkBool(true), ctx);


                // for robot goes first, starts in center
                // failed: 35,38,39,40
                // worked: 41,
                // best: 41

                // for human goes first, starts anywhere
                // failed: 100, 125, 126
                // worked: 170,160,150, 138, 130, 127
                // best: 127

                // both cases together
                // failed: 170, 200, 207, 211
                // worked: 230, 215, 213, 212
                // best: 212 (weird, since got lower to ~160 with other percolation method?)

                // robot first, min is 41 standalone
                if (computerFirst)
                {
                    if (skipLevels)
                        MakeGraphExpressionSkips(new List<Node> { startNode }, startPos, expr1);
                    else
                        MakeGraphExpressionAll(new List<Node>{startNode}, startPos, true, expr1);
                }

                // human first
                if (humanFirst)
                {
                    if (skipLevels)
                        MakeGraphExpressionSkips(startNode.children.ToList(), -1, expr2);
                    else
                        MakeGraphExpressionAll(new List<Node>{startNode}, startPos, false, expr2);
                }

                var expr = (humanFirst, computerFirst) switch
                {
                    (true, true) => MakeOr(expr1, expr2),
                    (false, true) => expr1,
                    (true, false) => expr2,
                    (false, false) => throw new Exception("Need human or computer or both first")
                };

                Expr MakeOr(Expr cFirst, Expr hFirst)
                {
                    var expr2 = new Expr(ctx.MkBool(true), ctx);
                    Console.WriteLine($"Verts {cFirst.verts.Count}, {hFirst.verts.Count}");
                    Add(cFirst);
                    Console.WriteLine($"Verts {expr2.verts.Count}");
                    Add(hFirst);
                    Console.WriteLine($"Verts {expr2.verts.Count}");


                    var cb1 = ctx.MkBoolConst("cFirst");
                    var cImpl = ctx.MkImplies(cb1, ctx.MkAnd(cb1, cFirst.expr));
                    var cnot = ctx.MkNot(cb1);
                    var hImpl = ctx.MkImplies(cnot, ctx.MkAnd(cnot, hFirst.expr));
                    var both = ctx.MkAnd(cImpl, hImpl);

                    var forall = ctx.MkForall(new[] { cb1 }, both);
                    expr2.expr = forall;

                    return expr2;

                    void Add(Expr e1)
                    {
                        foreach (var (k, v) in e1.verts)
                            if (!expr2.verts.ContainsKey(k))
                                expr2.verts.Add(k, v);
                    }

                }

                //uint bound = 212;

                // todo - learn how to use simplification
                /*
                var t = ctx.MkTactic("simplify");
                var g = ctx.MkGoal();
                var t2 = t.Apply(allExpr);
                ApplyTactic(ctx, , allExpr);
                */

                Console.Write(
                    $" => {expr.verts.Count} verts, {expr.edges.Count} edges");
                if (showSearch)
                    Console.WriteLine();

                var minSuccessful = FindEdge(expr.verts.Count,
                    bound =>
                    {
                        if (showSearch)
                            Console.Write($"Testing {bound} = ");
                        var (success, _, _) = CheckBoundedExpr(ctx, expr, (uint)bound);
                        if (showSearch)
                            Console.Write(success ? "SUCCESS, " : "FAILED, ");
                        return success;
                    });
                if (showSearch) 
                    Console.WriteLine();
                

                var (success, verts, edges) = CheckBoundedExpr(ctx, expr, (uint)minSuccessful);
                Trace.Assert(success);
                Trace.Assert(minSuccessful==verts.Count);
                Console.WriteLine($": Optimal {verts.Count} verts, {edges.Count} edges");
                return (verts, edges);
            }
        }


        public int FindEdge(int upperBound, Func<int, bool> predicate)
        {
            var maxFail = upperBound; // start here, shrink until fails
            while (true)
            {
                var bound = maxFail;
                var ok = predicate(bound);

                if (ok)
                    maxFail /= 2;
                else
                    break;
                if (maxFail == 0)
                    throw new Exception("all succeeded!?");
            }


            var minSuccess = maxFail;

            while (true)
            {
                var bound = minSuccess;

                var ok = predicate(bound);

                if (!ok)
                    minSuccess *= 2;
                else
                    break;

                if (minSuccess > upperBound)
                    throw new Exception("all failed!?");

            }

            while (maxFail != minSuccess - 1)
            {
                var bound = (maxFail + minSuccess) / 2;

                var ok = predicate(bound);

                if (ok)
                    minSuccess = bound;
                else
                    maxFail = bound;
            }

            return minSuccess;

        }

        List<Node> positions = new();
        // get all poitions reachable from start into class instance member
        void ReadPositions(Node startNode)
        {
            positions.Clear();
            Util.Recurse(startNode, n =>
                {
                    positions.Add(n);
                    return true;
                });

        }

        public void MakeStats(Node startNode, bool dumpNodes = false)
        {
            ReadPositions(startNode);
            // robot first move corner, size 56
            // robot first move edge, size 94
            // robot first move middle, size 41
            // if allowed to have any child (even losing), 35 entries
            foreach (var startPos in new[]{0,1,4})
            {
                var b1 = FindOptimalGraph(startNode: startNode,
                    humanFirst: false,
                    computerFirst: true,
                    skipLevels: false, 
                    startPos: startPos, showSearch: false);
                if (dumpNodes)
                    Dump(b1.verts);

                var b2 = FindOptimalGraph(startNode: startNode,
                    humanFirst: false,
                    computerFirst: true,
                    skipLevels: true, 
                    startPos: startPos, showSearch: false);
                if (dumpNodes)
                    Dump(b2.verts);
            }

            void Dump(List<string> v)
            {
                var tb = Parse((v, new()));
                foreach (var t in tb)
                {
                    Console.Write($"{{{t.MinHash},{t.BestMove},{t.Result}}},");
                }

                Console.WriteLine();

            }

            // human 
            var b3 = FindOptimalGraph(startNode: startNode,
                humanFirst: true,
                computerFirst: false, 
                skipLevels: false, 
                startPos: -1,
                showSearch: false
                );
            if (dumpNodes)
                Dump(b3.verts);
            var b4 = FindOptimalGraph(startNode: startNode,
                humanFirst: true,
                computerFirst: false,
                skipLevels: true,
                startPos: -1,
                showSearch: false
                );
            if (dumpNodes)
                Dump(b4.verts);

        }


        public (List<TableEntry> bestRobot, List<TableEntry> bestHuman, List<TableEntry> bestBoth) Solve(Node startNode)
        {
            ReadPositions(startNode);

            // robot first move corner, size 56
            // robot first move edge, size 94
            // robot first move middle, size 41
            // if allowed to have any child (even losing), 35 entries
            var bestRobot = FindOptimalGraph(startNode: startNode, humanFirst: false, computerFirst: true);
            //return (Parse(bestRobot), new(),new());

            // 127
            var bestHuman = FindOptimalGraph(startNode: startNode, humanFirst: true, computerFirst: false);

            return (Parse(bestRobot), Parse(bestHuman), new());

            // 212
            var bestBoth = FindOptimalGraph(startNode: startNode, humanFirst: true, computerFirst: true);

            return (Parse(bestRobot), Parse(bestHuman), Parse(bestBoth));

        }

        List<TableEntry> Parse((List<string> verts, List<string> edges) b)
        {
            var regexV = new Regex(@"v_(?<hash>[0-9]+)->true");
            var regexE = new Regex(@"e_(?<hash>[0-9]+)_(?<move>[0-9])->true");

            var dVerts = b.verts.Select((b1, i) =>
            {
                // vert: v_n->true map to integer n
                // edge: e_n_m->true map to entry n, move m

                var m = regexV.Match(b1);
                Trace.Assert(m.Success);
                var n = m.Groups["hash"].Value;
                return Int32.Parse(n); // node hash value
            }).ToList();
            var dEdges = b.edges.Select((b2, i) =>
            {
                // vert: v_n->true map to integer n
                // edge: e_n_m->true map to entry n, move m

                var m = regexE.Match(b2);
                Trace.Assert(m.Success);
                var n = m.Groups["hash"].Value;
                var mv = m.Groups["move"].Value;
                return (Int32.Parse(n), Int32.Parse(mv));
            }).ToList();
            //Trace.Assert(dVerts.Count == dEdges.Count);
            //List<int> ans = new();
            //ans.AddRange(dVerts);
            //return ans;
            if (dEdges.Any())
                return dEdges.Select(e => new TableEntry(e.Item1, e.Item2, 0)).ToList();

            // must get move to match the node selected
            return dVerts
                .Select(n => (node: n, pair: BestMove(n, dVerts)))
                .Select(t => new TableEntry(t.node, t.pair.move, t.pair.result))
                .ToList();
        }

        // find the (single!?) move from node hash n into another node in the list, and the result
        (int move, int result) BestMove(int n, List<int> nodes)
        {
            var parent = GetNode(n);
            var bm = -1; // best move
            var result = 9; // marks continue
            for (int i = 0; i < 9; ++i)
            {
                var child = parent.children[i];
                if (child != null)
                {
                    var (h, mh, p) = Util.MinHash(child.board);
                    if (nodes.Contains(h) || nodes.Contains(mh))
                    {
                        // found it!
                        bm = i;
                        result = Util.Outcome(child.board);
                        break;
                    }
                }
            }
            return (bm,result);
        }

        Node GetNode(int n)
        {
            var node = positions.FirstOrDefault(p =>
            {
                var (h,mh,_) = Util.MinHash(p.board);
                return h == n || mh == n;
            });
            Trace.Assert(node != null);
            return node;

        }


        static (Model?,bool) Check(Context ctx, BoolExpr f, Status sat)
        {
            var s = ctx.MkSolver();
            s.Assert(f);
            if (s.Check() != sat)
                return (null, false); // failed
            if (sat == Status.SATISFIABLE)
                return (s.Model,true);
            else
                return (null,true);
        }

    }
}
