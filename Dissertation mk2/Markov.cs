using System;
using System.Text;

namespace Dissertation_mk2
{
    public class Markov
    {
        private readonly Random rand = new Random();
        public bool Aggressive;
        public bool Explorer;
        public bool Speedy;
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
                SetSpeedy(p);
            }
            else if (p < 2/(double)3)
            {
                SetAggressive(p);
            }
            else
            {
                SetExplorer(p);
            }
            PrintMatrix();
        }

        private void SetSpeedy(double p)
        {
            double prob = ((1 / (double)6) - p);
            double[] line1 = { FiveSixths + prob, 1 - (p / 2), 1 };
            double[] line2 = { Third + prob, 1 - p, 1 };
            double[] line3 = { Third - p, 0.5 + prob, 1 };
            matrix = new[] { line1, line2, line3 };
            Speedy = true;
            Personality = "Speedy";
            Console.WriteLine("Speedy");
        }

        private void SetAggressive(double p)
        {
            double prob = 0.5 - p;
            double[] line1 = { 0.5 + prob, FiveSixths + (prob / 3), 1 };
            double[] line2 = { Third - (p / 3), 1 - (p / 3), 1 };
            double[] line3 = { OneSixth + (prob / 3), 0.5 + prob, 1 };
            matrix = new[] { line1, line2, line3 };
            Aggressive = true;
            Personality = "Aggressive";
            Console.WriteLine("Aggressive");
        }

        private void SetExplorer(double p)
        {
            double prob = ((5 / (double)6) - p);
            double[] line1 = { Third + (1 - p), TwoThirds + prob, 1 };
            double[] line2 = { 1 - p, TwoThirds + prob, 1 };
            double[] line3 = { (1 - p) / 2, OneSixth + prob, 1 };
            matrix = new[] { line1, line2, line3 };
            Explorer = true;
            Personality = "Explorer";
            Console.WriteLine("Explorer");
        }

        public void Transition()
        {
            double transition = rand.NextDouble();
            if (Speedy)
            {
                CheckTransition(transition, 0);
            }
            else if (Aggressive)
            {
                CheckTransition(transition, 1);
            }
            else
            {
                CheckTransition(transition, 2);
            }
        }

        void CheckTransition(double transition, int row)
        {
            for (int i = 0; i < matrix.Length; i++)
            {
                if (!(transition < matrix[row][i])) continue;
                WhichBehaviour(i);
                return;
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
