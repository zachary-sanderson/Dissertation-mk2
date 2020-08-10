using System;
using System.Collections.Generic;
using System.Linq;
using Accord;
using static Dissertation_mk2.DataStorage;

namespace Dissertation_mk2
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            //Random rand = new Random();
            //double pValue = rand.NextDouble();
            int Rows = 25;
            int Columns = 25;
            int WallCountMin = 100;
            int WallCountMax = 140;
            int ItemCountMin = 4;
            int ItemCountMax = 4;
            int EnemyCountMin = 5;
            int EnemyCountMax = 5;

            int iter = 0;

            double pValue = 1d;
            Markov markov = new Markov(pValue);

            List<Solution> solutions = new List<Solution>();

            while (iter < 1000)
            {
                Board newBoard = new Board(markov, Columns, Rows, WallCountMin, WallCountMax, ItemCountMin, ItemCountMax, EnemyCountMin, EnemyCountMax);
                while (!newBoard.validated)
                {
                    Console.WriteLine("newboard not validated!");
                    newBoard = new Board(markov, Columns, Rows, WallCountMin, WallCountMax, ItemCountMin, ItemCountMax, EnemyCountMin, EnemyCountMax);
                }

                Solution solution = new Solution(newBoard, newBoard.markov.Personality, pValue, newBoard.numItems,
                    newBoard.numEnemies);

                solutions.Add(solution);

                GameManager gm = new GameManager(solution, 0);
                {
                    gm.PlayGame();
                }

                iter++;
            }
            */

            /*
            foreach (var solution in solutions)
            {
                Solution closestSolution = null;
                double closestFlow = 1000;
                var diff = 10000;
                foreach (var solution2 in solutions)
                {
                    if (!solution.Equals(solution2.initialBoard, Rows, Columns))
                    {
                        var mapDiff = solution.CompareMap(solution2);
                        if (mapDiff.Sum() < diff)
                        {
                            Console.WriteLine(solution.Builder(solution.initialBoard));
                            Console.WriteLine(solution2.Builder(solution2.initialBoard));
                            closestSolution = solution2;
                            closestFlow = solution2.averageFlow;
                            diff = mapDiff.Sum();
                            Console.WriteLine(mapDiff.Sum());
                            foreach (var diffValue in mapDiff)
                            {
                                Console.WriteLine(diffValue);
                            }
                        }
                    }
                }

                if (closestSolution != null)
                {
                    Console.WriteLine(solution.Builder(solution.initialBoard));
                    Console.WriteLine(closestSolution.Builder(closestSolution.initialBoard));
                    Console.WriteLine(diff);
                    Console.WriteLine("Solution1 flow: " + solution.averageFlow);
                    Console.WriteLine("Solution2 flow: " + closestFlow);
                }
            }
            */

           _ = new GA();
            
           /*
           foreach (var solution in solutions.Where(solution => solution.averageFlow < 1))
           {
               DataStorage.StoreData(solution.initialBoard, solution.averageFlow, pValue);
           }
           */
           

        }
    }
}
