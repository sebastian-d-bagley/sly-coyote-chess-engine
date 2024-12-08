using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Chess_Fixed
{
    public class Game1 : Game
    {
        // CONFIG

        bool FLIP = false;
        int MODE = 6; // 1 for anarchy, 2 for player vs player, 3 for player vs computer, 4 for computer vs computer, 5 for stockfish vs. computer, 6 for computer vs. chess.com, 7 for computer vs. computer weight tuning
        // for mode 6, you must first run 'main.py' in the Python Integration folder, and then run this (after setting the corners). Once the game has started all you have to do is press 's' (if you are black press 's' after white moves).
        // for mode 6, you must also have the green themed board, coordinates off, no animation type, highlight moves, and white must always be on the bottom. I use classic pieces, but I'm not sure if it matters
        bool COMPCOLOR = false;
        int ANIMTICKS = 10;
        bool UI = false;
        bool TIMECONTROLS = true;
        int STARTINGTIME = 60000;
        int REGAINED = -550;

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        Texture2D boardSprite;
        Texture2D pawnSpriteW;
        Texture2D pawnSpriteB;
        Texture2D rookSpriteW;
        Texture2D rookSpriteB;
        Texture2D knightSpriteW;
        Texture2D knightSpriteB;
        Texture2D bishopSpriteW;
        Texture2D bishopSpriteB;
        Texture2D queenSpriteW;
        Texture2D queenSpriteB;
        Texture2D kingSpriteW;
        Texture2D kingSpriteB;
        Texture2D square;
        Texture2D whiteCircle;
        Texture2D blackCircle;
        Texture2D whiteTakingCircle;
        Texture2D blackTakingCircle;
        Texture2D circle;
        Texture2D smallCircle;
        Texture2D whiteRect;
        Texture2D blackRect;

        SpriteFont font;
        SpriteFont profFont;
        SpriteFont small;
        SpriteFont clock;

        int animCounter = 0;
        bool anim = false;
        int[] pos;
        bool heldDown = false;
        bool pastHeldDown = false;
        bool clicked = false;
        int[] dragPiece = new int[] { 9, 9 };
        bool drag = false;
        bool stop = false;
        string dragRenderVal;
        int highPiece;
        MouseState mouseState;
        int index;
        int[][] compMove;
        int slider = 0;
        int[] sliderLoc;
        int moves = 0;
        int mate = -1;
        List<string> threefold = new List<string>();
        int wait = 0;
        int[][] lastMove = null;
        int[] start = null;
        KeyboardState oldState = Keyboard.GetState();
        List<double> time = new List<double>();
        int remainingWhite;
        int remainingBlack;

        // test position: r1b1k2r/pp3p2/2p5/3p2p1/1Q1Pn2p/B1P1P3/P3BqPP/2RK3R b - - 0 1
        // Plugged in Best Time: depth 4: 97 ms, depth 5: 420 ms, depth 6: 1410
        // Battery Power best time: depth 5: 701 ms, depth 6: 2858 ms

        //test position 2: r1b1k1nr/p1ppqpb1/Bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPB1PPP/R3K2R b KQkq - 0 1
        // Plugged in Best Time: depth 4: 105 ms, depth 5: 364 ms, depth 6: 1977
        // Battery Power Best Time: depth 5: 1951 ms

        Chess_Game game = new Chess_Game();
        List<int[]> legalMoves = new List<int[]>();
        bool pastMove = false;
        bool setupPython = false;
        string text = null;
        string pastText = null;
        bool read = false;
        string path;
        string sCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        bool lastTime = false;
        int count = 0;
        Stopwatch timer;
        bool pastColor = false;
        bool color = true;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            color = game.color;
            pastColor = color;
            SlyCoyote.ZobristHash();
            remainingBlack = STARTINGTIME;
            remainingWhite = STARTINGTIME;
            if (MODE == 7)
            {
                SlyCoyoteTuning.PlayGames(100, 50);
                while (true) { }
            }
            _graphics.IsFullScreen = false;
            if (TIMECONTROLS)
                _graphics.PreferredBackBufferWidth = 900;
            else
                _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 800;
            _graphics.ApplyChanges();
            base.Initialize();
            sCurrentDirectory = sCurrentDirectory.Substring(0, sCurrentDirectory.Length - 25);
            //path = sCurrentDirectory + "/Python Integration/relay.txt";
            path = @"C:\Users\Sebastian Bagley\source\repos\Chess Fixed\Chess Fixed\Python Integration\relay.txt";
            if (MODE == 6)
            {
                while (true)
                {
                    try
                    {
                        string[] temp = File.ReadAllLines(path);

                        string temporary = "";
                        foreach (string line in temp)
                        {
                            temporary += line;
                        }
                        if (temporary != "Ready...")
                            throw new Exception("Python file not running");
                        File.WriteAllText(path, String.Empty);
                        break;
                    }
                    catch
                    {

                    }
                }
            }
            //SlyCoyote.LoadBook(sCurrentDirectory + "/Python Integration/Opening Book.txt");
            SlyCoyote.LoadBook(@"C:\Users\Sebastian Bagley\source\repos\Chess Fixed\Chess Fixed\Python Integration\Opening Book.txt");

            ulong input = SlyCoyote.GetBitboard(game.board);
            double[] output = new double[65];

            for (int i = 0; i < 64; i++)
            {
                output[i] = (input & (1UL << i)) != 0 ? 1.0 : 0.0;
            }
            if (game.color)
                output[64] = 1;
            else
                output[64] = 0;
            //Debug.WriteLine(SlyCoyote.Predict(output));
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            boardSprite = Content.Load<Texture2D>("board");
            pawnSpriteW = Content.Load<Texture2D>("white pawn");
            pawnSpriteB = Content.Load<Texture2D>("black pawn");
            rookSpriteW = Content.Load<Texture2D>("white rook");
            rookSpriteB = Content.Load<Texture2D>("black rook");
            knightSpriteW = Content.Load<Texture2D>("white knight");
            knightSpriteB = Content.Load<Texture2D>("black knight");
            bishopSpriteW = Content.Load<Texture2D>("white bishop");
            bishopSpriteB = Content.Load<Texture2D>("black bishop");
            queenSpriteW = Content.Load<Texture2D>("white queen");
            queenSpriteB = Content.Load<Texture2D>("black queen");
            kingSpriteW = Content.Load<Texture2D>("white king");
            kingSpriteB = Content.Load<Texture2D>("black king");
            square = Content.Load<Texture2D>("red square");
            whiteCircle = Content.Load<Texture2D>("White Circle");
            blackCircle = Content.Load<Texture2D>("Black Circle");
            whiteTakingCircle = Content.Load<Texture2D>("White Taking Circle");
            blackTakingCircle = Content.Load<Texture2D>("Black Taking Circle");
            circle = Content.Load<Texture2D>("circle");
            smallCircle = Content.Load<Texture2D>("cocentric circles");
            whiteRect = Content.Load<Texture2D>("white square");
            blackRect = Content.Load<Texture2D>("black square");

            font = Content.Load<SpriteFont>("galleryFont");
            profFont = Content.Load<SpriteFont>("professionalFont");
            small = Content.Load<SpriteFont>("smaller");
            clock = Content.Load<SpriteFont>("timer");
        }

        protected override void Update(GameTime gameTime)
        {
            if (!UI)
            {
                pastColor = color;
                color = game.color;
                if (count == 0)
                {
                    timer = new Stopwatch();
                    timer.Start();
                }
                count++;
            }
            if (MODE == 6)
            {
                try
                {
                    string[] temp = File.ReadAllLines(path);
                    text = "";
                    foreach (string line in temp)
                    {
                        text += line;
                    }
                }
                catch
                {
                    text = null;
                }
            }
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                Exit();

            KeyboardState newState = Keyboard.GetState();
            if (MODE == 6)
            {
                if (!setupPython && (text != null && text != ""))
                {
                    if (text == "white")
                        COMPCOLOR = true;
                    else
                        COMPCOLOR = false;
                    setupPython = true;
                    read = COMPCOLOR;
                    lastTime = !COMPCOLOR;
                    File.WriteAllText(path, String.Empty);
                }
                if (setupPython && text != pastText)
                {
                    if (text != "white" && text != "black" && COMPCOLOR != game.color && text != "" && text != null && lastTime && text[0] == 'R' && text != "RESET" && (game.Mate() != 1 && game.Mate() != 0))
                    {
                        int[][] tempMove = new int[][] { new int[] { text[1] - '0', text[2] - '0' }, new int[] { text[3] - '0', text[4] - '0' } };
                        if (game.board[tempMove[0][1]][tempMove[0][0]] == 0)
                            game.Move(tempMove[1], tempMove[0]);
                        else if (game.board[tempMove[1][1]][tempMove[1][0]] == 0)
                            game.Move(tempMove[0], tempMove[1]);
                        else if (game.board[tempMove[1][1]][tempMove[1][0]] > 0 != COMPCOLOR)
                            game.Move(tempMove[1], tempMove[0]);
                        else
                            game.Move(tempMove[0], tempMove[1]);
                        File.WriteAllText(path, String.Empty);
                        lastTime = false;
                    }
                    if (game.color == COMPCOLOR && text != "white" && text == "" && !lastTime && text != "RESET" && (game.Mate() != 1 && game.Mate() != 0))
                    {
                        int[][] bestMove;
                        var watch = new Stopwatch();
                        watch.Start();
                        if (game.color)
                            bestMove = SlyCoyote.GenMove(game, remainingWhite, REGAINED);
                        else
                            bestMove = SlyCoyote.GenMove(game, remainingBlack, REGAINED);
                        watch.Stop();
                        if (game.color)
                        {
                            remainingWhite -= (int)watch.ElapsedMilliseconds;
                            remainingWhite += REGAINED;
                        }
                        else
                        {
                            remainingBlack -= (int)watch.ElapsedMilliseconds;
                            remainingBlack += REGAINED;
                        }
                        game.Move(bestMove[0], bestMove[1]);
                        pastText = bestMove[0][0].ToString() + bestMove[0][1].ToString() + bestMove[1][0].ToString() + bestMove[1][1].ToString();
                        File.WriteAllText(path, "W" + pastText);
                        lastTime = true;
                    }
                    if (text == "RESET")
                    {
                        File.WriteAllText(path, String.Empty);
                        game = new Chess_Game();
                        setupPython = false;
                    }
                }
            }
            oldState = newState;
            if (stop)
            {
                Debug.WriteLine("Average time per move: " + time.Sum() / time.Count + " ms");
                Debug.WriteLine("Total number of moves: " + time.Count);
                Debug.WriteLine("Total time of game: " + time.Sum() + " ms");
                Debug.WriteLine("Moves: ");
                for (int i = 0; i < game.moveArr.Count; i++)
                {
                    Debug.WriteLine("(" + game.moveArr[i][0][0] + ", " + game.moveArr[i][0][1] + ") to (" + game.moveArr[i][1][0] + ", " + game.moveArr[i][1][1] + ")");
                }
                Thread.Sleep(5000);
                UI = true;
                stop = false;
                game = new Chess_Game();
                SlyCoyote.ZobristHash();
                mate = -1;
                remainingWhite = STARTINGTIME;
                remainingBlack = STARTINGTIME;
                count = 0;
            }
            IsFixedTimeStep = false;
            if (!anim && !UI)
            {
                drag = false;
                pastHeldDown = heldDown;
                compMove = null;
                clicked = false;
                mouseState = Mouse.GetState();
                pos = new int[] { mouseState.X, mouseState.Y };
                heldDown = false;
                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    heldDown = true;
                }
                if (!pastHeldDown && heldDown)
                {
                    clicked = true;
                    dragPiece = new int[] { (int)(mouseState.X) / 100, (int)(mouseState.Y) / 100 };
                    dragPiece = Chess_Game.FlipCoords(dragPiece, FLIP);
                    start = new int[] { dragPiece[0], dragPiece[1] };
                    if (dragPiece[0] < 8 && dragPiece[0] > -1 && dragPiece[1] < 8 && dragPiece[1] > -1)
                        legalMoves = game.LegalMoves(dragPiece);
                    drag = true;
                    if (index != -1)
                    {
                        try
                        {
                            highPiece = game.board[dragPiece[1]][dragPiece[0]];
                        }
                        catch
                        {
                            highPiece = 0;
                        }
                        if (highPiece != 0)
                        {
                            if (highPiece > 0)
                            {
                                dragRenderVal = "w" + game.GetType(dragPiece);
                            }
                            else
                            {
                                dragRenderVal = "b" + game.GetType(dragPiece);
                            }
                        }
                        else
                            dragPiece = new int[] { -1, -1 };
                    }
                }
                else if (!heldDown && pastHeldDown)
                    dragPiece = null;
                if (pastHeldDown && !heldDown && highPiece != 0)
                {
                    dragPiece = new int[] { (int)(mouseState.X) / 100, (int)(mouseState.Y) / 100 };
                    dragPiece = Chess_Game.FlipCoords(dragPiece, FLIP);
                    if (!(start[0] == dragPiece[0] && start[1] == dragPiece[1]))
                    {
                        bool flag = false;
                        List<int[]> temp = game.LegalMoves(start);
                        for (int i = 0; i < temp.Count; i++)
                        {
                            if (temp[i][0] == dragPiece[0] && temp[i][1] == dragPiece[1])
                                flag = true;
                        }
                        if (!stop && (MODE == 1 || (MODE == 2 && flag && game.color == (highPiece > 0)) || (MODE == 3 && game.color == !COMPCOLOR && game.color == highPiece > 0 && flag)))
                        {
                            lastMove = new int[][] { start, dragPiece };
                            if (game.color)
                                remainingWhite += REGAINED;
                            else
                                remainingBlack += REGAINED;
                            game.Move(start, dragPiece);
                            pastMove = true;
                            mate = game.Mate();

                            highPiece = 0;
                            moves++;
                            dragPiece = null;
                        }
                    }
                    else
                    {
                        highPiece = 0;
                        dragPiece = null;
                    }
                }
                else if (!stop && ((MODE == 3 || MODE == 5) && game.color == COMPCOLOR))
                {
                    // the depth at which the computer generates moves
                    if (game.color)
                        compMove = SlyCoyote.GenMove(game, remainingWhite, REGAINED);
                    else
                        compMove = SlyCoyote.GenMove(game, remainingBlack, REGAINED);
                    slider = game.board[compMove[0][1]][compMove[0][0]];
                    sliderLoc = new int[] { compMove[0][0] * 100 + ((compMove[1][0] * 100 - compMove[0][0] * 100) * animCounter) / ANIMTICKS, compMove[0][1] * 100 + ((compMove[1][1] * 100 - compMove[0][1] * 100) * animCounter) / ANIMTICKS };
                    anim = true;
                    animCounter = 0;
                    if (FLIP)
                    {
                        sliderLoc = new int[] { 700 - sliderLoc[0], 700 - sliderLoc[1] };
                    }
                }
                else if (MODE == 4 && wait > 0 && !stop)
                {
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    if (game.color)
                        compMove = SlyCoyote.GenMove(game, remainingWhite, REGAINED);
                    else
                        compMove = SlyCoyote.GenMove(game, remainingBlack, REGAINED);
                    stopwatch.Stop();
                    time.Add(stopwatch.ElapsedMilliseconds);
                    slider = game.board[compMove[0][1]][compMove[0][0]];
                    sliderLoc = new int[] { compMove[0][0] * 100 + ((compMove[1][0] * 100 - compMove[0][0] * 100) * animCounter) / ANIMTICKS, compMove[0][1] * 100 + ((compMove[1][1] * 100 - compMove[0][1] * 100) * animCounter) / ANIMTICKS };
                    anim = true;
                    animCounter = 0;
                    if (FLIP)
                    {
                        sliderLoc = new int[] { 700 - sliderLoc[0], 700 - sliderLoc[1] };
                    }
                    wait = 0;
                }
            }
            else if (compMove != null && !UI)
            {
                sliderLoc = new int[] { compMove[0][0] * 100 + ((compMove[1][0] * 100 - compMove[0][0] * 100) * animCounter) / ANIMTICKS, compMove[0][1] * 100 + ((compMove[1][1] * 100 - compMove[0][1] * 100) * animCounter) / ANIMTICKS };
                if (FLIP)
                {
                    sliderLoc = new int[] { 700 - sliderLoc[0], 700 - sliderLoc[1] };
                }
                animCounter++;
                if (animCounter == ANIMTICKS)
                {
                    anim = false;
                    slider = 0;
                    animCounter = 0;
                    if (game.color)
                        remainingWhite += REGAINED;
                    else
                        remainingBlack += REGAINED;
                    game.Move(compMove[0], compMove[1]);
                    lastMove = new int[][] { compMove[0], compMove[1] };
                    mate = game.Mate();
                    moves++;
                    compMove = null;
                }
            }
            else if (UI)
            {
                _graphics.PreferredBackBufferWidth = 250;
                _graphics.PreferredBackBufferHeight = 320;
                _graphics.ApplyChanges();

                pastHeldDown = heldDown;
                mouseState = Mouse.GetState();
                heldDown = false;
                if (mouseState.LeftButton == ButtonState.Pressed)
                    heldDown = true;
                pos = new int[] { mouseState.X, mouseState.Y };

                if (heldDown && !pastHeldDown)
                {
                    if (pos[1] > 45 && pos[1] <= 65)
                    {
                        COMPCOLOR = !COMPCOLOR;
                        FLIP = COMPCOLOR;
                    }
                    else if (pos[1] > 65 && pos[1] <= 85)
                    {
                        if (MODE == 4)
                            MODE = 3;
                        else
                            MODE = 4;
                    }
                    else if (pos[1] > 85 && pos[1] <= 105)
                        TIMECONTROLS = !TIMECONTROLS;
                    else if (pos[1] > 125 && pos[1] <= 145)
                        STARTINGTIME = 60000;
                    else if (pos[1] > 145 && pos[1] <= 165)
                        STARTINGTIME = 180000;
                    else if (pos[1] > 165 && pos[1] <= 185)
                        STARTINGTIME = 300000;
                    else if (pos[1] > 185 && pos[1] <= 205)
                        STARTINGTIME = 600000;
                    else if (pos[1] > 225 && pos[1] <= 245)
                        REGAINED = 0;
                    else if (pos[1] > 245 && pos[1] <= 265)
                        REGAINED = 2000;
                    else if (pos[1] > 265 && pos[1] <= 285)
                        REGAINED = 5000;
                    else if (pos[1] > 290 && pos[1] < 305 && pos[0] > 60 && pos[0] < 240)
                    {
                        UI = false;
                        if (TIMECONTROLS)
                            _graphics.PreferredBackBufferWidth = 900;
                        else
                            _graphics.PreferredBackBufferWidth = 800;
                        _graphics.PreferredBackBufferHeight = 800;
                        _graphics.ApplyChanges();
                    }
                    remainingBlack = STARTINGTIME;
                    remainingWhite = STARTINGTIME;
                    if (!TIMECONTROLS)
                    {
                        REGAINED = -1;
                    }
                }
            }
            wait++;
            base.Update(gameTime);
            if ((count == 30 || color != pastColor) && MODE != 6)
            {
                timer.Stop();
                if (game.color)
                    remainingWhite -= (int)timer.ElapsedMilliseconds;
                else
                    remainingBlack -= (int)timer.ElapsedMilliseconds;
                count = 0;
            }
        }
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();
            _spriteBatch.Draw(boardSprite, new Vector2(0, 0), Color.White);
            if (dragPiece != null && start != null && start[0] > -1 && start[0] < 8 && start[1] > -1 && start[1] < 8 && dragRenderVal != "69" && dragRenderVal != null && heldDown && game.board[start[1]][start[0]] != 0)
            {
                foreach (int[] item in legalMoves)
                {
                    if (!FLIP)
                    {
                        if ((item[0] + item[1]) % 2 == 0)
                        {
                            if (game.board[item[1]][item[0]] == 0)
                                _spriteBatch.Draw(whiteCircle, new Vector2(item[0] * 100, item[1] * 100), Color.White);
                            else
                                _spriteBatch.Draw(whiteTakingCircle, new Vector2(item[0] * 100, item[1] * 100), Color.White);
                        }
                        else
                        {
                            if (game.board[item[1]][item[0]] == 0)
                                _spriteBatch.Draw(blackCircle, new Vector2(item[0] * 100, item[1] * 100), Color.White);
                            else
                                _spriteBatch.Draw(blackTakingCircle, new Vector2(item[0] * 100, item[1] * 100), Color.White);
                        }
                    }
                    else
                    {
                        int index1 = -1;
                        int index2 = -1;
                        if ((item[0] + item[1]) % 2 == 0)
                        {
                            index1 = item[0];
                            index2 = item[1];
                            item[0] = 7 - item[0];
                            item[1] = 7 - item[1];
                            if (game.board[index2][index1] == 0)
                                _spriteBatch.Draw(whiteCircle, new Vector2(item[0] * 100, item[1] * 100), Color.White);
                            else
                                _spriteBatch.Draw(whiteTakingCircle, new Vector2(item[0] * 100, item[1] * 100), Color.White);
                            item[0] = 7 - item[0];
                            item[1] = 7 - item[1];
                        }
                        else
                        {
                            index1 = item[0];
                            index2 = item[1];
                            item[0] = 7 - item[0];
                            item[1] = 7 - item[1];
                            if (game.board[index2][index1] == 0)
                                _spriteBatch.Draw(blackCircle, new Vector2(item[0] * 100, item[1] * 100), Color.White);
                            else
                                _spriteBatch.Draw(blackTakingCircle, new Vector2(item[0] * 100, item[1] * 100), Color.White);
                            item[0] = 7 - item[0];
                            item[1] = 7 - item[1];
                        }
                    }
                }
            }
            if (lastMove != null)
            {
                _spriteBatch.Draw(square, new Vector2(Chess_Game.FlipCoords(lastMove[0], FLIP)[0] * 100, Chess_Game.FlipCoords(lastMove[0], FLIP)[1] * 100), Color.Red);
                _spriteBatch.Draw(square, new Vector2(Chess_Game.FlipCoords(lastMove[1], FLIP)[0] * 100, Chess_Game.FlipCoords(lastMove[1], FLIP)[1] * 100), Color.Red);
            }
            for (int x = 0; x < game.board.Length; x++)
            {
                for (int y = 0; y < game.board.Length; y++)
                {
                    if (dragPiece == null || !heldDown || (game.board[y][x] != 0 && (x != dragPiece[0] || y != dragPiece[1])))
                    {
                        if (anim && compMove[0][0] == x && compMove[0][1] == y)
                            continue;
                        int thing = game.board[y][x];
                        if (FLIP)
                        {
                            x = 7 - x;
                            y = 7 - y;
                        }
                        if (thing == 1)
                            _spriteBatch.Draw(pawnSpriteW, new Vector2(x * 100, y * 100), Color.White);
                        else if (thing == 2)
                            _spriteBatch.Draw(rookSpriteW, new Vector2(x * 100, y * 100), Color.White);
                        else if (thing == 3)
                            _spriteBatch.Draw(knightSpriteW, new Vector2(x * 100, y * 100), Color.White);
                        else if (thing == 4)
                            _spriteBatch.Draw(bishopSpriteW, new Vector2(x * 100, y * 100), Color.White);
                        else if (thing == 5)
                            _spriteBatch.Draw(kingSpriteW, new Vector2(x * 100, y * 100), Color.White);
                        else if (thing == 6)
                            _spriteBatch.Draw(queenSpriteW, new Vector2(x * 100, y * 100), Color.White);
                        else if (thing == -1)
                            _spriteBatch.Draw(pawnSpriteB, new Vector2(x * 100, y * 100), Color.White);
                        else if (thing == -2)
                            _spriteBatch.Draw(rookSpriteB, new Vector2(x * 100, y * 100), Color.White);
                        else if (thing == -3)
                            _spriteBatch.Draw(knightSpriteB, new Vector2(x * 100, y * 100), Color.White);
                        else if (thing == -4)
                            _spriteBatch.Draw(bishopSpriteB, new Vector2(x * 100, y * 100), Color.White);
                        else if (thing == -5)
                            _spriteBatch.Draw(kingSpriteB, new Vector2(x * 100, y * 100), Color.White);
                        else if (thing == -6)
                            _spriteBatch.Draw(queenSpriteB, new Vector2(x * 100, y * 100), Color.White);
                        if (FLIP)
                        {
                            x = 7 - x;
                            y = 7 - y;
                        }

                    }
                }
            }

            if (dragPiece != null && start != null && start[0] > -1 && start[0] < 8 && start[1] > -1 && start[1] < 8 && dragRenderVal != "69" && dragRenderVal != null && heldDown && game.board[start[1]][start[0]] != 0)
            {

                if (dragRenderVal[0] == 'w')
                {
                    if (dragRenderVal[3] == 'o')
                        _spriteBatch.Draw(rookSpriteW, new Vector2(pos[0] - 50, pos[1] - 50), Color.White);
                    else if (dragRenderVal[3] == 'w')
                        _spriteBatch.Draw(pawnSpriteW, new Vector2(pos[0] - 50, pos[1] - 50), Color.White);
                    else if (dragRenderVal[3] == 's')
                        _spriteBatch.Draw(bishopSpriteW, new Vector2(pos[0] - 50, pos[1] - 50), Color.White);
                    else if (dragRenderVal[3] == 'i')
                        _spriteBatch.Draw(knightSpriteW, new Vector2(pos[0] - 50, pos[1] - 50), Color.White);
                    else if (dragRenderVal[3] == 'e')
                        _spriteBatch.Draw(queenSpriteW, new Vector2(pos[0] - 50, pos[1] - 50), Color.White);
                    else
                        _spriteBatch.Draw(kingSpriteW, new Vector2(pos[0] - 50, pos[1] - 50), Color.White);
                }
                else
                {
                    if (dragRenderVal[3] == 'o')
                        _spriteBatch.Draw(rookSpriteB, new Vector2(pos[0] - 50, pos[1] - 50), Color.White);
                    else if (dragRenderVal[3] == 'w')
                        _spriteBatch.Draw(pawnSpriteB, new Vector2(pos[0] - 50, pos[1] - 50), Color.White);
                    else if (dragRenderVal[3] == 's')
                        _spriteBatch.Draw(bishopSpriteB, new Vector2(pos[0] - 50, pos[1] - 50), Color.White);
                    else if (dragRenderVal[3] == 'i')
                        _spriteBatch.Draw(knightSpriteB, new Vector2(pos[0] - 50, pos[1] - 50), Color.White);
                    else if (dragRenderVal[3] == 'e')
                        _spriteBatch.Draw(queenSpriteB, new Vector2(pos[0] - 50, pos[1] - 50), Color.White);
                    else
                        _spriteBatch.Draw(kingSpriteB, new Vector2(pos[0] - 50, pos[1] - 50), Color.White);
                }
            }
            if (anim)
            {
                if (Math.Abs(slider) == 1)
                {
                    if (slider > 0)
                        _spriteBatch.Draw(pawnSpriteW, new Vector2(sliderLoc[0], sliderLoc[1]), Color.White);
                    else
                        _spriteBatch.Draw(pawnSpriteB, new Vector2(sliderLoc[0], sliderLoc[1]), Color.White);
                }
                else if (Math.Abs(slider) == 2)
                {
                    if (slider > 0)
                        _spriteBatch.Draw(rookSpriteW, new Vector2(sliderLoc[0], sliderLoc[1]), Color.White);
                    else
                        _spriteBatch.Draw(rookSpriteB, new Vector2(sliderLoc[0], sliderLoc[1]), Color.White);
                }
                else if (Math.Abs(slider) == 4)
                {
                    if (slider > 0)
                        _spriteBatch.Draw(bishopSpriteW, new Vector2(sliderLoc[0], sliderLoc[1]), Color.White);
                    else
                        _spriteBatch.Draw(bishopSpriteB, new Vector2(sliderLoc[0], sliderLoc[1]), Color.White);
                }
                else if (Math.Abs(slider) == 3)
                {
                    if (slider > 0)
                        _spriteBatch.Draw(knightSpriteW, new Vector2(sliderLoc[0], sliderLoc[1]), Color.White);
                    else
                        _spriteBatch.Draw(knightSpriteB, new Vector2(sliderLoc[0], sliderLoc[1]), Color.White);
                }
                else if (Math.Abs(slider) == 6)
                {
                    if (slider > 0)
                        _spriteBatch.Draw(queenSpriteW, new Vector2(sliderLoc[0], sliderLoc[1]), Color.White);
                    else
                        _spriteBatch.Draw(queenSpriteB, new Vector2(sliderLoc[0], sliderLoc[1]), Color.White);
                }
                else
                {
                    if (slider > 0)
                        _spriteBatch.Draw(kingSpriteW, new Vector2(sliderLoc[0], sliderLoc[1]), Color.White);
                    else
                        _spriteBatch.Draw(kingSpriteB, new Vector2(sliderLoc[0], sliderLoc[1]), Color.White);
                }
            }
            if (!FLIP)
            {
                if (game.color)
                {
                    _spriteBatch.Draw(whiteRect, new Vector2(812, 670), Color.White);
                    _spriteBatch.Draw(whiteRect, new Vector2(812, 680), Color.White);
                }
                else
                {
                    _spriteBatch.Draw(whiteRect, new Vector2(812, 90), Color.White);
                    _spriteBatch.Draw(whiteRect, new Vector2(812, 100), Color.White);
                }
                if (remainingBlack < 20000)
                    _spriteBatch.DrawString(clock, SlyCoyote.IntToString(remainingBlack), new Vector2(816, 90), Color.Red);
                else
                    _spriteBatch.DrawString(clock, SlyCoyote.IntToString(remainingBlack), new Vector2(816, 90), Color.Gray);
                if (remainingWhite < 20000)
                    _spriteBatch.DrawString(clock, SlyCoyote.IntToString(remainingWhite), new Vector2(816, 680), Color.Red);
                else
                    _spriteBatch.DrawString(clock, SlyCoyote.IntToString(remainingWhite), new Vector2(816, 680), Color.Gray);
            }
            else
            {
                if (!game.color)
                {
                    _spriteBatch.Draw(whiteRect, new Vector2(812, 670), Color.White);
                    _spriteBatch.Draw(whiteRect, new Vector2(812, 680), Color.White);
                }
                else
                {
                    _spriteBatch.Draw(whiteRect, new Vector2(812, 90), Color.White);
                    _spriteBatch.Draw(whiteRect, new Vector2(812, 100), Color.White);
                }
                if (remainingBlack < 20000)
                    _spriteBatch.DrawString(clock, SlyCoyote.IntToString(remainingBlack), new Vector2(816, 680), Color.Red);
                else
                    _spriteBatch.DrawString(clock, SlyCoyote.IntToString(remainingBlack), new Vector2(816, 680), Color.Gray);
                if (remainingWhite < 20000)
                    _spriteBatch.DrawString(clock, SlyCoyote.IntToString(remainingWhite), new Vector2(816, 90), Color.Red);
                else
                    _spriteBatch.DrawString(clock, SlyCoyote.IntToString(remainingWhite), new Vector2(816, 90), Color.Gray);
            }
            if (mate == 1)
            {
                if (game.color)
                {
                    _spriteBatch.DrawString(font, "LOOKS LIKE BLACK WON", new Vector2(35, 350), Color.Red);
                    stop = true;
                    Debug.WriteLine("black won");
                }

                else
                {
                    _spriteBatch.DrawString(font, "LOOKS LIKE WHITE WON", new Vector2(42, 350), Color.Red);
                    stop = true;
                    Debug.WriteLine("white won");
                }
            }
            else if (mate == 0)
            {
                _spriteBatch.DrawString(font, "YOU STALEMATED", new Vector2(130, 350), Color.Red);
                stop = true;
                Debug.WriteLine("stalemate");
            }
            if (game.ThreefoldRepetition())
            {
                _spriteBatch.DrawString(font, "              DRAW BY \nTHREEFOLD REPETITION", new Vector2(50, 300), Color.Red);
                stop = true;
                Debug.WriteLine("threefold");
            }
            if (remainingWhite <= 0 && TIMECONTROLS && !UI && MODE != 6)
            {
                _spriteBatch.DrawString(font, "BLACK WINS BY TIMEOUT", new Vector2(15, 350), Color.Red);
                stop = true;
                remainingWhite = 0;
                Debug.WriteLine(remainingWhite);
            }
            if (remainingBlack <= 0 && TIMECONTROLS && !UI && MODE != 6)
            {
                _spriteBatch.DrawString(font, "WHITE WINS BY TIMEOUT", new Vector2(15, 350), Color.Red);
                stop = true;
                remainingBlack = 0;
                Debug.WriteLine("white by timeout");
            }
            if (UI)
            {
                _spriteBatch.End();
                _spriteBatch.Begin();
                GraphicsDevice.Clear(Color.White);
                _spriteBatch.DrawString(profFont, "Chess Game Setup", new Vector2(10, 15), Color.Black);

                _spriteBatch.DrawString(small, "Play with white", new Vector2(25, 45), Color.Black);
                _spriteBatch.Draw(circle, new Vector2(10, 48), Color.White);
                if (!COMPCOLOR)
                    _spriteBatch.Draw(smallCircle, new Vector2(10, 48), Color.White);


                _spriteBatch.DrawString(small, "Computer vs. computer", new Vector2(25, 65), Color.Black);
                _spriteBatch.Draw(circle, new Vector2(10, 68), Color.White);
                if (MODE == 4)
                    _spriteBatch.Draw(smallCircle, new Vector2(10, 68), Color.White);

                _spriteBatch.DrawString(small, "Use time controls", new Vector2(25, 85), Color.Black);
                _spriteBatch.Draw(circle, new Vector2(10, 88), Color.White);
                if (TIMECONTROLS)
                    _spriteBatch.Draw(smallCircle, new Vector2(10, 88), Color.White);

                _spriteBatch.DrawString(small, "Starting time: ", new Vector2(10, 105), Color.Black);

                _spriteBatch.DrawString(small, "1 minute", new Vector2(30, 125), Color.Black);
                _spriteBatch.Draw(circle, new Vector2(15, 128), Color.White);
                if (STARTINGTIME == 60000)
                    _spriteBatch.Draw(smallCircle, new Vector2(15, 128), Color.White);

                _spriteBatch.DrawString(small, "3 minutes", new Vector2(30, 145), Color.Black);
                _spriteBatch.Draw(circle, new Vector2(15, 148), Color.White);
                if (STARTINGTIME == 180000)
                    _spriteBatch.Draw(smallCircle, new Vector2(15, 148), Color.White);

                _spriteBatch.DrawString(small, "5 minutes", new Vector2(30, 165), Color.Black);
                _spriteBatch.Draw(circle, new Vector2(15, 168), Color.White);
                if (STARTINGTIME == 300000)
                    _spriteBatch.Draw(smallCircle, new Vector2(15, 168), Color.White);

                _spriteBatch.DrawString(small, "10 minutes", new Vector2(30, 185), Color.Black);
                _spriteBatch.Draw(circle, new Vector2(15, 188), Color.White);
                if (STARTINGTIME == 600000)
                    _spriteBatch.Draw(smallCircle, new Vector2(15, 188), Color.White);

                _spriteBatch.DrawString(small, "Time regained per move: ", new Vector2(10, 205), Color.Black);

                _spriteBatch.DrawString(small, "0 seconds", new Vector2(30, 225), Color.Black);
                _spriteBatch.Draw(circle, new Vector2(15, 228), Color.White);
                if (REGAINED == 0)
                    _spriteBatch.Draw(smallCircle, new Vector2(15, 228), Color.White);

                _spriteBatch.DrawString(small, "2 seconds", new Vector2(30, 245), Color.Black);
                _spriteBatch.Draw(circle, new Vector2(15, 248), Color.White);
                if (REGAINED == 2000)
                    _spriteBatch.Draw(smallCircle, new Vector2(15, 248), Color.White);

                _spriteBatch.DrawString(small, "5 seconds", new Vector2(30, 265), Color.Black);
                _spriteBatch.Draw(circle, new Vector2(15, 268), Color.White);
                if (REGAINED == 5000)
                    _spriteBatch.Draw(smallCircle, new Vector2(15, 268), Color.White);

                _spriteBatch.DrawString(small, "Click here to start", new Vector2(60, 290), Color.Black);
            }
            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}