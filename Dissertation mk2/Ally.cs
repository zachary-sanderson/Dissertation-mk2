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



        public void TakeTurn(GA ga)
        {
            this.ga = ga;
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
            board.gameManager.AddMove(new Move(board.gameManager.TurnCount, Dissertation_mk2.Move.UnitType.Ally, startPos, endPos, hasAttacked));
            hasAttacked = false;
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
            var (path, reachable) = FindPath(pos, board.goalPos);
            pathToGoal = path;
            if (pathToGoal.Count < 6 && reachable)
            {
                Console.WriteLine("Path to goal:");
                foreach (var node in pathToGoal)
                {
                    Console.WriteLine(node[0] + " " + node[1]);
                }
                goalInRange = true;
            }
        }

        private void CheckForEnemies()
        {
            foreach (var enemy in board.gameManager.enemies.Where(enemy => enemy.hp > 0))
            {
                var (path, reachable) = FindPath(pos, enemy.pos);
                enemyPaths.Add(path);

                if (path.Count < range + 1 && reachable)
                {
                    Console.WriteLine("Path to Enemy " + enemy.id + ":");
                    foreach (var node in path)
                    {
                        Console.WriteLine(node[0] + " " + node[1]);
                    }
                    enemiesInRange.Add(enemy.pos);
                }
            }
        }

        private void CheckForItems()
        {
            foreach (var item in board.itemPositions)
            {
                var (path, reachable) = FindPath(pos, item);
                itemPaths.Add(path);
                if (path.Count < 6 && reachable)
                {
                    Console.WriteLine("item in range: " + item[0] + " " + item[1]);
                    itemsInRange.Add(item);
                }
            }
        }







        private void Move()
        {
            bool nearGameOver = NearGameOver();
            Console.WriteLine("enemies in range:" + enemiesInRange.Count);
            Console.WriteLine("item in range:" + itemsInRange.Count);
            Console.WriteLine("score:" + board.score + " numitems: " + board.numItems + " EnemiesKilled:" + board.gameManager.EnemiesKilled + " numEnemies:" + board.numEnemies);
            //If all items collected and enemies dead move towards goal.
            if (board.score == board.numItems && board.gameManager.EnemiesKilled == board.numEnemies)
            {
                if (goalInRange)
                {
                    SwapPosition(board.goalPos);
                    board.gameManager.LevelComplete();
                }
                else
                {
                    
                    List<int> move = pathToGoal.Count < range + 1 ? pathToGoal.Last() : pathToGoal[range];
                    SwapPosition(move);
                }
            }
            //If close to game over then abandon remaining enemies/items and head to goal
            else if (nearGameOver)
            {
                List<int> move = pathToGoal.Count < range + 1 ? pathToGoal.Last() : pathToGoal[range];
                SwapPosition(move);
            }
            else if (enemiesInRange.Count > 0)
            {
                FindEnemyToAttack();
            }
            else if (itemsInRange.Count > 0)
            {
                MoveToItem();
            }
            else
            {
                List<int> move = BestMove();
                if (move != null)
                {
                    SwapPosition(move);
                }
            }
        }

        private bool NearGameOver()
        {
            int allyCombinedHp = board.gameManager.allies.Sum(ally => ally.hp);
            int enemyCombinedHp = board.gameManager.enemies.Sum(enemy => enemy.hp);

            return allyCombinedHp < 5 && enemyCombinedHp > 5;
        }

        //Finds and attacks enemy unit in range with the lowest hp. If null calls BestMove() and moves to returned pos instead.
        private void FindEnemyToAttack()
        {
            Enemy unit = FindTarget();
            if (unit != null)
            {
                var (move, targetFound) = FindMove(unit.pos);
                if (move != null && targetFound)
                {
                    SwapPosition(move);
                }

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
            List<Enemy> targets = new List<Enemy>();

            foreach (var enemyPos in enemiesInRange)
            {
                foreach (var enemy in board.gameManager.enemies)
                {
                    if(enemyPos.SequenceEqual(enemy.pos)) targets.Add(enemy);
                }
            }

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

        private void MoveToItem()
        {
            var move = itemsInRange[0];
            itemsInRange.Remove(move);
            board.itemPositions.Remove(move);
            SwapPosition(move);
            board.score += board.itemValue;
        }






        /*Moves towards enemy with lowest Hp that's within 2 moves away*/
        /*If there are no enemies in range, moves towards the goal*/
        private List<int> BestMove()
        {
            List<int> move = null;

            if (board.markov.Spread)
            {
                move = BestSpreadMove();
            }

            else if (board.markov.PrioritiseEnemies)
            {
                var enemyAlive = false;
                foreach (var _ in board.gameManager.enemies.Where(enemy => enemy.hp > 0))
                {
                    enemyAlive = true;
                }
                move = enemyAlive ? BestGroupUpMove(true) : SpreadForItems();
            }

            else
            {
                move = BestGroupUpMove();
            }

            

            if (move != null)
                return move;
            return pathToGoal.Count < range + 1 ? pathToGoal.Last() : pathToGoal[range];
        }

        //Return move closest to the nearest item that DOES NOT have too many allies moving towards it this turn already
        private List<int> BestSpreadMove()
        {
            //Sort by itemPaths by shortest
            itemPaths.Sort((a, b) => a.Count - b.Count);

            if (board.gameManager.enemies.Count == 0 && itemPaths.Count > 0) return itemPaths[0].Count < range + 1 ? itemPaths[0].Last() : itemPaths[0][range];

            foreach (var itemPath in itemPaths)
            {
                //get the number of enemies within 10 spaces of item
                var count = board.gameManager.enemies.Where(enemy => enemy.hp > 0).Count(enemy => CheckDistance(itemPath.Last(), enemy.pos) < range * 2 + 1);

                if (TooManyAlliesAlready(itemPath.Last(), count)) continue;

                board.gameManager.allyTargetPositions.Add(itemPath.Last());
                return itemPath.Count < range + 1 ? itemPath.Last() : itemPath[range];
            }

            if (enemyPaths.Count == 0) return null;
            enemyPaths.Sort((a, b) => a.Count - b.Count);
            return enemyPaths[0].Count < range + 1 ? enemyPaths[0].Last() : enemyPaths[0][range];
        }

        //Given an item position and the number of enemies near it,
        //the function returns false if too many allies are already moving towards the item position
        private bool TooManyAlliesAlready(IReadOnlyCollection<int> move, int numEnemies)
        {
            int count = board.gameManager.allyTargetPositions.Count(allyMove => allyMove.SequenceEqual(move));
            return count <= numEnemies;
        }

        //If PrioritiseEnemies strategy and all enemies dead spreads allies out to get remaining items
        private List<int> SpreadForItems()
        {
            //Sort by itemPaths by shortest
            itemPaths.Sort((a, b) => a.Count - b.Count);


            foreach (var itemPath in itemPaths)
            {
                var itemPos = itemPath.Last();
                //if any allies already moving towards item skip
                int count = board.gameManager.allyTargetPositions.Count(allyMove => allyMove.SequenceEqual(itemPos));
                if (count > 0) continue;

                board.gameManager.allyTargetPositions.Add(itemPath.Last());
                return itemPath.Count < range + 1 ? itemPath.Last() : itemPath[range];
            }

            var penultimateIndex = pathToGoal.Count - 2;
            if (penultimateIndex < 0) penultimateIndex = 0;
            return pathToGoal.Count < range + 1 ? pathToGoal[penultimateIndex] : pathToGoal[range];
        }

        //Returns move in direction of other allies target or nearest item/enemy if first ally to move this turn
        private List<int> BestGroupUpMove(bool prioritiseEnemies = false)
        {
            if (board.gameManager.allyTargetPositions.Count > 0)
            {
                var (bestMove, _) = FindMove(board.gameManager.allyTargetPositions[0]);
                return bestMove;
            }

            List<int> move = null;
            List<int> target = null;
            int closestObjective = 1000;

            if (!prioritiseEnemies)
            {
                foreach (var itemPath in itemPaths)
                {
                    var count = itemPath.Count;
                    if (count >= closestObjective) continue;
                    closestObjective = count;
                    target = itemPath.Last();
                    move = itemPath.Count < range + 1 ? target : itemPath[range];
                }
            }

            foreach (var enemyPath in enemyPaths)
            {
                var count = enemyPath.Count;
                if (count >= closestObjective) continue;
                closestObjective = count;
                target = enemyPath.Last();
                move = enemyPath.Count < range + 1 ? target : enemyPath[range];
            }

            if (move != null) board.gameManager.allyTargetPositions.Add(target);

            return move;
        }

        

        



        //*****************************************************************************************************************
        //GIVEN ENEMY POS FIND OPTIMAL POSITION NEXT TO THEM TO ATTACK FROM. POSITION MINIMIZES NUMBER OF ENEMIES THAT CAN ATTACK NEXT TURN
        //*****************************************************************************************************************
        /*
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
                    var (path, targetFound) = FindPath(enemy.pos, position);
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
        */





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
                if (item1.Count < range + 1 && item2)
                {
                    numEnemies += 1;
                }
            }

            return numEnemies;
        }
    }
}