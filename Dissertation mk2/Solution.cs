using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Linq;

namespace Dissertation_mk2
{
    public class Solution
    {
        public string Personality;
        public List<List<int>> initialBoard = new List<List<int>>();
        public Board boardObj;
        public int numItems;
        public int numEnemies;
        public List<List<int>> itemPositions = new List<List<int>>();
        public List<List<int>> enemyPositions;

        public int score;
        public double averageFlow;
        public int numTurns;
        public int enemKilled;
        private List<int> anxietyEachTurn;
        public int averageAnxiety;
        public double pValue;
        public List<int> allyHp;
        public bool wasGameOver;
        public List<Move> moves;

        //Personality evaluation
        public double fitness;

        public Solution(Board board, string personality, double pValue, int numItems, int numEnemies)
        {
            SaveBoard(board.board, board.itemPositions);
            boardObj = board;
            Personality = personality;
            this.pValue = pValue;
            this.numItems = numItems;
            this.numEnemies = numEnemies;
            enemyPositions = board.enemyPositions;
        }

        /*
        public Solution(Solution solution)
        {
            SaveBoard(solution.initialBoard.AsReadOnly());
        }
        */

        private void SaveBoard(IEnumerable<List<int>> initial, IEnumerable<List<int>> itemPositionsEnumerable)
        {
            foreach (var initialRow in initial.Select(row => row.ToList()))
            {
                initialBoard.Add(initialRow);
            }

            foreach (var pos in itemPositionsEnumerable.Select(itemPos => itemPos.ToList()))
            {
                itemPositions.Add(pos);
            }
        }

        public void UpdateFitness(double fitness)
        {
            this.fitness = fitness;
        }

        public void UpdateValues(int score, int numTurns, int enemKilled, List<int> anxietyEachTurn, List<int> allyHp, List<Move> moves, bool wasGameOver = false)
        {
            this.score = score;
            this.numTurns = numTurns;
            this.enemKilled = enemKilled;
            this.anxietyEachTurn = anxietyEachTurn;
            this.allyHp = allyHp;
            this.moves = moves;
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
            Console.WriteLine("Personality: " + Personality);
            Console.WriteLine("Hp: " + IntBuilder(allyHp));
            Console.WriteLine("Num turns: " + numTurns);
            Console.WriteLine("Num items: " + score + "/" + numItems);
            Console.WriteLine("Num enemies killed: " + enemKilled + "/" + numEnemies);
        }

        public bool Equals(List<List<int>> obj, int rows, int columns)
        {
            bool equals = true;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    if (initialBoard[i][j] != obj[i][j]) equals = false;
                }
            }

            return equals;
        }

        public List<int> CompareMap(Solution other)
        {
            return new List<int> {CompareWalls(other), CompareEnemies(other).Sum(), CompareItems(other).Sum()};
            //return CompareWalls(other) + CompareEnemies(other).Sum() + CompareItems(other).Sum();
        }

        private int CompareWalls(Solution other)
        {
            int count = 0;
            for (int i = 0; i < initialBoard.Count; i++)
            {
                for (int j = 0; j < initialBoard.Count; j++)
                {
                    var tile1 = initialBoard[i][j];
                    var tile2 = other.initialBoard[i][j];
                    if (tile1 == 1 && tile2 != 1) count++;
                    if (tile2 == 1 && tile1 != 1) count++;
                }
            }
            return count;
        }

        /*
        private int TestCompareWalls(Solution other)
        {
            
            for (int i = 0; i < initialBoard.Count; i++)
            {
                for (int j = 0; j < initialBoard.Count; j++)
                {
                    
                }
            }

            //

            int row = boardObj.rows / 5;
            int col =  boardObj.columns / 5;

            List<List<int>> segmentDiffs = new List<List<int>>();
            for (int i = 0; i < boardObj.rows; i += row)
            {
                for (int j = 0; j < boardObj.columns; j += col)
                {
                    var count = 0;
                    for (int k = i; k < (i += row); k++)
                    {
                        for (int l = j; l < (j += col); l++)
                        {
                            if (initialBoard[i][j] == 1) wallPos1.Add(new List<int> { i, j });
                            if (other.initialBoard[i][j] == 1) wallPos2.Add(new List<int> { i, j });
                        }
                    }
                    segmentDiffs[i/row][j/col] 
                }
            }

            //

            var count = 0;
            foreach (var pos in wallPos1)
            {
                
            }

            return count;
        }
        */

        //For two solutions compares the shortest distance each enemy has from another enemy in the initial map.
        private IEnumerable<int> CompareEnemies(Solution other)
        {
            return enemyPositions.Select(position => other.enemyPositions.Select(otherPosition => CheckDistance(position, otherPosition)).Concat(new[] {100}).Min()).ToList();
        }

        //For two solutions compares the shortest distance each item has from another item in the initial map.
        private IEnumerable<int> CompareItems(Solution other)
        {
            return itemPositions.Select(position => other.itemPositions.Select(otherPosition => CheckDistance(position, otherPosition)).Concat(new[] {100}).Min()).ToList();
        }

        private static int CheckDistance(IReadOnlyList<int> startPos, IReadOnlyList<int> endPos)
        {
            int xDiff = Math.Abs(endPos[0] - startPos[0]);
            int yDiff = Math.Abs(endPos[1] - startPos[1]);
            return xDiff + yDiff;
        }

        private static string IntBuilder(IEnumerable<int> ints)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var value in ints)
            {
                builder.Append(value + " ");
            }
            return builder.ToString();
        }

        public string Builder(IEnumerable<List<int>> board)
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
    }
}
