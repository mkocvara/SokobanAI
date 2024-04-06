import numpy as np
import gym
from gym import spaces
import sys
import time
import os
# from IPython import display


class SokobanEnv(gym.Env):

    def __init__(self,
                 level=1,
                 levels_dir=f'../Levels/',
                 empty_space_reward=-.1,
                 wall_reward = -3,
                 cannot_move_box_reward=-1,
                 moved_box_reward=1,
                 end_of_game_reward=10):
        
        super(SokobanEnv, self).__init__()
        
        self.levels_dir = levels_dir
        self.setup_level(level)

        self.height, self.width = self.map.shape
        self.player_position = self.find_player_position()
        self.action_space = spaces.Discrete(4)  # Four actions: 0=up, 1=down, 2=left, 3=right
        self.observation_space = spaces.Box(low=0, high=3, shape=(self.height, self.width), dtype=np.uint8)
        self.steps = 0
        self.max_steps = 100  # Define maximum number of steps per episode
        self.memory = []
        self.use_memory = True
        self.target_position = [1, 1]

        # Rewards
        self.REWARD_EMPTY_SPACE = empty_space_reward
        self.REWARD_MOVE_TO_WALL = wall_reward
        self.REWARD_CANNOT_MOVE_BOX = cannot_move_box_reward
        self.REWARD_MOVED_BOX = moved_box_reward
        self.REWARD_END_OF_GAME = end_of_game_reward

        

    def map_to_str(self, map):
      return "\n".join(''.join(row) for row in map) + "\n"
    
    def char_to_int(self):
        # Define mapping from characters to integers
        return {
            '.': 0,  # Empty space
            '#': 1,  # Wall
            'x': 2,  # Target location for boxes
            'p': 3,  # Player
            'b': 4,  # Box,
            '!': 5   # Target location reached (box on top)
        }
    
    def map_to_int(self, map):
        return np.array([[self.char_to_int()[char] for char in row] for row in map])
        

    def map_to_float(self, map):
        # Normalize values
        max_value = max(self.char_to_int().values())
        char_to_float = {char: value / max_value for char, value in self.char_to_int().items()}

        # Convert each character in the map to its corresponding integer value
        float_map = [[char_to_float[char] for char in row] for row in map]

        return np.array(float_map)

    def setup_level(self, level: int):
        if level < 1:
            print("Level must be greater or equal to 1.")
            return
        if self.load_map_txt(os.path.join(self.levels_dir, str(level))):
            self.level = level

    def find_player_position(self):
        return np.array(np.where(self.map == 'p'))[:, 0]
    
    def find_box_positions(self):
        return np.array(np.where(np.logical_or(self.map == 'b', self.map == '!')))
    
    def number_achieved_targets(self):
        return len(np.where(self.map == '!')[0])

    def step(self, action):
        done = False
        self.steps += 1
        reward = 0
        new_player_position = self.player_position.copy()

        if action == 0:  # Move up
            new_player_position -= [1, 0]
        elif action == 1:  # Move right
            new_player_position += [0, 1]
        elif action == 2:  # Move down
            new_player_position += [1, 0]
        elif action == 3:  # Move left
            new_player_position -= [0, 1]

        # Check if the new position is valid
        if self.map[tuple(new_player_position)] == '#':  # Wall
            reward = self.REWARD_MOVE_TO_WALL
        elif self.map[tuple(new_player_position)] in ['.', 'x']:  # Empty space
            self.map[tuple(self.player_position)] = '.'
            self.map[tuple(new_player_position)] = 'p'
            self.player_position = new_player_position
            reward = self.REWARD_EMPTY_SPACE
        elif self.map[tuple(new_player_position)] == 'b':  # Box
            box_new_position = new_player_position + (new_player_position - self.player_position)
            if self.map[tuple(box_new_position)] in ['#', 'b']:  # Box cannot be moved
                reward = self.REWARD_CANNOT_MOVE_BOX
            elif self.map[tuple(box_new_position)] == 'x': # Box reached the right destination
                self.map[tuple(self.player_position)] = '.'
                self.map[tuple(new_player_position)] = 'p'
                self.map[tuple(box_new_position)] = '!'
                reward = self.REWARD_END_OF_GAME
                done = True
            else:
                self.map[tuple(self.player_position)] = '.'
                self.map[tuple(new_player_position)] = 'p'
                self.map[tuple(box_new_position)] = 'b'
                self.player_position = new_player_position
                reward = self.REWARD_MOVED_BOX
        
        # Rewrites the targets if they've been
        # overwritten by the passage of the robot
        self.fix_targets()

        done = done or self.steps >= self.max_steps  # Game is done if the player reaches the target or maximum steps reached

        if self.use_memory:
          self.memory.append(self.map.copy())

        return self.map_to_float(self.map), reward, done, {}
    
    def fix_targets(self):
        tile = self.map[self.target_position[0], self.target_position[1]]
        if tile == '.':
            self.map[self.target_position[0], self.target_position[1]] = 'x'
    
    def up(self):
        self.step(0)
    
    def right(self):
        self.step(1)
    
    def down(self):
        self.step(2)
    
    def left(self):
        self.step(3)

    def reset(self):
        #self.load_map_txt(os.path.join(self.levels_dir, str(self.level)))
        self.map = self.map_basestate.copy()
        self.player_position = self.find_player_position()
        self.steps = 0
        self.memory = [self.map.copy()]
        return self.map_to_float(self.map)

    # def render(self, mode='human'):
    #     if mode == 'human':
    #         display.clear_output(wait=True)
    #         print(self.map_to_str(self.map))
    #     else:
    #         outfile = sys.stdout
    #         outfile.write(self.map_to_str(self.map))
    #         return outfile

    # def play_memory(self, framerate=2):
    #     for i, map in enumerate(self.memory):
    #         display.clear_output(wait=True)
    #         print(f'Frame {i+1}/{len(self.memory)}')
    #         print("\n".join(''.join(row) for row in map) + "\n")
    #         if i != len(self.memory)-1:
    #           time.sleep(1/framerate)
    #     print("Replay over!")

    # Loads a game recorded on a txt file in memory
    def load_game_txt(self, filepath):
        with open(filepath, "r") as f:
            data = f.read()
            data = data.split('\n-\n')
            if data:
              self.reset()
              self.memory = []
            for map_str in data:
                if map_str:
                  map = map_str.split('\n')
                  self.memory.append(np.array([list(row) for row in map]))

    def load_map_txt(self, filepath):
        try:
            print(f'Loading level from file: {filepath}')
            with open(filepath, "r") as f:
                map_str = f.read()
                _map = map_str.split('\n')
                map_start = _map.index('M')
                instr_start = _map.index('I')
                _map = _map[map_start+1:instr_start]
                
                _map = [list(row.replace(' ', '.')) for row in _map]
                width = len(max(_map, key = len))
                _map = [row + ['.'] * (width-len(row)) if len(row) < width else row for row in _map]

                self.map = np.array(_map)
                self.map_basestate = self.map.copy()
                
            success = True
        except FileNotFoundError:
            print(f"Cannot find level with filepath:\t{filepath}:")
            success = False
        return success

    # Save a game in memory on a txt file
    def save_game_txt(self, filepath):
        if not self.memory:
            print(f"The memory is empty, nothing was saved in {filepath}")
        with open(filepath, "w") as f:
            for map in self.memory:
              f.write(self.map_to_str(map))
              f.write("-\n")
        print(f"The last game was successfully saved in {filepath}")

    # def save_map_txt(self, filepath):
    #     with open(filepath, "w") as f:
    #         f.write(self.map_to_str(self.map))