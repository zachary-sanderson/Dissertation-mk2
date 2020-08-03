using System;
using System.Collections.Generic;
using System.Text;

namespace Dissertation_mk2
{
    public class Markov
    {
        private readonly Random rand = new Random();
        public bool Spread;
        public bool PrioritiseEnemies;
        public bool GroupUp;
        public string Personality;

        private double[][] matrix;

        private const double Third = 1 / (double) 3;
        private const double TwoThirds = 2 / (double) 3;
        private const double OneSixth = Third / 2;
        private const double FiveSixths = TwoThirds + OneSixth;

        public Markov(double p)
        {
            Console.WriteLine(p);
            
            if (p < 1/(double)3)
            {
                SetSpread(p);
            }
            else if (p < 2/(double)3)
            {
                SetPrioritiseEnemies(p);
            }
            else
            {
                SetGroupUp(p);
            }
            PrintMatrix();
        }

        private void SetPrioritiseEnemies(double p)
        {

            double prob = 0.5 - p;
            double[] line1 = { 0.5 + prob, FiveSixths + (prob / 3), 1 };
            double[] line2 = { Third - (p / 3), 1 - (p / 3), 1 };
            double[] line3 = { OneSixth + (prob / 3), 0.5 + prob, 1 };
            matrix = new[] { line1, line2, line3 };
            PrioritiseEnemies = true;
            Personality = "PrioritiseEnemies";
            Console.WriteLine("PrioritiseEnemies");
        }

        private void SetSpread(double p)
        {
            double prob = ((1 / (double)6) - p);
            double[] line1 = { FiveSixths + prob, 1 - (p / 2), 1 };
            double[] line2 = { Third + prob, 1 - p, 1 };
            double[] line3 = { Third - p, 0.5 + prob, 1 };
            matrix = new[] { line1, line2, line3 };
            Spread = true;
            Personality = "Spread";
            Console.WriteLine("Spread");
        }

        private void SetGroupUp(double p)
        {
            double prob = ((5 / (double)6) - p);
            double[] line1 = { Third + (1 - p), TwoThirds + prob, 1 };
            double[] line2 = { 1 - p, TwoThirds + prob, 1 };
            double[] line3 = { (1 - p) / 2, OneSixth + prob, 1 };
            matrix = new[] { line1, line2, line3 };
            GroupUp = true;
            Personality = "GroupUp";
            Console.WriteLine("GroupUp");
        }

        public void Transition()
        {
            double transition = rand.NextDouble();
            if (PrioritiseEnemies)
            {
                CheckTransition(transition, 0);
            }
            else if (Spread)
            {
                CheckTransition(transition, 1);
            }
            else
            {
                CheckTransition(transition, 2);
            }
        }

        private void CheckTransition(double transition, int row)
        {
            for (int i = 0; i < matrix.Length; i++)
            {
                if (!(transition < matrix[row][i])) continue;
                WhichBehaviour(i);
                return;
            }
        }

        private void WhichBehaviour(int i)
        {
            Console.WriteLine("i is " + i);
            switch (i)
            {
                case 0:
                    Spread = true;
                    PrioritiseEnemies = false;
                    GroupUp = false;
                    Console.WriteLine("PrioritiseEnemies");
                    break;
                case 1:
                    Spread = false;
                    PrioritiseEnemies = true;
                    GroupUp = false;
                    Console.WriteLine("Spread");
                    break;
                default:
                    Spread = false;
                    PrioritiseEnemies = false;
                    GroupUp = true;
                    Console.WriteLine("GroupUp");
                    break;
            }
        }

        private void PrintMatrix()
        {
            Console.WriteLine(Builder(matrix));
        }

        private static string Builder(IEnumerable<double[]> matrix)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var line in matrix)
            {
                foreach(var value in line)
                    builder.Append(value + " ");
                builder.Append("\n");
            }
            return builder.ToString();
        }
    }
}
