using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Transactions;

namespace Dissertation_mk2
{
    public class Markov
    {
        readonly Random rand = new Random();
        public bool Aggressive;
        public bool Explorer;
        public bool Speedy;
        public string Personality;
        public double pValue;

        private double[][] matrix;

        public Markov(double p)
        {
            Console.WriteLine(p);
            double third = 1 / (double) 3;
            double twoThirds = 2 / (double) 3;
            double oneSixth = third / 2;
            double fiveSixths = twoThirds + oneSixth;
            if (p < 1/(double)3)
            {
                double prob = ((1 / (double)6) - p);
                double[] line1 = {fiveSixths + prob,  1-(p/2), 1};
                double[] line2 = {third + prob, 1 - p, 1};
                double[] line3 = {third - p, 0.5 + prob, 1};
                matrix = new[] { line1, line2, line3 };
                Speedy = true;
                Personality = "Speedy";
                Console.WriteLine("Speedy");
            }
            else if (p < 2/(double)3)
            {
                double prob = 0.5 - p;
                double[] line1 = { 0.5 + prob, fiveSixths + (prob/3), 1};
                double[] line2 = {third - (p/3), 1 - (p/3), 1 };
                double[] line3 = {oneSixth + (prob/3), 0.5 + prob, 1};
                matrix = new[] { line1, line2, line3 };
                Aggressive = true;
                Personality = "Aggressive";
                Console.WriteLine("Aggressive");
            }
            else
            {
                double prob = ((5 / (double) 6) - p);
                double[] line1 = { third + (1-p), twoThirds + prob, 1 };
                double[] line2 = {1 - p, twoThirds + prob, 1 };
                double[] line3 = {(1 - p)/2, oneSixth + prob, 1 };
                matrix = new[] {line1, line2, line3};
                Explorer = true;
                Personality = "Explorer";
                Console.WriteLine("Explorer");
            }
            PrintMatrix();
        }

        public void Transition()
        {
            double transition = rand.NextDouble();
            if (Speedy)
            {
                for (int i = 0; i < matrix.Length; i++)
                {
                    if (!(transition < matrix[0][i])) continue;
                    WhichBehaviour(i);
                    return;
                }
            }
            else if (Aggressive)
            {
                for (int i = 0; i < matrix.Length; i++)
                {
                    if (!(transition < matrix[1][i])) continue;
                    WhichBehaviour(i);
                    return;
                }
            }
            else
            {
                for (int i = 0; i < matrix.Length; i++)
                {
                    if (!(transition < matrix[2][i])) continue;
                    WhichBehaviour(i);
                    return;
                }
            }
        }

        public void WhichBehaviour(int i)
        {
            Console.WriteLine("i is " + i);
            switch (i)
            {
                case 0:
                    Speedy = true;
                    Aggressive = false;
                    Explorer = false;
                    Console.WriteLine("Speedy");
                    break;
                case 1:
                    Speedy = false;
                    Aggressive = true;
                    Explorer = false;
                    Console.WriteLine("Aggressive");
                    break;
                default:
                    Speedy = false;
                    Aggressive = false;
                    Explorer = true;
                    Console.WriteLine("Explorer");
                    break;
            }
        }

        public void PrintMatrix()
        {
            Console.WriteLine(Builder(matrix));
        }

        private static string Builder(double[][] matrix)
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
