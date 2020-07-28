using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace Dissertation_mk2
{
    public class DataStorage
    {
        private const string cs = @"Server=localhost\SQLEXPRESS01;Database=Dissertation;Trusted_Connection=True;";

        public void StoreData(List<List<float>> board, double averageFlow, double personality)
        {
            using var con = new SqlConnection(cs);
            con.Open();

                var boardName = Get8CharacterRandomString();

                var cmd = new SqlCommand
                {
                    Connection = con,
                    CommandText = "INSERT INTO boards(board, flow, personality) VALUES('" + boardName + "'," +
                                  averageFlow + "," + personality + ")"
                };

                cmd.ExecuteNonQuery();

                cmd.CommandText = @"CREATE TABLE " + boardName +
                                  "(id int identity(1,1) NOT NULL PRIMARY KEY, row INT, column INT, tile FLOAT)";
                cmd.ExecuteNonQuery();

                for (int i = 0; i < board.Count; i++)
                {
                    for (int j = 0; j < board[0].Count; j++)
                    {
                        cmd.CommandText = "INSERT INTO " + boardName + "(row, column, tile) VALUES('" + i + "'," + j +
                                          "," +
                                          board[i][j] + ")";
                        cmd.ExecuteNonQuery();
                    }
                }

                Console.WriteLine("boards updated");
        }

        public string Get8CharacterRandomString()
        {
            string path = Path.GetRandomFileName();
            path = path.Replace(".", ""); // Remove period.
            return path.Substring(0, 8);  // Return 8 character string
        }

        public List<string> GetBoardNames(double personality)
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

            return new List<string>();
        }

        public List<List<List<float>>> GetBoards(List<string> boardNames)
        {
            var boards = new List<List<List<float>>>();

            using var con = new SqlConnection(cs);
            con.Open();

            for (int i = 0; i < boardNames.Count; i++)
            {

                string sql = "SELECT * FROM " + boardNames[i];
                var cmd = new SqlCommand(sql, con);

                SqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    Console.WriteLine("{0} {1} {2} {3}", rdr.GetInt32(0), rdr.GetInt32(1),
                        rdr.GetInt32(2), rdr.GetFloat(3));

                    boards[i][rdr.GetInt32(1)][rdr.GetInt32(2)] = rdr.GetFloat(3);
                }
            }

            return boards;
        }
    }
}
