﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;
using Microsoft.Win32;
using System.IO;
using SimsLib.FAR3;

namespace Mr.Shipper
{
    class Program
    {
        private static int[] m_RandomNumbers = new int[200];
        private static int m_RandomCounter = 0;

        static void Main(string[] args)
        {
            Random Rnd = new Random();
            m_RandomNumbers = Enumerable.Range(10000, 10200).OrderBy(i => Rnd.Next()).ToArray();

            //Find the path to TSO on the user's system.
            RegistryKey softwareKey = Registry.LocalMachine.OpenSubKey("SOFTWARE");
            if (Array.Exists(softwareKey.GetSubKeyNames(), delegate(string s) { return s.CompareTo("Maxis") == 0; }))
            {
                RegistryKey maxisKey = softwareKey.OpenSubKey("Maxis");
                if (Array.Exists(maxisKey.GetSubKeyNames(), delegate(string s) { return s.CompareTo("The Sims Online") == 0; }))
                {
                    RegistryKey tsoKey = maxisKey.OpenSubKey("The Sims Online");
                    string installDir = (string)tsoKey.GetValue("InstallDir");
                    installDir += "\\TSOClient\\";
                    GlobalSettings.Default.StartupPath = installDir;
                }
                else
                {
                    Console.WriteLine("Error TSO was not found on your system.");
                    Console.ReadLine();
                    return;
                }
            }
            else
            {
                Console.WriteLine("Error: No Maxis products were found on your system.");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("Generating uigraphics database...");
            GenerateUIGraphicsDatabase();
            Console.WriteLine("Done!");
            Console.WriteLine("Generating collections database...");

            m_RandomNumbers = Enumerable.Range(10200, 10400).OrderBy(i => Rnd.Next()).ToArray();

            GenerateCollectionsDatabase();
            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        /// <summary>
        /// Generates a database of the files in the uigraphics folder,
        /// as well as a *.cs file with an enumeration of the same files
        /// and their corresponding FileIDs.
        /// </summary>
        private static void GenerateUIGraphicsDatabase()
        {
            Dictionary<Far3Entry, string> UIEntries = new Dictionary<Far3Entry, string>();

            AddFilesFromDir(GlobalSettings.Default.StartupPath + "uigraphics\\", "", ref UIEntries);

            Directory.CreateDirectory("packingslips");
            StreamWriter Writer = new StreamWriter(File.Create("packingslips\\UIFileIDs.cs"));

            Writer.WriteLine("using System;");
            Writer.WriteLine("");
            Writer.WriteLine("namespace TSOClient");
            Writer.WriteLine("{");
            Writer.WriteLine("  //Generated by Mr. Shipper - filenames have been sanitized, and does not match");
            Writer.WriteLine("  //actual filenames character for character!");
            Writer.WriteLine("  partial class FileIDs");
            Writer.WriteLine("  {");
            Writer.WriteLine("      enum UIFileIDs");
            Writer.WriteLine("      {");

            int StopCounter = 0;
            foreach (KeyValuePair<Far3Entry, string> KVP in UIEntries)
            {
                StopCounter++;

                if (StopCounter < UIEntries.Count)
                {
                    Writer.WriteLine("          " + SanitizeFilename(KVP.Key.Filename) + " = 0x" + 
                        string.Format("{0:X}", KVP.Key.FileID + ","));
                }
                else
                {
                    Writer.WriteLine("          " + SanitizeFilename(KVP.Key.Filename) + " = 0x" +
                        string.Format("{0:X}", KVP.Key.FileID));
                }
            }

            Writer.WriteLine("      };");
            Writer.WriteLine("  }");
            Writer.WriteLine("}");
            Writer.Close();

            Writer = new StreamWriter(File.Create("packingslips\\uigraphics.xml"));
            Writer.WriteLine("<?xml version=\"1.0\"?>");
            Writer.WriteLine("<AssetList>");

            //For some really weird reason, "key" and "assetID" are written in reverse order...
            foreach (KeyValuePair<Far3Entry, string> KVP in UIEntries)
            {
                if (KVP.Value.Contains(".dat"))
                {
                    Writer.WriteLine("  " + "<DefineAssetString key=\"" + KVP.Value + 
                        "\" assetID=\"0x" + string.Format("{0:X}", KVP.Key.FileID) + "\"/>");
                }
                else
                {
                    DirectoryInfo DirInfo = new DirectoryInfo(KVP.Value);
                    Writer.WriteLine("  " + "<DefineAssetString key=\"" + DirInfo.Parent + "\\" +
                        Path.GetFileName(KVP.Value) + "\" assetID=\"0x" + 
                        string.Format("{0:X}", KVP.Key.FileID) + "\"/>");
                }
            }

            Writer.WriteLine("</AssetList>");
            Writer.Close();
        }

        private static void GenerateCollectionsDatabase()
        {
            Dictionary<Far3Entry, string> CollectionEntries = new Dictionary<Far3Entry, string>();

            AddFilesFromDir(GlobalSettings.Default.StartupPath + "avatardata\\bodies\\", "collections", ref CollectionEntries);
            AddFilesFromDir(GlobalSettings.Default.StartupPath + "avatardata\\heads\\", "collections", ref CollectionEntries);
            AddFilesFromDir(GlobalSettings.Default.StartupPath + "avatardata2\\bodies\\", "collections", ref CollectionEntries);
            AddFilesFromDir(GlobalSettings.Default.StartupPath + "avatardata2\\heads\\", "collections", ref CollectionEntries);
            AddFilesFromDir(GlobalSettings.Default.StartupPath + "avatardata3\\bodies\\", "collections", ref CollectionEntries);
            AddFilesFromDir(GlobalSettings.Default.StartupPath + "avatardata3\\heads\\", "collections", ref CollectionEntries);

            StreamWriter Writer = new StreamWriter(File.Create("packingslips\\CollectionsFileIDs.cs"));

            Writer.WriteLine("using System;");
            Writer.WriteLine("");
            Writer.WriteLine("namespace TSOClient");
            Writer.WriteLine("{");
            Writer.WriteLine("  //Generated by Mr. Shipper - filenames have been sanitized, and does not match");
            Writer.WriteLine("  //actual filenames character for character!");
            Writer.WriteLine("  partial class FileIDs");
            Writer.WriteLine("  {");
            Writer.WriteLine("      enum CollectionsFileIDs");
            Writer.WriteLine("      {");

            int StopCounter = 0;
            foreach (KeyValuePair<Far3Entry, string> KVP in CollectionEntries)
            {
                StopCounter++;

                if (StopCounter < CollectionEntries.Count)
                {
                    Writer.WriteLine("          " + SanitizeFilename(KVP.Key.Filename) + " = 0x" +
                        string.Format("{0:X}", KVP.Key.FileID + ","));
                }
                else
                {
                    Writer.WriteLine("          " + SanitizeFilename(KVP.Key.Filename) + " = 0x" +
                        string.Format("{0:X}", KVP.Key.FileID));
                }
            }

            Writer.WriteLine("      };");
            Writer.WriteLine("  }");
            Writer.WriteLine("}");
            Writer.Close();

            Writer = new StreamWriter(File.Create("packingslips\\collections.xml"));
            Writer.WriteLine("<?xml version=\"1.0\"?>");
            Writer.WriteLine("<AssetList>");

            //For some really weird reason, "key" and "assetID" are written in reverse order...
            foreach (KeyValuePair<Far3Entry, string> KVP in CollectionEntries)
            {
                if (KVP.Value.Contains(".dat"))
                {
                    Writer.WriteLine("  " + "<DefineAssetString key=\"" + KVP.Value +
                        "\" assetID=\"0x" + string.Format("{0:X}", KVP.Key.FileID) + "\"/>");
                }
                else
                {
                    DirectoryInfo DirInfo = new DirectoryInfo(KVP.Value);
                    Writer.WriteLine("  " + "<DefineAssetString key=\"" + DirInfo.Parent + "\\" +
                        Path.GetFileName(KVP.Value) + "\" assetID=\"0x" +
                        string.Format("{0:X}", KVP.Key.FileID) + "\"/>");
                }
            }

            Writer.WriteLine("</AssetList>");
            Writer.Close();
        }

        /// <summary>
        /// Adds files from a specified directory to a dictionary of entries.
        /// </summary>
        /// <param name="EntryDir">The directory to scan for entries.</param>
        /// <param name="Filetype">A fully qualified lowercase filetype to scan for (can be empty).</param>
        /// <param name="Entries">The Dictionary to add entries to.</param>
        private static void AddFilesFromDir(string EntryDir, string Filetype, ref Dictionary<Far3Entry, string> Entries)
        {
            string[] Dirs = Directory.GetDirectories(EntryDir);

            foreach(string Dir in Dirs)
            {
                if (Filetype != "")
                {
                    if (Dir.Contains(Filetype))
                    {
                        string[] Files = Directory.GetFiles(Dir);
                        string[] SubDirs = Directory.GetDirectories(Dir);
                        foreach (string Fle in Files)
                        {
                            if (Fle.Contains(".dat"))
                            {
                                FAR3Archive Archive = new FAR3Archive(Fle);

                                foreach (Far3Entry Entry in Archive.GetAllFAR3Entries())
                                    Entries.Add(Entry, Fle.Replace(GlobalSettings.Default.StartupPath, ""));
                            }
                            else
                            {
                                //This works for now, as there are always less than 100 unarchived files.
                                if (m_RandomCounter < 200)
                                    m_RandomCounter++;

                                Far3Entry Entry = new Far3Entry();
                                Entry.Filename = Fle.Replace(GlobalSettings.Default.StartupPath, "");
                                Entry.FileID = (uint)m_RandomNumbers[m_RandomCounter];

                                CheckCollision(Entry.FileID, Entries);

                                Entries.Add(Entry, Entry.Filename);
                            }
                        }

                        foreach (string SubDir in SubDirs)
                        {
                            Files = Directory.GetFiles(SubDir);
                            foreach (string SubFle in Files)
                            {
                                if (SubFle.Contains(".dat"))
                                {
                                    FAR3Archive Archive = new FAR3Archive(SubFle);

                                    foreach (Far3Entry Entry in Archive.GetAllFAR3Entries())
                                        Entries.Add(Entry, SubFle.Replace(GlobalSettings.Default.StartupPath, ""));
                                }
                                else
                                {
                                    //This works for now, as there are always less than 100 unarchived files.
                                    if (m_RandomCounter < 200)
                                        m_RandomCounter++;

                                    Far3Entry Entry = new Far3Entry();
                                    Entry.Filename = SubFle.Replace(GlobalSettings.Default.StartupPath, "");
                                    Entry.FileID = (uint)m_RandomNumbers[m_RandomCounter];

                                    CheckCollision(Entry.FileID, Entries);

                                    Entries.Add(Entry, Entry.Filename);
                                }
                            }
                        }
                    }
                }
                else //Filetype was empty, so just add all filetypes found...
                {
                    string[] Files = Directory.GetFiles(Dir);
                    string[] SubDirs = Directory.GetDirectories(Dir);
                    foreach (string Fle in Files)
                    {
                        if (Fle.Contains(".dat"))
                        {
                            FAR3Archive Archive = new FAR3Archive(Fle);

                            foreach (Far3Entry Entry in Archive.GetAllFAR3Entries())
                                Entries.Add(Entry, Fle.Replace(GlobalSettings.Default.StartupPath, ""));
                        }
                        else
                        {
                            //This works for now, as there are always less than 100 unarchived files.
                            if (m_RandomCounter < 200)
                                m_RandomCounter++;

                            Far3Entry Entry = new Far3Entry();
                            Entry.Filename = Fle.Replace(GlobalSettings.Default.StartupPath, "");
                            Entry.FileID = (uint)m_RandomNumbers[m_RandomCounter];

                            CheckCollision(Entry.FileID, Entries);

                            Entries.Add(Entry, Entry.Filename);
                        }
                    }

                    foreach (string SubDir in SubDirs)
                    {
                        Files = Directory.GetFiles(SubDir);
                        foreach (string SubFle in Files)
                        {
                            if (SubFle.Contains(".dat"))
                            {
                                FAR3Archive Archive = new FAR3Archive(SubFle);

                                foreach (Far3Entry Entry in Archive.GetAllFAR3Entries())
                                    Entries.Add(Entry, SubFle.Replace(GlobalSettings.Default.StartupPath, ""));
                            }
                            else
                            {
                                //This works for now, as there are always less than 100 unarchived files.
                                if (m_RandomCounter < 200)
                                    m_RandomCounter++;

                                Far3Entry Entry = new Far3Entry();
                                Entry.Filename = SubFle.Replace(GlobalSettings.Default.StartupPath, "");
                                Entry.FileID = (uint)m_RandomNumbers[m_RandomCounter];

                                CheckCollision(Entry.FileID, Entries);

                                Entries.Add(Entry, Entry.Filename);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks for collisions between existing and generated IDs, and prints out if any were found.
        /// </summary>
        /// <param name="FileID">The generated ID to check.</param>
        /// <param name="UIEntries">The entries to check.</param>
        /// <returns>True if any collisions were found.</returns>
        private static bool CheckCollision(uint FileID, Dictionary<Far3Entry, string> UIEntries)
        {
            foreach(KeyValuePair<Far3Entry, string> KVP in UIEntries)
            {
                if (KVP.Key.FileID == FileID)
                {
                    Console.WriteLine("Found ID collision: " + FileID);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sanitize's a file's name so it can be used in an C# enumeration.
        /// </summary>
        /// <param name="Filename">The name to sanitize.</param>
        /// <returns>The sanitized filename.</returns>
        private static string SanitizeFilename(string Filename)
        {
            return Filename.Replace(".bmp", "").Replace(".tga", "").
                        Replace("'", "").Replace("-", "_").Replace(".ttf", "").Replace(".wve", "").
                        Replace(".png", "").Replace(" ", "_").Replace("1024_768frame", "_1024_768frame").
                        Replace(".anim", "").Replace(".mesh", "").Replace(".skel", "").Replace(".col", "");
        }
    }
}
