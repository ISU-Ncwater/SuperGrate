﻿using SuperGrate.Classes;
using SuperGrate.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace SuperGrate
{
    class USMT
    {
        public static bool Canceled = false;
        public static List<string> UploadedGUIDs = new List<string>();
        private static bool Failed = false;
        private static bool Running = false;
        private static string CurrentTarget = "";
        private static Misc.OSArchitecture DetectedArch = Misc.OSArchitecture.Unknown;
        private static string PayloadPathLocal
        {
            get {
                return Config.Settings["SuperGratePayloadPath"];
            }
        }
        private static string PayloadPathTarget {
            get {
                if(Misc.IsHostThisMachine(CurrentTarget))
                {
                    return PayloadPathLocal;
                }
                return Path.Combine(@"\\", CurrentTarget, PayloadPathLocal.Replace(':', '$'));
            }
        }
        private static string USMTPath
        {
            get
            {
                if(DetectedArch == Misc.OSArchitecture.X64)
                {
                    return Config.Settings["USMTPathX64"];
                }
                if(DetectedArch == Misc.OSArchitecture.X86)
                {
                    return Config.Settings["USMTPathX86"];
                }
                return null;
            }
        }
        /// <summary>
        /// Runs the main USMT / Super Grate logic.
        /// </summary>
        /// <param name="Mode">USMT mode to use.</param>
        /// <param name="IDs">List of profiles to either backup or restore.</param>
        /// <returns>A task with bool, true for success.</returns>
        public static Task<bool> Do(USMTMode Mode, string[] IDs)
        {
            UploadedGUIDs.Clear();
            Canceled = false;
            Failed = false;
            string exec = "";
            string configParams = "";
            if(Mode == USMTMode.LoadState)
            {
                exec = "loadstate.exe";
                configParams = Config.Settings["LoadStateParameters"];
                CurrentTarget = Main.DestinationComputer;
            }
            if(Mode == USMTMode.ScanState)
            {
                exec = "scanstate.exe";
                configParams = Config.Settings["ScanStateParameters"];
                CurrentTarget = Main.SourceComputer;
            }
            return Task.Run(async () => {
                DetectedArch = await Misc.GetRemoteArch(CurrentTarget);
                if (Canceled || DetectedArch == Misc.OSArchitecture.Unknown) return false;
                Failed = !await CopyUSMT();
                if (Canceled || Failed) return false;
                Logger.UpdateProgress(0);
                foreach (string ID in IDs)
                {
                    if(Canceled || Failed) break;
                    string SID = "";
                    if (Mode == USMTMode.LoadState)
                    {
                        Failed = !await DownloadFromStore(ID);
                        SID = await Misc.GetSIDFromStore(ID);
                        configParams += await BuildLoadStateMUParameter(ID);
                        Logger.Information("Applying user state: '" + await Misc.GetUserByIdentity(ID) + "' on '" + CurrentTarget + "'...");
                        if (Canceled || Failed || SID == null) break;
                    }
                    if (Mode == USMTMode.ScanState)
                    {
                        Logger.Information("Capturing user state: '" + await Misc.GetUserByIdentity(ID, CurrentTarget) + "' on '" + CurrentTarget + "'...");
                        SID = ID;
                    }
                    Failed = !await Remote.StartProcess(CurrentTarget,
                        Path.Combine(PayloadPathLocal, exec) + " " +
                        PayloadPathLocal + " " +
                        @"/ue:*\* " +
                        "/ui:" + SID + " " +
                        "/l:SuperGrate.log " +
                        "/progress:SuperGrate.progress " +
                        configParams
                    , PayloadPathLocal);
                    if (Canceled || Failed) break;
                    Running = true;
                    StartWatchLog("SuperGrate.log");
                    StartWatchLog("SuperGrate.progress");
                    Failed = !await WaitForUsmtExit(exec.Replace(".exe", ""));
                    if (Canceled || Failed) break;
                    Logger.UpdateProgress(0);
                    if (Mode == USMTMode.LoadState)
                    {
                        Failed = !await SetStoreExportParameters(ID);
                        if (Canceled || Failed) break;
                    }
                    if (Mode == USMTMode.ScanState)
                    {
                        Failed = !await UploadToStore(SID, out string GUID);
                        if (Canceled || Failed) break;
                        UploadedGUIDs.Add(GUID);
                    }
                    Logger.UpdateProgress(0);
                }
                Failed = !await CleaupUSMT();
                if(Canceled || Failed)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            });
        }
        /// <summary>
        /// Copy USMT to the remote machine.
        /// </summary>
        /// <returns>A task with bool, true if success.</returns>
        public static Task<bool> CopyUSMT()
        {
            return Task.Run(async () => {
                Logger.Information("Uploading USMT to: " + CurrentTarget);
                try
                {
                    if (!Directory.Exists(USMTPath))
                    {
                        Logger.Warning("Could not find the USMT folder at: " + USMTPath + ".");
                        if(!await DownloadUSMTFromWeb())
                        {
                            return false;
                        }
                    }
                    if (FileOperations.CopyFolder(
                        USMTPath,
                        PayloadPathTarget
                    )) {
                        Logger.Success("USMT uploaded successfully.");
                        return true;
                    }
                    else
                    {
                        Logger.Warning("USMT upload canceled.");
                        return false;
                    }
                }
                catch(Exception e)
                {
                    Logger.Exception(e, "Error uploading USMT to: " + CurrentTarget);
                    return false;
                }
            });
        }
        /// <summary>
        /// Kill USMT on remote machine.
        /// </summary>
        public static async void Cancel()
        {
            Canceled = true;
            Logger.Information("Sending KILL command to USMT...");
            await Remote.KillProcess(CurrentTarget, "loadstate.exe");
            await Remote.KillProcess(CurrentTarget, "scanstate.exe");
            if(await Remote.KillProcess(CurrentTarget, "mighost.exe"))
            {
                Logger.Success("KILL command sent.");
            }
            else
            {
                Logger.Error("Failed to send KILL command.");
            }
        }
        /// <summary>
        /// Cleanup USMT from remote machine.
        /// </summary>
        /// <returns>A task with bool, true if success.</returns>
        public static Task<bool> CleaupUSMT()
        {
            return Task.Run(async () => {
                int tries = 0;
                bool deleted = false;
                Logger.Information("Removing USMT from: " + CurrentTarget + "...");
                while (tries <= 30)
                {
                    try
                    {
                        Directory.Delete(PayloadPathTarget, true);
                        deleted = true;
                        break;
                    }
                    catch (Exception e)
                    {
                        Logger.Verbose(e.Message);
                        Logger.Verbose(e.StackTrace);
                        if (tries % 5 == 0)
                        {
                            Logger.Warning("Could not delete, USMT might still be running. Attempt " + tries + "/30.");
                        }
                        tries++;
                        await Task.Delay(1000);
                    }
                }
                if(deleted)
                {
                    Logger.Success("USMT removed successfully.");
                    return true;
                }
                else
                {
                    Logger.Error("Could not delete USMT from: " + CurrentTarget + ".");
                    return false;
                }
            });
        }
        /// <summary>
        /// Generate a build load state CLI parameter to use with USMT.
        /// </summary>
        /// <param name="GUID">Store GUID of profile to use.</param>
        /// <returns>A task with a string containing the CLI parameter.</returns>
        private static Task<string> BuildLoadStateMUParameter(string GUID)
        {
            return Task.Run(() => {
                string parameter = "";
                string srcUser = "";
                string dstUser = "";
                try
                {
                    string storePath = Path.Combine(Config.Settings["MigrationStorePath"], GUID);
                    string targetNtAccountPath = Path.Combine(storePath, "targetntaccount");
                    string sourceNtAccountPath = Path.Combine(storePath, "ntaccount");
                    if (File.Exists(targetNtAccountPath) && File.Exists(targetNtAccountPath))
                    {
                        srcUser = File.ReadAllText(sourceNtAccountPath);
                        dstUser = File.ReadAllText(targetNtAccountPath);
                    }
                    if (srcUser != "" && dstUser != "")
                    {
                        Logger.Information("'" + srcUser + "' will be applied as '" + dstUser + "'.");
                        return " /mu:" + srcUser + ":" + dstUser;
                    }
                }
                catch(Exception)
                {
                    Logger.Warning("Failed to build MU parameter.");
                }
                return parameter;
            });
        }
        /// <summary>
        /// Uploads a user from the remote machine to the store.
        /// </summary>
        /// <param name="SID">Profile Security ID.</param>
        /// <param name="GUID">Store GUID.</param>
        /// <returns>A task with bool, true if success.</returns>
        private static Task<bool> UploadToStore(string SID, out string GUID)
        {
            string lGUID = Guid.NewGuid().ToString();
            GUID = lGUID;
            return Task.Run(async () => {
                Logger.Information("Uploading user state to the Store...");
                string Destination = Path.Combine(Config.Settings["MigrationStorePath"], lGUID);
                try
                {
                    Directory.CreateDirectory(Destination);
                    File.WriteAllText(Path.Combine(Destination, "sid"), SID);
                    File.WriteAllText(Path.Combine(Destination, "source"), await Misc.GetHostNameFromHost(CurrentTarget));
                    File.WriteAllText(Path.Combine(Destination, "ntaccount"), await Misc.GetUserByIdentity(SID, CurrentTarget));
                    File.WriteAllText(Path.Combine(Destination, "importedon"), DateTime.Now.ToFileTime().ToString());
                    File.WriteAllText(Path.Combine(Destination, "importedby"), Environment.UserDomainName + "\\" + Environment.UserName);
                    FileOperations.CopyFile(
                        Path.Combine(Path.Combine(PayloadPathTarget, @"USMT\USMT.MIG")),
                        Path.Combine(Destination, "data")
                    );
                    Logger.Success("User state successfully uploaded.");
                    return true;
                }
                catch(Exception e)
                {
                    if (Directory.Exists(Destination))
                    {
                        Directory.Delete(Destination, true);
                    }
                    Logger.Exception(e, "Failed to upload user state to the Store.");
                    return false;
                }
            });
        }
        /// <summary>
        /// Downloads the profile from the store to a destination machine.
        /// </summary>
        /// <param name="GUID">Store GUID to use.</param>
        /// <returns>A task with bool, true if success.</returns>
        private static Task<bool> DownloadFromStore(string GUID)
        {
            return Task.Run(() => {
                Logger.Information(Language.Get("DownloadingUserStateTo", Main.DestinationComputer));
                string Destination = Path.Combine(PayloadPathTarget, "USMT");
                try
                {
                    Directory.CreateDirectory(Destination);
                    FileOperations.CopyFile(
                        Path.Combine(Config.Settings["MigrationStorePath"], GUID, "data"),
                        Path.Combine(Destination, "USMT.MIG")
                    );
                    Logger.Success(Language.Get("UserStateSuccessfullyTransferred"));
                    return true;
                }
                catch(Exception e)
                {
                    Logger.Exception(e, Language.Get("FailedToDownloadUserStateTo", Main.DestinationComputer));
                    return false;
                }
            });
        }
        /// <summary>
        /// Downloads USMT from the web.
        /// </summary>
        /// <returns>A task with bool, true if success.</returns>
        private static Task<bool> DownloadUSMTFromWeb()
        {
            return Task.Run(async () => {
                try
                {
                    bool success = true;
                    Logger.Information(Language.Get("DownloadingUSMTFromTheWeb", DetectedArch.ToString()));
                    if (!Directory.Exists(USMTPath))
                    {
                        Directory.CreateDirectory(USMTPath);
                    }
                    string dlPath = Path.Combine(USMTPath, "USMT.zip");
                    if (DetectedArch == Misc.OSArchitecture.X64)
                    {
                        success = await new Download(Constants.USMTx64URL, dlPath).Start();
                        if (!success) throw new Exception(Language.Get("FailedToDownload", Constants.USMTx64URL));
                    }
                    else if (DetectedArch == Misc.OSArchitecture.X86)
                    {
                        success = await new Download(Constants.USMTx86URL, dlPath).Start();
                        if (!success) throw new Exception(Language.Get("FailedToDownload", Constants.USMTx86URL));
                    }
                    else
                    {
                        throw new Exception(Language.Get("FailedToDetermineTargetArch"));
                    }
                    Logger.Information(Language.Get("DecompressingUSMT"));
                    ZipFile.ExtractToDirectory(dlPath, USMTPath);
                    Logger.Information(Language.Get("CleaningUp"));
                    File.Delete(dlPath);
                    return true;
                }
                catch(Exception e)
                {
                    Directory.Delete(USMTPath, true);
                    Logger.Exception(e, Language.Get("FailedToDownloadUSMT"));
                    return false;
                }
            });
        }
        /// <summary>
        /// Sets the store export parameters.
        /// </summary>
        /// <param name="ID">The GUID to save to.</param>
        /// <returns>A task with bool, True if success.</returns>
        private static Task<bool> SetStoreExportParameters(string ID)
        {
            string storeObjPath = Path.Combine(Config.Settings["MigrationStorePath"], ID);
            return Task.Run(async () => {
                try
                {
                    File.WriteAllText(Path.Combine(storeObjPath, "destination"), await Misc.GetHostNameFromHost(CurrentTarget));
                    File.WriteAllText(Path.Combine(storeObjPath, "exportedby"), Environment.UserDomainName + "\\" + Environment.UserName);
                    File.WriteAllText(Path.Combine(storeObjPath, "exportedon"), DateTime.Now.ToFileTime().ToString());
                    return true;
                }
                catch(Exception e)
                {
                    Logger.Exception(e, Language.Get("FailedToWriteStoreParameter", ID));
                    return false;
                }
            });
        }
        /// <summary>
        /// Waits for USMT to exit.
        /// </summary>
        /// <param name="ImageName">Name of the process to wait on.</param>
        /// <returns>A task with bool, true if USMT exited gracefully.</returns>
        private static async Task<bool> WaitForUsmtExit(string ImageName)
        {
            Running = true;
            bool result = await Remote.WaitForProcessExit(CurrentTarget, ImageName);
            Running = false;
            if (result)
            {
                Logger.Success(Language.Get("USMTFinished"));
            }
            await Task.Delay(3000);
            return result;
        }
        /// <summary>
        /// Watches the USMT log and updates the main UI on progress.
        /// </summary>
        /// <param name="LogFile">Log file to watch.</param>
        private static async void StartWatchLog(string LogFile)
        {
            try
            {
                string logFilePath = Path.Combine(PayloadPathTarget, LogFile);
                FileSystemWatcher logFileWatcher = new FileSystemWatcher(PayloadPathTarget, LogFile)
                {
                    NotifyFilter = NotifyFilters.LastWrite,
                    EnableRaisingEvents = true
                };
                FileStream logStream = File.Open(logFilePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
                StreamReader logReader = new StreamReader(logStream);
                long lastPosition = 0;
                bool firstRead = true;
                while (Running)
                {
                    logStream.Position = lastPosition;
                    string log = await logReader.ReadToEndAsync();
                    lastPosition = logStream.Length;
                    if (log != "" && !firstRead)
                    {
                        string startsWith = "PercentageCompleted, ";
                        if (log.Contains(startsWith))
                        {
                            int start = log.LastIndexOf(startsWith) + startsWith.Length;
                            int end = log.LastIndexOf("\r\n");
                            if (int.TryParse(log.Substring(start, end - start), out int percent))
                            {
                                Logger.UpdateProgress(percent);
                                Logger.Verbose(log, true);
                            }
                        }
                        else
                        {
                            Logger.Verbose(log, true);
                        }
                    }
                    firstRead = false;
                    logFileWatcher.WaitForChanged(WatcherChangeTypes.Changed, 3000);
                }
                logStream.Close();
                logFileWatcher.Dispose();
            }
            catch(Exception e)
            {
                Logger.Exception(e, Language.Get("FailedToWatchRemoteLog", LogFile));
            }
        }
    }
    public enum USMTMode
    {
        ScanState = 1,
        LoadState = 2
    }
}