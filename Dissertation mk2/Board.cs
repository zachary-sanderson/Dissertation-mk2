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
            public int minimum;
            public int maximum;

            public Count(int min, int max)
            {
                minimum = min;
                maximum = max;
            }
        }
        
        public int columns;
        public int rows;
        public Count wallCount;
        public Count itemCount;
        public Count enemyCount;

        public GameManager gameManager;
        public Markov markov;
        public List<List<int>> board = new List<List<int>>();
        public List<List<int>> positions = new List<List<int>>();
        public List<List<int>> itemPositions = new List<List<int>>();
        public List<List<int>> enemyPositions = new List<List<int>>();
        public List<int> goalPos = new List<int>();

        public List<Enemy> enemies = new List<Enemy>();
        public List<Ally> allies = new List<Ally>();

        public bool validated;
        public int score = 0;
        public int itemValue = 1;

        public int numItems;
        public int numEnemies;
        public int numWalls;
        public int numAllies;

        private readonly int[][] neighbourPositions = 
        {
            new[] { 0, 1 }, new[] { 0, -1 }, new[] { 1, 0 }, new[] { -1, 0 }, new[] { 1, 1 },
            new[] { -1, 1 }, new[] { -1, -1 }, new[] { 1, -1 } 
        };




        /*
        public Board()
        {
            SetupSmoothScene();
            Console.WriteLine(Builder(board));
        }
        */

        public Board(Markov markov, int numColumns, int numRows, int wallCountMin, int wallCountMax, int itemCountMin, int itemCountMax, int enemyCountMin, int enemyCountMax, bool smooth = false)
        {
            this.markov = markov;
            columns = numColumns;
            rows = numRows;
            wallCount= new Count(wallCountMin, wallCountMax);
            itemCount = new Count(itemCountMin, itemCountMax);
            enemyCount = new Count(enemyCountMin, enemyCountMax);
            if (smooth) SetupSmoothScene();
            else SetupScene();
        }

        public Board(List<List<int>> board, Markov markov, int numColumns, int numRows, int wallCountMin, int wallCountMax, int itemCountMin, int itemCountMax, int enemyCountMin, int enemyCountMax)
        {
            validated = false;
            CopyBoard(board);
            this.markov = markov;
            columns = numColumns;
            rows = numRows;
            wallCount = new Count(wallCountMin, wallCountMax);
            itemCount = new Count(itemCountMin, itemCountMax);
            enemyCount = new Count(enemyCountMin, enemyCountMax);
            goalPos.Add(0); goalPos.Add(columns - 1);
            Console.WriteLine(Builder(board));
            validated = ValidateBoard();
        }

        private void CopyBoard(IEnumerable<List<int>> initial)
        {
            foreach (var initialRow in initial.Select(row => row.ToList()))
            {
                board.Add(initialRow);
            }
        }

        public bool Equals(List<List<int>> obj)
        {
            bool equals = true;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    if (board[i][j] != obj[i][j]) equals = false;
                }
            }

            return equals;
        }

        public void BoardSetup()
        {
            int[][] allyPositions = { new[] { rows - 3, 0 }, new[] { rows - 2, 0 },
                new[] { rows - 1, 2 }, new[] { rows - 1, 1 }, new[] { rows - 1, 0 } };

            goalPos.Add(0); goalPos.Add(columns - 1);
            for (int i = 0; i < columns; i++)
            {
                List<int> row = new List<int>();
                for (int j = 0; j < rows; j++)
                {
                    bool skipPosition = false;
                    List<int> pos = new List<int> {i, j};
                    if (pos.SequenceEqual(goalPos)) skipPosition = true;
                    foreach (var allyPos in allyPositions)
                    {
                        if (allyPos.SequenceEqual(pos))
                        {
                            skipPosition = true;
                        }
                    }

                    row.Add(0);
                    if (skipPosition) continue;
                    positions.Add(pos);
                }
                board.Add(row);
            }

            positions.Remove(goalPos);
        }

        public void UpdateTile(List<int> pos, int tile)
        {
            board[pos[0]][pos[1]] = tile;
        }

        private List<int> RandomPosition()
        {
            Random rand = new Random();
            int index = rand.Next(positions.Count - 1);
            List<int> pos = positions[index];
            positions.RemoveAt(index);
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
            int[][] allyPositions = { new[] { rows - 3, 0 }, new[] { rows - 2, 0 },
                new[] { rows - 1, 2 }, new[] { rows - 1, 1 }, new[] { rows - 1, 0 } };
            foreach (var pos in allyPositions)
            {
                List<int> allyPos = new List<int> {pos[0], pos[1]};
                positions.Remove(allyPos);
                board[allyPos[0]][allyPos[1]] = 5;
            }
        }

        public bool OutOfRange(List<int> position)
        {
            return rows - 1 < position[0] || position[0] < 0 || columns - 1 < position[1] || position[1] < 0;
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

        public bool ValidateBoard(bool isSmooth = false)
        {
            CheckTiles();
            if (!CheckForGoal())
                return false;
            if (numAllies != 5)
                return false;
            if (numEnemies != enemyCount.maximum)
                return false;
            if (numItems != itemCount.maximum)
                return false;
            if (!isSmooth && (numWalls < wallCount.minimum || numWalls > wallCount.maximum))
                return false;

            bool pathToGoal = false;
            bool[] items = new bool[numItems];
            bool[] enemyBools = new bool[numEnemies];


            foreach (var pos in enemyPositions)
            {
                board[pos[0]][pos[1]] = 0;
            }

            foreach (var ally in allies)
            {
                var (_, found) = ally.FindPath(ally.pos, goalPos);
                if (found)
                {
                    Console.WriteLine("path to goal found");
                    pathToGoal = true;
                }

                for (int i = 0; i < itemPositions.Count; i++)
                {
                    var (_, itemFound) = ally.FindPath(ally.pos, itemPositions[i]);
                    if (itemFound)
                        items[i] = true;
                }
                for (int i = 0; i < enemyPositions.Count; i++)
                {
                    var (_, enemyFound) = ally.FindPath(ally.pos, enemyPositions[i]);
                    if (enemyFound)
                        enemyBools[i] = true;
                }
            }

            foreach (var enemy in enemies)
            {
                board[enemy.pos[0]][enemy.pos[1]] = 4;
            }

            return pathToGoal && items.All(pathToItem => pathToItem) && enemyBools.All(pathToEnemy => pathToEnemy);
        }

        private void CheckTiles()
        {
            List<List<int>> enemyPosList = new List<List<int>>();
            List<List<int>> allyPosList = new List<List<int>>();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    List<int> pos = new List<int> { i, j };
                    var tile = CheckPosition(pos);
                    switch (tile)
                    {
                        case 1:
                            numWalls++;
                            break;
                        case 2:
                            numItems++;
                            itemPositions.Add(pos);
                            break;
                        case 3:
                            break;
                        case 4:
                            numEnemies++;
                            enemyPosList.Add(pos);
                            break;
                        case 5:
                            numAllies++;
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
                enemyPositions.Add(pos);
                enemies.Add(enemy);
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
                allies.Add(ally);
            }
        }

        public void SetupScene()
        {
            //0=Floor,1=Wall,2=Item,3=goal,4=enemy,5=player
            BoardSetup();
            InitialiseAllies();
            board[goalPos[0]][goalPos[1]] = 3;
            LayoutObjectAtRandom(1, wallCount.minimum, wallCount.maximum);
            LayoutObjectAtRandom(2, itemCount.minimum, itemCount.maximum);
            LayoutObjectAtRandom(4, enemyCount.minimum, enemyCount.maximum);
            validated = ValidateBoard();
        }

        //*****************************************************************
        //             TESTING SMOOTH BOARD
        //*****************************************************************
        
        public void SetupSmoothScene()
        {
            //0=Floor,1=Wall,2=Item,3=goal,4=enemy,5=player
            BoardSetup();
            InitialiseAllies();
            board[goalPos[0]][goalPos[1]] = 3;
            LayoutObjectAtRandom(1, wallCount.minimum, wallCount.maximum);
            SmoothBoard();
            LayoutObjectAtRandom(2, itemCount.minimum, itemCount.maximum);
            LayoutObjectAtRandom(4, enemyCount.minimum, enemyCount.maximum);
            validated = ValidateBoard(true);
            Console.WriteLine(validated);
        }

        private void SmoothBoard()
        {
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    int tile = (int)board[i][j];
                    switch (tile)
                    {
                        case 0:
                            Smooth(false, i, j);
                            break;
                        case 1:
                            Smooth(true, i, j);
                            break;
                    }
                }
            }
        }

        private void Smooth(bool isWall, int i, int j)
        {
            var count = CheckNeighbours(i, j);
            if (isWall)
            {
                if (count < 2) board[i][j] = 0;
            }
            else
            {
                if (count <= 4) return;
                board[i][j] = 1;
                positions.Remove(new List<int> { i, j });
            }
        }

        private int CheckNeighbours(int i, int j)
        {
            int count = 0;
            foreach (var position in neighbourPositions)
            {
                var newPos = new List<int> { i + position[0], j + position[1] };
                if (OutOfRange(newPos)) count++;
                else if ((int)board[newPos[0]][newPos[1]] == 1) count++;
            }
            return count;
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
            return board[0][columns-1] == 3;
        }
    }

}

