using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GenericParsing;
using System.IO;

namespace Converter
{
    public partial class MainForm : Form
    {
        static string configuration_path = Directory.GetParent(Directory.GetCurrentDirectory().ToString()).ToString() + "\\Configuration\\";
        string path = Directory.GetParent(Directory.GetCurrentDirectory().ToString()).ToString() + "\\writefiles\\";
        private Exporter ex = new Exporter(configuration_path);
        private FTPMgr ftp = new FTPMgr();
        private string remote_treks_directory = "treks-website/wp-content/uploads/";

        public MainForm()
        {
            InitializeComponent();
            convert_button.Click += new System.EventHandler(toJson_Click);
            treks_upload_button.Click += new System.EventHandler(treks_upload_Click);
        }

        private void treks_upload_Click(object sender, System.EventArgs e)
        {
            try
            {
                results_label.Text = "Uploading Umbwe...";
                ftp.upload(path + "kili-umbwe.csv", remote_treks_directory + "kili-umbwe.csv");
                results_label.Text = "Uploading Western Approach...";
                ftp.upload(path + "kili-western.csv", remote_treks_directory + "kili-western.csv");
                results_label.Text = "Uploading Grand Traverse...";
                ftp.upload(path + "kili-gt.csv", remote_treks_directory + "kili-gt.csv");

                results_label.Text = "Upload complete.";
                //MessageBox.Show("Upload succeeded");
            }
            catch (Exception crap)
            {
                results_label.Text = "Upload failed:\n" + crap;
                //MessageBox.Show("Upload failed:\n" + crap);
            }
        }

        /// <summary>
        ///     SEND TO JSON
        /// 
        ///     Method to send prices and dates into a new JSON file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toJson_Click(object sender, System.EventArgs e)
        {
            DataTable table = new DataTable();
            try
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        if (ofd.FileName.Substring(ofd.FileName.Length - 4, 4) == ".csv")
                        {
                            using (GenericParserAdapter parser = new GenericParserAdapter(ofd.FileName))
                            {
                                parser.ColumnDelimiter = ",".ToCharArray()[0];
                                parser.FirstRowHasHeader = true;

                                table = parser.GetDataTable();

                                table.Columns["n_SNITtrip"].ColumnName              = "TripID";
                                table.Columns["s_TripCode"].ColumnName              = "TripCode";
                                table.Columns["s_DestinationCode"].ColumnName       = "Destination";
                                table.Columns["s_TripType"].ColumnName              = "TripType";
                                table.Columns["d_Start"].ColumnName                 = "Start";
                                table.Columns["d_End"].ColumnName                   = "End";
                                table.Columns["d_Cancelled"].ColumnName             = "Cancelled";
                                table.Columns["cn_CntPax"].ColumnName               = "number_of_pax";     // Kili trip "limited" at 6 pax for LE/UM, 4 pax for GT
                                table.Columns["cn_CntRoomsSLD"].ColumnName          = "numPax";
                                table.Columns["PRItrpTRPorMA::n_A"].ColumnName      = "adultPrice";
                                table.Columns["PRItrpTRPorMA::n_T"].ColumnName      = "teenPrice";
                                table.Columns["PRItrpTRPorMA::n_C"].ColumnName      = "childPrice";
                                //table.Columns["cn_SpacesAvailable"].ColumnName    = "availableSpace";    // use rooms not "spaces"
                                table.Columns["cn_SumRoomsAvailable"].ColumnName    = "availableSpace"; 

                                //string path = Directory.GetParent(Directory.GetCurrentDirectory().ToString()).ToString() + "\\writefiles\\";
                                Directory.CreateDirectory(path);
                                bool JSONsuccess = ex.ToJson(path, table);

                                if (JSONsuccess) { MessageBox.Show("JSON export succeeded."); }
                                else { MessageBox.Show("JSON export failed."); }

                                
                                results_label.Text = ex.get_final_status();
                            }
                        }
                        else
                        {
                            MessageBox.Show("Please select a .csv file");
                            return;
                        }
                    }
                }
            }
            catch (Exception crap)
            {
                Console.WriteLine("parser failed: " + crap);
            }
        }

        private void results_label_Click(object sender, EventArgs e)
        {

        }
    }
}
