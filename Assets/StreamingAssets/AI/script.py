import os
import sys

message = 'ULLDRDRDDRRDR\nULLDRDRUUUUU\nEND'

outfilepath = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.realpath(__file__)) + '/ai-out.txt'

#outfilepath = 'D:/Code Repos/Unity/SokobanAI/Assets/StreamingAssets/AI/out.txt'
#outfilepath = os.path.dirname(os.path.realpath(__file__)) + '/test.txt'

with open(file=outfilepath, mode='w') as f:
    f.write(message)