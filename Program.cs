// Code to analyze TicTacToe trees and to make small resource playing versions
// Chris Lomont Jul 2024

/* Design:
- todo - cleanup messages here
- board is int[9], 0=empty, 1 = player 1, 2 = player 2
- order is (conceptually) top row first (left to right), then middle row, then bottom row

- game trees can be made and inspected

- for each board in tree, need:
   - board number
   - if is draw = 0, win 1 = 1,win 2 = 2, unknown = 3 result
   - best move square (relative to min hash tree)

Results
- to hash board, store as integer less than in 3^9=19683
- to make small, remove symmetries in game tree (use min hashed version per board)
- total reachable boards = 5478 via walking tree
- symmetries lowers to 750ish
- min needed ~ 256

Notes:
[1] https://web.archive.org/web/20130628112339/http://www.mathrec.org/old/2002jan/solutions.html
[2] optimal strategy https://xkcd.com/832/

TODO - ideas to integrate
  1. make board in Node hidden, immutable?
  2. unify Node result and Util result and who wins
  3. nice pics in Mma code
 */

using System.Diagnostics;
using Lomont.Games.TicTacToe;

// things to run
Tests.Testing();

TreeTypes();
// MakeSmallTable(false);
// Z3Stats();
// TestPlayEngine();
// DumpMMA();
// TestTinyPlay();
// PlayTrees();
 PlayTinyNodes();
// PlayTblVsRandomContinued();
// DumpNodes();

return;

// dump tree to mathematica
void DumpMma()
{
    var gen = new GenerateTree();
    gen.Generate(0); // full tree
    gen.ScoreGraph(true);
    var edgesFull = WalkMma(gen.startNode);
    var edgesWritten = 0;
    Func<Misc.Edge, bool> pred = e =>
    {
        if (!Corner(e.StartHash) || !Corner(e.EndHash))
            return false;
        var pieces = Util.Dehash(e.StartHash);
        if (Util.Depth(pieces) < 3)
        {
            edgesWritten++;
            return true;
        }

        return false;
    };
    Misc.DumpMathematicaGraph("full_mma.txt", edgesFull, pred);
    Console.WriteLine($"MMA full {edgesFull.Count} edges, {edgesWritten} written");


    gen.Generate(2); // symm tree
    gen.ScoreGraph(true);
    var edgesSymm= WalkMma(gen.startNode);
    edgesWritten = 0;

    Misc.DumpMathematicaGraph("symm_mma.txt", edgesSymm, pred);
    Console.WriteLine($"MMA full {edgesFull.Count} edges, {edgesWritten} written");

    bool Corner(int hash)
    {
        var pieces = Util.Dehash(hash);
        // upper left 4 only
        return !(pieces[2] != 0 || pieces[5] != 0 || pieces[6] != 0 || pieces[7] != 0 || pieces[8] != 0);
    }

    List<Misc.Edge> WalkMma(Node startNode)
    {
        List<Misc.Edge> edges = new();
        Util.Recurse(startNode, (p, c, m) => {
            Trace.Assert(Util.Hash(c.board) != 0);
            edges.Add(new Misc.Edge(Util.Hash(p.board), m, Util.Hash(c.board), -1));
            return true;
        });
        return edges;
    }
}

void DumpNodes()
{
    var gen = new GenerateTree();
    gen.Generate(2); // symm tree
    gen.ScoreGraph(true);
    foreach (var k in gen.nodes.Keys.OrderBy(v => v))
    {
        var n = gen.nodes[k];
        var bm = n.bestMoves.Any() ? n.bestMoves[0] : -1;
        Console.Write($"{{{Util.MinHash(n.board).minHash},{bm},-1}},");
    }
}

// test the tiny player
void TestTinyPlay()
{
    var ph = new TinyPlay();
    ph.Play();

   // Play.PlayGame(new RandomPlayer(), new TinyPlay(), 1000, false);
   // Play.PlayGame(new TinyPlay(), new RandomPlayer(), 1000, false);
   // Play.PlayGame(new TinyPlay(), new TinyPlay(), 1000, false);
}

// check tree types
void TreeTypes()
{
    /*
Full tree: 549946 nodes examined, 549946 retained, reachable nodes 549946=549946, leaf count 255168
Scoring graph:
   Pass: 1->1 updated 141120 unscored 153658
   Pass: 1->2 updated 89424 unscored 64234
   Pass: 2->6 updated 46944 unscored 17290
   Pass: 6->16 updated 13680 unscored 3610
   Pass: 16->60 updated 3024 unscored 586
   Pass: 60->216 updated 504 unscored 82
   Pass: 216->1008 updated 72 unscored 10
   Pass: 1008->5184 updated 9 unscored 1
   Pass: 5184->46080 updated 1 unscored 0
   Pass: 46080->46080 updated 0 unscored 0
   Player 1 wins 131184(51.41%), Player 2 wins 77904(30.53%), draws 46080(18.06%)
Largest node is 19560
Hashed nodes: 16168 nodes examined, 5478 retained, reachable nodes 549946=549946, leaf count 255168
Scoring graph:
   Pass: 1->864 updated 1695 unscored 2825
   Pass: 864->864 updated 1458 unscored 1367
   Pass: 864->864 updated 835 unscored 532
   Pass: 864->1008 updated 348 unscored 184
   Pass: 1008->5184 updated 119 unscored 65
   Pass: 5184->5184 updated 44 unscored 21
   Pass: 5184->5184 updated 15 unscored 6
   Pass: 5184->5184 updated 5 unscored 1
   Pass: 5184->46080 updated 1 unscored 0
   Pass: 46080->46080 updated 0 unscored 0
   Player 1 wins 131184(51.41%), Player 2 wins 77904(30.53%), draws 46080(18.06%)
Largest node is 19560
Reduced symmetries: 2271 nodes examined, 765 retained, reachable nodes 549946=549946, leaf count 255168
Scoring graph:
   Pass: 1->6 updated 168 unscored 459
   Pass: 6->36 updated 189 unscored 270
   Pass: 36->72 updated 142 unscored 128
   Pass: 72->216 updated 79 unscored 49
   Pass: 216->1008 updated 32 unscored 17
   Pass: 1008->1008 updated 10 unscored 7
   Pass: 1008->4608 updated 4 unscored 3
   Pass: 4608->5184 updated 2 unscored 1
   Pass: 5184->46080 updated 1 unscored 0
   Pass: 46080->46080 updated 0 unscored 0
   Player 1 wins 131184(51.41%), Player 2 wins 77904(30.53%), draws 46080(18.06%)     
 Largest node is 17141
    */
    var gen = new GenerateTree();
    gen.Generate(0); // full tree
    gen.ScoreGraph(true);
    Console.WriteLine($"Largest node is {gen.nodes.Max(pair=>Util.Hash(pair.Value.board))}");

    gen.Generate(1); // hashed tree
    gen.ScoreGraph(true);
    Console.WriteLine($"Largest node is {gen.nodes.Max(pair => Util.Hash(pair.Value.board))}");

    // reduce by symmetry
    gen.Generate(2);
    gen.ScoreGraph(true);
    Console.WriteLine($"Largest node is {gen.nodes.Max(pair => Util.Hash(pair.Value.board))}");
}

void TestPlayEngine()
{
    Console.Clear();
    //Play.PlayGame(new ConsolePlayer(), new ConsolePlayer(), 1000, false);
    Play.PlayGame(new RandomPlayer(), new RandomPlayer(), 1000, false);

    // todo - test
    // var ap = new AllPosPlayer();


}

(List<TableEntry> bestRobot, List<TableEntry> bestHuman, List<TableEntry> bestBoth) MakeMinimal()
{
    var gen = new GenerateTree();

    // reduce by symmetry
    gen.Generate(2);
    gen.ScoreGraph(false);

    var startNode = gen.startNode;
    var z3 = new SolverZ3();
    var (bestRobot, bestHuman, bestBoth) = z3.Solve(startNode!);
    var hs = new HashSet<int>();
    hs.UnionWith(bestRobot.Select(t => t.MinHash));
    hs.UnionWith(bestHuman.Select(t => t.MinHash));
    Console.WriteLine(
        $"best min robot 1st {bestRobot.Count}, human 1st {bestHuman.Count}, both {bestBoth.Count}, union {hs.Count}");

    List<TableEntry> bestBoth2 = new();
    bestBoth2.AddRange(bestHuman);
    bestBoth2.AddRange(bestRobot);
    Console.WriteLine($"Best table sizes: computer 1st {bestRobot.Count}, human 1st {bestHuman.Count}, either 1st {bestBoth2.Count}");

    return (bestRobot, bestHuman, bestBoth2);
}

void Z3Stats()
{
    /*
Full tree: 549946 nodes examined, 549946 retained, reachable nodes 549946=549946, leaf count 255168
Z3 solver on symmetric tree
Z3: human 1st False, computer 1st True, start pos 0, skip levels False => 352 verts, 0 edges: Optimal 56 verts, 0 edges
Z3: human 1st False, computer 1st True, start pos 1, skip levels False => 408 verts, 0 edges: Optimal 94 verts, 0 edges
Z3: human 1st False, computer 1st True, start pos 4, skip levels False => 201 verts, 0 edges: Optimal 41 verts, 0 edges
Z3: human 1st True, computer 1st False, start pos -1, skip levels False => 282 verts, 0 edges: Optimal 127 verts, 0 edges


Z3: human 1st False, computer 1st True, start pos 0, skip levels True => 195 verts, 344 edges: Optimal 30 verts, 30 edges
Z3: human 1st False, computer 1st True, start pos 1, skip levels True => 226 verts, 400 edges: Optimal 51 verts, 51 edges
Z3: human 1st False, computer 1st True, start pos 4, skip levels True => 95 verts, 201 edges: Optimal 22 verts, 22 edges
Z3: human 1st True, computer 1st False, start pos -1, skip levels True => 158 verts, 248 edges: Optimal 75 verts, 74 edges

Hashed nodes: 16168 nodes examined, 5478 retained, reachable nodes 549946=549946, leaf count 255168
Z3 solver on position hash tree
Z3: human 1st False, computer 1st True, start pos 0, skip levels False => 352 verts, 0 edges: Optimal 56 verts, 0 edges
Z3: human 1st False, computer 1st True, start pos 0, skip levels True => 195 verts, 344 edges: Optimal 30 verts, 30 edges
Z3: human 1st False, computer 1st True, start pos 1, skip levels False => 408 verts, 0 edges: Optimal 94 verts, 0 edges
Z3: human 1st False, computer 1st True, start pos 1, skip levels True => 226 verts, 400 edges: Optimal 51 verts, 51 edges
Z3: human 1st False, computer 1st True, start pos 4, skip levels False => 201 verts, 0 edges: Optimal 41 verts, 0 edges
Z3: human 1st False, computer 1st True, start pos 4, skip levels True => 95 verts, 201 edges: Optimal 22 verts, 22 edges
Z3: human 1st True, computer 1st False, start pos -1, skip levels False => 282 verts, 0 edges: Optimal 127 verts, 0 edges
Z3: human 1st True, computer 1st False, start pos -1, skip levels True => 158 verts, 248 edges: Optimal 75 verts, 74 edges     */

    var gen = new GenerateTree();

    bool dumpNodes = false;

    // reduce by symmetry hash
    gen.Generate(0);
    gen.ScoreGraph(false);

    Console.WriteLine("Z3 solver on symmetric tree");
    var z3sym = new SolverZ3();
    z3sym.MakeStats(gen.startNode!,dumpNodes);

    return; // below same
    // reduce by position hash
    gen.Generate(1);
    gen.ScoreGraph(false);

    Console.WriteLine("Z3 solver on position hash tree");
    var z3pos = new SolverZ3();
    z3pos.MakeStats(gen.startNode!);
}

void MakeSmallTable(bool makeCode = true)
{
    var (bestRobot, bestHuman, bestBoth) = MakeMinimal();

    Console.WriteLine("Best tbl: ");
    foreach (var e in bestBoth.OrderBy(b => b.MinHash))
    {
        var pieces = Util.Dehash(e.MinHash);
        pieces[e.BestMove] = Util.ToMove(pieces);
        var result = Util.Outcome(pieces);
        var e2 = e with { Result = result };
        if (makeCode)
        {
            Console.WriteLine($"if (minHash == {e2.MinHash}) {{move = {e2.BestMove}; result = {e2.Result}; return; }}");
        }
        else
        {
            Console.Write($"{{{e2.MinHash},{e2.BestMove},{e2.Result}}},");
        }
    }
    Console.WriteLine();
}

// play each tree type
void PlayTrees()
{
    /*
Hashed nodes: 16168 nodes examined, 5478 retained, reachable nodes 549946=549946, leaf count 255168
FINAL: Score: Hashed tree 96844, Random Player 0, draws 3156
FINAL: Score: Random Player 0, Hashed tree 77579, draws 22421
FINAL: Score: Hashed tree 0, Hashed tree 0, draws 100000
Reduced symmetries: 2271 nodes examined, 765 retained, reachable nodes 549946=549946, leaf count 255168
FINAL: Score: Symmetry tree 96772, Random Player 0, draws 3228
FINAL: Score: Random Player 0, Symmetry tree 77524, draws 22476
FINAL: Score: Symmetry tree 0, Symmetry tree 0, draws 100000     
     */
    var gen = new GenerateTree();

    gen.Generate(1);
    gen.ScoreGraph(false);

    int size = 100000;
    var tp1 = new TreePlayer(gen.nodes, "Hashed tree");
    Play.PlayGame(tp1, new RandomPlayer(), size, false);
    Play.PlayGame(new RandomPlayer(), tp1, size, false);
    Play.PlayGame(tp1, tp1, size, false);


    gen.Generate(2);
    gen.ScoreGraph(false);
    tp1 = new TreePlayer(gen.nodes, "Symmetry tree");
    Play.PlayGame(tp1, new RandomPlayer(), size, false);
    Play.PlayGame(new RandomPlayer(), tp1, size, false);
    Play.PlayGame(tp1, tp1, size, false);
}

// play tiny nodes against hashed tree
void PlayTinyNodes()
{
    var (bestRobot, bestHuman, bestBoth) = MakeMinimal();
    var rbp = new TablePlayer(bestRobot,"Computer First Table Player");
    var hbp = new TablePlayer(bestHuman,"Robot First Table Player");
    var dbp = new TablePlayer(bestBoth, "Both Table Player");

    var gen = new GenerateTree();
    gen.Generate(1);
    gen.ScoreGraph(false);
    var tp1 = new TreePlayer(gen.nodes, "Hash tree player");
    gen.Generate(2);
    gen.ScoreGraph(false);
    var tp2 = new TreePlayer(gen.nodes, "Symmetry tree player");

    // test full tree vs random, and merge tree both sides vs random
    Play.PlayGame(tp1, new RandomPlayer(), 100000, false); // 
    Play.PlayGame(tp2, new RandomPlayer(), 100000, false); // 
    Play.PlayGame(rbp, new RandomPlayer(), 100000, false); // 
    Play.PlayGame(dbp, new RandomPlayer(), 100000, false); // 
    Play.PlayGame(new RandomPlayer(), tp1, 100000, false); // 
    Play.PlayGame(new RandomPlayer(), tp2, 100000, false); // 
    Play.PlayGame(new RandomPlayer(), hbp, 100000, false); // 
    Play.PlayGame(new RandomPlayer(), dbp, 100000, false); // 
    Play.PlayGame(tp1, dbp, 100000, false);                // 
    Play.PlayGame(dbp, tp1, 100000, false);                // 

    /*
    FINAL: Score: Hash tree player 96844(96.84%), Random Player 0(0.00%), draws 3156(3.16%)
    FINAL: Score: Symmetry tree player 96772(96.77%), Random Player 0(0.00%), draws 3228(3.23%)
    FINAL: Score: Computer First Table Player 95777(95.78%), Random Player 0(0.00%), draws 4223(4.22%)
    FINAL: Score: Both Table Player 95777(95.78%), Random Player 0(0.00%), draws 4223(4.22%)
    FINAL: Score: Random Player 0(0.00%), Hash tree player 77579(77.58%), draws 22421(22.42%)
    FINAL: Score: Random Player 0(0.00%), Symmetry tree player 77524(77.52%), draws 22476(22.48%)
    FINAL: Score: Random Player 0(0.00%), Robot First Table Player 85070(85.07%), draws 14930(14.93%)
    FINAL: Score: Random Player 0(0.00%), Both Table Player 85070(85.07%), draws 14930(14.93%)
    FINAL: Score: Hash tree player 0(0.00%), Both Table Player 0(0.00%), draws 100000(100.00%)
    FINAL: Score: Both Table Player 0(0.00%), Hash tree player 0(0.00%), draws 100000(100.00%)
     */

}



var gen = new GenerateTree();

gen.Generate(1);
gen.ScoreGraph(true);
var tp1 = new TreePlayer(gen.nodes);

// reduce by symmetry
gen.Generate(2);
gen.ScoreGraph(true);
var tp2 = new TreePlayer(gen.nodes);
var nodes = gen.nodes;


// hash board:
//gen.Generate(1);
//gen.ScoreGraph(true);

//Draw1.PosBrowser(gen.nodes);

//Play.PlayGame(new AllPosPlayer(), new RandomPlayer(),1000);

//Play.PlayGame(new TreePlayer(nodes), ap,1000);
//Console.WriteLine(ap.seen.Count);

//var tp1 = new TreePlayer(nodes);
//var tp2 = new TreePlayer(nodes);
//Play.PlayGame(tp1,tp2,1000);


//return;

// DumpGraph(startNode);


//var robotTree = IntsToTree(bestRobot, nodes);
//var humanTree = IntsToTree(bestHuman, nodes);
//var dualTree = IntsToTree(hs.ToList(), nodes);

//robotTree = FilterDepth(robotTree,false);
//humanTree = FilterDepth(humanTree,true);

//return;

void CountBest()
{
    /*
    Full tree: 549946 nodes examined, 549946 retained, reachable nodes 549946=549946, leaf count 255168
    BEST P1:0(0.00%) P2:5856(62.03%) draws:3584(37.97%)
    Full tree: 549946 nodes examined, 549946 retained, reachable nodes 549946=549946, leaf count 255168
    BEST P1:27456(88.45%) P2:0(0.00%) draws:3584(11.55%)
     */
    CountBestVsRand(true);
    CountBestVsRand(false);



    // count best versus uniform rand via recursion
    // best is over all best choices
    // unif is over all moves
    // done on full tree, each game unique leaf
    void CountBestVsRand(bool bestFirst)
    {
        var gen = new GenerateTree();
        // methods
        gen.Generate(0); // full tree
        gen.ScoreGraph(false);

        int wins1 = 0, draws = 0, wins2 = 0;

        Recurse(gen.startNode);
        var total = wins1 + wins2 + draws;
        Console.WriteLine($"BEST P1:{wins1}({Util.Pct(wins1,total)}) P2:{wins2}({Util.Pct(wins2,total)}) draws:{draws}({Util.Pct(draws,total)})");

        void Recurse(Node p)
        {
            if (p.LeafCount == 1)
            { // result
                switch (p.score)
                {
                    case -1:
                        wins2++;
                        break;
                    case 0:
                        draws++;
                        break;
                    case 1:
                        wins1++;
                        break;
                    default:
                        throw new Exception("ERROR! Invalid node");
                }
                return;
            }

            var depth = Util.Depth(p.board);
            var isOdd = (depth & 1) == 1;
            if (bestFirst ^ !isOdd)
            { // best moves
                Trace.Assert(p.bestMoves.Count > 0);
                foreach (var bm in p.bestMoves)
                {
                    Recurse(p.children[bm]);
                }
            }
            else
            {
                for (var k = 0; k < 9; ++k)
                {
                    var ch = p.children[k];
                    if (ch == null)
                        continue;

                    Recurse(ch);
                }
            }
        }


    }
    return;
}

// infinite tiny table testing
void PlayTblVsRandomContinued()
{
    var (_,_,both) = MakeMinimal();
    var dbp = new TablePlayer(both, "Either 1st table");

    var rnd = new RandomPlayer(); // keep counter updating
    int wins1 = 0, wins2 = 0, draws = 0, games = 0;
    while (true)
    {
        int sz = 10000;
        int w1, w2, d;
        (w1, w2, d) = Play.PlayGame(dbp, rnd, sz, false);
        wins1 += w1;
        wins2 += w2;
        draws += d;
        (w2, w1, d) = Play.PlayGame(rnd, dbp, sz, false);
        wins1 += w1;
        wins2 += w2;
        draws += d;
        games += 2;
        if ((games % 100) == 0)
        {
            Console.WriteLine($"Tally: 1:{wins1}, 2:{wins2}, d:{draws}");

        }
    }
}



Dictionary<int, Node> FilterDepth(Dictionary<int, Node> nodes, bool flip = false)
{
    var t2 = new Dictionary<int, Node>();
    foreach (var pos in nodes)
    {
        var isEven = (Util.Depth(pos.Value.board) & 1) == 0;
        if (isEven^flip)
        {
            pos.Value.children = new Node[9];
            t2.Add(pos.Key, pos.Value);
        }
    }
    return t2;
}


Dictionary<int,Node> IntsToTree(List<int> boardList, Dictionary<int, Node> dictionary)
{
    var ans = new Dictionary<int, Node>();

    foreach (var hash in boardList)
    {
        // new board with single move
        var nb = new Node();
        nb.board = Util.Dehash(hash);

        ans.Add(hash, nb);
    }

    foreach (var (hash,nb) in ans)
    {
        // old board
        var ob = dictionary[hash];

        // fill in old board (mostly)
        nb.score = ob.score;
        nb.wins1 = ob.wins1; // these not from this graph...
        nb.wins2 = ob.wins2;
        nb.draws = ob.draws;

        foreach (var bm in ob.bestMoves)
        {
            // see if child in tree
            var ch = ob.children[bm];
            var chHash = Util.Hash(ch!.board);
            if (ans.ContainsKey(chHash))
            {
                nb.bestMoves.Add(bm);
            }
        }

        Trace.Assert(ob.bestMoves.Count == 0 || ob.bestMoves.Count > 0);

//        nb.bestMoves;
//        nb.children;


    }

    var evenDepth = ans.Count(b => (Util.Depth(b.Value.board) & 1) == 0);
    var evenDepthLeaf = ans.Count(b => (Util.Depth(b.Value.board) & 1) == 0 && b.Value.LeafCount==1);
    var oddDepth = ans.Count(b => (Util.Depth(b.Value.board) & 1) != 0);
    var oddDepthLeaf = ans.Count(b => (Util.Depth(b.Value.board) & 1) != 0 && b.Value.LeafCount == 1);
    Console.WriteLine($"ans {ans.Count} even {evenDepth} (leaf {evenDepthLeaf}) odd {oddDepth} (leaf {oddDepthLeaf}) ");

    return ans;
}

record TableEntry(int MinHash, int BestMove, int Result);
