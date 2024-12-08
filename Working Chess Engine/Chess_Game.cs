using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class Chess_Game
{
    public const int blank = 0;
    public const int pawn = 1;
    public const int rook = 2;
    public const int knight = 3;
    public const int bishop = 4;
    public const int king = 5;
    public const int queen = 6;

    public int[][] board = new int[][] { new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8] };
    public int[][] attackedWhite = new int[][] { new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8] };
    public int[][] attackedBlack = new int[][] { new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8] };

    public bool color;
    public bool loadedFromFEN = false;
    public bool whiteKingSide = false;
    public bool whiteQueenSide = false;
    public bool blackKingSide = false;
    public bool blackQueenSide = false;

    public int perftCount = 0;

    public List<int[]> checkListWhite = new List<int[]>();
    public List<int[]> checkListBlack = new List<int[]>();
    public List<int[]> pinnedPiecesWhite = new List<int[]>();
    public List<int[]> pinnedPiecesBlack = new List<int[]>();

    public List<List<int[]>> checkListWhiteArr = new List<List<int[]>>();
    public List<List<int[]>> checkListBlackArr = new List<List<int[]>>();
    public List<List<int[]>> pinnedPiecesWhiteArr = new List<List<int[]>>();
    public List<List<int[]>> pinnedPiecesBlackArr = new List<List<int[]>>();
    public List<bool> enPassantArr = new List<bool>();
    public List<int[][]> attackedWhiteArr = new List<int[][]>();
    public List<int[][]> attackedBlackArr = new List<int[][]>();
    public List<int[][]> whitePawnAttackedArr = new List<int[][]>();
    public List<int[][]> blackPawnAttackedArr = new List<int[][]>();
    public List<int[]> promotionArr = new List<int[]>();
    public List<int> takenArr = new List<int>();
    public List<int[][]> moveArr = new List<int[][]>();
    public List<bool[]> castling = new List<bool[]>();
    public List<ulong> hashArr = new List<ulong>();

    public List<int[][]> savedMoves;

    public Chess_Game()
    {
        for (int i = 0; i < board.Length; i++)
        {
            for (int j = 0; j < board[i].Length; j++)
            {
                board[i][j] = blank;
            }
        }

        board[0][0] = -rook;
        board[0][1] = -knight;
        board[0][2] = -bishop;
        board[0][3] = -queen;
        board[0][4] = -king;
        board[0][5] = -bishop;
        board[0][6] = -knight;
        board[0][7] = -rook;
        for (int i = 0; i < 8; i++)
        {
            board[1][i] = -pawn;
        }

        board[7][0] = rook;
        board[7][1] = knight;
        board[7][2] = bishop;
        board[7][3] = queen;
        board[7][4] = king;
        board[7][5] = bishop;
        board[7][6] = knight;
        board[7][7] = rook;
        for (int i = 0; i < 8; i++)
        {
            board[6][i] = pawn;
        }

        color = true;
        blackKingSide = true;
        whiteKingSide = true;
        blackQueenSide = true;
        whiteQueenSide = true;
    }
    public Chess_Game(string FEN)
    {
        loadedFromFEN = true;
        int lastIndex = FEN.IndexOf(' ');
        int x = 0;
        int y = 0;
        Clear(board);

        for (int i = 0; i < lastIndex + 1; i++)
        {
            if (Char.IsDigit(FEN[i]))
            {
                x += int.Parse(FEN[i].ToString());
            }
            else if (FEN[i] == '/')
            {
                x = 0;
                y++;
            }
            else
            {
                if (FEN[i] == 'p')
                    board[y][x] = -pawn;
                else if (FEN[i] == 'P')
                    board[y][x] = pawn;
                else if (FEN[i] == 'r')
                    board[y][x] = -rook;
                else if (FEN[i] == 'R')
                    board[y][x] = rook;
                else if (FEN[i] == 'n')
                    board[y][x] = -knight;
                else if (FEN[i] == 'N')
                    board[y][x] = knight;
                else if (FEN[i] == 'b')
                    board[y][x] = -bishop;
                else if (FEN[i] == 'B')
                    board[y][x] = bishop;
                else if (FEN[i] == 'k')
                    board[y][x] = -king;
                else if (FEN[i] == 'K')
                    board[y][x] = king;
                else if (FEN[i] == 'q')
                    board[y][x] = -queen;
                else if (FEN[i] == 'Q')
                    board[y][x] = queen;
                x++;
            }
        }
        if (FEN[lastIndex + 1] == 'w')
            color = true;
        else
            color = false;

        if (FEN[lastIndex + 3] == '-')
        {
            whiteKingSide = false;
            whiteQueenSide = false;
            blackKingSide = false;
            blackQueenSide = false;
        }
        else
        {
            for (int i = lastIndex + 3; i < lastIndex + 3 + 4; i++)
            {
                if (FEN[i] == 'K')
                    whiteKingSide = true;
                else if (FEN[i] == 'Q')
                    whiteQueenSide = true;
                else if (FEN[i] == 'k')
                    blackKingSide = true;
                else if (FEN[i] == 'q')
                    blackQueenSide = true;
                if (FEN[i] == ' ')
                    break;
            }
        }
        castling.Add(new bool[] { whiteKingSide, whiteQueenSide, blackKingSide, blackQueenSide });
        SetupBoard();
    }
    public int Perft(int depth)
    {
        var watch = Stopwatch.StartNew();
        RepeatingPerft(depth, color);
        watch.Stop();
        Debug.WriteLine("It took " + watch.ElapsedMilliseconds + " ms to find all possible positions up to depth " + depth + ", of which there are " + perftCount + ".");
        return perftCount;
    }
    private void RepeatingPerft(int depth, bool color)
    {
        if (depth == 0)
        {
            perftCount++;
            return;
        }
        List<int[][]> allMoves = new List<int[][]>();
        try
        {
            allMoves = AllMoves(color);
        }
        catch
        {
            Debug.WriteLine(GenerateFEN());
            allMoves = AllMoves(color);
        }
        foreach (int[][] move in allMoves)
        {
            Move(move[0], move[1]);
            RepeatingPerft(depth - 1, !color);
            UnMove();
        }
    }

    public List<int[][]> AllMoves(bool color)
    {
        var list = new List<int[][]>();
        List<int[]> temp;
        var temp2 = new List<int[][]>();

        for (int i = 0; i < board.Length; i++)
        {
            for (int j = 0; j < board[i].Length; j++)
            {
                temp2.Clear();
                if (board[i][j] > 0 == color && board[i][j] != blank)
                {
                    temp = LegalMoves(new int[] { j, i });
                    foreach (int[] item in temp)
                    {
                        temp2.Add(new int[][] { new int[] { j, i }, item });
                    }
                    list.AddRange(temp2);
                }
            }
        }
        return list;
    }
    public List<int[]> PseudoLegalMoves(int[] piece, bool takeOwn)
    {
        int pieceType = board[piece[1]][piece[0]];
        List<int[]> legal = new List<int[]>();
        if (pieceType == blank)
        {
            return legal;
        }
        else if (Math.Abs(pieceType) == pawn)
        {
            if (pieceType > 0)
            {
                if (board[piece[1] - 1][piece[0]] == 0 && piece[1] - 1 >= 0)
                {
                    legal.Add(new int[] { piece[0], piece[1] - 1 });
                    if (piece[1] == 6 && board[piece[1] - 2][piece[0]] == blank)
                        legal.Add(new int[] { piece[0], piece[1] - 2 });
                }
                if (piece[0] != 7 && (board[piece[1] - 1][piece[0] + 1] < 0 || takeOwn))
                    legal.Add(new int[] { piece[0] + 1, piece[1] - 1 });
                if (piece[0] != 0 && (board[piece[1] - 1][piece[0] - 1] < 0 || takeOwn))
                    legal.Add(new int[] { piece[0] - 1, piece[1] - 1 });
            }
            else
            {
                if (board[piece[1] + 1][piece[0]] == 0 && piece[1] - 1 <= 7)
                {
                    legal.Add(new int[] { piece[0], piece[1] + 1 });
                    if (piece[1] == 1 && board[piece[1] + 2][piece[0]] == blank)
                        legal.Add(new int[] { piece[0], piece[1] + 2 });
                }
                if (piece[0] != 7 && (board[piece[1] + 1][piece[0] + 1] > blank || takeOwn))
                    legal.Add(new int[] { piece[0] + 1, piece[1] + 1 });
                if (piece[0] != 0 && (board[piece[1] + 1][piece[0] - 1] > blank || takeOwn))
                    legal.Add(new int[] { piece[0] - 1, piece[1] + 1 });
            }

            if (pieceType > 0 && piece[1] == 3 && moveArr.Count > 0)
            {
                if (piece[0] != 7 && (board[piece[1]][piece[0] + 1] == -pawn && Enumerable.SequenceEqual(moveArr.Last()[1], new int[] { piece[0] + 1, piece[1] }) && Enumerable.SequenceEqual(moveArr.Last()[0], new int[] { piece[0] + 1, piece[1] - 2 })))
                {
                    legal.Add(new int[] { piece[0] + 1, piece[1] - 1 });
                }
                if (piece[0] != 0 && board[piece[1]][piece[0] - 1] == -pawn && Enumerable.SequenceEqual(moveArr.Last()[1], new int[] { piece[0] - 1, piece[1] }) && Enumerable.SequenceEqual(moveArr.Last()[0], new int[] { piece[0] - 1, piece[1] - 2 }))
                {
                    legal.Add(new int[] { piece[0] - 1, piece[1] - 1 });
                }
            }
            else if (pieceType < 0 && piece[1] == 4 && moveArr.Count > 0)
            {
                if (piece[0] != 7 && board[piece[1]][piece[0] + 1] == pawn && Enumerable.SequenceEqual(moveArr.Last()[1], new int[] { piece[0] + 1, piece[1] }) && Enumerable.SequenceEqual(moveArr.Last()[0], new int[] { piece[0] + 1, piece[1] + 2 }))
                {
                    legal.Add(new int[] { piece[0] + 1, piece[1] + 1 });
                }
                if (piece[0] != 0 && board[piece[1]][piece[0] - 1] == pawn && Enumerable.SequenceEqual(moveArr.Last()[1], new int[] { piece[0] - 1, piece[1] }) && Enumerable.SequenceEqual(moveArr.Last()[0], new int[] { piece[0] - 1, piece[1] + 2 }))
                {
                    legal.Add(new int[] { piece[0] - 1, piece[1] + 1 });
                }
            }
        }
        else if (Math.Abs(pieceType) == king)
        {
            legal.Add(new int[] { piece[0], piece[1] + 1 });
            legal.Add(new int[] { piece[0] + 1, piece[1] + 1 });
            legal.Add(new int[] { piece[0] - 1, piece[1] + 1 });
            legal.Add(new int[] { piece[0] - 1, piece[1] - 1 });
            legal.Add(new int[] { piece[0], piece[1] - 1 });
            legal.Add(new int[] { piece[0] + 1, piece[1] - 1 });
            legal.Add(new int[] { piece[0] + 1, piece[1] });
            legal.Add(new int[] { piece[0] - 1, piece[1] });
            for (int i = 0; i < legal.Count; i++)
            {
                if (legal[i][0] < 0 || legal[i][0] > 7 || legal[i][1] < 0 || legal[i][1] > 7)
                {
                    legal.RemoveAt(i);
                    i--;
                    continue;
                }
                int item = board[legal[i][1]][legal[i][0]];
                if (item != blank)
                {
                    if ((pieceType > 0) == (item > 0) && !takeOwn)
                    {
                        legal.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
            }
            if (pieceType > 0)
            {
                if (whiteKingSide && board[7][7] == rook && board[piece[1]][piece[0] + 1] == blank && board[piece[1]][piece[0] + 2] == blank && (attackedBlackArr.Count == 0 || attackedBlackArr.Last()[piece[1]][piece[0]] == blank && attackedBlackArr.Last()[piece[1]][piece[0] + 1] == blank))
                    legal.Add(new int[] { 6, 7 });
                else if (whiteQueenSide && board[7][0] == rook && board[piece[1]][piece[0] - 1] == blank && board[piece[1]][piece[0] - 2] == 0 && board[piece[1]][piece[0] - 3] == blank && (attackedBlackArr.Count == blank || attackedBlackArr.Last()[piece[1]][piece[0]] == blank && attackedBlackArr.Last()[piece[1]][piece[0] - 1] == blank && attackedBlackArr.Last()[piece[1]][piece[0] - 2] == blank && attackedBlackArr.Last()[piece[1]][piece[0] - 3] == blank))
                    legal.Add(new int[] { 2, 7 });
            }
            else
            {
                if (blackKingSide && board[0][7] == -rook && board[piece[1]][piece[0] + 1] == blank && board[piece[1]][piece[0] + 2] == blank && (attackedWhiteArr.Count == blank || attackedWhiteArr.Last()[piece[1]][piece[0]] == blank && attackedWhiteArr.Last()[piece[1]][piece[0] + 1] == blank))
                    legal.Add(new int[] { 6, 0 });
                else if (blackQueenSide && board[0][0] == -rook && board[piece[1]][piece[0] - 1] == blank && board[piece[1]][piece[0] - 2] == blank && board[piece[1]][piece[0] - 3] == blank && (attackedWhiteArr.Count == blank || attackedWhiteArr.Last()[piece[1]][piece[0]] == blank && attackedWhiteArr.Last()[piece[1]][piece[0] - 1] == blank && attackedWhiteArr.Last()[piece[1]][piece[0] - 2] == blank && attackedWhiteArr.Last()[piece[1]][piece[0] - 3] == blank))
                    legal.Add(new int[] { 2, 0 });
            }
        }
        else if (Math.Abs(pieceType) == knight)
        {
            legal.Add(new int[] { piece[0] - 1, piece[1] + 2 });
            legal.Add(new int[] { piece[0] + 1, piece[1] + 2 });
            legal.Add(new int[] { piece[0] - 1, piece[1] - 2 });
            legal.Add(new int[] { piece[0] + 1, piece[1] - 2 });
            legal.Add(new int[] { piece[0] + 2, piece[1] - 1 });
            legal.Add(new int[] { piece[0] + 2, piece[1] + 1 });
            legal.Add(new int[] { piece[0] - 2, piece[1] - 1 });
            legal.Add(new int[] { piece[0] - 2, piece[1] + 1 });
            for (int i = 0; i < legal.Count; i++)
            {
                if (legal[i][0] < 0 || legal[i][0] > 7 || legal[i][1] < 0 || legal[i][1] > 7)
                {
                    legal.RemoveAt(i);
                    i--;
                    continue;
                }
                int item = board[legal[i][1]][legal[i][0]];
                if (item != blank)
                {
                    if ((pieceType > 0) == (item > 0) && !takeOwn)
                    {
                        legal.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
            }
        }
        else if (Math.Abs(pieceType) == queen)
        {
            int[] dists = new int[] { piece[0], 7 - piece[0], piece[1], 7 - piece[1], Math.Min(piece[0], piece[1]), Math.Min(piece[0], 7 - piece[1]), Math.Min(7 - piece[0], piece[1]), Math.Min(7 - piece[0], 7 - piece[1]) };
            for (int j = 0; j < dists.Length; j++)
            {
                for (int i = 1; i <= dists[j]; i++)
                {
                    int[] temp;

                    if (j == 0)
                        temp = new int[] { piece[0] - i, piece[1] };
                    else if (j == 1)
                        temp = new int[] { piece[0] + i, piece[1] };
                    else if (j == 2)
                        temp = new int[] { piece[0], piece[1] - i };
                    else if (j == 3)
                        temp = new int[] { piece[0], piece[1] + i };
                    else if (j == 4)
                        temp = new int[] { piece[0] - i, piece[1] - i };
                    else if (j == 5)
                        temp = new int[] { piece[0] - i, piece[1] + i };
                    else if (j == 6)
                        temp = new int[] { piece[0] + i, piece[1] - i };
                    else
                        temp = new int[] { piece[0] + i, piece[1] + i };
                    if (board[temp[1]][temp[0]] == blank)
                        legal.Add(temp);
                    else
                    {
                        if ((pieceType > 0) == (board[temp[1]][temp[0]] > 0) && !takeOwn)
                            break;
                        else
                        {
                            legal.Add(temp);
                            break;
                        }
                    }
                }
            }
        }
        else if (Math.Abs(pieceType) == rook)
        {
            int[] dists = new int[] { piece[0], 7 - piece[0], piece[1], 7 - piece[1] };
            for (int j = 0; j < dists.Length; j++)
            {
                for (int i = 1; i <= dists[j]; i++)
                {
                    int[] temp;

                    if (j == 0)
                        temp = new int[] { piece[0] - i, piece[1] };
                    else if (j == 1)
                        temp = new int[] { piece[0] + i, piece[1] };
                    else if (j == 2)
                        temp = new int[] { piece[0], piece[1] - i };
                    else
                        temp = new int[] { piece[0], piece[1] + i };
                    if (board[temp[1]][temp[0]] == blank)
                        legal.Add(temp);
                    else
                    {
                        if ((pieceType > 0) == (board[temp[1]][temp[0]] > 0) && !takeOwn)
                            break;
                        else
                        {
                            legal.Add(temp);
                            break;
                        }
                    }
                }
            }
        }
        else if (Math.Abs(pieceType) == bishop)
        {
            int[] dists = new int[] { Math.Min(piece[0], piece[1]), Math.Min(piece[0], 7 - piece[1]), Math.Min(7 - piece[0], piece[1]), Math.Min(7 - piece[0], 7 - piece[1]) };
            for (int j = 0; j < dists.Length; j++)
            {
                for (int i = 1; i <= dists[j]; i++)
                {
                    int[] temp;

                    if (j == 0)
                        temp = new int[] { piece[0] - i, piece[1] - i };
                    else if (j == 1)
                        temp = new int[] { piece[0] - i, piece[1] + i };
                    else if (j == 2)
                        temp = new int[] { piece[0] + i, piece[1] - i };
                    else
                        temp = new int[] { piece[0] + i, piece[1] + i };
                    if (board[temp[1]][temp[0]] == blank)
                        legal.Add(temp);
                    else
                    {
                        if ((pieceType > 0) == (board[temp[1]][temp[0]] > 0) && !takeOwn)
                            break;
                        else
                        {
                            legal.Add(temp);
                            break;
                        }
                    }
                }
            }
        }
        return legal;
    }
    public List<int[]> AllPseudoLegalMovesForMove(bool color)
    {
        checkListWhite.Clear();
        checkListBlack.Clear();
        List<int[]> ret = new List<int[]>();
        List<int[]> temp;
        int[] saved = null;
        for (int i = 0; i < board.Length; i++)
        {
            for (int j = 0; j < board.Length; j++)
            {
                if (Math.Abs(board[i][j]) == king && (board[i][j] > 0) != color)
                {
                    board[i][j] = 0;
                    saved = new int[] { j, i };
                }
            }
        }
        if (saved == null)
        {
            Debug.WriteLine("Moves:");
            for (int i = 0; i < moveArr.Count; i++)
            {
                Debug.WriteLine("(" + moveArr[i][0][0] + ", " + moveArr[i][0][1] + ") to (" + moveArr[i][1][0] + ", " + moveArr[i][1][1] + ")");
            }
            Debug.WriteLine(GenerateFEN());
        }
        if (color)
        {
            whitePawnAttackedArr.Add(new int[][] { new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8] });
            Clear(whitePawnAttackedArr.Last());
            blackPawnAttackedArr.Add(null);
        }
        else
        {
            blackPawnAttackedArr.Add(new int[][] { new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8] });
            Clear(blackPawnAttackedArr.Last());
            whitePawnAttackedArr.Add(null);
        }
        for (int x = 0; x < board.Length; x++)
        {
            for (int y = 0; y < board[x].Length; y++)
            {
                if ((board[x][y] > 0) == color && board[x][y] != blank)
                {
                    temp = PseudoLegalMoves(new int[] { y, x }, true);
                    if (Math.Abs(board[x][y]) == pawn)
                    {
                        for (int i = 0; i < temp.Count; i++)
                        {
                            if (temp[i][0] == y)
                            {
                                temp.RemoveAt(i);
                                i--;
                                continue;
                            }
                            if (board[x][y] > 0)
                                whitePawnAttackedArr.Last()[temp[i][1]][temp[i][0]] = 1;
                            else
                                blackPawnAttackedArr.Last()[temp[i][1]][temp[i][0]] = 1;
                        }
                    }
                    for (int i = 0; i < temp.Count; i++)
                    {
                        if (temp[i][0] == saved[0] && temp[i][1] == saved[1])
                        {
                            if (!color)
                                checkListWhite.Add(new int[] { y, x });
                            else
                                checkListBlack.Add(new int[] { y, x });
                        }
                    }
                    ret.AddRange(temp);
                }
            }
        }
        if (!color)
            board[saved[1]][saved[0]] = king;
        else
            board[saved[1]][saved[0]] = -king;
        return ret;
    }
    public List<int[]> AllPseudoLegalMoves(bool color)
    {
        List<int[]> ret = new List<int[]>();
        List<int[]> temp;

        for (int x = 0; x < board.Length; x++)
        {
            for (int y = 0; y < board[x].Length; y++)
            {
                if ((board[x][y] > 0) == color && board[x][y] != blank)
                {
                    temp = PseudoLegalMoves(new int[] { y, x }, true);
                    if (Math.Abs(board[x][y]) == pawn)
                    {
                        for (int i = 0; i < temp.Count; i++)
                        {
                            if (temp[i][0] == y)
                            {
                                temp.RemoveAt(i);
                                i--;
                                continue;
                            }

                        }
                    }
                    ret.AddRange(temp);
                }
            }
        }
        return ret;
    }
    public void SetupBoard()
    {
        List<int[]> attacked;
        if (color)
        {
            attacked = AllPseudoLegalMovesForMove(false);
            Clear(attackedBlack);
            foreach (int[] item in attacked)
            {
                attackedBlack[item[1]][item[0]] = 1;
            }
        }
        else
        {
            attacked = AllPseudoLegalMovesForMove(true);
            Clear(attackedWhite);
            foreach (int[] item in attacked)
            {
                attackedWhite[item[1]][item[0]] = 1;
            }
        }
        int[] king = null;
        for (int i = 0; i < board.Length; i++)
        {
            for (int j = 0; j < board[i].Length; j++)
            {
                if (Math.Abs(board[i][j]) == 5 && board[i][j] > 0 == color)
                    king = new int[] { j, i };
            }
        }
        int[] dists = new int[] { king[0], 7 - king[0], king[1], 7 - king[1], Math.Min(king[0], king[1]), Math.Min(king[0], 7 - king[1]), Math.Min(7 - king[0], king[1]), Math.Min(7 - king[0], 7 - king[1]) };
        pinnedPiecesWhite.Clear();
        pinnedPiecesBlack.Clear();
        for (int j = 0; j < dists.Length; j++)
        {
            List<int[]> pinned = new List<int[]>();
            for (int i = 1; i <= dists[j]; i++)
            {
                int[] temp;

                if (j == 0)
                    temp = new int[] { king[0] - i, king[1] };
                else if (j == 1)
                    temp = new int[] { king[0] + i, king[1] };
                else if (j == 2)
                    temp = new int[] { king[0], king[1] - i };
                else if (j == 3)
                    temp = new int[] { king[0], king[1] + i };
                else if (j == 4)
                    temp = new int[] { king[0] - i, king[1] - i };
                else if (j == 5)
                    temp = new int[] { king[0] - i, king[1] + i };
                else if (j == 6)
                    temp = new int[] { king[0] + i, king[1] - i };
                else
                    temp = new int[] { king[0] + i, king[1] + i };
                if (board[temp[1]][temp[0]] != 0 && board[temp[1]][temp[0]] > 0 == color)
                {
                    pinned.Add(temp);
                }
                else if (pinned.Count == 0 && board[temp[1]][temp[0]] != 0 && board[temp[1]][temp[0]] > 0 == color)
                    break;
                if (pinned.Count > 1)
                    break;
                else if (pinned.Count == 1 && board[temp[1]][temp[0]] != 0 && board[temp[1]][temp[0]] > 0 != color)
                {
                    int type = Math.Abs(board[temp[1]][temp[0]]);
                    if (j < 4 && (type == 2 || type == 6))
                    {
                        if (color)
                            pinnedPiecesWhite.Add(new int[] { pinned[0][0], pinned[0][1], j });
                        else
                            pinnedPiecesBlack.Add(new int[] { pinned[0][0], pinned[0][1], j });
                    }
                    else if (j > 3 && (type == 4 || type == 6))
                    {
                        if (color)
                            pinnedPiecesWhite.Add(new int[] { pinned[0][0], pinned[0][1], j });
                        else
                            pinnedPiecesBlack.Add(new int[] { pinned[0][0], pinned[0][1], j });
                    }
                }
            }
        }
        pinnedPiecesWhiteArr.Add(pinnedPiecesWhite);
        pinnedPiecesBlackArr.Add(pinnedPiecesBlack);
        checkListWhiteArr.Add(checkListWhite);
        checkListBlackArr.Add(checkListBlack);
        attackedWhiteArr.Add(attackedWhite);
        attackedBlackArr.Add(attackedBlack);
    }
    public string GenerateFEN()
    {
        string ret = "";
        for (int y = 0; y < board.Length; y++)
        {
            for (int x = 0; x < board[y].Length; x++)
            {
                if (board[y][x] == 0)
                    ret += " ";
                else
                {
                    string temp = GetType(new int[] { x, y });
                    if (temp == "knight")
                    {
                        temp = temp.Substring(1);
                    }
                    char let = temp[0];
                    if (board[y][x] > 0)
                        let = Char.ToUpper(let);
                    ret += let;
                }
            }
            ret += "/";
        }
        string FEN2 = "";

        int space = 0;
        for (int i = 0; i < ret.Length; i++)
        {
            if (ret[i] != ' ')
            {
                if (space == 0)
                    FEN2 += ret[i];
                else
                {
                    FEN2 += space.ToString();
                    FEN2 += ret[i];
                }
                space = 0;
            }
            else
                space++;
        }
        FEN2 = FEN2.Substring(0, FEN2.Length - 1);
        ret = FEN2;

        if (color)
            ret += " w ";
        else
            ret += " b ";
        if (!whiteKingSide && !whiteQueenSide && !blackKingSide && !blackQueenSide)
            ret += "-";
        else
        {
            if (whiteKingSide)
                ret += "K";
            if (whiteQueenSide)
                ret += "Q";
            if (blackKingSide)
                ret += "k";
            if (blackQueenSide)
                ret += "q";
        }
        ret += " -";
        ret += " 0 0";
        return ret;
    }
    public List<int[]> LegalMoves(int[] piece)
    {
        int type = board[piece[1]][piece[0]];
        List<int[]> moves = PseudoLegalMoves(piece, false);
        if (checkListWhiteArr.Count == 0 || checkListBlackArr.Count == 0)
            return moves;
        if (Math.Abs(type) == 5)
        {
            if (type > 0)
            {
                for (int i = 0; i < moves.Count; i++)
                {
                    if (attackedBlackArr.Last()[moves[i][1]][moves[i][0]] == 1)
                    {
                        moves.RemoveAt(i);
                        i--;
                    }
                }
            }
            else
            {
                for (int i = 0; i < moves.Count; i++)
                {
                    if (attackedWhiteArr.Last()[moves[i][1]][moves[i][0]] == 1)
                    {
                        moves.RemoveAt(i);
                        i--;
                    }
                }
            }
            return moves;
        }
        int[] king = null;
        for (int i = 0; i < board.Length; i++)
        {
            for (int j = 0; j < board[i].Length; j++)
            {
                if (Math.Abs(board[i][j]) == 5)
                {
                    if (board[i][j] > 0 == color)
                    {
                        king = new int[] { j, i };
                    }
                }
            }
        }
        if (color)
        {
            if (pinnedPiecesWhiteArr.Last().Count > 0)
            {
                int index = -1;
                for (int i = 0; i < pinnedPiecesWhiteArr.Last().Count; i++)
                {
                    if (pinnedPiecesWhiteArr.Last()[i][0] == piece[0] && pinnedPiecesWhiteArr.Last()[i][1] == piece[1])
                    {
                        index = i;
                        break;
                    }
                }
                if (index != -1)
                {
                    for (int i = 0; i < moves.Count; i++)
                    {
                        if (pinnedPiecesWhiteArr.Last()[index][2] == 0 || pinnedPiecesWhiteArr.Last()[index][2] == 1)
                        {
                            if (moves[i][1] != king[1])
                            {
                                moves.RemoveAt(i);
                                i--;
                            }
                        }
                        else if (pinnedPiecesWhiteArr.Last()[index][2] == 2 || pinnedPiecesWhiteArr.Last()[index][2] == 3)
                        {
                            if (moves[i][0] != king[0])
                            {
                                moves.RemoveAt(i);
                                i--;
                            }
                        }
                        else if (pinnedPiecesWhiteArr.Last()[index][2] == 4 || pinnedPiecesWhiteArr.Last()[index][2] == 7)
                        {
                            if (!(Math.Abs(moves[i][0] - king[0]) == Math.Abs(moves[i][1] - king[1])) || !(moves[i][0] > king[0] == moves[i][1] > king[1]))
                            {
                                moves.RemoveAt(i);
                                i--;
                            }
                        }
                        else if (pinnedPiecesWhiteArr.Last()[index][2] == 5 || pinnedPiecesWhiteArr.Last()[index][2] == 6)
                        {
                            if (!(Math.Abs(moves[i][0] - king[0]) == Math.Abs(moves[i][1] - king[1])) || !(moves[i][0] > king[0] != moves[i][1] > king[1]))
                            {
                                moves.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                }
            }
            if (checkListWhiteArr.Last().Count == 1)
            {
                if (board[checkListWhiteArr.Last()[0][1]][checkListWhiteArr.Last()[0][0]] == -3 || board[checkListWhiteArr.Last()[0][1]][checkListWhiteArr.Last()[0][0]] == -1)
                {
                    for (int i = 0; i < moves.Count; i++)
                    {
                        if (!Enumerable.SequenceEqual(moves[i], checkListWhiteArr.Last()[0]))
                        {
                            moves.RemoveAt(i);
                            i--;
                        }
                    }
                    return moves;
                }
                else
                {
                    int[][] legalMask = new int[][] { new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8] };
                    Clear(legalMask);
                    int dist;
                    dist = Math.Abs(king[1] - checkListWhiteArr.Last()[0][1]);
                    if (king[0] == checkListWhiteArr.Last()[0][0])
                    {
                        dist = Math.Abs(king[1] - checkListWhiteArr.Last()[0][1]);
                        if (king[1] > checkListWhiteArr.Last()[0][1])
                        {
                            // down
                            for (int i = 1; i <= dist; i++)
                            {
                                legalMask[king[1] - i][king[0]] = 1;
                            }
                        }
                        else
                        {
                            // up
                            for (int i = 1; i <= dist; i++)
                            {
                                legalMask[king[1] + i][king[0]] = 1;
                            }
                        }
                    }
                    else if (king[1] == checkListWhiteArr.Last()[0][1])
                    {
                        dist = Math.Abs(king[0] - checkListWhiteArr.Last()[0][0]);
                        if (king[0] > checkListWhiteArr.Last()[0][0])
                        {
                            // right
                            for (int i = 1; i <= dist; i++)
                            {
                                legalMask[king[1]][king[0] - i] = 1;
                            }
                        }
                        else
                        {
                            // left
                            for (int i = 1; i <= dist; i++)
                            {
                                legalMask[king[1]][king[0] + i] = 1;
                            }
                        }
                    }
                    else if (king[1] > checkListWhiteArr.Last()[0][1])
                    {
                        if (king[0] > checkListWhiteArr.Last()[0][0])
                        {
                            // down right
                            for (int i = 1; i <= dist; i++)
                            {
                                legalMask[king[1] - i][king[0] - i] = 1;
                            }
                        }
                        else
                        {
                            // down left
                            for (int i = 1; i <= dist; i++)
                            {
                                legalMask[king[1] - i][king[0] + i] = 1;
                            }
                        }
                    }
                    else
                    {
                        if (king[0] > checkListWhiteArr.Last()[0][0])
                        {
                            // up right
                            for (int i = 1; i <= dist; i++)
                            {
                                legalMask[king[1] + i][king[0] - i] = 1;
                            }
                        }
                        else
                        {
                            // up left
                            for (int i = 1; i <= dist; i++)
                            {
                                legalMask[king[1] + i][king[0] + i] = 1;
                            }
                        }
                    }
                    for (int i = 0; i < moves.Count; i++)
                    {
                        if (legalMask[moves[i][1]][moves[i][0]] != 1)
                        {
                            moves.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }
            else if (checkListWhiteArr.Last().Count == 2)
            {
                return new List<int[]>();
            }
            return moves;
        }
        else
        {
            if (pinnedPiecesBlackArr.Last().Count > 0)
            {
                int index = -1;
                for (int i = 0; i < pinnedPiecesBlackArr.Last().Count; i++)
                {
                    if (pinnedPiecesBlackArr.Last()[i][0] == piece[0] && pinnedPiecesBlackArr.Last()[i][1] == piece[1])
                    {
                        index = i;
                        break;
                    }
                }
                if (index != -1)
                {
                    for (int i = 0; i < moves.Count; i++)
                    {
                        if (pinnedPiecesBlackArr.Last()[index][2] == 0 || pinnedPiecesBlackArr.Last()[index][2] == 1)
                        {
                            if (moves[i][1] != king[1])
                            {
                                moves.RemoveAt(i);
                                i--;
                            }
                        }
                        else if (pinnedPiecesBlackArr.Last()[index][2] == 2 || pinnedPiecesBlackArr.Last()[index][2] == 3)
                        {
                            if (moves[i][0] != king[0])
                            {
                                moves.RemoveAt(i);
                                i--;
                            }
                        }
                        else if (pinnedPiecesBlackArr.Last()[index][2] == 4 || pinnedPiecesBlackArr.Last()[index][2] == 7)
                        {
                            if (!(Math.Abs(moves[i][0] - king[0]) == Math.Abs(moves[i][1] - king[1])) || !(moves[i][0] > king[0] == moves[i][1] > king[1]))
                            {
                                moves.RemoveAt(i);
                                i--;
                            }
                        }
                        else if (pinnedPiecesBlackArr.Last()[index][2] == 5 || pinnedPiecesBlackArr.Last()[index][2] == 6)
                        {
                            if (!(Math.Abs(moves[i][0] - king[0]) == Math.Abs(moves[i][1] - king[1])) || !(moves[i][0] > king[0] != moves[i][1] > king[1]))
                            {
                                moves.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                }
                else if (checkListBlackArr.Last().Count == 2)
                {
                    return new List<int[]>();
                }
            }
            if (checkListBlackArr.Last().Count == 1)
            {
                if (board[checkListBlackArr.Last()[0][1]][checkListBlackArr.Last()[0][0]] == 3 || board[checkListBlackArr.Last()[0][1]][checkListBlackArr.Last()[0][0]] == 1)
                {
                    for (int i = 0; i < moves.Count; i++)
                    {
                        if (!Enumerable.SequenceEqual(moves[i], checkListBlackArr.Last()[0]))
                        {
                            moves.RemoveAt(i);
                            i--;
                        }
                    }
                    return moves;
                }
                else
                {
                    int[][] legalMask = new int[][] { new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8] };
                    Clear(legalMask);
                    int dist = 0;
                    if (king[0] == checkListBlackArr.Last()[0][0])
                    {
                        dist = Math.Abs(king[1] - checkListBlackArr.Last()[0][1]);
                        if (king[1] > checkListBlackArr.Last()[0][1])
                        {
                            // down
                            for (int i = 1; i <= dist; i++)
                            {
                                legalMask[king[1] - i][king[0]] = 1;
                            }
                        }
                        else
                        {
                            // up
                            for (int i = 1; i <= dist; i++)
                            {
                                legalMask[king[1] + i][king[0]] = 1;
                            }
                        }
                    }
                    else if (king[1] == checkListBlackArr.Last()[0][1])
                    {
                        dist = Math.Abs(king[0] - checkListBlackArr.Last()[0][0]);
                        if (king[0] > checkListBlackArr.Last()[0][0])
                        {
                            // right
                            for (int i = 1; i <= dist; i++)
                            {
                                legalMask[king[1]][king[0] - i] = 1;
                            }
                        }
                        else
                        {
                            // left
                            for (int i = 1; i <= dist; i++)
                            {
                                legalMask[king[1]][king[0] + i] = 1;
                            }
                        }
                    }
                    else if (king[1] > checkListBlackArr.Last()[0][1])
                    {
                        dist = Math.Abs(king[0] - checkListBlackArr.Last()[0][0]);
                        if (king[0] > checkListBlackArr.Last()[0][0])
                        {
                            // down right
                            for (int i = 1; i <= dist; i++)
                            {
                                legalMask[king[1] - i][king[0] - i] = 1;
                            }
                        }
                        else
                        {
                            // down left
                            for (int i = 1; i <= dist; i++)
                            {
                                legalMask[king[1] - i][king[0] + i] = 1;
                            }
                        }
                    }
                    else
                    {
                        dist = Math.Abs(king[0] - checkListBlackArr.Last()[0][0]);
                        if (king[0] > checkListBlackArr.Last()[0][0])
                        {
                            // up right
                            for (int i = 1; i <= dist; i++)
                            {
                                legalMask[king[1] + i][king[0] - i] = 1;
                            }
                        }
                        else
                        {
                            // up left
                            for (int i = 1; i <= dist; i++)
                            {
                                legalMask[king[1] + i][king[0] + i] = 1;
                            }
                        }
                    }
                    for (int i = 0; i < moves.Count; i++)
                    {
                        if (legalMask[moves[i][1]][moves[i][0]] != 1)
                        {
                            moves.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }
            else if (checkListBlackArr.Last().Count == 2)
            {
                return new List<int[]>();
            }
        }
        return moves;
    }
    public void SimpMove(int[] start, int[] end)
    {
        color = !color;
        int piece = board[start[1]][start[0]];
        promotionArr.Add(null);
        if (piece == 1 && end[1] == 0)
        {
            board[start[1]][start[0]] = 6;
            piece = 6;
            promotionArr.RemoveAt(promotionArr.Count - 1);
            promotionArr.Add(end);
        }
        else if (piece == -1 && end[1] == 7)
        {
            board[start[1]][start[0]] = -6;
            piece = -6;
            promotionArr.RemoveAt(promotionArr.Count - 1);
            promotionArr.Add(end);
        }
        if (Math.Abs(piece) == 5 && Math.Abs(start[0] - end[0]) > 1)
        {
            if (piece > 0)
            {
                if (start[0] - end[0] < 0)
                {
                    board[7][7] = 0;
                    board[7][5] = 2;
                }
                else
                {
                    board[7][0] = 0;
                    board[7][3] = 2;
                }
            }
            else
            {
                if (start[0] - end[0] < 0)
                {
                    board[0][7] = 0;
                    board[0][5] = -2;
                }
                else
                {
                    board[0][0] = 0;
                    board[0][3] = -2;
                }
            }
        }

        if (piece == 5)
        {
            whiteKingSide = false;
            whiteQueenSide = false;
        }
        else if (piece == -5)
        {
            blackKingSide = false;
            blackQueenSide = false;
        }
        else if (piece == 2 && start[0] == 7 && start[1] == 7)
            whiteKingSide = false;
        else if (piece == 2 && start[0] == 0 && start[1] == 7)
            whiteQueenSide = false;
        else if (piece == -2 && start[0] == 7 && start[1] == 0)
            blackKingSide = false;
        else if (piece == -2 && start[0] == 0 && start[1] == 0)
            blackQueenSide = false;

        takenArr.Add(board[end[1]][end[0]]);
        enPassantArr.Add(false);
        if (Math.Abs(piece) == 1)
        {
            if (Math.Abs(start[0] - end[0]) == Math.Abs(start[1] - end[1]) && board[end[1]][end[0]] == 0)
            {
                enPassantArr.RemoveAt(enPassantArr.Count - 1);
                enPassantArr.Add(true);
                takenArr[takenArr.Count - 1] = board[start[1]][end[0]];
                board[start[1]][end[0]] = 0;
            }
        }
        board[end[1]][end[0]] = piece;
        board[start[1]][start[0]] = 0;
        moveArr.Add(new int[][] { start, end });
        castling.Add(new bool[] { whiteKingSide, whiteQueenSide, blackKingSide, blackQueenSide });
        ulong zobrist = SlyCoyote.GetZobristHash(board);
        hashArr.Add(zobrist);
    }
    public void SimpUnMove()
    {
        if (promotionArr.Last() != null)
        {
            if (board[promotionArr.Last()[1]][promotionArr.Last()[0]] > 0)
                board[promotionArr.Last()[1]][promotionArr.Last()[0]] = 1;
            else
                board[promotionArr.Last()[1]][promotionArr.Last()[0]] = -1;
        }
        whiteKingSide = castling.Last()[0];
        whiteQueenSide = castling.Last()[1];
        blackKingSide = castling.Last()[2];
        blackQueenSide = castling.Last()[3];

        int tempSelf = board[moveArr.Last()[1][1]][moveArr.Last()[1][0]];
        if (!enPassantArr.Last())
            board[moveArr.Last()[1][1]][moveArr.Last()[1][0]] = takenArr.Last();
        else
        {
            board[moveArr.Last()[0][1]][moveArr.Last()[1][0]] = takenArr.Last();
            board[moveArr.Last()[1][1]][moveArr.Last()[1][0]] = 0;
        }
        board[moveArr.Last()[0][1]][moveArr.Last()[0][0]] = tempSelf;

        if (Math.Abs(tempSelf) == 5 && Math.Abs(moveArr.Last()[0][0] - moveArr.Last()[1][0]) > 1)
        {
            if (moveArr.Last()[1][0] == 6)
            {
                if (tempSelf > 0)
                {
                    board[7][5] = 0;
                    board[7][7] = 2;
                    whiteKingSide = true;
                }
                else
                {
                    board[0][5] = 0;
                    board[0][7] = -2;
                    blackKingSide = true;
                }
            }
            else
            {
                if (tempSelf > 0)
                {
                    board[7][3] = 0;
                    board[7][0] = 2;
                    whiteQueenSide = true;
                }
                else
                {
                    board[0][3] = 0;
                    board[0][0] = -2;
                    blackQueenSide = true;
                }
            }
        }

        color = !color;
        castling.RemoveAt(castling.Count - 1);
        moveArr.RemoveAt(moveArr.Count - 1);
        takenArr.RemoveAt(takenArr.Count - 1);
        promotionArr.RemoveAt(promotionArr.Count - 1);
        enPassantArr.RemoveAt(enPassantArr.Count - 1);
        hashArr.RemoveAt(hashArr.Count - 1);
    }
    public void Move(int[] start, int[] end)
    {
        color = !color;
        int piece = board[start[1]][start[0]];
        promotionArr.Add(null);
        if (piece == 1 && end[1] == 0)
        {
            board[start[1]][start[0]] = 6;
            piece = 6;
            promotionArr.RemoveAt(promotionArr.Count - 1);
            promotionArr.Add(end);
        }
        else if (piece == -1 && end[1] == 7)
        {
            board[start[1]][start[0]] = -6;
            piece = -6;
            promotionArr.RemoveAt(promotionArr.Count - 1);
            promotionArr.Add(end);
        }
        if (Math.Abs(piece) == 5 && Math.Abs(start[0] - end[0]) > 1)
        {
            if (piece > 0)
            {
                if (start[0] - end[0] < 0)
                {
                    board[7][7] = 0;
                    board[7][5] = 2;
                }
                else
                {
                    board[7][0] = 0;
                    board[7][3] = 2;
                }
            }
            else
            {
                if (start[0] - end[0] < 0)
                {
                    board[0][7] = 0;
                    board[0][5] = -2;
                }
                else
                {
                    board[0][0] = 0;
                    board[0][3] = -2;
                }
            }
        }

        if (piece == 5)
        {
            whiteKingSide = false;
            whiteQueenSide = false;
        }
        else if (piece == -5)
        {
            blackKingSide = false;
            blackQueenSide = false;
        }
        else if (piece == 2 && start[0] == 7 && start[1] == 7)
            whiteKingSide = false;
        else if (piece == 2 && start[0] == 0 && start[1] == 7)
            whiteQueenSide = false;
        else if (piece == -2 && start[0] == 7 && start[1] == 0)
            blackKingSide = false;
        else if (piece == -2 && start[0] == 0 && start[1] == 0)
            blackQueenSide = false;

        takenArr.Add(board[end[1]][end[0]]);
        enPassantArr.Add(false);
        if (Math.Abs(piece) == 1)
        {
            if (Math.Abs(start[0] - end[0]) == Math.Abs(start[1] - end[1]) && board[end[1]][end[0]] == 0)
            {
                enPassantArr.RemoveAt(enPassantArr.Count - 1);
                enPassantArr.Add(true);
                takenArr[takenArr.Count - 1] = board[start[1]][end[0]];
                board[start[1]][end[0]] = 0;
            }
        }
        board[end[1]][end[0]] = piece;
        board[start[1]][start[0]] = 0;
        moveArr.Add(new int[][] { start, end });
        castling.Add(new bool[] { whiteKingSide, whiteQueenSide, blackKingSide, blackQueenSide });
        List<int[]> attacked;
        if (color)
        {
            attackedBlack = new int[][] { new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8] }; ;
            attacked = AllPseudoLegalMovesForMove(false);
            Clear(attackedBlack);
            foreach (int[] item in attacked)
            {
                attackedBlack[item[1]][item[0]] = 1;
            }
        }
        else
        {
            attacked = AllPseudoLegalMovesForMove(true);
            attackedWhite = new int[][] { new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8] }; ;
            Clear(attackedWhite);
            foreach (int[] item in attacked)
            {
                attackedWhite[item[1]][item[0]] = 1;
            }
        }
        int[] king = null;
        for (int i = 0; i < board.Length; i++)
        {
            for (int j = 0; j < board[i].Length; j++)
            {
                if (Math.Abs(board[i][j]) == 5 && board[i][j] > 0 == color)
                    king = new int[] { j, i };
            }
        }
        int[] dists = new int[] { king[0], 7 - king[0], king[1], 7 - king[1], Math.Min(king[0], king[1]), Math.Min(king[0], 7 - king[1]), Math.Min(7 - king[0], king[1]), Math.Min(7 - king[0], 7 - king[1]) };
        if (color)
            pinnedPiecesWhite.Clear();
        else
            pinnedPiecesBlack.Clear();
        for (int j = 0; j < dists.Length; j++)
        {
            List<int[]> pinned = new List<int[]>();
            for (int i = 1; i <= dists[j]; i++)
            {
                int[] temp;

                if (j == 0)
                    temp = new int[] { king[0] - i, king[1] };
                else if (j == 1)
                    temp = new int[] { king[0] + i, king[1] };
                else if (j == 2)
                    temp = new int[] { king[0], king[1] - i };
                else if (j == 3)
                    temp = new int[] { king[0], king[1] + i };
                else if (j == 4)
                    temp = new int[] { king[0] - i, king[1] - i };
                else if (j == 5)
                    temp = new int[] { king[0] - i, king[1] + i };
                else if (j == 6)
                    temp = new int[] { king[0] + i, king[1] - i };
                else
                    temp = new int[] { king[0] + i, king[1] + i };
                if (board[temp[1]][temp[0]] != 0 && board[temp[1]][temp[0]] > 0 == color)
                {
                    pinned.Add(temp);
                }
                else if (pinned.Count == 0 && board[temp[1]][temp[0]] != 0 && board[temp[1]][temp[0]] > 0 == color)
                    break;
                if (pinned.Count > 1)
                    break;
                else if (pinned.Count == 1 && board[temp[1]][temp[0]] != 0 && board[temp[1]][temp[0]] > 0 != color)
                {
                    int type = Math.Abs(board[temp[1]][temp[0]]);
                    if (j < 4 && (type == 2 || type == 6))
                    {
                        if (color)
                            pinnedPiecesWhite.Add(new int[] { pinned[0][0], pinned[0][1], j });
                        else
                            pinnedPiecesBlack.Add(new int[] { pinned[0][0], pinned[0][1], j });
                    }
                    else if (j > 3 && (type == 4 || type == 6))
                    {
                        if (color)
                            pinnedPiecesWhite.Add(new int[] { pinned[0][0], pinned[0][1], j });
                        else
                            pinnedPiecesBlack.Add(new int[] { pinned[0][0], pinned[0][1], j });
                    }
                    else
                        break;
                }
            }
        }
        ulong zobrist = SlyCoyote.GetZobristHash(board);
        hashArr.Add(zobrist);

        pinnedPiecesWhiteArr.Add(pinnedPiecesWhite);
        pinnedPiecesBlackArr.Add(pinnedPiecesBlack);
        checkListWhiteArr.Add(checkListWhite);
        checkListBlackArr.Add(checkListBlack);
        attackedWhiteArr.Add(attackedWhite);
        attackedBlackArr.Add(attackedBlack);
    }
    public void UnMove()
    {
        if (promotionArr.Last() != null)
        {
            if (board[promotionArr.Last()[1]][promotionArr.Last()[0]] > 0)
                board[promotionArr.Last()[1]][promotionArr.Last()[0]] = 1;
            else
                board[promotionArr.Last()[1]][promotionArr.Last()[0]] = -1;
        }
        whiteKingSide = castling.Last()[0];
        whiteQueenSide = castling.Last()[1];
        blackKingSide = castling.Last()[2];
        blackQueenSide = castling.Last()[3];

        int tempSelf = board[moveArr.Last()[1][1]][moveArr.Last()[1][0]];
        if (!enPassantArr.Last())
            board[moveArr.Last()[1][1]][moveArr.Last()[1][0]] = takenArr.Last();
        else
        {
            board[moveArr.Last()[0][1]][moveArr.Last()[1][0]] = takenArr.Last();
            board[moveArr.Last()[1][1]][moveArr.Last()[1][0]] = 0;
        }
        board[moveArr.Last()[0][1]][moveArr.Last()[0][0]] = tempSelf;

        if (Math.Abs(tempSelf) == 5 && Math.Abs(moveArr.Last()[0][0] - moveArr.Last()[1][0]) > 1)
        {
            if (moveArr.Last()[1][0] == 6)
            {
                if (tempSelf > 0)
                {
                    board[7][5] = 0;
                    board[7][7] = 2;
                    whiteKingSide = true;
                }
                else
                {
                    board[0][5] = 0;
                    board[0][7] = -2;
                    blackKingSide = true;
                }
            }
            else
            {
                if (tempSelf > 0)
                {
                    board[7][3] = 0;
                    board[7][0] = 2;
                    whiteQueenSide = true;
                }
                else
                {
                    board[0][3] = 0;
                    board[0][0] = -2;
                    blackQueenSide = true;
                }
            }
        }

        color = !color;
        castling.RemoveAt(castling.Count - 1);
        moveArr.RemoveAt(moveArr.Count - 1);
        takenArr.RemoveAt(takenArr.Count - 1);

        pinnedPiecesWhiteArr.RemoveAt(pinnedPiecesWhiteArr.Count - 1);
        pinnedPiecesBlackArr.RemoveAt(pinnedPiecesBlackArr.Count - 1);
        checkListWhiteArr.RemoveAt(checkListWhiteArr.Count - 1);
        checkListBlackArr.RemoveAt(checkListBlackArr.Count - 1);
        attackedWhiteArr.RemoveAt(attackedWhiteArr.Count - 1);
        attackedBlackArr.RemoveAt(attackedBlackArr.Count - 1);
        promotionArr.RemoveAt(promotionArr.Count - 1);
        enPassantArr.RemoveAt(enPassantArr.Count - 1);
        whitePawnAttackedArr.RemoveAt(whitePawnAttackedArr.Count - 1);
        blackPawnAttackedArr.RemoveAt(blackPawnAttackedArr.Count - 1);
        hashArr.RemoveAt(hashArr.Count - 1);
    }
    public string GetType(int[] coords)
    {
        int thing = Math.Abs(board[coords[1]][coords[0]]);
        if (thing == 1)
            return "pawn";
        else if (thing == 2)
            return "rook";
        else if (thing == 3)
            return "knight";
        else if (thing == 4)
            return "bishop";
        else if (thing == 5)
            return "king";
        else if (thing == 6)
            return "queen";
        return "null";
    }
    public string GetTypeChar(int[] coords)
    {
        int thing = Math.Abs(board[coords[1]][coords[0]]);
        string ret = " ";
        if (thing == 1)
            ret = "p";
        else if (thing == 2)
            ret = "r";
        else if (thing == 3)
            ret = "n";
        else if (thing == 4)
            ret = "b";
        else if (thing == 5)
            ret = "k";
        else if (thing == 6)
            ret = "q";
        if (board[coords[1]][coords[0]] < 0)
            ret = ret.ToUpper();
        return ret;
    }
    public char GetTypeChar(int item)
    {
        int thing = Math.Abs(item);
        char ret = ' ';
        if (thing == 1)
            ret = 'p';
        else if (thing == 2)
            ret = 'r';
        else if (thing == 3)
            ret = 'n';
        else if (thing == 4)
            ret = 'b';
        else if (thing == 5)
            ret = 'k';
        else if (thing == 6)
            ret = 'q';
        if (item > 0)
            ret = Char.ToUpper(ret);
        return ret;
    }
    public void Clear(int[][] board)
    {
        for (int i = 0; i < board.Length; i++)
        {
            for (int j = 0; j < board[i].Length; j++)
            {
                board[i][j] = 0;
            }
        }
    }
    public static int[] FlipCoords(int[] coords, bool doit)
    {
        if (doit)
        {
            return new int[] { 7 - coords[0], 7 - coords[1] };
        }
        return coords;
    }
    public int Mate()
    {
        int[] king = null;
        for (int i = 0; i < board.Length; i++)
            for (int j = 0; j < board.Length; j++)
                if (Math.Abs(board[i][j]) == 5 && board[i][j] > 0 == color)
                    king = new int[] { j, i };
        List<int[][]> moves = AllMoves(color);
        if (moves.Count == 0)
        {
            if (color)
            {
                if (attackedBlackArr.Last()[king[1]][king[0]] == 1)
                    return 1;
                else
                    return 0;
            }
            else
            {
                if (attackedWhiteArr.Last()[king[1]][king[0]] == 1)
                    return 1;
                else
                    return 0;
            }
        }
        return -1;
    }
    public bool ThreefoldRepetition()
    {
        if (hashArr.Count < 3)
            return false;
        ulong lastItem = hashArr.Last();
        int count = 1;
        for (int i = 0; i < hashArr.Count - 1; i++)
        {
            if (hashArr[i] == lastItem)
                count++;
        }
        if (count >= 3)
            return true;
        return false;
    }
    public List<int[][]> CaptureMoves()
    {
        List<int[][]> moves = AllMoves(color);
        savedMoves = new List<int[][]>(moves);
        for (int i = 0; i < moves.Count; i++)
        {
            if (board[moves[i][1][1]][moves[i][1][0]] == 0)
            {
                moves.RemoveAt(i);
                i--;
            }
        }
        return moves;
    }
    public bool SafePosition()
    {
        int[][] attackedWhite = null;
        int[][] attackedBlack = null;
        if (color)
        {
            if (attackedBlackArr.Count != 0)
                attackedBlack = attackedBlackArr.Last();
            List<int[]> attacked = AllPseudoLegalMoves(true);
            attackedWhite = new int[][] { new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8] };
            Clear(attackedWhite);
            foreach (int[] item in attacked)
            {
                attackedWhite[item[1]][item[0]] = 1;
            }
        }
        else
        {
            if (attackedWhiteArr.Count != 0)
                attackedWhite = attackedWhiteArr.Last();
            attackedBlack = new int[][] { new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8] };
            List<int[]> attacked = AllPseudoLegalMoves(false);
            Clear(attackedBlack);
            foreach (int[] item in attacked)
            {
                attackedBlack[item[1]][item[0]] = 1;
            }
        }


        if (attackedBlackArr.Count != 0)
            for (int i = 0; i < attackedBlackArr.Last().Length; i++)
                for (int j = 0; j < 8; j++)
                    if (attackedBlack[i][j] == 1 && board[i][j] > 0)
                    {
                        return false;
                    }
        if (attackedWhiteArr.Count != 0)
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                    if (attackedWhite[i][j] == 1 && board[i][j] < 0)
                    {
                        return false;
                    }
        return true;
    }
    public Chess_Game Copy()
    {
        Chess_Game ret = new Chess_Game(GenerateFEN());
        ret.hashArr = new List<ulong>(hashArr);
        return ret;
    }
}
