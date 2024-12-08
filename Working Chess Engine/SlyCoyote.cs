using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class SlyCoyote
{
    public static List<string> openingBook = new List<string>();

    public static int[] lookUpValues = new int[] { 0, 1, 5, 3, 3, 0, 9 };
    public struct TranspositionEntry
    {
        public double Evaluation;
        public int Depth;
    }

    public static Dictionary<ulong, TranspositionEntry> transpositionTable = new Dictionary<ulong, TranspositionEntry>();
    public static int MaxDepth = 0;
    public static int[][] bestMove = null;
    public static int batchSize = 20;

    public static double materialWeight = 1.019342874650946;
    public static double locationWeight = 0.006885803199543796;
    public static double pawnStructureWeight = 0.17247488872550812;
    public static double mateTheoryWeight = 0.1;

    public static int[][] mateMap =
    {
        new int[] { 60, 55, 50, 45, 45, 50, 55, 60 },
        new int[] { 55, 35, 30, 30, 30, 30, 35, 55 },
        new int[] { 50, 30, 15, 10, 10, 15, 30, 50 },
        new int[] { 45, 30, 10, 0, 0, 10, 30, 45 },
        new int[] { 45, 30, 10, 0, 0, 10, 30, 45 },
        new int[] { 50, 30, 15, 10, 10, 15, 30, 50 },
        new int[] { 55, 35, 30, 30, 30, 30, 35, 55 },
        new int[] { 60, 55, 50, 45, 45, 50, 55, 60 },
    };
    public static int[][] kingMap =
    {
        new int[] { -70, -70, -70, -70, -70, -70, -70, -70 },
        new int[] { -70, -70, -70, -70, -70, -70, -70, -70 },
        new int[] { -60, -60, -60, -60, -60, -60, -60, -60 },
        new int[] { -50, -50, -50, -50, -50, -50, -50, -50 },
        new int[] { -30, -50, -50, -50, -50, -50, -50, -30 },
        new int[] { -10, -20, -30, -40,-40, -30, -20, -10 },
        new int[] { 10, 5, 0, -30, -30, 0, 5, 10 },
        new int[] { 20, 30, 10, -20, -20, 10, 30, 20 }
    };

    public static int[][] rookMap =
    {
         new int[] {0,  0,  0,  0,  0,  0,  0,  0 },
         new int[] {5, 10, 10, 10, 10, 10, 10,  5 },
         new int[] {-5,  0,  0,  0,  0,  0,  0, -5 },
         new int[] {-5,  0,  0,  0,  0,  0,  0, -5 },
         new int[] {-5,  0,  0,  0,  0,  0,  0, -5 },
         new int[] {-5,  0,  0,  0,  0,  0,  0, -5 },
         new int[] {-5,  0,  0,  0,  0,  0,  0, -5 },
         new int[] { 0,  0,  0,  5,  5,  0,  0,  0 }
    };
    public static int[][] knightMap =
    {
        new int[] {-50,-40,-30,-30,-30,-30,-40,-50 },
        new int[] {-40,-20,  0,  0,  0,  0,-20,-40 },
        new int[] {-30,  0, 10, 15, 15, 10,  0,-30 },
        new int[] {-30,  5, 15, 20, 20, 15,  5,-30 },
        new int[] {-30,  0, 15, 20, 20, 15,  0,-30 },
        new int[] {-30,  5, 10, 15, 15, 10,  5,-30 },
        new int[] {-40,-20,  0,  5,  5,  0,-20,-40 },
        new int[] {-50,-40,-30,-30,-30,-30,-40,-50 }
    };
    public static int[][] bishopMap =
    {
        new int[] {-20,-10,-10,-10,-10,-10,-10,-20 },
        new int[] {-10,  0,  0,  0,  0,  0,  0,-10 },
        new int[] {-10,  0,  5, 10, 10,  5,  0,-10 },
        new int[] {-10,  5,  5, 10, 10,  5,  5,-10 },
        new int[] {-10,  0, 10, 10, 10, 10,  0,-10 },
        new int[] {-10, 10, 10, 10, 10, 10, 10,-10 },
        new int[] {-10,  5,  0,  0,  0,  0,  5,-10 },
        new int[] {-20,-10,-10,-10,-10,-10,-10,-20 }
    };
    public static int[][] queenMap =
    {
        new int[] {-20,-10,-10, -5, -5,-10,-10,-20 },
        new int[] {-10,  0,  0,  0,  0,  0,  0,-10 },
        new int[] {-10,  0,  5,  5,  5,  5,  0,-10 },
        new int[] { -5,  0,  5,  5,  5,  5,  0, -5 },
        new int[] {  0,  0,  5,  5,  5,  5,  0, -5 },
        new int[] {-10,  5,  5,  5,  5,  5,  0,-10 },
        new int[] {-10,  0,  5,  0,  0,  0,  0,-10 },
        new int[] {-20,-10,-10, -5, -5,-10,-10,-20 }
    };
    public static int[][] empty = new int[][] { new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 } };
    public static int[][][] locationLookup = new int[][][] { empty, empty, rookMap, knightMap, bishopMap, kingMap, queenMap };
    public static int[][][] locationLookupB = new int[][][] { empty, empty, rookMap, knightMap, bishopMap, kingMap, queenMap };
    public static int counter = 0;

    public static double OrderingEvaluation(Chess_Game game, int[][] move)
    {
        double eval = 0;
        int start = game.board[move[0][1]][move[0][0]];
        int end = game.board[move[1][1]][move[1][0]];
        if (start > 0)
        {
            if (start == 2)
                eval += rookMap[move[1][1]][move[1][0]] - rookMap[move[0][1]][move[0][0]];
            else if (start == 3)
                eval += knightMap[move[1][1]][move[1][0]] - knightMap[move[0][1]][move[0][0]];
            else if (start == 4)
                eval += bishopMap[move[1][1]][move[1][0]] - bishopMap[move[0][1]][move[0][0]];
            else if (start == 6)
                eval += queenMap[move[1][1]][move[1][0]] - queenMap[move[0][1]][move[0][0]];
        }
        else
        {
            if (start == -2)
                eval += rookMap[7 - move[1][1]][move[1][0]] - rookMap[7 - move[0][1]][move[0][0]];
            else if (start == -3)
                eval += knightMap[7 - move[1][1]][move[1][0]] - knightMap[7 - move[0][1]][move[0][0]];
            else if (start == -4)
                eval += bishopMap[7 - move[1][1]][move[1][0]] - bishopMap[7 - move[0][1]][move[0][0]];
            else if (start == -6)
                eval += queenMap[7 - move[1][1]][move[1][0]] - queenMap[7 - move[0][1]][move[0][0]];
        }
        eval /= 60;
        if (start > 0)
            eval += 0.5 * move[0][1] - move[1][1];
        else
            eval -= 0.5 * move[0][1] - move[1][1];
        if (end != 0)
        {
            eval += 10 * Value(end) - Value(start);
        }
        else if (game.blackPawnAttackedArr.Count > 0 || game.whitePawnAttackedArr.Count > 0)
        {
            if (game.color)
            {
                if (game.blackPawnAttackedArr.Last()[move[1][1]][move[1][0]] == 1)
                    eval -= 10 * (Value(start) - 1);
                else if (game.attackedBlackArr.Last()[move[1][1]][move[1][0]] == 1)
                {
                    eval -= 4 * Value(start) - 4.5;
                }
            }
            else
            {
                if (game.whitePawnAttackedArr.Last()[move[1][1]][move[1][0]] == 1)
                    eval -= 10 * (Value(start) - 1);
                else if (game.attackedWhiteArr.Last()[move[1][1]][move[1][0]] == 1)
                {
                    eval -= 4 * Value(start) - 4.5;
                }
            }
        }
        if (Math.Abs(start) == 1)
        {
            if (start > 0)
            {
                if (move[1][1] == 0)
                    eval += 50;
            }
            else
            {
                if (move[1][1] == 7)
                    eval += 50;
            }
        }
        return eval;
    }
    public static List<int[][]> OrderMoves(List<int[][]> moves, Chess_Game game)
    {
        List<int[][]> SortedList = moves.OrderByDescending(o => OrderingEvaluation(game, o)).ToList();
        return SortedList;
    }
    public static int NodesAtDepth(Chess_Game game, int depth)
    {
        counter = 0;
        RecursionNodesAtDepth(game, depth);
        return counter;
    }
    public static void RecursionNodesAtDepth(Chess_Game game, int depth)
    {
        if (depth == 0)
        {
            counter++;
            return;
        }
        List<int[][]> allMoves = game.AllMoves(game.color);
        if (allMoves.Count == 0)
        {
            counter++;
            return;
        }
        foreach (int[][] item in allMoves)
        {
            if (depth != 1)
                game.Move(item[0], item[1]);
            else
                game.SimpMove(item[0], item[1]);
            RecursionNodesAtDepth(game, depth - 1);
            if (depth != 1)
                game.UnMove();
            else
                game.SimpUnMove();
        }
    }
    public static int[][] GenMove(Chess_Game game, int remaining, int increment)
    {
        if (game.moveArr.Count < 21 && !game.loadedFromFEN)
        {
            string stringMoves = "[";
            for (int i = 0; i < game.moveArr.Count; i++)
            {
                stringMoves += "(" + game.moveArr[i][0][0] + ", " + game.moveArr[i][0][1] + ", " + game.moveArr[i][1][0] + ", " + game.moveArr[i][1][1] + ")";
                if (i != game.moveArr.Count - 1)
                    stringMoves += ", ";
            }
            List<string> possibilities = new List<string>();
            foreach (string line in openingBook)
            {
                if (line.Length <= stringMoves.Length)
                    break;
                string temp = line.Substring(0, stringMoves.Length);
                if (temp == stringMoves)
                {
                    possibilities.Add(line);
                }
            }
            if (possibilities.Count != 0)
            {
                Random rnd2 = new Random();
                string randLine = possibilities[rnd2.Next(possibilities.Count)];
                int moveNum = game.moveArr.Count;
                int count = 0;
                for (int i = 0; i < randLine.Length; i++)
                {
                    if (randLine[i] == '(')
                        count++;
                    if (count - 1 == moveNum)
                    {
                        return new int[][] { new int[] { randLine[i + 1] - '0', randLine[i + 4] - '0' }, new int[] { randLine[i + 7] - '0', randLine[i + 10] - '0' } };
                    }
                }
            }
        }
        int timeAlloted;
        if (increment != -1)
        {
            int movesRemaining = MovesLeft(game);
            timeAlloted = (int)(0.6*TimePerMove(remaining, increment, movesRemaining));
        }
        else
        {
            timeAlloted = 1000;
        }
        Debug.WriteLine("\nTime alloted: " + timeAlloted + ", " + increment);
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        for (int i = 4; i < 50; i++)
        {
            Stopwatch test = new Stopwatch();
            test.Start();
            MaxDepth = i;
            Minimax(game, i, double.MinValue, double.MaxValue);
            test.Stop();
            if (test.ElapsedMilliseconds * 5 > timeAlloted)
            {
                Debug.WriteLine("Looked at depth " + i);
                break;
            }
        }
        stopwatch.Stop();

        Debug.WriteLine("Time spent: " + stopwatch.ElapsedMilliseconds + " ms");
        return bestMove;
    }
    public static double Minimax(Chess_Game game, int depth, double alpha, double beta)
    {
        if (depth == 0)
        {
            double eval = EvalPositionBasic(game, false);
            return eval;
        }
        List<int[][]> moves = game.AllMoves(game.color);
        if (moves.Count == 0)
        {
            double eval = EvalPositionBasic(game, true) * (depth + 1);
            return eval;
        }
        if (game.ThreefoldRepetition())
            return 0;
        moves = OrderMoves(moves, game);

        List<List<int[][]>> batchedMoves = new List<List<int[][]>>();

        if (depth == MaxDepth)
        {
            int batched = 0;
            List<int[][]> temp = new List<int[][]>();
            for (int i = 0; i < moves.Count; i++)
            {
                temp.Add(moves[i]);
                batched++;
                if (batched == batchSize)
                {
                    batched = 0;
                    batchedMoves.Add(new List<int[][]>(temp));
                    temp.Clear();
                }
            }
            if (temp.Count != 0)
            {
                batchedMoves.Add(temp);
            }
        }
        if (game.color)
        {
            double maxEval = double.MinValue;
            if (depth != MaxDepth)
                for (int i = 0; i < moves.Count; i++)
                {
                    if (depth != 1)
                        game.Move(moves[i][0], moves[i][1]);
                    else
                        game.SimpMove(moves[i][0], moves[i][1]);
                    double eval = Minimax(game, depth - 1, alpha, beta);
                    if (depth != 1)
                        game.UnMove();
                    else
                        game.SimpUnMove();
                    if (depth == MaxDepth && eval > maxEval)
                    {
                        bestMove = moves[i];
                    }
                    maxEval = Math.Max(eval, maxEval);
                    alpha = Math.Max(alpha, eval);
                    if (beta <= alpha)
                        break;
                }
            else
            {
                for (int i = 0; i < batchedMoves.Count; i++)
                {
                    Task<double>[] tasks = new Task<double>[batchedMoves[i].Count];
                    for (int j = 0; j < batchedMoves[i].Count; j++)
                    {
                        Chess_Game temp = game.Copy();
                        temp.Move(batchedMoves[i][j][0], batchedMoves[i][j][1]);

                        tasks[j] = Task<double>.Factory.StartNew((values) =>
                        {
                            object[] array = (object[])values;
                            Chess_Game temp = (Chess_Game)array[0];
                            int depth = (int)array[1];
                            double alpha = (double)array[2];
                            double beta = (double)array[3];
                            return Minimax(temp, depth - 1, alpha, beta);
                        }, new object[] { temp, depth, alpha, beta });
                    }
                    Task.WaitAll(tasks);
                    List<double> evals = new List<double>();
                    foreach (var task in tasks)
                    {
                        evals.Add(task.Result);
                    }
                    double max = evals.Max();
                    int indexOf = evals.IndexOf(max);
                    alpha = Math.Max(alpha, max);
                    if (max > maxEval)
                    {
                        maxEval = max;
                        bestMove = batchedMoves[i][indexOf];
                    }
                    if (beta <= alpha)
                        break;
                }
            }
            return maxEval;
        }
        else
        {
            double maxEval = double.MaxValue;
            if (depth != MaxDepth)
                for (int i = 0; i < moves.Count; i++)
                {
                    if (depth != 1)
                        game.Move(moves[i][0], moves[i][1]);
                    else
                        game.SimpMove(moves[i][0], moves[i][1]);
                    double eval = Minimax(game, depth - 1, alpha, beta);
                    if (depth != 1)
                        game.UnMove();
                    else
                        game.SimpUnMove();
                    if (depth == MaxDepth && eval < maxEval)
                    {
                        bestMove = moves[i];
                    }
                    maxEval = Math.Min(eval, maxEval);
                    beta = Math.Min(beta, eval);
                    if (beta <= alpha)
                        break;
                }
            else
            {
                for (int i = 0; i < batchedMoves.Count; i++)
                {
                    Task<double>[] tasks = new Task<double>[batchedMoves[i].Count];
                    for (int j = 0; j < batchedMoves[i].Count; j++)
                    {
                        Chess_Game temp = game.Copy();
                        temp.Move(batchedMoves[i][j][0], batchedMoves[i][j][1]);

                        tasks[j] = Task<double>.Factory.StartNew((values) =>
                        {
                            object[] array = (object[])values;
                            Chess_Game temp = (Chess_Game)array[0];
                            int depth = (int)array[1];
                            double alpha = (double)array[2];
                            double beta = (double)array[3];
                            return Minimax(temp, depth - 1, alpha, beta);
                        }, new object[] { temp, depth, alpha, beta });
                    }
                    Task.WaitAll(tasks);
                    List<double> evals = new List<double>();
                    foreach (var task in tasks)
                    {
                        evals.Add(task.Result);
                    }
                    double max = evals.Min();
                    int indexOf = evals.IndexOf(max);
                    alpha = Math.Min(alpha, max);
                    if (max < maxEval)
                    {
                        maxEval = max;
                        bestMove = batchedMoves[i][indexOf];
                    }
                    if (beta <= alpha)
                        break;
                }
            }
            return maxEval;
        }
    }

    public static double EvalPositionBasic(Chess_Game game, bool mater)
    {
        if (game.ThreefoldRepetition())
            return 0;
        double eval = 0;
        double material = 0;
        int total = 0;
        for (int i = 0; i < game.board.Length; i++)
        {
            for (int j = 0; j < game.board.Length; j++)
            {
                int val = Value(game.board[i][j]);
                if (game.board[i][j] > 0)
                {
                    material += val;
                    eval += locationWeight * LocationEvaluation(game, new int[] { j, i });
                }
                else
                {
                    material -= val;
                    eval -= locationWeight * LocationEvaluation(game, new int[] { j, i });
                }
                total += val;
            }
        }
        eval += materialWeight * material;
        eval += pawnStructureWeight * EvaluatePawnStructure(game, true);
        eval -= pawnStructureWeight * EvaluatePawnStructure(game, false);
        double endgameFactor = CalcEndgameFactor(total);
        eval += EndgameMate(game, true) * endgameFactor * mateTheoryWeight;
        eval -= EndgameMate(game, false) * endgameFactor * mateTheoryWeight;

        if (mater)
        {
            int mate = game.Mate();
            if (game.color)
            {
                if (mate == 1)
                    return -100000;
                else if (mate == 0)
                    return 0;
            }
            else
            {
                if (mate == 1)
                {
                    return 100000;
                }
                else if (mate == 0)
                    return 0;
            }
        }
        return eval;
    }
    public static int[] CoordFromString(string pos)
    {
        List<string> letters = new List<string>();
        int[] ret = new int[2];
        letters.Add("a");
        letters.Add("b");
        letters.Add("c");
        letters.Add("d");
        letters.Add("e");
        letters.Add("f");
        letters.Add("g");
        letters.Add("h");
        ret[0] = letters.IndexOf(pos.Substring(0, 1));
        if (pos.Length == 2)
            ret[1] = 8 - int.Parse(pos.Substring(1));
        else
            ret[1] = 8 - int.Parse(pos[pos.Length - 2].ToString());
        return ret;
    }
    public static int Value(int piece)
    {
        piece = Math.Abs(piece);
        return lookUpValues[piece];
    }
    public static double EvaluatePawnStructure(Chess_Game game, bool color)
    {
        double score = 0;
        for (int i = 0; i < game.board.Length; i++)
        {
            for (int j = 0; j < game.board.Length; j++)
            {
                int piece = game.board[i][j];
                int absPiece = Math.Abs(piece);
                if (absPiece == 1 && piece > 0 == color)
                {
                    int temp;
                    if (i != 7)
                        temp = game.board[i + 1][j];
                    else
                        temp = -10;
                    if (temp != -10 && Math.Abs(temp) == 1 && temp < 0 == color)
                        score -= 1;

                    if (j != 7)
                        temp = game.board[i][j + 1];
                    else
                        temp = -10;
                    if (temp != -10 && Math.Abs(temp) == 1 && temp < 0 == color)
                        score += 0.4;

                    if (j != 7 && i != 7)
                        temp = game.board[i + 1][j + 1];
                    else
                        temp = -10;
                    if (temp != -10 && Math.Abs(temp) == 1 && temp < 0 == color)
                        score += 0.6;

                    if (j != 0 && i != 7)
                        temp = game.board[i + 1][j - 1];
                    else
                        temp = -10;
                    if (temp != -10 && Math.Abs(temp) == 1 && temp < 0 == color)
                        score += 0.6;

                    if (color)
                        score += 0.1 * (7 - i);
                    else
                        score += 0.1 * i;
                }
            }
        }

        return score;
    }
    private static double Sigmoid(double x, double offset, double coefficient, double adder)
    {
        return 1 / (1 + Math.Pow(Math.E, offset - x * coefficient)) + adder;
    }
    public static int LocationEvaluation(Chess_Game game, int[] piece)
    {
        int type = game.board[piece[1]][piece[0]];
        int absType = Math.Abs(type);
        int y = (type < 0) ? 7 - piece[1] : piece[1];
        return locationLookup[absType][y][piece[0]];
    }
    public static void LoadBook(string path)
    {
        var linesRead = File.ReadLines(path);

        foreach (string lineRead in linesRead)
        {
            openingBook.Add(lineRead);
        }
    }
    public static ulong GetBitboard(int[][] board)
    {
        ulong result = 0;
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                int piece = board[rank][file];
                if (piece == 0) continue; // skip empty squares

                int color = piece > 0 ? 0 : 1; // 0 for white, 1 for black
                piece = Math.Abs(piece) - 1; // convert to 0-based index

                int square = rank * 8 + file; // convert rank and file to square index
                ulong mask = 1UL << square; // create a mask for the square

                // use the correct piece type index
                int[] pieceTypes = { 0, 1, 2, 3, 4, 5, 6 };
                ulong pieceMask = mask << (color * 7 + pieceTypes[piece]);
                result |= pieceMask; // set the bit for the piece on the bitboard
            }
        }
        return result;
    }

    public static double CalcEndgameFactor(int material)
    {
        return Sigmoid(78 - material, 10, 0.25, 0);
    }
    public static double EndgameMate(Chess_Game game, bool color)
    {
        int[] kingCoordsUs = null;
        int[] kingCoordsOther = null;
        double eval = 0;

        for (int i = 0; i < game.board.Length; i++)
        {
            for (int j = 0; j < game.board.Length; j++)
            {
                if (Math.Abs(game.board[i][j]) == 5)
                {
                    if (game.board[i][j] > 0 == color)
                        kingCoordsUs = new int[] { j, i };
                    else
                        kingCoordsOther = new int[] { j, i };
                }
            }
        }
        eval -= 1 * (Math.Abs(kingCoordsUs[0] - kingCoordsOther[0]) + Math.Abs(kingCoordsUs[1] - kingCoordsOther[1]));

        eval += 0.5 * mateMap[kingCoordsOther[1]][kingCoordsOther[0]];
        return eval;
    }
    private static readonly ulong[,,] zobristTable = new ulong[8, 8, 12];

    public static void ZobristHash()
    {
        // Initialize the Zobrist table with random numbers
        Random rand = new Random();
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                for (int k = 0; k < 12; k++)
                {
                    zobristTable[i, j, k] = (ulong)rand.NextLong();
                }
            }
        }
    }
    public static ulong GetZobristHash(int[][] board)
    {
        // Initialize Zobrist hash value to 0
        ulong hash = 0;

        // Loop through the board and update the hash value using the Zobrist table
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                int piece = board[i][j];
                if (piece != 0)
                {
                    int index = Math.Abs(piece) - 1;
                    if (piece < 0)
                    {
                        index += 6;
                    }
                    hash ^= zobristTable[i, j, index];
                }
            }
        }

        return hash;
    }
    public static string IntToString(int n)
    {
        n = n / 1000;
        int seconds = n % 60;
        int minutes = n / 60;
        string secs = seconds.ToString();
        string mins = minutes.ToString();

        if (mins.Length == 1)
            mins = "0" + mins;
        if (secs.Length == 1)
            secs = "0" + secs;

        return mins + ":" + secs;
    }
    public static int MovesLeft(Chess_Game game)
    {
        int totalMaterial = 0;
        int totalMovesPreviously = game.moveArr.Count;

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                totalMaterial += Value(game.board[i][j]);
            }
        }
        return (int)Math.Round(totalMaterial / 1.5);
    }
    public static int TimePerMove(int remaining, int increment, int movesLeft)
    {
        return (remaining - movesLeft * increment) / movesLeft;
    }
}
