using System;
using System.Collections.Generic;
using System.Text;

namespace Dissertation_mk2
{
    public class Move
    {
        public enum UnitType
        {
            Ally,
            Enemy
        }
        public int TurnNum { get; }
        public UnitType Type { get; }
        public List<int> StartPos { get; }
        public List<int> EndPos { get; }
        public bool Attack { get; }

        public Move(int turnNum, UnitType type, List<int> startPos, List<int> endPos, bool attack)
        {
            TurnNum = turnNum;
            Type = type;
            StartPos = startPos;
            EndPos = endPos;
            Attack = attack;
        }
    }
}
