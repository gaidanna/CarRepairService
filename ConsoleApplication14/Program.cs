using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace CarService
{
    class Program
    {
        public const byte CARPERSHIFT = 10;
        public const int MINTIME = 10000;
        public const int MAXTIME = 20000;
        public const int CARRANDOMNUMBER = 999999;
        public const string COMPLETEDSHIFT = "{0}'s shift is over.";
        public const string HANDLINGTIME = "Time for car repair - {0} sec";
        public const string MECHANICGOTCAR = "Mechanic {0}, got it - {1}, {2} one, number - {3}";
        public const string NEWCARARRIVED = "\r\n'New car arrived!'";
        public const string NOWORKERS = "No free mechanics.";
        public const string CLIENTNOTIFICATION = "\r\nCar- {0}, number - {1} finished handling";
        public const string MECHANICAGAINFREE = "\r\nMechanic {0}, free again.";
        public static readonly string[] CARS = new string[] { "Toyota", "Honda", "Opel", "Audi", "Mercedes" };
        public static readonly string[] COLORS = new string[] { "yellow", "blue", "white", "red", "green", "grey" };
        public static readonly List<Client> CLIENTS = new List<Client> { new Client(), new Client(), new Client(), new Client(), new Client() };

        public class NewCarEventArgs : EventArgs
        {
            public NewCarEventArgs(string model, string color, int number)
            {
                Model = model;
                Color = color;
                Number = number;
            }

            public string Model
            {
                get;
                private set;
            }

            public string Color
            {
                get;
                private set;
            }

            public int Number
            {
                get;
                private set;
            }

            public bool Handled
            {
                get;
                set;
            }
        }

        public class Client
        {
            Random rand = new Random();

            public Client()
            { }

            public event EventHandler<NewCarEventArgs> NewArrival;

            public int Id
            {
                get;
                set;
            }

            public bool IsOnService
            {
                get;
                private set;
            }

            public virtual void OnNewArrival(NewCarEventArgs e)
            {
                NewArrival(this, e);
            }

            public void OnWorkStarted(object sender, EventArgs e)
            {
                (sender as Service).CarStarted -= OnWorkStarted;   
                IsOnService = true;
            }

            public void OnWorkFinished(object sender, NewCarEventArgs e)
            {
                if (e.Number == Id)
                {
                    Console.WriteLine(CLIENTNOTIFICATION, e.Model, e.Number);
                    IsOnService = false;
                    (sender as Service).CarDone -= OnWorkFinished;
                }
            }
        }

        public static bool SimulateNewClient(string model, string color, int number)
        {
            NewCarEventArgs args;

            args = new NewCarEventArgs(model, color, number);
            CLIENTS.Find(x => x.Id == number).OnNewArrival( args);

            return args.Handled;
        }

        public class Service
        {
            public event EventHandler<NewCarEventArgs> NewCarArrived;
            public event EventHandler<NewCarEventArgs> CarStarted;
            public event EventHandler<NewCarEventArgs> CarDone;

            protected virtual void OnCarStarted(NewCarEventArgs e)
            {
                CarStarted(this, e);
            }

            protected virtual void OnCarDone(NewCarEventArgs e)
            {
                CarDone(this, e);
            }

            protected virtual void OnServiceNewCarArrived(NewCarEventArgs e)
            {
                NewCarArrived(this, e);
            }

            public void ServiceNewCar(object sender, NewCarEventArgs e)
            {
                OnServiceNewCarArrived(e);
            }

            public void NotificationOfRepairStart(object sender, NewCarEventArgs e)
            {
                Console.WriteLine(HANDLINGTIME, Math.Round(TimeSpan.FromMilliseconds((sender as Mechanic).HandlingTime).TotalSeconds, 2).ToString());

                OnCarStarted(e);
            }

            public void NotificationOfRepairEnd(object sender, NewCarEventArgs e)
            {
                OnCarDone(e);
            }
        }

        public class Mechanic
        {
            private bool free = true;
            private byte ProcessedCars = 0;

            public event EventHandler<NewCarEventArgs> RepairStart;
            public event EventHandler<NewCarEventArgs> RepairEnd;

            public Mechanic(string name)
            {
                Name = name;
            }

            public string Name
            {
                get;
                private set;
            }
            public double HandlingTime
            {
                get;
                private set;
            }

            protected virtual void OnRepairStart(NewCarEventArgs e)
            {
                RepairStart(this, e);
            }

            protected virtual void OnRepairEnd(NewCarEventArgs e)
            {
                RepairEnd(this, e);
            }

            public void GotNewCar(object sender, NewCarEventArgs e)
            {
                Random random;
                Timer timer;

                if (!e.Handled && free)
                {
                    random = new Random();

                    e.Handled = true;
                    free = false;
                    ProcessedCars++;

                    timer = new Timer(random.Next(MINTIME, MAXTIME));
                    HandlingTime = timer.Interval;
                    OnRepairStart(e);

                    Console.WriteLine(MECHANICGOTCAR, Name, e.Model, e.Color, e.Number);

                    timer.Elapsed += new ElapsedEventHandler((sender1, b) => OnCarProcessed(sender1, b, e));

                    timer.Enabled = true;
                    timer.AutoReset = false;
                }
            }

            private void OnCarProcessed(object sender, ElapsedEventArgs a, NewCarEventArgs e)
            {
                OnRepairEnd(e);

                if (ProcessedCars < CARPERSHIFT)
                {
                    Console.WriteLine(MECHANICAGAINFREE, Name);

                    free = true;
                }
                else
                {
                    Console.WriteLine(COMPLETEDSHIFT, Name);
                }
            }
        }

        static void Main(string[] args)
        {
            List<string> MechanicsNames;
            List<Mechanic> Mechanics;
            Timer timerForCar;
            Service service;

            timerForCar = new Timer();
            service = new Service();
            MechanicsNames = new List<string>();
            Mechanics = new List<Mechanic>();

            MechanicsNames.Add("Fred");
            MechanicsNames.Add("Alan");
            MechanicsNames.Add("Bob");

            for (int a = 0; a < MechanicsNames.Count; a++)
            {
                Mechanics.Add(new Mechanic(MechanicsNames[a]));
                service.NewCarArrived += Mechanics[a].GotNewCar;
                Mechanics[a].RepairStart += service.NotificationOfRepairStart;
                Mechanics[a].RepairEnd += service.NotificationOfRepairEnd;
            }

            timerForCar.Elapsed += new ElapsedEventHandler((sender, e) => timerForCarElapsed(sender, e, service));

            timerForCar.Interval = 5000;
            timerForCar.Enabled = true;
            Console.ReadLine();
        }

        public static void timerForCarElapsed(object sender, EventArgs e, Service service)
        {
            bool handledCar;
            Random r;

            r = new Random();

            int i = CLIENTS.FindIndex(item => item.IsOnService == false);
            if (i != -1)
            {
                Console.WriteLine(NEWCARARRIVED);

                CLIENTS[i].NewArrival += service.ServiceNewCar;
                service.CarStarted += CLIENTS[i].OnWorkStarted;
                service.CarDone += CLIENTS[i].OnWorkFinished;

                CLIENTS[i].Id = r.Next(CARRANDOMNUMBER);

                handledCar = SimulateNewClient(CARS[r.Next(CARS.Length)], COLORS[r.Next(COLORS.Length)], CLIENTS[i].Id);

                if (!handledCar)
                {
                    Console.WriteLine(NOWORKERS);
                }
            }
        }
    }
}