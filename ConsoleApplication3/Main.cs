using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;

namespace BowlingNamespace
{
    class Program
    {
        static void Main(string[] args)
        {
            Game game = new Game();
            game.GameStart();
            game.GamePlay();
            game.GameEnd();
        }
    }

    //=======================================================================================================
    /// <summary>
    /// Class dedicated to global variables.
    /// </summary>
    //=======================================================================================================
    public static class Globals
    {
        //SQL Parameters
        public const string server_name = "Bowling";
        public const string db_user = "sa";
        public const string db_password = "pa$$word";

        //Messages
        public const string msg_welcome = "\n~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\nWelcome to the Bowling Alley!\n~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n";
        public const string msg_illegal_command = "Illegal command. Try Again.";
        public const string msg_instructions = "- To roll your ball, type \"roll\" and the number of dropped pins.\n"
                                             + "- To save your progress, type \"save\". You will receive your Game ID when game is saved.\n"
                                             + "- To load a game, type \"load\" and your Game ID.\n"
                                             + "- To exit the game, type \"exit\".\n"
                                             + "\nHave fun!\n";
        public const string msg_end = "\nYour final score is: {0}!"
                                    + "\nPress any key to exit...";
        public const string msg_strike = "Strike!!!";
        public const string msg_spare = "Spare!";
        public const string frame_separator = "------";

        //DB Action Messages
        public const string msg_db_connect = "Connecting to Database...";
        public const string msg_db_connect_success = "Connection Succesful!";
        public const string msg_db_connect_fail = "Failed to connect to DB. You will not be able to save/load games.";
        public const string msg_game_save = "Saving game to DB...";
        public const string msg_save_success = "Game Saved! Your Game ID is {0}.";
        public const string msg_save_fail = "Failed to save to DB.";
        public const string msg_load_warning = "Loading will overwrite the current game. Are you sure you want to continue? Y/N";
        public const string msg_loading = "Loading game...";
        public const string msg_not_loading = "Continuing current game.";
        public const string msg_load_success = "Game loaded successfully!";
        public const string msg_load_fail = "Failed to load from DB.";

        //Commands
        public const string cmd_roll = "roll";
        public const string cmd_exit = "exit";
        public const string cmd_save = "save";
        public const string cmd_load = "load";
        public static readonly string[] cmd_yes = { "Y", "y" };

        //===================================================================================================
        /// <summary>
        /// DB Connection String Getter
        /// </summary>
        //===================================================================================================
        public static string GetSqlConnectionString()
        {
            string res = "Data Source=(local); Initial Catalog=" + server_name
                       + "; User ID=" + db_user
                       + "; Password=" + db_password;
            return res;
        }
    }

    //=======================================================================================================
    /// <summary>
    /// Const queries for DB.
    /// </summary>
    //=======================================================================================================
    public static class Queries
    {
        public const string insert_games = "INSERT INTO games VALUES ({0}, {1}, {2}, {3}, {4});";
        public const string insert_game_frames = "INSERT INTO game_frames VALUES ({0}, {1}, {2}, {3}, {4});";
        public const string insert_game_rolls = "INSERT INTO game_rolls VALUES ({0}, {1}, {2});";
        public const string insert_game_strikes = "INSERT INTO game_strikes VALUES ({0}, {1});";

        public const string count_games = "SELECT COUNT(game_id) AS count FROM games;";

        public const string select_game = "SELECT * FROM games WHERE game_id = {0};";
        public const string select_game_frames = "SELECT * FROM game_frames WHERE game_id = {0} ORDER BY frame_id, roll_id;";
        public const string select_game_rolls = "SELECT * FROM game_rolls WHERE game_id = {0} ORDER BY roll_id;";
        public const string select_game_strikes = "SELECT * FROM game_strikes WHERE game_id = {0} ORDER BY strike_ids;";
    }

    //=======================================================================================================
    /// <summary>
    /// Single game instance.
    /// </summary>
    //=======================================================================================================
    public class Game
    {
        private int game_id, curr_score, curr_frame_id, curr_roll;
        private bool prev_is_spare;
        private List<Frame> frames;
        private List<int> rolls, strike_ids;
        private DBHandler dbh;

        public Game()
        {
            curr_score = 0;
            curr_frame_id = 0;
            curr_roll = 0;
            prev_is_spare = false;
            frames = new List<Frame>();
            rolls = new List<int>();
            strike_ids = new List<int>();
            dbh = new DBHandler(this);
        }

        //===================================================================================================
        /// <summary>
        /// Initialize game by overwriting current game data with loaded game data.
        /// </summary>
        /// <param name="_curr_score"></param>
        /// <param name="_curr_frame_id"></param>
        /// <param name="_curr_roll"></param>
        /// <param name="_prev_is_spare"></param>
        /// <param name="_frames"></param>
        /// <param name="_rolls"></param>
        /// <param name="_strike_indices"></param>
        //===================================================================================================
        private void InitGame(int _game_id, int _curr_score, int _curr_frame_id, int _curr_roll, bool _prev_is_spare,
                                    List<Frame> _frames, List<int> _rolls, List<int> _strike_indices)
        {
            game_id = _game_id;
            curr_score = _curr_score;
            curr_frame_id = _curr_frame_id;
            curr_roll = _curr_roll;
            prev_is_spare = false;
            frames = _frames;
            rolls = _rolls;
            strike_ids = _strike_indices;
        }

        //===================================================================================================
        /// <summary>
        /// Game start procedures and messages.
        /// </summary>
        //===================================================================================================
        public bool GameStart()
        {
            dbh.ConnectToDB();
            Console.WriteLine(Globals.msg_welcome);
            Console.WriteLine(Globals.msg_instructions);
            return true;
        }

        //===================================================================================================
        /// <summary>
        /// Gameplay session
        /// </summary>
        /// <returns></returns>
        //===================================================================================================
        public bool GamePlay()
        {   
            List<string> curr_input;
            int int_param;

            //Start single game loop
            while(curr_frame_id < 10)
            {
                curr_input = Console.ReadLine().Split(' ').ToList();
                switch (curr_input[0])
                {
                    case Globals.cmd_roll:
                        if(!Int32.TryParse(curr_input[1], out int_param)) { Console.WriteLine(Globals.msg_illegal_command); }
                        if (roll(int_param))
                        {
                            Console.WriteLine(Globals.frame_separator);
                            curr_frame_id++;
                        }
                        curr_roll++;
                        break;

                    case Globals.cmd_exit:
                        GameExit();
                        break;
                    case Globals.cmd_save:
                        dbh.SaveGame();
                        break;
                    case Globals.cmd_load:
                        if (!Int32.TryParse(curr_input[1], out int_param)) { Console.WriteLine(Globals.msg_illegal_command); }
                        dbh.LoadGame(int_param);
                        break;
                    default:
                        Console.WriteLine(Globals.msg_illegal_command);
                        break;
                }
            }
            return true;
        }

        //===================================================================================================
        /// <summary>
        /// Game end procedures.
        /// </summary>
        //===================================================================================================
        public bool GameEnd()
        {
            score();
            GameExit();
            return true;
        }

        //===================================================================================================
        /// <summary>
        /// Game Exit procedures.
        /// </summary>
        //===================================================================================================
        private void GameExit()
        {
            dbh.DisconnectFromDB();
            Environment.Exit(0);
        }
        //===================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pins"></param>
        /// <returns></returns>
        //===================================================================================================
        private bool roll(int pins)
        {
            rolls.Add(pins);
            if (frames.Count <= curr_frame_id)
            {
                frames.Add(new Frame(curr_frame_id));
            }

            Frame curr_frame = frames[curr_frame_id];
            curr_frame.UpdateFrameScore(pins, curr_roll, true);

            HandleSpare();
            HandleStrike();

            return HandleFrameEnd(curr_frame);
        }

        //===================================================================================================
        /// <summary>
        /// Prints the final game score to the console.
        /// </summary>
        //===================================================================================================
        private void score()
        {
            if(curr_frame_id == 10)
            {
                Console.WriteLine(Globals.msg_end, curr_score);
                Console.ReadKey();
            }
        }

        //===================================================================================================
        /// <summary>
        /// Verifies and applies additional points from a strike.
        /// </summary>
        //===================================================================================================
        private void HandleStrike()
        {
            if (strike_ids.Count > 0 && frames[strike_ids.First()].GetFrameRollIDs().Last() == curr_roll - 2)
            {
                frames[strike_ids.First()].UpdateFrameScore(GetStrikePoints(), -1, false);
                curr_score += GetStrikePoints();
                strike_ids.RemoveAt(0);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Verifies and applies additional points from a spare.
        /// </summary>
        //===================================================================================================
        private void HandleSpare()
        {
            if (prev_is_spare)
            {
                frames[curr_frame_id - 1].UpdateFrameScore(rolls.Last(), -1, false);
                curr_score += rolls.Last();
                prev_is_spare = false;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Check spare/strike status and update score. Returns true if the frame is done.
        /// </summary>
        /// <param name="curr_frame"></param>
        /// <returns></returns>
        //===================================================================================================
        private bool HandleFrameEnd(Frame curr_frame)
        {
            if (curr_frame.FrameDone())
            {
                if (curr_frame.IsStrike())
                {
                    Console.WriteLine(Globals.msg_strike);
                    strike_ids.Add(curr_frame_id);
                }

                else if (curr_frame.IsSpare())
                {
                    Console.WriteLine(Globals.msg_spare);
                    prev_is_spare = true;
                }

                curr_score += curr_frame.GetFrameScore();
                return true;
            }
            return false;
        }

        //===================================================================================================
        /// <summary>
        /// Return the points added to a successful strike.
        /// </summary>
        /// <returns></returns>
        //===================================================================================================
        private int GetStrikePoints()
        {
            int points = rolls.GetRange(rolls.Count - 2, 2).Sum();
            return points;
        }


        //===================================================================================================
        /// <summary>
        /// Nested DB Handling Class
        /// </summary>
        //===================================================================================================
        public class DBHandler
        {
            private SqlConnection cnn;
            private string query;
            private SqlCommand cmd;
            private Game game;

            public DBHandler(Game _game)
            {
                game = _game;
            }
            //===============================================================================================
            /// <summary>
            /// DB Connection
            /// </summary>
            //===============================================================================================
            internal bool ConnectToDB()
            {
                cnn = new SqlConnection(Globals.GetSqlConnectionString());
                Console.WriteLine(Globals.msg_db_connect);
                try
                {
                    cnn.Open();
                    Console.WriteLine(Globals.msg_db_connect_success);
                    return true;
                }
                catch
                {
                    Console.WriteLine(Globals.msg_db_connect_fail);
                    return false;
                }
            }

            //===============================================================================================
            /// <summary>
            /// Disconnect from DB.
            /// </summary>
            //===============================================================================================
            internal bool DisconnectFromDB()
            {
                cnn.Close();
                return true;
            }
            //===============================================================================================
            /// <summary>
            /// Saves current state of the game.
            /// </summary>
            //===============================================================================================
            internal bool SaveGame()
            {
                Console.WriteLine(Globals.msg_game_save);
                game.game_id = GetGameID();


                bool success;
                success = SaveGamesTable();
                success = SaveGameRolls();
                success = SaveGameStrikes();
                success = SaveGameFrames();

                if (game.game_id != -1 && success)
                {
                    Console.WriteLine(Globals.msg_save_success, game.game_id);
                    return true;
                }
                else
                {
                    Console.WriteLine(Globals.msg_save_fail);
                    return false;
                }
            }

            //===============================================================================================
            /// <summary>
            /// Returns the game_id by either adding one to DB count or returning the current nonzero game_id.
            /// </summary>
            /// <returns></returns>
            //===============================================================================================
            private int GetGameID()
            {
                if (game.game_id > 0) { return game.game_id; }

                cmd = new SqlCommand(Queries.count_games, cnn);
                try
                {
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            game.game_id = (int)r[0];
                        }
                    }
                }

                catch
                {
                    game.game_id = -1;
                }
                return game.game_id;
            }

            //===============================================================================================
            /// <summary>
            /// Saves game data into "games" table in the DB.
            /// </summary>
            /// <returns>Success value</returns>
            //===============================================================================================
            private bool SaveGamesTable()
            {
                int prev_is_spare_bit = game.prev_is_spare ? 1 : 0;
                query = string.Format(Queries.insert_games, game.game_id, game.curr_score, game.curr_frame_id,
                                             game.curr_roll, prev_is_spare_bit);
                cmd = new SqlCommand(query, cnn);
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    return false;
                }
                return true;
            }

            //===============================================================================================
            /// <summary>
            /// Saves the rolls performed in current game into "game_rolls" table in the DB.
            /// </summary>
            /// <returns></returns>
            //===============================================================================================
            private bool SaveGameRolls()
            {
                for (int i = 0; i < game.rolls.Count; i++)
                {
                    int roll = game.rolls[i];
                    query = string.Format(Queries.insert_game_rolls, game.game_id, i, roll);
                    cmd = new SqlCommand(query, cnn);
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        return false;
                    }
                }
                return true;
            }

            //===============================================================================================
            /// <summary>
            /// Saves the strike IDs in the current game into "game_strikes" table in the DB.
            /// </summary>
            /// <returns></returns>
            //===============================================================================================
            private bool SaveGameStrikes()
            {
                foreach (int strike in game.strike_ids)
                {
                    query = string.Format(Queries.insert_game_strikes, game.game_id, strike);
                    cmd = new SqlCommand(query, cnn);
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        return false;
                    }
                }
                return true;
            }

            //===============================================================================================
            /// <summary>
            /// Saves the Frames of the current game into "game_frames" table in the DB.
            /// </summary>
            /// <returns></returns>
            //===============================================================================================
            private bool SaveGameFrames()
            {
                foreach (Frame frame in game.frames)
                {
                    int frame_id = frame.GetFrameID();
                    int fscore = frame.GetFrameScore();
                    List<int> roll_scores = frame.GetFrameRollScores();
                    List<int> roll_ids = frame.GetFrameRollIDs();
                    for (int i = 0; i < roll_scores.Count; i++)
                    {
                        query = string.Format(Queries.insert_game_frames, game.game_id, frame_id,
                                              fscore, roll_ids[i], roll_scores[i]);
                        cmd = new SqlCommand(query, cnn);
                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception e)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }

            //===============================================================================================
            /// <summary>
            /// Overwrite current game with a loaded game.
            /// </summary>
            /// <returns></returns>
            //===============================================================================================
            internal bool LoadGame(int id)
            {
                Console.WriteLine(Globals.msg_load_warning);
                string input = Console.ReadLine();
                if(Globals.cmd_yes.Contains(input))
                {
                    Console.WriteLine(Globals.msg_loading);
                }
                else
                {
                    Console.WriteLine(Globals.msg_not_loading);
                    return false;
                }

                int load_curr_score = 0, load_curr_frame_id = 0, load_curr_roll = 0;
                bool load_prev_is_spare = false;
                List<Frame> load_frames = new List<Frame>();
                List<int> load_rolls = new List<int>(), load_strike_ids = new List<int>();

                bool success;
                success = LoadGameData(id, ref load_curr_score, ref load_curr_frame_id, ref load_curr_roll,
                                       ref load_prev_is_spare);
                success = LoadGameRolls(id, ref load_rolls);
                success = LoadGameStrikes(id, ref load_strike_ids);
                success = LoadGameFrames(id, ref load_frames);

                if (success)
                {
                    game.InitGame(id, load_curr_score, load_curr_frame_id, load_curr_roll, load_prev_is_spare,
                             load_frames, load_rolls, load_strike_ids);
                    Console.WriteLine(Globals.msg_load_success);
                    return true;
                }
                else
                {
                    Console.WriteLine(Globals.msg_load_fail);
                    return false;
                }
            }

            //===============================================================================================
            /// <summary>
            /// Load game data from DB.
            /// </summary>
            /// <param name="id"></param>
            /// <param name="score"></param>
            /// <param name="frame_id"></param>
            /// <param name="roll"></param>
            /// <param name="prev"></param>
            /// <returns></returns>
            //===============================================================================================
            private bool LoadGameData(int id, ref int score, ref int frame_id, ref int roll, ref bool prev)
            {
                query = string.Format(Queries.select_game, id);
                cmd = new SqlCommand(query, cnn);

                try
                {
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            score = (int)r["score"];
                            frame_id = (int)r["curr_frame_id"];
                            roll = (int)r["curr_roll"];
                            prev = (bool)r["prev_is_spare"];
                        }
                    }
                }
                catch (Exception e)
                {
                    return false;
                }
                return true;
            }

            //===============================================================================================
            /// <summary>
            /// Load game rolls from DB.
            /// </summary>
            /// <param name="id"></param>
            /// <param name="rolls"></param>
            /// <returns></returns>
            //===============================================================================================
            private bool LoadGameRolls(int id, ref List<int> rolls)
            {
                query = string.Format(Queries.select_game_rolls, id);
                cmd = new SqlCommand(query, cnn);

                try
                {
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            rolls.Add((int)r["roll"]);
                        }
                    }
                }
                catch (Exception e)
                {
                    return false;
                }
                return true;
            }

            //===============================================================================================
            /// <summary>
            /// Load game strike ids from DB.
            /// </summary>
            /// <param name="id"></param>
            /// <param name="strikes"></param>
            /// <returns></returns>
            //===============================================================================================
            private bool LoadGameStrikes(int id, ref List<int> strikes)
            {
                query = string.Format(Queries.select_game_strikes, id);
                cmd = new SqlCommand(query, cnn);

                try
                {
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            strikes.Add((int)r["strike_ids"]);
                        }
                    }
                }
                catch (Exception e)
                {
                    return false;
                }
                return true;
            }

            //===============================================================================================
            /// <summary>
            /// Load game frames from DB.
            /// </summary>
            /// <param name="id"></param>
            /// <param name="load_frames"></param>
            /// <returns></returns>
            //===============================================================================================
            private bool LoadGameFrames(int id, ref List<Frame> load_frames)
            {
                query = string.Format(Queries.select_game_frames, id);
                cmd = new SqlCommand(query, cnn);
                Frame temp_frame = null;
                int frame_id;

                try
                {
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            frame_id = (int)r["frame_id"];
                            if (temp_frame == null || temp_frame.GetFrameID() != frame_id)
                            {
                                if (temp_frame != null)
                                {
                                    load_frames.Add(temp_frame);
                                }
                                temp_frame = new Frame(frame_id);
                            }

                            temp_frame.UpdateFrameScore((int)r["roll_score"], (int)r["roll_id"], true);
                            temp_frame.OverwriteFscore((int)r["fscore"]);
                        }
                    }
                }
                catch (Exception e)
                {
                    return false;
                }
                load_frames.Add(temp_frame); //Add last frame.
                return true;
            }
        }
    }

    //=======================================================================================================
    /// <summary>
    /// A single bowling frame and its relevant functionalities.
    /// </summary>
    //=======================================================================================================
    public class Frame
    {
        private int id;
        private List<int> roll_scores, roll_ids;
        private int fscore;

        public Frame(int _id)
        {
            id = _id;
            roll_scores = new List<int>();
            roll_ids = new List<int>();
        }

        //===================================================================================================
        /// <summary>
        /// Return true if the frame is completed.
        /// </summary>
        /// <returns></returns>
        //===================================================================================================
        public bool FrameDone()
        {
            if(id < 9)
            {
                return (roll_scores.Count >= 2 || (roll_scores.Sum() != 0 && roll_scores.Sum() % 10 == 0));
            }
            else
            {
                return (roll_scores.Count > 1 && GetFirstTwoSum() < 10) || roll_scores.Count > 2;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Checks if frame is a strike.
        /// </summary>
        /// <returns></returns>
        //===================================================================================================
        public bool IsStrike()
        {
            return roll_scores.Any(t => t == 10);
        }

        //===================================================================================================
        /// <summary>
        /// Checks if the frame is a spare.
        /// </summary>
        /// <returns></returns>
        //===================================================================================================
        public bool IsSpare()
        {
            return roll_scores.Count > 1 && GetFirstTwoSum() == 10;
        }

        //===================================================================================================
        /// <summary>
        /// Updates frame score and roll data.
        /// </summary>
        /// <param name="roll"></param>
        /// <param name="roll_id"></param>
        /// <param name="update_throws"></param>
        /// <returns></returns>
        //===================================================================================================
        public bool UpdateFrameScore(int roll, int roll_id, bool update_throws)
        {
            if (update_throws)
            {
                roll_scores.Add(roll);
                roll_ids.Add(roll_id);
            }
            fscore += roll;

            return true;
        }

        //===================================================================================================
        /// <summary>
        /// Overwrite the frame score.
        /// Do not use except for game loading purposes!
        /// </summary>
        /// <param name="new_fscore"></param>
        /// <returns></returns>
        //===================================================================================================
        public bool OverwriteFscore(int new_fscore)
        {
            //Future development: Add authentication
            fscore = new_fscore;
            return true;
        }

        //===================================================================================================
        /// <summary>
        /// Getter for frame id.
        /// </summary>
        /// <returns></returns>
        //===================================================================================================
        public int GetFrameID()
        {
            return id;
        }

        //===================================================================================================
        /// <summary>
        /// Returns the sum of the first two rolls in the frame.
        /// </summary>
        /// <returns></returns>
        //===================================================================================================
        public int GetFirstTwoSum()
        {
            return roll_scores.GetRange(0, Math.Min(roll_scores.Count, 2)).Sum();
        }

        //===================================================================================================
        /// <summary>
        /// Getter for frame roll scores.
        /// </summary>
        /// <returns></returns>
        //===================================================================================================
        public List<int> GetFrameRollScores()
        {
            return roll_scores;
        }

        //===================================================================================================
        /// <summary>
        /// Getter for frame roll ids.
        /// </summary>
        /// <returns></returns>
        //===================================================================================================
        public List<int> GetFrameRollIDs()
        {
            return roll_ids;
        }

        //===================================================================================================
        /// <summary>
        /// Getter for frame score.
        /// </summary>
        /// <returns></returns>
        //===================================================================================================
        public int GetFrameScore()
        {
            return fscore;
        }
    }
}
