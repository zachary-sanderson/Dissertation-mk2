using System;
using System.Collections.Generic;
using System.Linq;

namespace Dissertation_mk2
{
    public class Enemy : Unit
    {

        public Enemy(Board board, float id, List<int> pos)
        {
            Board = board;
            Id = id;
            Pos = pos;
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
                Console.WriteLine("Can't Move.");

        }

        private void CheckMoves()
        {
            foreach (var enemy in Board.GameManager.Allies)
            {
                if (enemy.Hp <= 0) continue;
                var path = FindPath(Pos, enemy.Pos);
                if (path.Item1.Count < Range + 1 && path.Item2)
                {
                    AlliesInRange.Add(enemy.Pos);
                    Console.WriteLine("Path to Ally " + enemy.Id + ":");
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
            if (AlliesInRange != null)
            {
                Ally unit = FindTarget();
                if (unit != null)
                {
                    move = FindMove(unit.Pos);
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
            IEnumerable<Ally> enemies = CheckEnemiesInRange(Range * 2 + 1);

            foreach (var enemy in enemies.Where(enemy => enemy.Hp <= lowestHp))
            {
                lowestHp = enemy.Hp;
                target = enemy;
            }

            return target != null ? FindMove(target.Pos) : (null, false);
        }

        protected Ally FindTarget()
        {
            List<Ally> targets = new List<Ally>();

            foreach (var allyPos in AlliesInRange)
            {
                targets.AddRange(Board.GameManager.Allies.Where(ally => allyPos.SequenceEqual(ally.Pos)));
            }

            return LowestHp(targets);
        }

        private static Ally LowestHp(IEnumerable<Ally> targets)
        {
            var lowestHp = 5;
            bool engagedEnemy = false;
            Ally enemy = null;
            foreach (var unit in targets)
            {
                if (unit.Hp == 1)
                {
                    enemy = unit;
                    lowestHp = unit.Hp;
                }
                else if (unit.Engaged && lowestHp != 1)
                {
                    engagedEnemy = true;
                    enemy = unit;
                }
                else if (!engagedEnemy && unit.Hp <= lowestHp)
                {
                    lowestHp = unit.Hp;
                    enemy = unit;
                }
            }

            return enemy;
        }

        public void Attack(Ally unit)
        {
            HasAttacked = true;
            Console.WriteLine(Id + " attacking " + unit.Id + ", units hp is " + unit.Hp);
            if (unit.Engaged)
            {
                unit.Hp -= Dmg;
                Console.WriteLine("After attack " + Id + " health is " + Hp + " and "
                                  + unit.Id + " health is " + unit.Hp);
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
                    CheckIfDead(unit);
            }
        }

        private IEnumerable<Ally> CheckEnemiesInRange(int shortestDistance)
        {
            List<Ally> enemies = new List<Ally>();

            enemies.AddRange(from enemy in Board.GameManager.Allies
                let enemyDist = CheckDistance(Pos, enemy.Pos)
                where enemyDist <= shortestDistance
                select enemy);

            return enemies;
        }

        public void CheckIfDead(Ally ally)
        {
            if (ally == null || ally.Hp > 0) return;
            Board.board[ally.Pos[0]][ally.Pos[1]] = 0;
            Board.GameManager.AllyDead(ally);
            if (Board.GameManager.CheckIfGameOver())
            {
                Board.GameManager.GameOver();
            }
        }

        public void EndTurn()
        {
            if (IsDead)
            {
                Board.GameManager.RemoveEnemyFromList(this);
            }
            else
            {
                Moves.Clear();
                ItemsInRange.Clear();
                EnemiesInRange.Clear();
                AlliesInRange.Clear();
                GoalInRange = false;
            }
        }
    }
}
