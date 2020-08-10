using System;
using System.Collections.Generic;
using System.Linq;

namespace Dissertation_mk2
{
    public class Ally : Unit
    {
        private readonly List<List<List<int>>> enemyPaths = new List<List<List<int>>>();
        private readonly List<List<List<int>>> itemPaths = new List<List<List<int>>>();
        private List<List<int>> pathToGoal;

        public Ally(Board board, float id, List<int> pos)
        {
            this.Board = board;
            this.Id = id;
            this.Pos = pos;
            InitialHp = Hp;
        }


        public void TakeTurn()
        {
            Console.WriteLine(Id);
            if (CanMove())
            {
                CheckMoves();
                Move();
            }
            else
            {
                Console.WriteLine("Can't Move.");
            }
        }


        //**********************************************************************************
        //    CHECK IF GOAL, ENEMIES OR ITEMS ARE IN RANGE SO OPTIMAL MOVE CAN BE DETERMINED
        //**********************************************************************************
        private void CheckMoves()
        {
            CheckForGoal();
            CheckForEnemies();
            CheckForItems();
        }

        private void CheckForGoal()
        {
            var (path, reachable) = FindPath(Pos, Board.GoalPos);
            pathToGoal = path;
            if (pathToGoal.Count < 6 && reachable)
            {
                Console.WriteLine("Path to goal:");
                foreach (var node in pathToGoal) Console.WriteLine(node[0] + " " + node[1]);
                GoalInRange = true;
            }
        }

        private void CheckForEnemies()
        {
            foreach (var enemy in Board.GameManager.Enemies.Where(enemy => enemy.Hp > 0))
            {
                var (path, reachable) = FindPath(Pos, enemy.Pos);
                enemyPaths.Add(path);

                if (path.Count < Range + 1 && reachable)
                {
                    Console.WriteLine("Path to Enemy " + enemy.Id + ":");
                    foreach (var node in path) Console.WriteLine(node[0] + " " + node[1]);
                    EnemiesInRange.Add(enemy.Pos);
                }
            }
        }

        private void CheckForItems()
        {
            foreach (var item in Board.ItemPositions)
            {
                var (path, reachable) = FindPath(Pos, item);
                itemPaths.Add(path);
                if (path.Count < 6 && reachable)
                {
                    Console.WriteLine("item in range: " + item[0] + " " + item[1]);
                    ItemsInRange.Add(item);
                }
            }
        }

        //Decide best move based on strategy
        private void Move()
        {
            Console.WriteLine("enemies in range:" + EnemiesInRange.Count);
            Console.WriteLine("item in range:" + ItemsInRange.Count);
            Console.WriteLine("score:" + Board.Score + " numitems: " + Board.NumItems + " EnemiesKilled:" +
                              Board.GameManager.EnemiesKilled + " numEnemies:" + Board.NumEnemies);
            //If all items collected and enemies dead move towards goal.
            if (Board.Score == Board.NumItems && Board.GameManager.EnemiesKilled == Board.NumEnemies)
            {
                if (GoalInRange)
                {
                    SwapPosition(Board.GoalPos);
                    Board.GameManager.LevelComplete();
                }
                else
                {
                    var move = pathToGoal.Count < Range + 1 ? pathToGoal.Last() : pathToGoal[Range];
                    SwapPosition(move);
                }
            }
            else if (EnemiesInRange.Count > 0)
            {
                FindEnemyToAttack();
            }
            else if (ItemsInRange.Count > 0)
            {
                MoveToItem();
            }
            else
            {
                var move = BestMove();
                if (move != null) SwapPosition(move);
            }
        }

        //Finds and attacks enemy unit in range with the lowest hp. If null calls BestMove() and moves to returned pos instead.
        private void FindEnemyToAttack()
        {
            var unit = FindTarget();
            if (unit != null)
            {
                var (move, targetFound) = FindMove(unit.Pos);
                if (move != null && targetFound) SwapPosition(move);

                Attack(unit);
            }
            else
            {
                var move = BestMove();
                if (move != null) SwapPosition(move);
            }
        }

        protected Enemy FindTarget()
        {
            var targets = new List<Enemy>();

            foreach (var enemyPos in EnemiesInRange)
            foreach (var enemy in Board.GameManager.Enemies)
                if (enemyPos.SequenceEqual(enemy.Pos))
                    targets.Add(enemy);

            return LowestHp(targets);
        }

        private static Enemy LowestHp(IEnumerable<Enemy> targets)
        {
            var lowestHp = 5;
            var engagedEnemy = false;
            Enemy enemy = null;
            foreach (var unit in targets)
                if (unit.Hp == 1)
                {
                    enemy = unit;
                    lowestHp = unit.Hp;
                }
                else if (unit.Engaged && lowestHp != 1 && !unit.IsDead)
                {
                    engagedEnemy = true;
                    enemy = unit;
                }
                else if (!engagedEnemy && unit.Hp <= lowestHp && !unit.IsDead)
                {
                    lowestHp = unit.Hp;
                    enemy = unit;
                }

            return enemy;
        }

        private void MoveToItem()
        {
            var move = ItemsInRange[0];
            ItemsInRange.Remove(move);
            Board.ItemPositions.Remove(move);
            SwapPosition(move);
            Board.Score += Board.ItemValue;
            ItemPickup = true;
        }


        /*Moves towards enemy with lowest Hp that's within 2 moves away*/
        /*If there are no enemies in range, moves towards the goal*/
        private List<int> BestMove()
        {
            List<int> move;

            if (Board.Markov.Spread)
            {
                move = BestSpreadMove();
            }

            else if (Board.Markov.PrioritiseEnemies)
            {
                var enemyAlive = false;
                foreach (var _ in Board.GameManager.Enemies.Where(enemy => enemy.Hp > 0)) enemyAlive = true;
                move = enemyAlive ? BestPrioritiseEnemies() : SpreadForItems();
            }

            else
            {
                var enemyAlive = false;
                foreach (var _ in Board.GameManager.Enemies.Where(enemy => enemy.Hp > 0)) enemyAlive = true;
                move = enemyAlive ? BestGroupUpMove() : SpreadForItems();
            }


            if (move != null)
                return move;
            return pathToGoal.Count < Range + 1 ? pathToGoal.Last() : pathToGoal[Range];
        }


        //Return move closest to the nearest item that DOES NOT have too many allies moving towards it this turn already
        private List<int> BestSpreadMove()
        {
            //Sort by itemPaths by shortest
            itemPaths.Sort((a, b) => a.Count - b.Count);

            if (Board.GameManager.Enemies.Count == 0 && itemPaths.Count > 0)
                return itemPaths[0].Count < Range + 1 ? itemPaths[0].Last() : itemPaths[0][Range];

            foreach (var itemPath in itemPaths)
            {
                //get the number of enemies within 10 spaces of item
                var count = Board.GameManager.Enemies.Where(enemy => enemy.Hp > 0)
                    .Count(enemy => CheckDistance(itemPath.Last(), enemy.Pos) < Range * 2 + 1);

                if (TooManyAlliesAlready(itemPath.Last(), count)) continue;

                Board.GameManager.AllyTargetPositions.Add(itemPath.Last());
                return itemPath.Count < Range + 1 ? itemPath.Last() : itemPath[Range];
            }

            if (enemyPaths.Count == 0) return null;
            enemyPaths.Sort((a, b) => a.Count - b.Count);
            return enemyPaths[0].Count < Range + 1 ? enemyPaths[0].Last() : enemyPaths[0][Range];
        }


        //Given an item position and the number of enemies near it,
        //the function returns false if too many allies are already moving towards the item position
        private bool TooManyAlliesAlready(IReadOnlyCollection<int> move, int numEnemies)
        {
            var count = Board.GameManager.AllyTargetPositions.Count(allyMove => allyMove.SequenceEqual(move));
            return count <= numEnemies;
        }


        //If PrioritiseEnemies or GroupUp strategy and all enemies dead spreads allies out to get remaining items
        private List<int> SpreadForItems()
        {
            //Sort by itemPaths by shortest
            itemPaths.Sort((a, b) => a.Count - b.Count);


            foreach (var itemPath in itemPaths)
            {
                var itemPos = itemPath.Last();
                //if any allies already moving towards item skip
                var count = Board.GameManager.AllyTargetPositions.Count(allyMove => allyMove.SequenceEqual(itemPos));
                if (count > 0) continue;

                Board.GameManager.AllyTargetPositions.Add(itemPath.Last());
                return itemPath.Count < Range + 1 ? itemPath.Last() : itemPath[Range];
            }

            var penultimateIndex = pathToGoal.Count - 2;
            if (penultimateIndex < 0) penultimateIndex = 0;
            return pathToGoal.Count < Range + 1 ? pathToGoal[penultimateIndex] : pathToGoal[Range];
        }

        //Returns move that prioritizes the nearest enemy.
        private List<int> BestPrioritiseEnemies()
        {
            List<int> move = null;
            var closestObjective = 1000;

            foreach (var enemyPath in enemyPaths)
            {
                var count = enemyPath.Count;
                if (count >= closestObjective) continue;
                closestObjective = count;
                var target = enemyPath.Last();
                move = enemyPath.Count < Range + 1 ? target : enemyPath[Range];
            }

            return move;
        }


        //Returns move in direction of other allies target or nearest item/enemy if first ally to move this turn
        private List<int> BestGroupUpMove(bool prioritiseEnemies = false)
        {
            if (Board.GameManager.AllyTargetPositions.Count > 0)
            {
                var (bestMove, _) = FindMove(Board.GameManager.AllyTargetPositions[0]);
                return bestMove;
            }

            List<int> move = null;
            List<int> target = null;
            var closestObjective = 1000;

            if (!prioritiseEnemies)
                foreach (var itemPath in itemPaths)
                {
                    var count = itemPath.Count;
                    if (count >= closestObjective) continue;
                    closestObjective = count;
                    target = itemPath.Last();
                    move = itemPath.Count < Range + 1 ? target : itemPath[Range];
                }

            foreach (var enemyPath in enemyPaths)
            {
                var count = enemyPath.Count;
                if (count >= closestObjective) continue;
                closestObjective = count;
                target = enemyPath.Last();
                move = enemyPath.Count < Range + 1 ? target : enemyPath[Range];
            }

            if (move != null) Board.GameManager.AllyTargetPositions.Add(target);

            return move;
        }

        //Deal damage to the specified target
        public void Attack(Enemy unit)
        {
            HasAttacked = true;
            Console.WriteLine(Id + " attacking " + unit.Id + ", enemy hp is " + unit.Hp);
            if (unit.Engaged)
            {
                Console.WriteLine("After attack " + Id + " health is " + Hp + " and "
                                  + unit.Id + " health is " + unit.Hp);
                unit.Hp -= Dmg;
                CheckIfDead(unit);
            }
            else
            {
                unit.Hp -= Dmg;
                if (unit.Hp > 0)
                {
                    Hp -= unit.Dmg;
                    Console.WriteLine("After attack " + Id + " health is " + Hp + " and "
                                      + unit.Id + " health is " + unit.Hp);
                    unit.Engaged = true;
                    unit.CheckIfDead(this);
                }
                else
                {
                    CheckIfDead(unit);
                }
            }
        }

        public void CheckIfDead(Enemy enemy)
        {
            if (enemy == null || enemy.Hp > 0) return;
            Console.WriteLine("unit dead");
            Board.board[enemy.Pos[0]][enemy.Pos[1]] = 0;
            Board.GameManager.EnemyDead(enemy);
        }


        public void EndTurn()
        {
            if (IsDead)
            {
                Board.GameManager.RemoveAllyFromList(this);
            }
            else
            {
                Moves.Clear();
                ItemsInRange.Clear();
                EnemiesInRange.Clear();
                AlliesInRange.Clear();
                pathToGoal?.Clear();
                enemyPaths.Clear();
                itemPaths.Clear();
                GoalInRange = false;
            }
        }
    }
}