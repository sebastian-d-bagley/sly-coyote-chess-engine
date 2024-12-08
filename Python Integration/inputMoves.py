import time, pyautogui, keyboard

print("Move your cursor over the top-left of the chess board, press 'a' when you get there, then do the same for the bottom-right of the screen. Press 's' when you are ready to begin.")

startingPos = None
endingPos = None
squareSize = 0
textFile = "moves.txt"
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
        squareSize = abs(endingPos[0] - startingPos[0])/8
        if (keyboard.is_pressed('s')):
            start = True
    if (start):
        fp = open(textFile)
        for i, line in enumerate(fp):
            if i == moveNumer:
                moveNumer += 1
                startMove = [int(line[1]), int(line[4])]
                endMove =  [int(line[-6]), int(line[-3])]
                pyautogui.moveTo(startingPos[0] + (squareSize * startMove[0]) + (squareSize/2), startingPos[1] + (squareSize * startMove[1]) + (squareSize/2))
                time.sleep(0.1)
                pyautogui.click()
                time.sleep(0.1)
                pyautogui.moveTo(startingPos[0] + (squareSize * endMove[0]) + (squareSize/2), startingPos[1] + (squareSize * endMove[1]) + (squareSize/2))
                time.sleep(0.1)
                pyautogui.click()
                time.sleep(0.1)
                pyautogui.click()
        fp.close()

            