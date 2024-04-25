# SokobanAI
AI powered Sokoban game made in 2024 for KF7004 MComp Research Project, a University module and final Masters project. It is built in Unity Engine with the addition of Python scripts.

This game is a non-traditional version of the classic game of Sokoban, where the player must push boxes onto targets in a simple 2D grid map. In this game, the player doesn't directly control the robot representing the player. Instead, you create rules to be given to a machine learning model, which powers the robot.

## Masters Reseach Project
The purpose of the game was to support a research project titled "Evaluating Game-Based Learning Against the Traditional Lecture in Teaching Basics of AI". This project was undertaken by myself alongside [Vincent Briat](https://github.com/vincentbriat) over the course of our Masters year. It intended to compare the effectiveness of games as a vehicle to teach technical knowledge to laypeople with that of traditional lecture-based teaching. We had undertaken a study in which we asked some participants to watch a lecture explaining the basics of aritificial intelligence and machine learning, others to play this game (teaching the same knowledge), and comparing results from both via a follow-up questionnaire.

## Machine Learning
The Sokoban player character is powered by a simple Q-learning algorithm written in python and executed as a process by the game using the bundled Python interpreter. The scripts were written by Vincent and integrated into the game by me.

The Q-learning algorithm requires a set of rules to train with, which determine the rewards it gets for situations it achieves. The main principal of the game is for the player to set this set of rules. Upon pressing 'Play', the rules are written to a file, which is read by the AI to determine its rules. In addition to rules, in much the same way, the number of generations and the exploration threshold are set for the AI. The algorithm then runs the training process for the model, starting from scratch on every play. The player can see the generations play out within the game, as the game shows the latest run the model did until the end, where it shows the trained model's attempt and evaluates, whether it managed to complete the given level.

## Teaching
Designed from ground up with teaching in mind, this game leads the player through 8 levels exploring the concepts behind AI and ML, particularly training, generations, exploration threshold (randomness), agent and environment, and Q-values. The game does it by focusing on one concept at a time each level, building up the player's knowledge throughout the game.

## Other Features
In addition to the base functionality explained above, the game offers a level picker, a way to save and load rulesets, option to load a set of rules to solve a level for stuck players, and a way to change the playback speed (including pausing the playback).
