using Microsoft.Win32.SafeHandles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data.Common;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Sly_Scorpion;

namespace Move_Generation
{
    internal class Chess
    {
        // Total number of pieces
        int total = 6;

        // Indices of each piece
        public int pawn = 0;
        public int rook = 1;
        public int knight = 2;
        public int bishop = 3;
        public int queen = 4;
        public int king = 5;

        // Boolean to store whose turn it is to move
        public bool whiteToMove = true;

        // Bitboards storing the position of all the pieces for each side
        public ulong whitePieces;
        public ulong blackPieces;

        // Bitboards storing the position of specific pieces for white
        public ulong whiteRooks;
        public ulong whiteKnights;
        public ulong whiteBishops;
        public ulong whiteQueens;
        public ulong whiteKing;
        public ulong whitePawns;

        // Bitboards storing the position of specific pieces for black
        public ulong blackRooks;
        public ulong blackKnights;
        public ulong blackBishops;
        public ulong blackQueens;
        public ulong blackKing;
        public ulong blackPawns;

        // Arrays containing each color's bitboards
        public ulong[] whitePieceBitboards;
        public ulong[] blackPieceBitboards;

        // Bitboard masks
        const ulong borderMask = 0x007E7E7E7E7E7E00;

        // Booleans to store whether either king can castle to either side
        public bool whiteKingSide = false;
        public bool whiteQueenSide = false;
        public bool blackKingSide = false;
        public bool blackQueenSide = false;
        // Masks to check whether there are any pieces on the squares we need for castling
        public const ulong originalWhiteKingPosition = 1UL << 4;
        public const ulong originalBlackKingPosition = 1UL << 60;
        public const ulong whiteKingSideMask = 1UL << 5 | 1UL << 6;
        public const ulong whiteQueenSideMask = 1UL << 1 | 1UL << 2 | 1UL << 3;
        public const ulong blackKingSideMask = 1UL << 61 | 1UL << 62;
        public const ulong blackQueenSideMask = 1UL << 57 | 1UL << 58 | 1UL << 59;
        // The move that each castling would move the king to
        public const ulong whiteKingMove = 1UL << 6;
        public const ulong whiteQueenMove = 1UL << 2;
        public const ulong blackKingMove = 1UL << 62;
        public const ulong blackQueenMove = 1UL << 58;
        // Starting rook positions
        public const ulong whiteQueenSideRook = 1UL;
        public const ulong whiteKingSideRook = 1UL << 7;
        public const ulong blackQueenSideRook = 1UL << 56;
        public const ulong blackKingSideRook = 1UL << 63;

        // En passant mask
        public ulong enPassantMask = 0UL;
        // The rows the pawns start on
        public ulong homeRowWhite = 0xFF << 8;
        public ulong homeRowBlack = 0xFF000000000000;
        public ulong longMoveWhite = 0xFF000000;
        public ulong longMoveBlack = 0xFF00000000;

        // Mask for the sides of the board
        public ulong leftMask = 0x0101010101010101;
        public ulong rightMask = 0x8080808080808080;
        public ulong bottomMask = 0xFF;
        public ulong topMask = 0xFF00000000000000;

        // Precomputed values for legal moves
        public ulong checkingPieces;
        public ulong attackMask;
        public ulong pins;

        // Lists to store old values for `UnMakeMove`
        public List<ulong> enPassantList = new List<ulong>();
        public List<bool> whiteKingSideList = new List<bool>();
        public List<bool> whiteQueenSideList = new List<bool>();
        public List<bool> blackKingSideList = new List<bool>();
        public List<bool> blackQueenSideList = new List<bool>();
        public List<ulong[]> whitePieceList = new List<ulong[]>();
        public List<ulong[]> blackPieceList = new List<ulong[]>();
        public List<ulong> zobristHashes = new List<ulong>();

        // Counter used for the `Perft` function
        private long perftCount = 0;

        // Base constructor to start a chess game from the starting position
        public Chess()
        {
            // Initializes bitboard arrays
            whitePieceBitboards = new ulong[total];
            blackPieceBitboards = new ulong[total];

            // Set the starting positions of the pawns
            whitePawns = 0xFF00;
            blackPawns = 0x00FF000000000000;
            whitePieceBitboards[pawn] = whitePawns;
            blackPieceBitboards[pawn] = blackPawns;

            // Set the starting positions of the rooks
            whiteRooks = 1UL << 7 | 1UL;
            blackRooks = 1UL << 63 | 1UL << 56;
            whitePieceBitboards[rook] = whiteRooks;
            blackPieceBitboards[rook] = blackRooks;

            // Set the starting positions of the knights
            whiteKnights = 1UL << 6 | 1UL << 1;
            blackKnights = 1UL << 62 | 1UL << 57;
            whitePieceBitboards[knight] = whiteKnights;
            blackPieceBitboards[knight] = blackKnights;

            // Set the starting positions of the bishops
            whiteBishops = 1UL << 5 | 1UL << 2;
            blackBishops = 1UL << 61 | 1UL << 58;
            whitePieceBitboards[bishop] = whiteBishops;
            blackPieceBitboards[bishop] = blackBishops;

            // Set the starting positions of the queens
            whiteQueens = 1UL << 3;
            blackQueens = 1UL << 59;
            whitePieceBitboards[queen] = whiteQueens;
            blackPieceBitboards[queen] = blackQueens;

            // Set the starting positions of the kings
            whiteKing = 1UL << 4;
            blackKing = 1UL << 60;
            whitePieceBitboards[king] = whiteKing;
            blackPieceBitboards[king] = blackKing;

            // Set the castling booleans
            whiteKingSide = true;
            whiteQueenSide = true;
            blackKingSide = true;
            blackQueenSide = true;

            // Update the bitboards that store all the pieces
            whitePieces = whiteRooks | whiteKnights | whiteBishops | whiteQueens | whiteKing | whitePawns;
            blackPieces = blackRooks | blackKnights | blackBishops | blackQueens | blackKing | blackPawns;
        }

        // Constructor to start a chess game from the given FEN position
        public Chess(string FEN)
        {
            // Initializes bitboard arrays
            whitePieceBitboards = new ulong[total];
            blackPieceBitboards = new ulong[total];

            for (int i = 0; i < total; i++)
            {
                whitePieceBitboards[i] = 0UL;
                blackPieceBitboards[i] = 0UL;
            }

            int lastIndex = FEN.IndexOf(' ');
            int x = 0;
            int y = 0;

            for (int i = 0; i < lastIndex + 1; i++)
            {
                if (Char.IsDigit(FEN[i]))
                    x += int.Parse(FEN[i].ToString());
                else if (FEN[i] == '/')
                {
                    x = 0;
                    y++;
                }
                else
                {
                    if (FEN[i] == 'p')
                        blackPieceBitboards[pawn] |= 1UL << (-(8 - x - 1) + (8 - y) * 8 - 1);
                    else if (FEN[i] == 'P')
                        whitePieceBitboards[pawn] |= 1UL << (-(8 - x - 1) + (8 - y) * 8 - 1);
                    else if (FEN[i] == 'r')
                        blackPieceBitboards[rook] |= 1UL << (-(8 - x - 1) + (8 - y) * 8 - 1);
                    else if (FEN[i] == 'R')
                        whitePieceBitboards[rook] |= 1UL << (-(8 - x - 1) + (8 - y) * 8 - 1);
                    else if (FEN[i] == 'n')
                        blackPieceBitboards[knight] |= 1UL << (-(8 - x - 1) + (8 - y) * 8 - 1);
                    else if (FEN[i] == 'N')
                        whitePieceBitboards[knight] |= 1UL << (-(8 - x - 1) + (8 - y) * 8 - 1);
                    else if (FEN[i] == 'b')
                        blackPieceBitboards[bishop] |= 1UL << (-(8 - x - 1) + (8 - y) * 8 - 1);
                    else if (FEN[i] == 'B')
                        whitePieceBitboards[bishop] |= 1UL << (-(8 - x - 1) + (8 - y) * 8 - 1);
                    else if (FEN[i] == 'k')
                        blackPieceBitboards[king] |= 1UL << (-(8 - x - 1) + (8 - y) * 8 - 1);
                    else if (FEN[i] == 'K')
                        whitePieceBitboards[king] |= 1UL << (-(8 - x - 1) + (8 - y) * 8 - 1);
                    else if (FEN[i] == 'q')
                        blackPieceBitboards[queen] |= 1UL << (-(8 - x - 1) + (8 - y) * 8 - 1);
                    else if (FEN[i] == 'Q')
                        whitePieceBitboards[queen] |= 1UL << (-(8 - x - 1) + (8 - y) * 8 - 1);
                    x++;
                }
            }
            if (FEN[lastIndex + 1] != 'w')
                whiteToMove = false;

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
            UpdateAllPieces();
        }

        // Moves a piece from a start position to an end position
        public void MakeMove(ulong start, ulong end)
        {
            if ((start & (whitePieces | blackPieces)) == 0L)
            {
                Debug.WriteLine(AllLegalMoves(false).Count);
                Debug.WriteLine(CheckMates());
                Debug.WriteLine(whiteToMove);
                PrintBoard();
                Bitboard.PrintBitboard(start);
                Bitboard.PrintBitboard(end);
                throw new ArgumentOutOfRangeException("No piece at the given position");
            }

            enPassantList.Add(enPassantMask);
            enPassantMask = 0UL;
            whiteKingSideList.Add(whiteKingSide);
            whiteQueenSideList.Add(whiteQueenSide);
            blackKingSideList.Add(blackKingSide);
            blackQueenSideList.Add(blackQueenSide);
            whitePieceList.Add(new ulong[] {whitePieceBitboards[pawn], whitePieceBitboards[rook], whitePieceBitboards[knight], whitePieceBitboards[bishop], whitePieceBitboards[queen], whitePieceBitboards[king]});
            blackPieceList.Add(new ulong[] {blackPieceBitboards[pawn], blackPieceBitboards[rook], blackPieceBitboards[knight], blackPieceBitboards[bishop], blackPieceBitboards[queen], blackPieceBitboards[king]});

            int pieceType = -1;
            int captureType = -1;
            bool white = (start & whitePieces) != 0UL;
            bool castled = false;

            if (white)
            {
                for (int i = 0; i < total; i++)
                {
                    if ((whitePieceBitboards[i] & start) != 0UL)
                        pieceType = i;
                    if ((blackPieceBitboards[i] & end) != 0UL)
                        captureType = i;
                }
                whitePieceBitboards[pieceType] = (whitePieceBitboards[pieceType] ^ start) | end;
                if (captureType != -1)
                    blackPieceBitboards[captureType] = blackPieceBitboards[captureType] ^ end;

                // Check for en passant
                if (pieceType == pawn && enPassantMask != 0UL && ((enPassantMask << 8) & end) != 0UL)
                    blackPieceBitboards[pawn] = blackPieceBitboards[pawn] & (~enPassantMask);

                // Promote white pawn to queen
                if (pieceType == pawn && white && (end & topMask) != 0UL)
                {
                    whitePieceBitboards[pawn] &= ~end;
                    whitePieceBitboards[queen] |= end;
                }

                // Check for castling
                // King side
                if (pieceType == king && (start & originalWhiteKingPosition) != 0UL && (end & whiteKingMove) != 0UL)
                {
                    whitePieceBitboards[rook] = (whitePieceBitboards[rook] & ~(1UL << 7)) | (whiteKingMove >> 1);
                    castled = true;
                    whiteKingSide = false;
                    whiteQueenSide = false;
                }
                // Queen side
                if (pieceType == king && (start & originalWhiteKingPosition) != 0UL && (end & whiteQueenMove) != 0UL)
                {
                    whitePieceBitboards[rook] = (whitePieceBitboards[rook] & ~1UL) | (whiteQueenMove << 1);
                    castled = true;
                    whiteKingSide = false;
                    whiteQueenSide = false;
                }
                if (pieceType == king && !castled)
                {
                    whiteQueenSide = false;
                    whiteKingSide = false;
                }
                if (pieceType == rook && (start & whiteQueenSideRook) != 0UL)
                    whiteQueenSide = false;
                else if (pieceType == rook && (start & whiteKingSideRook) != 0UL)
                    whiteKingSide = false;

                if ((start & homeRowWhite) != 0UL && (end & longMoveWhite) != 0UL)
                    enPassantMask = end;
            }
            else
            {
                for (int i = 0; i < total; i++)
                {
                    if ((blackPieceBitboards[i] & start) != 0UL)
                        pieceType = i;
                    if ((whitePieceBitboards[i] & end) != 0UL)
                        captureType = i;
                }
                blackPieceBitboards[pieceType] = (blackPieceBitboards[pieceType] ^ start) | end;
                if (captureType != -1)
                    whitePieceBitboards[captureType] = whitePieceBitboards[captureType] ^ end;

                // Check for en passant
                if (pieceType == pawn && enPassantMask != 0UL && ((enPassantMask >> 8) & end) != 0UL)
                    whitePieceBitboards[pawn] = whitePieceBitboards[pawn] & (~enPassantMask);

                if (pieceType == pawn && !white && (end & bottomMask) != 0UL)
                {
                    blackPieceBitboards[pawn] &= ~end;
                    blackPieceBitboards[queen] |= end;
                }

                // Check for castling
                // King side
                if (pieceType == king && (start & originalBlackKingPosition) != 0UL && (end & blackKingMove) != 0UL)
                {
                    blackPieceBitboards[rook] = (blackPieceBitboards[rook] & ~(1UL << 63)) | (blackKingMove >> 1);
                    castled = true;
                    blackKingSide = false;
                    blackQueenSide = false;
                }
                // Queen side
                if (pieceType == king && (start & originalBlackKingPosition) != 0UL && (end & blackQueenMove) != 0UL)
                {
                    blackPieceBitboards[rook] = (blackPieceBitboards[rook] & ~(1UL << 56)) | (blackQueenMove << 1);
                    castled = true;
                    blackKingSide = false;
                    blackQueenSide = false;
                }
                if (pieceType == king && !castled)
                {
                    blackKingSide = false;
                    blackQueenSide = false;
                }
                if (pieceType == rook && (start & blackQueenSideRook) != 0UL)
                    blackQueenSide = false;
                else if (pieceType == rook && (start & blackKingSideRook) != 0UL)
                    blackKingSide = false;

                if ((start & homeRowBlack) != 0UL && (end & longMoveBlack) != 0UL)
                    enPassantMask = end;
            }

            // Switch who is to move
            whiteToMove = !whiteToMove;

            zobristHashes.Add(Zobrist.CalculateZobristHash(whitePieceBitboards, blackPieceBitboards));

            // Update the bitboards that store all the pieces
            UpdateAllPieces();
        }

        // Unmakes the previous move made
        public void UnMakeMove()
        {
            whiteToMove = !whiteToMove;

            enPassantMask = enPassantList[^1];
            enPassantList.RemoveAt(enPassantList.Count - 1);

            whiteKingSide = whiteKingSideList[^1];
            whiteKingSideList.RemoveAt(whiteKingSideList.Count - 1);
            whiteQueenSide = whiteQueenSideList[^1];
            whiteQueenSideList.RemoveAt(whiteQueenSideList.Count - 1);
            blackKingSide = blackKingSideList[^1];
            blackKingSideList.RemoveAt(blackKingSideList.Count - 1);
            blackQueenSide = blackQueenSideList[^1];
            blackQueenSideList.RemoveAt(blackQueenSideList.Count - 1);

            whitePieceBitboards = whitePieceList[^1];
            whitePieceList.RemoveAt(whitePieceList.Count - 1);

            blackPieceBitboards = blackPieceList[^1];
            blackPieceList.RemoveAt(blackPieceList.Count - 1);

            zobristHashes.RemoveAt(zobristHashes.Count - 1);

            UpdateAllPieces();
        }

        // Updates the `whitePieces` and `blackPieces` bitboards
        public void UpdateAllPieces()
        {
            whitePieces = 0UL;
            blackPieces = 0UL;

            for (int i = 0; i < total; i++)
            {
                whitePieces |= whitePieceBitboards[i];
                blackPieces |= blackPieceBitboards[i];
            }
        }

        // Writes the current board position to the console
        public void PrintBoard()
        {
            for (int rank = 7; rank >= 0; rank--)
            {
                for (int file = 0; file < 8; file++)
                {
                    ulong position = 1UL << (rank * 8 + file);
                    // White piece
                    if ((whitePieces & position) != 0)
                    {
                        // Print the piece that is on the current position
                        if ((whitePieceBitboards[pawn] & position) != 0)
                            Debug.Write("P ");
                        else if ((whitePieceBitboards[rook] & position) != 0)
                            Debug.Write("R ");
                        else if ((whitePieceBitboards[knight] & position) != 0)
                            Debug.Write("N ");
                        else if ((whitePieceBitboards[bishop] & position) != 0)
                            Debug.Write("B ");
                        else if ((whitePieceBitboards[queen] & position) != 0)
                            Debug.Write("Q ");
                        else if ((whitePieceBitboards[king] & position) != 0)
                            Debug.Write("K ");
                        else
                            Debug.Write(". ");
                    }
                    // Black piece
                    else if ((blackPieces & position) != 0)
                    {
                        // Print the piece that is on the current position
                        if ((blackPieceBitboards[pawn] & position) != 0)
                            Debug.Write("p ");
                        else if ((blackPieceBitboards[rook] & position) != 0)
                            Debug.Write("r ");
                        else if ((blackPieceBitboards[knight] & position) != 0)
                            Debug.Write("n ");
                        else if ((blackPieceBitboards[bishop] & position) != 0)
                            Debug.Write("b ");
                        else if ((blackPieceBitboards[queen] & position) != 0)
                            Debug.Write("q ");
                        else if ((blackPieceBitboards[king] & position) != 0)
                            Debug.Write("k ");
                        else
                            Debug.Write(". ");
                    }
                    else
                        Debug.Write(". ");
                }
                Debug.WriteLine("");
            }
            Debug.WriteLine("");
        }

        // Precomputes a knight move given the position
        private ulong KnightMoves(ulong knightPosition)
        {
            // Masks to prevent wraparound on the board
            ulong notAFile = 0xfefefefefefefefe; // ~0x0101010101010101
            ulong notABFile = 0xfcfcfcfcfcfcfcfc; // ~0x0303030303030303
            ulong notHFile = 0x7f7f7f7f7f7f7f7f; // ~0x8080808080808080
            ulong notGHFile = 0x3f3f3f3f3f3f3f3f; // ~0xc0c0c0c0c0c0c0c0

            // Generate the possible moves
            ulong moves = (knightPosition >> 17) & notHFile;   // Move up and to the left
            moves |= (knightPosition >> 15) & notAFile;        // Move up and to the right
            moves |= (knightPosition >> 10) & notGHFile;       // Move left and up
            moves |= (knightPosition >> 6) & notABFile;        // Move right and up
            moves |= (knightPosition << 17) & notAFile;        // Move down and to the right
            moves |= (knightPosition << 15) & notHFile;        // Move down and to the left
            moves |= (knightPosition << 10) & notABFile;       // Move right and down
            moves |= (knightPosition << 6) & notGHFile;        // Move left and down

            return moves;
        }

        // Calculate pseudo-legal moves for horizontal/vertical sliders
        public ulong PseudoLegalHorizontalVerticalMoves(ulong piece, bool color, bool takeOwn)
        {
            // Finds the position of the piece
            int position = (int)Math.Log2(piece);

            // Bitboards of a column and row
            ulong horizontal = 0xFF;
            ulong vertical = 0x0101010101010101;

            // Calculates the row and column of the piece
            int row = position / 8;
            int column = position % 8;

            // Creates a bitboard of a cross with the center at the piece
            ulong cross = (horizontal << row * 8) | (vertical << column);
            // Separates the cross into rays in the four different directions
            ulong leftBitboard = Bitboard.GetLeftBits(piece, cross);
            ulong upBitboard = Bitboard.GetUpBits(piece, cross);
            ulong rightBitboard = Bitboard.GetRightBits(piece, cross);
            ulong downBitboard = Bitboard.GetDownBits(piece, cross);

            // Finds the location at which the ray must stop its movement
            // Calculates which of the opponent's pieces block this ray
            // Border mask prevents wraparound after the shift

            ulong upOpponentBlockers = upBitboard & (!color ? whitePieces : blackPieces) & ~topMask;
            // Shift the opponent blockers back because the ray can take them, so they are stopped later
            upOpponentBlockers <<= 8;
            // Finds the allies that block the piece's ray
            ulong upAllyBlockers = upBitboard & (color ? whitePieces : blackPieces);
            if (takeOwn)
            {
                upAllyBlockers &= ~topMask;
                upAllyBlockers <<= 8;
            }
            // Filter out the pieces behind the one that blocks the ray
            ulong upStops = Bitboard.GetLeastSignificantBit(upAllyBlockers | upOpponentBlockers);

            ulong downOpponentBlockers = downBitboard & (!color ? whitePieces : blackPieces) & ~bottomMask;
            downOpponentBlockers >>= 8;
            ulong downAllyBlockers = downBitboard & (color ? whitePieces : blackPieces);
            if (takeOwn)
            {
                downAllyBlockers &= ~bottomMask;
                downAllyBlockers >>= 8;
            }
            ulong downStops = Bitboard.GetMostSignificantBit(downAllyBlockers | downOpponentBlockers);

            ulong leftOpponentBlockers = leftBitboard & (!color ? whitePieces : blackPieces) & ~leftMask;
            leftOpponentBlockers >>= 1;
            ulong leftAllyBlockers = leftBitboard & (color ? whitePieces : blackPieces);
            if (takeOwn)
            {
                leftAllyBlockers &= ~leftMask;
                leftAllyBlockers >>= 1;
            }
            ulong leftStops = Bitboard.GetMostSignificantBit(leftAllyBlockers | leftOpponentBlockers);

            ulong rightOpponentBlockers = rightBitboard & (!color ? whitePieces : blackPieces) & ~rightMask;
            rightOpponentBlockers <<= 1;
            ulong rightAllyBlockers = rightBitboard & (color ? whitePieces : blackPieces);
            if (takeOwn)
            {
                rightAllyBlockers &= ~rightMask;
                rightAllyBlockers <<= 1;
            }
            ulong rightStops = Bitboard.GetLeastSignificantBit(rightAllyBlockers | rightOpponentBlockers);

            // Separate the ray into a vertical and horizontal column
            ulong verticalColumn = vertical << column;
            ulong horizontalColumn = horizontal << row * 8;


            // Cut the cross such that it is blocked by the stops calculated above, giving us pseudo-legal rook moves
            if (upStops != 0UL)
                verticalColumn = Bitboard.GetDownBits(upStops, verticalColumn);
            if (downStops != 0UL)
                verticalColumn = Bitboard.GetUpBits(downStops, verticalColumn);
            if (rightStops != 0UL)
                horizontalColumn = Bitboard.GetLeftBits(rightStops, horizontalColumn);
            if (leftStops != 0UL)
                horizontalColumn = Bitboard.GetRightBits(leftStops, horizontalColumn);

            return (verticalColumn | horizontalColumn) & ~piece;
        }

        // Calculate pseudo-legal horizontal slider moves
        public ulong PseudoLegalHorizontalMoves(ulong piece, bool color, bool takeOwn)
        {
            // Finds the position of the piece
            int position = (int)Math.Log2(piece);

            // Bitboards of a column and row
            ulong horizontal = 0xFF;

            // Calculates the row and column of the piece
            int row = position / 8;

            // Creates a bitboard of a cross with the center at the piece
            ulong cross = (horizontal << row * 8);
            // Separates the cross into rays in the four different directions
            ulong leftBitboard = Bitboard.GetLeftBits(piece, cross);
            ulong rightBitboard = Bitboard.GetRightBits(piece, cross);

            ulong leftOpponentBlockers = leftBitboard & (!color ? whitePieces : blackPieces) & ~leftMask;
            leftOpponentBlockers >>= 1;
            ulong leftAllyBlockers = leftBitboard & (color ? whitePieces : blackPieces);
            if (takeOwn)
            {
                leftAllyBlockers &= ~leftMask;
                leftAllyBlockers >>= 1;
            }
            ulong leftStops = Bitboard.GetMostSignificantBit(leftAllyBlockers | leftOpponentBlockers);

            ulong rightOpponentBlockers = rightBitboard & (!color ? whitePieces : blackPieces) & ~rightMask;
            rightOpponentBlockers <<= 1;
            ulong rightAllyBlockers = rightBitboard & (color ? whitePieces : blackPieces);
            if (takeOwn)
            {
                rightAllyBlockers &= ~rightMask;
                rightAllyBlockers <<= 1;
            }
            ulong rightStops = Bitboard.GetLeastSignificantBit(rightAllyBlockers | rightOpponentBlockers);

            ulong horizontalColumn = horizontal << row * 8;

            if (rightStops != 0UL)
                horizontalColumn = Bitboard.GetLeftBits(rightStops, horizontalColumn);
            if (leftStops != 0UL)
                horizontalColumn = Bitboard.GetRightBits(leftStops, horizontalColumn);

            return horizontalColumn & ~piece;
        }

        // Calculate pseudo-legal vertical slider moves
        public ulong PseudoLegalVerticalMoves(ulong piece, bool color, bool takeOwn)
        {
            // Finds the position of the piece
            int position = (int)Math.Log2(piece);

            // Bitboards of a column and row
            ulong vertical = 0x0101010101010101;

            // Calculates the row and column of the piece
            int column = position % 8;

            // Creates a bitboard of a cross with the center at the piece
            ulong cross = vertical << column;
            // Separates the cross into rays in the four different directions
            ulong upBitboard = Bitboard.GetUpBits(piece, cross);
            ulong downBitboard = Bitboard.GetDownBits(piece, cross);

            // Finds the location at which the ray must stop its movement
            // Calculates which of the opponent's pieces block this ray
            // Border mask prevents wraparound after the shift

            ulong upOpponentBlockers = upBitboard & (!color ? whitePieces : blackPieces) & ~topMask;
            // Shift the opponent blockers back because the ray can take them, so they are stopped later
            upOpponentBlockers <<= 8;
            // Finds the allies that block the piece's ray
            ulong upAllyBlockers = upBitboard & (color ? whitePieces : blackPieces);
            if (takeOwn)
            {
                upAllyBlockers &= ~topMask;
                upAllyBlockers <<= 8;
            }
            // Filter out the pieces behind the one that blocks the ray
            ulong upStops = Bitboard.GetLeastSignificantBit(upAllyBlockers | upOpponentBlockers);

            ulong downOpponentBlockers = downBitboard & (!color ? whitePieces : blackPieces) & ~bottomMask;
            downOpponentBlockers >>= 8;
            ulong downAllyBlockers = downBitboard & (color ? whitePieces : blackPieces);
            if (takeOwn)
            {
                downAllyBlockers &= ~bottomMask;
                downAllyBlockers >>= 8;
            }
            ulong downStops = Bitboard.GetMostSignificantBit(downAllyBlockers | downOpponentBlockers);

            // Separate the ray into a vertical and horizontal column
            ulong verticalColumn = vertical << column;


            // Cut the cross such that it is blocked by the stops calculated above, giving us pseudo-legal rook moves
            if (upStops != 0UL)
                verticalColumn = Bitboard.GetDownBits(upStops, verticalColumn);
            if (downStops != 0UL)
                verticalColumn = Bitboard.GetUpBits(downStops, verticalColumn);

            return verticalColumn & ~piece;
        }

        // Calculates pseudo-legal moves for diagonal sliders
        public ulong PseudoLegalDiagonalMoves(ulong piece, bool color, bool takeOwn)
        {
            // Finds the position of the piece
            int position = (int)Math.Log2(piece);

            // Calculates the row and column of the piece
            int row = position / 8;
            int column = position % 8;

            // The decimal numbers for two diagonal crosses across the board
            ulong crossTopLeft = 9241421688590303745;
            ulong crossTopRight = 72624976668147840;

            // Calculate how much the program has to shift the top left cross to have it overlap with the piece
            int leftShift = row > column ? row - column : column - row;
            // Whether the program shifts left or right
            bool leftRight = row > column;
            // Creates a mask to prevent wraparound
            ulong leftShiftMask = !leftRight ? 0xFFFFFFFFFFFFFFFFUL >> (leftShift * 8) : 0xFFFFFFFFFFFFFFFF << (leftShift * 8);
            // Applies the mask
            crossTopLeft &= leftShiftMask;

            // Calculate how much the program has to shift the top right cross to have it overlap with the piece
            int rightShift = (row - (8 - column) + 1);
            // Whether to shift left or right
            bool rightRight = rightShift > 0;
            // Creates a mask to prevent wraparound
            ulong rightShiftMask = rightRight ? 0xFFFFFFFFFFFFFFFFUL << (rightShift * 8) : 0xFFFFFFFFFFFFFFFF >> (-rightShift * 8);
            // Applies the mask
            crossTopRight &= rightShiftMask;

            // Shifts the diagonal crosses to overlap with the piece
            ulong crossLeft = !leftRight ? crossTopLeft << leftShift : crossTopLeft >> leftShift;
            ulong crossRight = rightRight ? crossTopRight << rightShift : crossTopRight >> -rightShift;
            // Splits the diagonal crosses into different rays
            ulong topLeft = Bitboard.GetMoreSignificantBits(crossRight, piece) & ~piece;
            ulong topRight = Bitboard.GetMoreSignificantBits(crossLeft, piece) & ~piece;
            ulong bottomLeft = crossLeft & ~topRight & ~piece;
            ulong bottomRight = crossRight & ~topLeft & ~piece;

            // Similar process to that used in the horizontal/vertical slider function to calculate the stops
            ulong topLeftOpponentBlockers = topLeft & (!color ? whitePieces : blackPieces) & borderMask;
            topLeftOpponentBlockers <<= 7;
            ulong topLeftAllyBlockers = topLeft & (color ? whitePieces : blackPieces);
            if (takeOwn)
            {
                topLeftAllyBlockers &= borderMask;
                topLeftAllyBlockers <<= 7;
            }
            ulong topLeftStops = Bitboard.GetLeastSignificantBit(topLeftAllyBlockers | topLeftOpponentBlockers);

            ulong topRightOpponentBlockers = topRight & (!color ? whitePieces : blackPieces) & borderMask;
            topRightOpponentBlockers <<= 9;
            ulong topRightAllyBlockers = topRight & (color ? whitePieces : blackPieces);
            if (takeOwn)
            {
                topRightAllyBlockers &= borderMask;
                topRightAllyBlockers <<= 9;
            }
            ulong topRightStops = Bitboard.GetLeastSignificantBit(topRightAllyBlockers | topRightOpponentBlockers);

            ulong bottomLeftOpponentBlockers = bottomLeft & (!color ? whitePieces : blackPieces) & borderMask;
            bottomLeftOpponentBlockers >>= 9;
            ulong bottomLeftAllyBlockers = bottomLeft & (color ? whitePieces : blackPieces);
            if (takeOwn)
            {
                bottomLeftAllyBlockers &= borderMask;
                bottomLeftAllyBlockers >>= 9;
            }
            ulong bottomLeftStops = Bitboard.GetMostSignificantBit(bottomLeftAllyBlockers | bottomLeftOpponentBlockers);

            ulong bottomRightOpponentBlockers = bottomRight & (!color ? whitePieces : blackPieces) & borderMask;
            bottomRightOpponentBlockers >>= 7;
            ulong bottomRightAllyBlockers = bottomRight & (color ? whitePieces : blackPieces);
            if (takeOwn)
            {
                bottomRightAllyBlockers &= borderMask;
                bottomRightAllyBlockers >>= 7;
            }
            ulong bottomRightStops = Bitboard.GetMostSignificantBit(bottomRightAllyBlockers | bottomRightOpponentBlockers);
            // Cuts the rays off at the stops
            if (topLeftStops != 0UL)
                crossRight = Bitboard.GetLessSignificantBits(crossRight, topLeftStops);
            if (topRightStops != 0UL)
                crossLeft = Bitboard.GetLessSignificantBits(crossLeft, topRightStops);
            if (bottomLeftStops != 0UL)
                crossLeft = Bitboard.GetMoreSignificantBits(crossLeft, bottomLeftStops) & ~bottomLeftStops;
            if (bottomRightStops != 0UL)
                crossRight = Bitboard.GetMoreSignificantBits(crossRight, bottomRightStops) & ~bottomRightStops;

            return (crossLeft | crossRight) ^ piece;
        }

        // Calculates a pseudo-legal top-right cross
        public ulong PseudoLegalTopRightMoves(ulong piece, bool color, bool takeOwn)
        {
            // Finds the position of the piece
            int position = (int)Math.Log2(piece);

            // Calculates the row and column of the piece
            int row = position / 8;
            int column = position % 8;

            // The decimal numbers for two diagonal crosses across the board
            ulong crossTopLeft = 9241421688590303745;

            // Calculate how much the program has to shift the top left cross to have it overlap with the piece
            int leftShift = row > column ? row - column : column - row;
            // Whether the program shifts left or right
            bool leftRight = row > column;
            // Creates a mask to prevent wraparound
            ulong leftShiftMask = !leftRight ? 0xFFFFFFFFFFFFFFFFUL >> (leftShift * 8) : 0xFFFFFFFFFFFFFFFF << (leftShift * 8);
            // Applies the mask
            crossTopLeft &= leftShiftMask;

            // Shifts the diagonal crosses to overlap with the piece
            ulong crossLeft = !leftRight ? crossTopLeft << leftShift : crossTopLeft >> leftShift;
            // Splits the diagonal crosses into different rays
            ulong topRight = Bitboard.GetMoreSignificantBits(crossLeft, piece) & ~piece;
            ulong bottomLeft = crossLeft & ~topRight & ~piece;

            ulong topRightOpponentBlockers = topRight & (!color ? whitePieces : blackPieces) & borderMask;
            topRightOpponentBlockers <<= 9;
            ulong topRightAllyBlockers = topRight & (color ? whitePieces : blackPieces);
            if (takeOwn)
            {
                topRightAllyBlockers &= borderMask;
                topRightAllyBlockers <<= 9;
            }
            ulong topRightStops = Bitboard.GetLeastSignificantBit(topRightAllyBlockers | topRightOpponentBlockers);

            ulong bottomLeftOpponentBlockers = bottomLeft & (!color ? whitePieces : blackPieces) & borderMask;
            bottomLeftOpponentBlockers >>= 9;
            ulong bottomLeftAllyBlockers = bottomLeft & (color ? whitePieces : blackPieces);
            if (takeOwn)
            {
                bottomLeftAllyBlockers &= borderMask;
                bottomLeftAllyBlockers >>= 9;
            }
            ulong bottomLeftStops = Bitboard.GetMostSignificantBit(bottomLeftAllyBlockers | bottomLeftOpponentBlockers);

            // Cuts the rays off at the stops
            if (topRightStops != 0UL)
                crossLeft = Bitboard.GetLessSignificantBits(crossLeft, topRightStops);
            if (bottomLeftStops != 0UL)
                crossLeft = Bitboard.GetMoreSignificantBits(crossLeft, bottomLeftStops) & ~bottomLeftStops;

            return crossLeft ^ piece;

        }

        // Calculates a pseudo-legal top-left cross
        public ulong PseudoLegalTopLeftMoves(ulong piece, bool color, bool takeOwn)
        {
            // Finds the position of the piece
            int position = (int)Math.Log2(piece);

            // Calculates the row and column of the piece
            int row = position / 8;
            int column = position % 8;

            // The decimal numbers for two diagonal crosses across the board
            ulong crossTopRight = 72624976668147840;

            // Calculate how much the program has to shift the top right cross to have it overlap with the piece
            int rightShift = (row - (8 - column) + 1);
            // Whether to shift left or right
            bool rightRight = rightShift > 0;
            // Creates a mask to prevent wraparound
            ulong rightShiftMask = rightRight ? 0xFFFFFFFFFFFFFFFFUL << (rightShift * 8) : 0xFFFFFFFFFFFFFFFF >> (-rightShift * 8);
            // Applies the mask
            crossTopRight &= rightShiftMask;

            // Shifts the diagonal crosses to overlap with the piece
            ulong crossRight = rightRight ? crossTopRight << rightShift : crossTopRight >> -rightShift;
            // Splits the diagonal crosses into different rays
            ulong topLeft = Bitboard.GetMoreSignificantBits(crossRight, piece) & ~piece;
            ulong bottomRight = crossRight & ~topLeft & ~piece;

            // Similar process to that used in the horizontal/vertical slider function to calculate the stops
            ulong topLeftOpponentBlockers = topLeft & (!color ? whitePieces : blackPieces) & borderMask;
            topLeftOpponentBlockers <<= 7;
            ulong topLeftAllyBlockers = topLeft & (color ? whitePieces : blackPieces);
            if (takeOwn)
            {
                topLeftAllyBlockers &= borderMask;
                topLeftAllyBlockers <<= 7;
            }
            ulong topLeftStops = Bitboard.GetLeastSignificantBit(topLeftAllyBlockers | topLeftOpponentBlockers);

            ulong bottomRightOpponentBlockers = bottomRight & (!color ? whitePieces : blackPieces) & borderMask;
            bottomRightOpponentBlockers >>= 7;
            ulong bottomRightAllyBlockers = bottomRight & (color ? whitePieces : blackPieces);
            if (takeOwn)
            {
                bottomRightAllyBlockers &= borderMask;
                bottomRightAllyBlockers >>= 7;
            }
            ulong bottomRightStops = Bitboard.GetMostSignificantBit(bottomRightAllyBlockers | bottomRightOpponentBlockers);
            // Cuts the rays off at the stops
            if (topLeftStops != 0UL)
                crossRight = Bitboard.GetLessSignificantBits(crossRight, topLeftStops);
            if (bottomRightStops != 0UL)
                crossRight = Bitboard.GetMoreSignificantBits(crossRight, bottomRightStops) & ~bottomRightStops;

            return crossRight ^ piece;
        }

        // Calculates pseudo-legal knight moves
        public ulong PseudoLegalKnightMoves(ulong piece, bool color, bool takeOwn)
        {
            ulong moves = KnightMoves(piece);
            ulong allies = color ? whitePieces : blackPieces;
            return takeOwn ? moves : moves & ~allies;
        }

        // Calculates pseudo-legal rook moves
        public ulong PseudoLegalRookMoves(ulong piece, bool color, bool takeOwn)
        {
            return PseudoLegalHorizontalVerticalMoves(piece, color, takeOwn);
        }

        // Calculates pseudo-legal bishop moves
        public ulong PseudoLegalBishopMoves(ulong piece, bool color, bool takeOwn)
        {
            return PseudoLegalDiagonalMoves(piece, color, takeOwn);
        }

        // Calculates pseudo-legal queen moves
        public ulong PseudoLegalQueenMoves(ulong piece, bool color, bool takeOwn)
        {
            return PseudoLegalDiagonalMoves(piece, color, takeOwn) | PseudoLegalHorizontalVerticalMoves(piece, color, takeOwn);
        }

        // Calculates pseudo-legal king moves
        public ulong PseudoLegalKingMoves(ulong piece, bool color, bool takeOwn)
        {
            // Masks to prevent horizontal wraparound
            ulong notAFile = 0xfefefefefefefefe; // 1111 1110 ... for all ranks, prevents wrap to the right
            ulong notHFile = 0x7f7f7f7f7f7f7f7f; // 0111 1111 ... for all ranks, prevents wrap to the left

            // King move calculations with wraparound prevention
            ulong leftMoves = (piece & notAFile) >> 1;
            ulong rightMoves = (piece & notHFile) << 1;
            ulong upMoves = piece << 8;
            ulong downMoves = piece >> 8;
            ulong upLeftMoves = (piece & notAFile) << 7;
            ulong upRightMoves = (piece & notHFile) << 9;
            ulong downLeftMoves = (piece & notAFile) >> 9;
            ulong downRightMoves = (piece & notHFile) >> 7;

            // Combine all possible moves
            ulong moves = leftMoves | rightMoves | upMoves | downMoves | upLeftMoves | upRightMoves | downLeftMoves | downRightMoves;

            // Get allies (same color pieces) to prevent moves on them
            ulong allies = color ? whitePieces : blackPieces;

            if (color)
            {
                if (whiteKingSide && ((whitePieces | blackPieces) & whiteKingSideMask) == 0UL && (attackMask & whiteKingSideMask) == 0UL && (whiteKingSideRook & whitePieceBitboards[rook]) != 0UL)
                    moves |= whiteKingMove;
                if (whiteQueenSide && ((whitePieces | blackPieces) & whiteQueenSideMask) == 0UL && (attackMask & whiteQueenSideMask) == 0UL && (whiteQueenSideRook & whitePieceBitboards[rook]) != 0UL)
                    moves |= whiteQueenMove;
            }
            else
            {
                if (blackKingSide && ((whitePieces | blackPieces) & blackKingSideMask) == 0UL && (attackMask & blackKingSideMask) == 0UL && (blackKingSideRook & blackPieceBitboards[rook]) != 0UL)
                    moves |= blackKingMove;
                if (blackQueenSide && ((whitePieces | blackPieces) & blackQueenSideMask) == 0UL && (attackMask & blackQueenSideMask) == 0UL && (blackKingSideRook & blackPieceBitboards[rook]) != 0UL)
                    moves |= blackQueenMove;
            }

            // Return only moves that do not land on allied pieces
            return takeOwn ? moves : moves & ~allies;
        }

        // Calculates pseudo-legal pawn moves
        public ulong PseudoLegalPawnMoves(ulong piece, bool color)
        {
            ulong moves = 0UL;
            ulong leftBorder = 0x0101010101010101UL;
            ulong rightBorder = leftBorder << 7;

            if (color)
            {
                ulong homeRow = homeRowWhite;
                moves |= ((piece << 8) & ~(whitePieces | blackPieces));
                if (moves != 0UL && (piece & homeRow) != 0UL && ((piece << 16) & (whitePieces | blackPieces)) == 0UL)
                    moves |= piece << 16;

                if ((piece & leftBorder) == 0UL && (piece << 7 & blackPieces) != 0UL)
                    moves |= piece << 7;
                if ((piece & rightBorder) == 0UL && (piece << 9 & blackPieces) != 0UL)
                    moves |= piece << 9;

                if ((piece & leftBorder) == 0UL && ((piece << 1) & enPassantMask) != 0UL && (enPassantMask & blackPieces) != 0UL)
                    moves |= piece << 9;
                if ((piece & rightBorder) == 0UL && ((piece >> 1) & enPassantMask) != 0UL && (enPassantMask & blackPieces) != 0UL)
                    moves |= piece << 7;
            }
            else
            {
                ulong homeRow = homeRowBlack;
                moves |= ((piece >> 8) & ~(whitePieces | blackPieces));
                if (moves != 0UL && (piece & homeRow) != 0UL && ((piece >> 16) & (whitePieces | blackPieces)) == 0UL)
                    moves |= piece >> 16;
                if ((piece & rightBorder) == 0UL && (piece >> 7 & whitePieces) != 0UL)
                    moves |= piece >> 7;
                if ((piece & leftBorder) == 0UL && (piece >> 9 & whitePieces) != 0UL)
                    moves |= piece >> 9;

                if ((piece & leftBorder) == 0UL && ((piece << 1) & enPassantMask) != 0UL && (enPassantMask & whitePieces) != 0UL)
                    moves |= piece >> 7;
                if ((piece & rightBorder) == 0UL && ((piece >> 1) & enPassantMask) != 0UL && (enPassantMask & whitePieces) != 0UL)
                    moves |= piece >> 9;
            }
            return moves;
        }

        // Calculates the attacking pawn moves (not including en passant)
        public ulong AttackingPawnMoves(ulong piece, bool color)
        {
            ulong moves = 0UL;
            ulong leftBorder = 0x0101010101010101UL;
            ulong rightBorder = leftBorder << 7;

            if (color)
            {
                if ((piece & leftBorder) == 0UL)
                    moves |= piece << 7;
                if ((piece & rightBorder) == 0UL)
                    moves |= piece << 9;
            }
            else
            {
                if ((piece & rightBorder) == 0UL)
                    moves |= piece >> 7;
                if ((piece & leftBorder) == 0UL)
                    moves |= piece >> 9;
            }
            return moves;
        }

        // Calculates the pseudo legal move of a piece given its type
        public ulong PseudoLegalMove(ulong piece, int type, bool color)
        {
            switch (type)
            {
                case 0:
                    return PseudoLegalPawnMoves(piece, color);
                case 1:
                    return PseudoLegalRookMoves(piece, color, false);
                case 2:
                    return PseudoLegalKnightMoves(piece, color, false);
                case 3:
                    return PseudoLegalBishopMoves(piece, color, false);
                case 4:
                    return PseudoLegalQueenMoves(piece, color, false);
                case 5:
                    return PseudoLegalKingMoves(piece, color, false);
                default:
                    return 0UL;
            }
        }

        // Calculates the mask of attacked pieces
        public ulong AttackMask(bool color)
        {
            ulong mask = 0UL;

            ulong[] bitboards = color ? whitePieceBitboards : blackPieceBitboards;
            ulong[] opponentBitboards = color ? blackPieceBitboards : whitePieceBitboards;

            // Delete the other king from the board
            ulong savedKing = opponentBitboards[king];

            if (!color)
                whitePieces &= ~savedKing;
            else
                blackPieces &= ~savedKing;

            // Kings
            ulong kingBitboard = bitboards[king];
            mask |= PseudoLegalKingMoves(kingBitboard, color, true);

            // Rooks
            ulong rookBitboard = bitboards[rook];
            int rookPopCount = BitOperations.PopCount(rookBitboard);
            if (rookPopCount == 1)
                mask |= PseudoLegalHorizontalVerticalMoves(rookBitboard, color, true);
            else if (rookPopCount == 2)
            {
                mask |= PseudoLegalHorizontalVerticalMoves(rookBitboard & ~(rookBitboard - 1), color, true);
                mask |= PseudoLegalHorizontalVerticalMoves(~(rookBitboard & ~(rookBitboard - 1)) & rookBitboard, color, true);
            }

            // Bishops
            ulong bishopBitboard = bitboards[bishop];
            int bishopPopCount = BitOperations.PopCount(bishopBitboard);
            if (bishopPopCount == 1)
                mask |= PseudoLegalDiagonalMoves(bishopBitboard, color, true);
            else if (bishopPopCount == 2)
            {
                mask |= PseudoLegalDiagonalMoves(bishopBitboard & ~(bishopBitboard - 1), color, true);
                mask |= PseudoLegalDiagonalMoves(~(bishopBitboard & ~(bishopBitboard - 1)) & bishopBitboard, color, true);
            }

            // Knights
            ulong knightBitboard = bitboards[knight];
            int knightPopCount = BitOperations.PopCount(knightBitboard);
            if (knightPopCount == 1)
                mask |= PseudoLegalKnightMoves(knightBitboard, color, true);
            else if (knightPopCount == 2)
            {
                mask |= PseudoLegalKnightMoves(knightBitboard & ~(knightBitboard - 1), color, true);
                mask |= PseudoLegalKnightMoves(~(knightBitboard & ~(knightBitboard - 1)) & knightBitboard, color, true);
            }

            // Queens
            ulong queenBitboard = bitboards[queen];
            int queenPopCount = BitOperations.PopCount(queenBitboard);
            for (int i = 0; i < queenPopCount; i++)
            {
                ulong single = queenBitboard & ~(queenBitboard - 1);
                mask |= PseudoLegalQueenMoves(single, color, true);
                queenBitboard &= ~single;
            }

            // Pawns
            ulong pawnBitboard = bitboards[pawn];
            int pawnPopCount = BitOperations.PopCount(pawnBitboard);
            for (int i = 0; i < pawnPopCount; i++)
            {
                ulong single = pawnBitboard & ~(pawnBitboard - 1);
                mask |= AttackingPawnMoves(single, color);
                pawnBitboard &= ~single;
            }

            // Restore the king's bitboard
            if (!color)
                whitePieces |= savedKing;
            else
                blackPieces |= savedKing;

            opponentBitboards[king] = savedKing;

            return mask;
        }

        // Creates a bitboard containing all the pieces attacking the given piece
        public ulong AttackingPieces(ulong piece, bool color)
        {
            ulong attackingPieces = 0UL;

            ulong[] opponents = color ? blackPieceBitboards : whitePieceBitboards;

            attackingPieces |= opponents[knight] & PseudoLegalKnightMoves(piece, color, false);
            attackingPieces |= opponents[rook] & PseudoLegalHorizontalVerticalMoves(piece, color, false);
            attackingPieces |= opponents[bishop] & PseudoLegalBishopMoves(piece, color, false);
            attackingPieces |= opponents[queen] & PseudoLegalQueenMoves(piece, color, false);
            attackingPieces |= opponents[pawn] & AttackingPawnMoves(piece, color);

            return attackingPieces;
        }

        // Creates a bitboard with the moves that will block the check
        public ulong PushMask(ulong piece, ulong possibleCheckers, bool color)
        {
            // Finds the position of the piece
            int position = (int)Math.Log2(piece);

            // Bitboards of a column and row
            ulong horizontal = 0xFF;
            ulong vertical = 0x0101010101010101;

            // Calculates the row and column of the piece
            int row = position / 8;
            int column = position % 8;

            // Creates a bitboard of a cross with the center at the piece
            ulong cross = (horizontal << row * 8) | (vertical << column);

            // The decimal numbers for two diagonal crosses across the board
            ulong crossTopLeft = 9241421688590303745;
            ulong crossTopRight = 72624976668147840;

            // Calculate how much the program has to shift the top left cross to have it overlap with the piece
            int leftShift = row > column ? row - column : column - row;
            // Whether the program shifts left or right
            bool leftRight = row > column;
            // Creates a mask to prevent wraparound
            ulong leftShiftMask = !leftRight ? 0xFFFFFFFFFFFFFFFFUL >> (leftShift * 8) : 0xFFFFFFFFFFFFFFFF << (leftShift * 8);
            // Applies the mask
            crossTopLeft &= leftShiftMask;

            // Calculate how much the program has to shift the top right cross to have it overlap with the piece
            int rightShift = (row - (8 - column) + 1);
            // Whether to shift left or right
            bool rightRight = rightShift > 0;
            // Creates a mask to prevent wraparound
            ulong rightShiftMask = rightRight ? 0xFFFFFFFFFFFFFFFFUL << (rightShift * 8) : 0xFFFFFFFFFFFFFFFF >> (-rightShift * 8);
            // Applies the mask
            crossTopRight &= rightShiftMask;

            // Shifts the diagonal crosses to overlap with the piece
            ulong crossLeft = !leftRight ? crossTopLeft << leftShift : crossTopLeft >> leftShift;
            ulong crossRight = rightRight ? crossTopRight << rightShift : crossTopRight >> -rightShift;

            // Splits the diagonal crosses into different rays
            ulong topLeft = Bitboard.GetMoreSignificantBits(crossRight, piece) & ~piece;
            ulong topRight = Bitboard.GetMoreSignificantBits(crossLeft, piece) & ~piece;
            ulong bottomLeft = crossLeft & ~topRight & ~piece;
            ulong bottomRight = crossRight & ~topLeft & ~piece;
            // Separates the cross into rays in the four different directions
            ulong leftBitboard = Bitboard.GetLeftBits(piece, cross);
            ulong upBitboard = Bitboard.GetUpBits(piece, cross);
            ulong rightBitboard = Bitboard.GetRightBits(piece, cross);
            ulong downBitboard = Bitboard.GetDownBits(piece, cross);

            ulong[] rays = new ulong[BitOperations.PopCount(possibleCheckers & (topLeft | topRight | bottomLeft | bottomRight | leftBitboard | upBitboard | rightBitboard | downBitboard))];
            int index = 0;

            if ((topLeft & possibleCheckers) != 0UL)
                rays[index++] = Bitboard.GetLessSignificantBits(topLeft, topLeft & possibleCheckers);
            if ((topRight & possibleCheckers) != 0UL)
                rays[index++] = Bitboard.GetLessSignificantBits(topRight, topRight & possibleCheckers);
            if ((bottomLeft & possibleCheckers) != 0UL)
                rays[index++] = Bitboard.GetMoreSignificantBits(bottomLeft, bottomLeft & possibleCheckers);
            if ((bottomRight & possibleCheckers) != 0UL)
                rays[index++] = Bitboard.GetMoreSignificantBits(bottomRight, bottomRight & possibleCheckers);

            if ((upBitboard & possibleCheckers) != 0UL)
                rays[index++] = Bitboard.GetDownBits(upBitboard & possibleCheckers, upBitboard);
            if ((downBitboard & possibleCheckers) != 0UL)
                rays[index++] = Bitboard.GetUpBits(downBitboard & possibleCheckers, downBitboard);
            if ((rightBitboard & possibleCheckers) != 0UL)
                rays[index++] = Bitboard.GetLeftBits(rightBitboard & possibleCheckers, rightBitboard);
            if ((leftBitboard & possibleCheckers) != 0UL)
                rays[index++] = Bitboard.GetRightBits(leftBitboard & possibleCheckers, leftBitboard);

            ulong ret = 0UL;
            for (int i = 0; i < rays.Length; i++)
                ret |= rays[i];

            return ret;
        }

        // Find the pin ray of a piece
        public ulong CalculatePinRay(ulong pinPiece, ulong king, bool color)
        {
            // Finds the position of the piece
            int position = (int)Math.Log2(king);

            // Bitboards of a column and row
            ulong horizontal = 0xFF;
            ulong vertical = 0x0101010101010101;

            // Calculates the row and column of the piece
            int row = position / 8;
            int column = position % 8;

            // Creates a bitboard of a cross with the center at the piece
            horizontal <<= row * 8;
            vertical <<= column;

            if ((horizontal & pinPiece) != 0UL)
                return horizontal;
            if ((vertical & pinPiece) != 0UL)
                return vertical;

            // The decimal numbers for two diagonal crosses across the board
            ulong crossTopLeft = 9241421688590303745;
            ulong crossTopRight = 72624976668147840;

            // Calculate how much the program has to shift the top left cross to have it overlap with the piece
            int leftShift = row > column ? row - column : column - row;
            // Whether the program shifts left or right
            bool leftRight = row > column;
            // Creates a mask to prevent wraparound
            ulong leftShiftMask = !leftRight ? 0xFFFFFFFFFFFFFFFFUL >> (leftShift * 8) : 0xFFFFFFFFFFFFFFFF << (leftShift * 8);
            // Applies the mask
            crossTopLeft &= leftShiftMask;
            ulong crossLeft = !leftRight ? crossTopLeft << leftShift : crossTopLeft >> leftShift;

            if ((crossLeft & pinPiece) != 0UL)
                return crossLeft;

            // Calculate how much the program has to shift the top right cross to have it overlap with the piece
            int rightShift = (row - (8 - column) + 1);
            // Whether to shift left or right
            bool rightRight = rightShift > 0;
            // Creates a mask to prevent wraparound
            ulong rightShiftMask = rightRight ? 0xFFFFFFFFFFFFFFFFUL << (rightShift * 8) : 0xFFFFFFFFFFFFFFFF >> (-rightShift * 8);
            // Applies the mask
            crossTopRight &= rightShiftMask;
            ulong crossRight = rightRight ? crossTopRight << rightShift : crossTopRight >> -rightShift;

            if ((crossRight & pinPiece) != 0UL)
                return crossRight;

            return 0UL;
        }

        // Find the pinned pieces
        public ulong PinnedPieces(ulong king, bool color)
        {
            ulong[] opponentArray = color ? blackPieceBitboards : whitePieceBitboards;

            ulong pinnedPieces = 0UL;

            ulong horizontalKingMoves = PseudoLegalHorizontalMoves(king, color, true);
            ulong verticalKingMoves = PseudoLegalVerticalMoves(king, color, true);
            ulong leftCrossKingMoves = PseudoLegalTopLeftMoves(king, color, true);
            ulong rightCrossKingMoves = PseudoLegalTopRightMoves(king, color, true);

            ulong orthogonalMovers = opponentArray[rook] | opponentArray[queen];
            ulong diagonalMovers = opponentArray[bishop] | opponentArray[queen];

            int popCountOrthogonal = BitOperations.PopCount(orthogonalMovers);
            int popCountDiagonal = BitOperations.PopCount(diagonalMovers);

            ulong moves;

            for (int i = 0; i < popCountOrthogonal; i++)
            {
                ulong single = orthogonalMovers & ~(orthogonalMovers - 1);
                moves = PseudoLegalHorizontalMoves(single, color, true);
                if (BitOperations.PopCount(horizontalKingMoves & moves) == 1)
                    pinnedPieces |= horizontalKingMoves & moves;
                moves = PseudoLegalVerticalMoves(single, color, true);
                if (BitOperations.PopCount(verticalKingMoves & moves) == 1)
                    pinnedPieces |= verticalKingMoves & moves;
                orthogonalMovers &= ~single;
            }

            for (int i = 0; i < popCountDiagonal; i++)
            {
                ulong single = diagonalMovers & ~(diagonalMovers - 1);
                moves = PseudoLegalTopLeftMoves(single, color, true);
                if (BitOperations.PopCount(leftCrossKingMoves & moves) == 1)
                    pinnedPieces |= leftCrossKingMoves & moves;
                moves = PseudoLegalTopRightMoves(single, color, true);
                if (BitOperations.PopCount(rightCrossKingMoves & moves) == 1)
                    pinnedPieces |= rightCrossKingMoves & moves;
                diagonalMovers &= ~single;
            }

            return pinnedPieces;
        }

        // Generates the legal moves for a piece
        public ulong LegalMoves(ulong piece, int type, bool color)
        {
            ulong kingPiece = color ? whitePieceBitboards[king] : blackPieceBitboards[king];

            // If the king's in double check, you must move the king
            if (BitOperations.PopCount(checkingPieces) > 1 && piece != kingPiece)
                return 0UL;

            ulong psuedoLegalMoves = PseudoLegalMove(piece, type, color);

            // If the king isn't in check, allow all legal moves if the piece isn't pinned
            if (checkingPieces == 0UL)
            {
                if (piece == kingPiece)
                    return psuedoLegalMoves & ~attackMask;

                bool pinned = (pins & piece) != 0UL;
                if (!pinned)
                    return psuedoLegalMoves;
                return CalculatePinRay(piece, kingPiece, color) & psuedoLegalMoves;
            }

            // If the king is only being checked by one piece
            else
            {
                // If the king is moving out of check, generate the psuedo legal moves for the king and remove the moves into the attack mask
                if (type == king)
                    return psuedoLegalMoves & ~attackMask;

                // Add taking the piece to the legal move mask
                // Add moves that block the check to the legal move mask
                ulong push = PushMask(kingPiece, checkingPieces, color);
                ulong legalMask = checkingPieces | push;
                // Check whether the piece moving is pinned
                bool pinned = (pins & piece) != 0UL;
                if (pinned)
                {
                    // If the piece is pinned, calculate the ray along which it can move
                    ulong pinRay = CalculatePinRay(piece, kingPiece, color);
                    legalMask &= pinRay & push;
                }
                return psuedoLegalMoves & legalMask;
            }
        }

        // Pre-computes necessary values to generate legal moves
        public void PreComputeMoveValues(bool color)
        {
            ulong kingPiece = color ? whitePieceBitboards[king] : blackPieceBitboards[king];
            pins = PinnedPieces(kingPiece, color);
            attackMask = AttackMask(!color);
            checkingPieces = AttackingPieces(kingPiece, color);
        }

        // All the pseudo legal moves for all the pieces in a given position
        public List<Move> AllPseudoLegalMoves(bool color)
        {
            List<Move> moves = new List<Move>();

            ulong[] bitboards = color ? whitePieceBitboards : blackPieceBitboards;

            // Kings
            ulong kingBitboard = bitboards[king];
            moves.AddRange(Bitboard.SplitBitboard(kingBitboard, PseudoLegalKingMoves(kingBitboard, color, false)));

            // Rooks
            ulong rookBitboard = bitboards[rook];
            int rookPopCount = BitOperations.PopCount(rookBitboard);
            for (int i = 0; i < rookPopCount; i++)
            {
                ulong single = rookBitboard & ~(rookBitboard - 1);
                moves.AddRange(Bitboard.SplitBitboard(single, PseudoLegalRookMoves(single, color, false)));
                rookBitboard &= ~single;
            }

            // Bishops
            ulong bishopBitboard = bitboards[bishop];
            int bishopPopCount = BitOperations.PopCount(bishopBitboard);
            for (int i = 0; i < bishopPopCount; i++)
            {
                ulong single = bishopBitboard & ~(bishopBitboard - 1);
                moves.AddRange(Bitboard.SplitBitboard(single, PseudoLegalBishopMoves(single, color, false)));
                bishopBitboard &= ~single;
            }

            // Knights
            ulong knightBitboard = bitboards[knight];
            int knightPopCount = BitOperations.PopCount(knightBitboard);
            for (int i = 0; i < knightPopCount; i++)
            {
                ulong single = knightBitboard & ~(knightBitboard - 1);
                moves.AddRange(Bitboard.SplitBitboard(single, PseudoLegalKnightMoves(single, color, false)));
                knightBitboard &= ~single;
            }

            // Queens
            ulong queenBitboard = bitboards[queen];
            int queenPopCount = BitOperations.PopCount(queenBitboard);
            for (int i = 0; i < queenPopCount; i++)
            {
                ulong single = queenBitboard & ~(queenBitboard - 1);
                moves.AddRange(Bitboard.SplitBitboard(single, PseudoLegalQueenMoves(single, color, false)));
                queenBitboard &= ~single;
            }

            // Pawns
            ulong pawnBitboard = bitboards[pawn];
            int pawnPopCount = BitOperations.PopCount(pawnBitboard);

            for (int i = 0; i < pawnPopCount; i++)
            {
                ulong single = pawnBitboard & ~(pawnBitboard - 1);
                moves.AddRange(Bitboard.SplitBitboard(single, PseudoLegalPawnMoves(single, color)));
                pawnBitboard &= ~single;
            }

            return moves;
        }


        // All the legal moves for all the pieces in a given position
        public List<Move> AllLegalMoves(bool color)
        {
            PreComputeMoveValues(color);

            List<Move> moves = new List<Move>();

            ulong[] bitboards = color ? whitePieceBitboards : blackPieceBitboards;

            // Kings
            ulong kingBitboard = bitboards[king];
            if (kingBitboard == 0)
            {
                PrintBoard();
                UnMakeMove();
                PrintBoard();
                throw new Exception("No king");
            }

            moves.AddRange(Bitboard.SplitBitboard(kingBitboard, LegalMoves(kingBitboard, king, color)));

            // Rooks
            ulong rookBitboard = bitboards[rook];
            int rookPopCount = BitOperations.PopCount(rookBitboard);
            for (int i = 0; i < rookPopCount; i++)
            {
                ulong single = rookBitboard & ~(rookBitboard - 1);
                moves.AddRange(Bitboard.SplitBitboard(single, LegalMoves(single, rook, color)));
                rookBitboard &= ~single;
            }

            // Bishops
            ulong bishopBitboard = bitboards[bishop];
            int bishopPopCount = BitOperations.PopCount(bishopBitboard);
            for (int i = 0; i < bishopPopCount; i++)
            {
                ulong single = bishopBitboard & ~(bishopBitboard - 1);
                moves.AddRange(Bitboard.SplitBitboard(single, LegalMoves(single, bishop, color)));
                bishopBitboard &= ~single;
            }

            // Knights
            ulong knightBitboard = bitboards[knight];
            int knightPopCount = BitOperations.PopCount(knightBitboard);
            for (int i = 0; i < knightPopCount; i++)
            {
                ulong single = knightBitboard & ~(knightBitboard - 1);
                moves.AddRange(Bitboard.SplitBitboard(single, LegalMoves(single, knight, color)));
                knightBitboard &= ~single;
            }

            // Queens
            ulong queenBitboard = bitboards[queen];
            int queenPopCount = BitOperations.PopCount(queenBitboard);
            for (int i = 0; i < queenPopCount; i++)
            {
                ulong single = queenBitboard & ~(queenBitboard - 1);
                moves.AddRange(Bitboard.SplitBitboard(single, LegalMoves(single, queen, color)));
                queenBitboard &= ~single;
            }

            // Pawns
            ulong pawnBitboard = bitboards[pawn];
            int pawnPopCount = BitOperations.PopCount(pawnBitboard);

            for (int i = 0; i < pawnPopCount; i++)
            {
                ulong single = pawnBitboard & ~(pawnBitboard - 1);
                moves.AddRange(Bitboard.SplitBitboard(single, LegalMoves(single, pawn, color)));
                pawnBitboard &= ~single;
            }

            return moves;
        }

        // Runs a performance test on move generation
        public long Perft(int depth)
        {
            perftCount = 0;
            PerftRecursive(depth);
            return perftCount;
        }

        // Recursive function for the `Perft` function
        public void PerftRecursive(int depth)
        {
            if (depth == 0)
            {
                perftCount++;
                return;
            }
            List<Move> moves = AllLegalMoves(whiteToMove);

            int num = 0;

            foreach (Move move in moves)
            {
                MakeMove(move.start, move.end);
                PerftRecursive(depth - 1);
                UnMakeMove();
                num++;
            }

            if (num == 0)
                perftCount++;
        }

        // Finds the type and color of a piece given a position bitboard
        public int PieceType(ulong piece)
        {
            bool color = (piece & whitePieces) != 0UL;
            ulong[] pieceBitboards = color ? whitePieceBitboards : blackPieceBitboards;
            for (int i = 0; i < total; i++)
            {
                if ((piece & pieceBitboards[i]) != 0UL)
                    return color ? i+1 : -(i+1);
            }

            return 0;
        }

        // Checks whether the board is in a draw or checkmate: 2 for checkmate, 1 for stalemate, 0 for nothing
        public int CheckMates()
        {
            ulong kingPiece = whiteToMove ? whitePieceBitboards[king] : blackPieceBitboards[king];

            if (BitOperations.PopCount(LegalMoves(kingPiece, king, whiteToMove)) == 0)
                if (AllLegalMoves(whiteToMove).Count == 0)
                {
                    return KingChecked() ? 2 : 1;
                }

            return 0;
        }

        // Checks whether the king is in check
        public bool KingChecked()
        {
            ulong kingPiece = whiteToMove ? whitePieceBitboards[king] : blackPieceBitboards[king];
            return (attackMask & kingPiece) != 0UL;
        }

        // Checks for threefold repetition
        public bool ThreefoldRepetition()
        {
            if (zobristHashes.Count < 3)
                return false;
            ulong lastItem = zobristHashes[^1];
            int count = 1;
            for (int i = 0; i < zobristHashes.Count - 1; i++)
            {
                if (zobristHashes[i] == lastItem)
                    count++;
            }
            if (count >= 3)
                return true;
            return false;
        }
    }
}
