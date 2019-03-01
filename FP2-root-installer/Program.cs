using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace FP2_root_installer
{
    internal class Program
    {
        static string savefolder;

        private const string autobuildserv = "https://fp2.retsifp.de/autobuild/";
        private const string fp2downloadhelppage = "https://support.fairphone.com/hc/en-us/articles/213290023-Fairphone-OS-downloads-for-Fairphone-2";

        static void Main(string[] args)
        {
            savefolder = Path.Combine(Path.GetTempPath(), "fp2-root\\");
            Directory.CreateDirectory(savefolder);

            Console.WriteLine("Welcome to FairPhone2 root updater V1");
            Console.WriteLine("Emerick Herve - 2019");
            string manuals = "";
            while (manuals != "a" && manuals != "m" && manuals != "k") {
                Console.WriteLine("Do you want to auto-check for last root version available or manually open the boot file? Enter a (auto), m (manual) or k (keep last downloaded file)");
                manuals = Console.ReadLine();
            }

            string filepath = "";

            if (manuals == "a")
            {
                List<string> versions = getLinks(autobuildserv, "fp2-gms-", autobuildserv, true);

                if (versions.Count > 0)
                {
                    int selv = enterNumber("Enter version #:", versions.Count) - 1;
                    List<string> imgfiles = getLinks(autobuildserv + versions[selv], ".img", autobuildserv + versions[selv] + "/", true);

                    if (imgfiles.Count > 0)
                    {
                        int selimg = enterNumber("Enter img #:", imgfiles.Count) - 1;

                        string url = autobuildserv + versions[selv] + "/" + imgfiles[selimg];
                        filepath = downloadFile(url, "boot.img");
                    }
                    else
                    {
                        errorAndExit("not any img file has been found, please download manually the boot file and restart this programm selecting manual mode.");
                    }
                }
                else
                {
                    errorAndExit("not any version has been found, please download manually the boot file and restart this programm selecting manual mode.");
                }

            }
            else if(manuals == "m")
            {
                Console.WriteLine("Enter path:");
                filepath = Console.ReadLine();
            }
            else
            {
                filepath = Path.Combine(Path.GetTempPath(), "fp2-root\\", "boot.img");
            }


            string manualu = "";
            while (manualu != "a" && manualu != "m" && manualu != "k")
            {
                Console.WriteLine("Do you want to automaticaly check for available versions manually open the boot file? Enter a (auto), m (manual) or k (keep last downloaded file)");
                manualu = Console.ReadLine();
            }

            string updatepath = "";

            if (manualu == "a")
            {
                List<string> updates = getLinks(fp2downloadhelppage, "-manual.zip");
                int selupdate = enterNumber("Enter update link #:", updates.Count) - 1;

                updatepath = downloadFile(updates[selupdate], "update.zip");
            }
            else if (manualu == "m")
            {
                Console.WriteLine("Enter path:");
                updatepath = Console.ReadLine();
            }
            else
            {
                updatepath = Path.Combine(Path.GetTempPath(), "fp2-root\\", "update.zip");
            }

            bool retry = true;
            string updatefolder = Path.Combine(savefolder, "update\\");
            while (retry)
            {
                try
                {
                    Console.WriteLine("Unziping update...");
                    if (Directory.Exists(updatefolder))
                    {
                        Directory.Delete(updatefolder, true);
                    }
                    Directory.CreateDirectory(updatefolder);
                    ZipFile.ExtractToDirectory(updatepath, updatefolder);
                    Console.WriteLine("Done!");

                    retry = false;
                }
                catch (Exception ex)
                {
                    errorAndExit(ex.Message, true);
                }
            }

            retry = true;

            while (retry)
            {
                try
                {
                    if (File.Exists(updatefolder + "images\\boot-orig.img"))
                        File.Delete(updatefolder + "images\\boot-orig.img");
                    Console.WriteLine("Moving original boot...");
                    File.Move(updatefolder + "images\\boot.img", updatefolder + "images\\boot-orig.img");
                    Console.WriteLine("Done!");

                    Console.WriteLine("Copying new boot file...");
                    File.Copy(savefolder + "boot.img", updatefolder + "images\\boot.img");
                    Console.WriteLine("Done!");

                    Console.WriteLine("Removing SHA256 and MD5 checksums...");
                    deleteLineFromFile(updatefolder + "images\\MD5SUMS", "*boot.img");
                    deleteLineFromFile(updatefolder + "images\\SHA256SUMS", "*boot.img");
                    Console.WriteLine("Done!");

                    Console.WriteLine("Select your script");
                    int i = 0;
                    List<string> scripts = new List<string>();
                    foreach (string file in Directory.GetFiles(updatefolder))
                    {
                        if (file.Contains("flash-"))
                        {
                            scripts.Add(file);
                            i++;
                            Console.WriteLine(" #" + i + ": " + file);
                        }
                    }

                    int selscript = enterNumber("Enter script #:", scripts.Count) - 1;

                    ProcessStartInfo sinfo = new ProcessStartInfo(scripts[selscript]);
                    sinfo.WorkingDirectory = updatefolder;
                    Process p = Process.Start(sinfo);

                    p.WaitForExit();

                    retry = false;

                    exitApp();
                }
                catch (Exception ex)
                {
                    errorAndExit(ex.Message, true);
                }
            }

            Console.ReadLine();
        }

        static void exitApp()
        {
            string deltemp = "";
            while (deltemp != "y" && deltemp != "n")
            {
                Console.WriteLine("Delete temporary files ? (Recommended if install is sucessfull and you will not need to update another file). Enter y or n:");
                deltemp = Console.ReadLine();
            }

            if (deltemp == "y")
            {
                Directory.Delete(savefolder, true);
            }

            Environment.Exit(0);
        }

        static void deleteLineFromFile(string file, string delete)
        {
            string[] Lines = File.ReadAllLines(file);
            File.Delete(file);// Deleting the file
            using (StreamWriter sw = File.AppendText(file))

            {
                foreach (string line in Lines)
                {
                    if (line.Contains(delete))
                    {
                        //Skip the line
                        continue;
                    }
                    else
                    {
                        sw.WriteLine(line);
                    }
                }
            }
        }

        static string downloadFile(string url, string filename)
        {
            try
            {
                WebClient myWebClient = new WebClient();
                Console.WriteLine("Downloading file \"{0}\"...", url);

                string path = Path.Combine(savefolder, filename);
                myWebClient.DownloadFile(url, path);
                Console.WriteLine("Saved to {0}!", path);
                return path;
            }
            catch (Exception ex)
            {
                errorAndExit(ex.Message);
                return ""; // never used
            }
        }

        static int enterNumber(string question, int max)
        {
            int sel = 0;
            bool ok = false;
            while (!ok)
            {
                Console.WriteLine(question);
                try
                {
                    sel = Convert.ToInt32(Console.ReadLine());
                    if (sel <= max && sel >= 0)
                    {
                        ok = true;
                    }
                }
                catch (Exception)
                {
                    ok = false; // maybe not necessary
                }
            }
            return sel;
        }

        static void errorAndExit(string error, bool retry = false)
        {
            Console.WriteLine("Error: " + error);
            if (!retry)
            {
                Console.WriteLine("Press enter to exit");
                Console.ReadLine();
                exitApp();
            }
            else
            {
                Console.WriteLine("Press enter to retry");
                Console.ReadLine();
            }
        }

        static List<string> getLinks(string searchurl, string filter, string removetolink = "", bool replace = false)
        {
            HtmlWeb hw = new HtmlWeb();
            HtmlDocument doc = hw.Load(searchurl);
            Console.WriteLine();
            Console.WriteLine("Found versions:");

            List<string> versions = new List<string>();
            int i = 0;
            foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
            {
                string url = link.GetAttributeValue("href", null);
                if (url.Contains(filter))
                {
                    string version = url;
                    if (replace)
                        version = version.Replace(removetolink, "").Replace("/", "");
                    if (!versions.Contains(version))
                    {
                        i++;
                        Console.WriteLine(" #" + i + ": " + version);
                        versions.Add(version);
                    }
                }
            }
            return versions;
        }
    }
}
