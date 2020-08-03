using System;

namespace Dissertation_mk2
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            int iter = 0;
            //Random rand = new Random();
            //double pValue = rand.NextDouble();
            double pValue = 1d;
            while (iter < 100)
            {
                Board newBoard = new Board(pValue);
                while (!newBoard.validated)
                {
                    Console.WriteLine("newboard not validated!");
                    newBoard = new Board(pValue);
                }

                Solution solution = new Solution(newBoard, newBoard.markov.Personality, pValue, newBoard.numItems,
                    newBoard.numEnemies);

                GameManager gm = new GameManager(solution, 0);
                {
                    gm.PlayGame();
                }

                iter++;
            }
            */


            //_ = new Board();
            _ = new GA();
        }
    }
}
