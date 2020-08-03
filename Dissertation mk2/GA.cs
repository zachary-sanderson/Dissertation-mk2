using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Dissertation_mk2
{
    public class GA
    {
        private const int Rows = 25;
        private const int Columns = 25;
        public List<List<int>> Positions = new List<List<int>>();

        private readonly List<Solution> feasible = new List<Solution>();

        public List<Board> Infeasible = new List<Board>();


        private const int NumGenerations = 10;
        private const int NumParents = 30;
        private const int MaxInfeasible = 50;
        private const float MutationRate = 0.01f;

        private readonly Solution bestSolution;
        private readonly double bestFlow = 1000;
        private double finalPersonalityEstimate;

        public GA()
        {
            int iter = 0;
            //Random rand = new Random();
            //double pValue = rand.NextDouble();
            double pValue = 0.5d;
            List<int> numTurns = new List<int>();
            while (iter < NumGenerations)
            {
                InitialiseGridPositions();
                
                
                while (feasible.Count < NumParents)
                {
                    Board newBoard = new Board(pValue);
                    IsFeasible(newBoard, pValue);
                }

                foreach (var gameManager in feasible.Select(solution => new GameManager(solution, 0)))
                {
                    gameManager.PlayGame();
                }

                iter++;

                foreach (var solution in feasible)
                {
                    if (solution.averageFlow < bestFlow && !solution.wasGameOver)
                    {
                        bestFlow = Math.Abs(solution.averageFlow);
                        bestSolution = solution;
                    }
                    numTurns.Add(solution.numTurns);
                }

                Console.WriteLine("Best average flow at " + iter + " generations: " + bestFlow);
                Console.WriteLine("Best board:");
                Console.WriteLine(Builder(bestSolution.initialBoard));
                Console.WriteLine("num enemies:"+ bestSolution.numEnemies);
                Console.WriteLine("num items:" + bestSolution.numItems);

                CrossoverFeasible(pValue);

                CrossoverInfeasible(pValue);

                Console.WriteLine("Feasible Count: " + feasible.Count);
                Console.WriteLine("Infeasible Count: " + Infeasible.Count);
            }
            Console.WriteLine("Best Flow: " + bestFlow);
            Console.WriteLine(Builder(bestSolution.initialBoard));

            //EstimatePersonality(personalityFLagsList);

            var finalBoard = new Board(bestSolution.initialBoard, pValue);
            var finalSolution = new Solution(finalBoard, finalBoard.markov.Personality, pValue, finalBoard.numItems,
                finalBoard.numEnemies);
            var finalGM = new GameManager(finalSolution, 0);
            finalGM.PlayGame();
            /*
            Console.WriteLine("Average num turns: " + (double)numTurns.Sum()/numTurns.Count);
            foreach (var move in bestSolution.moves)
            {
                string attacked;
                attacked = move.Attack ? "attacks." : "doesn't attack.";
                Console.WriteLine("turn:" + move.TurnNum + "  " + move.Type + " moves from " + move.StartPos[0] + " " + move.StartPos[1] + " to " + move.EndPos[0] + " " + move.EndPos[1] + " and " + attacked);
            }
            */
        }

        private void CrossoverFeasible(double pValue)
        {
            var parents = ChooseParents(feasible);
            feasible.Clear();

            for (int i = 1; i < parents.Count; i += 2)
            {
                var children = Crossover(parents[i - 1].initialBoard, parents[i].initialBoard);
                foreach (var newBoard in children.Select(child => new Board(child, pValue)))
                {
                    IsFeasible(newBoard, pValue);
                }
            }
        }

        //maybe add selective breeding to this too
        private void CrossoverInfeasible(double pValue)
        {
            int max;
            if (Infeasible.Count % 2 == 0)
                max = Infeasible.Count;
            else
                max = Infeasible.Count - 1;
            if (max < 2) return;
            if (max > MaxInfeasible)
            {
                var parents = ChooseInfeasibleParents();
                Infeasible.Clear();
                for (int i = 1; i < MaxInfeasible; i += 2)
                {
                    var children = Crossover(parents[i - 1].board, parents[i].board);
                    foreach (var newBoard in children.Select(child => new Board(child, pValue)))
                    {
                        IsFeasible(newBoard, pValue);
                    }
                }
            }
            else
            {
                for (int i = 1; i < max; i += 2)
                {
                    var children = Crossover(Infeasible[i - 1].board, Infeasible[i].board);
                    foreach (var newBoard in children.Select(child => new Board(child, pValue)))
                    {
                        IsFeasible(newBoard, pValue);
                    }
                }
            }
        }

        private void IsFeasible(Board newBoard, double pValue)
        {
            Console.WriteLine("num enemies = " + newBoard.numEnemies);
            Console.WriteLine("num items = " + newBoard.numItems);
            Console.WriteLine("num walls = " + newBoard.numWalls);
            if (newBoard.validated && newBoard.CheckForGoal())
                feasible.Add(new Solution(newBoard, newBoard.markov.Personality, pValue, newBoard.numItems, newBoard.numEnemies));
            else if (newBoard.CheckForGoal())
                Infeasible.Add(newBoard);
        }

        private void InitialiseGridPositions()
        {
            for (int i = 0; i < Columns; i++)
            {
                for (int j = 0; j < Rows; j++)
                {
                    if (i == 0 || j == 0 || i == Columns - 1 || j == Rows - 1) continue;
                    List<int> pos = new List<int> { i, j };
                    Positions.Add(pos);
                }
            }
        }

        private List<int> RandomPosition()
        {
            Random rand = new Random();
            int index = rand.Next(Positions.Count - 1);
            List<int> pos = Positions[index];
            return pos;
        }

        private void Mutate(List<List<float>> board)
        {
            var pos = RandomPosition();
            Random rand = new  Random();
            var tile = rand.NextDouble();
            if (tile < 0.25) board[pos[0]][pos[1]] = 0;
            else if (tile < 0.5) board[pos[0]][pos[1]] = 1;
            else if (tile < 0.75) board[pos[0]][pos[1]] = 2;
            else board[pos[0]][pos[1]] = 4.5f;
        }

        private List<List<List<float>>> Crossover(List<List<float>> first, List<List<float>> second)
        {
            List<List<List<float>>> newBoards = new List<List<List<float>>>();

            Console.WriteLine("FIRST");
            Console.WriteLine(Builder(first));
            Console.WriteLine("second");
            Console.WriteLine(Builder(second));
            Random rand = new Random();
            int row = rand.Next(5);
            int col = rand.Next(5);

            for (int i = row * 5; i < (row + 1) * 5; i++)
            {
                for (int j = col * 5; j < (col + 1) * 5; j++)
                {
                    Console.WriteLine(first[i].Count);
                    var temp = first[i][j];
                    first[i][j] = second[i][j];
                    second[i][j] = temp;
                }
            }

            newBoards.Add(first);
            newBoards.Add(second);
            Console.WriteLine("FIRST AFTER");
            Console.WriteLine(Builder(first));
            Console.WriteLine("second AFTER");
            Console.WriteLine(Builder(second));

            double mutate = rand.NextDouble();
            if (mutate < MutationRate)
            {
                Mutate(first);
                Console.WriteLine("FIRST MUTATE");
                Console.WriteLine(Builder(first));
            }

            mutate = rand.NextDouble();
            if (mutate < MutationRate)
            {
                Mutate(second);
                Console.WriteLine("SECOND MUTATE");
                Console.WriteLine(Builder(second));
            }

            return newBoards;
        }


        
        
        //FOR FEASIBLE
        private static List<Solution> ChooseParents(List<Solution> solutions)
        {
            var probabilities = GetProbabilities(solutions);
            List<Solution> parents = new List<Solution>();
            Random rand = new Random();
            while (parents.Count < NumParents)
            {
                var prob = rand.NextDouble();
                double cumProb = 0;
                for (int i = 0; i < solutions.Count; i++)
                {
                    cumProb += probabilities[i];
                    if (prob < cumProb)
                        parents.Add(solutions[i]);
                }
            }

            return parents;
        }

        private static List<double> GetProbabilities(List<Solution> solutions)
        {
            //Since we're trying to minimize fitness to 0 values must be inverted to get normalized probabilities for pool selection
            var max = HighestFitness(solutions);
            List<double> invertedFitness = solutions.Select(solution => max - solution.averageFlow).ToList();

            var cumFitness = invertedFitness.Sum();
            List<double> probabilities = invertedFitness.Select(fitness => fitness / cumFitness).ToList();
            return probabilities;
        }

        private static double HighestFitness(List<Solution> solutions)
        {
            var highest = solutions.Select(solution => solution.averageFlow).Concat(new double[] {0}).Max();
            return highest;
        }



        //FOR INFEASIBLE
        private List<Board> ChooseInfeasibleParents()
        {
            var probabilities = GetInfeasibleProbabilities();
            List<Board> parents = new List<Board>();
            Random rand = new Random();
            while (parents.Count < MaxInfeasible)
            {
                var prob = rand.NextDouble();
                double cumProb = 0;
                for (int i = 0; i < Infeasible.Count; i++)
                {
                    cumProb += probabilities[i];
                    if (prob < cumProb)
                        parents.Add(Infeasible[i]);
                }
            }

            return parents;
        }

        private List<double> GetInfeasibleProbabilities()
        {
            //Since we're trying to minimize fitness to 0 values must be inverted to get normalized probabilities for pool selection
            var fitness = InfeasibleFitness();
            var max = fitness.Max();

            for (int i = 0; i < fitness.Count; i++)
                fitness[i] = max - fitness[i];

            var cumFitness = fitness.Sum();
            List<double> probabilities = fitness.Select(fit => (double)fit / (double)cumFitness).ToList();
            return probabilities;
        }

        private List<int> InfeasibleFitness()
        {
            List<int> fitness = new List<int>();
            foreach (var board in Infeasible)
            {
                var enem = Math.Abs(5 - board.numEnemies) * 5;
                var item = Math.Abs(4 - board.numItems) * 5;
                int walls = 0;
                if (board.numWalls < 100)
                    walls = 100 - board.numWalls;
                else if (board.numWalls > 120)
                    walls = board.numWalls - 120;
                fitness.Add(enem + item + walls);
            }

            return fitness;
        }


        private void EstimatePersonality(IReadOnlyList<List<double>> personalityFlagList)
        {
            List<double> sums = new List<double>();
            int count = 0;
            for (int i = 0; i < personalityFlagList.Count; i++)
            {
                Console.WriteLine("Generation " + i + " flags");
                foreach (var flag in personalityFlagList[i])
                {
                    Console.WriteLine(flag);
                }
                count += personalityFlagList[i].Count;
                double sum = personalityFlagList[i].Sum();
                Console.WriteLine("Generation: " + i + ". Estimated Personality: " + sum / personalityFlagList[i].Count);
                sums.Add(sum);
            }

            finalPersonalityEstimate = (sums.Sum() / count);
            Console.WriteLine("Final Personality Estimate: " + finalPersonalityEstimate);
        }

        private static string Builder(IEnumerable<List<float>> board)
        {
            var enumerable = board as List<float>[] ?? board.ToArray();
            Console.WriteLine(enumerable.Count());
            StringBuilder builder = new StringBuilder();
            foreach (var row in enumerable)
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
