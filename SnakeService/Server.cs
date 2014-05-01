using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using SnakeLibrary;

//Class Name: Server
//Authors:    Jordon DeHoog & Jon Decher
//Date:       April 11, 2014
//Purpose:    Server for the snake game. 

namespace SnakeService
{
    class Server
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Service is activated, Press <Enter> to quit.");
                // Endpoint Address
                ServiceHost servHost = new ServiceHost(typeof(Game));

                // Start the service
                servHost.Open();

                // Keep the service running until <Enter> is pressed
                Console.ReadKey();

                // Shut down the service
                servHost.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadKey();
            }
        }
    }
}
