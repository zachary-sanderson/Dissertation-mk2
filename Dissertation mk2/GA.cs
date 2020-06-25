using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Dissertation_mk2
{
    public class GA
    {
        private int rows = 25;
        private int columns = 5;
        public List<List<int>> positions = new List<List<int>>();

        private List<Solution> Feasible = new List<Solution>();
        private List<Solution> Children = new List<Solution>();

        public List<Board> Infeasible = new List<Board>();


        private readonly int numGenerations = 0;
        private readonly int numParents = 50;

        public GA()
        {
            InitialiseGridPositions();
            Random rand = new Random();
            double pValue = rand.NextDouble();
            while (Feasible.Count < numParents)
            {
                Board newBoard = new Board(pValue);
                IsFeasible(newBoard, pValue);
            }

            foreach (var gameManager in Feasible.Select(solution => new GameManager(this, solution, 0))) 
            { 
                gameManager.PlayGame();
            }

            numGenerations++;

            Console.WriteLine(pValue);
            double bestFlow = Feasible.Min(solution => solution.averageFlow);
            Console.WriteLine("Best average flow at " + numGenerations + " generations: " + bestFlow);

            CrossoverFeasible(pValue);

            CrossoverInfeasible(pValue);

            Console.WriteLine("Feasible Count: " + Feasible.Count);
            Console.WriteLine("Infeasible Count: " + Infeasible.Count);
        }

        private void CrossoverFeasible(double pValue)
        {
            var parents = ChooseParents(Feasible);
            Feasible.Clear();
            for (int i = 1; i < parents.Count; i += 2)
            {
                var children = Crossover(parents[i - 1].initialBoard, parents[i].initialBoard);
                foreach (var newBoard in children.Select(child => new Board(child, pValue)))
                {
                    IsFeasible(newBoard, pValue);
                }
            }
        }

        private void CrossoverInfeasible(double pValue)
        {
            int max;
            if (Infeasible.Count % 2 == 0)
                max = Infeasible.Count;
            else
                max = Infeasible.Count - 1;
            if (max < 2) return;
            for (int i = 1; i < max; i += 2)
            {
                var children = Crossover(Infeasible[i - 1].board, Infeasible[i].board);
                foreach (var newBoard in children.Select(child => new Board(child, pValue)))
                {
                    IsFeasible(newBoard, pValue);
                }
            }
        }

        private void IsFeasible(Board newBoard, double pValue)
        {
            Console.WriteLine("num enemies = " + newBoard.numEnemies);
            Console.WriteLine("num items = " + newBoard.numItems);
            Console.WriteLine("num walls = " + newBoard.numWalls);
            Console.WriteLine(Builder(newBoard.board));
            if (newBoard.validated)
                Feasible.Add(new Solution(newBoard, newBoard.markov.Personality, pValue, newBoard.numItems, newBoard.numItems));
            else
                Infeasible.Add(newBoard);
        }

        private void InitialiseGridPositions()
        {
            for (int i = 0; i < columns; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    if (i == 0 || j == 0 || i == columns - 1 || j == rows - 1) continue;
                    List<int> pos = new List<int> { i, j };
                    positions.Add(pos);
                }
            }
        }

        private List<int> RandomPosition()
        {
            Random rand = new Random();
            int index = rand.Next(positions.Count - 1);
            List<int> pos = positions[index];
            positions.RemoveAt(index);
            return pos;
        }

        private List<List<List<float>>> Crossover(List<List<float>> first, List<List<float>> second)
        {
            List<List<List<float>>> newBoards = new List<List<List<float>>>();

            Random rand = new Random();
            int row = rand.Next(1, rows - 1);

            List<float> temp;

            for (int i = 0; i < row; i++)
            {
                temp = first[i];
                first[i] = second[i];
                second[i] = temp;
            }

            newBoards.Add(first);
            newBoards.Add(second);

            return newBoards;
        }

        private List<Solution> ChooseParents(List<Solution> solutions)
        {
            var probabilities = GetProbabilities(solutions);
            List<Solution> parents = new List<Solution>();
            Random rand = new Random();
            while (parents.Count < numParents)
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

    }
}
