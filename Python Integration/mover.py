import time, pyautogui, keyboard
import cv2
from mss import mss
from PIL import Image
import numpy as np
import os

print("Move your cursor over the top-left of the chess board, press 'a' when you get there, then do the same for the bottom-right of the screen, then press 's' to start.")

autoRejoin = False # not fully implemented
crossCoords = [1838, 1100]

startingPos = None
endingPos = None
squareSize = 0
textFile = "relay.txt"
start = False
moveNumer = 0
startMove = []
endMove = []
pastPressed = False
currentPressed = False

while True:
    pastPressed = currentPressed
    currentPressed = keyboard.is_pressed('a')
    if currentPressed and not pastPressed:
        if (startingPos == None):
            print("Starting position recorded.")
            startingPos = pyautogui.position()
        else:
            print("Ending position recorded.")
            endingPos = pyautogui.position()
    if endingPos != None and startingPos != None:
        break

squareSize = abs(endingPos[0] - startingPos[0])/8

sct = mss()

file = open(textFile, "w")
file.write("Ready...")
file.close()

flag = False
count = 0
moveArr = []
move = ""
lastMove = []

while True:
    if os.path.getsize(textFile) == 0 and not flag and keyboard.is_pressed("s"):
        print("Started")
        flag = True
    if (flag):
        count += 1
        img = pyautogui.screenshot(region=(startingPos[0], startingPos[1], endingPos[0]-startingPos[0], endingPos[1]-startingPos[1]))
        frame = np.array(img)
        frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        moveArr = []
        for i in range(0, 8):
            for j in range(0, 8):
                if (245 in frame[int(i*squareSize + squareSize/6), int(j*squareSize + squareSize/6)] or 185 in frame[int(i*squareSize + squareSize/6), int(j*squareSize + squareSize/6)]):
                    moveArr.append([j, i])
        if (count == 1 and len(moveArr) == 0):
            file = open(textFile, "r+")
            file.write("white")
            file.close()
            color = True
        elif (count == 1 and len(moveArr) == 2):
            file = open(textFile, "r+")
            file.write("black")
            file.close()
            color = False

        if (count == 1):
            read = color
        
        if (read):
            if os.path.getsize(textFile) != 0:
                try:
                    file = open(textFile, "r+")
                    move = file.read()
                    file.close()
                except:
                    move = ""
                if (move != "white" and move != "black" and move != "" and move[0] == "W"):
                    time.sleep(0.05)
                    pyautogui.moveTo(int(move[1]) * squareSize + startingPos[0] + squareSize/2, int(move[2]) * squareSize + startingPos[1] + squareSize/2)
                    pyautogui.click()
                    time.sleep(0.05)
                    pyautogui.moveTo(int(move[3]) * squareSize + startingPos[0] + squareSize/2, int(move[4]) * squareSize + startingPos[1] + squareSize/2)
                    lastMove = [[int(move[1]), int(move[2])], [int(move[3]), int(move[4])]]
                    pyautogui.click()
                    time.sleep(0.05)
                    read = not read
                    while (True):
                        try:
                            file = open(textFile, "w")
                            move = file.write("white")
                            file.close()
                            break
                        except:
                            pass
    
        else:
            if (len(moveArr) == 2 and (lastMove != moveArr and lastMove != [moveArr[1], moveArr[0]])):
                try:
                    file = open(textFile, "w+")
                    if (file.read() != "white" or file.read() != ""):
                        file.write("R" + str(moveArr[0][0]) + str(moveArr[0][1]) + str(moveArr[1][0]) + str(moveArr[1][1]))
                    file.close()
                    read = not read
                except:
                    pass
        

        if (cv2.waitKey(1) & 0xFF) == ord('q') or keyboard.is_pressed('q'):
            cv2.destroyAllWindows()
            break
            