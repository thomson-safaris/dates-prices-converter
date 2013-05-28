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
        private string configuration_path;
        private string extra_included_trips_for_status = "";
        private string extra_excluded_trips_for_status = "";
        private string last_included_trip = "";
        private string last_excluded_trip = "";


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

            // add a date format column to sort by date
            DataColumn dateForSort = table.Columns.Add("DateForSort", typeof(DateTime));
            dateForSort.AllowDBNull = true;

            // put the dates from the start date into it
            foreach (DataRow rowSort in table.Rows)
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
            DataView dv = table.DefaultView;
            dv.Sort = "DateForSort ASC";
            table = dv.ToTable();

            try
            {
                process_table(table, "KILI UM", 0, 0, path + @"\kilium_regular.txt");
                process_table(table, "KILI UM", 5, 2990, path + @"\kilium_five.txt");
                process_table(table, "KILI UM", 7, 3990, path + @"\kilium_seven.txt");
                process_table(table, "KILI LE", 0, 0, path + @"\kilile_regular.txt");
                process_table(table, "KILI LE", 5, 2990, path + @"\kilile_five.txt");
                process_table(table, "KILI LE", 7, 3990, path + @"\kilile_seven.txt");
                process_table(table, "KILI GT", 0, 0, path + @"\kiligt_regular.txt");
                process_table(table, "KILI GT", 5, 2990, path + @"\kiligt_five.txt");
                process_table(table, "KILI GT", 7, 3990, path + @"\kiligt_seven.txt");
                process_table(table, "SIG", 0, 0, path + @"\thomson_signature.txt");
                process_table(table, "TWS", 0, 0, path + @"\tanzania_wildlife.txt");
                process_table(table, "TTS", 0, 0, path + @"\trekking_safari.txt");
                process_table(table, "CULT", 0, 0, path + @"\wildlife_cultural.txt");
                process_table(table, "N&S", 0, 0, path + @"\north_and_south.txt");
                process_table(table, "BIGGS", 0, 0, path + @"\photography.txt");
                process_table(table, "TFS", 0, 0, path + @"\family.txt");
                process_table(table, "SFS", 0, 0, path + @"\short_family.txt");
                process_table(table, "TAS", 0, 0, path + @"\active_teens.txt");
                return true;
            }
            catch (Exception crap)
            {
                Console.WriteLine("Json export failed: " + crap);
                return false;
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


        private string create_notes(DataRow row, string custom_note)
        {
            try
            {
                string note = "";
                string links_note = "";
                double availableSpace = Convert.ToDouble(row["availableSpace"]);
                double numPax = Convert.ToDouble(row["numPax"]);
                double number_of_pax = Convert.ToDouble(row["number_of_pax"]);

                // check kili availability first
                if ((row["TripCode"].ToString().Substring(0, 7) == "KILI LE") || (row["TripCode"].ToString().Substring(0, 7) == "KILI UM"))
                {
                    if (number_of_pax >= 24)
                    {
                        note = @"Sold out - <a href='/contact-us' title='800-235-0289'>call</a> for new options";
                    }
                    else if (number_of_pax >= 6)   // kili um/le limit
                    {
                        note = "Limited Availability";
                    }
                }
                else if (row["TripCode"].ToString().Substring(0, 7) == "KILI GT")
                {
                    if (number_of_pax >= 8)
                    {
                        note = @"Sold out - <a href='/contact-us' title='800-235-0289'>call</a> for new options";
                    }
                    else if (number_of_pax >= 4)   // kili gt limit
                    {
                        note = "Limited Availability";
                    }
                }
                else // then check everything else
                {
                    if (availableSpace <= 0)
                    {
                        note = "Sold Out";
                    }
                    else if (numPax / (numPax + availableSpace) >= 0.4)   // limited availability if greater than 40% booked
                    {
                        note = "Limited Availability";
                    }
                }

                // for custom notes
                if ((custom_note != "") && (note != ""))
                {
                    note = custom_note + ", " + note;
                }
                else if (custom_note != "")
                {
                    note = custom_note;
                }

                // Biggs itineraries only
                if (row["TripCode"].ToString().Substring(0, 5) == "BIGGS")
                {
                    links_note = get_photo_safaris_links(row);
                    if ((note != "") && (links_note != ""))
                    {
                        note += ". " + links_note;
                    }
                    else if (links_note != "")
                    {
                        note = links_note;
                    }
                }
                return note;
            }
            catch (Exception crap)
            {
                Console.WriteLine("Could not convert either available space or num pax: " + crap);
                include_trip = false;
                return "";
            }
        }


        private string get_photo_safaris_links(DataRow row)
        {
            string html_tag_start = @"<a href='";
            string html_tag_end = @"'>Itinerary</a>";
            string path = configuration_path + "photo_links_data.csv";
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
                        if (r["TripName"].ToString() == row["TripCode"].ToString())
                        {
                            return html_tag_start + r["Link"].ToString() + html_tag_end;
                        }
                    }
                }
            }
            catch (Exception crap)
            {
                MessageBox.Show("Photo safari itinerary links file missing or invalid.  Please contact the developer.\n\nError message: " + crap);
            }
            return "";
        }


        private int get_soldout_status(string note)
        {
            if (note.Length >= 8)
            {
                if (note.Substring(0, 8).ToLower() == "sold out") { return 1; }
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



        private void process_table(DataTable table, string flag, int daysToAdd, int priceToAdd, string path)
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
            

            string sortOrder = "DateForSort ASC";
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            int i = 0;
            DataRow[] foundRows = table.Select(filter, sortOrder);

            foreach (DataRow row in foundRows)
            {
                include_trip = true;
                DateTime start_date = get_start_date(row);
                DateTime end_date = get_end_date(row, daysToAdd);
                string notes;
                string adult_price = get_adult_price(row, priceToAdd);
                string teen_price = get_teen_price(row, priceToAdd);
                string child_price = get_child_price(row, priceToAdd);
                if (((flag == "TFS") || (flag == "SFS")) && (child_price == ""))
                {
                    notes = create_notes(row, "Teen Trip");
                }
                else
                {
                    notes = create_notes(row, "");
                }
                int soldOut = get_soldout_status(notes);

                if (include_trip)
                {
                    sb.Append(@"""" + i + @""":{""depart_us"":""");
                    sb.Append(String.Format("{0:M/d/yyyy}", start_date) + @""",");
                    sb.Append(@"""return_us"":""");
                    sb.Append(String.Format("{0:M/d/yyyy}", end_date) + @""",");
                    sb.Append(@"""notes"":""");
                    sb.Append(notes);
                    sb.Append(@""",");
                    sb.Append(@"""adult_price"":""" + adult_price + @""",");
                    sb.Append(@"""teen_price"":""" + teen_price + @""",");
                    sb.Append(@"""child_price"":""" + child_price + @""",");
                    sb.Append(@"""soldout"":""" + soldOut.ToString() + @"""");
                    sb.Append(@"},");
                    i++; // increment i because it shows up in output!
                }
                
            }

            // remove the last comma
            sb.Remove(sb.Length - 1, 1);
            // add the final closing bracket
            sb.Append(@"}");

            JsonWriter(path, sb);
        }

        /// <summary>
        ///     Write a stringbuilder to the specified path
        /// </summary>
        /// <param name="path"></param>
        /// <param name="sb"></param>
        /// <returns></returns>
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

        /// <summary>
        ///     Export the provided table to a csv file of the user's choosing
        /// </summary>
        /// <param name="path"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        public bool toCSV(DataTable table)
        {
            StringBuilder sb = new StringBuilder();
            SaveFileDialog fileName = new SaveFileDialog();
            string fileType;
            bool successfulExport;

            GetColumnHeadersForCSV(sb, table);
            GetTableDataForCSV(sb, table);
            fileType = "csv";
            fileName = SaveFileAs(fileType);
            successfulExport = WriteToMyFile(fileName, sb);

            if (successfulExport) return true;
            else return false;
        }

        /// <summary>
        ///     WRITE TO MY FILE
        /// </summary>
        /// <param name="saveFileDialog1"></param>
        /// <param name="sb"></param>
        /// <returns>
        ///     Returns nothing, only writes a stringbuilder to a file
        /// </returns>
        private bool WriteToMyFile(SaveFileDialog saveFileDialog1, StringBuilder sb)
        {
            Stream myStream;
            try
            {
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    if ((myStream = saveFileDialog1.OpenFile()) != null)
                    {
                        System.Text.ASCIIEncoding encoder = new ASCIIEncoding();
                        myStream.Write(encoder.GetBytes(sb.ToString()), 0, sb.Length);
                        myStream.Close();
                    }
                    else return false;
                }
                else return false;
            }
            catch (Exception crap)
            {
                Console.WriteLine("Export failed: " + crap);
                return false;
            }
            return true;
        }

        /// <summary>
        ///     SAVE FILE AS
        /// </summary>
        /// <param name="fileType"></param>
        /// <returns>
        ///     Returns a SaveFileDialog with the selected filename to save as
        /// </returns>
        private SaveFileDialog SaveFileAs(string fileType)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = String.Format("All files (*.*)|*.*|{0} files (*.{0})|*.{0}",fileType);
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;

            return saveFileDialog1;
        }

        /// <summary>
        ///     GET COLUMN HEADERS FOR CSV    
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="table"></param>
        /// <returns>
        ///     Returns the column headers from the provided table in the provided stringbuilder
        ///     Returns the data in CSV format
        /// </returns>
        private StringBuilder GetColumnHeadersForCSV(StringBuilder sb, DataTable table)
        {
            bool firstCol = true;
            foreach (DataColumn c in table.Columns)
            {
                if (firstCol)
                {
                    firstCol = false;
                    sb.Append(c.ColumnName.ToString());
                }
                else
                {
                    sb.Append(@"," + c.ColumnName.ToString());
                }
            }
            sb.Append("\r\n");
            return sb;
        }

        /// <summary>
        ///     GET TABLE DATA FOR CSV
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="table"></param>
        /// <returns>
        ///     Returns the table data from the provided table in the provided stringbuilder
        ///     Returns the data in CSV format
        /// </returns>
        private StringBuilder GetTableDataForCSV(StringBuilder sb, DataTable table)
        {
            foreach (DataRow r in table.Rows)
            {
                bool firstColumn = true;
                foreach (DataColumn c in table.Columns)
                {
                    if (firstColumn)
                    {
                        firstColumn = false;
                        sb.Append(r[c].ToString());
                    }
                    else
                    {
                        sb.Append(@"," + r[c].ToString());
                    }
                }
                sb.Append("\r\n");
            }
            return sb;
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
