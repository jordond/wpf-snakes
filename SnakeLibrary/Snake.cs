using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

//Class Name: Snake
//Authors:    Jordon DeHoog & Jon Decher
//Date:       April 11, 2014
//Purpose:    Instantiates the snake object and handles the collision detection.

namespace SnakeLibrary
{
    /// <summary>
    /// Snake class containing the properties of the Snake object and the collision methods.
    /// </summary>
    public class Snake
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int Score { get; set; }
        public List<Rect> Location { get; set; }
        public int Length { get; set; }
        public int Direction { get; set; }
        public int PreviousDirection { get; set; }

        /// <summary>
        /// Default constructor initializing new instance member variables.
        /// </summary>
        public Snake()
        {
            Score = 0;
            Direction = 0;
            PreviousDirection = 0;
            Length = 5;
            Location = new List<Rect>();
        }

        /// <summary>
        /// Constructor initializing instance member variables.
        /// </summary>
        /// <param name="name">Name of snake</param>
        /// <param name="loc">Location of snake</param>
        public Snake(string name, Rect loc)
        {
            Name = name;
            Score = 0;
            Direction = 0;
            PreviousDirection = 0;
            Length = 5;
            Location = new List<Rect>();
            Location.Add(loc);
        }

        /// <summary>
        /// Checks if the snake is within the boundary of the game.
        /// </summary>
        /// <param name="SNAKESIZE">The snake size.</param>
        /// <returns>True if snake exits the boundary. False otherwise.</returns>
        public bool checkBoundaryCollision(int SNAKESIZE)
        {
            if ((Location[0].X < 0) || (Location[0].X > 500 - SNAKESIZE) ||
                (Location[0].Y < 0) || (Location[0].Y > 500 - SNAKESIZE))
                return true;
            return false;
        }

        /// <summary>
        /// Checks if the snake has collided with itself.
        /// </summary>
        /// <param name="SNAKESIZE">The snake size.</param>
        /// <returns>True if player collides withitself. False otherwise.</returns>
        public bool checkSelfCollision(int SNAKESIZE)
        {
            if (Length != 10)
            {
                for (int i = 1; i < Location.Count - SNAKESIZE; i++)
                {
                    if (Location[0].Contains(Location[i].X + 5, Location[i].Y + 5))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if players have collided.
        /// </summary>
        /// <param name="players">The players in the current game.</param>
        /// <returns>True if players have collided. False otherwise.</returns>
        public bool checkEnemyCollision(Dictionary<int, Snake> players)
        {
            List<Rect> otherPlayerLocations = new List<Rect>();
            foreach (var enemy in players.Values)
            {
                if (enemy.ID != ID)
                {
                    for (int i = 0; i < enemy.Location.Count; i++)
                        otherPlayerLocations.Add(enemy.Location[i]);
                }
            }

            for (int i = 0; i < otherPlayerLocations.Count; i++)
            {
                if (Location[0].IntersectsWith(otherPlayerLocations[i]))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the player has intersected with food.
        /// </summary>
        /// <param name="foodLocation">The food location.</param>
        /// <returns>True is player has intersected with food. False otherwise.</returns>
        public bool checkFoodCollision(Rect foodLocation)
        {
            if (Location[0].IntersectsWith(foodLocation))
                return true;
            return false;
        }

    }
}
