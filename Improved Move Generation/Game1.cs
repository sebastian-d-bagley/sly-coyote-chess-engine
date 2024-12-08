using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Move_Generation;
using Color = Microsoft.Xna.Framework.Color;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Sly_Scorpion
{
    public class Game1 : Game
    {
        // Configuration
        private const bool Mirrored = false;

        private const bool ComputerColor = false;

        private const string PreloadedBoard = "";//r1bqkb1r/pppn1ppp/3p1n2/4p3/3PPP2/2N5/PPP3PP/R1BQKBNR w KQkq - 0 1";//""r3kb1r/4pppp/p1ppbn2/6B1/4P3/2NB4/PqP2PPP/R2QK2R w KQkq - 0 1";

        private const float AnimationTicks = 10;

        private const int Mode = 3; // 1 is computer vs. person, 2 is person vs. person. 3 for computer vs. computer
        // End of configuration

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Texture2D _boardSprite;
        
        private Texture2D _pawnSpriteW;
        private Texture2D _rookSpriteW;
        private Texture2D _knightSpriteW;
        private Texture2D _bishopSpriteW;
        private Texture2D _queenSpriteW;
        private Texture2D _kingSpriteW;

        private Texture2D _pawnSpriteB;
        private Texture2D _rookSpriteB;
        private Texture2D _knightSpriteB;
        private Texture2D _bishopSpriteB;
        private Texture2D _queenSpriteB;
        private Texture2D _kingSpriteB;

        private Texture2D[] _whitePieceSprites;
        private Texture2D[] _blackPieceSprites;

        private Texture2D _highlightedSquareTexture;
        private Texture2D _whiteCircle;
        private Texture2D _blackCircle;
        private Texture2D _whiteTakingCircle;
        private Texture2D _blackTakingCircle;

        private Chess _chess;
        private Chess_Engine _chessEngine;

        private bool _isClicked = false;
        private bool _pastClicked = false;

        private int[] _highlightedSquare = null;
        private ulong _highlightedBitboard = 0UL;
        private ulong _legalMoves = 0UL;

        private bool _animation = false;
        private int[] _startCoordinates = null;
        private int[] _endCoordinates = null;
        private Vector2 _currentLocation;
        private Vector2 _velocityVector;
        private ulong _animatedPiece = 0UL;
        private ulong _endSquare = 0UL;


        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            if (PreloadedBoard == "")
                _chess = new();
            else
                _chess = new(PreloadedBoard);

            _chessEngine = new Chess_Engine(_chess);
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 800;
            _graphics.ApplyChanges();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _boardSprite = Content.Load<Texture2D>("board");

            _pawnSpriteW = Content.Load<Texture2D>("white pawn");
            _rookSpriteW = Content.Load<Texture2D>("white rook");
            _knightSpriteW = Content.Load<Texture2D>("white knight");
            _bishopSpriteW = Content.Load<Texture2D>("white bishop");
            _queenSpriteW = Content.Load<Texture2D>("white queen");
            _kingSpriteW = Content.Load<Texture2D>("white king");

            _pawnSpriteB = Content.Load<Texture2D>("black pawn");
            _rookSpriteB = Content.Load<Texture2D>("black rook");
            _knightSpriteB = Content.Load<Texture2D>("black knight");
            _bishopSpriteB = Content.Load<Texture2D>("black bishop");
            _queenSpriteB = Content.Load<Texture2D>("black queen");
            _kingSpriteB = Content.Load<Texture2D>("black king");

            _whiteCircle = Content.Load<Texture2D>("White Circle");
            _blackCircle = Content.Load<Texture2D>("Black Circle");
            _whiteTakingCircle = Content.Load<Texture2D>("White Taking Circle");
            _blackTakingCircle = Content.Load<Texture2D>("Black Taking Circle");

            _whitePieceSprites = new[] { _pawnSpriteW, _rookSpriteW, _knightSpriteW, _bishopSpriteW, _queenSpriteW, _kingSpriteW };
            _blackPieceSprites = new[] { _pawnSpriteB, _rookSpriteB, _knightSpriteB, _bishopSpriteB, _queenSpriteB, _kingSpriteB };

            _highlightedSquareTexture = Content.Load<Texture2D>("red square");
        }

        protected override void Update(GameTime gameTime)
        {
            IsFixedTimeStep = false;

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var mouseState = Mouse.GetState();

            _pastClicked = _isClicked;

            _isClicked = false || mouseState.LeftButton == ButtonState.Pressed;

            if (!_pastClicked && _isClicked)
            {
                int x = mouseState.X / 100;
                int y = mouseState.Y / 100;

                if (!_animation && (_chess.whiteToMove != ComputerColor || Mode == 2) && Mode != 3)
                {
                    // If you click on the same square
                    if (_highlightedSquare != null && x == _highlightedSquare[0] && y == _highlightedSquare[1])
                    {
                        _highlightedSquare = null;
                        _legalMoves = 0UL;
                    }

                    else
                    {
                        ulong square = (ulong)Math.Pow(2, (7 - y) * 8 + x);
                        int type = _chess.PieceType(_highlightedBitboard);

                        if (_highlightedSquare == null && _chess.PieceType(square) != 0 && _chess.PieceType(square) > 0 == _chess.whiteToMove)
                        {
                            _chess.PreComputeMoveValues(_chess.whiteToMove);
                            _highlightedBitboard = square;
                            _highlightedSquare = new int[] { x, y };
                            _legalMoves = _chess.LegalMoves(_highlightedBitboard,
                                Math.Abs(_chess.PieceType(square)) - 1, _chess.whiteToMove);
                        }
                        else if ((_legalMoves & square) != 0UL && type > 0 == _chess.whiteToMove)
                        {
                            _animatedPiece = _highlightedBitboard;
                            _animation = true;
                            _startCoordinates = new int[] { _highlightedSquare[0] * 100, _highlightedSquare[1] * 100 };
                            _endCoordinates = new int[] { x * 100, y * 100 };
                            _currentLocation = new Vector2(_startCoordinates[0], _startCoordinates[1]);
                            _velocityVector = new Vector2(-(_startCoordinates[0] - _endCoordinates[0]) / AnimationTicks,
                                -(_startCoordinates[1] - _endCoordinates[1]) / AnimationTicks);
                            _endSquare = square;
                        }
                        else
                        {
                            _highlightedBitboard = 0UL;
                            _highlightedSquare = null;
                            _legalMoves = 0UL;
                        }
                    }
                }
            }
            else if (!_animation && ((_chess.whiteToMove == ComputerColor && Mode == 1) || Mode == 3))
            {
                _chessEngine.alphabeta = !_chess.whiteToMove;
                _chessEngine.otherTransposition = _chess.whiteToMove;
                Move move = _chessEngine.BestMove(_chess, 3000);
                if (move != null)
                {
                    _highlightedBitboard = move.start;
                    _endSquare = move.end;
                    _animatedPiece = move.start;
                    _animation = true;
                    _startCoordinates = new int[] { xCoord(move.start) * 100, yCoord(move.start) * 100 };
                    _endCoordinates = new int[] { xCoord(move.end) * 100, yCoord(move.end) * 100 };
                    _currentLocation = new Vector2(_startCoordinates[0], _startCoordinates[1]);
                    _velocityVector = new Vector2(-(_startCoordinates[0] - _endCoordinates[0]) / AnimationTicks,
                        -(_startCoordinates[1] - _endCoordinates[1]) / AnimationTicks);
                    _endSquare = move.end;
                }
                else
                {
                    while (true) { }
                    Exit();
                }

            }
            if (_animation)
            {
                _currentLocation += _velocityVector;
                if (MathF.Abs(_currentLocation.X - _endCoordinates[0]) <= MathF.Abs(_velocityVector.X) &&
                    MathF.Abs(_currentLocation.Y - _endCoordinates[1]) <= MathF.Abs(_velocityVector.Y))
                {
                    _animation = false;
                    _chess.MakeMove(_highlightedBitboard, _endSquare);

                    _highlightedBitboard = 0UL;
                    _highlightedSquare = null;
                    _legalMoves = 0UL;
                }
            }

            base.Update(gameTime);
        }

        private int xCoord(ulong u)
        {
            return (int)Math.Log2(u) % 8;
        }

        private int yCoord(ulong u)
        {
            return 7 - (int)Math.Log2(u) / 8;
        }

        private void DrawPiece(ulong pieces, Texture2D texture, bool mirrored)
        {
            int numberOfPieces = BitOperations.PopCount(pieces);
            for (int i = 0; i < numberOfPieces; i++)
            {
                ulong single = pieces & ~(pieces - 1);

                if ((single & _animatedPiece) != 0UL && _animation)
                {
                    pieces &= ~single;
                    continue;
                }


                int position = (int)Math.Log2(single);
                int row = 7 - position / 8;
                int column = position % 8;

                if (mirrored)
                {
                    row = 7 - row;
                    column = 7 - column;
                }

                _spriteBatch.Draw(texture, new Vector2(column * 100, row * 100), Color.White);

                pieces &= ~single;
            }
        }

        private void DrawLegalMoves()
        {
            if (_legalMoves == 0UL)
                return;
            int totalMoves = BitOperations.PopCount(_legalMoves);

            ulong copy = _legalMoves;

            for (int i = 0; i < totalMoves; i++)
            {
                ulong single = copy & ~(copy - 1);
                if ((single & _animatedPiece) != 0UL && _animation)
                {
                    copy &= ~single;
                    continue;
                }
                int position = (int)Math.Log2(single);
                int row = 7 - position / 8;
                int column = position % 8;

                if (Mirrored)
                {
                    row = 7 - row;
                    column = 7 - column;
                }

                if ((single & (_chess.whitePieces | _chess.blackPieces)) == 0UL)
                    _spriteBatch.Draw(((row+column) % 2 == 0) ? _whiteCircle : _blackCircle, new Vector2(column * 100, row * 100), Color.White);
                else
                    _spriteBatch.Draw(((row + column) % 2 == 0) ? _whiteTakingCircle : _blackTakingCircle, new Vector2(column * 100, row * 100), Color.White);

                copy &= ~single;
            }
        }

        private void DrawAnimatedPiece()
        {
            int type = _chess.PieceType(_animatedPiece);
            if (type == 0)
                return;
            bool color = type > 0;
            int pieceType = Math.Abs(type) - 1;
            Texture2D[] textureList = color ? _whitePieceSprites : _blackPieceSprites;
            _spriteBatch.Draw(textureList[pieceType], _currentLocation, Color.White);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Wheat);
            _spriteBatch.Begin();

            _spriteBatch.Draw(_boardSprite, new Vector2(0, 0), Color.White);

            DrawLegalMoves();

            if (_highlightedSquare != null)
                _spriteBatch.Draw(_highlightedSquareTexture, new Vector2(_highlightedSquare[0]*100, _highlightedSquare[1]*100), Color.White);

            if (_animation)
                DrawAnimatedPiece();

            for (int i = _chess.pawn; i <= _chess.king; i++)
            {
                DrawPiece(_chess.whitePieceBitboards[i], _whitePieceSprites[i], Mirrored);
                DrawPiece(_chess.blackPieceBitboards[i], _blackPieceSprites[i], Mirrored);
            }

            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
