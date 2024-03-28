import os
import sys

message = 'Hello World!'
# filepath = 'D:/Code Repos/Unity/SokobanAI/Assets/StreamingAssets/AI/out.txt'
filepath = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.realpath(__file__)) + '\\out.txt'
# filepath = os.path.dirname(os.path.realpath(__file__)) + '\\test.txt'

with open(file=filepath, mode='w') as f:
    f.write(message)

# print(f'"{message}" written in "{filepath}"')