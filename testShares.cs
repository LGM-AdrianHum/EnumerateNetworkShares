//  _______          __                       __    
//  \      \   _____/  |___  _  _____________|  | __
//  /   |   \_/ __ \   __\ \/ \/ /  _ \_  __ \  |/ /
// /    |    \  ___/|  |  \     (  <_> )  | \/    < 
// \____|__  /\___  >__|   \/\_/ \____/|__|  |__|_ \
//         \/     \/                              \/
//   _________.__                                   
//  /   _____/|  |__ _____ _______   ____   ______  
//  \_____  \ |  |  \\__  \\_  __ \_/ __ \ /  ___/  
//  /        \|   Y  \/ __ \|  | \/\  ___/ \___ \   
// /_______  /|___|  (____  /__|    \___  >____  >  
//         \/      \/     \/            \/     \/   
// File: EnumShares/EnumShares/testShares.cs
// User: Adrian Hum/
// 
// Created:  2017-10-22 11:51 PM
// Modified: 2017-10-23 12:18 AM

using System;

namespace EnumShares
{
    /// <summary>
    ///     A console app to test the Share class.
    /// </summary>
    internal class SharesTest
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        internal static void TestShares()
        {
            // Enumerate shares on local computer
            Console.WriteLine("\nShares on local computer:");
            var shi = ShareCollection.LocalShares;
            if (shi != null)
                foreach (Share si in shi)
                {
                    Console.WriteLine("{0}: {1} [{2}]",
                        si.ShareType, si, si.Path);

                    // If this is a file-system share, try to
                    // list the first five subfolders.
                    // NB: If the share is on a removable device,
                    // you could get "Not ready" or "Access denied"
                    // exceptions.
                    if (si.IsFileSystem)
                        try
                        {
                            var d = si.Root;
                            var flds = d.GetDirectories();
                            for (var i = 0; i < flds.Length && i < 5; i++)
                                Console.WriteLine("\t{0} - {1}", i, flds[i].FullName);

                            Console.WriteLine();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("\tError listing {0}:\n\t{1}\n",
                                si, ex.Message);
                        }
                }
            else
                Console.WriteLine("Unable to enumerate the local shares.");

            Console.WriteLine();

            // Enumerate shares on a remote computer
            Console.Write("Enter the NetBios name of a server on your network: ");
            var server = Console.ReadLine();

            if (server != null && server.Trim().Length > 0)
            {
                Console.WriteLine("\nShares on {0}:", server);
                shi = ShareCollection.GetShares(server);
                if (shi != null)
                    foreach (Share si in shi)
                    {
                        Console.WriteLine("{0}: {1} [{2}]",
                            si.ShareType, si, si.Path);

                        // If this is a file-system share, try to
                        // list the first five subfolders.
                        // NB: If the share is on a removable device,
                        // you could get "Not ready" or "Access denied"
                        // exceptions.
                        // If you don't have permissions to the share,
                        // you will get security exceptions.
                        if (si.IsFileSystem)
                            try
                            {
                                var d = si.Root;
                                var flds = d.GetDirectories();
                                for (var i = 0; i < flds.Length && i < 5; i++)
                                    Console.WriteLine("\t{0} - {1}", i, flds[i].FullName);

                                Console.WriteLine();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("\tError listing {0}:\n\t{1}\n",
                                    si, ex.Message);
                            }
                    }
                else
                    Console.WriteLine("Unable to enumerate the shares on {0}.\n"
                                      + "Make sure the machine exists, and that you have permission to access it.",
                        server);

                Console.WriteLine();
            }

            // Resolve local paths to UNC paths.
            string fileName;
            do
            {
                Console.Write("Enter the path to a file, or \"Q\" to exit: ");
                fileName = Console.ReadLine();
                if (string.IsNullOrEmpty(fileName)) continue;
                if (fileName.ToUpper() == "Q") fileName = string.Empty;
                else
                    Console.WriteLine("{0} = {1}", fileName, ShareCollection.PathToUnc(fileName));
            } while (!string.IsNullOrEmpty(fileName));
        }
    }
}