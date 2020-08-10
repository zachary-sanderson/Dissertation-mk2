using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Dissertation_mk2
{
    public class Board
    {
        [Serializable]
        public class Count
        {
            public int Minimum;
            public int Maximum;

            public Count(int min, int max)
            {
                Minimum = min;
                Maximum = max;
            }
        }
        
        public int Columns;
        public int Rows;
        public Count WallCount;
        public Count ItemCount;
        public Count EnemyCount;

        public GameManager GameManager;
        public Markov Markov;
        public List<List<int>> board = new List<List<int>>();
        public List<List<int>> Positions = new List<List<int>>();
        public List<List<int>> ItemPositions = new List<List<int>>();
        public List<List<int>> EnemyPositions = new List<List<int>>();
        public List<int> GoalPos = new List<int>();

        public List<Enemy> Enemies = new List<Enemy>();
        public List<Ally> Allies = new List<Ally>();

        public bool Validated;
        public int Score = 0;
        public int ItemValue = 1;

        public int NumItems;
        public int NumEnemies;
        public int NumWalls;
        public int NumAllies;

        public Board(Markov markov, int numColumns, int numRows, int wallCountMin, int wallCountMax, int itemCountMin, int itemCountMax, int enemyCountMin, int enemyCountMax)
        {
            Markov = markov;
            Columns = numColumns;
            Rows = numRows;
            WallCount= new Count(wallCountMin, wallCountMax);
            ItemCount = new Count(itemCountMin, itemCountMax);
            EnemyCount = new Count(enemyCountMin, enemyCountMax);
            SetupScene();
        }

        public Board(IReadOnlyCollection<List<int>> board, Markov markov, int numColumns, int numRows, int wallCountMin, int wallCountMax, int itemCountMin, int itemCountMax, int enemyCountMin, int enemyCountMax)
        {
            Validated = false;
            CopyBoard(board);
            this.Markov = markov;
            Columns = numColumns;
            Rows = numRows;
            WallCount = new Count(wallCountMin, wallCountMax);
            ItemCount = new Count(itemCountMin, itemCountMax);
            EnemyCount = new Count(enemyCountMin, enemyCountMax);
            GoalPos.Add(0); GoalPos.Add(Columns - 1);
            Console.WriteLine(Builder(board));
            Validated = ValidateBoard();
        }

        //Function to make a copy of a level
        private void CopyBoard(IEnumerable<List<int>> initial)
        {
            foreach (var initialRow in initial.Select(row => row.ToList()))
            {
                board.Add(initialRow);
            }
        }

        public void BoardSetup()
        {
            int[][] allyPositions = { new[] { Rows - 3, 0 }, new[] { Rows - 2, 0 },
                new[] { Rows - 1, 2 }, new[] { Rows - 1, 1 }, new[] { Rows - 1, 0 } };

            GoalPos.Add(0); GoalPos.Add(Columns - 1);
            for (int i = 0; i < Columns; i++)
            {
                List<int> row = new List<int>();
                for (int j = 0; j < Rows; j++)
                {
                    bool skipPosition = false;
                    List<int> pos = new List<int> {i, j};
                    if (pos.SequenceEqual(GoalPos)) skipPosition = true;
                    foreach (var allyPos in allyPositions)
                    {
                        if (allyPos.SequenceEqual(pos))
                        {
                            skipPosition = true;
                        }
                    }

                    row.Add(0);
                    if (skipPosition) continue;
                    Positions.Add(pos);
                }
                board.Add(row);
            }

            Positions.Remove(GoalPos);
        }

        public void UpdateTile(List<int> pos, int tile)
        {
            board[pos[0]][pos[1]] = tile;
        }

        private List<int> RandomPosition()
        {
            Random rand = new Random();
            int index = rand.Next(Positions.Count - 1);
            List<int> pos = Positions[index];
            Positions.RemoveAt(index);
            return pos;
        }

        private void LayoutObjectAtRandom(int type, int minimum, int maximum)
        {
            Random rand = new Random();
            int objectCount = rand.Next(minimum, maximum + 1);

            for (int i = 0; i < objectCount; i++)
            {
                List<int> pos = RandomPosition();
                board[pos[0]][pos[1]] = type;
            }
        }

        private void InitialiseAllies()
        {
            int[][] allyPositions = { new[] { Rows - 3, 0 }, new[] { Rows - 2, 0 },
                new[] { Rows - 1, 2 }, new[] { Rows - 1, 1 }, new[] { Rows - 1, 0 } };
            foreach (var pos in allyPositions)
            {
                List<int> allyPos = new List<int> {pos[0], pos[1]};
                Positions.Remove(allyPos);
                board[allyPos[0]][allyPos[1]] = 5;
            }
        }

        public bool OutOfRange(List<int> position)
        {
            return Rows - 1 < position[0] || position[0] < 0 || Columns - 1 < position[1] || position[1] < 0;
        }

        public int CheckPosition(List<int> position)
        {
            try
            {
                if (OutOfRange(position))
                    Console.WriteLine("Position out of range: " + position[0] + " " + position[1]);
                return board[position[0]][position[1]];
            }
            catch (ArgumentOutOfRangeException e)
            {
                Console.WriteLine(e);
                return 0;
            }
        }

        //Determine whether board must go in infeasible or fesaible population
        public bool ValidateBoard(bool isSmooth = false)
        {
            CheckTiles();
            if (!CheckForGoal())
                return false;
            if (NumAllies != 5)
                return false;
            if (NumEnemies != EnemyCount.Maximum)
                return false;
            if (NumItems != ItemCount.Maximum)
                return false;
            if (!isSmooth && (NumWalls < WallCount.Minimum || NumWalls > WallCount.Maximum))
                return false;

            bool pathToGoal = false;
            bool[] items = new bool[NumItems];
            bool[] enemyBools = new bool[NumEnemies];


            foreach (var pos in EnemyPositions)
            {
                board[pos[0]][pos[1]] = 0;
            }

            foreach (var ally in Allies)
            {
                var (_, found) = ally.FindPath(ally.Pos, GoalPos);
                if (found)
                {
                    Console.WriteLine("path to goal found");
                    pathToGoal = true;
                }

                for (int i = 0; i < ItemPositions.Count; i++)
                {
                    var (_, itemFound) = ally.FindPath(ally.Pos, ItemPositions[i]);
                    if (itemFound)
                        items[i] = true;
                }
                for (int i = 0; i < EnemyPositions.Count; i++)
                {
                    var (_, enemyFound) = ally.FindPath(ally.Pos, EnemyPositions[i]);
                    if (enemyFound)
                        enemyBools[i] = true;
                }
            }

            foreach (var enemy in Enemies)
            {
                board[enemy.Pos[0]][enemy.Pos[1]] = 4;
            }

            return pathToGoal && items.All(pathToItem => pathToItem) && enemyBools.All(pathToEnemy => pathToEnemy);
        }

        private void CheckTiles()
        {
            List<List<int>> enemyPosList = new List<List<int>>();
            List<List<int>> allyPosList = new List<List<int>>();
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Rows; j++)
                {
                    List<int> pos = new List<int> { i, j };
                    var tile = CheckPosition(pos);
                    switch (tile)
                    {
                        case 1:
                            NumWalls++;
                            break;
                        case 2:
                            NumItems++;
                            ItemPositions.Add(pos);
                            break;
                        case 3:
                            break;
                        case 4:
                            NumEnemies++;
                            enemyPosList.Add(pos);
                            break;
                        case 5:
                            NumAllies++;
                            allyPosList.Add(pos);
                            break;
                    }
                }
            }
            UpdateEnemies(enemyPosList);
            UpdateAllies(allyPosList);
        }

        private void UpdateEnemies(List<List<int>> posList)
        {
            if (posList.Count == 0) return;

            for (int i = 0; i < posList.Count; i++)
            {
                var pos = posList[i];
                float id = 4.0f + (i+1) / 10f;
                Enemy enemy = new Enemy(this, id, pos);
                EnemyPositions.Add(pos);
                Enemies.Add(enemy);
            }
        }

        private void UpdateAllies(List<List<int>> posList)
        {
            if (posList.Count == 0) return;

            for (int i = 0; i < posList.Count; i++)
            {
                var pos = posList[i];
                float id = 5.0f + (i + 1) / 10f;
                Ally ally = new Ally(this, id, pos);
                Allies.Add(ally);
            }
        }

        public void SetupScene()
        {
            //0=Floor,1=Wall,2=Item,3=goal,4=enemy,5=player
            BoardSetup();
            InitialiseAllies();
            board[GoalPos[0]][GoalPos[1]] = 3;
            LayoutObjectAtRandom(1, WallCount.Minimum, WallCount.Maximum);
            LayoutObjectAtRandom(2, ItemCount.Minimum, ItemCount.Maximum);
            LayoutObjectAtRandom(4, EnemyCount.Minimum, EnemyCount.Maximum);
            Validated = ValidateBoard();
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

        public bool CheckForGoal()
        {
            return board[0][Columns-1] == 3;
        }
    }

}

