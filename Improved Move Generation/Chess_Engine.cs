using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using Microsoft.Xna.Framework.Graphics;
using Move_Generation;

namespace Sly_Scorpion
{
    internal class Chess_Engine
    {
        private static readonly float MaterialWeight = 1.073f;
        private static readonly float PositionWeight = 0.011f;

        public bool otherTransposition = true;
        public bool alphabeta = false;
        public int transUse = 0;

        private Random random = new(69420);

        public struct TranspositionEntry
        {
            public int depth;
            public float evaluation;

            public TranspositionEntry(int depth, float evaluation)
            {
                this.depth = depth;
                this.evaluation = evaluation;
            }
        }

        public enum NodeType { Exact, LowerBound, UpperBound }

        private Dictionary<ulong, TranspositionEntry> _transpositionTable = new Dictionary<ulong, TranspositionEntry>();

        public static readonly int[][] MateMap =
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

        private static readonly int[][][] PieceLocationValues = new int[][][]
        {
            new int[][]
            {
                new int[] { 0, 0, 0, 0, 0, 0, 0, 0 },
                new int[] { 0, 0, 0, 0, 0, 0, 0, 0 },
                new int[] { 0, 0, 0, 0, 0, 0, 0, 0 },
                new int[] { 0, 0, 0, 0, 0, 0, 0, 0 },
                new int[] { 0, 0, 0, 0, 0, 0, 0, 0 },
                new int[] { 0, 0, 0, 0, 0, 0, 0, 0 },
                new int[] { 0, 0, 0, 0, 0, 0, 0, 0 },
                new int[] { 0, 0, 0, 0, 0, 0, 0, 0 },
            },
            
            new int[][] 
            {
                new int[] {0,  0,  0,  0,  0,  0,  0,  0 },
                new int[] {5, 10, 10, 10, 10, 10, 10,  5 },
                new int[] {-5,  0,  0,  0,  0,  0,  0, -5 },
                new int[] {-5,  0,  0,  0,  0,  0,  0, -5 },
                new int[] {-5,  0,  0,  0,  0,  0,  0, -5 },
                new int[] {-5,  0,  0,  0,  0,  0,  0, -5 },
                new int[] {-5,  0,  0,  0,  0,  0,  0, -5 },
                new int[] { 0,  0,  0,  5,  5,  0,  0,  0 }
            },

            new int[][] 
            {
                new int[] {-50,-40,-30,-30,-30,-30,-40,-50 },
                new int[] {-40,-20,  0,  0,  0,  0,-20,-40 },
                new int[] {-30,  0, 10, 15, 15, 10,  0,-30 },
                new int[] {-30,  5, 15, 20, 20, 15,  5,-30 },
                new int[] {-30,  0, 15, 20, 20, 15,  0,-30 },
                new int[] {-30,  5, 10, 15, 15, 10,  5,-30 },
                new int[] {-40,-20,  0,  5,  5,  0,-20,-40 },
                new int[] {-50,-40,-30,-30,-30,-30,-40,-50 }
            },

            new int[][]
            {
                new int[] {-20,-10,-10,-10,-10,-10,-10,-20 },
                new int[] {-10,  0,  0,  0,  0,  0,  0,-10 },
                new int[] {-10,  0,  5, 10, 10,  5,  0,-10 },
                new int[] {-10,  5,  5, 10, 10,  5,  5,-10 },
                new int[] {-10,  0, 10, 10, 10, 10,  0,-10 },
                new int[] {-10, 10, 10, 10, 10, 10, 10,-10 },
                new int[] {-10,  5,  0,  0,  0,  0,  5,-10 },
                new int[] {-20,-10,-10,-10,-10,-10,-10,-20 }
            },

            new int[][]
            {
                new int[] {-20,-10,-10, -5, -5,-10,-10,-20 },
                new int[] {-10,  0,  0,  0,  0,  0,  0,-10 },
                new int[] {-10,  0,  5,  5,  5,  5,  0,-10 },
                new int[] { -5,  0,  5,  5,  5,  5,  0, -5 },
                new int[] {  0,  0,  5,  5,  5,  5,  0, -5 },
                new int[] {-10,  5,  5,  5,  5,  5,  0,-10 },
                new int[] {-10,  0,  5,  0,  0,  0,  0,-10 },
                new int[] {-20,-10,-10, -5, -5,-10,-10,-20 }
            },

            new int[][]
            {
                new int[] { -70, -70, -70, -70, -70, -70, -70, -70 },
                new int[] { -70, -70, -70, -70, -70, -70, -70, -70 },
                new int[] { -60, -60, -60, -60, -60, -60, -60, -60 },
                new int[] { -50, -50, -50, -50, -50, -50, -50, -50 },
                new int[] { -30, -50, -50, -50, -50, -50, -50, -30 },
                new int[] { -10, -20, -30, -40,-40, -30, -20, -10 },
                new int[] { 10, 5, 0, -30, -30, 0, 5, 10 },
                new int[] { 20, 30, 10, -20, -20, 10, 30, 20 }
            }
        };

        private static readonly float[] PieceValues = new float[] { 1, 5, 3, 3, 9, 0 };

        private Move _bestMove;
        private int _maxDepth;

        private Chess _board;

        public Chess_Engine(Chess board)
        {
            Zobrist.InitializeZobristKeys();
            _board = board;
        }

        public float BaseHeuristic(Chess board)
        {
            float ret = 0;

            int totalMaterial = 0;

            for (int i = 0; i < 6; i++)
            {
                ulong whitePieces = board.whitePieceBitboards[i];
                ulong blackPieces = board.blackPieceBitboards[i];

                float whiteValues = PieceValues[i] * BitOperations.PopCount(whitePieces);
                float blackValues = PieceValues[i] * BitOperations.PopCount(blackPieces);

                ret += whiteValues;
                ret -= blackValues;
                totalMaterial += (int)(whiteValues + blackValues);
                while (whitePieces != 0)
                {
                    int index = BitOperations.TrailingZeroCount(whitePieces);
                    int rank = index / 8;
                    int file = index % 8;
                    ret += PieceLocationValues[i][rank][file] * PositionWeight;
                    whitePieces &= whitePieces - 1;
                }

                while (blackPieces != 0)
                {
                    int index = BitOperations.TrailingZeroCount(blackPieces);
                    int rank = index / 8;
                    int file = index % 8;
                    ret -= PieceLocationValues[i][7-rank][file] * PositionWeight;
                    blackPieces &= blackPieces - 1;
                }
            }

            float endgameFactor = CalcEndgameFactor(totalMaterial);
            ret += EndgameMate(true) * endgameFactor * 0.11f;
            ret -= EndgameMate(false) * endgameFactor * 0.09f;

            //ret += ((float)random.NextDouble()-0.5f)/100f;

            return ret;
        }

        public Move BestMove(Chess board, int milliseconds)
        {
            _transpositionTable.Clear();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            int currentDepth = 1;
            _bestMove = null;


            while (stopwatch.ElapsedMilliseconds < milliseconds/3)
            {
                _maxDepth = currentDepth;
                _maxDepth = 4;
                transUse = 0;
                Minimax(_maxDepth, float.MinValue, float.MaxValue, board.whiteToMove);
                if (transUse != 0)
                    Debug.WriteLine(transUse);
                return _bestMove;
                //throw new Exception();
                currentDepth++;
            }
            stopwatch.Stop();

            return _bestMove;
        }

        public float MoveOrderingEvaluation(Move move)
        {
            float eval = 0;

            bool color = (move.start & _board.whitePieces) != 0UL;
            int moveStartRow = (int)Math.Log2(move.start) / 8;
            int moveStartCol = (int)Math.Log2(move.start) % 8;
            int moveEndRow = (int)Math.Log2(move.end) / 8;
            int moveEndCol = (int)Math.Log2(move.end) % 8;

            int startType = Math.Abs(_board.PieceType(move.start)) - 1;
            int endType = Math.Abs(_board.PieceType(move.end)) - 1;

            if (color)
                eval += 0.5f * (-PieceLocationValues[startType][moveStartRow][moveStartCol] + PieceLocationValues[startType][moveEndRow][moveEndCol]);
            else
                eval += 0.5f * (-PieceLocationValues[startType][7-moveStartRow][moveStartCol] + PieceLocationValues[startType][7-moveEndRow][moveEndCol]);

            if (color)
                eval += 0.25f * (moveStartRow - moveEndRow);
            else
                eval -= 0.25f * (moveStartRow - moveEndRow);

            if (endType != -1)
                eval += 10 * PieceValues[endType] - PieceValues[startType];

            if ((move.end & _board.attackMask) != 0UL)
                eval -= 3f*(PieceValues[startType] - 2);

            return eval;
        }

        public IOrderedEnumerable<Move> OrderMoves(List<Move> moves)
        {
            IOrderedEnumerable<Move> SortedList = moves.OrderByDescending(o => MoveOrderingEvaluation(o));
            return SortedList;
        }

        public float Minimax(int depth, float alpha, float beta, bool max)
        {
            if (_board.ThreefoldRepetition())
                return 0;

            if (depth == 0)
                return BaseHeuristic(_board);

            float best;
            var moves = _board.AllLegalMoves(_board.whiteToMove);

            if (moves.Count == 0)
            {
                if (_board.KingChecked())
                    return max ? -100000 * (depth + 1) : 100000 * (depth + 1);
                return 0;
            }

            IOrderedEnumerable<Move> orderedMoves = OrderMoves(moves);

            best = max ? float.MinValue : float.MaxValue;

            foreach (Move move in orderedMoves)
            {
                _board.MakeMove(move.start, move.end);
                
                // Transposition
                ulong zobristHash = _board.zobristHashes[^1];
                float eval;
                if (otherTransposition && _transpositionTable.TryGetValue(zobristHash, out TranspositionEntry transposition) &&
                    transposition.depth >= depth)
                {
                    transUse++;
                    eval = _transpositionTable[zobristHash].evaluation;
                }
                else
                {
                    eval = Minimax(depth - 1, alpha, beta, !max);
                    if (_transpositionTable.TryGetValue(zobristHash, out TranspositionEntry wrongDepth))
                        _transpositionTable[zobristHash] = new TranspositionEntry(depth, eval);
                    else
                        _transpositionTable.Add(zobristHash, new TranspositionEntry(depth, eval));
                }
                // End transposition
                
                _board.UnMakeMove();

                if (max)
                {
                    if (eval > best && depth == _maxDepth)
                    {
                        _bestMove = new Move(move.start, move.end);
                    }

                    best = Math.Max(best, eval);
                    alpha = Math.Max(alpha, eval);
                }
                else
                {
                    if (eval < best && depth == _maxDepth)
                    {
                        _bestMove = new Move(move.start, move.end);
                    }

                    best = Math.Min(best, eval);
                    beta = Math.Min(beta, eval);
                }

                if (beta <= alpha && alphabeta)
                    break;
            }

            return best;
        }

        // Sigmoid function
        private float Sigmoid(float x, float offset, float coefficient, float adder)
        {
            return 1 / (1 + MathF.Pow(MathF.E, offset - x * coefficient)) + adder;
        }

        // Calculates the end game factor
        public float CalcEndgameFactor(int material)
        {
            return Sigmoid(78 - material, 10, 0.25f, 0);
        }

        // Calculates the end game mate
        public float EndgameMate(bool color)
        {
            float eval = 0;

            ulong kingUs =
                color ? _board.whitePieceBitboards[_board.king] : _board.blackPieceBitboards[_board.king];
            ulong kingOther =
                !color ? _board.whitePieceBitboards[_board.king] : _board.blackPieceBitboards[_board.king];

            // ***FIX THIS LATER***
            if (kingOther == 0)
                return 0;

            int indexUs = (int)Math.Log2(kingUs);
            int indexThem = (int)Math.Log2(kingOther);

            eval -= 1 * (Math.Abs(indexUs / 8 - indexThem / 8) + Math.Abs(indexUs % 8 - indexThem % 8));

            eval += 0.5f * MateMap[indexThem / 8][indexThem % 8];

            return eval;
        }
    }
}
