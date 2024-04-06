import json
import os, sys
import argparse
import numpy as np
import SokobanEnv
import time

class Q_table:
    def __init__(self, board_width: int, board_height: int, action_space: int, alpha, gamma=.99):
        # Creates a multi-dimentional table to be accessed like so:
        # self.table[robotX, robotY, boxX, boxY, action]
        # Each state is represented by a combination of positions (robot and box)
        # (robotX, robotY) is the position of the robot
        # (boxX, boxY) is the position of the box
        # action is the action that can be taken for a given state
        self.table = np.zeros((board_width, board_height, board_width, board_height, action_space))
        # learning rate
        self.alpha = alpha
        # Exploration decay factor
        self.gamma = gamma
    
    def choose_action(self, env: SokobanEnv.SokobanEnv):
        state = map_to_id(env)
        return np.argmax(self.table[state])
    
    def learn(self, state, action, reward, new_state):
        self.table[state][action] += self.alpha * (reward + self.gamma * np.max(self.table[new_state]) - self.table[state][action])

def extract_json(path: str):
    print(f'Extracting {path}...')
    with open(path, 'r') as file:
        data = json.load(file)
    return data

def map_to_id(env):
    player_position = env.find_player_position()
    
    # We will only process 1 box instances for now
    box1_position = env.find_box_positions()[:,0]

    return (player_position[1], player_position[0], box1_position[1], box1_position[0])

def write_out(file_path, line, file_mode='a'):
    fSuccess = False
    while not(fSuccess):
        try:
            with open(file=file_path, mode=file_mode) as f:
                f.write(line)
                fSuccess = True
        except:
            time.sleep(0.01)

def write_actions(actions, episode):
    # Convert actions to letters
    actions_to_letters = ['U', 'R', 'D', 'L']
    actions_letters = [actions_to_letters[a] for a in actions]

    write_out(outfile_path, ''.join(actions_letters) + '\n', 'a' if episode else 'w')

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Q-Learning for Sokoban")

    default_parameters_path = os.path.join(os.path.dirname(os.path.realpath(__file__)), '/parameters.json')
    parser.add_argument("--params-path", help="Path to the parameters file", default=default_parameters_path)

    default_outfile_path = os.path.join(os.path.dirname(os.path.realpath(__file__)), '/ai-out.txt')
    parser.add_argument("--out-path", help="Path to the output file", default=default_outfile_path)
    
    default_levels_path = os.path.join(os.path.dirname(os.path.dirname(os.path.realpath(__file__))), '/Levels/')
    parser.add_argument("--levels-path", help="Path to the levels directory", default=default_levels_path)

    args = parser.parse_args()
    outfile_path = args.out_path
    parameters_path = args.params_path
    levels_path = args.levels_path


    # Loads parameters
    params = extract_json(parameters_path)

    rules_json = params['Rules']
    # 0 -> Reward for empty space
    # 1 -> Reward for moving into wall
    # 2 -> Reward for moving the box
    # 3 -> Reward for winning the game
    # 4 -> Reward for moving the box into a wall

    rules = np.zeros((5))
    for rule in rules_json:
        rules[rule['Action']] = rule['Reward']


    # Load map
    game = SokobanEnv.SokobanEnv(level=params['Level'],
                                 levels_dir=levels_path,
                                 empty_space_reward=rules[0],
                                 wall_reward=rules[1],
                                 cannot_move_box_reward=rules[4],
                                 moved_box_reward=rules[2],
                                 end_of_game_reward=rules[3])
    game.max_steps = 25

    # Create model (needs board width, board height, learning rate)
    q_table = Q_table(game.width, game.height, 4, 1e-3)

    exploration_threshold = params['ExplorationThreshold']
    beta = game.action_space.n / exploration_threshold * (1 / .5 - 1)
    episodes = params['NumGenerations']

    # Model training
    for episode in range(episodes-1):
        actions = []
        game.reset()
        state_id = map_to_id(game)

        total_reward = 0
        epsilon = 1 / (1 + beta * (episode / game.action_space.n)) # Decreasing level of exploration over episodes

        i = 0

        done = False
        while not done:
            # Epsilon-greedy policy
            rand_num = np.random.random()
            if rand_num <= epsilon:
                action = game.action_space.sample() # Random action
            else:
                action = q_table.choose_action(game)
            
            actions.append(action)

            _, reward, done, info = game.step(action)
            new_state_id = map_to_id(game)
            total_reward += reward
            
            q_table.learn(state_id, action, reward, new_state_id)

            state_id = new_state_id
        
        # Write actions in file
        write_actions(actions, episode)
    

    # Inference
    game.reset()
    total_reward = 0
    done = False

    actions = []

    while not done:
        action = q_table.choose_action(game)
        actions.append(action)
        _, reward, done, _ = game.step(action)

    did_win = len(np.where(game.map == 'b')[0]) == 0
    print(f'Can the Model win the game?\t{did_win}')

    # Write actions in file
    write_actions(actions, -1)
    
    write_out(outfile_path, "END")
    print(f'Successfully wrote actions in {outfile_path}')