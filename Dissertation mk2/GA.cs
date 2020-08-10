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

        private List<Solution> feasible = new List<Solution>();

        private List<Board> Infeasible = new List<Board>();


        private const int NumGenerations = 50;
        private const int NumParents = 30;
        private const int MaxInfeasible = 30;
        private const float MutationRate = 0.01f;

        private readonly Solution bestSolution;
        private readonly double bestFitness = 1000;
        private double finalPersonalityEstimate;

        private int iter;

        private Markov markov;

        //Temp
        int[][] allyPositions = { new[] { Rows - 3, 0 }, new[] { Rows - 2, 0 },
            new[] { Rows - 1, 2 }, new[] { Rows - 1, 1 }, new[] { Rows - 1, 0 } };



        public GA()
        {
            //Random rand = new Random();
            //double pValue = rand.NextDouble();
            double pValue = 1d;
            List<int> numTurns = new List<int>();
            markov = new Markov(pValue);
            while (feasible.Count < NumParents)
            {
                Board newBoard = new Board(markov, Columns, Rows, WallCountMin, WallCountMax, ItemCountMin, ItemCountMax, EnemyCountMin, EnemyCountMax);
                IsFeasible(newBoard, pValue);
            }

            List<List<List<int>>> bestBoards = GetBestBoards(pValue);

            List<GMM> feasibleGmms = feasibleToGmms();

            GMM bestBoard = FindBestBoard(bestBoards, feasibleGmms);

            List<double> bestFlowEachTurn = new List<double>();
            List<int> numFeasiblePerGen = new List<int>();
            List<int> diffSolutionsEachTurn = new List<int>();
            List<List<double>> flowsEachTurn = new List<List<double>>();

            feasibleGmms.Clear();
            while (iter < NumGenerations)
            {
                List<double> flowsThisTurn = new List<double>();
                numFeasiblePerGen.Add(feasible.Count);

                feasibleGmms = feasibleToGmms();

                for (int i = 0; i < feasible.Count; i++)
                {
                    feasible[i].UpdateFitness(bestBoard.Compare(feasibleGmms[i]));
                    /*
                    GameManager gm = new GameManager(solution, 0);
                    gm.PlayGame();
                   // markov.Transition();
                   flowsThisTurn.Add(solution.averageFlow);
                   if (solution.wasGameOver)
                   {
                       markov.Transition();
                   }
                    */
                }

                flowsEachTurn.Add(flowsThisTurn);

                iter++;

                foreach (var solution in feasible)
                {
                    if (solution.fitness < bestFitness && !solution.wasGameOver)
                    {
                        bestFitness = solution.fitness;
                        var bestFitnessBoard = new Board(solution.initialBoard, markov, Columns, Rows, WallCountMin, WallCountMax, ItemCountMin, ItemCountMax, EnemyCountMin, EnemyCountMax);

                        bestSolution = new Solution(bestFitnessBoard, bestFitnessBoard.markov.Personality, pValue, bestFitnessBoard.numItems, bestFitnessBoard.numEnemies);
                        //bestSolution = new Solution(solution);
                        Console.WriteLine(bestFitness);
                        Console.WriteLine(Builder(bestSolution.initialBoard));
                    }
                    numTurns.Add(solution.numTurns);
                }

                diffSolutionsEachTurn.Add(HowManyDifferentThanBest());

                Console.WriteLine("Best average flow at " + iter + " generations: " + bestFitness);
                Console.WriteLine("Best board:");
                Console.WriteLine(Builder(bestSolution.initialBoard));
                Console.WriteLine("num enemies:"+ bestSolution.numEnemies);
                Console.WriteLine("num items:" + bestSolution.numItems);

                foreach (var flow in flowsThisTurn)
                {
                    Console.WriteLine(flow);
                }

                bestFlowEachTurn.Add(bestFitness);

                if (iter == NumGenerations) continue;

                CrossoverFeasible(pValue);

                CrossoverInfeasible(pValue, iter);

                Console.WriteLine("Feasible Count: " + feasible.Count);
                Console.WriteLine("Infeasible Count: " + Infeasible.Count);

                feasibleGmms.Clear();
            }
            Console.WriteLine("Best Flow: " + bestFitness);
            Console.WriteLine(Builder(bestSolution.initialBoard));

            //EstimatePersonality(personalityFLagsList);;
            var finalBoard = new Board(bestSolution.initialBoard, markov, Columns, Rows, WallCountMin, WallCountMax, ItemCountMin, ItemCountMax, EnemyCountMin, EnemyCountMax);

            var finalSolution = new Solution(finalBoard, finalBoard.markov.Personality, pValue, finalBoard.numItems, finalBoard.numEnemies);

            double cSkill = 0;
            switch (pValue)
            {
                case 0:
                    cSkill = 1.46;
                    break;
                case 0.5:
                    cSkill = -0.56;
                    break;
                case 1:
                    cSkill = 1.46;
                    break;
            }

            var finalGM = new GameManager(finalSolution, cSkill);

            finalGM.PlayGame();

            for(int i = 0; i < NumGenerations; i++)
            {
                Console.WriteLine("num Feasible: " + numFeasiblePerGen[i]);
                Console.WriteLine(bestFlowEachTurn[i]);
                Console.WriteLine("Num diff solutions in feasible: " + diffSolutionsEachTurn[i]);
                Console.WriteLine("Average flow this gen: " + flowsEachTurn[i].Sum()/flowsEachTurn[i].Count);
            }
            /*
            foreach (var move in finalSolution.moves)
            {
                var attacked = move.Attack ? "attacks." : "doesn't attack.";
                var itemPickup = "item picked up.";
                if (!move.ItemPickup)
                    Console.WriteLine("turn:" + move.TurnNum + "  " + move.Type + " moves from " + move.StartPos[0] + " " + move.StartPos[1] + " to " + move.EndPos[0] + " " + move.EndPos[1] + " and " + attacked);
                else
                    Console.WriteLine("turn:" + move.TurnNum + "  " + move.Type + " moves from " + move.StartPos[0] + " " + move.StartPos[1] + " to " + move.EndPos[0] + " " + move.EndPos[1] + " and " + itemPickup);
            }
            */
        }

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

        private List<GMM> feasibleToGmms()
        {
            List<List<List<int>>> feasibleBoards = new List<List<List<int>>>();

            foreach (var solution in feasible)
            {
                feasibleBoards.Add(solution.initialBoard);
            }

            return GetGmms(feasibleBoards);
        }

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
            return feasible.Count(solution => !bestSolution.boardObj.Equals(solution.boardObj));
        }


        private void CrossoverFeasible(double pValue)
        {
            if (feasible.Count < 2) return;
            var parents = ChooseParents(feasible);
            feasible.Clear();
            for (int i = 1; i < parents.Count; i += 2)
            {
                var children = Crossover(parents[i - 1].initialBoard.AsReadOnly(), parents[i].initialBoard.AsReadOnly());
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
                //Infeasible = Infeasible.OrderBy(a => Guid.NewGuid()).ToList();
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
            Console.WriteLine("num enemies = " + newBoard.numEnemies);
            Console.WriteLine("num items = " + newBoard.numItems);
            Console.WriteLine("num walls = " + newBoard.numWalls);
            Console.WriteLine(Builder(newBoard.board));
            List<List<int>> copyNewBoard = new List<List<int>>(newBoard.board);
            Board copy = new Board(copyNewBoard, markov, Columns, Rows, WallCountMin, WallCountMax, ItemCountMin, ItemCountMax, EnemyCountMin, EnemyCountMax);
            if (copy.validated)
            {
                feasible.Add(new Solution(copy, newBoard.markov.Personality, pValue, newBoard.numItems,
                    newBoard.numEnemies));
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

        private void Mutate(List<List<int>> board)
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
                var enem = Math.Abs(EnemyCountMax - board.numEnemies) * 5;
                var item = Math.Abs(ItemCountMax - board.numItems) * 5;
                int walls = 0;
                if (board.numWalls < WallCountMin)
                    walls = WallCountMin - board.numWalls;
                else if (board.numWalls > WallCountMax)
                    walls = board.numWalls - WallCountMax;
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
