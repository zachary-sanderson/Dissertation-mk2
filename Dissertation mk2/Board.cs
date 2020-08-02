using System;
using System.Collections.Generic;
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
        
        public int columns = 25;
        public int rows = 25;
        public Count wallCount = new Count(100, 120);
        public Count itemCount = new Count(4, 4);
        public Count enemyCount = new Count(5, 5);

        private int[][] neighbourPositions = { new[] { 1, 0 }, new[] { -1, 0 },
            new[] { 0, 1 }, new[] { 0, -1 }, new[] { 1, 1 }, new[] { 1, -1 },
            new[] { -1, 1 }, new[] { -1, -1 } };

        public GameManager gameManager;
        public Markov markov;
        public List<List<float>> board = new List<List<float>>();
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
        private bool hasGoal;

        /*
        public Board()
        {
            SetupSmoothScene();
            Console.WriteLine(Builder(board));
        }
        */

        public Board(double pValue)
        {
            markov = new Markov(pValue);
            SetupScene();
        }

        public Board(List<List<float>> board, double pValue)
        {
            this.board = board;
            markov = new Markov(pValue);
            goalPos.Add(0); goalPos.Add(columns - 1);
            validated = ValidateBoard();
        }

        public void BoardSetup()
        {
            int[][] allyPositions = { new[] { rows - 3, 0 }, new[] { rows - 2, 0 },
                new[] { rows - 1, 2 }, new[] { rows - 1, 1 }, new[] { rows - 1, 0 } };

            goalPos.Add(0); goalPos.Add(columns - 1);
            for (int i = 0; i < columns; i++)
            {
                List<float> row = new List<float>();
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
            for (int i = 0; i < allyPositions.Length; i++)
            {
                List<int> allyPos = new List<int> {allyPositions[i][0], allyPositions[i][1]};
                positions.Remove(allyPos);
                float id = 5 + (i + 1f) / 10f;
                board[allyPos[0]][allyPos[1]] = id;
            }
        }

        public bool OutOfRange(List<int> position)
        {
            return rows - 1 < position[0] || position[0] < 0 || columns - 1 < position[1] || position[1] < 0;
        }

        public float CheckPosition(List<int> position)
        {
            if (OutOfRange(position))
                Console.WriteLine("Position out of range: " + position[0] + " " + position[1]);
            if (position.Count == 2)
                return board[position[0]][position[1]];
            Console.WriteLine("Invalid CheckPosition, Count != 2");
            return 0f;
        }

        public bool ValidateBoard(bool isSmooth = false)
        {
            CheckTiles();
            if (!hasGoal)
                return false;
            if (numAllies != 5)
                return false;
            if (numEnemies != 5)
                return false;
            if (numItems != 4)
                return false;
            if (!isSmooth && (numWalls < wallCount.minimum || numWalls > wallCount.maximum))
                return false;

            bool pathToGoal = false;
            bool[] items = new bool[numItems];

            foreach (var pos in enemyPositions)
            {
                board[pos[0]][pos[1]] = 0f;
            }

            foreach (var ally in allies)
            {
                var (_, found) = ally.FindPath(ally.pos, goalPos);
                if (found)
                    pathToGoal = true;
                for (int i = 0; i < itemPositions.Count; i++)
                {
                    var (_, itemFound) = ally.FindPath(ally.pos, itemPositions[i]);
                    if (itemFound)
                        items[i] = true;
                }
            }

            foreach (var enemy in enemies)
            {
                board[enemy.pos[0]][enemy.pos[1]] = enemy.id;
            }

            return pathToGoal && items.All(pathToItem => pathToItem);
        }

        private void CheckTiles()
        {
            List<List<int>> enemyPosList = new List<List<int>>();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    List<int> pos = new List<int> { i, j };
                    float tile = CheckPosition(pos);
                    switch ((int)tile)
                    {
                        case 1:
                            numWalls++;
                            break;
                        case 2:
                            numItems++;
                            itemPositions.Add(pos);
                            break;
                        case 3:
                            hasGoal = true;
                            break;
                        case 4:
                            numEnemies++;
                            enemyPosList.Add(pos);
                            break;
                        case 5:
                            numAllies++;
                            Ally ally = new Ally(this, tile, pos);
                            allies.Add(ally);
                            break;

                    }
                }
            }
            UpdateEnemies(enemyPosList);
        }

        public void UpdateEnemies(List<List<int>> posList)
        {
            if (posList.Count == 0) return;

            for (int i = 0; i < posList.Count; i++)
            {
                var pos = posList[i];
                float id = 4.0f + (i+1) / 10f;
                Enemy enemy = new Enemy(this, id, pos);
                board[pos[0]][pos[1]] = id;
                enemyPositions.Add(pos);
                enemies.Add(enemy);
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
        /*
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
        */
    }

}

