using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FiosChannelManager.DataObjects;
using FiosChannelManager.Controller;

namespace FiosChannelManager
{
    class FCMChannelLogoMenu: iFCMMenu
    {
        public bool GoBack {get; set;}
        public bool Quit { get; set; }

        private FiOSChannel _queryChannel;
        private FiOSChannel _selectedChannel;
        private ChannelLogoController _channelLogoCtrl;
        private readonly const string QUERYGO = "*";

        public FCMChannelLogoMenu()
        {
            this.GoBack = false;
            this.Quit = false;
        }

        public void SetPreviousMenu()
        {
            Program.PreviousMenu = new FCMMainMenu();
        }

        public void Display()
        {
            Console.Clear();
            Console.ResetColor();

            this._queryChannel = new FiOSChannel();
            this._channelLogoCtrl = new ChannelLogoController(Program.ChannelLogoRepository);


            Console.WriteLine("***Specify Channel Parameters***\n");
            Console.WriteLine("- If the field is not known, you may leave it blank and press enter.");
            Console.WriteLine("- To run query without specifying any more channels, simply type '*' as the value of the next parameter.");
            Console.WriteLine("- To cancel and go back to main menu, type 'q' as the value of the next parameter.");

            Console.Write("Region ID --> ");
            this._queryChannel.RegionId = Console.ReadLine();
            shouldRunQuery(this._queryChannel.RegionId);
            if (this.GoBack)
                return;

            while(true)
            {
                Console.Write("\nChannel # --> ");
                string val = Console.ReadLine();
                if (string.IsNullOrEmpty(val)) { break; }
                int chNum;
                if (int.TryParse(val, out chNum))
                {
                    this._queryChannel.ChannelPosition = chNum;
                    break;
                }
                else
                {
                    shouldRunQuery(val);
                    if (this.GoBack)
                        return;
                    Console.WriteLine("Invalid value specified. Please try again.");
                    continue;
                }
            }
        }

        private void shouldRunQuery(string userValue)
        {
            if (userValue == QUERYGO)
            {
                selectChannel(this._channelLogoCtrl.GetChannelQuery(this._queryChannel));
                return;
            }
            else if (userValue.ToUpper() == "Q")
            {
                this.GoBack = true;
            }
        }

        public object GetChoice()
        {
            this.GoBack = false;
            this.Quit = false;
            int attempts = 1;
            while (!(this.Quit || this.GoBack))
            {
                Console.Write("\n-->");
                string selection = Console.ReadLine();

                switch (selection.ToUpper())
                {
                    case "1":
                        return new FCMChannelLogoMenu();
                    case "B":
                        this.GoBack = true;
                        break;
                    case "Q":
                        this.Quit = true;
                        break;
                    default:
                        if (attempts == 5)
                        {
                            Console.WriteLine("Max allowed attempts. Please restart application and try again.");
                            this.Quit = true;
                        }
                        Console.WriteLine("Invalid Input, try again.");
                        attempts++;
                        break;
                }
            }
            return this;
        }

        private void selectChannel(IEnumerable<FiOSChannel> channels)
        {            
            int selection;

            while(true)
            {
                Console.Clear();
                Console.WriteLine("Please select which channel you would like to update (specify '0' to cancel): \n\n");

                for (int i = 0; i < channels.Count(); i++)
                {
                    Console.WriteLine("{0} :", i.ToString());
                    Console.WriteLine(channels.ElementAt(i).ToString());
                    Console.WriteLine("----------------------\n");
                }

                Console.Write("\n\n--> ");
                if (int.TryParse(Console.ReadLine(), out selection))
                {
                    if (selection == 0)
                    {
                        this._selectedChannel = null;
                        break;
                    }

                    try
                    {
                        this._selectedChannel = channels.ElementAt(selection);
                        break;
                    }
                    catch
                    {
                        Console.WriteLine("\nNo channel found at specified index. Please try again.");
                        continue;
                    }
                }
                else
                {
                    Console.WriteLine("Invalid character provided. Please try again.");
                    continue;
                }
            }
        }
    }
}
