using System;
using System.Collections.Generic;
using System.Linq;

namespace Dissertation_mk2
{
    public class Ally : Unit
    {
        public int initialHp;
        private List<List<int>> pathToGoal;
        private List<List<List<int>>> enemyPaths = new List<List<List<int>>>();
        private List<List<List<int>>> itemPaths = new List<List<List<int>>>();

        public Ally(Board board, float id, List<int> pos)
        {
            this.board = board;
            this.id = id;
            this.pos = pos;
            initialHp = hp;
        }

        //Take turn unless checking the anxiety presented by a future turn.
        public void TakeTurn()
        {
            List<int> startPos = new List<int> { pos[0], pos[1] };
            Console.WriteLine(id);
            if (CanMove())
            {
                CheckMoves();
                Move();
            }
            else
                Console.WriteLine("Can't Move.");

            List<int> endPos = new List<int> { pos[0], pos[1] };
            board.gameManager.AddMove(new Move(board.gameManager.turnCount, Dissertation_mk2.Move.UnitType.Ally, startPos, endPos, hasAttacked));
            hasAttacked = false;
        }

        private bool NearGameOver()
        {
            int combinedHp = board.gameManager.allies.Sum(ally => ally.hp);

            return combinedHp < 6;
        }

        private void CheckMoves()
        {
            foreach (var enemy in board.gameManager.enemies)
            {
                if (enemy.hp <= 0) continue;

                var path = FindPath(pos, enemy.pos);
                enemyPaths.Add(path.Item1);

                if (path.Item1.Count < 6 && path.Item2)
                {
                    Console.WriteLine("Path to Enemy " + enemy.id + ":");
                    foreach (var node in path.Item1)
                    {
                        Console.WriteLine(node[0] + " " + node[1]);
                    }
                    enemiesInRange.Add(enemy.pos);
                }
            }

            foreach (var item in board.itemPositions)
            {
                var path = FindPath(pos, item);
                itemPaths.Add(path.Item1);
                if (path.Item1.Count < 6 && path.Item2)
                {
                    Console.WriteLine("item in range: " + item[0] + " " + item[1]);
                    itemsInRange.Add(item);
                }
            }

            var goalPath = FindPath(pos, board.goalPos);
            pathToGoal = goalPath.Item1;
            if (pathToGoal.Count < 6 && goalPath.Item2)
            {
                Console.WriteLine("Path to goal:");
                foreach (var node in pathToGoal)
                {
                    Console.WriteLine(node[0] + " " + node[1]);
                }
                goalInRange = true;
            }
        }

        private void Move()
        {
            bool lowHp = NearGameOver();
            Console.WriteLine("enemies in range:" + enemiesInRange.Count);
            Console.WriteLine("item in range:" + itemsInRange.Count);

            if (goalInRange && (board.markov.Aggressive || board.markov.Speedy))
            {
                SwapPosition(board.goalPos);
                board.gameManager.LevelComplete();
            }
            else if (board.gameManager.cSkill > 0 && lowHp)
            {
                List<int> move = pathToGoal.Count < range + 1 ? pathToGoal.Last() : pathToGoal[range];
                SwapPosition(move);
            }
            else if (enemiesInRange.Count > 0)
            {
                Enemy unit = FindTarget();
                if (unit != null)
                {
                    if (board.gameManager.cSkill > 2)
                    {
                        var move = BestAttack(unit.pos);
                        board.gameManager.allyTargetPositions.Add(unit.pos);
                        SwapPosition(move);
                    }
                    else
                    {
                        var move = FindMove(unit.pos);
                        if (move.Item1 != null && move.Item2)
                        {
                            if (board.gameManager.cSkill > 1)
                                board.gameManager.allyTargetPositions.Add(move.Item1);
                            SwapPosition(move.Item1);
                        }
                    }

                    Attack(unit);
                }
                else
                {
                    var move = BestMove();
                    if (move != null)
                    {
                        if (board.gameManager.cSkill > 2)
                            board.gameManager.allyTargetPositions.Add(move);
                        SwapPosition(move);
                    }
                }
            }
            else if (itemsInRange.Count > 0)
            {
                List<int> move = itemsInRange[0];
                itemsInRange.Remove(move);
                board.itemPositions.Remove(move);
                SwapPosition(move);
                board.score += board.itemValue;
            }
            else if (goalInRange)
            {
                SwapPosition(board.goalPos);
                board.gameManager.LevelComplete();
            }
            else
            {
                List<int> move = BestMove();
                if (move != null)
                { 
                    if (board.gameManager.cSkill > 2)
                        board.gameManager.allyTargetPositions.Add(move);
                    SwapPosition(move);
                }
            }
        }

        /*Moves towards enemy with lowest Hp that's within 2 moves away*/
        /*If there are no enemies in range, moves towards the goal*/
        private List<int> BestMove()
        {
            if (board.markov.Aggressive && !NearGameOver())
            {
                int lowestHp = 5;
                Enemy target = null;

                var enemies = CheckEnemiesInRange(range * 2 + 1);

                foreach (var enemy in enemies.Where(enemy => enemy.hp <= lowestHp))
                {
                    if (board.gameManager.cSkill < 2)
                    {
                        lowestHp = enemy.hp;
                        target = enemy;
                    }
                    else if (IsSmartMove(enemy.pos))
                    {
                        lowestHp = enemy.hp;
                        target = enemy;
                    }
                }

                if (target == null && !goalInRange)
                {
                    return pathToGoal.Count > range ? pathToGoal[range] : pathToGoal.Last();
                }
                return FindMove(target?.pos).Item1;
            }

            if (board.markov.Speedy)
            {
                return pathToGoal.Count > range ? pathToGoal[range] : pathToGoal.Last();
            }

            List<int> move = null;
            int pathLength = 160;
            if (board.markov.Explorer)
            {
                foreach (var path in itemPaths.Where(path => path.Count < pathLength))
                {
                    if (board.gameManager.cSkill < 2)
                    {
                        if (path.Count < range + 1)
                        {
                            pathLength = path.Count;
                            move = path.Last();
                        }
                        else
                        {
                            pathLength = path.Count;
                            move = path[range];
                        }
                    }
                    else if (IsSmartMove(path[-1]))
                    {
                        if (path.Count < range + 1)
                        {
                            pathLength = path.Count;
                            move = path.Last();
                        }
                        else
                        {
                            pathLength = path.Count;
                            move = path[range];
                        }
                    }
                }
            }

            if (move != null)
                return move;
            return pathToGoal.Count < range + 1 ? pathToGoal.Last() : pathToGoal[range];
        }

        private bool IsSmartMove(List<int> move)
        {
            int count = board.gameManager.allyTargetPositions.Count(allyMove => move.SequenceEqual(move));
            return count <= 2;
        }

        protected Enemy FindTarget()
        {
            List<Enemy> targets = new List<Enemy>();
            List<float> enemyIds = enemiesInRange.Select(position => board.CheckPosition(position)).ToList();

            targets.AddRange(from id in enemyIds
                from unit in board.gameManager.enemies
                where id == unit.id
                select unit);
            
            return LowestHp(targets);
        }

        private static Enemy LowestHp(IEnumerable<Enemy> targets)
        {
            var lowestHp = 5;
            bool engagedEnemy = false;
            Enemy enemy = null;
            foreach (var unit in targets)
            {
                if (unit.hp == 1)
                {
                    enemy = unit;
                    lowestHp = unit.hp;
                }
                else if (unit.engaged && lowestHp != 1 && !unit.isDead)
                {
                    engagedEnemy = true;
                    enemy = unit;
                }
                else if (!engagedEnemy && unit.hp <= lowestHp && !unit.isDead)
                {
                    lowestHp = unit.hp;
                    enemy = unit;
                }
            }

            return enemy;
        }

        protected List<int> BestAttack(List<int> enemyPos)
        {
            List<List<int>> validPositions = new List<List<int>>();
            foreach (var direction in directions)
            {
                List<int> move = enemyPos.Select((t, i) => t + direction[i]).ToList();
                if ((int)board.CheckPosition(move) != 0) continue;
                var (path, targetFound) = FindPath(pos, move);
                if (targetFound && path.Count < range + 1) validPositions.Add(move);
            }

            int lowestNum = 5;
            List<int> bestPosition = null;
            
            foreach (List<int> position in validPositions)
            {
                int numEnemies = 0;
                foreach (var enemy in board.gameManager.enemies)
                {
                    var (path, targetFound) = FindPath(pos, position);
                    if (targetFound && path.Count < range + 1) numEnemies++;
                }
                if (numEnemies < lowestNum)
                {
                    lowestNum = numEnemies;
                    bestPosition = position;
                }
            }

            return bestPosition;
        }

        private IEnumerable<Enemy> CheckEnemiesInRange(int shortestDistance)
        {
            List<Enemy> enemies = new List<Enemy>();

            enemies.AddRange(from enemy in board.gameManager.enemies let enemyDist = CheckDistance(pos, enemy.pos) 
                where enemyDist <= shortestDistance select enemy);

            return enemies;
        }


        public void Attack(Enemy unit)
        {
            hasAttacked = true;
            Console.WriteLine(id + " attacking " + unit.id + ", enemy hp is " + unit.hp);
            if (unit.engaged)
            {
                Console.WriteLine("After attack " + id + " health is " + hp + " and "
                                  + unit.id + " health is " + unit.hp);
                unit.hp -= dmg;
                CheckIfDead(unit);
            }
            else
            {
                unit.hp -= dmg;
                if (unit.hp > 0)
                {
                    hp -= unit.dmg;
                    Console.WriteLine("After attack " + id + " health is " + hp + " and "
                                      + unit.id + " health is " + unit.hp);
                    unit.engaged = true;
                    unit.CheckIfDead(this);
                }
                else
                    CheckIfDead(unit);
            }
        }

        public void CheckIfDead(Enemy enemy)
        {
            if (enemy == null || enemy.hp > 0) return;
            Console.WriteLine("unit dead");
            board.board[enemy.pos[0]][enemy.pos[1]] = 0;
            board.gameManager.EnemyDead(enemy);
        }

        public void EndTurn()
        {
            if (isDead)
            {
                board.gameManager.RemoveAllyFromList(this);
            }
            else
            {
                moves.Clear();
                itemsInRange.Clear();
                enemiesInRange.Clear();
                alliesInRange.Clear();
                pathToGoal?.Clear();
                enemyPaths.Clear();
                itemPaths.Clear();
                goalInRange = false;
            }
        }

        public void UpdateHp()
        {
            initialHp = hp;
        }

        public int CheckAnxiety()
        {
            int numEnemies = 0;
            foreach (var enemy in board.gameManager.enemies.Where(enemy => enemy.hp > 0))
            {
                var (item1, item2) = FindPath(pos, enemy.pos);
                if (item1.Count < 6 && item2)
                {
                    numEnemies += 1;
                }
            }

            return numEnemies;
        }
    }
}