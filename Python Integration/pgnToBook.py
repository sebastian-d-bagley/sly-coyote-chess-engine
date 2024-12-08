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
    if (move[i:i+4] == "11. "):
        reading = False
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

def chess_moves_to_coordinates(moves_string):
    # Create a new chess board object
    board = chess.Board()

    # Split the input string into a list of moves
    moves = moves_string.split()

    # Convert each move to its corresponding coordinate move
    coord_moves = []
    for move in moves:
        # Parse the SAN move to a Move object
        san_move = chess.Move.from_uci(board.parse_san(move).uci())
        # Get the starting and ending squares of the move
        start_square = san_move.from_square
        end_square = san_move.to_square
        # Convert the squares to coordinates
        start_coord = (start_square % 8, 7 - start_square // 8)
        end_coord = (end_square % 8, 7 - end_square // 8)
        # Add the coordinate move to the list
        coord_moves.append(start_coord + end_coord)

        # Make the move on the board
        board.push(san_move)

    return coord_moves

book = open("Opening Book.txt", "a")

with open('stage2.txt') as file:
    for line in file:
        book.write(str(chess_moves_to_coordinates(line)) + "\n")

open('stage1.txt', 'w').close()
open('stage2.txt', 'w').close()
