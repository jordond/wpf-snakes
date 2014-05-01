using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.Serialization;

//Class Name: GameInformation
//Authors:    Jordon DeHoog & Jon Decher
//Date:       April 11, 2014
//Purpose:    Holds the properties for the game.

namespace SnakeLibrary
{
    [DataContract]
    public class GameInformation
    {
        [DataMember]
        public string[] Scoreboard { get; private set; }
        [DataMember]
        public string[] Messages { get; private set; }
        [DataMember]
        public Rect FoodLocation { get; private set; }

        public GameInformation(string[] s, string[] m, Rect l)
        {
            Scoreboard = s;
            Messages = m;
            FoodLocation = l;
        }
    }
}
