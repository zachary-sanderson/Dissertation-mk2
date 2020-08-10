using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Text;

namespace Dissertation_mk2
{
    public class DataStorage
    {

        private const string cs = @"Server=localhost\SQLEXPRESS01;Database=Dissertation;Trusted_Connection=True;";

        public static void StoreData(List<List<int>> board, double averageFlow, double personality)
        {
            using var con = new SqlConnection(cs);
            con.Open();

                var boardName = GenerateRandomString(12, new Random());

                var cmd = new SqlCommand
                {
                    Connection = con,
                    CommandText = "INSERT INTO boards(board, flow, personality) VALUES('" + boardName + "'," +
                                  averageFlow + "," + personality + ")"
                };

                cmd.ExecuteNonQuery();

                cmd.CommandText = @"CREATE TABLE " + boardName +
                                  "(id int identity(1,1) NOT NULL PRIMARY KEY,rowNum SMALLINT,colNum SMALLINT,tile SMALLINT);";
                cmd.ExecuteNonQuery();

                for (int i = 0; i < board.Count; i++)
                {
                    for (int j = 0; j < board[0].Count; j++)
                    {
                        cmd.CommandText = "INSERT INTO " + boardName + "(rowNum, colNum, tile) VALUES('" + i + "'," + j +
                                          "," +
                                          board[i][j] + ")";
                        cmd.ExecuteNonQuery();
                    }
                }

                Console.WriteLine("boards updated");
        }

        private static string GenerateRandomString(int length, Random random)
        {
            string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            StringBuilder result = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                result.Append(characters[random.Next(characters.Length)]);
            }
            return result.ToString();
        }

        public static List<string> GetBoardNames(double personality)
        {
            using var con = new SqlConnection(cs);
            con.Open();

            string sql = "SELECT * FROM boards";
            var cmd = new SqlCommand(sql, con);

            SqlDataReader rdr = cmd.ExecuteReader();

            var boardNames = new List<string>();
            while (rdr.Read())
            {
                Console.WriteLine("{0} {1} {2} {3}", rdr.GetInt32(0), rdr.GetString(1),
                    rdr.GetDouble(2), rdr.GetDouble(3));

                if (Math.Abs(personality - rdr.GetDouble(3)) < 0.1d)
                    boardNames.Add(rdr.GetString(1));
            }

            return boardNames;
        }

        public static List<List<List<int>>> GetBoards(List<string> boardNames)
        {
            var boards = new List<List<List<int>>>();

            for (int i = 0; i < boardNames.Count; i++)
            {
                using var con = new SqlConnection(cs);
                con.Open();

                string sql = "SELECT * FROM " + boardNames[i];
                var cmd = new SqlCommand(sql, con);

                SqlDataReader rdr = cmd.ExecuteReader();

                List<List<int>> board = new List<List<int>>();

                int rowNum = 0;
                List<int> row = new List<int>();
                while (rdr.Read())
                {
                    Console.WriteLine("{0} {1} {2} {3}", rdr.GetInt32(0), rdr.GetInt16(1),
                        rdr.GetInt16(2), rdr.GetInt16(3));

                    if (rdr.GetInt16(1) == rowNum)
                    {
                        row.Add(rdr.GetInt16(3));
                    }
                    else
                    {
                        board.Add(row);
                        rowNum = rdr.GetInt16(1);
                        row = new List<int> {rdr.GetInt16(3)};
                    }
                }
                board.Add(row);
                boards.Add(board);

                rdr.Close();
            }

            return boards;
        }
    }
}
