using System;
using System.Reflection;
using System.Xml;
using System.Xml.XPath;

namespace RenewableElectricityReports
{
    class Program
    {
        static XmlDocument xmlDoc = new XmlDocument();
        static string xmlFilePath = "renewable-electricity.xml";
        static string settingsFilePath = "report-settings.xml";
        static string finalXmlFilePath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))));
        static string selectedMenu = "";
        static int selectedCountryIndex = -1;
        static string selectedSourceType = "";
        static double selectedMinPercent = 0;
        static double selectedMaxPercent = 0;

        static void Main(string[] args)
        {
            LoadXmlData();
            LoadSettings();
            DisplayMenu();
        }

        static void LoadXmlData()
        {
            try
            {                
                xmlDoc.Load(Path.Combine(finalXmlFilePath, xmlFilePath));
                Console.WriteLine("XML data loaded.");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"File IO error: {ex.Message}");
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"XML parsing error: {ex.Message}");
            }
            catch (XPathException ex)
            {
                Console.WriteLine($"XPath error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
            }
        }

        static void DisplayMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Renewable Electricity Production in 2021");
                Console.WriteLine("========================================");
                Console.WriteLine("Enter 'C' to select a country");
                Console.WriteLine("Enter 'S' to select a specific source");
                Console.WriteLine("Enter 'P' to select a % range of renewables production");
                Console.WriteLine("Enter 'X' to quit");

                string choice = Console.ReadLine().ToUpper();
                switch (choice)
                {
                    case "C":
                        selectedMenu = "C";
                        SelectCountry();
                        break;
                    case "S":
                        selectedMenu = "S";
                        SelectSource();
                        break;
                    case "P":
                        selectedMenu = "P";
                        SelectPercentageRange();
                        break;
                    case "X":
                        SaveSettings();
                        return;
                    default:
                        Console.WriteLine("Invalid input. Please try again.");
                        break;
                }
            }
        }

        static void SelectCountry()
        {
            Console.WriteLine("Select a country by number.");
            XmlNodeList countries = xmlDoc.SelectNodes("/renewable-electricity/country");
            for (int i = 0; i < countries.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {countries[i].Attributes["name"].Value}");
            }

            int countryIndex = GetValidInput(1, countries.Count) - 1;
            selectedCountryIndex = countryIndex;
            GenerateCountryReport(countryIndex);
        }

        static void GenerateCountryReport(int countryIndex)
        {
            XmlNode countryNode = xmlDoc.SelectNodes("/renewable-electricity/country")[countryIndex];
            string countryName = countryNode.Attributes["name"].Value;

            Console.WriteLine($"Renewable Electricity Production in {countryName}");
            Console.WriteLine("------------------------------------------");

            XmlNodeList renewableSources = countryNode.SelectNodes("source");
            foreach (XmlNode source in renewableSources)
            {
                string type = source.Attributes["type"].Value;
                string amount = source.Attributes["amount"].Value;
                string percentTotal = source.Attributes["percent-of-all"].Value;
                string percentRenewables = source.Attributes["percent-of-renewables"].Value;

                Console.WriteLine($"{type,-20} {amount,10} {percentTotal,10} {percentRenewables,10}");
            }
            Console.WriteLine($"{renewableSources.Count} match(es) found.");
            Console.WriteLine("Press any key to return to the menu.");
            Console.ReadKey();
        }

        static void SelectSource()
        {
            Console.WriteLine("Select a renewable source by number.");
            XmlNodeList sources = xmlDoc.SelectNodes("/renewable-electricity/country/source");
            var sourceList = new List<string>();
            for (int i = 0; i < sources.Count; i++)
            {
                if (!sourceList.Any(t=>t== sources[i].Attributes["type"].Value))
                {
                    sourceList.Add(sources[i].Attributes["type"].Value);
                }
            }
            for (int i= 0;i < sourceList.Count();i++)
            {
                Console.WriteLine($"{i + 1}. {sourceList[i]}");
            }

            int sourceIndex = GetValidInput(1, sourceList.Count) - 1;
            selectedSourceType = sourceList[sourceIndex];
            GenerateSourceReport(sourceList[sourceIndex]);
        }

        static void GenerateSourceReport(string sourceType)
        {
            Console.WriteLine($"Electricity Production from {sourceType}");
            Console.WriteLine("------------------------------------------");

            XmlNodeList countries = xmlDoc.SelectNodes($"/renewable-electricity/country/source[@type='{sourceType}']");
            foreach (XmlNode country in countries)
            {
                string countryName = country.ParentNode.Attributes["name"].Value;
                string amount = country.Attributes["amount"].Value;
                string percentTotal = country.Attributes["percent-of-all"].Value;
                string percentRenewables = country.Attributes["percent-of-renewables"].Value;

                Console.WriteLine($"{countryName,-20} {amount,10} {percentTotal,10} {percentRenewables,10}");
            }
            Console.WriteLine($"{countries.Count} match(es) found.");
            Console.WriteLine("Press any key to return to the menu.");
            Console.ReadKey();
        }

        static void SelectPercentageRange()
        {
            Console.WriteLine("Enter the minimum % of renewables produced OR press enter for no minimum:");
            string minInput = Console.ReadLine();
            double minPercent = string.IsNullOrWhiteSpace(minInput) ? 0 : double.Parse(minInput);

            Console.WriteLine("Enter the maximum % of renewables produced OR press enter for no maximum:");
            string maxInput = Console.ReadLine();
            double maxPercent = string.IsNullOrWhiteSpace(maxInput) ? 100 : double.Parse(maxInput);

            selectedMinPercent = minPercent;
            selectedMaxPercent = maxPercent;
            GeneratePercentageRangeReport(minPercent, maxPercent);
        }

        static void GeneratePercentageRangeReport(double minPercent, double maxPercent)
        {
            Console.WriteLine($"Countries Where Renewables Account for {minPercent}% to {maxPercent}% of Electricity Generation");
            Console.WriteLine("---------------------------------------------------------------------------------");

            XmlNodeList totals = xmlDoc.SelectNodes("/renewable-electricity/country/totals");
            int matchCount = 0;

            foreach (XmlNode total in totals)
            {
                double totalElectricity = double.Parse(total.Attributes["all-sources"].Value);
                double renewableElectricity = double.Parse(total.Attributes["all-renewables"].Value);
                double percentRenewable = double.Parse(total.Attributes["renewable-percent"].Value);

                if (percentRenewable >= minPercent && percentRenewable <= maxPercent)
                {
                    string countryName = total.ParentNode.Attributes["name"].Value;
                    Console.WriteLine($"{countryName,-20} {totalElectricity,10:N0} {renewableElectricity,10:N0} {percentRenewable,10:F2}");
                    matchCount++;
                }
            }
            Console.WriteLine($"{matchCount} match(es) found.");
            Console.WriteLine("Press any key to return to the menu.");
            Console.ReadKey();
        }

        static int GetValidInput(int min, int max)
        {
            int input;
            while (true)
            {
                if (int.TryParse(Console.ReadLine(), out input) && input >= min && input <= max)
                {
                    break;
                }
                else
                {
                    Console.WriteLine($"Please enter a number between {min} and {max}.");
                }
            }
            return input;
        }

        static void SaveSettings()
        {
            try
            {
                XmlDocument settingsDoc = new XmlDocument();
                XmlElement root = settingsDoc.CreateElement("settings");

                XmlElement typeElement = settingsDoc.CreateElement("reportType");
                typeElement.InnerText = selectedMenu;
                root.AppendChild(typeElement);

                XmlElement countryElement = settingsDoc.CreateElement("countryIndex");
                countryElement.InnerText = selectedCountryIndex.ToString();
                root.AppendChild(countryElement);

                XmlElement sourceElement = settingsDoc.CreateElement("sourceType");
                sourceElement.InnerText = selectedSourceType;
                root.AppendChild(sourceElement);

                XmlElement minElement = settingsDoc.CreateElement("minPercent");
                minElement.InnerText = selectedMinPercent.ToString();
                root.AppendChild(minElement);

                XmlElement maxElement = settingsDoc.CreateElement("maxPercent");
                maxElement.InnerText = selectedMaxPercent.ToString();
                root.AppendChild(maxElement);

                settingsDoc.AppendChild(root);
                settingsDoc.Save(Path.Combine(finalXmlFilePath,settingsFilePath));
                Console.WriteLine("Settings saved.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        static void LoadSettings()
        {
            try
            {
                if (System.IO.File.Exists(settingsFilePath))
                {
                    XmlDocument settingsDoc = new XmlDocument();
                    settingsDoc.Load(Path.Combine(finalXmlFilePath,settingsFilePath));

                    selectedMenu = settingsDoc.SelectSingleNode("/settings/reportType").InnerText;
                    selectedCountryIndex = int.Parse(settingsDoc.SelectSingleNode("/settings/countryIndex").InnerText);
                    selectedSourceType = settingsDoc.SelectSingleNode("/settings/sourceType").InnerText;
                    selectedMinPercent = double.Parse(settingsDoc.SelectSingleNode("/settings/minPercent").InnerText);
                    selectedMaxPercent = double.Parse(settingsDoc.SelectSingleNode("/settings/maxPercent").InnerText);

                    switch (selectedMenu)
                    {
                        case "C":
                            GenerateCountryReport(selectedCountryIndex);
                            break;
                        case "S":
                            GenerateSourceReport(selectedSourceType);
                            break;
                        case "P":
                            GeneratePercentageRangeReport(selectedMinPercent,selectedMaxPercent);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }
        }
    }
}