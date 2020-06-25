using System.Collections.Generic;
using System.Linq;

namespace Dissertation_mk2
{
    public class Node
    {
        public List<int> Pos;
        public Node Parent;
        public int F;
        public int G;
        public int H;
        public bool StartNode;

        public Node(List<int> pos, int g, int h, Node parent, bool startNode = false)
        {
            Pos = pos;
            F = g + h;
            G = g;
            H = h;
            Parent = parent;
            StartNode = startNode;
        }

        public bool IsInOpenList(IEnumerable<Node> openList)
        {
            foreach (var node in openList.Where(node => Pos.SequenceEqual(node.Pos)))
            {
                if (F < node.F)
                {
                    node.F = F;
                    node.Parent = Parent;
                }

                return true;
            }

            return false;
        }

        public bool IsInClosedList(IEnumerable<Node> closedList)
        {
            return closedList.Any(node => Pos.SequenceEqual(node.Pos));
        }
    }
}