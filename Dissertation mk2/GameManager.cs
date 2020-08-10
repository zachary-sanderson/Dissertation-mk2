using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dissertation_mk2
{
    public class GameManager
    {
        //For modelling flow
        private readonly List<int> anxietyEachTurn = new List<int>();

        //For GA
        private readonly Solution currentSolution;
        public List<Ally> Allies = new List<Ally>();
        public List<List<int>> AllyTargetPositions = new List<List<int>>();
        private int anxiety;
        public Board Board;
        private int cDecay = 1;
        public double CSkill;

        public List<Enemy> Enemies = new List<Enemy>();
        public bool gameOver;

        //For level ranking
        public int TurnCount;

        private int turnsWithoutAction;

        public GameManager(Solution solution, double cSkill)
        {
            CSkill = cSkill;
            Board = solution.BoardObj;
            Board.GameManager = this;
            currentSolution = solution;
            for (var i = 0; i < Board.NumEnemies; i++)
            {
                Enemies.Add(Board.Enemies[i]);
                Allies.Add(Board.Allies[i]);
            }

            InitGame();
        }

        public int EnemiesKilled { get; private set; }

        public void PlayGame()
        {
            Console.WriteLine("Enemy count " + Enemies.Count);
            Console.WriteLine("Ally Count " + Allies.Count);
            while (!gameOver)
            {
                TurnCount++;
                Allies = Allies.OrderByDescending(ally => ally.Hp).ToList();
                foreach (var ally in Allies.Where(ally => !ally.IsDead))
                {
                    if (gameOver) continue;
                    ally.Engaged = false;
                    ally.TakeTurn();
                }


                var tempAllies = Allies.ToArray();
                foreach (var ally in tempAllies) ally.EndTurn();


                AllyTargetPositions.Clear();
                foreach (var enemy in Enemies.Where(enemy => !enemy.IsDead))
                {
                    if (gameOver) continue;
                    enemy.Engaged = false;
                    enemy.TakeTurn();
                }


                var tempEnemies = Enemies.ToArray();
                foreach (var enemy in tempEnemies) enemy.EndTurn();


                if (Enemies.Count == 0)
                    cDecay = 1;
                Console.WriteLine(Builder(Board.board));
                if (gameOver)
                    continue;
                UpdateAnxiety();
                Console.WriteLine(anxiety);
                if (50 < anxiety || anxiety < -50 || TurnCount > 50)
                    GameOver();
            }
        }

        private void InitGame()
        {
            Board.Enemies.Clear();
            Board.Allies.Clear();
            Console.WriteLine(Builder(Board.board));
        }

        private static string Builder(IEnumerable<List<int>> board)
        {
            var builder = new StringBuilder();
            foreach (var row in board)
            {
                foreach (var tile in row) builder.Append(tile + " ");
                builder.AppendLine();
            }

            return builder.ToString();
        }

        public void RemoveEnemyFromList(Enemy enemy)
        {
            Enemies.Remove(enemy);
        }

        public void RemoveAllyFromList(Ally ally)
        {
            Allies.Remove(ally);
        }

        public bool CheckIfGameOver()
        {
            return Allies.All(ally => ally.IsDead);
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
            var allyHp = Allies.Select(ally => ally.Hp).ToList();
            currentSolution.UpdateValues(Board.Score, TurnCount, EnemiesKilled, anxietyEachTurn, allyHp, isGameOver);
        }

        //Called each term to update the anxiety value;
        private void UpdateAnxiety()
        {
            if (Enemies.Count == 0)
            {
                if (anxiety < 0) anxiety++;
                if (anxiety > 0) anxiety--;
            }
            else
            {
                AnxietyCalc();
            }

            anxietyEachTurn.Add(anxiety);
        }

        private void AnxietyCalc()
        {
            var alliesHp = Allies.Sum(ally => ally.Hp);
            var enemiesHp = Enemies.Sum(enemy => enemy.Hp);

            var hpDiff = alliesHp - enemiesHp;

            Console.WriteLine("hp diff = " + hpDiff);

            if (hpDiff > 2) anxiety--;
            if (hpDiff < -2) anxiety++;

            var alliesHpLossThisTurn = Allies.Sum(ally => ally.InitialHp - ally.Hp);
            var enemiesHpLossThisTurn = Allies.Sum(enemy => enemy.InitialHp - enemy.Hp);

            foreach (var ally in Allies) ally.UpdateHp();

            foreach (var enemy in Enemies) enemy.UpdateHp();

            if (alliesHpLossThisTurn > 0 || enemiesHpLossThisTurn > 0) turnsWithoutAction = 0;
            else turnsWithoutAction++;

            if (turnsWithoutAction > 2) anxiety -= cDecay;

            var hpLossDiffThisTurn = alliesHpLossThisTurn - enemiesHpLossThisTurn;

            Console.WriteLine("hp loss this turn = " + hpLossDiffThisTurn);

            if (hpLossDiffThisTurn > 2) anxiety--;
            if (hpLossDiffThisTurn < -2) anxiety++;

            var unitDiff = Allies.Count - Enemies.Count;

            Console.WriteLine("unit diff = " + unitDiff);

            if (unitDiff > 0) anxiety--;
            if (unitDiff < 0) anxiety++;
        }

        public void AllyDead(Ally ally)
        {
            ally.IsDead = true;
        }

        public void EnemyDead(Enemy enemy)
        {
            enemy.IsDead = true;
            EnemiesKilled++;
        }
    }
}