using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class SlyCoyoteTuning : SlyCoyote
{
    public static double materialWeightA = 1;
    public static double materialWeightB = 1.019342874650946;
    public static double locationWeightA = 0.006885803199543796;
    public static double locationWeightB = 0.006885803199543796;
    public static double pawnStructureWeightA = 0.17247488872550812;
    public static double pawnStructureWeightB = 0.17247488872550812;
    public static double kingSafetyWeightA = 0.25;
    public static double kingSafetyWeightB = 0.25;

    public static double originalMat = 1;
    public static double originalLoc = 0.006885803199543796;
    public static double originalPawn = 0.17247488872550812;
    public static double originalKing = 0;

    public static List<int[][]> movesInstance = new List<int[][]>();
    public static List<double> evaluations = new List<double>();

    public static void AvB(int testSize)
    {
        int winsA = 0;
        int winsB = 0;
        int draws = 0;
        for (int i = 0; i < testSize; i++)
        {
            double result = PlayGame();
            if (result > 0)
                winsA++;
            else if (result < 0)
                winsB++;
            else if (result == 0)
                draws++;
        }
        Debug.WriteLine("Wins for A: " + winsA);
        Debug.WriteLine("Wins for B: " + winsB);
        Debug.WriteLine("Draws: " + draws);
    }

    public static void PlayGames(int numberOfGames, int batchNumber)
    {
        Random rnd = new Random();
        int score = 0;
        for (int g = 1; g <= numberOfGames; g++)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            score = 0;
            for (int i = 0; i < batchNumber; i++)
            {
                double evaluation = PlayGame();
                if (evaluation > 0)
                    score += 1;
                else if (evaluation < 0)
                    score -= 1;
            }
            if (score < 0)
            {
                materialWeightA = materialWeightB;
                locationWeightA = locationWeightB;
                pawnStructureWeightA = pawnStructureWeightB;
                kingSafetyWeightA = kingSafetyWeightB;
            }
            else if (score > 0)
            {
                materialWeightB = materialWeightA;
                locationWeightB = locationWeightA;
                pawnStructureWeightB = pawnStructureWeightA;
                kingSafetyWeightB = kingSafetyWeightA;
            }

            materialWeightA += (rnd.NextDouble() * 0.1) - 0.05;
            //materialWeightB += (rnd.NextDouble() * 0.1) - 0.05;
            locationWeightA += (rnd.NextDouble() * 0.002) - 0.001;
            //locationWeightB += (rnd.NextDouble() * 0.002) - 0.001;
            pawnStructureWeightA += (rnd.NextDouble() * 0.01) - 0.005;
            //pawnStructureWeightB += (rnd.NextDouble() * 0.01) - 0.005;
            kingSafetyWeightA += (rnd.NextDouble() * 0.05) - 0.0025;
            //kingSafetyWeightB += (rnd.NextDouble() * 0.002) - 0.001;

            stopwatch.Stop();
            Debug.WriteLine("Batch " + g + " finished of " + numberOfGames + ". Score: " + score + ". Total time: " + stopwatch.ElapsedMilliseconds + " ms.");
        }
        Debug.WriteLine("\nTuning Finished.\n");
        Debug.WriteLine("Winning weights: ");
        Debug.WriteLine("materialWeight = " + materialWeightB);
        Debug.WriteLine("locationWeight = " + locationWeightB);
        Debug.WriteLine("pawnStructreWeight = " + pawnStructureWeightB);
        Debug.WriteLine("kingSafteyWeight = " + kingSafetyWeightB);
        materialWeightA = originalMat;
        locationWeightA = originalLoc;
        pawnStructureWeightA = originalPawn;
        kingSafetyWeightA = originalKing;
        int draws = 0;
        int wins = 0;
        int losses = 0;

        for (int i = 0; i < 100; i++)
        {
            double result = PlayGame();
            if (result > 0)
                losses++;
            else if (result < 0)
                wins++;
            else if (result == 0)
                draws++;
            else
                wins++;
        }
        Debug.WriteLine("\nRecord against original weights: Wins: " + wins + ", Losses: " + losses + ", Draws: " + draws);
    }

    static double PlayGame()
    {
        Chess_Game game = new Chess_Game();
        for (int i = 0; i < 75; i++)
        {
            int[][] move;
            if (game.color)
                move = GenMove(game, 3);
            else
                move = GenMove2(game, 3);
            if (move == null)
                break;
            game.Move(move[0], move[1]);
            if (game.Mate() != -1)
                break;
        }
        return SlyCoyote.Minimax(game, 5, double.MinValue, double.MaxValue);
    }




    static int[][] GenMove(Chess_Game game, int depth)
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
        MaxDepth = depth;
        Minimax(game, depth, double.MinValue, double.MaxValue);
        List<int[][]> plausible = new List<int[][]>();
        double best;
        if (game.color)
            best = evaluations.Max();
        else
            best = evaluations.Min();
        for (int i = 0; i < evaluations.Count; i++)
        {
            if (Math.Abs(evaluations.ElementAt(i) - best) == 0)
                plausible.Add(movesInstance.ElementAt(i));
        }
        evaluations.Clear();
        movesInstance.Clear();
        Random rnd = new Random();

        //return game.AllMoves(game.color)[rnd.Next(game.AllMoves(game.color).Count)];
        //return moves[evals.IndexOf(best)];
        if (plausible.Count > 0)
        {
            return plausible.ElementAt(rnd.Next(plausible.Count));
        }
        else
        {
            return null;
        }
    }
    static double Minimax(Chess_Game game, int depth, double alpha, double beta)
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
        moves = OrderMoves(moves, game);
        if (game.color)
        {
            double maxEval = double.MinValue;
            foreach (int[][] move in moves)
            {
                if (move == null) continue;
                if (depth != 1)
                    game.Move(move[0], move[1]);
                else
                    game.SimpMove(move[0], move[1]);
                double eval = Minimax(game, depth - 1, alpha, beta);
                if (depth != 1)
                    game.UnMove();
                else
                    game.SimpUnMove();
                if (depth == MaxDepth)
                {
                    movesInstance.Add(move);
                    evaluations.Add(eval);
                }
                maxEval = Math.Max(eval, maxEval);
                alpha = Math.Max(alpha, eval);
                if (beta <= alpha)
                    break;
            }
            return maxEval;
        }
        else
        {
            double maxEval = double.MaxValue;
            foreach (int[][] move in moves)
            {
                if (move == null) continue;
                if (depth != 1)
                    game.Move(move[0], move[1]);
                else
                    game.SimpMove(move[0], move[1]);
                double eval = Minimax(game, depth - 1, alpha, beta);
                if (depth != 1)
                    game.UnMove();
                else
                    game.SimpUnMove();
                if (depth == MaxDepth)
                {
                    movesInstance.Add(move);
                    evaluations.Add(eval);
                }
                maxEval = Math.Min(eval, maxEval);
                beta = Math.Min(beta, eval);
                if (beta <= alpha)
                    break;
            }
            return maxEval;
        }
    }
    static double EvalPositionBasic(Chess_Game game, bool mater)
    {
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
                    eval += locationWeightA * LocationEvaluation(game, new int[] { j, i });
                }
                else
                {
                    material -= val;
                    eval -= locationWeightA * LocationEvaluation(game, new int[] { j, i });
                }
                total += val;
            }
        }
        eval += materialWeightA * material;
        eval += pawnStructureWeightA * EvaluatePawnStructure(game, true);
        eval -= pawnStructureWeightA * EvaluatePawnStructure(game, false);
        if (game.ThreefoldRepetition())
            return 0;

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

    public static int LocationEvaluationB(Chess_Game game, int[] piece)
    {
        int type = game.board[piece[1]][piece[0]];
        int absType = Math.Abs(type);
        int y = (type < 0) ? 7 - piece[1] : piece[1];
        return locationLookupB[absType][y][piece[0]];
    }

    public static int[][] GenMove2(Chess_Game game, int depth)
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
        MaxDepth = depth;
        Minimax2(game, depth, double.MinValue, double.MaxValue);
        List<int[][]> plausible = new List<int[][]>();
        double best;
        if (game.color)
            best = evaluations.Max();
        else
            best = evaluations.Min();
        for (int i = 0; i < evaluations.Count; i++)
        {
            if (Math.Abs(evaluations.ElementAt(i) - best) == 0)
                plausible.Add(movesInstance.ElementAt(i));
        }
        Random rnd = new Random();
        evaluations.Clear();
        movesInstance.Clear();
        //return game.AllMoves(game.color)[rnd.Next(game.AllMoves(game.color).Count)];
        //return moves[evals.IndexOf(best)];
        if (plausible.Count > 0)
        {
            return plausible.ElementAt(rnd.Next(plausible.Count));
        }
        else
        {
            return null;
        }
    }
    static double Minimax2(Chess_Game game, int depth, double alpha, double beta)
    {
        if (depth == 0)
        {
            double eval = EvalPositionBasic2(game, false);
            return eval;
        }
        List<int[][]> moves = game.AllMoves(game.color);
        if (moves.Count == 0)
        {
            double eval = EvalPositionBasic2(game, true) * (depth + 1);
            return eval;
        }
        moves = OrderMoves(moves, game);
        if (game.color)
        {
            double maxEval = double.MinValue;
            foreach (int[][] move in moves)
            {
                if (move == null) continue;
                if (depth != 1)
                    game.Move(move[0], move[1]);
                else
                    game.SimpMove(move[0], move[1]);
                double eval = Minimax2(game, depth - 1, alpha, beta);
                if (depth != 1)
                    game.UnMove();
                else
                    game.SimpUnMove();
                if (depth == MaxDepth)
                {
                    movesInstance.Add(move);
                    evaluations.Add(eval);
                }
                maxEval = Math.Max(eval, maxEval);
                alpha = Math.Max(alpha, eval);
                if (beta <= alpha)
                    break;
            }
            return maxEval;
        }
        else
        {
            double maxEval = double.MaxValue;
            foreach (int[][] move in moves)
            {
                if (move == null) continue;
                if (depth != 1)
                    game.Move(move[0], move[1]);
                else
                    game.SimpMove(move[0], move[1]);
                double eval = Minimax2(game, depth - 1, alpha, beta);
                if (depth != 1)
                    game.UnMove();
                else
                    game.SimpUnMove();
                if (depth == MaxDepth)
                {
                    movesInstance.Add(move);
                    evaluations.Add(eval);
                }
                maxEval = Math.Min(eval, maxEval);
                beta = Math.Min(beta, eval);
                if (beta <= alpha)
                    break;
            }
            return maxEval;
        }
    }
    static double EvalPositionBasic2(Chess_Game game, bool mater)
    {
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
                    eval += locationWeightB * LocationEvaluationB(game, new int[] { j, i });
                }
                else
                {
                    material -= val;
                    eval -= locationWeightB * LocationEvaluationB(game, new int[] { j, i });
                }
                total += val;
            }
        }
        eval += materialWeightB * material;
        eval += pawnStructureWeightB * EvaluatePawnStructure(game, true);
        eval -= pawnStructureWeightB * EvaluatePawnStructure(game, false);
        if (game.ThreefoldRepetition())
            return 0;
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
}