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
        private readonly List<int> anxietyEachTurn = new List<int>();
        public int cSkill;
        private int anxiety;
        private int cDecay = 2;
        private bool noEnemiesNear = true;
        private GA ga;
        private int iter;

        //For level ranking
        public int TurnCount;
        public int EnemiesKilled { get; private set; }

        //For GA
        private readonly Solution currentSolution;
        private List<Move> moves = new List<Move>();

        public GameManager(Solution solution, int cSkill, GA ga, int iter)
        {
            this.cSkill = cSkill;
            this.ga = ga;
            this.iter = iter;
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
            Console.WriteLine("Enemy count " + enemies.Count);
            Console.WriteLine("Ally Count " + allies.Count);
            while (!gameOver)
            {

                TurnCount++;
                allies = allies.OrderByDescending(ally => ally.hp).ToList();
                foreach (var ally in allies.Where(ally => !ally.isDead))
                {
                    if (gameOver) continue;
                    ally.engaged = false;
                    ally.TakeTurn(ga);
                }


                Ally[] tempAllies = allies.ToArray();
                foreach (var ally in tempAllies)
                {
                    ally.EndTurn();
                }


                allyTargetPositions.Clear();
                foreach (var enemy in enemies.Where(enemy => !enemy.isDead))
                {
                    if (gameOver) continue;
                    enemy.engaged = false;
                    enemy.TakeTurn(ga);
                }


                Enemy[] tempEnemies = enemies.ToArray();
                foreach (var enemy in tempEnemies)
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
                if (TurnCount % 5 == 0)
                    board.markov.Transition();
                noEnemiesNear = true;
                if (50 < anxiety || anxiety < -50)
                    GameOver();

            }
        }

        private void InitGame()
        {
            board.enemies.Clear();
            board.allies.Clear();
            Console.WriteLine(Builder(board.board));
        }

        private static string Builder(IEnumerable<List<int>> board)
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

        public void AddMove(Move move)
        {
            moves.Add(move);
        }

        public void RemoveEnemyFromList(Enemy enemy)
        {
            enemies.Remove(enemy);
        }

        public void RemoveAllyFromList(Ally ally)
        {
            allies.Remove(ally);
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
            currentSolution.UpdateValues(board.score, TurnCount, EnemiesKilled, anxietyEachTurn, allyHp, moves, isGameOver);
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
            EnemiesKilled++;
            anxiety -= 3;
        }
    }
}