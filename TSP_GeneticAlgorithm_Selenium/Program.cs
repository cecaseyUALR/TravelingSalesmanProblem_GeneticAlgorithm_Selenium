using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium.Support.UI;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace TSP_GeneticAlgorithm_Selenium
{
    class Program
    {
        public static void Main(string[] args)
        {
            System.IO.StreamReader file = new System.IO.StreamReader(@"C:\Users\Christian Casey\Desktop\cities.txt");
            List<List<string>> RoutesArray = RoutesTxtArray(file);

            Globals.ShortestTime = 999999;
            Globals.ShortestDist = 999999;
            Globals.NumGenerationsWithoutChange = 0;
            IWebDriver webDriver = new FirefoxDriver();

            RoutesArray = InitializePopulation(RoutesArray, webDriver);

            for (int i = 0; i < Globals.MaxNumGenerations; i++)
            {
                RoutesArray = SelectionAndCrossover(RoutesArray, webDriver);
                // Stop after this many generations have passed without change in best route
                if (Globals.NumGenerationsWithoutChange >= Globals.MaxNumGenerationsWithoutChange)
                {
                    Globals.NumGenerationsTillCompletion = i;
                    break;
                }
                Globals.NumGenerationsTillCompletion = Globals.MaxNumGenerations;
            }

            // Best route has been found, so display it for user
            NavigateToBestRoute(RoutesArray, webDriver);

            // create string of the best route
            string BestRoute = "";
            for (int j = 0; j < RoutesArray[0].Count(); j++)
            {
                if (j < RoutesArray[0].Count())
                    BestRoute += RoutesArray[0][j] + " --> ";
            }
            // Display best route and its info
            MessageBox.Show("The best route is: " + BestRoute + " \n \n Travel time: " + Globals.ShortestTime +
                "  hours. \n Total travel distance: " + Globals.ShortestDist + " miles. \n " +
                "Stopped after " + Globals.NumGenerationsTillCompletion + " generations.");
        }

        // Reads from text file, returns an array of random routes, each with same point of origin
        static List<List<string>> RoutesTxtArray(System.IO.StreamReader file)
        {
            string line;
            List<string> FileArray = new List<string>();
            while ((line = file.ReadLine()) != null)
            {
                FileArray.Add(line);
            }
            Globals.CityOfOrigin = FileArray[0];

            List<List<string>> routes = new List<List<string>>();
            for (int d = 0; d < Globals.NumPopulation; d++)
            {
                routes.Add(new List<string>());
                routes[d].Add(Globals.CityOfOrigin);
            } // This ensures that each routes starts at the same city of origin

            FileArray.RemoveAt(0);
            for (int j = 0; j < Globals.NumPopulation; j++)
            {
                FileArray = FileArray.OrderBy(a => Guid.NewGuid()).ToList(); // shuffle list
                for (int m = 0; m < FileArray.Count(); m++)
                {
                    routes[j].Add(FileArray[m]);
                }
            } // This loop causes a random initial population of routes

            // Add original city back to each route so that it navigates back to city of origin
            for (int k = 0; k < Globals.NumPopulation; k++) routes[k].Add(Globals.CityOfOrigin);

            return routes;
        }

        static List<List<string>> SelectionAndCrossover(List<List<string>> routesArray, IWebDriver driver)
        {
            // at this point the two routes at end of array (the slowest) are forgotten/no longer needed
            List<List<string>> ParentGenes = new List<List<string>>();
            for (int d = 0; d < 2; d++) { ParentGenes.Add(new List<string>()); } // initialize ParentsCopy
            for (int c = 0; c < 2; c++)
            {
                for (int d = 1; d < routesArray[0].Count() - 1; d++)
                {
                    ParentGenes[c].Add(routesArray[c][d]);
                }
            } // Parents' genes are now created (they consist of the two fastest routes without city of origin)

            // CHILD 1
            Random rnd = new Random();
            int numElements1 = rnd.Next(1, ParentGenes[0].Count() / 2 + 1);
            List<string> child1 = new List<string>();
            // Firstly, add random number of elements of parent 1
            for (int i = 0; i < numElements1; i++)
            {
                child1.Add(ParentGenes[0][i]);
            }
            int coinFlip = rnd.Next(int.MaxValue) % 2;
            /* Explanation of coinFlip:
                If 0, add in elements from second parent from its last index towards its first index
                If 1, add in elements from second parent from its first index towards its last index
                This allows for more diversity in children while still maintaining potentially ideal sub-routes
                i.e. If route A-->B-->C = 30 miles, then C-->B-->A = 30 miles as well
            */
            if (coinFlip == 0) // add in elements from parent 2, front to back
            {
                for (int j = 0; child1.Count() < ParentGenes[0].Count(); j++)
                {
                    if (!child1.Contains(ParentGenes[1][j]))
                    {
                        child1.Add(ParentGenes[1][j]);
                    }
                }
            }
            else // add in elements from parent 2, back to front
            {
                for (int j = ParentGenes[0].Count() - 1; child1.Count() < ParentGenes[0].Count(); j--)
                {
                    if (!child1.Contains(ParentGenes[1][j]))
                    {
                        child1.Add(ParentGenes[1][j]); // remainder of child is elements of parent 1
                    }
                }
            }
            // Now, do the same for child 2, except with the roles of parent 1 and parent 2 swapped

            // CHILD 2
            int numElements2 = rnd.Next(1, ParentGenes[0].Count() / 2 + 1);
            List<string> child2 = new List<string>();

            for (int a = 0; a < numElements2; a++)
            {
                child2.Add(ParentGenes[1][a]);
            }
            coinFlip = rnd.Next(int.MaxValue) % 2;
            if (coinFlip == 0) // add in elements from parent 1, front to back
            {
                for (int b = 0; child2.Count() < ParentGenes[0].Count(); b++)
                {
                    if (!child2.Contains(ParentGenes[0][b]))
                    {
                        child2.Add(ParentGenes[0][b]);
                    }
                }
            }
            else // add in elements from parent 1, back to front
            {
                for (int b = ParentGenes[0].Count() - 1; child2.Count() < ParentGenes[0].Count(); b--)
                {
                    if (!child2.Contains(ParentGenes[0][b]))
                    {
                        child2.Add(ParentGenes[0][b]); 
                    }
                }
            }

            coinFlip = rnd.Next(int.MaxValue) % 2;
            // if children are the same, guarantee mutation of one child
            if (child1 == child2) 
            {
                if (coinFlip == 0)
                child2 = ChanceOfMutation(child2, true);
                else
                child1 = ChanceOfMutation(child1, true);

            }
            // if potentially reaching local maxima, guarantee mutation of one child
            else if (Globals.NumGenerationsWithoutChange >= Globals.StopPotentialLocalMaximaNumber)
            {
                if (coinFlip == 0)
                    child2 = ChanceOfMutation(child2, true);
                else
                    child1 = ChanceOfMutation(child1, true);
            }
            else // leave it up to chance
            {
                child2 = ChanceOfMutation(child2, false);
                child1 = ChanceOfMutation(child1, false);
            }

            // Attach city of origin back to beginning and end of child routes
            child1.Insert(0, Globals.CityOfOrigin); child1.Add(Globals.CityOfOrigin);
            child2.Insert(0, Globals.CityOfOrigin); child2.Add(Globals.CityOfOrigin);

            List<List<string>> children = new List<List<string>>
            {
                child1,
                child2
            };

            // Rank newly created children
            children = FitnessFunction(children, driver);
            routesArray[2] = children[0];
            routesArray[3] = children[1];

            int[] Indexer = { 0, 1, 2, 3 };
            // Sort the population's distances (parents and children) with Indexer
            Array.Sort(Globals.PopulationDistances, Indexer);

            // Use Indexer to then sort the routes to match the order of their sorted distances
            return SortRoutesArrayFromIndexer(routesArray, Indexer);
        }

        // Chance to swap two randomly selected cities within child route
        // City of origin is not included in route when this function is called
        static List<string> ChanceOfMutation(List<string> child, bool GuaranteeMutation = false)
        {
            Random rnd = new Random();
            int chanceToMutate = rnd.Next(1, 5);
            int swapIndex1, swapIndex2;
            if (chanceToMutate == 2 || GuaranteeMutation == true)
            {
                swapIndex1 = rnd.Next(0, child.Count());
                int nonMatchingSwapIndex;
                do
                {
                    nonMatchingSwapIndex = rnd.Next(0, child.Count());
                } while (nonMatchingSwapIndex == swapIndex1);
                swapIndex2 = nonMatchingSwapIndex;
            }
            else return child;
            string temp = child[swapIndex1];
            child[swapIndex1] = child[swapIndex2];
            child[swapIndex2] = temp;
            return child;
        }

        static List<List<string>> InitializePopulation(List<List<string>> routesArray, IWebDriver driver)
        {
            return FitnessFunction(routesArray, driver, true);
        }

        static List<List<string>> FitnessFunction(List<List<string>> routesArray, IWebDriver driver, bool initializePop = false)
        {
            int[] Distances = GetRouteDistances(routesArray, driver).ToArray();
            int[] Indexer = new int[routesArray.Count()];
            for (int i = 0; i < routesArray.Count(); i++) Indexer[i] = i;

            Array.Sort(Distances, Indexer);
            if (initializePop == false)
            {
                if (!(Distances[0] < Globals.PopulationDistances[0]))
                {
                    Globals.NumGenerationsWithoutChange++;
                }
                else
                {
                    Globals.NumGenerationsWithoutChange = 0;
                }
                Globals.PopulationDistances[2] = Distances[0];
                Globals.PopulationDistances[3] = Distances[1];
            }
            else if (initializePop == true)
            {
                Globals.PopulationDistances[0] = Distances[0];
                Globals.PopulationDistances[1] = Distances[1];
            }

            return SortRoutesArrayFromIndexer(routesArray, Indexer);
        }

        static List<List<string>> SortRoutesArrayFromIndexer(List<List<string>> routesArray, int[] Indexer)
        {
            List<List<string>> copy = new List<List<string>>();
            for (int c = 0; c < routesArray.Count(); c++) copy.Add(routesArray[c]);
            int temp;
            for (int j = 0; j < routesArray.Count(); j++)
            {
                if (Indexer[j] != j)
                {
                    for (int k = 0; k < routesArray[0].Count(); k++)
                    {
                        routesArray[j][k] = copy[Indexer[j]][k];
                        routesArray[Indexer[j]][k] = copy[j][k];
                    }
                    temp = Indexer[j];
                    Indexer[j] = j;
                    Indexer[temp] = temp;
                }
            }
            return routesArray;
        }

        // at the end of program, display best route in maps
        static void NavigateToBestRoute(List<List<string>> RoutesArray, IWebDriver driver)
        {
            WaitForMilliseconds(3000);
            string url = "https://www.google.com/maps/dir/";
            for (int j = 0; j < RoutesArray[0].Count(); j++)
            {
                url = url + RoutesArray[0][j] + "/";
            }
            driver.Navigate().GoToUrl(url);
        }

        static List<int> GetRouteDistances(List<List<string>> routesArr, IWebDriver driver)
        {
            List<int> Distances = new List<int>();
            for (int i = 0; i < routesArr.Count(); i++)
            {
                string url = "https://www.google.com/maps/dir/";
                for (int j = 0; j < routesArr[0].Count(); j++)
                {
                    url = url + routesArr[i][j] + "/";
                }
                // sometimes after many web requests, it takes a while for url to be reached
                // so this failsafe is added to wait for 10 seconds before timeout failure
                try { driver.Navigate().GoToUrl(url);
                //IWebElement error = driver.FindElement(By.XPath("//div[@class='section-directions-error']"));
                }
                catch { WaitForMilliseconds(10000); driver.Navigate().GoToUrl(url); }
                int routeTime = GetTotalTravelTimeHours(driver);
                if (routeTime < Globals.ShortestTime)
                {
                    Globals.ShortestTime = routeTime;
                }

                int routeDistance = GetTotalDistanceMiles(driver);
                if (routeDistance < Globals.ShortestDist)
                {
                    Globals.ShortestDist = routeDistance;
                    Globals.ShortestDistURL = url;
                }

                Distances.Add(routeDistance);
            }
            return Distances;
        }

        static int GetTotalTravelTimeHours(IWebDriver driver)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
            wait.Until(ExpectedConditions.ElementToBeClickable(By.
                XPath("//button[@class='blue-link section-directions-action-button']")));

            // Retrives travel time in hours element from browser
            string distanceString = driver.FindElement(By
                .XPath("//div[@class='section-directions-trip-duration']")).GetAttribute("textContent");

            int x =
            Convert.ToInt32(
                Regex.Replace(
                    distanceString,
                    "[^0-9]", // Select everything that is not in the range of 0-9
                    ""        // Replace that with an empty string.
            ));
            return x;
        }

        static int GetTotalDistanceMiles(IWebDriver driver)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
            wait.Until(ExpectedConditions.ElementToBeClickable(By
                .XPath("//button[@class='blue-link section-directions-action-button']")));

            // Retrives travel distance element in miles from browser
            string distanceString = driver.FindElement(By
                .XPath("//div[@class='section-directions-trip-distance section-directions-trip-secondary-text']")).GetAttribute("textContent");

            int x =
            Convert.ToInt32(
                Regex.Replace(
                    distanceString,
                    "[^0-9]", // Select everything that is not in the range of 0-9
                    ""        // Replace that with an empty string.
            ));
            return x;
        }

        // Timing is dependent on connection speed at times, so this function helps keep
        // program from searching for value before it exists, causing failure
        static void WaitForMilliseconds(int milliseconds = 200)
        {
            System.Threading.Thread.Sleep(milliseconds);
        }

        public static class Globals
        {
            public static int[] PopulationDistances = new int[4];
            public static int ShortestTime;
            public static int ShortestDist;
            public static int NumGenerationsTillCompletion;
            public static int NumGenerationsWithoutChange;
            public const int MaxNumGenerationsWithoutChange = 10;
        // Purpose of StopPotentialLocalMaximaNumber:
        // once there have been about 70% of the max no. of generations without change, this variable
        // will trigger guaranteed mutation in a child routeto try combatting getting an answer at a local maxima
            public const int StopPotentialLocalMaximaNumber = (MaxNumGenerationsWithoutChange / 2) +
                ((MaxNumGenerationsWithoutChange / 2) / 2);
            public const int MaxNumGenerations = 50;
            public const int NumPopulation = 4;
            public static string ShortestDistURL;
            public static string CityOfOrigin;
        }
    }
}