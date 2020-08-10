using System;
using System.Collections.Generic;
using System.Linq;

namespace Dissertation_mk2
{
    public abstract class Unit
    {
        protected readonly int[][] Directions = {new[] {1, 0}, new[] {0, -1}, new[] {0, 1}, new[] {-1, 0}};
        public List<List<int>> AlliesInRange = new List<List<int>>();

        public Board Board;
        public int Dmg = 1;
        public List<List<int>> EnemiesInRange = new List<List<int>>();
        public bool Engaged;
        public bool GoalInRange;
        public bool HasAttacked = false;
        public int Hp = 5;
        public float Id;
        public int InitialHp;
        public bool IsDead = false;
        public bool ItemPickup = false;
        public List<List<int>> ItemsInRange = new List<List<int>>();

        public List<List<int>> Moves = new List<List<int>>();
        public List<int> Pos;
        public int Range = 5;

        protected bool CanMove()
        {
            var canMove = false;
            foreach (var direction in Directions)
            {
                var move = Pos.Select((t, i) => t + direction[i]).ToList();
                if (Board.OutOfRange(move)) continue;
                var tile = Board.CheckPosition(move);
                if (tile == 0 || tile == 2)
                    canMove = true;
            }

            return canMove;
        }

        //Returns the best move towards targetPos
        protected (List<int>, bool) FindMove(List<int> targetPos)
        {
            List<int> move = null;
            Console.WriteLine(targetPos[0] + " " + targetPos[1]);
            var (path, targetFound) = FindPath(Pos, targetPos);
            if (path.Count > Range)
                move = path[Range];
            else if (path.Count >= 2)
                move = path[^1];

            return (move, targetFound);
        }

        //A* algorithm designed to work for a matrix
        public (List<List<int>>, bool) FindPath(List<int> startPos, List<int> targetPos)
        {
            var currentNode = new Node(startPos, 0, CheckDistance(startPos, targetPos), null, true);
            var closedList = new List<Node>();
            var openList = new List<Node> {currentNode};
            List<int> currentPos;
            var targetFound = false;

            do
            {
                currentPos = currentNode.Pos;
                openList.Remove(currentNode);
                closedList.Add(currentNode);

                foreach (var direction in Directions)
                {
                    if (targetFound) continue;

                    var move = currentPos.Select((t, i) => t + direction[i]).ToList();

                    if (Board.OutOfRange(move)) continue;

                    var node = new Node(move, currentNode.G + 1, CheckDistance(move, targetPos), currentNode);

                    if (move.SequenceEqual(targetPos))
                    {
                        targetFound = true;
                        if (Board.CheckPosition(move) == 0) closedList.Add(node);
                        continue;
                    }

                    if (node.IsInClosedList(closedList)) continue;

                    if (node.IsInOpenList(openList)) continue;

                    var tile = Board.CheckPosition(move);

                    if (tile == 0 || tile == 2)
                        openList.Add(node);
                }

                if (!targetFound && openList.Count > 0) currentNode = ChooseNextNode(openList, targetPos);
            } while (openList.Count > 0 && !targetFound && currentNode != null);

            if (!targetFound)
            {
                var dist = 1000;
                foreach (var node in closedList)
                    if (node.H < dist)
                        currentNode = node;
            }

            var path = new List<List<int>> {currentPos};

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
            foreach (var node in openList.Where(node => node.Pos.SequenceEqual(targetPos))) return node;
            Node nextNode = null;
            var f = 160;
            foreach (var node in openList)
                if (node.F <= f)
                {
                    nextNode = node;
                    f = node.F;
                }
                else if (node.F > 160)
                {
                    Console.WriteLine(node.F + " " + node.Pos[0] + " " + node.Pos[1]);
                }

            if (nextNode == null)
                Console.WriteLine("open list not empty, f is " + f);
            return nextNode;
        }

        protected static int CheckDistance(List<int> startPos, List<int> endPos)
        {
            var xDiff = Math.Abs(endPos[0] - startPos[0]);
            var yDiff = Math.Abs(endPos[1] - startPos[1]);
            return xDiff + yDiff;
        }

        protected void SwapPosition(List<int> move)
        {
            if (CheckDistance(Pos, move) < Range + 1)
            {
                Console.WriteLine("Moving from: " + Pos[0] + " " + Pos[1] + " to:" + move[0] + " " + move[1]);
                Board.UpdateTile(Pos, 0);
                var newTile = (int) Id;
                Board.UpdateTile(move, newTile);
                Pos[0] = move[0];
                Pos[1] = move[1];
            }
        }

        public void UpdateHp()
        {
            InitialHp = Hp;
        }
    }
}