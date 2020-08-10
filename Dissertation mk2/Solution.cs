using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dissertation_mk2
{
    public class Solution
    {
        public List<int> allyHp;
        private List<int> anxietyEachTurn;
        public int averageAnxiety;
        public double averageFlow;
        public Board BoardObj;
        public int enemKilled;
        public List<List<int>> EnemyPositions;


        public double fitness;
        public List<List<int>> InitialBoard = new List<List<int>>();
        public List<List<int>> ItemPositions = new List<List<int>>();
        public int NumEnemies;
        public int NumItems;
        public int numTurns;
        public string Strategy;
        public double pValue;

        public int score;
        public bool wasGameOver;

        public Solution(Board board, string strategy, double pValue, int numItems, int numEnemies)
        {
            SaveBoard(board.board, board.ItemPositions);
            BoardObj = board;
            Strategy = strategy;
            this.pValue = pValue;
            NumItems = numItems;
            NumEnemies = numEnemies;
            EnemyPositions = board.EnemyPositions;
        }

        private void SaveBoard(IEnumerable<List<int>> initial, IEnumerable<List<int>> itemPositionsEnumerable)
        {
            foreach (var initialRow in initial.Select(row => row.ToList())) InitialBoard.Add(initialRow);

            foreach (var pos in itemPositionsEnumerable.Select(itemPos => itemPos.ToList())) ItemPositions.Add(pos);
        }

        public void UpdateFitness(double fitness)
        {
            this.fitness = fitness;
        }

        public void UpdateValues(int score, int numTurns, int enemKilled, List<int> anxietyEachTurn, List<int> allyHp,
            bool wasGameOver = false)
        {
            this.score = score;
            this.numTurns = numTurns;
            this.enemKilled = enemKilled;
            this.anxietyEachTurn = anxietyEachTurn;
            this.allyHp = allyHp;
            this.wasGameOver = wasGameOver;
            CalculateValues();
        }

        private void CalculateValues()
        {
            averageAnxiety = anxietyEachTurn.Sum();
            averageFlow = (double) anxietyEachTurn.Sum(Math.Abs) / anxietyEachTurn.Count;
            Console.WriteLine("Anxieties: " + IntBuilder(anxietyEachTurn));
            Console.WriteLine("Cumulative anxiety: " + averageAnxiety);
            Console.WriteLine("Average Distance from flow: " + averageFlow);
            Console.WriteLine("Strategy: " + Strategy);
            Console.WriteLine("Hp: " + IntBuilder(allyHp));
            Console.WriteLine("Num turns: " + numTurns);
            Console.WriteLine("Num items: " + score + "/" + NumItems);
            Console.WriteLine("Num enemies killed: " + enemKilled + "/" + NumEnemies);
        }


        private static string IntBuilder(IEnumerable<int> ints)
        {
            var builder = new StringBuilder();
            foreach (var value in ints) builder.Append(value + " ");
            return builder.ToString();
        }
    }
}