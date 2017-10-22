using System;
using System.IO;
using Trinet.Networking;

/// <summary>
/// A console app to test the Share class.
/// </summary>
class SharesTest
{
	/// <summary>
	/// The main entry point for the application.
	/// </summary>
	[STAThread]
	static void Main(string[] args)
	{
		TestShares();
	}
	
	static void TestShares() 
	{
		// Enumerate shares on local computer
		Console.WriteLine("\nShares on local computer:");
		ShareCollection shi = ShareCollection.LocalShares;
		if (shi != null) 
		{
			foreach(Share si in shi) 
			{
				Console.WriteLine("{0}: {1} [{2}]", 
					si.ShareType, si, si.Path);

				// If this is a file-system share, try to
				// list the first five subfolders.
				// NB: If the share is on a removable device,
				// you could get "Not ready" or "Access denied"
				// exceptions.
				if (si.IsFileSystem) 
				{
					try 
					{
						DirectoryInfo d = si.Root;
						DirectoryInfo[] Flds = d.GetDirectories();
						for (int i=0; i < Flds.Length && i < 5; i++)
							Console.WriteLine("\t{0} - {1}", i, Flds[i].FullName);

						Console.WriteLine();
					}
					catch (Exception ex) 
					{
						Console.WriteLine("\tError listing {0}:\n\t{1}\n", 
							si, ex.Message);
					}
				}
			}
		}
		else
			Console.WriteLine("Unable to enumerate the local shares.");
		
		Console.WriteLine();
		
		// Enumerate shares on a remote computer
		Console.Write("Enter the NetBios name of a server on your network: ");
		string server = Console.ReadLine();
		
		if (server != null && server.Trim().Length > 0)
		{
			Console.WriteLine("\nShares on {0}:", server);
			shi = ShareCollection.GetShares(server);
			if (shi != null) 
			{
				foreach(Share si in shi) 
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
					{
						try 
						{
							System.IO.DirectoryInfo d = si.Root;
							System.IO.DirectoryInfo[] Flds = d.GetDirectories();
							for (int i=0; i<Flds.Length && i < 5; i++)
								Console.WriteLine("\t{0} - {1}", i, Flds[i].FullName);
							
							Console.WriteLine();
						}
						catch (Exception ex) 
						{
							Console.WriteLine("\tError listing {0}:\n\t{1}\n", 
								si.ToString(), ex.Message);
						}
					}
				}
			}
			else
				Console.WriteLine("Unable to enumerate the shares on {0}.\n"
					+ "Make sure the machine exists, and that you have permission to access it.",
					server);
			
			Console.WriteLine();
		}
		
		// Resolve local paths to UNC paths.
		string fileName = string.Empty;
		do
		{
			Console.Write("Enter the path to a file, or \"Q\" to exit: ");
			fileName = Console.ReadLine();
			if (fileName != null && fileName.Length > 0) 
			{
				if (fileName.ToUpper() == "Q") fileName = string.Empty;
				else
				{
					Console.WriteLine("{0} = {1}", fileName, ShareCollection.PathToUnc(fileName));
				}
			}
			
		} while (fileName != null && fileName.Length > 0);
	}
}
