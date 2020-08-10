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
        public double cSkill;
        private int anxiety;
        private int cDecay = 1;
        private bool noEnemiesNear = true;

        private int turnsWithoutAction = 0;

        //For level ranking
        public int TurnCount;
        public int EnemiesKilled { get; private set; }

        //For GA
        private readonly Solution currentSolution;
        private List<Move> moves = new List<Move>();

        public GameManager(Solution solution, double cSkill)
        {
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
            Console.WriteLine("Enemy count " + enemies.Count);
            Console.WriteLine("Ally Count " + allies.Count);
            while (!gameOver)
            {

                TurnCount++;
                allies = allies.OrderByDescending(ally => ally.hp).ToList();
                //allies = new List<Ally>(allies.OrderBy(ally => ally.CheckForReachableObjective() ? 0 : 1));
                foreach (var ally in allies.Where(ally => !ally.isDead))
                {
                    if (gameOver) continue;
                    ally.engaged = false;
                    ally.TakeTurn();
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
                    enemy.TakeTurn();
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
                noEnemiesNear = true;
                if (50 < anxiety || anxiety < -50 || TurnCount > 50)
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

            if (enemies.Count == 0)
            {
                if (anxiety < 0) anxiety++;
                if (anxiety > 0) anxiety--;
            }
            else AnxietyCalc();
            anxietyEachTurn.Add(anxiety);
        }

        private void AnxietyCalc()
        {
            int alliesHp = allies.Sum(ally => ally.hp);
            int enemiesHp = enemies.Sum(enemy => enemy.hp);

            int hpDiff = alliesHp - enemiesHp;

            Console.WriteLine("hp diff = " + hpDiff);

            if (hpDiff > 2) anxiety--;
            if (hpDiff < -2) anxiety++;

            int alliesHpLossThisTurn = allies.Sum(ally => ally.initialHp - ally.hp);
            int enemiesHpLossThisTurn = allies.Sum(enemy => enemy.initialHp - enemy.hp);

            if (alliesHpLossThisTurn > 0 || enemiesHpLossThisTurn > 0) turnsWithoutAction = 0;
            else turnsWithoutAction++;

            if (turnsWithoutAction > 2) anxiety-=cDecay;

            int hpLossDiffThisTurn = alliesHpLossThisTurn - enemiesHpLossThisTurn;

            Console.WriteLine("hp loss this turn = " + hpLossDiffThisTurn);

            if (hpLossDiffThisTurn > 2) anxiety--;
            if (hpLossDiffThisTurn < -2) anxiety++;

            int unitDiff = allies.Count - enemies.Count;

            Console.WriteLine("unit diff = " + unitDiff);

            if (unitDiff > 0) anxiety--;
            if (unitDiff < 0) anxiety++;

        }

        public void AllyDead(Ally ally)
        {
            ally.isDead = true;
        }

        public void EnemyDead(Enemy enemy)
        {
            enemy.isDead = true;
            EnemiesKilled++;
        }
    }
}