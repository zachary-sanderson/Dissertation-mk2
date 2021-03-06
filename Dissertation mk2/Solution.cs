﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Linq;

namespace Dissertation_mk2
{
    public class Solution
    {
        public string Personality;
        public List<List<float>> initialBoard = new List<List<float>>();
        public Board boardObj;
        public int numItems;
        public int numEnemies;
        public List<List<int>> itemPositions;
        public List<List<int>> enemyPositions;

        public int score;
        public double averageFlow;
        public int numTurns;
        public int enemKilled;
        private List<int> anxietyEachTurn;
        public int averageAnxiety;
        public double pValue;
        public List<int> allyHp;
        public bool wasGameOver;

        //Personality evaluation
        

        public Solution(Board board, string personality, double pValue, int numItems, int numEnemies)
        {
            SaveBoard(board.board);
            boardObj = board;
            Personality = personality;
            this.pValue = pValue;
            this.numItems = numItems;
            this.numEnemies = numEnemies;
            itemPositions = board.itemPositions;
            enemyPositions = board.enemyPositions;
        }

        private void SaveBoard(List<List<float>> initial)
        {
            foreach (var initialRow in initial.Select(row => row.ToList()))
            {
                initialBoard.Add(initialRow);
            }
        }

        public void UpdateValues(int score, int numTurns, int enemKilled, List<int> anxietyEachTurn, List<int> allyHp, bool wasGameOver = false)
        {
            this.score = score;
            this.numTurns = numTurns;
            this.enemKilled = enemKilled;
            this.anxietyEachTurn = anxietyEachTurn;
            this.allyHp = allyHp;
            this.wasGameOver = wasGameOver;
            CalculateValues();
        }

        private void CalculateValues()
        {
            averageAnxiety = anxietyEachTurn.Sum();
            averageFlow = (double) anxietyEachTurn.Sum(Math.Abs) / anxietyEachTurn.Count;
            Console.WriteLine("Anxieties: " + IntBuilder(anxietyEachTurn));
            Console.WriteLine("Average anxiety: " + averageAnxiety);
            Console.WriteLine("Average Distance from flow: " + averageFlow);
            Console.WriteLine("Personality: " + Personality);
            Console.WriteLine("Hp: " + IntBuilder(allyHp));
            Console.WriteLine("Num turns: " + numTurns);
            Console.WriteLine("Num items: " + score + "/" + numItems);
            Console.WriteLine("Num enemies killed: " + enemKilled + "/5");
        }


        public int CompareMap(Solution other)
        {
            int count = 0;
            for (int i = 0; i < initialBoard.Count; i++)
            {
                for (int j = 0; i < initialBoard.Count; i++)
                {
                    if ((int)initialBoard[i][j] == (int)other.initialBoard[i][j]) count++;
                }
            }
            return count;
        }

        //For two solutions compares the shortest distance each enemy has from another enemy in the initial map.
        public List<int> CompareEnemies(Solution other)
        {
            return enemyPositions.Select(position => other.enemyPositions.Select(otherPosition => CheckDistance(position, otherPosition)).Concat(new[] {100}).Min()).ToList();
        }

        //For two solutions compares the shortest distance each item has from another item in the initial map.
        public List<int> CompareItems(Solution other)
        {
            return itemPositions.Select(position => other.itemPositions.Select(otherPosition => CheckDistance(position, otherPosition)).Concat(new[] {100}).Min()).ToList();
        }

        private static int CheckDistance(List<int> startPos, List<int> endPos)
        {
            int xDiff = Math.Abs(endPos[0] - startPos[0]);
            int yDiff = Math.Abs(endPos[1] - startPos[1]);
            return xDiff + yDiff;
        }

        private static string IntBuilder(IEnumerable<int> ints)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var value in ints)
            {
                builder.Append(value + " ");
            }
            return builder.ToString();
        }
    }
}
