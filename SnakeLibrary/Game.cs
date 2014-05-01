using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.ServiceModel;
using System.Windows;
using System.Net;
using System.Net.Sockets;

//Class Name: Game
//Authors:    Jordon DeHoog & Jon Decher
//Date:       April 11, 2014
//Purpose:    Creates, updates and deletes all objects in game including users.

namespace SnakeLibrary
{
    public interface ICallback
    {
        [OperationContract(IsOneWay = true)]
        void UpdateInfo(GameInformation newInfo);
        [OperationContract(IsOneWay = true)]
        void UpdatePlayers(Dictionary<int, List<Rect>> playersLocation);
    }

    [ServiceContract(CallbackContract = typeof(ICallback))]
    public interface IGame
    {
        [OperationContract(IsOneWay = true)]
        void updateDirection(int id, int direction);
        [OperationContract]
        int createPlayer(string name);
        [OperationContract(IsOneWay = true)]
        void deletePlayer(int id);
        int Spectator { [OperationContract] get; [OperationContract] set; }
        [OperationContract]
        int CallbackRegister();
        [OperationContract(IsOneWay = true)]
        void CallbackUnregister(int id);
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class Game : IGame
    {
        public const int MSGLEVEL = 1;                          //level to output to client
        public const int CONSOLELEVEL = 2;                      //level to output to console

        private const int MAXPLAYERS = 8;                       //default 6-8 - do not exceed 8
        private const int GAMESPEED = 20;                       //game clock 20 milliseconds        
        private const int SNAKESIZE = 10;                       //default 10
        private const int FOODSIZE = SNAKESIZE;                 //default 10
        private const int MOVEMENTSPEED = SNAKESIZE - 5;        //default 5 - lower = faster
        private const int UPPERBOUNDS = 500 - SNAKESIZE;        //high bounds for random XY
        private const int LOWERBOUNDS = 0 + SNAKESIZE;          //low bounds for random XY

        private Dictionary<int, Snake> _players = new Dictionary<int, Snake>();     //contains all player objects
        private Dictionary<int, List<Rect>> _playerLocations = new Dictionary<int, List<Rect>>(); //contains list of coords
        private Rect _foodLocation;                             //contains XY for food 
        private int _spectators = 0;                            //number of spectators
        private bool isGameOver = false;

        private Dictionary<int, ICallback> clientCallbacks = new Dictionary<int, ICallback>();

        private Dictionary<string, int> _scoreBoard = new Dictionary<string, int>(); //player and score list
        private List<string> _messages = new List<string>();    //gui messages, level set by MSGLEVEL

        private Timer timer = new Timer(GAMESPEED);     //timer object calls timerTick set by GAMESPEED
        Random rnd = new Random();

        private enum DIRECTION
        {
            UP = 8,
            DOWN = 2,
            LEFT = 4,
            RIGHT = 6
        };

        /// <summary>
        /// Default constructor, register the timer elapsed event.
        /// post information messages about game.
        /// </summary>
        public Game()
        {
            timer.Elapsed += new ElapsedEventHandler(timerTick);
            timer.Enabled = true;

            postMessage(2, "Local server address: " + getIPAddress());
            postMessage(2, "Gamespeed: " + GAMESPEED + " milliseconds");
            postMessage(2, "Snake Size: " + SNAKESIZE);
            postMessage(2, "Food Size: " + FOODSIZE);
            postMessage(2, "Movement Speed: " + MOVEMENTSPEED);
            postMessage(0, "Game has been loaded");
            postMessage(0, "Waiting for players...");
        }

        /// <summary>
        /// Register each GUI client for callbacks
        /// </summary>
        /// <returns>Returns clients callback ID (int)</returns>
        public int CallbackRegister()
        {
            ICallback callback = OperationContext.Current.GetCallbackChannel<ICallback>();
            int id = rnd.Next(1, 1000);
            clientCallbacks.Add(id, callback);

            return id;
        }

        /// <summary>
        /// Unregister client from the callback functions
        /// </summary>
        /// <param name="id">ID of the client to unregister</param>
        public void CallbackUnregister(int id)
        {
            timer.Stop();
            clientCallbacks.Remove(id);
            timer.Start();
        }

        /// <summary>
        /// Set the location of the food to a random spot on the board
        /// </summary>
        private void createFood()
        {
            int x = rnd.Next(LOWERBOUNDS, UPPERBOUNDS);
            int y = rnd.Next(LOWERBOUNDS, UPPERBOUNDS);
            _foodLocation = new Rect(x, y, FOODSIZE, FOODSIZE);
            postMessage(2, "Ball Created at " + "(" + x + "," + y + ")");
        }

        /// <summary>
        /// Set the location of the snake to a random spot on the board
        /// </summary>
        /// <param name="name">Name of the player</param>
        /// <returns>Snake object with random location</returns>
        private Snake createSnakeHead(string name)
        {
            int x = rnd.Next(LOWERBOUNDS, UPPERBOUNDS);
            int y = rnd.Next(LOWERBOUNDS, UPPERBOUNDS);
            var newHead = new Rect(x, y, SNAKESIZE, SNAKESIZE);

            Snake newSnakeHead = new Snake(name, newHead);
            newSnakeHead.ID = rnd.Next(1, 1000);

            return newSnakeHead;
        }

        /// <summary>
        /// Client method.  Creates a Snake object based on the name
        /// provided by the client.  Checks if the max number of players
        /// has been reached, checks if the players name already exists.
        /// Starts the game clock and creates a food object
        /// </summary>
        /// <param name="name">Name of the player</param>
        /// <returns>0 if spectating, int ID of player if otherwise</returns>
        public int createPlayer(string name)
        {
            if (_players.Count >= MAXPLAYERS)
            {
                postMessage(0, "Max Players reached, Spectating instead");
                return 0;
            }

            if (_scoreBoard.ContainsKey(name))
            {
                string temp = name;
                postMessage(2, name + " - already exists");
                name += rnd.Next(0, 10);
                postMessage(1, "Renaming '" + temp + "' to: " + name);
            }

            Snake newHead = createSnakeHead(name);
            _scoreBoard.Add(newHead.Name, newHead.Score);

            if (_players.Count == 0)
            {
                timer.Start();
                postMessage(2, "Game clock started...");
                createFood();
            }

            _players.Add(newHead.ID, newHead);
            postMessage(0, "Player: " + newHead.Name + " has entered the game.");
            postMessage(2, "Player " + newHead.Name + " created at " + "(" + newHead.Location.ElementAt(0).X + "," + newHead.Location.ElementAt(0).Y + ")");
            postMessage(2, "Num Players: " + _players.Count());
            return newHead.ID;
        }

        /// <summary>
        /// Deletes the snake object from the players dictionary, and
        /// removes player from the scoreboard.  If no players are left
        /// the game clock will be stopped.
        /// </summary>
        /// <param name="id">ID of the player to be deleted</param>
        public void deletePlayer(int id)
        {
            Snake player = _players[id];
            _players.Remove(id);
            _scoreBoard.Remove(player.Name);
            postMessage(0, player.Name + " has left the game.");
            postMessage(1, "Players remaining: " + _players.Count());

            if (_players.Count == 0)
            {
                postMessage(1, "Waiting for players...");
                timer.Stop();
                postMessage(2, "Game clock stopped.");
            }
        }

        /// <summary>
        /// Accessor methods for Spectator property
        /// </summary>
        public int Spectator
        {
            get { return _spectators; }
            set
            {
                if (value < _spectators)
                {
                    _spectators--;
                    postMessage(1, "A Spectator has left.");
                    postMessage(2, "Spectators remaining: " + _spectators);
                }
                else
                {
                    _spectators++;
                    postMessage(1, "New Spectator has joined.");
                    postMessage(2, "Spectators remaining: " + _spectators);
                }
            }
        }

        /// <summary>
        /// Game clock event:
        /// First it stops to timer to prevent race condition and multiple calls
        /// to the event before it can finish.
        /// Checks if there are any players to save processing time.
        /// Loops through each <Snake>player</Snake> and calculates a new XY based on
        /// the direction of the player. It then performs collision detection (self,wall,food)
        /// Adds an extra <Rect>head</Rect> object to give the illusion of a "body". Based
        /// on the length of the snake it will remove excess body pieces.
        /// It then populates the Dictionary<int,List<Rect>> with the location of all players
        /// then performs a callback to the clients to update the paint method.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timerTick(object sender, EventArgs e)
        {
            timer.Stop(); //allow event to finish
            if (_players.Count != 0 && !isGameOver)
            {
                var listToModify = new List<Snake>();
                foreach (var player in _players.Values)
                {
                    Rect head = player.Location[0];
                    switch (player.Direction)
                    {
                        case (int)DIRECTION.DOWN:
                            head.Y += MOVEMENTSPEED;
                            break;
                        case (int)DIRECTION.UP:
                            head.Y -= MOVEMENTSPEED;
                            break;
                        case (int)DIRECTION.LEFT:
                            head.X -= MOVEMENTSPEED;
                            break;
                        case (int)DIRECTION.RIGHT:
                            head.X += MOVEMENTSPEED;
                            break;
                    }

                    //refactor - maybe resetSnake()
                    if (player.checkBoundaryCollision(SNAKESIZE))
                    {
                        Snake temp = createSnakeHead(player.Name);
                        temp.ID = player.ID;
                        listToModify.Add(temp);
                        _scoreBoard[player.Name] = 0;
                        postMessage(0, player.Name + " hit a wall, and has been reset.");
                    }

                    //Move, and grow the snake
                    player.Location[0] = head;
                    player.Location.Add(head); // snake body
                    if (player.Location.Count > player.Length) //cutoff snake
                        player.Location.RemoveAt(player.Location.Count - player.Length);

                    if (player.checkFoodCollision(_foodLocation))
                    {
                        createFood();
                        player.Length += 10;
                        player.Score++;
                        if (player.Score >= 30)
                            gameOver(player);
                        _scoreBoard[player.Name] = player.Score;
                        postMessage(0, player.Name + " has scored a point!.");
                        postMessage(2, player.Name + " new length: " + player.Length);
                    }
                    if (player.checkSelfCollision(SNAKESIZE) || player.checkEnemyCollision(_players))
                    {
                        postMessage(0, player.Name + " has had an accident!");
                        Snake temp = createSnakeHead(player.Name);
                        temp.ID = player.ID;
                        listToModify.Add(temp);
                        _scoreBoard[player.Name] = 0;
                        postMessage(1, player.Name + " has been reset.");
                    }
                    postMessage(3, player.Name + " position: " + "(" + player.Location.ElementAt(0).X + "," + player.Location.ElementAt(0).Y + ")");
                }

                foreach (var item in listToModify)
                    _players[item.ID] = item;

                getPlayerLocations();
                foreach (ICallback c in clientCallbacks.Values)
                    c.UpdatePlayers(_playerLocations);
            }
            timer.Start();
        }

        /// <summary>
        /// Client call method, updates the players movement direction.
        /// First checks to see if the direction is the same as the last direction.
        /// It then checks to make sure the direction isn't the opposite, so that the snake
        /// cannot move backwards and through itself
        /// </summary>
        /// <param name="id">ID of the player</param>
        /// <param name="direction">New direction of the player</param>
        public void updateDirection(int id, int direction)
        {
            //timer.Stop(); //temporary fix for timerTick race condition
            Snake player = _players[id];

            if (direction != player.PreviousDirection)
            {
                switch (direction)
                {
                    case (int)DIRECTION.DOWN:
                        if (player.PreviousDirection != (int)DIRECTION.UP)
                            player.Direction = direction;
                        break;
                    case (int)DIRECTION.UP:
                        if (player.PreviousDirection != (int)DIRECTION.DOWN)
                            player.Direction = direction;
                        break;
                    case (int)DIRECTION.LEFT:
                        if (player.PreviousDirection != (int)DIRECTION.RIGHT)
                            player.Direction = direction;
                        break;
                    case (int)DIRECTION.RIGHT:
                        if (player.PreviousDirection != (int)DIRECTION.LEFT)
                            player.Direction = direction;
                        break;
                }
                _players[player.ID].PreviousDirection = direction; //trying new fix for race condition
                _players[player.ID].Direction = player.Direction;
                //_players[player.ID] = player; //causing race condition in timerTick foreach
                postMessage(3, "Player: " + player.Name + " Direction: " + direction + " Previous direction: " + player.PreviousDirection);
                //timer.Start(); // temporary fix for timerTick race condition

            }
        }

        /// <summary>
        /// Disables the timer and displays the winning player with a "Game Over!" message to all players.
        /// </summary>
        /// <param name="player">The current player</param>
        private void gameOver(Snake player)
        {
            timer.Enabled = false;
            isGameOver = true;
            postMessage(0, "Game over! " + player.Name + " has won the game!");
        }

        /// <summary>
        /// Populate the Dictonary<int,List<Rect>> with the players ID
        /// and list of locations.
        /// </summary>
        private void getPlayerLocations()
        {
            _playerLocations.Clear();
            foreach (Snake player in _players.Values)
                _playerLocations.Add(player.ID, player.Location);
        }

        /// <summary>
        /// Helper method to convert the Dictionary of players and scores into
        /// a string array. For less overhead over the wire
        /// </summary>
        /// <returns>String arrary of player and score</returns>
        private string[] getScoreboard()
        {
            if (_scoreBoard == null)
                return null;

            List<string> scores = new List<string>();
            int count = 1;
            foreach (var item in _scoreBoard)
            {
                scores.Add(count + ". " + item.Key + ": " + item.Value);
                count++;
            }

            return scores.ToArray<string>();
        }

        //fix sending multiple times
        /// <summary>
        /// Adds messages to the List<string> of messages, checks to see
        /// the level of message reporting from the MSGLEVEL and CONSOLELVL
        /// It then creates a GameInformation object and sends that to the client
        /// </summary>
        /// <param name="code">Level of message reporting</param>
        /// <param name="message">Contents of message</param>
        public void postMessage(int code, string message)
        {
            if (code <= MSGLEVEL)
            {
                _messages.Insert(0, message);
                if (clientCallbacks.Count != 0)
                {
                    GameInformation newInfo = new GameInformation(getScoreboard(), _messages.ToArray<string>(), _foodLocation);
                    foreach (ICallback c in clientCallbacks.Values)
                        c.UpdateInfo(newInfo);
                }
            }
            if (code <= CONSOLELEVEL)
            {
                string level = "";
                switch (code)
                {
                    case 0:  level = "IMPORTANT: "; break;
                    case 1:  level = "INFO:  ";     break;
                    case 2:  level = "DEBUG: ";     break;
                    case 3:  level = "VERBOSE: ";   break;
                    default: level = "NONE: ";      break;
                }
                Console.WriteLine(level + message);
            }
        }

        /// <summary>
        /// Gets the current local IP address from the dns 
        /// taken from: https://stackoverflow.com/questions/6803073/get-local-ip-address-c-sharp
        /// </summary>
        /// <returns>local IP address</returns>
        public string getIPAddress()
        {
            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                IPHostEntry host;
                string localIP = "";
                host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIP = ip.ToString();
                        break;
                    }
                }
                return localIP;
            }
            return "Unvailable";
        }

    }//Game.cs
}
