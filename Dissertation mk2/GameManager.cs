using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dissertation_mk2
{
    public class GameManager
    {
        public Board board;
        public bool gameOver;

        public List<Enemy> enemies = new List<Enemy>();
        public List<Ally> allies = new List<Ally>();
        public List<List<int>> allyTargetPositions = new List<List<int>>();

        //For modelling flow
        private List<int> anxietyEachTurn = new List<int>();
        public int cSkill;
        private int anxiety;
        private int cDecay = 2;
        private bool noEnemiesNear = true;

        //For level ranking
        private int turnCount;
        public int numItems;

        //For GA
        private GA geneticAlg;
        private Solution currentSolution;

        /*
        public GameManager(GA geneticAlg, double pValue, int cSkill)
        {
            this.geneticAlg = geneticAlg;
            this.cSkill = cSkill;
            board = new Board(this, pValue);
            InitGame();
            currentSolution = new Solution(board, board.markov.Personality, board.markov.pValue, numItems, enemies.Count);
            PlayGame();
        }
        */

        public GameManager(GA geneticAlg, Solution solution, int cSkill)
        {
            this.geneticAlg = geneticAlg;
            this.cSkill = cSkill;
            board = solution.boardObj;
            board.gameManager = this;
            currentSolution = solution;
            for (int i = 0; i < board.numEnemies; i++)
            {
                enemies.Add(board.enemies[i]);
                allies.Add(board.allies[i]);
            }
            InitGame();
        }

        public void PlayGame()
        {
            Console.WriteLine("enemy count " + enemies.Count);
            Console.WriteLine("Ally Count " + allies.Count);
            while (!gameOver)
            {
                turnCount++;
                if (cSkill > 0)
                    allies = allies.OrderByDescending(ally => ally.hp).ToList();
                foreach (var ally in allies.Where(ally => !ally.isDead))
                {
                    if (gameOver) continue;
                    ally.engaged = false;
                    ally.TakeTurn();
                }

                Ally[] tempAlly = allies.ToArray();
                foreach (var ally in tempAlly)
                {
                    ally.EndTurn();
                }

                allyTargetPositions.Clear();
                foreach (var enemy in enemies.Where(enemy => !enemy.isDead))
                {
                    if (gameOver) continue;
                    enemy.engaged = false;
                    enemy.TakeTurn();
                }

                Enemy[] tempEnemy = enemies.ToArray();
                foreach (var enemy in tempEnemy)
                {
                    enemy.EndTurn();
                }

                if (enemies.Count == 0)
                    cDecay = 1;
                Console.WriteLine(Builder(board.board));
                if (gameOver)
                    continue;
                UpdateAnxiety();
                Console.WriteLine(anxiety);
                if (turnCount % 5 == 0)
                    board.markov.Transition();
                noEnemiesNear = true;
            }
        }

        private void InitGame()
        {
            board.enemies.Clear();
            board.allies.Clear();
            Console.WriteLine(Builder(board.board));
        }

        private static string Builder(IEnumerable<List<float>> board)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var row in board)
            {
                foreach (var tile in row)
                {
                    builder.Append(tile + " ");
                }
                builder.AppendLine();
            }
            return builder.ToString();
        }

        public void AddEnemyToList(Enemy script)
        {
            enemies.Add(script);
        }

        public void AddAllyToList(Ally script)
        {
            allies.Add(script);
        }

        public void RemoveEnemyFromList(Enemy script)
        {
            enemies.Remove(script);
        }

        public void RemoveAllyFromList(Ally script)
        {
            allies.Remove(script);
        }

        public bool CheckIfGameOver()
        {
            return allies.All(ally => ally.isDead);
        }

        public void GameOver()
        {
            Console.WriteLine("Game Over");
            gameOver = true;
            LevelComplete(true);
        }

        public void LevelComplete(bool isGameOver = false)
        {
            Console.WriteLine("Level Complete");
            StoreData(isGameOver);
            gameOver = true;
        }

        //Store data for genetic algorithm
        private void StoreData(bool isGameOver)
        {
            List<int> allyHp = allies.Select(ally => ally.hp).ToList();
            currentSolution.UpdateValues(board.score, turnCount, anxietyEachTurn, allyHp, isGameOver);
        }

        //Called each term to update the anxiety value;
        private void UpdateAnxiety()
        {
            foreach (Ally ally in allies)
            {
                CalculateAnxiety(ally);
            }

            if (enemies.Count == 0) cDecay = 1; 
            if (noEnemiesNear)
                anxiety -= 1;
            else
                anxiety -= cDecay;
            anxietyEachTurn.Add(anxiety);
        }

        private void CalculateAnxiety(Ally ally)
        {
            //Increases anxiety based on how many enemies are in range to attack next turn
            if (ally.enemiesInRange != null)
            {
                noEnemiesNear = false;
                for (int i = 1; i < ally.enemiesInRange.Count; i++)
                {
                    anxiety += ally.CheckAnxiety();
                }
            }

            //Increases anxiety relative to Hp lost this turn
            int loss = ally.initialHp - ally.hp;
            if (loss > 2)
            {
                anxiety += loss * 2;
            }
            else
            {
                anxiety += loss;
            }
            ally.UpdateHp();
        }

        public void AllyDead(Ally ally)
        {
            ally.isDead = true;
            anxiety += 5;
        }

        public void EnemyDead(Enemy enemy)
        {
            enemy.isDead = true;
            anxiety -= 3;
        }
    }
}