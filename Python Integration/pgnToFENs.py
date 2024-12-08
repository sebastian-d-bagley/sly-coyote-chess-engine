textFile = "pgn.txt"

file = open(textFile, "r+")
move = file.read()
file.close()

book = open("stage1.txt", "a")

reading = False
pastReading = False

for i in range(1, len(move)):
    pastReading = reading
    if (move[i-1:i+3] == "\n1. "):
        reading = True
    elif (move[i:i+2] == "0-" or move[i:i+2] == "1-" or move[i:i+2] == "1/"):
        reading = False

    if (reading) and move[i] != "\n":
        book.write(move[i])
    if (reading and move[i] == "\n"):
        book.write(" ")
    if (not reading and pastReading):
        book.write("\n")


book = open("stage2.txt", "a")

with open('stage1.txt') as file:
    for line in file:
        arr = line.split()
        lenth = len(arr)
        for i in range(len(arr)):
            if (i >= len(arr)):
                break
            if "." in arr[i]:
                arr.pop(i)
                i -= 1
        for item in arr:
            book.write(item + " ")
        book.write("\n")

book.close()
file.close()

import chess

def chess_moves_to_fen(moves_string):
    # Create a new chess board object
    board = chess.Board()

    # Split the input string into a list of moves
    moves = moves_string.split()

    # Convert each move to its corresponding coordinate move
    FENs = []
    for move in moves:
        # Parse the SAN move to a Move object
        san_move = chess.Move.from_uci(board.parse_san(move).uci())
        FENs.append(board.fen())

        # Make the move on the board
        board.push(san_move)

    return FENs

FENList = open("Dataset.txt", "a")

with open('stage2.txt') as file:
    for line in file:
        thing = chess_moves_to_fen(line)
        for item in thing:
            FENList.write(item + "\n")

open('stage1.txt', 'w').close()
open('stage2.txt', 'w').close()
