using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Dissertation_mk2.DataStorage;


namespace Dissertation_mk2
{
    public class GA
    {
        private const int Rows = 25;
        private const int Columns = 25;
        private const int WallCountMin = (int)(0.2 * (Rows * Columns));
        private const int WallCountMax = (int)(0.25*(Rows*Columns));
        private const int ItemCountMin = 4;
        private const int ItemCountMax = 4;
        private const int EnemyCountMin = 5;
        private const int EnemyCountMax = 5;


        public List<List<int>> Positions = new List<List<int>>();

        private readonly List<Solution> feasible = new List<Solution>();

        private readonly List<Board> Infeasible = new List<Board>();


        private const int NumGenerations = 50;
        private const int NumParents = 30;
        private const int MaxInfeasible = 30;
        private const float MutationRate = 0.01f;

        private readonly Solution bestSolution;
        private readonly double bestFitness = 1000;

        private readonly int iter;

        private readonly Markov markov;


        public GA()
        {
            double pValue = 1d;
            markov = new Markov(pValue);
            while (feasible.Count < NumParents)
            {
                Board newBoard = new Board(markov, Columns, Rows, WallCountMin, WallCountMax, ItemCountMin, ItemCountMax, EnemyCountMin, EnemyCountMax);
                IsFeasible(newBoard, pValue);
            }

            List<List<List<int>>> bestBoards = GetBestBoards(pValue);

            List<GMM> feasibleGmms = feasibleToGmms();

            GMM bestBoard = FindBestBoard(bestBoards, feasibleGmms);

            List<double> bestFitnessEachTurn = new List<double>();
            List<int> numFeasiblePerGen = new List<int>();

            feasibleGmms.Clear();
            while (iter < NumGenerations)
            {
                numFeasiblePerGen.Add(feasible.Count);

                feasibleGmms = feasibleToGmms();

                for (int i = 0; i < feasible.Count; i++)
                {
                    feasible[i].UpdateFitness(bestBoard.Compare(feasibleGmms[i]));
                }

                iter++;

                //If new best solution updates
                foreach (var solution in feasible.Where(solution => solution.fitness < bestFitness && !solution.wasGameOver))
                {
                    bestFitness = solution.fitness;
                    var bestFitnessBoard = new Board(solution.InitialBoard, markov, Columns, Rows, WallCountMin, WallCountMax, ItemCountMin, ItemCountMax, EnemyCountMin, EnemyCountMax);

                    bestSolution = new Solution(bestFitnessBoard, bestFitnessBoard.Markov.Strategy, pValue, bestFitnessBoard.NumItems, bestFitnessBoard.NumEnemies);
                    Console.WriteLine(bestFitness);
                    Console.WriteLine(Builder(bestSolution.InitialBoard));
                }

                Console.WriteLine("Best average flow at " + iter + " generations: " + bestFitness);
                Console.WriteLine("Best board:");
                Console.WriteLine(Builder(bestSolution.InitialBoard));
                Console.WriteLine("num enemies:"+ bestSolution.NumEnemies);
                Console.WriteLine("num items:" + bestSolution.NumItems);

                bestFitnessEachTurn.Add(bestFitness);

                if (iter == NumGenerations) continue;

                CrossoverFeasible(pValue);

                CrossoverInfeasible(pValue, iter);

                Console.WriteLine("Feasible Count: " + feasible.Count);
                Console.WriteLine("Infeasible Count: " + Infeasible.Count);

                feasibleGmms.Clear();
            }
            Console.WriteLine("Best Flow: " + bestFitness);
            Console.WriteLine(Builder(bestSolution.InitialBoard));

            var finalBoard = new Board(bestSolution.InitialBoard, markov, Columns, Rows, WallCountMin, WallCountMax, ItemCountMin, ItemCountMax, EnemyCountMin, EnemyCountMax);

            var finalSolution = new Solution(finalBoard, finalBoard.Markov.Strategy, pValue, finalBoard.NumItems, finalBoard.NumEnemies);

            //Assigns the skill values mentioned in the report for the 3 strategies
            double cSkill = pValue switch
            {
                0 => 1.46,
                0.5 => -0.56,
                1 => 1.46,
                _ => 0
            };

            var finalGM = new GameManager(finalSolution, cSkill);

            finalGM.PlayGame();

            for(int i = 0; i < NumGenerations; i++)
            {
                Console.WriteLine("Generation " + i);
                Console.WriteLine("Num Feasible This Gen: " + numFeasiblePerGen[i]);
                Console.WriteLine("Best Fitness This Gen: " + bestFitnessEachTurn[i]);
            }
        }

        //Retrieves the best boards from the database
        private List<List<List<int>>> GetBestBoards(double pValue)
        {
            List<string> names = GetBoardNames(1);
            Console.WriteLine(names.Count);
            foreach (var name in names)
            {
                Console.WriteLine(name);
            }

            return GetBoards(names);
        }

        //Returns GMMs for the feasible population
        private List<GMM> feasibleToGmms()
        {
            List<List<List<int>>> feasibleBoards = new List<List<List<int>>>();

            foreach (var solution in feasible)
            {
                feasibleBoards.Add(solution.InitialBoard);
            }

            return GetGmms(feasibleBoards);
        }

        //Determines which board to use as a fitness function based on initial population of solutions
        private GMM FindBestBoard(List<List<List<int>>> bestBoards, List<GMM> feasibleGmms)
        {
            List<GMM> feasibleGmmList = feasibleGmms;
            List<GMM> bestBoardsGmmList = GetGmms(bestBoards);

            double[] boardDiffs = new double[bestBoards.Count];

            for (int i = 0; i < bestBoards.Count; i++)
            {
                boardDiffs[i] = 0;
                foreach (var feasibleGmm in feasibleGmmList)
                {
                    boardDiffs[i] += bestBoardsGmmList[i].Compare(feasibleGmm);
                }
            }

            int min = 1000;
            int index = 0;
            for (int i = 0; i < boardDiffs.Length; i++)
            {
                if (boardDiffs[i] < min) index = i;
            }

            return bestBoardsGmmList[index];
        }

        //Returns GMMs for the list of best boards from the database
        private List<GMM> GetGmms(List<List<List<int>>> boards)
        {
            List<GMM> gmmList = new List<GMM>();
            foreach (var board in boards)
            {
                List<List<double>> points = new List<List<double>>();
                for (int i = 0; i < Rows; i++)
                {
                    for (int j = 0; j < Columns; j++)
                    {
                        if (board[i][j] == 1) points.Add(new List<double> { i, j });
                    }
                }
                gmmList.Add(new GMM(points));
                Console.WriteLine("GMM added for board:");
                Console.WriteLine(Builder(board));
            }

            return gmmList;
        }

        private int HowManyDifferentThanBest()
        {
            return feasible.Count(solution => !bestSolution.BoardObj.Equals(solution.BoardObj));
        }


        private void CrossoverFeasible(double pValue)
        {
            if (feasible.Count < 2) return;
            var parents = ChooseParents(feasible);
            feasible.Clear();
            for (int i = 1; i < parents.Count; i += 2)
            {
                var children = Crossover(parents[i - 1].InitialBoard.AsReadOnly(), parents[i].InitialBoard.AsReadOnly());
                foreach (var newBoard in children.Select(child => new Board(child, markov, Columns, Rows, WallCountMin, WallCountMax, ItemCountMin, ItemCountMax, EnemyCountMin, EnemyCountMax)))
                { ;
                    IsFeasible(newBoard, pValue);
                }
            }
        }

        //maybe add selective breeding to this too
        private void CrossoverInfeasible(double pValue, int iter)
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
                foreach (var parent in parents)
                {
                    if (parent.board[0][Columns -1] != 3)
                    {
                        Console.WriteLine(Builder(parent.board));
                    }
                }
                for (int i = 1; i < MaxInfeasible; i += 2)
                {
                    var children = Crossover(parents[i - 1].board.AsReadOnly(), parents[i].board.AsReadOnly());
                    foreach (var newBoard in children.Select(child => new Board(child, markov, Columns, Rows, WallCountMin, WallCountMax, ItemCountMin, ItemCountMax, EnemyCountMin, EnemyCountMax)))
                    {
                        IsFeasible(newBoard, pValue);
                    }
                }
            }
            else
            {
                List<List<List<int>>> children = new List<List<List<int>>>();
                for (int i = 1; i < max; i += 2)
                {
                    var childrenToAdd = Crossover(Infeasible[i - 1].board.AsReadOnly(), Infeasible[i].board.AsReadOnly());
                    children.AddRange(childrenToAdd);
                }
                Infeasible.Clear();
                foreach (var newBoard in children.Select(child => new Board(child, markov, Columns, Rows, WallCountMin, WallCountMax, ItemCountMin, ItemCountMax, EnemyCountMin, EnemyCountMax)))
                {
                    IsFeasible(newBoard, pValue);
                }
            }
        }

        private void IsFeasible(Board newBoard, double pValue)
        {
            Console.WriteLine("num enemies = " + newBoard.NumEnemies);
            Console.WriteLine("num items = " + newBoard.NumItems);
            Console.WriteLine("num walls = " + newBoard.NumWalls);
            Console.WriteLine(Builder(newBoard.board));
            List<List<int>> copyNewBoard = new List<List<int>>(newBoard.board);
            Board copy = new Board(copyNewBoard, markov, Columns, Rows, WallCountMin, WallCountMax, ItemCountMin, ItemCountMax, EnemyCountMin, EnemyCountMax);
            if (copy.Validated)
            {
                feasible.Add(new Solution(copy, newBoard.Markov.Strategy, pValue, newBoard.NumItems,
                    newBoard.NumEnemies));
            }
            else
            {
                Infeasible.Add(copy);
            }
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

        private void Mutate(IReadOnlyList<List<int>> board)
        {
            Positions.Clear();
            InitialiseGridPositions();
            var pos = RandomPosition();
            Random rand = new  Random();
            var tile = rand.NextDouble();
            if (tile < 0.25) board[pos[0]][pos[1]] = 0;
            else if (tile < 0.5) board[pos[0]][pos[1]] = 1;
            else if (tile < 0.75) board[pos[0]][pos[1]] = 2;
            else board[pos[0]][pos[1]] = 4;
        }

        private List<List<List<int>>> Crossover(IReadOnlyList<List<int>> first, IReadOnlyCollection<List<int>> second)
        {
            List<List<List<int>>> newBoards = new List<List<List<int>>>();

            Console.WriteLine("FIRST");
            Console.WriteLine(Builder(first));
            Console.WriteLine("second");
            Console.WriteLine(Builder(second));
            Random rand = new Random();
            int row = rand.Next(Rows/5);
            int col = rand.Next(Columns/5);

            var localFirst = CopyBoard(first);
            var localSecond = CopyBoard(second);

            for (int i = row * 5; i < (row + 1) * 5; i++)
            {
                for (int j = col * 5; j < (col + 1) * 5; j++)
                {
                    Console.WriteLine(first[i].Count);
                    var temp = localFirst[i][j];
                    localFirst[i][j] = localSecond[i][j];
                    localSecond[i][j] = temp;
                }
            }

            Console.WriteLine("FIRST AFTER");
            Console.WriteLine(Builder(localFirst));
            Console.WriteLine("second AFTER");
            Console.WriteLine(Builder(localSecond));

            double mutate = rand.NextDouble();
            if (mutate < MutationRate)
            {
                Mutate(localFirst);
                Console.WriteLine("FIRST MUTATE");
                Console.WriteLine(Builder(localFirst));
            }

            mutate = rand.NextDouble();
            if (mutate < MutationRate)
            {
                Mutate(localSecond);
                Console.WriteLine("SECOND MUTATE");
                Console.WriteLine(Builder(localSecond));
            }

            List<List<int>> newFirst = new List<List<int>>(localFirst);
            List<List<int>> newSecond = new List<List<int>>(localSecond);

            newBoards.Add(newFirst);
            newBoards.Add(newSecond);

            return newBoards;
        }


        private List<List<int>> CopyBoard(IEnumerable<List<int>> initial)
        {
            var board = new List<List<int>>();
            foreach (var initialRow in initial.Select(row => row.ToList()))
            {
                board.Add(initialRow);
            }
            return board; 
        }

        //FOR FEASIBLE
        private static List<Solution> ChooseParents(List<Solution> solutions)
        {
            var probabilities = GetProbabilities(solutions);
            if (probabilities.Sum() == 0) return solutions;
            List<Solution> parents = new List<Solution>();
            Random rand = new Random();
            while (parents.Count < NumParents)
            {
                var prob = rand.NextDouble();
                double cumProb = 0;
                for (int i = 0; i < solutions.Count; i++)
                {
                    cumProb += probabilities[i];
                    Console.WriteLine(probabilities[i]);
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
            if (cumFitness == 0) cumFitness = 1; 
            List<double> probabilities = invertedFitness.Select(fitness => fitness / cumFitness).ToList();

            return probabilities;
        }

        private static double HighestFitness(List<Solution> solutions)
        {
            var highest = solutions.Select(solution => solution.fitness).Concat(new double[] {0}).Max();
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
                    Console.WriteLine(probabilities[i]);
                    if (prob < cumProb)
                    {
                        if (Infeasible[i].board[0][Columns-1] != 3)
                        {
                            Console.WriteLine(Builder(Infeasible[i].board));
                        }

                        parents.Add(Infeasible[i]);
                    }
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
                var enem = Math.Abs(EnemyCountMax - board.NumEnemies) * 5;
                var item = Math.Abs(ItemCountMax - board.NumItems) * 5;
                int walls = 0;
                if (board.NumWalls < WallCountMin)
                    walls = WallCountMin - board.NumWalls;
                else if (board.NumWalls > WallCountMax)
                    walls = board.NumWalls - WallCountMax;
                fitness.Add(enem + item + walls);
            }

            return fitness;
        }


        private static string Builder(IEnumerable<List<int>> board)
        {
            var enumerable = board as List<int>[] ?? board.ToArray();
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
