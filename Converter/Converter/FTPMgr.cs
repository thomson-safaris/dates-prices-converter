using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WinSCP;
using System.Data;
using System.Text.RegularExpressions;
using System.IO;

namespace Converter
{
    class FTPMgr
    {
        /// <summary>
        ///     Set up session options for WinSCP
        /// </summary>
        SessionOptions sessionOptions = new SessionOptions
        {
            Protocol = Protocol.Ftp,
            HostName = "192.237.253.205",
            UserName = "varftp",
            Password = "fjo0H#$Fnvo0w34hj",
            PortNumber = 21
        };

        /// <summary>
        ///     Default constructor
        /// </summary>
        public FTPMgr()
        {
            //
        }


        /// <summary>
        ///     Method to upload files
        /// </summary>
        public bool upload(string localLocation, string remoteLocation)
        {
            try
            {
                using (Session session = new Session())
                {
                    session.Open(sessionOptions);
                    TransferOptions transferOptions = new TransferOptions();
                    transferOptions.TransferMode = TransferMode.Automatic;
                    TransferOperationResult transferResult = session.PutFiles(localLocation, remoteLocation, false, transferOptions);

                    transferResult.Check();

                    foreach (TransferEventArgs transfer in transferResult.Transfers)
                    {
                        Console.WriteLine("Upload of {0} succeeded", transfer.FileName);
                    }
                    return true;
                }
            }
            catch (Exception SCPcrap)
            {
                Console.WriteLine("WinSCP upload failure: " + SCPcrap);
                return false;
            }
        }

        /// <summary>
        ///     Method to download files
        /// </summary>
        public bool download(string localLocation, string remoteLocation)
        {
            try
            {
                using (Session session = new Session())
                {
                    // open the session
                    session.Open(sessionOptions);

                    // upload your files
                    TransferOptions transferOptions = new TransferOptions();
                    transferOptions.TransferMode = TransferMode.Automatic;

                    TransferOperationResult transferResult = session.GetFiles(remoteLocation, localLocation, false, transferOptions);

                    transferResult.Check();

                    foreach (TransferEventArgs transfer in transferResult.Transfers)
                    {
                        Console.WriteLine("Download of {0} succeeded", transfer.FileName);
                    }
                    return true;
                }
            }
            catch (Exception SCPcrap)
            {
                Console.WriteLine("WinSCP download failure: " + SCPcrap);
                return false;
            }
        }

        /// <summary>
        ///     DELETE FILE ON REMOTE FTP DIRECTORY
        /// </summary>
        public bool delete(string remoteLocation)
        {
            try
            {
                using (Session session = new Session())
                {
                    session.Open(sessionOptions);

                    RemovalOperationResult removeResult = session.RemoveFiles(remoteLocation);
                    removeResult.Check();
                    return true;
                }
            }
            catch (Exception SCPcrap)
            {
                Console.WriteLine("WinSCP deletion failure: " + SCPcrap);
                return false;
            }

        }

    }
}
