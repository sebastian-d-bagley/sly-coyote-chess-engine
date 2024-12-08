import chess
import chess.engine, time
engine = chess.engine.SimpleEngine.popen_uci("C:/Users/sebas/source/repos/Chess/Chess/stockfish_20090216_x64.exe")

def stockfish_evaluation(board, time_limit=0.01):
    global engine
    result = engine.analyse(board, chess.engine.Limit(time=time_limit))
    score = result["score"].relative.score() # Convert to centipawns
    if score != None:
        if board.turn:
            return score / 100 # Convert to pawns
        else:
            return score/-100
    else:
        stringer = result['score'].__repr__()
        if "Mate" in stringer:
            ret = ""
            for i in range(len(stringer)):
                if stringer[i] == "(" and stringer[i-1] == "e" and stringer[i-2] == "t":
                    for j in range(i+1, len(stringer)):
                        if stringer[j] == ")":
                            break
                        ret += stringer[j]
                    if ret[0:2] == "-0":
                        return 1000
                    elif ret[0:2] == "+0":
                        return -1000
                    if ret[0] == "+" and board.turn:
                        return 750/abs(int(ret))
                    elif ret[0] == "-" and board.turn:
                        return -750/abs(int(ret))
                    elif ret[0] == "+" and not board.turn:
                        return -750/abs(int(ret))
                    elif ret[0] == "-" and not board.turn:
                        return 750/abs(int(ret))

chunk_size = 1000 # number of lines to process at once
with open('Dataset.txt') as infile, open("Dataset With Eval.txt", "w") as outfile:
    chunk = []
    for count, line in enumerate(infile):
        chunk.append(line.strip())
        if len(chunk) == chunk_size:
            for i, line in enumerate(chunk):
                try:
                    board = chess.Board(line)
                    score = stockfish_evaluation(board)
                    outfile.write(line + " " + str(score) + "\n")
                    del board
                except Exception as e:
                    print(f"Error processing line {count*chunk_size+i}: {e}")
            chunk = []
    # process remaining lines (less than chunk_size)
    for i, line in enumerate(chunk):
        try:
            board = chess.Board(line)
            score = stockfish_evaluation(board)
            outfile.write(line + " " + str(score) + "\n")
        except Exception as e:
            print(f"Error processing line {count*chunk_size+i}: {e}")
    del chunk # delete chunk to free up memory
