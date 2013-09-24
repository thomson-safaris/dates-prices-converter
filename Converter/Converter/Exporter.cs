using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using System.Windows.Forms;
using GenericParsing;

namespace Converter
{
    class Exporter
    {
        bool include_trip;
        bool sold_out;
        private string configuration_path;
        private string extra_included_trips_for_status = "";
        private string extra_excluded_trips_for_status = "";
        private string last_included_trip = "";
        private string last_excluded_trip = "";
        DataTable exporter_table = new DataTable();



        public Exporter(string path)
        {
            configuration_path = path;
        }


        public string get_final_status()
        {
            string status = "";
            if (extra_included_trips_for_status != "")
            {
                status += "Including additional trips: \n" + extra_included_trips_for_status + "\n";
            }
            status += "\n";
            if (extra_excluded_trips_for_status != "")
            {
                status += "Excluding trips: \n" + extra_excluded_trips_for_status + "\n";
            }

            return status;
        }


        public bool ToJson(string path, DataTable table)
        {
            DateTime dt = new DateTime();
            exporter_table = table;

            // add a date format column to sort by date
            DataColumn dateForSort = exporter_table.Columns.Add("DateForSort", typeof(DateTime));
            dateForSort.AllowDBNull = true;

            // put the dates from the start date into it
            foreach (DataRow rowSort in exporter_table.Rows)
            {
                if (rowSort["TripCode"].ToString().Length > 3)
                {
                    dt = Convert.ToDateTime(rowSort["Start"]).AddDays(1);
                    rowSort["DateForSort"] = dt;
                    if (rowSort["TripCode"].ToString().Substring(0, 5) == "S-SIG")
                    {
                        string super_sig_temp = rowSort["TripCode"].ToString().Substring(2, rowSort["TripCode"].ToString().Length - 2);
                        rowSort["TripCode"] = super_sig_temp;
                    }
                }
            }
            DataView dv = exporter_table.DefaultView;
            dv.Sort = "DateForSort ASC";
            exporter_table = dv.ToTable();

            try
            {
                process_table("KILI UM", 0, 0, path + @"\kilium_regular.txt");
                process_table("KILI UM", 5, 2990, path + @"\kilium_five.txt");
                process_table("KILI UM", 7, 3990, path + @"\kilium_seven.txt");
                process_table("KILI LE", 0, 0, path + @"\kilile_regular.txt");
                process_table("KILI LE", 5, 2990, path + @"\kilile_five.txt");
                process_table("KILI LE", 7, 3990, path + @"\kilile_seven.txt");
                process_table("KILI GT", 0, 0, path + @"\kiligt_regular.txt");
                process_table("KILI GT", 5, 2990, path + @"\kiligt_five.txt");
                process_table("KILI GT", 7, 3990, path + @"\kiligt_seven.txt");
                process_table("SIG", 0, 0, path + @"\thomson_signature.txt");
                process_table("TWS", 0, 0, path + @"\tanzania_wildlife.txt");
                process_table("TTS", 0, 0, path + @"\trekking_safari.txt");
                process_table("CULT", 0, 0, path + @"\wildlife_cultural.txt");
                process_table("N&S", 0, 0, path + @"\north_and_south.txt");
                process_table("BIGGS", 0, 0, path + @"\photography.txt");
                process_table("TFS", 0, 0, path + @"\family.txt");
                process_table("SFS", 0, 0, path + @"\short_family.txt");
                process_table("TAS", 0, 0, path + @"\active_teens.txt");
                process_table("WWS", 0, 0, path + @"\women_women_women.txt");
                process_table("KILI UM", 0, 0, path + @"\kili-umbwe.csv");
                process_table("KILI LE", 0, 0, path + @"\kili-western.csv");
                process_table("KILI GT", 0, 0, path + @"\kili-gt.csv");
                return true;
            }
            catch (Exception crap)
            {
                Console.WriteLine("Json export failed: " + crap);
                return false;
            }
        }


        private void process_table(string flag, int daysToAdd, int priceToAdd, string path)
        {
            string start_date, end_date, adult_price, teen_price, child_price, notes;
            DateTime new_season = new DateTime(2014, 4, 30, 0, 0, 0);
            DateTime start_date_date;
            int soldOut, start_season_comp, tempPriceToAdd;
            int i = 0;
            StringBuilder sb_json = new StringBuilder("{");
            StringBuilder sb_csv = new StringBuilder("Depart,Return from Trek Only,Return from Trek +5-Day Safari,Return from Trek +7-Day Safari,Notes\r\n");
            DateTime current_time = DateTime.Now;
            var last_trip_year = current_time.Year;
            sb_csv.Append(",,<span class=\"table-year\">" + last_trip_year.ToString() + "</span>,,\r\n");

            bool thomsontreks_export = false;
            if (path.Substring(path.Length-3,3) == "csv") 
            {
                thomsontreks_export = true;
            }

            foreach (DataRow row in exporter_table.Select(getFilter(flag), "DateForSort ASC"))
            {
                sold_out            = false; // this will be set to true if it's sold out
                include_trip        = true; // This flag may be set to false if the trip does not process favorably 
                start_date_date     = get_start_date(row);
                start_season_comp   = DateTime.Compare(new_season, start_date_date);
                tempPriceToAdd      = priceToAdd;
                if (flag.Length >= 4)
                {
                    if ((flag.Substring(0, 4) == "KILI") && (priceToAdd > 0) && (start_season_comp < 0))
                    {
                        tempPriceToAdd += 100;
                    }
                }

                start_date          = String.Format("{0:M/d/yyyy}", start_date_date);
                end_date            = String.Format("{0:M/d/yyyy}", get_end_date(row, daysToAdd));
                adult_price         = get_adult_price(row, tempPriceToAdd);
                teen_price          = get_teen_price(row, tempPriceToAdd);
                child_price         = get_child_price(row, tempPriceToAdd);
                notes               = create_notes(row, child_price, daysToAdd);
                soldOut             = get_soldout_status();

                if (include_trip)
                {
                    sb_json.Append(@"""" + i + @""":{");
                    sb_json.Append(@"""depart_us"":"""       + start_date + @""",");
                    sb_json.Append(@"""return_us"":"""       + end_date + @""",");
                    sb_json.Append(@"""notes"":"""           + notes + @""",");
                    sb_json.Append(@"""adult_price"":"""     + adult_price + @""",");
                    sb_json.Append(@"""teen_price"":"""      + teen_price + @""",");
                    sb_json.Append(@"""child_price"":"""     + child_price + @""",");
                    sb_json.Append(@"""soldout"":"""         + soldOut.ToString() + @"""");
                    sb_json.Append(@"},");
                    i++; // increasing count required for json format
                }

                // Handle thomsontreks.com export
                if (thomsontreks_export)
                {
                    if (last_trip_year < start_date_date.Year)
                    {
                        sb_csv.Append(",,<span class=\"table-year\">" + start_date_date.Year.ToString() + "</span>,,\r\n");
                    }
                    // Don't show trips past today's date
                    if (DateTime.Compare(start_date_date, current_time) > 0)
                    {
                        sb_csv.Append(start_date_date.ToString("MMM d") + ",");
                        sb_csv.Append(get_end_date(row, daysToAdd).ToString("MMM d") + ",");
                        sb_csv.Append(get_end_date(row, daysToAdd + 5).ToString("MMM d") + ",");
                        sb_csv.Append(get_end_date(row, daysToAdd + 7).ToString("MMM d") + ",");
                        sb_csv.Append(notes + "\r\n");

                        last_trip_year = start_date_date.Year;
                    }
                }
            }
            // remove the last comma and add the final closing bracket
            sb_json.Remove(sb_json.Length - 1, 1).Append("}");

            // Finish handling thomsontreks.com export
            if (thomsontreks_export)
            {
                // yep it works for csv too
                JsonWriter(path, sb_csv);
            }
            else
            {
                JsonWriter(path, sb_json);
            }
        }


        private DateTime get_start_date(DataRow row)
        {
            DateTime start_date = new DateTime();
            try
            {
                start_date = Convert.ToDateTime(row["Start"]).AddDays(-1);
            }
            catch (Exception crap)
            {
                Console.WriteLine("Date could not convert: " + crap);
                start_date = DateTime.Now;
                include_trip = false;
            }
            return start_date;
        }


        private DateTime get_end_date(DataRow row, int daysToAdd)
        {
            DateTime end_date = new DateTime();
            try
            {
                end_date = Convert.ToDateTime(row["End"]).AddDays(1 + daysToAdd);
            }
            catch (Exception crap)
            {
                Console.WriteLine("Date could not convert: " + crap);
                end_date = DateTime.Now;
                include_trip = false;
            }
            return end_date;
        }


        private string create_notes(DataRow row, string child_price, int days_to_add)
        {
            try
            {
                string note             = "";
                string custom_note      = "";
                string links_note       = "";
                string trip_code        = row["TripCode"].ToString();
                string extension_id     = trip_code.Substring(trip_code.Length - 3, 3);
                double availableSpace   = Convert.ToDouble(row["availableSpace"]);
                double numPax           = Convert.ToDouble(row["numPax"]);
                double number_of_pax    = Convert.ToDouble(row["number_of_pax"]);

                // Add custom teen trip text
                if (((trip_code.Substring(0, 3) == "TFS") || (trip_code.Substring(0, 3) == "SFS")) && (child_price == ""))
                {
                    custom_note += "Teen Trip";
                }

                // check kili availability first
                if (trip_code.Substring(0, 5) == "KILI ")
                {

                    if ((trip_code.Substring(0, 7) == "KILI LE") || (trip_code.Substring(0, 7) == "KILI UM"))
                    {
                        if (number_of_pax >= 24)
                        {
                            note += @"Sold out - <a href='/contact-us' title='800-235-0289'>call</a> for new options";
                        }
                        else if (number_of_pax >= 6)   // kili um/le limit
                        {
                            note += "Limited Availability";
                        }
                    }
                    else if (trip_code.Substring(0, 7) == "KILI GT")
                    {
                        if (number_of_pax >= 8)
                        {
                            note += @"Sold out - <a href='/contact-us' title='800-235-0289'>call</a> for new options";
                        }
                        else if (number_of_pax >= 4)   // kili gt limit
                        {
                            note += "Limited Availability";
                        }
                    }
                    if ((days_to_add > 0) && (note == "")) // check for extensions - but they're irrelevant if we already have a status for this kili
                    {
                        string extension_flag = "";
                        if (days_to_add == 5) { extension_flag = "KILIW "; } else { extension_flag = "KILIWC"; }
                        foreach (DataRow r in exporter_table.Select("TripCode LIKE '" + extension_flag + "*' AND TripType = 'Scheduled'", "DateForSort ASC"))
                        {
                            string ext_trip_code = r["TripCode"].ToString();
                            if ((extension_id == ext_trip_code.Substring(ext_trip_code.Length - 3, 3)) && (extension_flag == ext_trip_code.Substring(0,extension_flag.Length)))
                            {
                                double ext_availableSpace   = Convert.ToDouble(r["availableSpace"]);
                                double ext_numPax           = Convert.ToDouble(r["numPax"]);
                                if ((ext_availableSpace <= 0) || (ext_numPax / (ext_numPax + ext_availableSpace) >= 0.4))
                                {
                                    note += "Limited Availability";
                                }
                            }
                        }
                    }
                }
                else // then check everything else
                {
                    if (availableSpace <= 0)
                    {
                        note += "Sold Out";
                        sold_out = true;
                    }
                    else if (numPax / (numPax + availableSpace) >= 0.4)   // limited availability if greater than 40% booked
                    {
                        note += "Limited Availability";
                    }
                }

                // concatenate...
                if ((note != "") && (custom_note != "")) { note = custom_note + ", " + note; } else if (custom_note != "") { note = custom_note; }   

                // add custom links
                //if (trip_code.Substring(0, 5) == "BIGGS")
                //{
                if (trip_code.Substring(0, 4) == "KILI")
                {
                    trip_code = trip_code.Substring(0, 14);
                }
                links_note = get_custom_notes(trip_code);
                if ((note != "") && (links_note != ""))
                {
                    note += ". " + links_note;
                }
                else if (links_note != "")
                {
                    note = links_note;
                }
               // }
                return note;
            }
            catch (Exception crap)
            {
                Console.WriteLine("Could not convert either available space or num pax: " + crap);
                include_trip = false;
                return "";
            }
        }


        private string get_custom_notes(string trip_code)
        {
            string path = configuration_path + "custom_notes.csv";
            DataTable table = new DataTable();
            try
            {
                using (GenericParserAdapter parser = new GenericParserAdapter(path))
                {
                    parser.ColumnDelimiter = ",".ToCharArray()[0];
                    parser.FirstRowHasHeader = true;

                    table = parser.GetDataTable();
                    foreach (DataRow r in table.Rows)
                    {
                        if (r["Trip Name"].ToString() == trip_code)
                        {
                            if (r["Link"].ToString() != "")
                            {
                                return @"<a href='" + r["Link"].ToString() + @"'>" + r["Link Text"].ToString() + @"</a>";
                            }
                            else
                            {
                                return @"<strong>" + r["Link Text"].ToString() + @"</strong>";
                            }
                        }
                    }
                }
            }
            catch (Exception crap)
            {
                MessageBox.Show("Custom links file missing or invalid.  Please contact the developer.\n\nError message: " + crap);
            }
            return "";
        }


        private int get_soldout_status()
        {
            if (sold_out)
            {
                return 1;
            }
            return 0;
        }


        private string get_adult_price(DataRow row, int priceToAdd)
        {
            try
            {
                int price = Convert.ToInt32(row["adultPrice"]) + priceToAdd;
                return price.ToString("#,###");
            }
            catch (Exception crap)
            {
                Console.WriteLine("Adult price conversion failed: " + crap);
                include_trip = false;
                return "";
            }
        }


        private string get_teen_price(DataRow row, int priceToAdd)
        {
            try
            {
                if ((row["teenPrice"] != null) && (row["teenPrice"] != ""))
                {
                    int price = Convert.ToInt32(row["teenPrice"]) + priceToAdd;
                    return price.ToString("#,###");
                }
                else
                {
                    return "";
                }

            }
            catch (Exception crap)
            {
                Console.WriteLine("Teen price conversion failed: " + crap);
                return "";
            }
        }

        private string get_child_price(DataRow row, int priceToAdd)
        {
            try
            {
                if ((row["childPrice"] != null) && (row["childPrice"] != ""))
                {
                    int price = Convert.ToInt32(row["childPrice"]) + priceToAdd;
                    return price.ToString("#,###");
                }
                else
                {
                    return "";
                }
                
            }
            catch (Exception crap)
            {
                Console.WriteLine("Child price conversion failed: " + crap);
                return "";
            }
        }





        private string getFilter(string flag)
        {
            string filter;
            if (flag == "BIGGS")
            {
                filter = "TripCode LIKE '" + flag + "*'";
            }
            else
            {
                filter = "TripCode LIKE '" + flag + "*' AND TripType = 'Scheduled'";
            }
            filter = check_for_trips_to_exclude(filter, flag);
            filter = check_for_extra_trips_to_include(filter, flag);
            return filter;
        }


        public void JsonWriter(string path, StringBuilder sb)
        {
            try
            {
                using (StreamWriter outfile = new StreamWriter(path))
                {
                    outfile.Write(sb.ToString());
                }
            }
            catch (Exception crap)
            {
                Console.WriteLine("Writing to file failed: " + crap);
            }
        }

        private string check_for_extra_trips_to_include(string filter, string flag)
        {
            DataTable table = new DataTable();
            string path = configuration_path + "trips to include.csv";
            try
            {
                using (GenericParserAdapter parser = new GenericParserAdapter(path))
                {
                    parser.ColumnDelimiter = ",".ToCharArray()[0];
                    parser.FirstRowHasHeader = true;

                    table = parser.GetDataTable();
                    foreach (DataRow r in table.Rows)
                    {
                        if (r["Trip Code Type"].ToString() == flag)
                        {
                            filter += " OR TripCode LIKE '" + r["Trip Code"].ToString() + "*'";
                            if (last_included_trip != r["Trip Code"].ToString())
                            {
                                extra_included_trips_for_status += r["Trip Code"].ToString() + "\n";
                            }
                            last_included_trip = r["Trip Code"].ToString();
                        }
                    }
                }
            }
            catch (Exception crap)
            {
                MessageBox.Show("Trips to include file missing or invalid.  Please contact the developer.\n\nError message: " + crap);
            }

            return filter;
        }


        private string check_for_trips_to_exclude(string filter, string flag)
        {
            DataTable table = new DataTable();
            string path = configuration_path + "trips to exclude.csv";
            try
            {
                using (GenericParserAdapter parser = new GenericParserAdapter(path))
                {
                    parser.ColumnDelimiter = ",".ToCharArray()[0];
                    parser.FirstRowHasHeader = true;

                    table = parser.GetDataTable();
                    foreach (DataRow r in table.Rows)
                    {
                        if (r["Trip Code Type"].ToString() == flag)
                        {
                            filter += " AND TripCode NOT LIKE '" + r["Trip Code"].ToString() + "*'";
                            if (last_excluded_trip != r["Trip Code"].ToString())
                            {
                                extra_excluded_trips_for_status += r["Trip Code"].ToString() + "\n";
                            }
                            last_excluded_trip = r["Trip Code"].ToString();
                        }
                    }
                }
            }
            catch (Exception crap)
            {
                MessageBox.Show("Trips to exclude file missing or invalid.  Please contact the developer.\n\nError message: " + crap);
            }


            return filter;
        }
    }
}
