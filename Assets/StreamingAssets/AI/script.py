import os
import sys
import time

messages =  ['ULLDRDRDDRRDR',\
           '\nDRRURUUUDLL',\
           '\nLRLRLRLDUUD',\
           '\nLLLUUURRRDDD',\
           '\nULLDRDRUUUUU',\
           '\nEND']

outfilepath = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.realpath(__file__)) + '/ai-out.txt'

#outfilepath = 'D:/Code Repos/Unity/SokobanAI/Assets/StreamingAssets/AI/out.txt'
#outfilepath = os.path.dirname(os.path.realpath(__file__)) + '/test.txt'

firstTime = True
for m in messages:
    fileMode = 'w' if firstTime else 'a'
    firstTime = False
    fSuccess = False
    while not(fSuccess):
        try:
            with open(file=outfilepath, mode=fileMode) as f:
                f.write(m)
                f.close()
                fSuccess = True
        except:
            time.sleep(0.01)
    time.sleep(3)