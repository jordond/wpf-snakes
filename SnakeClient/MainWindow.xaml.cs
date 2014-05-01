using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ServiceModel;
using SnakeLibrary;

//Name:       MainWindow.xaml.cs
//Authors:    Jordon DeHoog & Jon Decher
//Date:       April 11, 2014
//Purpose:    Handles all tools used by the client. 
//            Updates to the server callback.
//            Paints snake and food object.

namespace SnakeClient
{
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, UseSynchronizationContext = false)]
    public partial class MainWindow : Window, ICallback
    {
        /// <summary>
        /// Array holding available colours for the snakes.
        /// </summary>
        private SolidColorBrush[] _colors = new SolidColorBrush[8] 
                    { Brushes.Blue, Brushes.Brown, Brushes.Coral, Brushes.DarkGreen,
                        Brushes.DarkOrange, Brushes.Gold, Brushes.Maroon, Brushes.Silver };

        /// <summary>
        /// Use keypad to move the snake
        /// </summary>
        private enum DIRECTION
        {
            UP = 8,
            DOWN = 2,
            LEFT = 4,
            RIGHT = 6
        };

        //Private member vaiables
        private IGame gameInstance = null;
        private int callbackId = 0;

        private Dictionary<int, List<Rect>> _players = new Dictionary<int, List<Rect>>();
        private Rect _food;
        private int _player = 0;
        private bool spectating = false;
        private int colorCode = 0;

        //Callback delegates
        private delegate void InfoUpdateDelegate(GameInformation newInfo);
        private delegate void PlayersUpdateDelegate(Dictionary<int, List<Rect>> playersLocation);

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Connect to game method - configures the endpoint details then activates the remote object
        /// Enables and disables tools depending on if the user is spectating or not
        /// Paints the arena gray
        /// </summary>
        public void connectToGame()
        {
            try
            {
                // Configure the Endpoint details
                DuplexChannelFactory<IGame> channel = new DuplexChannelFactory<IGame>(this, "Game");

                // Activate a remote game object
                gameInstance = channel.CreateChannel();
                if(callbackId == 0)
                    callbackId = gameInstance.CallbackRegister();

                if (!spectating)
                {
                    _player = gameInstance.createPlayer(txtName.Text);
                    if (_player == 0)
                    {
                        gameInstance.Spectator++;
                        spectating = true;
                        txtName.Text = "";
                        txtName.IsEnabled = true;
                        cmdConnect.IsEnabled = false;
                        cmdSpectate.IsEnabled = false;
                        lblColorText.Visibility = Visibility.Hidden;
                        lblColor.Visibility = Visibility.Hidden;
                    }
                    else
                        this.KeyDown += new KeyEventHandler(buttonDown);

                    lblColorText.Visibility = Visibility.Visible;
                    lblColor.Visibility = Visibility.Visible;
                    
                    colorCode++;
                }
                else
                {
                    gameInstance.Spectator++;
                    lblColorText.Visibility = Visibility.Hidden;
                    lblColor.Visibility = Visibility.Hidden;
                }

                gameBoard.Background = new SolidColorBrush(Colors.Gray);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error communicating with server: " + ex.Message);
            }
        }

        /// <summary>
        /// Moves the snake object around the arena with keys pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonDown(object sender, KeyEventArgs e)
        {
            try
            {
                int direction = 0;
                switch (e.Key)
                {
                    case Key.Down:
                        direction = (int)DIRECTION.DOWN;
                        break;
                    case Key.Up:
                        direction = (int)DIRECTION.UP;
                        break;
                    case Key.Left:
                        direction = (int)DIRECTION.LEFT;
                        break;
                    case Key.Right:
                        direction = (int)DIRECTION.RIGHT;
                        break;
                }
                gameInstance.updateDirection(_player, direction);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error communicating with server: " + ex.Message);
            }
        }

        //Painting methods
        /// <summary>
        /// Paints each snake being used individually
        /// </summary>
        private void paintSnakes()
        {
            int colorID = 0;
            foreach (var player in _players.Keys)
            {
                var listLocation = _players[player];
                if (player == _player)
                    lblColor.Background = _colors[colorID];
                foreach (var location in listLocation)
                {
                    Rectangle snakePiece = new Rectangle();
                    snakePiece.Fill = _colors[colorID];
                    snakePiece.Width = location.Width;
                    snakePiece.Height = location.Height;

                    Canvas.SetLeft(snakePiece, location.X);
                    Canvas.SetTop(snakePiece, location.Y);

                    gameBoard.Children.Add(snakePiece);                    
                }
                colorID++;
            }
        }

        /// <summary>
        /// Paints and sizes the food object then inserts it onto the board
        /// </summary>
        private void paintFood()
        {
            if (_food != null)
            {
                Ellipse food = new Ellipse();
                food.Fill = Brushes.Red;
                food.Width = _food.Width;
                food.Height = _food.Height;

                Canvas.SetLeft(food, _food.X);    //x 
                Canvas.SetTop(food, _food.Y);     //y

                gameBoard.Children.Insert(0, food);
            }
        }

        //Gui events
        /// <summary>
        /// Enables and disables the name input box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtName.Text != "")
                cmdConnect.IsEnabled = true;
            else
                cmdConnect.IsEnabled = false;
        }

        /// <summary>
        /// When connect button is clicked, enables the spectate and disconnect button
        /// Disables name input, connect button and spectating.
        /// Calls connectToGame()
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdConnect_Click(object sender, RoutedEventArgs e)
        {
            txtName.IsEnabled = false;
            cmdConnect.IsEnabled = false;
            cmdSpectate.IsEnabled = true;
            cmdDisconnect.IsEnabled = true;
            spectating = false;
            if (spectating)
                gameInstance.Spectator--;
            connectToGame(); //connect as player
        }

        /// <summary>
        /// When the spectate button is clicked it deletes the current player from the game 
        /// then allows the user to watch the current game in progress.
        /// Enables name input and disconeect button.
        /// Disables the spectate button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdSpectate_Click(object sender, RoutedEventArgs e)
        {
            if (_player != 0)
            {                
                gameInstance.deletePlayer(_player);
                _player = 0;
                this.KeyDown -= buttonDown;
            }

            spectating = true;
            connectToGame(); //connect as spectator
            txtName.Text = "";
            txtName.IsEnabled = true;
            cmdSpectate.IsEnabled = false;
            cmdDisconnect.IsEnabled = true;
            if(colorCode != 0)
                colorCode--;
        }

        /// <summary>
        /// When disconnect button is clicked it deletes the callback,
        /// deletes game connection, clears board and resets all buttons.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                //delete callback
                if (callbackId != 0 && gameInstance != null)
                {
                    gameInstance.CallbackUnregister(callbackId);
                    callbackId = 0;
                }

                if (_player != 0)
                {
                    gameInstance.deletePlayer(_player);
                    _player = 0;
                    this.KeyDown -= buttonDown;
                    if (colorCode != 0)
                        colorCode--;
                }
                else if (spectating)
                {
                    gameInstance.Spectator--;
                    spectating = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error with server: " + ex.Message);
            }

            //Delete game connection
            gameInstance = null;

            //Clear playing field
            gameBoard.Children.Clear();
            listPlayers.ItemsSource = null;
            listMessages.ItemsSource = null;
            txtName.Text = "";
            txtName.IsEnabled = true;
            lblColorText.Visibility = Visibility.Hidden;
            lblColor.Visibility = Visibility.Hidden;
            gameBoard.Background = Brushes.White;

            //Reset buttons
            cmdConnect.IsEnabled = false;
            cmdDisconnect.IsEnabled = false;
            cmdSpectate.IsEnabled = true;
        }

        /// <summary>
        /// Unregisters the callback when the game is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (callbackId != 0 && gameInstance != null)
                gameInstance.CallbackUnregister(callbackId);

            if (_player != 0)
            {
                gameInstance.deletePlayer(_player);
                _player = 0;
                this.KeyDown -= buttonDown;
                if (colorCode != 0)
                    colorCode--;
            }
        }

        //Server callbacks 
        /// <summary>
        /// Updates the info to the server callback
        /// </summary>
        /// <param name="newInfo">New game information</param>
        public void UpdateInfo(GameInformation newInfo)
        {
            if (System.Threading.Thread.CurrentThread == this.Dispatcher.Thread)
            {
                listPlayers.ItemsSource = null;
                listMessages.ItemsSource = null;
                listPlayers.ItemsSource = newInfo.Scoreboard;
                listMessages.ItemsSource = newInfo.Messages;
                _food = newInfo.FoodLocation;
            }
            else
                this.Dispatcher.BeginInvoke(new InfoUpdateDelegate(UpdateInfo), newInfo);
        }

        /// <summary>
        /// Updates the players to the server callback
        /// </summary>
        /// <param name="playersLocation">Location of the player</param>
        public void UpdatePlayers(Dictionary<int, List<Rect>> playersLocation)
        {
            if (System.Threading.Thread.CurrentThread == this.Dispatcher.Thread)
            {
                _players = playersLocation;
                gameBoard.Children.Clear();

                paintFood();
                paintSnakes();
            }
            else
                this.Dispatcher.BeginInvoke(new PlayersUpdateDelegate(UpdatePlayers), playersLocation);
        }
    }
}
