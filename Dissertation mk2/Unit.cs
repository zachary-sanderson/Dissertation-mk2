using System;
using System.Collections.Generic;
using System.Linq;

namespace Dissertation_mk2
{
    public abstract class Unit
    {
        public int hp = 5;
        public int dmg = 1;
        public int range = 5;
        public bool engaged;

        public Board board;
        public float id;
        public List<int> pos;
        public bool hasAttacked = false;
        protected readonly int[][] directions = { new[] { 1, 0 }, new[] { 0, -1 }, new[] { 0, 1 }, new[] { -1, 0 } };

        public List<List<int>> moves = new List<List<int>>();
        public List<List<int>> itemsInRange = new List<List<int>>();
        public List<List<int>> enemiesInRange = new List<List<int>>();
        public List<List<int>> alliesInRange = new List<List<int>>();
        public bool goalInRange;
        public bool isDead = false;

        protected bool CanMove()
        {
            bool canMove = false;
            foreach (int[] direction in directions)
            {
                List<int> move = pos.Select((t, i) => t + direction[i]).ToList();
                if (board.OutOfRange(move)) continue;
                var tile = board.CheckPosition(move);
                if ((int)tile == 0 || (int)tile == 2)
                    canMove = true;
            }

            return canMove;
        }

        protected (List<int>, bool) FindMove(List<int> targetPos)
        {
            List<int> move = null;
            Console.WriteLine(targetPos[0] + " " + targetPos[1]);
            var (path, targetFound) = FindPath(pos, targetPos);
            if (path.Count > range)
                move = path[range];
            else if (path.Count >= 2)
                move = path[^1];

            return (move, targetFound);
        }

        //A* algorithm designed to work for a matrix
        public (List<List<int>>, bool) FindPath(List<int> startPos, List<int> targetPos)
        {
            /*var (target, reachable) = FindReachable(targetPos);
            if (reachable == false)
                Console.WriteLine(targetPos[0] + " " + targetPos[1] + " is unreachable");
            targetPos = target ?? startPos;
            */
            Node currentNode = new Node(startPos, 0, CheckDistance(startPos, targetPos), null, true);
            List<Node> closedList = new List<Node>();
            List<Node> openList = new List<Node> { currentNode };
            List<int> currentPos;
            bool targetFound = false;

            do
            {
                currentPos = currentNode.Pos;
                openList.Remove(currentNode);
                closedList.Add(currentNode);

                foreach (int[] direction in directions)
                {
                    if (targetFound) continue;

                    List<int> move = currentPos.Select((t, i) => t + direction[i]).ToList();

                    if (board.OutOfRange(move)) continue;

                    Node node = new Node(move, currentNode.G + 1, CheckDistance(move, targetPos), currentNode);

                    if (move.SequenceEqual(targetPos))
                    {
                        targetFound = true;
                        if (board.CheckPosition(move) == 0)
                        {
                            closedList.Add(node);
                        }
                        continue;
                    }

                    if (node.IsInClosedList(closedList)) continue;

                    if (node.IsInOpenList(openList)) continue;

                    var tile = board.CheckPosition(move);

                    if (tile == 0 || tile == 2)
                        openList.Add(node);
                }

                if (!targetFound && openList.Count > 0)
                {
                    currentNode = ChooseNextNode(openList, targetPos);
                }

            } while (openList.Count > 0 && !targetFound && currentNode != null);

            List<List<int>> path = new List<List<int>> { currentPos };
            while (currentNode?.Parent != null)
            {
                path.Add(currentNode.Parent.Pos);
                currentNode = currentNode.Parent;
            }

            if (targetFound == false)
                Console.WriteLine(targetPos[0] + " " + targetPos[1] + " is unreachable");

            path.Reverse();
            return (path, targetFound);
        }

        private static Node ChooseNextNode(IReadOnlyCollection<Node> openList, IReadOnlyCollection<int> targetPos)
        {
            foreach (var node in openList.Where(node => node.Pos.SequenceEqual(targetPos)))
            {
                return node;
            }
            Node nextNode = null;
            int f = 160;
            foreach (var node in openList)
            {
                if (node.F <= f)
                {
                    nextNode = node;
                    f = node.F;
                }
                else if (node.F > 160)
                    Console.WriteLine(node.F + " " + node.Pos[0] + " " + node.Pos[1]);
            }

            if (nextNode == null)
                Console.WriteLine("open list not empty, f is " + f);
            return nextNode;
        }

        protected static int CheckDistance(List<int> startPos, List<int> endPos)
        {
            int xDiff = Math.Abs(endPos[0] - startPos[0]);
            int yDiff = Math.Abs(endPos[1] - startPos[1]);
            return xDiff + yDiff;
        }

        protected void SwapPosition(List<int> move)
        {
            if (CheckDistance(pos, move) < range + 1)
            {
                Console.WriteLine("Moving from: " + pos[0] + " " + pos[1] + " to:" + move[0] + " " + move[1]);
                board.UpdateTile(pos, 0);
                int newTile = (int) id;
                board.UpdateTile(move, newTile);
                pos[0] = move[0];
                pos[1] = move[1];
            }
        }

        /*If intended target for A* is unreachable uses another
         reachable node close to the target instead
        private (List<int>, bool) FindReachable(List<int> targetPos)
        {
            bool reachable = false;
            List<List<int>> adjacentMoves  = new List<List<int>>();
            List<int> move = null;
            List<int> currentMove;
            foreach (int[] direction in directions)
            {
                currentMove = targetPos.Select((t, i) => t + direction[i]).ToList();
                if (board.rows - 1 < currentMove[0] || currentMove[0] < 0 || 
                    board.columns - 1 < currentMove[1] || currentMove[1] < 0) continue;
                adjacentMoves.Add(currentMove);
                if ((int) Math.Floor(board.CheckPosition(currentMove)) == 0 || board.CheckPosition(currentMove) == id)
                    reachable = true;
            }

            if (reachable) return (targetPos, true);
            {
                int shortestDistance = 100;
                foreach (var adjacentMove in adjacentMoves)
                {
                    foreach (var direction in directions)
                    {
                        currentMove = adjacentMove.Select((t, i) => t + direction[i]).ToList();
                        if (board.rows - 1 < currentMove[0] || currentMove[0] < 0 ||
                            board.columns - 1 < currentMove[1] || currentMove[1] < 0) continue;
                        var currentDistance = CheckDistance(pos, currentMove);

                        if ((int) Math.Floor(board.CheckPosition(currentMove)) == 0 &&
                            currentDistance < shortestDistance)
                        {
                            shortestDistance = currentDistance;
                            move = adjacentMove;
                        }
                    }
                }
            }

            return (move, false);
        }
        */
    }
}