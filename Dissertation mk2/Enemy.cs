using System;
using System.Collections.Generic;
using System.Linq;

namespace Dissertation_mk2
{
    public class Enemy : Unit
    {
        private List<List<List<int>>> enemyPaths = new List<List<List<int>>>();

        public Enemy(Board board, float id, List<int> pos)
        {
            this.board = board;
            this.id = id;
            this.pos = pos;
        }

        public void TakeTurn()
        {
            List<int> startPos = new List<int> {pos[0], pos[1]};
            Console.WriteLine(id);
            if (CanMove())
            {
                CheckMoves();
                Move();
            }
            else
                Console.WriteLine("Can't Move.");

            List<int> endPos = new List<int> { pos[0], pos[1] };
            board.gameManager.AddMove(new Move(board.gameManager.turnCount, Dissertation_mk2.Move.UnitType.Enemy, startPos, endPos, hasAttacked));
            hasAttacked = false;
        }

        private void CheckMoves()
        {
            foreach (var enemy in board.gameManager.allies)
            {
                if (enemy.hp <= 0) continue;
                var path = FindPath(pos, enemy.pos);
                enemyPaths.Add(path.Item1);
                if (path.Item1.Count < range + 1 && path.Item2)
                {
                    alliesInRange.Add(enemy.pos);
                    Console.WriteLine("Path to Ally " + enemy.id + ":");
                    foreach (var node in path.Item1)
                    {
                        Console.WriteLine(node[0] + " " + node[1]);
                    }
                }
            }
        }

        private void Move()
        {
            (List<int>, bool) move;
            if (alliesInRange != null)
            {
                Ally unit = FindTarget();
                if (unit != null)
                {
                    move = FindMove(unit.pos);
                    if (move.Item1 != null && move.Item2)
                        SwapPosition(move.Item1);
                    Attack(unit);
                }
                else
                {
                    move = BestMove();
                    if (move.Item1 != null)
                        SwapPosition(move.Item1);
                }
            }
            else
            {
                move = BestMove();
                if (move.Item1 != null)
                    SwapPosition(move.Item1);
            }
        }

        private (List<int>, bool) BestMove()
        {
            int lowestHp = 5;
            Ally target = null;
            IEnumerable<Ally> enemies = CheckEnemiesInRange(range * 2 + 1);

            foreach (var enemy in enemies.Where(enemy => enemy.hp <= lowestHp))
            {
                lowestHp = enemy.hp;
                target = enemy;
            }

            return target != null ? FindMove(target.pos) : (null, false);
        }

        protected Ally FindTarget()
        {
            List<Ally> targets = new List<Ally>();

            List<float> enemyIds = alliesInRange.Select(position => board.CheckPosition(position)).ToList();

            targets.AddRange(from id in enemyIds
                from unit in board.gameManager.allies
                where id == unit.id
                select unit);

            return LowestHp(targets);
        }

        private static Ally LowestHp(IEnumerable<Ally> targets)
        {
            var lowestHp = 5;
            bool engagedEnemy = false;
            Ally enemy = null;
            foreach (var unit in targets)
            {
                if (unit.hp == 1)
                {
                    enemy = unit;
                    lowestHp = unit.hp;
                }
                else if (unit.engaged && lowestHp != 1)
                {
                    engagedEnemy = true;
                    enemy = unit;
                }
                else if (!engagedEnemy && unit.hp <= lowestHp)
                {
                    lowestHp = unit.hp;
                    enemy = unit;
                }
            }

            return enemy;
        }

        public void Attack(Ally unit)
        {
            hasAttacked = true;
            Console.WriteLine(id + " attacking " + unit.id + ", units hp is " + unit.hp);
            if (unit.engaged)
            {
                unit.hp -= dmg;
                Console.WriteLine("After attack " + id + " health is " + hp + " and "
                                  + unit.id + " health is " + unit.hp);
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

        private IEnumerable<Ally> CheckEnemiesInRange(int shortestDistance)
        {
            List<Ally> enemies = new List<Ally>();

            enemies.AddRange(from enemy in board.gameManager.allies
                let enemyDist = CheckDistance(pos, enemy.pos)
                where enemyDist <= shortestDistance
                select enemy);

            return enemies;
        }

        public void CheckIfDead(Ally ally)
        {
            if (ally == null || ally.hp > 0) return;
            board.board[ally.pos[0]][ally.pos[1]] = 0;
            board.gameManager.AllyDead(ally);
            if (board.gameManager.CheckIfGameOver())
            {
                board.gameManager.GameOver();
            }
        }

        public void EndTurn()
        {
            if (isDead)
            {
                board.gameManager.RemoveEnemyFromList(this);
            }
            else
            {
                moves.Clear();
                itemsInRange.Clear();
                enemiesInRange.Clear();
                alliesInRange.Clear();
                goalInRange = false;
            }
        }
    }
}
